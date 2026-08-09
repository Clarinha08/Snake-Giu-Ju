using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Bildschirmtexte über IMGUI – braucht keine Fonts oder Prefabs und skaliert
    /// dadurch ohne Zutun auf Handy- wie Desktop-Auflösungen.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        static readonly Color Accent = new Color(0.220f, 0.882f, 0.690f, 1f);
        static readonly Color Muted = new Color(0.72f, 0.77f, 0.85f, 1f);

        GameManager game;
        GUIStyle score;
        GUIStyle best;
        GUIStyle title;
        GUIStyle subtitle;
        GUIStyle call;
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

            GUI.Label(bar, $"Länge  {game.Distance:0} m", score);
            GUI.Label(bar, $"Rekord  {game.BestDistance:0} m", best);

            switch (game.State)
            {
                case GameState.Ready:
                    DrawCentered("SNAKE GIU JU", Steuerhinweis(), "Tippen oder Leertaste zum Start");
                    break;

                case GameState.Playing:
                    DrawZoneHints();
                    break;

                case GameState.GameOver:
                    string reason = game.Cause == DeathCause.Wall ? "Gegen den Rand gefahren" : "In eine Kurve gefahren";
                    DrawCentered("AUS", $"{reason} · {game.Distance:0} m", "Tippen oder Leertaste für neue Runde");
                    break;
            }
        }

        void DrawCentered(string headline, string line, string prompt)
        {
            float w = Screen.width;
            float h = Screen.height;

            float margin = w * 0.06f;
            GUI.Label(new Rect(margin, h * 0.30f, w - margin * 2f, h * 0.18f), headline, title);
            GUI.Label(new Rect(margin, h * 0.50f, w - margin * 2f, h * 0.12f), line, subtitle);
            GUI.Label(new Rect(margin, h * 0.64f, w - margin * 2f, h * 0.10f), prompt, call);
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

        static string Steuerhinweis()
        {
            return SteeringInput.HasTouchscreen
                ? "Linke oder rechte Bildschirmhälfte gedrückt halten"
                : "Pfeiltaste links oder rechts gedrückt halten";
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
            score.normal.textColor = Accent;

            best = new GUIStyle(score) { alignment = TextAnchor.UpperRight };
            best.normal.textColor = Muted;

            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 2.6f),
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
            call.normal.textColor = Accent;

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
