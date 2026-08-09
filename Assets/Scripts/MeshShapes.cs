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
    }
}
