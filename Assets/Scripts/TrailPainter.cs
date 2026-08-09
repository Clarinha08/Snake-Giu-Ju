using System.Collections.Generic;
using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Zeichnet die stehenbleibende Linie eines Spielers. Die Linie wird in Abschnitte
    /// fester Länge zerlegt: nur der aktive Abschnitt wird pro Frame angefasst, alle
    /// älteren bleiben unveränderte Meshes.
    /// </summary>
    public sealed class TrailPainter
    {
        const int PointsPerChunk = 128;

        readonly Transform root;
        readonly Material material;
        readonly float pointSpacing;
        float width;
        readonly List<Vector3> points = new List<Vector3>(PointsPerChunk);
        readonly List<GameObject> chunks = new List<GameObject>();

        LineRenderer active;

        /// <summary>Farbe der Linie. Wirkt ab dem naechsten <see cref="Restart"/>.</summary>
        public Color Color { get; set; }

        public TrailPainter(Transform parent, Material material, Color color, float width, float pointSpacing)
        {
            root = new GameObject("Trail").transform;
            root.SetParent(parent, false);
            this.material = material;
            this.width = width;
            this.pointSpacing = pointSpacing;
            Color = color;
        }

        public void Restart(Vector2 start)
        {
            foreach (GameObject chunk in chunks) Object.Destroy(chunk);
            chunks.Clear();
            StartChunk(start);
        }

        /// <summary>Hängt einen Punkt an, sobald er weit genug vom letzten entfernt ist.</summary>
        public void AddPoint(Vector2 position)
        {
            Vector3 p = position;
            if ((p - points[points.Count - 1]).sqrMagnitude < pointSpacing * pointSpacing) return;

            points.Add(p);
            active.positionCount = points.Count + 1;
            active.SetPosition(points.Count - 1, p);
            active.SetPosition(points.Count, p);

            if (points.Count >= PointsPerChunk)
            {
                // Abschnitt einfrieren und den nächsten exakt am letzten Punkt fortsetzen.
                active.positionCount = points.Count;
                StartChunk(p);
            }
        }

        /// <summary>Zieht die Spitze der Linie auf die aktuelle Kopfposition.</summary>
        public void SetHead(Vector2 position)
        {
            active.SetPosition(points.Count, position);
        }

        /// <summary>
        /// Ändert die Strichbreite ab hier. Der laufende Abschnitt wird dafür
        /// eingefroren – ein LineRenderer hat nur eine Breite für seine ganze Linie,
        /// die schon gezogene Strecke soll ihre aber behalten.
        /// </summary>
        public void SetWidth(float value)
        {
            if (Mathf.Approximately(value, width)) return;

            width = value;
            // Vor der ersten Runde gibt es noch keinen Abschnitt, der eingefroren werden müsste.
            if (active == null) return;

            active.positionCount = points.Count;
            StartChunk(points[points.Count - 1]);
        }

        void StartChunk(Vector2 start)
        {
            var go = new GameObject("Chunk " + chunks.Count);
            go.transform.SetParent(root, false);
            chunks.Add(go);

            active = go.AddComponent<LineRenderer>();
            active.sharedMaterial = material;
            active.useWorldSpace = true;
            active.widthMultiplier = width;
            active.numCapVertices = 6;
            active.numCornerVertices = 4;
            active.alignment = LineAlignment.View;
            active.textureMode = LineTextureMode.Stretch;
            active.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            active.receiveShadows = false;
            active.startColor = Color;
            active.endColor = Color;

            points.Clear();
            points.Add(start);
            active.positionCount = 2;
            active.SetPosition(0, start);
            active.SetPosition(1, start);
        }
    }
}
