using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Erzeugt abgerundete Formen als Textur zur Laufzeit - ein weißes Alpha-Mask,
    /// das über GUI.color eingefärbt wird. So lassen sich runde IMGUI-Elemente
    /// zeichnen, ohne eine Bilddatei mitzuliefern.
    /// </summary>
    public static class UITextures
    {
        /// <summary>
        /// Rechteck mit demselben Radius in allen vier Ecken. Radius = halbe Höhe
        /// ergibt eine Kapselform (Stadium), Radius = halbe Kantenlänge bei einem
        /// quadratischen Bild einen Kreis.
        /// </summary>
        public static Texture2D RoundedRect(int width, int height, float radius)
        {
            width = Mathf.Max(2, width);
            height = Mathf.Max(2, height);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = SignedDistance(x + 0.5f, y + 0.5f, width, height, radius);
                    // ~1px weicher Rand gegen Treppenstufen an der Kontur.
                    byte alpha = (byte)(Mathf.Clamp01(0.5f - dist) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Vorzeichenbehafteter Abstand zum Rand eines abgerundeten Rechtecks -
        /// negativ innerhalb, positiv außerhalb. Standardformel für ein Rechteck mit
        /// gleichmäßig abgerundeten Ecken (Inigo Quilez, sdRoundBox).
        /// </summary>
        static float SignedDistance(float px, float py, float width, float height, float radius)
        {
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float qx = Mathf.Abs(px - halfW) - halfW + radius;
            float qy = Mathf.Abs(py - halfH) - halfH + radius;
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float outsideDist = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
            float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outsideDist + insideDist - radius;
        }
    }
}
