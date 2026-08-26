using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Flächen, die das HUD zeichnet und die Spiellogik treffen muss. Beide Seiten
    /// rechnen hier, damit ein Element nicht woanders sitzt als es reagiert.
    /// </summary>
    public static class HudLayout
    {
        /// <summary>Schalter für den Power-up-Modus, in GUI-Koordinaten (Ursprung oben links).</summary>
        public static Rect PowerUpSwitch(float screenWidth, float screenHeight)
        {
            float height = Mathf.Min(screenWidth, screenHeight * 0.75f) * 0.075f;
            // Breit genug für Beschriftung, Zustandstext und Schaltbahn nebeneinander.
            float width = Mathf.Min(height * 7.6f, screenWidth * 0.9f);
            return new Rect((screenWidth - width) * 0.5f, screenHeight * 0.66f, width, height);
        }

        /// <summary>Start-Button, in GUI-Koordinaten (Ursprung oben links).</summary>
        public static Rect StartButton(float screenWidth, float screenHeight)
        {
            float height = Mathf.Min(screenWidth, screenHeight * 0.75f) * 0.11f;
            float width = Mathf.Min(screenWidth * 0.7f, height * 5f);
            return new Rect((screenWidth - width) * 0.5f, screenHeight * 0.87f, width, height);
        }

        /// <summary>„Play again“-Button auf dem Game-Over-Screen, startet mit denselben
        /// Einstellungen (Charakter, Power-ups) neu, die zu Rundenbeginn galten.</summary>
        public static Rect PlayAgainButton(float screenWidth, float screenHeight)
        {
            float height = Mathf.Min(screenWidth, screenHeight * 0.75f) * 0.11f;
            float width = Mathf.Min(screenWidth * 0.7f, height * 5f);
            return new Rect((screenWidth - width) * 0.5f, screenHeight * 0.60f, width, height);
        }

        /// <summary>Textlink zurück zur Charakterauswahl auf dem Game-Over-Screen.</summary>
        public static Rect BackToStartLink(float screenWidth, float screenHeight)
        {
            float height = Mathf.Min(screenWidth, screenHeight * 0.75f) * 0.07f;
            float width = screenWidth * 0.7f;
            return new Rect((screenWidth - width) * 0.5f, screenHeight * 0.75f, width, height);
        }

        /// <summary>Rechnet eine Zeigerposition (Ursprung unten links) in GUI-Koordinaten um.</summary>
        public static Vector2 ToGuiSpace(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }
    }
}
