using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Geometria compartilhada pelo cenario, camera, placas e verificacao de caminhos.
    // Blocos descrevem a planta; ObliqueScenery transforma essa planta em volumes 2.5D.
    public readonly struct CityBlock
    {
        public string Name { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public string Color { get; }
        public bool Solid { get; }
        public int Order { get; }
        public Rect Bounds => new Rect(Position.x - Size.x / 2, Position.y - Size.y / 2, Size.x, Size.y);

        public CityBlock(string name, float x, float y, float width, float height, string color, bool solid, int order)
        {
            Name = name; Position = new Vector2(x, y); Size = new Vector2(width, height);
            Color = color; Solid = solid; Order = order;
        }
    }

    public readonly struct CityDistrict
    {
        public string Name { get; }
        public Rect Bounds { get; }
        public Vector2 VisitPoint { get; }
        public CityDistrict(string name, Rect bounds, Vector2 visitPoint)
        { Name = name; Bounds = bounds; VisitPoint = visitPoint; }
    }

    public static class PrototypeCityLayout
    {
        public static readonly Rect Bounds = new Rect(-36, -24, 72, 48);
        public static readonly Vector2 Spawn = new Vector2(0, -4);
        public static readonly Vector2 Castle = new Vector2(0, 19);
        public static readonly Vector2 Guard = new Vector2(0, 12.5f);
        public static readonly Vector2 Citizen = new Vector2(-3.4f, -3);
        public static readonly Vector2 Chest = new Vector2(3.6f, -3.2f);
        public static readonly Vector2 Resident = new Vector2(-25.5f, -3);
        public static readonly Vector2 Gardener = new Vector2(26, 10);
        public static readonly Vector2 Explorer = new Vector2(26, -15);

        public static IReadOnlyList<CityDistrict> Districts { get; } = System.Array.AsReadOnly(new[]
        {
            new CityDistrict("BAIRRO RESIDENCIAL", new Rect(-36, -24, 19, 48), Resident + Vector2.down),
            new CityDistrict("JARDINS REAIS", new Rect(17, 2, 19, 22), Gardener + Vector2.down),
            new CityDistrict("RUINAS ANTIGAS", new Rect(17, -24, 19, 26), Explorer + Vector2.down),
            new CityDistrict("DISTRITO DO CASTELO", new Rect(-17, 10, 34, 14), new Vector2(0, 14)),
            new CityDistrict("PORTAO SUL", new Rect(-17, -24, 34, 14), new Vector2(0, -20)),
            new CityDistrict("PRACA CENTRAL", new Rect(-17, -10, 34, 20), Spawn)
        });

        public static string DistrictAt(Vector2 position)
        {
            foreach (CityDistrict district in Districts) if (district.Bounds.Contains(position)) return district.Name;
            return "CIDADE";
        }

        // Nos edificios, Color representa a cor do telhado.
        public static IReadOnlyList<CityBlock> Buildings { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Loja de itens", -9, 4.5f, 5, 4, "A86B49", true, 1),
            new CityBlock("Loja de materias", 9, 4.5f, 5, 4, "71638E", true, 1),
            new CityBlock("Hospedaria", -9, -5, 5, 4, "7F8D68", true, 1),
            new CityBlock("Loja de equipamentos", 9, -5, 5, 4, "627C8C", true, 1),
            new CityBlock("Castelo do tirano", Castle.x, Castle.y, 9, 6, "636B79", true, 1),
            new CityBlock("Torre oeste do castelo", -11, 19, 3, 5, "586374", true, 1),
            new CityBlock("Torre leste do castelo", 11, 19, 3, 5, "586374", true, 1),
            new CityBlock("Casa do tecelao", -29, 6, 5, 4, "98674F", true, 1),
            new CityBlock("Casa do padeiro", -19, 6, 5, 4, "B07B53", true, 1),
            new CityBlock("Casa das flores", -29, 15, 5, 4, "8D677B", true, 1),
            new CityBlock("Casa do carpinteiro", -19, 15, 5, 4, "80644C", true, 1),
            new CityBlock("Capela antiga", -24, 21, 7, 3, "707B91", true, 1),
            new CityBlock("Casa ribeirinha oeste", -29, -21, 5, 3, "9A735B", true, 1),
            new CityBlock("Casa ribeirinha leste", -19, -21, 5, 3, "936458", true, 1),
            new CityBlock("Pavilhao dos jardins", 20, 19, 5, 3, "728C79", true, 1),
            new CityBlock("Torre em ruinas", 29, -19, 4, 5, "6B6A66", true, 1)
        });

        public static IReadOnlyList<CityBlock> Blocks { get; } = CreateBlocks();

        private static IReadOnlyList<CityBlock> CreateBlocks()
        {
            var blocks = new List<CityBlock>
            {
                new CityBlock("Gramado", 0, 0, Bounds.width, Bounds.height, "45634D", false, -10),
                new CityBlock("Jardim - gramado", 25, 13, 18, 19, "55795A", false, -9),
                new CityBlock("Ruinas - terreno", 25, -17, 18, 11, "807C63", false, -9),
                new CityBlock("Rua norte-sul", 0, -1, 3.5f, 43, "C2AC80", false, -8),
                new CityBlock("Rua leste-oeste", 0, 0, 70, 3.5f, "C2AC80", false, -8),
                new CityBlock("Rua residencial", -24, 0, 3.5f, 42, "C2AC80", false, -8),
                new CityBlock("Rua dos jardins e ruinas", 24, 0, 3.5f, 42, "C2AC80", false, -8),
                new CityBlock("Alameda dos jardins", 25, 10, 17, 3, "C2AC80", false, -8),
                new CityBlock("Rua sul", 0, -18, 45, 3, "C2AC80", false, -8),
                new CityBlock("Patio do castelo", 0, 13.5f, 15, 5, "C3C1B4", false, -7),
                new CityBlock("Praca", 0, 0, 8, 7, "D1C19B", false, -7),
                new CityBlock("Lago dos jardins", 29, 14, 5, 4, "5BA5B6", true, -6),
                new CityBlock("Fonte - borda", 0, 0, 2.6f, 2.6f, "747A80", true, 2),
                new CityBlock("Fonte - agua", 0, 0, 2.05f, 2.05f, "5BA5B6", false, 3),
                new CityBlock("Fonte - centro", 0, 0, 0.6f, 0.6f, "C8D2CB", false, 4),
                new CityBlock("Banco oeste", -3, 0, 0.6f, 1.6f, "805E43", true, 2),
                new CityBlock("Banco leste", 3, 0, 0.6f, 1.6f, "805E43", true, 2),
                new CityBlock("Banco do jardim", 20, 13, 2, 0.6f, "805E43", true, 2),
                new CityBlock("Ruina - parede oeste", 19, -19, 0.8f, 5, "797A73", true, 2),
                new CityBlock("Ruina - parede norte", 20, -16, 3, 0.8f, "797A73", true, 2),
                new CityBlock("Ruina - parede sul", 24, -22, 7, 0.8f, "797A73", true, 2),
                new CityBlock("Pedestal antigo", 24, -19.5f, 1, 1, "AEA98E", true, 2),
                new CityBlock("Portao sul - torre oeste", -4, -22, 2, 2, "636B79", true, 3),
                new CityBlock("Portao sul - torre leste", 4, -22, 2, 2, "636B79", true, 3),
                new CityBlock("Muralha norte", 0, Bounds.yMax - 0.3f, Bounds.width, 0.6f, "434B55", true, 5),
                new CityBlock("Muralha sul", 0, Bounds.yMin + 0.3f, Bounds.width, 0.6f, "434B55", true, 5),
                new CityBlock("Muralha oeste", Bounds.xMin + 0.3f, 0, 0.6f, Bounds.height, "434B55", true, 5),
                new CityBlock("Muralha leste", Bounds.xMax - 0.3f, 0, 0.6f, Bounds.height, "434B55", true, 5)
            };

            // O rio e solido; as tres lacunas sao atravessadas pelas pontes.
            foreach (Vector2 span in new[] { new Vector2(-35.4f, -26), new Vector2(-22, -2), new Vector2(2, 22), new Vector2(26, 35.4f) })
                blocks.Add(new CityBlock("Rio", (span.x + span.y) / 2, -12, span.y - span.x, 2.2f, "437D96", true, -6));
            foreach (float x in new[] { -24f, 0f, 24f })
            {
                blocks.Add(new CityBlock("Ponte - piso", x, -12, 4, 4, "9C7950", false, -5));
                for (int plank = -3; plank <= 3; plank++)
                    blocks.Add(new CityBlock("Ponte - tabua", x, -12 + plank * 0.5f, 4, 0.07f, "69543D", false, -4));
                foreach (float side in new[] { -2f, 2f })
                    blocks.Add(new CityBlock("Ponte - corrimao", x + side, -12, 0.2f, 4, "69543D", true, 2));
            }

            foreach (Vector2 position in new[]
            {
                new Vector2(-14, 8), new Vector2(14, 8), new Vector2(-14, -8), new Vector2(14, -8),
                new Vector2(-33, 10), new Vector2(-33, -5), new Vector2(-20, -5),
                new Vector2(19, 5), new Vector2(29, 5), new Vector2(33, 10), new Vector2(33, 20),
                new Vector2(28, 20), new Vector2(16, 21), new Vector2(-16, 21),
                new Vector2(-32, -22), new Vector2(12, -20), new Vector2(-12, -20)
            })
            {
                blocks.Add(new CityBlock("Arvore - copa", position.x, position.y, 1.8f, 1.8f, "294936", true, 2));
                blocks.Add(new CityBlock("Arvore - detalhe", position.x - 0.15f, position.y + 0.15f, 1.1f, 1.1f, "3A6141", false, 3));
            }
            foreach (Vector2 position in new[] { new Vector2(20, 7), new Vector2(28, 7), new Vector2(20, 16), new Vector2(26, 18) })
            {
                blocks.Add(new CityBlock("Canteiro", position.x, position.y, 2, 1.2f, "36543A", false, -5));
                foreach (float offset in new[] { -0.6f, 0f, 0.6f })
                    blocks.Add(new CityBlock("Flores", position.x + offset, position.y, 0.25f, 0.3f, "D7B871", false, -4));
            }
            return blocks.AsReadOnly();
        }
    }
}
