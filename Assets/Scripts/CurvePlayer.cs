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
        readonly float baseRadius;
        readonly float speed;
        readonly float turnRateDeg;

        // Das jüngste Stück hinter dem Kopf wird bewusst noch nicht ins Raster
        // gestempelt, sonst würde der Kopf sofort mit seinem eigenen Körper kollidieren.
        readonly Queue<Vector2> pending = new Queue<Vector2>();
        float pendingLength;
        Vector2 lastStamped;

        public Vector2 Position { get; private set; }
        public float HeadingDeg { get; private set; }
        public float Distance { get; private set; }
        public float Score { get; private set; }
        public bool Alive { get; private set; }
        public DeathCause Cause { get; private set; }

        /// <summary>Halbe Linienbreite. Power-ups verändern sie während der Runde.</summary>
        public float Radius { get; set; }

        /// <summary>Tempofaktor. Die Winkelgeschwindigkeit zieht mit, der Mindestradius bleibt dadurch gleich.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        public float ScoreMultiplier { get; set; } = 1f;

        // Das jüngste Stück hinter dem Kopf bleibt ungestempelt. Der Abstand wächst
        // mit der Dicke mit, sonst würde eine fett gewordene Linie den eigenen Kopf treffen.
        float PaintDelay => Radius * 4f;

        public Vector2 Direction => new Vector2(
            Mathf.Cos(HeadingDeg * Mathf.Deg2Rad),
            Mathf.Sin(HeadingDeg * Mathf.Deg2Rad));

        public CurvePlayer(ArenaGrid grid, TrailPainter trail, float radius, float speed, float minTurnRadius)
        {
            this.grid = grid;
            this.trail = trail;
            baseRadius = radius;
            this.speed = speed;
            Radius = radius;
            turnRateDeg = speed / Mathf.Max(minTurnRadius, 0.01f) * Mathf.Rad2Deg;
        }

        public void Spawn(Vector2 position, float headingDeg)
        {
            Position = position;
            HeadingDeg = headingDeg;
            Distance = 0f;
            Score = 0f;
            Radius = baseRadius;
            SpeedMultiplier = 1f;
            ScoreMultiplier = 1f;
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

            // Tempo und Winkelgeschwindigkeit werden gemeinsam skaliert, damit der
            // Mindestkurvenradius auch im Speed-Rausch derselbe bleibt.
            float step = speed * SpeedMultiplier * dt;
            HeadingDeg -= steering * turnRateDeg * SpeedMultiplier * dt;
            Vector2 next = Position + Direction * step;

            Rect bounds = grid.Bounds;
            if (next.x < bounds.xMin + Radius || next.x > bounds.xMax - Radius ||
                next.y < bounds.yMin + Radius || next.y > bounds.yMax - Radius)
            {
                Alive = false;
                Cause = DeathCause.Wall;
                return;
            }

            if (grid.OverlapsDisc(next, Radius))
            {
                Alive = false;
                Cause = DeathCause.Trail;
                return;
            }

            Distance += step;
            Score += step * ScoreMultiplier;
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
                if (pendingLength - segment < PaintDelay) break;

                pending.Dequeue();
                grid.StampCapsule(lastStamped, next, Radius);
                lastStamped = next;
                pendingLength -= segment;
            }
        }
    }
}
