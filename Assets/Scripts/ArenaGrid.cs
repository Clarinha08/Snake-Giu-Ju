using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Belegungsraster der Arena. Gezeichnete Linien werden als Kapseln eingestempelt,
    /// die Kollisionsabfrage ist eine Kreisabfrage. Dadurch bleiben die Kosten pro Frame
    /// konstant, egal wie lang die Linien schon geworden sind.
    /// </summary>
    public sealed class ArenaGrid
    {
        readonly float cellSize;
        readonly float invCellSize;
        readonly int cols;
        readonly int rows;
        readonly bool[] occupied;
        readonly Vector2 origin;

        public Rect Bounds { get; }

        public ArenaGrid(Rect bounds, float cellSize)
        {
            Bounds = bounds;
            this.cellSize = cellSize;
            invCellSize = 1f / cellSize;
            origin = bounds.min;
            cols = Mathf.CeilToInt(bounds.width * invCellSize);
            rows = Mathf.CeilToInt(bounds.height * invCellSize);
            occupied = new bool[cols * rows];
        }

        public void Clear()
        {
            System.Array.Clear(occupied, 0, occupied.Length);
        }

        /// <summary>Markiert die Fläche einer Linie von <paramref name="a"/> nach <paramref name="b"/>.</summary>
        public void StampCapsule(Vector2 a, Vector2 b, float radius)
        {
            float sqrRadius = radius * radius;
            GetCellRange(a, b, radius, out int c0, out int c1, out int r0, out int r1);

            for (int r = r0; r <= r1; r++)
            {
                int rowOffset = r * cols;
                float y = origin.y + (r + 0.5f) * cellSize;
                for (int c = c0; c <= c1; c++)
                {
                    float x = origin.x + (c + 0.5f) * cellSize;
                    if (SqrDistanceToSegment(new Vector2(x, y), a, b) <= sqrRadius)
                    {
                        occupied[rowOffset + c] = true;
                    }
                }
            }
        }

        /// <summary>Prüft, ob ein Kreis bereits belegte Fläche berührt.</summary>
        public bool OverlapsDisc(Vector2 center, float radius)
        {
            float sqrRadius = radius * radius;
            GetCellRange(center, center, radius, out int c0, out int c1, out int r0, out int r1);

            for (int r = r0; r <= r1; r++)
            {
                int rowOffset = r * cols;
                float y = origin.y + (r + 0.5f) * cellSize;
                float dy = y - center.y;
                for (int c = c0; c <= c1; c++)
                {
                    if (!occupied[rowOffset + c]) continue;
                    float dx = origin.x + (c + 0.5f) * cellSize - center.x;
                    if (dx * dx + dy * dy <= sqrRadius) return true;
                }
            }

            return false;
        }

        void GetCellRange(Vector2 a, Vector2 b, float radius, out int c0, out int c1, out int r0, out int r1)
        {
            c0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - radius - origin.x) * invCellSize), 0, cols - 1);
            c1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, b.x) + radius - origin.x) * invCellSize), 0, cols - 1);
            r0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.y, b.y) - radius - origin.y) * invCellSize), 0, rows - 1);
            r1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.y, b.y) + radius - origin.y) * invCellSize), 0, rows - 1);
        }

        static float SqrDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqrLength = ab.sqrMagnitude;
            if (sqrLength < 1e-8f) return (p - a).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLength);
            return (p - (a + ab * t)).sqrMagnitude;
        }
    }
}
