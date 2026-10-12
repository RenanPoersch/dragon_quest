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
            Rect bounds = TerrainBounds;
            return new Vector2((ground.x - bounds.xMin) / bounds.width, (ground.y - bounds.yMin) / bounds.height);
        }

        public static ObliqueMeshData Build()
        {
            var mesh = new ObliqueMeshData();
            Rect bounds = TerrainBounds, city = PrototypeCityLayout.Bounds;
            float south = PrototypeCityLayout.Bridge.y - 2.4f, north = PrototypeCityLayout.Bridge.y + 2.4f;
            mesh.Plane(bounds.center.x, (bounds.yMin + south) / 2, bounds.width, south - bounds.yMin, 0, Color.white);
            mesh.Plane(bounds.center.x, (north + bounds.yMax) / 2, bounds.width, bounds.yMax - north, 0, Color.white);
            // Terra ao lado do fosso, integrada a mesma pintura e a mesma escala de UV.
            mesh.Plane((bounds.xMin + city.xMin) / 2, PrototypeCityLayout.Bridge.y, city.xMin - bounds.xMin, north - south, 0, Color.white);
            mesh.Plane((city.xMax + bounds.xMax) / 2, PrototypeCityLayout.Bridge.y, bounds.xMax - city.xMax, north - south, 0, Color.white);
            // O leito do fosso usa a mesma imagem, mas fica abaixo do piso para conservar a profundidade.
            mesh.Plane(city.center.x, PrototypeCityLayout.Bridge.y, city.width, north - south, -0.7f, Color.white);
            // A pintura da ponte fica ligeiramente acima do volume de pedra, sem sobreposicao de profundidade.
            mesh.Plane(0, PrototypeCityLayout.Bridge.y, 3.7f, 5.8f, 0.058f, Color.white);
            // Fecha as laterais rebaixadas para nao expor fundo vazio sob o terreno exterior.
            foreach (float x in new[] {city.xMin, city.xMax})
                mesh.Quad(new Vector3(x, south, 0), new Vector3(x, north, 0),
                    new Vector3(x, north, 0.7f), new Vector3(x, south, 0.7f), Color.white, false);
            for (int i = 0; i < mesh.Vertices.Count; i++)
                mesh.UVs[i] = MapUv(new Vector2(mesh.Vertices[i].x, mesh.Vertices[i].y));
            return mesh;
        }

    }
}
