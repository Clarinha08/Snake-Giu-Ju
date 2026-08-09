using UnityEngine;

namespace SnakeGiuJu
{
    public enum GameState
    {
        Ready,
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
        const string BestDistanceKey = "SnakeGiuJu.BestDistance";

        [Header("Rendering")]
        [SerializeField] Shader lineShader;
        [SerializeField] Color backgroundColor = new Color(0.043f, 0.055f, 0.078f, 1f);
        [SerializeField] Color borderColor = new Color(0.298f, 0.361f, 0.482f, 1f);
        [SerializeField] Color trailColor = new Color(0.220f, 0.882f, 0.690f, 1f);
        [SerializeField] Color headColor = new Color(1f, 1f, 1f, 1f);

        [Header("Arena")]
        [SerializeField] float arenaHeight = 20f;
        [SerializeField] float cellSize = 0.05f;

        [Header("Schlange")]
        [SerializeField] float lineWidth = 0.36f;
        [SerializeField] float moveSpeed = 7.2f;
        [SerializeField] float minTurnRadius = 1.6f;

        Camera cam;
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

        public GameState State { get; private set; } = GameState.Ready;
        public float Distance => player?.Distance ?? 0f;
        public float BestDistance { get; private set; }
        public DeathCause Cause => player?.Cause ?? DeathCause.None;

        void Awake()
        {
            Application.targetFrameRate = 60;
            BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);

            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;

            lineMaterial = new Material(lineShader) { name = "Line" };
            headMaterial = new Material(lineShader) { name = "Head" };
            headMaterial.SetColor("_BaseColor", headColor);

            CreateBorder();
            CreateHead();

            trail = new TrailPainter(transform, lineMaterial, trailColor, lineWidth, lineWidth * 0.5f);
            gameObject.AddComponent<Hud>().Bind(this);

            BuildArena();
            ResetRound();
        }

        void Update()
        {
            FitCamera();

            switch (State)
            {
                case GameState.Ready:
                    if (SteeringInput.ConfirmPressed()) SetState(GameState.Playing);
                    break;

                case GameState.Playing:
                    Simulate();
                    break;

                case GameState.GameOver:
                    // Kurze Sperre, damit der Tipp, der zum Tod geführt hat, nicht sofort neu startet.
                    if (Time.time - stateChangedAt > 0.5f && SteeringInput.ConfirmPressed())
                    {
                        ResetRound();
                        SetState(GameState.Playing);
                    }
                    break;
            }
        }

        void Simulate()
        {
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
            if (player.Distance > BestDistance)
            {
                BestDistance = player.Distance;
                PlayerPrefs.SetFloat(BestDistanceKey, BestDistance);
                PlayerPrefs.Save();
            }
            SetState(GameState.GameOver);
        }

        void SetState(GameState state)
        {
            State = state;
            stateChangedAt = Time.time;
        }

        void ResetRound()
        {
            grid.Clear();
            accumulator = 0f;
            player.Spawn(Vector2.zero, Random.Range(0f, 360f));
            trail.SetHead(player.Position);
            head.position = new Vector3(player.Position.x, player.Position.y, -0.2f);
        }

        void BuildArena()
        {
            // Die Arena übernimmt das Seitenverhältnis des Bildschirms, bleibt aber in
            // Grenzen spielbar – ein sehr schmales Hochformat wäre sonst kaum steuerbar.
            float aspect = Mathf.Clamp(Screen.width / (float)Mathf.Max(Screen.height, 1), 0.55f, 2.4f);
            float width = arenaHeight * aspect;
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
            go.transform.localScale = Vector3.one * (lineWidth * 1.3f);
            go.AddComponent<MeshFilter>().sharedMesh = MeshShapes.CreateDisc();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = headMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            head = go.transform;
        }
    }
}
