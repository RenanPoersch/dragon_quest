using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Exploration.Layouts
{
    public static class WorldLayout
    {
        public static readonly Rect Bounds = new Rect(-20, -14, 40, 28);
        public static readonly Vector2 Spawn = new Vector2(0, -7);
        public static readonly Vector2 Castle = Vector2.zero;
        // O ID west-forest permanece para conservar as jornadas existentes.
        public static readonly Vector2 WestForest = new Vector2(-8, 5);
        public static readonly Vector2 EastForest = new Vector2(8, -2.5f);
        public static readonly Vector2 Ruins = new Vector2(-8, -6);
        public static readonly Vector2 Volcano = -Ruins;
        public static readonly Vector2 Trail = Volcano + Vector2.down * 3.5f;
        public static readonly Vector2 RuinsReturn = Ruins + new Vector2(1.5f, -1.9f);
        public static readonly Rect GroundPaintingBounds = new Rect(-20, -14, 40, 28);
        public const float WardRadius = 2.45f;
        public static readonly Rect ForestBounds = new Rect(-9, -7, 18, 14);
        public static readonly Rect CastleBounds = new Rect(-10, -8, 20, 16);
        public static readonly Vector2 LocalExit = new Vector2(0, -5.8f);
        public static readonly Vector2 ForestSpawn = new Vector2(0, -3.5f);
        public static readonly Vector2 CastleSpawn = new Vector2(0, -5);
        public static readonly Vector2 CastleExit = new Vector2(0, -7);
        public static readonly Vector2 Throne = new Vector2(0, 5);
        public static readonly Vector2 CityEntry = CityLayout.Entry;
        public static readonly Vector2 CityExit = CityLayout.Exit;
        public static readonly Vector2 CastleDoor = CityLayout.CastleDoor;

        public static IReadOnlyList<Vector2> Coast { get; } = PeninsulaCoast();

        // Peninsulas e enseadas assimetricas; renderer e mar fisico compartilham o contorno.
        private static IReadOnlyList<Vector2> PeninsulaCoast()
        {
            var points = new List<Vector2>();
            for (int i = 0; i < 96; i++)
            {
                double angle = i * System.Math.PI * 2 / 96;
                double variation = 1 + 0.12 * System.Math.Sin(angle * 3 + 0.6) + 0.055 * System.Math.Cos(angle * 5 - 0.4) + 0.026 * System.Math.Sin(angle * 9);
                points.Add(new Vector2((float)((14.5 * System.Math.Cos(angle) + 0.7 * System.Math.Sin(angle)) * variation),
                    (float)((9.8 * System.Math.Sin(angle) - 0.5 * System.Math.Cos(angle)) * variation)));
            }
            return points.AsReadOnly();
        }

        public static bool IsLand(Vector2 position)
        {
            bool inside = false;
            for (int i = 0, previous = Coast.Count - 1; i < Coast.Count; previous = i++)
            {
                Vector2 a = Coast[i], b = Coast[previous];
                if ((a.y > position.y) != (b.y > position.y)
                    && position.x < (b.x - a.x) * (position.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public static bool WardOverlaps(Rect body)
        {
            float x = System.Math.Max(body.xMin, System.Math.Min(Volcano.x, body.xMax));
            float y = System.Math.Max(body.yMin, System.Math.Min(Volcano.y, body.yMax));
            return (x - Volcano.x) * (x - Volcano.x) + (y - Volcano.y) * (y - Volcano.y) <= WardRadius * WardRadius;
        }

        public static Vector2 SafeSavedPosition(Vector2 position, bool passageOpen)
        {
            var body = new Rect(position.x - 0.355f, position.y - 0.38f, 0.71f, 0.76f);
            foreach (Vector2 corner in new[] { body.min, body.max, new Vector2(body.xMin, body.yMax), new Vector2(body.xMax, body.yMin) })
                if (!IsLand(corner)) return Spawn;
            if (!passageOpen && WardOverlaps(body)) return Trail;
            foreach (var pair in new[] { (Castle, CastleIcon), (WestForest, QuarryIcon), (Ruins, RuinsLayout.Icon) })
                foreach (var block in pair.Item2)
                    if (block.Solid && body.Overlaps(new Rect(pair.Item1 + block.Position - block.Size / 2, block.Size))) return Spawn;
            return position;
        }

        public static IReadOnlyList<CityBlock> Blocks { get; } = CreateBlocks();
        public static IReadOnlyList<CityBlock> CastleBlocks { get; } = CreateCastleBlocks();
        public static IReadOnlyList<CityBlock> CastleIcon { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Castelo - corpo", 0, 0.1f, 1.2f, 0.8f, "A6ADB4", true, 3),
            new CityBlock("Castelo - torre oeste", -0.65f, 0.4f, 0.45f, 1, "8F99A7", false, 4),
            new CityBlock("Castelo - torre leste", 0.65f, 0.4f, 0.45f, 1, "8F99A7", false, 4),
            new CityBlock("Castelo - telhado oeste", -0.65f, 1, 0.55f, 0.35f, "776C8B", false, 5),
            new CityBlock("Castelo - telhado leste", 0.65f, 1, 0.55f, 0.35f, "776C8B", false, 5),
            new CityBlock("Castelo - porta", 0, -0.1f, 0.28f, 0.35f, "3C3C47", false, 5)
        });
        public static IReadOnlyList<CityBlock> ForestIcon { get; } = CreateForestIcon();
        public static IReadOnlyList<CityBlock> QuarryIcon { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Pedreira - base", 0, 0.1f, 1.6f, 0.7f, "9D9A88", true, 2)
        });
        public static IReadOnlyList<CityBlock> VolcanoIcon { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Vulcao - base", 0, 0, 1.7f, 0.6f, "797369", false, 3),
            new CityBlock("Vulcao - cume", 0, 0.4f, 1.1f, 0.7f, "67675F", false, 4),
            new CityBlock("Vulcao - cratera", 0, 0.7f, 0.6f, 0.25f, "D66A44", false, 5)
        });

        private static IReadOnlyList<CityBlock> CreateForestIcon()
        {
            var blocks = new List<CityBlock>();
            foreach (Vector2 offset in new[] { new Vector2(-0.45f, 0), new Vector2(0.45f, 0), new Vector2(0, 0.45f) })
            {
                blocks.Add(new CityBlock("Arvore - tronco", offset.x, offset.y - 0.2f, 0.16f, 0.45f, "79614A", false, 2));
                blocks.Add(new CityBlock("Arvore - copa", offset.x, offset.y + 0.2f, 0.8f, 0.85f, "2C5036", false, 3));
                blocks.Add(new CityBlock("Arvore - luz", offset.x - 0.08f, offset.y + 0.3f, 0.5f, 0.55f, "456B42", false, 4));
            }
            return blocks.AsReadOnly();
        }

        private static IReadOnlyList<CityBlock> CreateBlocks()
        {
            return System.Array.AsReadOnly(new[]
            {
                new CityBlock("Mar", 0, 0, 40, 28, "315F7E", false, -20),
                // Limites de referencia; a colisao real e circular, com WardRadius.
                new CityBlock(SecretLayout.SealedRocks, Volcano.x, Volcano.y,
                    WardRadius * 2, WardRadius * 2, "9377CE", true, 2)
            });
        }

        public static IReadOnlyList<CityBlock> CreateForestBlocks(bool eastern)
        {
            if (eastern)
            {
                var painted = new List<CityBlock>();
                // The painted clearing is bounded by EastForestLayout's floor contour.
                Vector2 stone = SecretLayout.ClawStone, tree = SecretLayout.HollowTree;
                painted.Add(new CityBlock("Pedra com escamas", stone.x, stone.y, 1.4f, 1, "8B927F", true, 2));
                painted.Add(new CityBlock("Raizes da arvore oca", tree.x, tree.y, 1.6f, 1, "385539", true, 2));
                return painted.AsReadOnly();
            }
            var blocks = new List<CityBlock>
            {
                new CityBlock("Chao da floresta", 0, 0, 18, 14, eastern ? "4D6945" : "3C6044", false, -10),
                new CityBlock("Clareira", 0, 0, 7, 9, "63815B", false, -9),
                new CityBlock("Caminho da floresta", 0, -1, 2.2f, 11, "A69A6D", false, -8)
            };
            AddWalls(blocks, ForestBounds, "2A4533");
            foreach (float x in new[] { -6.5f, -4.5f, 4.5f, 6.5f })
                foreach (float y in new[] { -4f, -1f, 2f, 5f })
                {
                    blocks.Add(new CityBlock("Arvore - copa", x, y, 1.8f, 2, eastern ? "385539" : "254A35", true, 2));
                    blocks.Add(new CityBlock("Arvore - folhagem", x - 0.15f, y + 0.15f, 1.2f, 1.3f, "52764C", false, 3));
                }
            blocks.Add(new CityBlock(eastern ? "Pedra com escamas" : "Pedra antiga", eastern ? 2 : -2, 3, 1.4f, 1, "8B927F", true, 2));
            if (!eastern) blocks.AddRange(SecretLayout.WestMonoliths);
            return blocks.AsReadOnly();
        }

        private static IReadOnlyList<CityBlock> CreateCastleBlocks()
        {
            var blocks = new List<CityBlock>
            {
                new CityBlock("Piso do castelo", 0, 0, 20, 16, "888C94", false, -10),
                new CityBlock("Tapete real", 0, 0, 3, 13, "944C59", false, -9),
                new CityBlock("Estrado do trono", 0, 5.5f, 6, 3, "B0A78C", false, -8),
                new CityBlock("Degraus do trono", 0, 3.7f, 6, 0.5f, "AAA396", false, -7),
                new CityBlock("Trono - encosto", 0, 6.1f, 1.3f, 1.2f, "775259", true, 3),
                new CityBlock("Trono - ouro", 0, 6.6f, 1.5f, 0.2f, "D8B86A", false, 4)
            };
            AddWalls(blocks, CastleBounds, "505562");
            foreach (float x in new[] { -6f, 6f })
                foreach (float y in new[] { -3f, 1f, 5f })
                {
                    blocks.Add(new CityBlock("Coluna", x, y, 1, 1.4f, "CBD0CD", true, 2));
                    blocks.Add(new CityBlock("Coluna - detalhe", x, y + 0.6f, 1.3f, 0.3f, "ADB4B4", false, 3));
                }
            return blocks.AsReadOnly();
        }

        private static void AddWalls(List<CityBlock> blocks, Rect bounds, string color)
        {
            blocks.Add(new CityBlock("Limite norte", 0, bounds.yMax - 0.3f, bounds.width, 0.6f, color, true, 5));
            blocks.Add(new CityBlock("Limite sul", 0, bounds.yMin + 0.3f, bounds.width, 0.6f, color, true, 5));
            blocks.Add(new CityBlock("Limite oeste", bounds.xMin + 0.3f, 0, 0.6f, bounds.height, color, true, 5));
            blocks.Add(new CityBlock("Limite leste", bounds.xMax - 0.3f, 0, 0.6f, bounds.height, color, true, 5));
        }
    }
}
