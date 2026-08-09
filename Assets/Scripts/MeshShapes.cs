using UnityEngine;

namespace SnakeGiuJu
{
    public static class MeshShapes
    {
        /// <summary>Kreisscheibe in der XY-Ebene mit Radius 0.5, damit die Skalierung dem Durchmesser entspricht.</summary>
        public static Mesh CreateDisc(int segments = 24)
        {
            var vertices = new Vector3[segments + 1];
            var colors = new Color[segments + 1];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            colors[0] = Color.white;

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                colors[i + 1] = Color.white;

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }

            var mesh = new Mesh { name = "Disc" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Ring in der XY-Ebene mit Aussenradius 0.5, damit die Skalierung dem
        /// Durchmesser entspricht. <paramref name="innerRadius"/> steuert die Ringstärke.
        /// </summary>
        public static Mesh CreateRing(float innerRadius, int segments = 40)
        {
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * 0.5f, sin * 0.5f, 0f);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;

                int next = (i + 1) % segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2 + 1;
                triangles[t + 3] = i * 2;
                triangles[t + 4] = next * 2 + 1;
                triangles[t + 5] = next * 2;
            }

            var mesh = new Mesh { name = "Ring" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
