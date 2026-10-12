using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class PeripheryBuildingArt
    {
        public readonly ObliqueMeshData Body = new ObliqueMeshData();
        public readonly ObliqueMeshData Details = new ObliqueMeshData();
        public readonly ObliqueMeshData Glow = new ObliqueMeshData();
    }

    // Arte original da periferia: volumes 3D, materiais pixel art e placas presas as fachadas.
    public static class CityPeripheryArt
    {
        private static readonly Color Iron = ObliqueMeshData.Hex("534A38");
        private static readonly Color Copper = ObliqueMeshData.Hex("946540");

        public static bool IsRoyalCastle(string name) => name == "Castelo do tirano" || name.EndsWith("do castelo", StringComparison.Ordinal);

        public static ObliqueMeshData Block(CityBlock block)
        {
            if (CityGroundArt.ReplacesBlock(block.Name)) return new ObliqueMeshData();
            if (block.Name.StartsWith("Fosso seco"))
            {
                var pit = new ObliqueMeshData();
                pit.DefaultSurface = PixelSurface.Wall;
                foreach (float y in new[] {-block.Size.y / 2, block.Size.y / 2})
                    pit.Box(0, y, block.Size.x, 0.25f, -0.7f, 0.12f, Iron);
                return pit;
            }
            if (block.Name.StartsWith("Ponte de pedra"))
            {
                var bridge = new ObliqueMeshData {DefaultSurface = PixelSurface.Paving};
                bool deck = block.Name.EndsWith("piso");
                bridge.Box(0, 0, block.Size.x, block.Size.y, -0.48f, deck ? 0.055f : 0.72f, Iron);
                if (!deck)
                {
                    bridge.DefaultSurface = PixelSurface.Wall;
                    bridge.Box(0, 0, block.Size.x + 0.08f, block.Size.y, 0.72f, 0.83f, Iron);
                    for (float y = -block.Size.y / 2 + 0.35f; y < block.Size.y / 2; y += 0.8f)
                        bridge.Box(0, y, block.Size.x + 0.12f, 0.3f, 0.83f, 0.98f, Iron);
                }
                return bridge;
            }
            var mesh = ObliqueScenery.BlockData(block.Name, block.Size, block.Color, block.Solid, block.Order);
            if (block.Name.StartsWith("Rua") || block.Name.StartsWith("Patio") && !block.Name.Contains("castelo"))
                for (int i = 0; i < mesh.Surfaces.Count; i++) mesh.Surfaces[i] = new Vector2((int)PixelSurface.Dirt, mesh.Surfaces[i].y);
            return mesh;
        }

        public static PeripheryBuildingArt Building(string name, Vector2 size, string hex)
        {
            if (IsRoyalCastle(name)) return CastleFortressArt.Building(name, size);
            var art = new PeripheryBuildingArt();
            float w = size.x, d = size.y, front = -d / 2 - 0.025f;
            bool fortified = name.Contains("Castelo") || name.Contains("Torre");
            if (fortified)
            {
                art.Body.Append(ObliqueScenery.BuildingData(size, hex, true), Vector3.zero);
                Panel(art.Details, 0, front - 0.01f, 2.35f, 0.8f, 0.85f, 10);
                Panel(art.Details, 0, front - 0.012f, 0.96f, name.Contains("Castelo") ? 1.7f : 0.85f, 1.85f, 6);
                foreach (float x in new[] {-w * 0.3f, w * 0.3f}) Panel(art.Details, x, front - 0.012f, 1.3f, 0.6f, 0.75f, 7);
                return art;
            }
            int variant = 0; foreach (char c in name) variant += c;
            bool items = name == "Loja de itens", materia = name == "Loja de materias";
            bool equipment = name == "Loja de equipamentos", inn = name == "Hospedaria";
            bool shop = items || materia || equipment || inn;
            float wallHeight = shop ? 2.6f : 1.9f + variant % 3 * 0.26f;
            var body = art.Body;
            body.Shadow(0.3f, -0.2f, w * 0.65f, d * 0.6f);
            body.DefaultSurface = PixelSurface.Wall;
            body.Box(0, 0, w, d, 0, 0.2f, Iron);
            if (items)
            {
                body.DefaultSurface = PixelSurface.Basalt;
                body.Cone(0, 0, Math.Min(w, d) * 0.49f, 0.2f, wallHeight, Iron, 12, Math.Min(w, d) * 0.49f);
                // A porta se apoia numa pequena fachada plana, dentro da mesma planta fisica.
                body.Box(0, -d * 0.3f, w * 0.45f, d * 0.4f, 0.2f, 2.15f, Iron);
                body.Cone(0, 0, Math.Min(w, d) * 0.52f, wallHeight, wallHeight + 0.95f, Iron, 12, 0.5f);
                body.Cone(0, 0, 0.48f, wallHeight + 0.92f, wallHeight + 1.65f, Copper, 8, 0.3f);
            }
            else
            {
                body.DefaultSurface = variant % 3 == 0 ? PixelSurface.Wood : PixelSurface.Wall;
                body.Box(0, 0, w, d, 0.2f, wallHeight, Iron);
                body.DefaultSurface = inn || variant % 2 == 0 ? PixelSurface.Fabric : PixelSurface.Roof;
                // Telhados assimetricos, lonas remendadas e beirais pouco salientes.
                body.Roof(-w * 0.045f, 0, w + 0.18f, d + 0.18f, wallHeight, wallHeight + 0.75f, ObliqueMeshData.Hex(hex));
                body.DefaultSurface = PixelSurface.Basalt;
                body.Box(w * 0.18f, d * 0.22f, w * 0.28f, d * 0.35f, wallHeight + 0.32f, wallHeight + 0.62f, Iron);
            }
            body.DefaultSurface = PixelSurface.Wood;
            foreach (float x in new[] {-w * 0.44f, w * 0.44f}) body.Box(x, front + 0.08f, 0.13f, 0.17f, 0.2f, wallHeight + 0.15f, Iron);
            body.DefaultSurface = PixelSurface.Basalt;
            body.Box(0, front + 0.08f, w * 0.96f, 0.18f, wallHeight - 0.2f, wallHeight - 0.06f, Iron);
            Panel(art.Details, 0, front - 0.01f, 0.8f, 0.82f, 1.52f, shop ? 5 : 6);
            foreach (float x in new[] {-w * 0.3f, w * 0.3f})
            {
                Panel(art.Details, x, front - 0.013f, 1.3f, Math.Min(0.86f, w * 0.2f), 0.95f, materia ? 8 : 7);
                body.Box(x, front - 0.04f, 0.9f, 0.15f, 0.76f, 0.84f, Copper);
            }
            Panel(art.Details, w / 2 + 0.01f, d * 0.1f, 1.4f, d * 0.36f, 1, 9, true);
            if (shop)
            {
                int sign = items ? 1 : materia ? 2 : equipment ? 3 : 4;
                float signWidth = items ? 2.25f : 3.45f;
                // A placa fica na borda do toldo, visivel com a camera inclinada.
                Panel(art.Details, 0, front - 0.72f, 2.2f, signWidth, 0.78f, sign);
                body.DefaultSurface = PixelSurface.Fabric;
                body.Quad(ObliqueMeshData.V(-w * 0.43f, front + 0.02f, 2.55f), ObliqueMeshData.V(w * 0.43f, front + 0.02f, 2.55f),
                    ObliqueMeshData.V(w * 0.43f, front - 0.65f, 2.25f), ObliqueMeshData.V(-w * 0.43f, front - 0.65f, 2.25f), Iron);
            }
            else if (variant % 2 == 0)
                Panel(art.Details, -w * 0.28f, front - 0.02f, 0.62f, 0.45f, 0.65f, 10);
            body.DefaultSurface = PixelSurface.Basalt;
            float chimneyX = -w * 0.28f, chimneyY = d * 0.21f, chimneyBase = wallHeight + 0.5f;
            body.Cone(chimneyX, chimneyY, 0.15f, chimneyBase, chimneyBase + 1.2f, Iron, 8, 0.15f);
            body.Cone(chimneyX, chimneyY, 0.22f, chimneyBase + 1.1f, chimneyBase + 1.26f, Iron, 8, 0.22f);
            Pipe(body, new Vector3(w * 0.39f, front, 0.45f), new Vector3(w * 0.39f, front, wallHeight + 0.45f), 0.09f);
            Pipe(body, new Vector3(w * 0.39f, front, wallHeight + 0.45f), new Vector3(w * 0.39f, d * 0.28f, wallHeight + 0.45f), 0.09f);
            body.DefaultSurface = PixelSurface.Wood;
            body.Box(-w * 0.33f, -d * 0.37f, 0.48f, 0.45f, 0, 0.5f, Iron);
            body.DefaultSurface = PixelSurface.Basalt;
            body.Cone(w * 0.35f, -d * 0.32f, 0.22f, 0, 0.62f, Copper, 8, 0.22f);
            // A luz das janelas e dos letreiros ja esta pintada no atlas de fachadas.
            Steam(art.Glow, chimneyX, chimneyY, chimneyBase + 1.3f, variant);
            return art;
        }

        // Coloca uma celula inteira do atlas no painel; UVs locais evitam texto repetido/cortado.
        public static void Panel(ObliqueMeshData mesh, float x, float y, float height, float width, float depth, int cell, bool side = false)
        {
            int start = mesh.UVs.Count;
            Vector3 a, b, c, d;
            if (side)
            {
                a = ObliqueMeshData.V(x, y - width / 2, height - depth / 2); b = ObliqueMeshData.V(x, y - width / 2, height + depth / 2);
                c = ObliqueMeshData.V(x, y + width / 2, height + depth / 2); d = ObliqueMeshData.V(x, y + width / 2, height - depth / 2);
            }
            else
            {
                a = ObliqueMeshData.V(x - width / 2, y, height - depth / 2); b = ObliqueMeshData.V(x - width / 2, y, height + depth / 2);
                c = ObliqueMeshData.V(x + width / 2, y, height + depth / 2); d = ObliqueMeshData.V(x + width / 2, y, height - depth / 2);
            }
            mesh.Quad(a, b, c, d, Color.white, false, (PixelSurface)cell);
            Vector2[] uv = {new Vector2(0.01f, 0.01f), new Vector2(0.01f, 0.99f), new Vector2(0.99f, 0.99f),
                new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f), new Vector2(0.99f, 0.01f)};
            for (int i = 0; i < uv.Length; i++) mesh.UVs[start + i] = uv[i];
        }

        // Tubo original em octogonos, com juntas; coordenadas de entrada usam altura positiva.
        private static void Pipe(ObliqueMeshData mesh, Vector3 from, Vector3 to, float radius)
        {
            Vector3 delta = to - from;
            double length = Math.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            Vector3 axis = new Vector3((float)(delta.x / length), (float)(delta.y / length), (float)(delta.z / length));
            Vector3 u = Math.Abs(axis.z) > 0.8f ? new Vector3(1, 0, 0) : new Vector3(-axis.y, axis.x, 0);
            float magnitude = (float)Math.Sqrt(u.x * u.x + u.y * u.y + u.z * u.z); u /= magnitude;
            Vector3 v = new Vector3(axis.y * u.z - axis.z * u.y, axis.z * u.x - axis.x * u.z, axis.x * u.y - axis.y * u.x);
            mesh.DefaultSurface = PixelSurface.Basalt;
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4, b = (i + 1) * Math.PI / 4;
                Vector3 r = u * (float)(Math.Cos(a) * radius) + v * (float)(Math.Sin(a) * radius);
                Vector3 s = u * (float)(Math.Cos(b) * radius) + v * (float)(Math.Sin(b) * radius);
                Vector3 p = from + r, q = from + s, t = to + s, z = to + r;
                mesh.Quad(ObliqueMeshData.V(p.x, p.y, p.z), ObliqueMeshData.V(q.x, q.y, q.z),
                    ObliqueMeshData.V(t.x, t.y, t.z), ObliqueMeshData.V(z.x, z.y, z.z), Copper);
            }
        }

        private static void Steam(ObliqueMeshData mesh, float x, float y, float height, int variation)
        {
            for (int i = 0; i < 4; i++)
            {
                float drift = i * 0.09f * (variation % 2 == 0 ? 1 : -1);
                mesh.Cone(x + drift, y, 0.09f + i * 0.035f, height + i * 0.2f, height + i * 0.2f + 0.14f,
                    new Color(0.7f, 0.74f, 0.6f, 0.08f), 5, 0.1f + i * 0.035f);
            }
        }

        public static PeripheryBuildingArt Streets()
        {
            var art = new PeripheryBuildingArt();
            // Viela, patios, musgo e grelhas agora fazem parte da pintura unica do chao.
            // Tubulacoes altas cruzam a rua como na referencia, deixando o chao livre.
            foreach (float y in new[] {-5.5f, 3.2f})
            {
                Pipe(art.Body, new Vector3(-5.2f, y, 3.1f), new Vector3(-1.2f, y, 3.9f), 0.12f);
                Pipe(art.Body, new Vector3(-1.2f, y, 3.9f), new Vector3(1.6f, y, 3.9f), 0.12f);
                Pipe(art.Body, new Vector3(1.6f, y, 3.9f), new Vector3(5.2f, y, 3.1f), 0.12f);
            }
            return art;
        }
    }
}
