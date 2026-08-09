using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Ein waehlbarer Charakter. Die Farbe faerbt die Linie im Spiel, das Bild
    /// erscheint im Auswahlscreen.
    /// </summary>
    [System.Serializable]
    public sealed class CharacterDefinition
    {
        public string displayName = "Giu";
        public Color color = new Color(0.133f, 0.890f, 1f, 1f);
        public Texture2D portrait;
    }
}
