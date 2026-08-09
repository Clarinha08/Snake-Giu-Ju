using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Bildschirmtexte und Charakterauswahl über IMGUI – braucht keine Fonts oder
    /// Prefabs und skaliert dadurch ohne Zutun auf Handy- wie Desktop-Auflösungen.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        static readonly Color Muted = new Color(0.72f, 0.77f, 0.85f, 1f);

        GameManager game;
        GUIStyle score;
        GUIStyle best;
        GUIStyle title;
        GUIStyle subtitle;
        GUIStyle call;
        GUIStyle characterName;
        GUIStyle zoneLeft;
        GUIStyle zoneRight;
        int builtForHeight = -1;
        int builtForWidth = -1;

        public void Bind(GameManager gameManager) => game = gameManager;

        void OnGUI()
        {
            if (game == null) return;

            BuildStyles();
            float w = Screen.width;
            float h = Screen.height;
            float margin = Mathf.Min(w, h) * 0.04f;
            var bar = new Rect(margin, margin, w - margin * 2f, h * 0.06f);

            // Der Akzent folgt dem gewählten Charakter.
            Color accent = game.Selected.color;
            score.normal.textColor = accent;
            call.normal.textColor = accent;

            if (game.State != GameState.CharacterSelect)
            {
                GUI.Label(bar, $"Länge  {game.Distance:0} m", score);
            }
            GUI.Label(bar, $"Rekord  {game.BestDistance:0} m", best);

            switch (game.State)
            {
                case GameState.CharacterSelect:
                    GUI.Label(TextRect(w, h, 0.10f, 0.11f), "SNAKE GIU JU", title);
                    GUI.Label(TextRect(w, h, 0.21f, 0.10f), Steuerhinweis(), subtitle);
                    DrawPicker(w, h, 0.52f);
                    GUI.Label(TextRect(w, h, 0.78f, 0.10f), Auswahlhinweis(), call);
                    break;

                case GameState.Playing:
                    DrawZoneHints();
                    break;

                case GameState.GameOver:
                    string reason = game.Cause == DeathCause.Wall
                        ? "Gegen den Rand gefahren"
                        : "In eine Kurve gefahren";
                    GUI.Label(TextRect(w, h, 0.10f, 0.11f), "AUS", title);
                    GUI.Label(TextRect(w, h, 0.21f, 0.10f), $"{reason} · {game.Distance:0} m", subtitle);
                    DrawPicker(w, h, 0.52f);
                    GUI.Label(TextRect(w, h, 0.78f, 0.10f), Auswahlhinweis(), call);
                    break;
            }
        }

        /// <summary>Beide Charaktere nebeneinander, der gewählte hervorgehoben.</summary>
        void DrawPicker(float w, float h, float centerY)
        {
            var characters = game.Characters;
            if (characters == null || characters.Count == 0) return;

            float box = Mathf.Min(w * 0.36f, h * 0.30f);
            float gap = box * 0.18f;
            float totalWidth = characters.Count * box + (characters.Count - 1) * gap;
            float x = (w - totalWidth) * 0.5f;
            float y = h * centerY - box * 0.5f;
            float border = Mathf.Max(2f, box * 0.02f);

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition character = characters[i];
                var frame = new Rect(x + i * (box + gap), y, box, box);
                bool selected = i == game.SelectedIndex;

                Color previous = GUI.color;
                if (selected)
                {
                    DrawRect(frame, new Color(character.color.r, character.color.g, character.color.b, 0.12f));
                    DrawFrame(frame, character.color, border);
                }
                else
                {
                    // Nicht gewählte Figur bewusst zurücknehmen statt ausgrauen –
                    // die Neonfarbe bleibt so als Vorschau lesbar.
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                }

                if (character.portrait != null)
                {
                    float pad = box * 0.06f;
                    var inner = new Rect(frame.x + pad, frame.y + pad, frame.width - pad * 2f, frame.height - pad * 2f);
                    GUI.DrawTexture(inner, character.portrait, ScaleMode.ScaleToFit);
                }
                else
                {
                    DrawRect(frame, character.color);
                }

                GUI.color = previous;

                characterName.normal.textColor = selected ? character.color : Muted;
                GUI.Label(new Rect(frame.x, frame.yMax + box * 0.04f, frame.width, box * 0.22f),
                    character.displayName, characterName);
            }
        }

        void DrawZoneHints()
        {
            if (!SteeringInput.HasTouchscreen) return;

            float w = Screen.width;
            float h = Screen.height;
            // Nur ASCII: der eingebaute IMGUI-Font hat keine Pfeil-Glyphen.
            GUI.Label(new Rect(0f, h * 0.90f, w * 0.5f, h * 0.08f), "<<  links", zoneLeft);
            GUI.Label(new Rect(w * 0.5f, h * 0.90f, w * 0.5f, h * 0.08f), "rechts  >>", zoneRight);
        }

        static Rect TextRect(float w, float h, float top, float height)
        {
            float margin = w * 0.06f;
            return new Rect(margin, h * top, w - margin * 2f, h * height);
        }

        static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void DrawFrame(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        static string Steuerhinweis()
        {
            return SteeringInput.HasTouchscreen
                ? "Im Spiel: linke oder rechte Bildschirmhälfte gedrückt halten"
                : "Im Spiel: Pfeiltaste links oder rechts gedrückt halten";
        }

        static string Auswahlhinweis()
        {
            return SteeringInput.HasTouchscreen
                ? "Auf die linke oder rechte Bildschirmhälfte tippen und loslegen"
                : "Mit den Pfeiltasten wählen, Leertaste startet";
        }

        void BuildStyles()
        {
            if (builtForHeight == Screen.height && builtForWidth == Screen.width && score != null) return;
            builtForHeight = Screen.height;
            builtForWidth = Screen.width;

            // An der kürzeren Kante ausrichten, sonst wird die Schrift im Hochformat
            // eines Handys so groß, dass Überschrift und Punktestand kollidieren.
            int baseSize = Mathf.Max(11, Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height * 0.75f) * 0.04f));

            score = new GUIStyle(GUI.skin.label)
            {
                fontSize = baseSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };

            best = new GUIStyle(score) { alignment = TextAnchor.UpperRight };
            best.normal.textColor = Muted;

            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 2.2f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = Color.white;

            subtitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.95f),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subtitle.normal.textColor = Muted;

            call = new GUIStyle(subtitle);

            characterName = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.2f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            zoneLeft = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.85f),
                alignment = TextAnchor.MiddleCenter
            };
            zoneLeft.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
            zoneRight = new GUIStyle(zoneLeft);
        }
    }
}
