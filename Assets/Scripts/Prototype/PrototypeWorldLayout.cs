using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeWorldLayout
    {
        public static readonly Rect Bounds = new Rect(-20, -14, 40, 28);
        public static readonly Vector2 Spawn = new Vector2(0, -7);
        public static readonly Vector2 Castle = Vector2.zero;
        public static readonly Vector2 WestForest = new Vector2(-8, 2);
        public static readonly Vector2 EastForest = new Vector2(7, -4);
        public static readonly Vector2 Volcano = new Vector2(3, 8.5f);
        public static readonly Vector2 Trail = new Vector2(3, 5.5f);
        public static readonly Rect ForestBounds = new Rect(-9, -7, 18, 14);
        public static readonly Rect CastleBounds = new Rect(-10, -8, 20, 16);
        public static readonly Vector2 LocalExit = new Vector2(0, -5.8f);
        public static readonly Vector2 ForestSpawn = new Vector2(0, -3.5f);
        public static readonly Vector2 CastleSpawn = new Vector2(0, -5);
        public static readonly Vector2 CastleExit = new Vector2(0, -7);
        public static readonly Vector2 Throne = new Vector2(0, 5);
        public static readonly Vector2 CityEntry = new Vector2(0, -20);
        public static readonly Vector2 CityExit = new Vector2(0, -22);
        public static readonly Vector2 CastleDoor = new Vector2(0, 15.5f);

        public static IReadOnlyList<Vector2> Coast { get; } = BeveledCoast(new[]
        {
            new Vector2(-13, -7), new Vector2(-10, -7), new Vector2(-10, -10), new Vector2(8, -10),
            new Vector2(8, -8), new Vector2(13, -8), new Vector2(13, -3), new Vector2(15, -3),
            new Vector2(15, 4), new Vector2(12, 4), new Vector2(12, 8), new Vector2(6, 8),
            new Vector2(6, 10), new Vector2(-4, 10), new Vector2(-4, 8), new Vector2(-11, 8),
            new Vector2(-11, 5), new Vector2(-15, 5), new Vector2(-15, -2), new Vector2(-13, -2)
        });

        // Cantos facetados compartilhados pela malha da ilha e pelo limite fisico do mar.
        private static IReadOnlyList<Vector2> BeveledCoast(Vector2[] corners)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 p = corners[i], previous = corners[(i + corners.Length - 1) % corners.Length], next = corners[(i + 1) % corners.Length];
                float before = (float)System.Math.Sqrt((previous.x-p.x)*(previous.x-p.x)+(previous.y-p.y)*(previous.y-p.y));
                float after = (float)System.Math.Sqrt((next.x-p.x)*(next.x-p.x)+(next.y-p.y)*(next.y-p.y));
                points.Add(new Vector2(p.x+(previous.x-p.x)*0.45f/before,p.y+(previous.y-p.y)*0.45f/before));
                points.Add(new Vector2(p.x+(next.x-p.x)*0.45f/after,p.y+(next.y-p.y)*0.45f/after));
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
            var blocks = new List<CityBlock> { new CityBlock("Mar", 0, 0, 40, 28, "315F7E", false, -20) };
            for (int x = -15; x < 15; x++)
                for (int y = -10; y < 10; y++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    if (!IsLand(point)) continue;
                    bool beach = !IsLand(point + Vector2.up) || !IsLand(point + Vector2.down)
                        || !IsLand(point + Vector2.left) || !IsLand(point + Vector2.right);
                    blocks.Add(new CityBlock(beach ? "Costa - areia" : "Ilha - gramado", point.x, point.y, 1, 1,
                        beach ? "D0BF89" : (x + y) % 3 == 0 ? "55774E" : "4D7048", false, -19));
                }
            blocks.Add(new CityBlock("Trilha para o castelo", 0, -3.8f, 0.45f, 6, "B2A578", false, -18));
            blocks.Add(new CityBlock("Trilha oeste", -4, 0, 8, 0.4f, "B2A578", false, -18));
            blocks.Add(new CityBlock("Trilha da floresta oeste", -8, 1, 0.4f, 2, "B2A578", false, -18));
            blocks.Add(new CityBlock("Trilha leste", 3.5f, -4, 7, 0.4f, "B2A578", false, -18));
            blocks.Add(new CityBlock("Vegetacao da passagem", 3, 4.8f, 2.8f, 2.4f, "3C5F40", false, -18));
            blocks.Add(new CityBlock("Trilha oculta", 3, 6.8f, 0.45f, 3.2f, "827B58", false, -17));
            blocks.Add(new CityBlock("Cordilheira oeste", -4.75f, 6.5f, 12.5f, 1, "777C72", true, 2));
            blocks.Add(new CityBlock("Cordilheira leste", 8.25f, 6.5f, 7.5f, 1, "777C72", true, 2));
            blocks.Add(new CityBlock("Rochas da passagem selada", 3, 6.5f, 3, 1, "676A68", true, 2));
            foreach (Vector2 point in new[] { new Vector2(-11, -4), new Vector2(-5, -5), new Vector2(10, 1), new Vector2(-5, 4), new Vector2(6, 3) })
                blocks.Add(new CityBlock("Arvore distante", point.x, point.y, 0.65f, 0.8f, "2D5038", false, 1));
            return blocks.AsReadOnly();
        }

        public static IReadOnlyList<CityBlock> CreateForestBlocks(bool eastern)
        {
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
