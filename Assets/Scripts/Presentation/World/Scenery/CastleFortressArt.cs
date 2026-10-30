using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Fortaleza de alvenaria: material proprio, terracos de pedra, ameias e portal em arco.
    public static class CastleFortressArt
    {
        private static readonly Color Stone = ObliqueMeshData.Hex("A2A8AF");
        private static readonly Color Dark = ObliqueMeshData.Hex("242A31");
        private static readonly Color Gold = ObliqueMeshData.Hex("BAA273");
        public const float TorchOffsetX = 1.9f, TorchFlameHeight = 2.19f;

        public static Vector3 TorchFlamePosition(float x, Vector2 size)
            => ObliqueMeshData.V(x, -size.y / 2 - 0.475f, TorchFlameHeight);

        public static PeripheryBuildingArt Building(string name, Vector2 size)
        {
            var art = new PeripheryBuildingArt(); var mesh = art.Body;
            bool tower = name.StartsWith("Torre");
            float w = size.x, d = size.y, wall = tower ? 7.1f : 5.8f, front = -d / 2 - 0.025f;
            mesh.Shadow(0.4f, -0.2f, w * 0.6f, d * 0.6f);
            mesh.DefaultSurface = PixelSurface.Sand;
            mesh.Box(0, 0, w, d, 0, 0.65f, Stone);
            mesh.DefaultSurface = PixelSurface.Wall;
            mesh.Box(0, 0, w, d, 0.65f, wall, Stone);
            // Contrafortes e cintas reforcam a massa das paredes.
            foreach (float x in new[] {-w * 0.42f, w * 0.42f})
                mesh.Box(x, -d / 2, w * 0.1f, d * 0.1f, 0, wall - 0.25f, Stone);
            mesh.DefaultSurface = PixelSurface.Basalt;
            mesh.Box(0, 0, w + 0.08f, d + 0.08f, wall - 0.18f, wall + 0.12f, Stone);
            Battlements(mesh, 0, 0, w, d, wall + 0.12f, tower ? 0.8f : 0.62f);
            if (tower)
            {
                foreach (float h in new[] {2.3f, 4.5f, 6.1f}) Slit(mesh, 0, front - 0.006f, h, 0.19f, 0.85f);
                Slit(mesh, w / 2 + 0.012f, 0, 4.2f, 0.17f, 0.95f, true);
                Banner(mesh, w * 0.31f, front - 0.04f, 5.45f, 0.58f, 1.65f);
            }
            else
            {
                Gate(mesh, front - 0.03f);
                foreach (float x in new[] {-w * 0.32f, w * 0.32f})
                {
                    Banner(mesh, x, front - 0.035f, 4.85f, 0.76f, 1.9f);
                    Slit(mesh, x, front - 0.004f, 2.25f, 0.2f, 1);
                }
                // Torre de menagem recuada acima do portao, com a mesma alvenaria.
                mesh.DefaultSurface = PixelSurface.Wall;
                mesh.Box(0, d * 0.16f, w * 0.32f, d * 0.58f, wall + 0.12f, 7.25f, Stone);
                Battlements(mesh, 0, d * 0.16f, w * 0.32f, d * 0.58f, 7.25f, 0.62f);
                Slit(mesh, 0, d * 0.16f - d * 0.29f - 0.01f, 6.6f, 0.25f, 0.7f);
                foreach (float x in new[] {-TorchOffsetX, TorchOffsetX}) Torch(mesh, x, size);
            }
            return art;
        }

        private static void Battlements(ObliqueMeshData mesh, float x, float y, float width, float depth, float bottom, float height)
        {
            // A single terrace shell: floor and parapets have no overlapping faces.
            // Start at bottom so this shell also never overlaps the keep wall below it.
            float edge = Math.Min(0.27f, Math.Min(width, depth) * 0.22f);
            float l = x - width / 2, r = x + width / 2, s = y - depth / 2, n = y + depth / 2;
            float floor = bottom + 0.08f, rim = bottom + 0.22f;
            mesh.DefaultSurface = PixelSurface.Paving;
            mesh.Plane(x, y, width - edge * 2, depth - edge * 2, floor, Stone);
            mesh.DefaultSurface = PixelSurface.Basalt;
            // Outer walls and inner faces above the recessed walking surface.
            mesh.Quad(ObliqueMeshData.V(l, s, bottom), ObliqueMeshData.V(l, s, rim), ObliqueMeshData.V(r, s, rim), ObliqueMeshData.V(r, s, bottom), Stone);
            mesh.Quad(ObliqueMeshData.V(r, n, bottom), ObliqueMeshData.V(r, n, rim), ObliqueMeshData.V(l, n, rim), ObliqueMeshData.V(l, n, bottom), Stone);
            mesh.Quad(ObliqueMeshData.V(l, n, bottom), ObliqueMeshData.V(l, n, rim), ObliqueMeshData.V(l, s, rim), ObliqueMeshData.V(l, s, bottom), Stone);
            mesh.Quad(ObliqueMeshData.V(r, s, bottom), ObliqueMeshData.V(r, s, rim), ObliqueMeshData.V(r, n, rim), ObliqueMeshData.V(r, n, bottom), Stone);
            mesh.Quad(ObliqueMeshData.V(r-edge, s+edge, floor), ObliqueMeshData.V(r-edge, s+edge, rim), ObliqueMeshData.V(l+edge, s+edge, rim), ObliqueMeshData.V(l+edge, s+edge, floor), Stone);
            mesh.Quad(ObliqueMeshData.V(l+edge, n-edge, floor), ObliqueMeshData.V(l+edge, n-edge, rim), ObliqueMeshData.V(r-edge, n-edge, rim), ObliqueMeshData.V(r-edge, n-edge, floor), Stone);
            mesh.Quad(ObliqueMeshData.V(l+edge, s+edge, floor), ObliqueMeshData.V(l+edge, s+edge, rim), ObliqueMeshData.V(l+edge, n-edge, rim), ObliqueMeshData.V(l+edge, n-edge, floor), Stone);
            mesh.Quad(ObliqueMeshData.V(r-edge, n-edge, floor), ObliqueMeshData.V(r-edge, n-edge, rim), ObliqueMeshData.V(r-edge, s+edge, rim), ObliqueMeshData.V(r-edge, s+edge, floor), Stone);
            foreach (float side in new[] {-1f, 1f})
            {
                mesh.Plane(x, y + side * (depth - edge) / 2, width, edge, rim, Stone);
                mesh.Plane(x + side * (width - edge) / 2, y, edge, depth - edge * 2, rim, Stone);
            }
            int countX = Math.Max(3, (int)Math.Ceiling(width / 0.8f)), countY = Math.Max(3, (int)Math.Ceiling(depth / 0.8f));
            float tileX = Math.Min(0.42f, width / countX * 0.65f), tileY = Math.Min(0.42f, depth / countY * 0.65f);
            for (int i = 0; i < countX; i++)
                foreach (float side in new[] {-1f, 1f})
                    mesh.Box(l + tileX / 2 + (width - tileX) * i / (countX - 1), y + side * (depth - edge) / 2,
                        tileX, edge, rim, bottom + height, Stone);
            for (int i = 1; i < countY - 1; i++)
                foreach (float side in new[] {-1f, 1f})
                    mesh.Box(x + side * (width - edge) / 2, s + tileY / 2 + (depth - tileY) * i / (countY - 1),
                        edge, tileY, rim, bottom + height, Stone);
        }

        private static void Torch(ObliqueMeshData mesh, float x, Vector2 size)
        {
            float front = -size.y / 2 - 0.025f, y = TorchFlamePosition(x, size).y;
            mesh.DefaultSurface = PixelSurface.None;
            // Wall plate, projecting bracket, wooden shaft and a flared iron basket.
            mesh.Box(x, front, 0.24f, 0.1f, 1.34f, 1.94f, Dark);
            mesh.Box(x, front - 0.23f, 0.1f, 0.44f, 1.65f, 1.75f, Dark);
            mesh.DefaultSurface = PixelSurface.Wood;
            mesh.Cone(x, y, 0.065f, 1.53f, 2.02f, Stone, 8, 0.065f);
            mesh.DefaultSurface = PixelSurface.None;
            mesh.Cone(x, y, 0.09f, 1.6f, 1.69f, Gold, 8, 0.09f);
            mesh.Cone(x, y, 0.11f, 1.96f, 2.15f, Dark, 8, 0.24f);
            mesh.Cone(x, y, 0.255f, 2.13f, 2.18f, Gold, 8, 0.255f);
            mesh.Cone(x, y, 0.2f, 2.18f, 2.185f, Dark, 8, 0.2f);
        }

        private static void Gate(ObliqueMeshData mesh, float y)
        {
            const float radius = 1.27f, center = 2.15f, outer = 1.62f;
            mesh.DefaultSurface = PixelSurface.Wood;
            mesh.Quad(ObliqueMeshData.V(-radius, y, 0), ObliqueMeshData.V(-radius, y, center),
                ObliqueMeshData.V(radius, y, center), ObliqueMeshData.V(radius, y, 0), Stone, false);
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI / 12, b = (i + 1) * Math.PI / 12;
                mesh.Triangle(ObliqueMeshData.V(0, y, center), ObliqueMeshData.V((float)Math.Cos(a) * radius, y, center + (float)Math.Sin(a) * radius),
                    ObliqueMeshData.V((float)Math.Cos(b) * radius, y, center + (float)Math.Sin(b) * radius), Stone, false);
                mesh.DefaultSurface = PixelSurface.Basalt;
                mesh.Quad(ObliqueMeshData.V((float)Math.Cos(a) * radius, y - 0.02f, center + (float)Math.Sin(a) * radius),
                    ObliqueMeshData.V((float)Math.Cos(a) * outer, y - 0.02f, center + (float)Math.Sin(a) * outer),
                    ObliqueMeshData.V((float)Math.Cos(b) * outer, y - 0.02f, center + (float)Math.Sin(b) * outer),
                    ObliqueMeshData.V((float)Math.Cos(b) * radius, y - 0.02f, center + (float)Math.Sin(b) * radius), Stone, false);
                mesh.DefaultSurface = PixelSurface.Wood;
            }
            mesh.DefaultSurface = PixelSurface.Basalt;
            foreach (float x in new[] {-(radius + outer) / 2, (radius + outer) / 2}) mesh.Box(x, y + 0.07f, outer - radius, 0.2f, 0, center, Stone);
            mesh.DefaultSurface = PixelSurface.None;
            foreach (float h in new[] {0.85f, 1.75f})
                mesh.Quad(ObliqueMeshData.V(-radius, y - 0.035f, h), ObliqueMeshData.V(-radius, y - 0.035f, h + 0.065f),
                    ObliqueMeshData.V(radius, y - 0.035f, h + 0.065f), ObliqueMeshData.V(radius, y - 0.035f, h), Dark, false);
            for (int i = -4; i <= 4; i++)
            {
                float x = i * 0.25f, top = center + (float)Math.Sqrt(radius * radius - x * x);
                mesh.Quad(ObliqueMeshData.V(x - 0.02f, y - 0.04f, 0.05f), ObliqueMeshData.V(x - 0.02f, y - 0.04f, top - 0.02f),
                    ObliqueMeshData.V(x + 0.02f, y - 0.04f, top - 0.02f), ObliqueMeshData.V(x + 0.02f, y - 0.04f, 0.05f), Dark, false);
            }
        }

        private static void Slit(ObliqueMeshData mesh, float x, float y, float height, float width, float length, bool side = false)
        {
            mesh.DefaultSurface = PixelSurface.None;
            if (side)
                mesh.Quad(ObliqueMeshData.V(x, y - width / 2, height - length / 2), ObliqueMeshData.V(x, y - width / 2, height + length / 2),
                    ObliqueMeshData.V(x, y + width / 2, height + length / 2), ObliqueMeshData.V(x, y + width / 2, height - length / 2), Dark, false);
            else
                mesh.Quad(ObliqueMeshData.V(x - width / 2, y, height - length / 2), ObliqueMeshData.V(x - width / 2, y, height + length / 2),
                    ObliqueMeshData.V(x + width / 2, y, height + length / 2), ObliqueMeshData.V(x + width / 2, y, height - length / 2), Dark, false);
        }

        private static void Banner(ObliqueMeshData mesh, float x, float y, float top, float width, float length)
        {
            mesh.DefaultSurface = PixelSurface.Fabric;
            mesh.Quad(ObliqueMeshData.V(x - width / 2, y, top - length + 0.2f), ObliqueMeshData.V(x - width / 2, y, top),
                ObliqueMeshData.V(x + width / 2, y, top), ObliqueMeshData.V(x + width / 2, y, top - length + 0.2f), Stone, false);
            mesh.Triangle(ObliqueMeshData.V(x - width / 2, y, top - length + 0.2f), ObliqueMeshData.V(x + width / 2, y, top - length + 0.2f),
                ObliqueMeshData.V(x, y, top - length), Stone, false);
            mesh.DefaultSurface = PixelSurface.None;
            mesh.Box(x, y + 0.01f, width + 0.12f, 0.06f, top, top + 0.06f, Gold);
        }

        public static ObliqueMeshData Icon()
        {
            var mesh = new ObliqueMeshData {DefaultSurface = PixelSurface.Wall};
            mesh.Shadow(0.15f, -0.1f, 1, 0.6f);
            mesh.Box(0, 0.1f, 1.2f, 0.8f, 0, 1.05f, Stone);
            Battlements(mesh, 0, 0.1f, 1.2f, 0.8f, 1.05f, 0.25f);
            foreach (float x in new[] {-0.65f, 0.65f})
            {
                mesh.DefaultSurface = PixelSurface.Wall;
                mesh.Box(x, 0.4f, 0.45f, 0.5f, 0, 1.65f, Stone);
                Battlements(mesh, x, 0.4f, 0.45f, 0.5f, 1.65f, 0.3f);
            }
            Slit(mesh, 0, -0.31f, 0.3f, 0.28f, 0.6f);
            return mesh;
        }
    }
}
