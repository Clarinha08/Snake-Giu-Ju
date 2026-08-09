using System.Collections.Generic;
using UnityEngine;

namespace SnakeGiuJu
{
    public enum DeathCause
    {
        None,
        Wall,
        Trail
    }

    /// <summary>
    /// Der Kopf der Schlange. Er fährt mit konstanter Geschwindigkeit und kann nur mit
    /// begrenzter Winkelgeschwindigkeit lenken – daraus ergibt sich der Mindestradius
    /// (Radius = Geschwindigkeit / Winkelgeschwindigkeit). Die gefahrene Strecke bleibt
    /// als Hindernis liegen und wird nie wieder gelöscht.
    /// </summary>
    public sealed class CurvePlayer
    {
        readonly ArenaGrid grid;
        readonly TrailPainter trail;
        readonly float radius;
        readonly float speed;
        readonly float turnRateDeg;
        readonly float paintDelay;

        // Das jüngste Stück hinter dem Kopf wird bewusst noch nicht ins Raster
        // gestempelt, sonst würde der Kopf sofort mit seinem eigenen Körper kollidieren.
        readonly Queue<Vector2> pending = new Queue<Vector2>();
        float pendingLength;
        Vector2 lastStamped;

        public Vector2 Position { get; private set; }
        public float HeadingDeg { get; private set; }
        public float Distance { get; private set; }
        public bool Alive { get; private set; }
        public DeathCause Cause { get; private set; }

        public Vector2 Direction => new Vector2(
            Mathf.Cos(HeadingDeg * Mathf.Deg2Rad),
            Mathf.Sin(HeadingDeg * Mathf.Deg2Rad));

        public CurvePlayer(ArenaGrid grid, TrailPainter trail, float radius, float speed, float minTurnRadius)
        {
            this.grid = grid;
            this.trail = trail;
            this.radius = radius;
            this.speed = speed;
            turnRateDeg = speed / Mathf.Max(minTurnRadius, 0.01f) * Mathf.Rad2Deg;
            // Reicht, um den eigenen Kopf freizuhalten, ist aber weit kürzer als ein
            // enger Vollkreis (2*pi*Mindestradius) – zu eng gefahrene Schleifen bleiben tödlich.
            paintDelay = radius * 4f;
        }

        public void Spawn(Vector2 position, float headingDeg)
        {
            Position = position;
            HeadingDeg = headingDeg;
            Distance = 0f;
            Alive = true;
            Cause = DeathCause.None;

            pending.Clear();
            pendingLength = 0f;
            lastStamped = position;
            trail.Restart(position);
        }

        /// <summary>Ein Simulationsschritt mit fester Schrittweite.</summary>
        public void Step(int steering, float dt)
        {
            if (!Alive) return;

            HeadingDeg -= steering * turnRateDeg * dt;
            Vector2 next = Position + Direction * (speed * dt);

            Rect bounds = grid.Bounds;
            if (next.x < bounds.xMin + radius || next.x > bounds.xMax - radius ||
                next.y < bounds.yMin + radius || next.y > bounds.yMax - radius)
            {
                Alive = false;
                Cause = DeathCause.Wall;
                return;
            }

            if (grid.OverlapsDisc(next, radius))
            {
                Alive = false;
                Cause = DeathCause.Trail;
                return;
            }

            Distance += speed * dt;
            pendingLength += Vector2.Distance(Position, next);
            pending.Enqueue(next);
            Position = next;

            StampDueSegments();
            trail.AddPoint(next);
        }

        void StampDueSegments()
        {
            while (pending.Count > 0)
            {
                Vector2 next = pending.Peek();
                float segment = Vector2.Distance(lastStamped, next);
                if (pendingLength - segment < paintDelay) break;

                pending.Dequeue();
                grid.StampCapsule(lastStamped, next, radius);
                lastStamped = next;
                pendingLength -= segment;
            }
        }
    }
}
