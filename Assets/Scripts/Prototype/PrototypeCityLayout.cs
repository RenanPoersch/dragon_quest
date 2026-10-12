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
        public static readonly Rect Bounds = new Rect(-13, -22, 26, 44);
        public const int GroundTextureWidth = 2305, GroundTextureHeight = 2975;
        public const int GroundCoreWidth = 965, GroundCoreHeight = 1629;
        // Correspondencia exata entre planta e pixels do PNG, na densidade da pintura-fonte.
        // Apenas terreno visivel: nao amplia a area de movimento nem altera portas/saves.
        public static readonly Rect TerrainBounds = new Rect(
            -Bounds.width * GroundTextureWidth / GroundCoreWidth / 2,
            -Bounds.height * GroundTextureHeight / GroundCoreHeight / 2,
            Bounds.width * GroundTextureWidth / GroundCoreWidth,
            Bounds.height * GroundTextureHeight / GroundCoreHeight);
        public static readonly Rect LegacyBounds = new Rect(-36, -24, 72, 48);
        public static readonly Vector2 Spawn = new Vector2(0, -10);
        public static readonly Vector2 Entry = new Vector2(0, -18.5f);
        public static readonly Vector2 Exit = new Vector2(0, -20.8f);
        public static readonly Vector2 ExitSize = new Vector2(Bounds.width, 1.2f);
        public static readonly Vector2 Castle = new Vector2(0, 19);
        public static readonly Vector2 CastleDoor = new Vector2(0, 16.2f);
        public static readonly Vector2 CastleReturn = new Vector2(0, 15);
        public static readonly Vector2 Bridge = new Vector2(0, 12.5f);
        public static readonly Vector2 Guard = new Vector2(0, 8.4f);
        public static readonly Vector2 Citizen = new Vector2(-1.2f, -15);
        public static readonly Vector2 Chest = new Vector2(0.5f, -4);
        public static readonly Vector2 Resident = new Vector2(-8.2f, 0);
        public static readonly Vector2 Gardener = new Vector2(8, 7.4f);
        public static readonly Vector2 Explorer = new Vector2(7.7f, -0.2f);
        public static Vector2 ItemDoor => DoorAt("Loja de itens");
        public static Vector2 EquipmentDoor => DoorAt("Loja de equipamentos");
        public static Vector2 MateriaDoor => DoorAt("Loja de materias");
        public static Vector2 InnDoor => DoorAt("Hospedaria");
        public static readonly Vector2 DoorSize = new Vector2(1, 0.9f);
        // A ancora coincide com o painel da porta; a faixa externa recebe os pes do heroi.
        private static Vector2 DoorAt(string name)
        {
            foreach (CityBlock building in Buildings)
                if (building.Name == name) return new Vector2(building.Position.x, building.Bounds.yMin - 0.035f);
            throw new System.ArgumentException("Edificio sem porta: " + name);
        }
        public static Vector2 DoorApproach(Vector2 door) => door + Vector2.down * 0.425f;
        public static Vector2 DoorReturn(Vector2 door) => door + Vector2.down * 1.1f;
        public static IReadOnlyList<Vector2> Street { get; } = System.Array.AsReadOnly(new[] {
            Exit, Entry, new Vector2(-0.7f, -12), new Vector2(1.1f, -7), new Vector2(0, -1),
            new Vector2(-0.6f, 4.5f), new Vector2(0, 9.7f)
        });
        public static IReadOnlyList<CityDistrict> Districts { get; } = System.Array.AsReadOnly(new[] {
            new CityDistrict("PORTAO SUL", new Rect(-13, -22, 26, 5), Entry),
            new CityDistrict("BECO DAS OFICINAS", new Rect(-13, -17, 26, 10), Spawn),
            new CityDistrict("MERCADO ANTIGO", new Rect(-13, -7, 26, 10), new Vector2(0, -2)),
            new CityDistrict("JARDIM ENTRE CASAS", new Rect(2, 3, 11, 7), Gardener + Vector2.down),
            new CityDistrict("BAIRRO DA CAPELA", new Rect(-13, 3, 15, 7), new Vector2(-0.5f, 5)),
            new CityDistrict("PONTE DO CASTELO", new Rect(-13, 10, 26, 12), Bridge)
        });
        public static string DistrictAt(Vector2 position)
        {
            foreach (CityDistrict district in Districts) if (district.Bounds.Contains(position)) return district.Name;
            return "PERIFERIA";
        }
        // Jornadas do mapa amplo continuam aceitas; uma coordenada agora bloqueada volta a rua central.
        public static Vector2 SafeSavedPosition(Vector2 position)
        {
            if (!Bounds.Contains(position)) return Spawn;
            foreach (CityBlock block in Buildings)
                if (OverlapsHero(block, position)) return Spawn;
            foreach (CityBlock block in Blocks)
                if (block.Solid && OverlapsHero(block, position)) return Spawn;
            foreach (Vector2 npc in new[] {Citizen, Resident, Gardener, Explorer})
                if (OverlapsHero(new CityBlock("Morador", npc.x, npc.y, 0.55f, 0.6f, "", true, 0), position)) return Spawn;
            if (OverlapsHero(new CityBlock("Guarda", Guard.x, Guard.y, 0.65f, 0.7f, "", true, 0), position)
                || OverlapsHero(new CityBlock("Bau", Chest.x, Chest.y, 0.9f, 0.7f, "", true, 0), position)) return Spawn;
            return position;
        }
        private static bool OverlapsHero(CityBlock block, Vector2 position)
        {
            Rect bounds = block.Bounds;
            return position.x + 0.274f > bounds.xMin && position.x - 0.274f < bounds.xMax
                && position.y + 0.299f > bounds.yMin && position.y - 0.299f < bounds.yMax;
        }
        public static IReadOnlyList<CityBlock> Buildings { get; } = System.Array.AsReadOnly(new[] {
            new CityBlock("Hospedaria", -5.1f, -14, 4, 3.6f, "8C7B59", true, 1),
            new CityBlock("Loja de equipamentos", 5.1f, -10, 4, 3.4f, "746553", true, 1),
            new CityBlock("Loja de itens", -4.9f, -6, 4.5f, 3.8f, "8B7052", true, 1),
            new CityBlock("Loja de materias", 5, 2.5f, 4, 3.6f, "76745B", true, 1),
            new CityBlock("Casa do tecelao", -10, -14.4f, 3.4f, 4, "887B61", true, 1),
            new CityBlock("Casa do padeiro", 10, -15, 3.2f, 2.8f, "80684F", true, 1),
            new CityBlock("Barracao sul", -10, -19, 3.6f, 3, "746B56", true, 1),
            new CityBlock("Abrigo sul leste", 10, -19, 3.2f, 4.4f, "85765D", true, 1),
            new CityBlock("Casa dos oleiros", 10, -10.5f, 3.2f, 4.8f, "796A56", true, 1),
            new CityBlock("Oficina das lanternas", 5.3f, -4.8f, 3.6f, 3.2f, "8E7B5B", true, 1),
            new CityBlock("Armazem de sucata", 10, -3.5f, 3.4f, 6, "776850", true, 1),
            new CityBlock("Casa do carpinteiro", -10, -8, 3.2f, 4.4f, "7C6B54", true, 1),
            new CityBlock("Oficina do remendao", -10, -3, 3.6f, 3.2f, "877B5B", true, 1),
            new CityBlock("Capela antiga", -5.2f, 1.5f, 4, 3.6f, "8A7A61", true, 1),
            new CityBlock("Casa das flores", -10, 3.2f, 3.4f, 4.6f, "82745E", true, 1),
            new CityBlock("Oficina do relojoeiro", -5.1f, 6.3f, 4.2f, 3, "746A57", true, 1),
            new CityBlock("Armazem das especiarias", 10, 3.6f, 3.4f, 4, "7D6F58", true, 1),
            new CityBlock("Pavilhao dos jardins", 5.3f, 7, 4, 3.6f, "8D8168", true, 1),
            new CityBlock("Torre em ruinas", -10, 8.1f, 3, 3.2f, "6F6A59", true, 1),
            new CityBlock("Castelo do tirano", Castle.x, Castle.y, 9.5f, 4.5f, "89919B", true, 1),
            new CityBlock("Torre oeste do castelo", -6.15f, 18.2f, 3.4f, 3.6f, "89919B", true, 1),
            new CityBlock("Torre leste do castelo", 6.15f, 18.2f, 3.4f, 3.6f, "89919B", true, 1)
        });
        public static IReadOnlyList<CityBlock> Blocks { get; } = CreateBlocks();
        private static IReadOnlyList<CityBlock> CreateBlocks()
        {
            var blocks = new List<CityBlock> {
                new CityBlock("Chao escuro da periferia", 0, 0, Bounds.width, Bounds.height, "454831", false, -10),
                new CityBlock("Patio da hospedaria", -3.7f, -16.5f, 5, 2.8f, "887955", false, -8),
                new CityBlock("Patio de equipamentos", 3.3f, -12.5f, 5.4f, 2, "887955", false, -8),
                new CityBlock("Patio da loja de itens", -3.3f, -8.8f, 4.8f, 2.2f, "887955", false, -8),
                new CityBlock("Patio das materias", 3.1f, -0.2f, 5, 2.5f, "887955", false, -8),
                new CityBlock("Canteiro do bairro", 9, 7.5f, 4, 3, "424D2F", false, -9),
                new CityBlock("Fosso seco - oeste", -7.75f, Bridge.y, 10.5f, 4.8f, "34392D", true, -6),
                new CityBlock("Fosso seco - leste", 7.75f, Bridge.y, 10.5f, 4.8f, "34392D", true, -6),
                new CityBlock("Ponte de pedra - piso", 0, Bridge.y, 3.7f, 5.8f, "998C72", false, -5),
                new CityBlock("Ponte de pedra - parapeito oeste", -2.05f, Bridge.y, 0.4f, 5.8f, "8C846D", true, 2),
                new CityBlock("Ponte de pedra - parapeito leste", 2.05f, Bridge.y, 0.4f, 5.8f, "8C846D", true, 2),
                new CityBlock("Patio da entrada do castelo", 0, 16, 8, 2.3f, "988B76", false, -7),
                new CityBlock("Muralha norte", 0, Bounds.yMax - 0.3f, Bounds.width, 0.6f, "555442", true, 5),
                new CityBlock("Muralha sul", 0, Bounds.yMin + 0.3f, Bounds.width, 0.6f, "555442", true, 5),
                new CityBlock("Muralha oeste", Bounds.xMin + 0.3f, 0, 0.6f, Bounds.height, "555442", true, 5),
                new CityBlock("Muralha leste", Bounds.xMax - 0.3f, 0, 0.6f, Bounds.height, "555442", true, 5)
            };
            blocks.Add(new CityBlock("Arvore - copa", 10, 8, 1, 1, "3C4B2E", true, 2));
            return blocks.AsReadOnly();
        }
    }
}
