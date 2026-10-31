using System;
using UnityEngine;

namespace DragonQuest.Presentation.World.Rendering
{
    public enum PixelSurface { None = -1, Grass, Dirt, Paving, Sand, Wood, Wall, Roof, Basalt, Leaves, Water, Lava, Fabric, Auto }
    public static class PixelSurfaces
    {
        public const float TileSize = 2f;
        public static PixelSurface ForBlock(string name, bool solid, int order)
        {
            name = name.ToLowerInvariant();
            if (name.StartsWith("lava")) return PixelSurface.Lava;
            if (name.Contains("arvore")) return PixelSurface.Leaves;
            if (name == "rio" || name.StartsWith("lago") || name.Contains("agua")) return PixelSurface.Water;
            if (name.Contains("basalto") || name.Contains("vulcanico") || name.Contains("arena ancestral")) return PixelSurface.Basalt;
            if (name.Contains("trilha") || name.Contains("clareira") || name.Contains("caminho")) return PixelSurface.Dirt;
            if (name.Contains("tapete") || name.Contains("cama")) return PixelSurface.Fabric;
            if (name.Contains("madeira") || name.Contains("hospedaria") || name.Contains("ponte") || name.Contains("banco") || name.Contains("balcao") || name == "estante") return PixelSurface.Wood;
            if (name.Contains("pedra") || name.Contains("rochas") || name.Contains("cordilheira")) return PixelSurface.Basalt;
            if (name.Contains("parede") || name.Contains("muralha") || name.Contains("coluna") || name.Contains("monolito") || name.Contains("altar") || name.Contains("limite")) return PixelSurface.Wall;
            if (name.Contains("piso") || name.Contains("praca") || name.Contains("patio") || name.Contains("rua") || name.Contains("calcada") || name.Contains("alameda")) return PixelSurface.Paving;
            if (name.Contains("grama") || name.Contains("jardim") || name.Contains("chao") || name.Contains("terreno") || name.Contains("bosque") || name.Contains("canteiro")) return PixelSurface.Grass;
            return PixelSurface.None;
        }
        public static Vector2 Project(Vector3 point, Vector3 normal)
        {
            float x = Math.Abs(normal.x), y = Math.Abs(normal.y), z = Math.Abs(normal.z);
            return z >= x && z >= y ? new Vector2(point.x / TileSize, point.y / TileSize)
                : x > y ? new Vector2(point.y / TileSize, -point.z / TileSize) : new Vector2(point.x / TileSize, -point.z / TileSize);
        }
        public static Rect AtlasCell(PixelSurface surface)
        {
            int index = (int)surface;
            if (index < 0 || index >= 12) throw new ArgumentOutOfRangeException(nameof(surface));
            return new Rect(index % 4 / 4f, (2 - index / 4) / 3f, 1f / 4, 1f / 3);
        }
    }
}
