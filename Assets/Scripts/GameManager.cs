using System.Collections.Generic;
using UnityEngine;

namespace SnakeGiuJu
{
    public enum GameState
    {
        CharacterSelect,
        Playing,
        GameOver
    }

    /// <summary>
    /// Baut Arena, Kamera und Spieler auf und treibt die Simulation an. Alles Sichtbare
    /// wird zur Laufzeit erzeugt, die Szene enthält nur Kamera und dieses Skript.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        const float SimulationStep = 1f / 120f;
        // Der Rekord zaehlt seit dem Power-up-Modus Punkte statt Meter, deshalb ein
        // neuer Schluessel - alte Meter-Rekorde waeren als Punkte gelesen irrefuehrend.
        const string BestScoreKey = "SnakeGiuJu.BestScore";
        const string PowerUpsKey = "SnakeGiuJu.PowerUps";

        [Header("Rendering")]
        [SerializeField] Shader lineShader;
        [SerializeField] Shader headShader;
        [SerializeField] Color backgroundColor = new Color(0.043f, 0.055f, 0.078f, 1f);
        [SerializeField] Color borderColor = new Color(0.298f, 0.361f, 0.482f, 1f);

        [Header("Charaktere")]
        [SerializeField] CharacterDefinition[] characters;

        [Header("Arena")]
        [SerializeField] float arenaHeight = 20f;
        [SerializeField] float cellSize = 0.05f;

        [Header("Schlange")]
        [SerializeField] float lineWidth = 0.36f;
        [SerializeField] float moveSpeed = 7.2f;
        [SerializeField] float minTurnRadius = 1.6f;
        // Größer als die Linienbreite, damit das Gesicht auf dem Porträt erkennbar
        // bleibt - aber nicht so groß, dass der Kopf beim Steuern die Sicht auf
        // nahende Kurven verdeckt.
        [SerializeField] float headScale = 2.4f;

        Camera cam;
        PowerUpField powerUps;
        Material lineMaterial;
        Material headMaterial;
        LineRenderer border;
        Transform head;
        ArenaGrid grid;
        TrailPainter trail;
        CurvePlayer player;
        Rect arena;
        float accumulator;
        float stateChangedAt;
        int selectedIndex;
        float widthFactor = 1f;
        float speedBoostUntil;
        bool suppressPointerSteering;

        public GameState State { get; private set; } = GameState.CharacterSelect;
        public float Score => player?.Score ?? 0f;
        public float BestScore { get; private set; }
        public DeathCause Cause => player?.Cause ?? DeathCause.None;

        public bool PowerUpsEnabled { get; private set; }

        /// <summary>Restlaufzeit des Temposchubs, 0 wenn keiner aktiv ist.</summary>
        public float BoostRemaining => Mathf.Max(0f, speedBoostUntil - Time.time);

        /// <summary>Zuletzt eingesammeltes Power-up und wann - fuer die Einblendung im HUD.</summary>
        public PowerUpKind LastPickup { get; private set; }
        public float LastPickupAt { get; private set; } = -99f;

        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public int SelectedIndex => selectedIndex;
        public CharacterDefinition Selected => characters[selectedIndex];

        void Awake()
        {
            Application.targetFrameRate = 60;
            BestScore = PlayerPrefs.GetFloat(BestScoreKey, 0f);
            PowerUpsEnabled = PlayerPrefs.GetInt(PowerUpsKey, 0) != 0;
            EnsureCharacters();

            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;

            lineMaterial = new Material(lineShader) { name = "Line" };
            headMaterial = new Material(headShader) { name = "Head" };

            CreateBorder();
            CreateHead();

            trail = new TrailPainter(transform, lineMaterial, Selected.color, lineWidth, lineWidth * 0.5f);
            powerUps = new PowerUpField(transform, lineShader);
            gameObject.AddComponent<Hud>().Bind(this);

            // Noch keine Runde aufsetzen: vor der ersten Auswahl soll weder Kopf
            // noch Linie im Spielfeld stehen.
            BuildArena();
            SetState(GameState.CharacterSelect);
        }

        void Update()
        {
            // Zwischen den Runden die Arena an die Fenstergröße nachziehen, damit der
            // Rahmen nicht beim Start umspringt. Während einer Runde nicht anfassen:
            // das würde die schon gezogene Linie verlieren.
            if (State != GameState.Playing) BuildArena();
            FitCamera();

            switch (State)
            {
                case GameState.CharacterSelect:
                    UpdatePicker(0f);
                    break;

                case GameState.Playing:
                    Simulate();
                    break;

                case GameState.GameOver:
                    // Kurze Sperre, damit der Tipp, der zum Tod geführt hat, nicht sofort neu startet.
                    UpdatePicker(0.5f);
                    break;
            }
        }

        /// <summary>
        /// Charakterauswahl: links wählt den ersten, rechts den letzten Charakter –
        /// dieselbe Geste wie das Lenken im Spiel. Gestartet wird nur noch über den
        /// eigenen Start-Button (oder Leertaste/Enter), ein Tipp irgendwo auf dem
        /// Bildschirm wählt nur noch, startet aber nicht mehr automatisch mit.
        /// </summary>
        void UpdatePicker(float guardSeconds)
        {
            bool ready = Time.time - stateChangedAt >= guardSeconds;

            // Der Schalter muss zuerst geprueft werden: ein Tipp auf ihn soll
            // umschalten, nicht die Charakterauswahl darunter beeinflussen.
            if (ready && (SteeringInput.TogglePressed() || PressLandedOn(HudLayout.PowerUpSwitch)))
            {
                TogglePowerUps();
                // Ein Tipp bleibt ueber mehrere Frames "gehalten", bis er losgelassen
                // wird. Ohne diese Sperre wuerde die noch gehaltene Zeigerposition auf
                // dem Schalter im naechsten Frame als Lenkeingabe gelesen und die
                // Charakterauswahl umspringen lassen.
                suppressPointerSteering = true;
                return;
            }

            if (suppressPointerSteering)
            {
                if (!SteeringInput.IsPointerDown()) suppressPointerSteering = false;
            }
            else
            {
                int steering = SteeringInput.ReadSteering();
                if (steering != 0) selectedIndex = steering < 0 ? 0 : characters.Length - 1;
            }

            if (!ready) return;
            if (!SteeringInput.KeyboardConfirmPressed() && !PressLandedOn(HudLayout.StartButton)) return;

            ResetRound();
            SetState(GameState.Playing);
        }

        static bool PressLandedOn(System.Func<float, float, Rect> area)
        {
            if (!SteeringInput.TryGetPressPosition(out Vector2 press)) return false;
            return area(Screen.width, Screen.height).Contains(HudLayout.ToGuiSpace(press));
        }

        void TogglePowerUps()
        {
            PowerUpsEnabled = !PowerUpsEnabled;
            PlayerPrefs.SetInt(PowerUpsKey, PowerUpsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!PowerUpsEnabled) powerUps.Clear();
        }

        void Collect(PowerUpKind kind)
        {
            LastPickup = kind;
            LastPickupAt = Time.time;

            switch (kind)
            {
                case PowerUpKind.Fett:
                    ScaleWidth(PowerUpRules.FettFactor);
                    break;
                case PowerUpKind.Duenn:
                    ScaleWidth(PowerUpRules.DuennFactor);
                    break;
                default:
                    // Ein zweiter Schub verlaengert, statt sich zu stapeln.
                    speedBoostUntil = Time.time + PowerUpRules.SpeedDuration;
                    break;
            }
        }

        void ScaleWidth(float factor)
        {
            widthFactor = Mathf.Clamp(widthFactor * factor,
                PowerUpRules.MinWidthFactor, PowerUpRules.MaxWidthFactor);
            ApplyWidth();
        }

        void ApplyWidth()
        {
            float width = lineWidth * widthFactor;
            player.Radius = width * 0.5f;
            trail.SetWidth(width);
            head.localScale = Vector3.one * (width * headScale);
        }

        void Simulate()
        {
            if (PowerUpsEnabled)
            {
                powerUps.Tick(Time.time, player.Position, player.Radius);
                for (int i = 0; i < powerUps.Collected.Count; i++) Collect(powerUps.Collected[i]);
            }

            bool boosting = Time.time < speedBoostUntil;
            player.SpeedMultiplier = boosting ? PowerUpRules.SpeedFactor : 1f;
            player.ScoreMultiplier = boosting ? PowerUpRules.SpeedScoreFactor : 1f;

            int steering = SteeringInput.ReadSteering();
            accumulator += Mathf.Min(Time.deltaTime, 0.25f);

            while (accumulator >= SimulationStep && player.Alive)
            {
                player.Step(steering, SimulationStep);
                accumulator -= SimulationStep;
            }

            trail.SetHead(player.Position);
            head.position = new Vector3(player.Position.x, player.Position.y, -0.2f);

            if (player.Alive) return;

            accumulator = 0f;
            if (player.Score > BestScore)
            {
                BestScore = player.Score;
                PlayerPrefs.SetFloat(BestScoreKey, BestScore);
                PlayerPrefs.Save();
            }
            SetState(GameState.GameOver);
        }

        void SetState(GameState state)
        {
            State = state;
            stateChangedAt = Time.time;
            // Sperre nicht ueber Zustandswechsel hinweg mitschleppen - ein neuer
            // Auswahlscreen soll nie mit einer Sperre aus einer vorigen Runde starten.
            suppressPointerSteering = false;
            // Vor der ersten Runde gibt es noch nichts zu zeigen - der Kopf soll
            // nicht als loser Punkt hinter dem Auswahlscreen stehen.
            head.gameObject.SetActive(state != GameState.CharacterSelect);
        }

        void ResetRound()
        {
            BuildArena();
            grid.Clear();
            accumulator = 0f;
            speedBoostUntil = 0f;
            LastPickupAt = -99f;

            // Breite vor dem Spawn zuruecksetzen: Spawn legt den ersten Linienabschnitt
            // an und der uebernimmt die dann gueltige Breite.
            widthFactor = 1f;
            trail.Color = Selected.color;
            // Fallback-Charaktere ohne Bild (siehe EnsureCharacters) zeigen die
            // Standardtextur des Shaders statt eines fehlenden Porträts.
            headMaterial.SetTexture("_MainTex", Selected.portrait);
            ApplyWidth();

            player.Spawn(Vector2.zero, Random.Range(0f, 360f));
            trail.SetHead(player.Position);
            head.position = new Vector3(player.Position.x, player.Position.y, -0.2f);
            powerUps.Restart(grid, Time.time);
        }

        /// <summary>
        /// Fällt auf zwei Standardcharaktere zurück, falls die Szene keine liefert –
        /// ohne sie hätte der Auswahlscreen nichts zu zeigen.
        /// </summary>
        void EnsureCharacters()
        {
            if (characters == null || characters.Length < 2)
            {
                Debug.LogWarning("Keine Charaktere in der Szene gesetzt, benutze Standardfarben ohne Bilder.");
                characters = new[]
                {
                    new CharacterDefinition { displayName = "Giu", color = new Color(1f, 0.247f, 0.816f, 1f) },
                    new CharacterDefinition { displayName = "Ju", color = new Color(0.133f, 0.890f, 1f, 1f) }
                };
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, characters.Length - 1);
        }

        /// <summary>
        /// Legt die Arena auf das aktuelle Seitenverhältnis aus. Wird vor jeder Runde
        /// aufgerufen, denn im Browser steht die endgültige Canvas-Größe beim Start des
        /// Spiels noch nicht fest – und zwischen zwei Runden kann sich das Fenster ändern.
        /// </summary>
        void BuildArena()
        {
            // Die Arena übernimmt das Seitenverhältnis des Bildschirms, bleibt aber in
            // Grenzen spielbar – ein sehr schmales Hochformat wäre sonst kaum steuerbar.
            float aspect = Mathf.Clamp(Screen.width / (float)Mathf.Max(Screen.height, 1), 0.55f, 2.4f);
            float width = arenaHeight * aspect;

            if (grid != null && Mathf.Abs(width - arena.width) < 0.01f) return;

            arena = new Rect(-width * 0.5f, -arenaHeight * 0.5f, width, arenaHeight);
            grid = new ArenaGrid(arena, cellSize);
            player = new CurvePlayer(grid, trail, lineWidth * 0.5f, moveSpeed, minTurnRadius);

            border.positionCount = 5;
            border.SetPosition(0, new Vector3(arena.xMin, arena.yMin, 0.1f));
            border.SetPosition(1, new Vector3(arena.xMax, arena.yMin, 0.1f));
            border.SetPosition(2, new Vector3(arena.xMax, arena.yMax, 0.1f));
            border.SetPosition(3, new Vector3(arena.xMin, arena.yMax, 0.1f));
            border.SetPosition(4, new Vector3(arena.xMin, arena.yMin, 0.1f));
        }

        void FitCamera()
        {
            float aspect = Mathf.Max(cam.aspect, 0.01f);
            cam.orthographicSize = Mathf.Max(arena.height * 0.5f, arena.width * 0.5f / aspect) * 1.04f;
        }

        void CreateBorder()
        {
            var go = new GameObject("Border");
            go.transform.SetParent(transform, false);

            border = go.AddComponent<LineRenderer>();
            border.sharedMaterial = lineMaterial;
            border.useWorldSpace = true;
            border.widthMultiplier = 0.12f;
            border.numCornerVertices = 0;
            border.numCapVertices = 0;
            border.alignment = LineAlignment.View;
            border.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            border.receiveShadows = false;
            border.startColor = borderColor;
            border.endColor = borderColor;
        }

        void CreateHead()
        {
            var go = new GameObject("Head");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * (lineWidth * headScale);
            // Die Kreisform kommt vom Alphakanal des Porträts (siehe
            // Art/prepare_avatar_photos.py), nicht von der Mesh-Form - ein
            // texturiertes Quadrat reicht deshalb statt einer runden Mesh.
            go.AddComponent<MeshFilter>().sharedMesh = MeshShapes.CreateQuad();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = headMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            head = go.transform;
        }
    }
}
