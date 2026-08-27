using System.Collections.Generic;
using UnityEngine;

namespace SnakeGiuJu
{
    public enum PowerUpKind
    {
        Fett,
        Duenn,
        Speed
    }

    /// <summary>
    /// Die Spielregeln der Power-ups an einer Stelle. Bewusst Konstanten statt
    /// Inspector-Felder: es sind festgelegte Regeln, keine Feinjustierung.
    /// </summary>
    public static class PowerUpRules
    {
        // Halbiert (vorher 5-10s): das Warten bis zum ersten Power-up wirkte zu lang.
        public const float MinSpawnInterval = 2.5f;
        public const float MaxSpawnInterval = 5f;
        public const float Lifetime = 20f;
        public const float Radius = 0.85f;

        /// <summary>Ab hier blinkt ein Power-up, weil es gleich verschwindet.</summary>
        public const float WarnTime = 4f;

        public const float FettFactor = 2f;
        public const float DuennFactor = 0.5f;
        // Ohne Grenzen liesse sich die Linie durch mehrfaches Einsammeln bis zur
        // Unspielbarkeit aufblasen oder auf Haaresbreite schrumpfen.
        public const float MinWidthFactor = 0.35f;
        public const float MaxWidthFactor = 3f;

        public const float SpeedDuration = 3f;
        public const float SpeedFactor = 1.25f;
        public const float SpeedScoreFactor = 1.5f;

        public static Color ColorOf(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Fett: return new Color(1f, 0.635f, 0.227f, 1f);
                case PowerUpKind.Duenn: return new Color(0.608f, 0.482f, 1f, 1f);
                default: return new Color(1f, 0.882f, 0.302f, 1f);
            }
        }

        public static string LabelOf(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Fett: return "THICK";
                case PowerUpKind.Duenn: return "THIN";
                default: return "SPEED";
            }
        }

        /// <summary>Ringstärke als zweites Unterscheidungsmerkmal neben der Farbe.</summary>
        public static float InnerRadiusOf(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Fett: return 0.10f;
                case PowerUpKind.Duenn: return 0.44f;
                default: return 0.30f;
            }
        }
    }

    /// <summary>
    /// Verwaltet die eingesammelbaren Ringe: Erscheinen, Ablaufen und Aufsammeln.
    /// Die Ringe sind keine Hindernisse und werden deshalb nie ins Kollisionsraster
    /// gestempelt – das Raster dient hier nur dazu, keinen Ring auf einer schon
    /// gezogenen Linie abzulegen.
    /// </summary>
    public sealed class PowerUpField
    {
        const int SpawnAttempts = 40;
        const float HeadClearance = 5f;

        sealed class Item
        {
            public PowerUpKind Kind;
            public Vector2 Position;
            public float ExpiresAt;
            public Transform Transform;
        }

        readonly Transform root;
        readonly Dictionary<PowerUpKind, Material> materials = new Dictionary<PowerUpKind, Material>();
        readonly Dictionary<PowerUpKind, Mesh> meshes = new Dictionary<PowerUpKind, Mesh>();
        readonly List<Item> items = new List<Item>();
        readonly List<PowerUpKind> collected = new List<PowerUpKind>();

        ArenaGrid grid;
        float nextSpawnAt;

        /// <summary>Was in diesem Frame eingesammelt wurde.</summary>
        public IReadOnlyList<PowerUpKind> Collected => collected;

        public PowerUpField(Transform parent, Shader shader)
        {
            root = new GameObject("PowerUps").transform;
            root.SetParent(parent, false);

            foreach (PowerUpKind kind in System.Enum.GetValues(typeof(PowerUpKind)))
            {
                var material = new Material(shader) { name = "PowerUp " + kind };
                material.SetColor("_BaseColor", PowerUpRules.ColorOf(kind));
                materials[kind] = material;
                meshes[kind] = MeshShapes.CreateRing(PowerUpRules.InnerRadiusOf(kind));
            }
        }

        public void Restart(ArenaGrid arenaGrid, float now)
        {
            grid = arenaGrid;
            Clear();
            nextSpawnAt = now + Random.Range(PowerUpRules.MinSpawnInterval, PowerUpRules.MaxSpawnInterval);
        }

        public void Clear()
        {
            foreach (Item item in items) Object.Destroy(item.Transform.gameObject);
            items.Clear();
            collected.Clear();
        }

        public void Tick(float now, Vector2 head, float headRadius)
        {
            collected.Clear();
            if (grid == null) return;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item item = items[i];

                if (Vector2.Distance(item.Position, head) <= PowerUpRules.Radius + headRadius)
                {
                    collected.Add(item.Kind);
                    Remove(i);
                    continue;
                }

                if (now >= item.ExpiresAt)
                {
                    Remove(i);
                    continue;
                }

                Animate(item, now);
            }

            if (now < nextSpawnAt) return;
            nextSpawnAt = now + Random.Range(PowerUpRules.MinSpawnInterval, PowerUpRules.MaxSpawnInterval);
            TrySpawn(now, head);
        }

        void Remove(int index)
        {
            Object.Destroy(items[index].Transform.gameObject);
            items.RemoveAt(index);
        }

        static void Animate(Item item, float now)
        {
            float remaining = item.ExpiresAt - now;
            bool warning = remaining <= PowerUpRules.WarnTime;
            float amplitude = warning ? 0.16f : 0.05f;
            float rate = warning ? 14f : 3f;
            float scale = PowerUpRules.Radius * 2f * (1f + amplitude * Mathf.Sin(now * rate));
            item.Transform.localScale = Vector3.one * scale;
        }

        void TrySpawn(float now, Vector2 head)
        {
            Rect bounds = grid.Bounds;
            float inset = PowerUpRules.Radius + 0.6f;

            for (int attempt = 0; attempt < SpawnAttempts; attempt++)
            {
                var candidate = new Vector2(
                    Random.Range(bounds.xMin + inset, bounds.xMax - inset),
                    Random.Range(bounds.yMin + inset, bounds.yMax - inset));

                // Nicht auf einer schon gezogenen Linie, nicht direkt vor dem Kopf und
                // nicht auf einem anderen Ring - sonst waere er nicht erreichbar oder
                // nicht als einzelner Ring zu erkennen.
                if (grid.OverlapsDisc(candidate, PowerUpRules.Radius + 0.3f)) continue;
                if (Vector2.Distance(candidate, head) < HeadClearance) continue;

                bool tooClose = false;
                foreach (Item other in items)
                {
                    if (Vector2.Distance(candidate, other.Position) < PowerUpRules.Radius * 3f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                Spawn(candidate, now);
                return;
            }
        }

        void Spawn(Vector2 position, float now)
        {
            var kind = (PowerUpKind)Random.Range(0, System.Enum.GetValues(typeof(PowerUpKind)).Length);

            var go = new GameObject("PowerUp " + kind);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(position.x, position.y, 0.05f);
            go.transform.localScale = Vector3.one * (PowerUpRules.Radius * 2f);
            go.AddComponent<MeshFilter>().sharedMesh = meshes[kind];

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[kind];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            items.Add(new Item
            {
                Kind = kind,
                Position = position,
                ExpiresAt = now + PowerUpRules.Lifetime,
                Transform = go.transform
            });
        }
    }
}
