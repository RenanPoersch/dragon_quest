using UnityEngine;

namespace DragonQuest.Prototype
{
    // Uma pintura cobre toda a planta; as alturas mantem a ponte e o fosso fisicamente legiveis.
    public static class CityGroundArt
    {
        public static Rect TerrainBounds => PrototypeCityLayout.TerrainBounds;
        public static bool ReplacesBlock(string name) => name == "Chao escuro da periferia"
            || name.StartsWith("Patio") || name == "Canteiro do bairro";

        public static Vector2 MapUv(Vector2 ground)
        {
            Rect bounds = PrototypeCityLayout.Bounds;
            return new Vector2((ground.x - bounds.xMin) / bounds.width, (ground.y - bounds.yMin) / bounds.height);
        }

        public static ObliqueMeshData Build()
        {
            var mesh = new ObliqueMeshData();
            Rect bounds = PrototypeCityLayout.Bounds;
            float south = PrototypeCityLayout.Bridge.y - 2.4f, north = PrototypeCityLayout.Bridge.y + 2.4f;
            mesh.Plane(bounds.center.x, (bounds.yMin + south) / 2, bounds.width, south - bounds.yMin, 0, Color.white);
            mesh.Plane(bounds.center.x, (north + bounds.yMax) / 2, bounds.width, bounds.yMax - north, 0, Color.white);
            // O leito do fosso usa a mesma imagem, mas fica abaixo do piso para conservar a profundidade.
            mesh.Plane(bounds.center.x, PrototypeCityLayout.Bridge.y, bounds.width, north - south, -0.7f, Color.white);
            // A pintura da ponte fica ligeiramente acima do volume de pedra, sem sobreposicao de profundidade.
            mesh.Plane(0, PrototypeCityLayout.Bridge.y, 3.7f, 5.8f, 0.058f, Color.white);
            for (int i = 0; i < mesh.Vertices.Count; i++)
                mesh.UVs[i] = MapUv(new Vector2(mesh.Vertices[i].x, mesh.Vertices[i].y));
            return mesh;
        }

        // Faixa apenas visual; o piso original e a area jogavel continuam dentro das muralhas.
        public static ObliqueMeshData BuildSurroundings()
        {
            var mesh = new ObliqueMeshData();
            Rect city = PrototypeCityLayout.Bounds, terrain = TerrainBounds;
            mesh.Plane(terrain.center.x, (terrain.yMin + city.yMin) / 2, terrain.width, city.yMin - terrain.yMin, 0, Color.white);
            mesh.Plane(terrain.center.x, (city.yMax + terrain.yMax) / 2, terrain.width, terrain.yMax - city.yMax, 0, Color.white);
            mesh.Plane((terrain.xMin + city.xMin) / 2, city.center.y, city.xMin - terrain.xMin, city.height, 0, Color.white);
            mesh.Plane((city.xMax + terrain.xMax) / 2, city.center.y, terrain.xMax - city.xMax, city.height, 0, Color.white);
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var point = mesh.Vertices[i];
                mesh.UVs[i] = new Vector2((point.x - terrain.xMin) / terrain.width, (point.y - terrain.yMin) / terrain.height);
            }
            return mesh;
        }
    }
}
