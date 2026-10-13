using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Exploration
{
    public sealed class PixelSpriteAtlas : IDisposable
    {
        public Texture2D Texture { get; }
        public Sprite[] Sprites { get; }
        public PixelSpriteAtlas(string resource)
        {
            Texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            if (Texture == null) throw new InvalidOperationException("Atlas ausente: " + resource);
            Color32[] pixels = Texture.GetPixels32(); var alpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a;
            bool walkingSheet = resource == "ArenWalk" || resource == "ArenDiagonalWalk";
            bool chest = resource == "ChestStates";
            bool worldPoi = resource == "WorldPoiSprites";
            bool dragon = resource == "Dragon";
            var frames = dragon ? PixelSpriteLayout.ReadSingle(Texture.width, Texture.height, alpha)
                : worldPoi ? PixelSpriteLayout.ReadWorldIcons(Texture.width, Texture.height, alpha)
                : chest ? PixelSpriteLayout.ReadPropPair(Texture.width, Texture.height, alpha)
                : PixelSpriteLayout.Read(Texture.width, Texture.height, alpha, walkingSheet);
            int maxHeight = 1; foreach (var frame in frames) maxHeight = Math.Max(maxHeight, frame.Height);
            // Mesma escala nos dois estados; abrir a tampa nao redimensiona a caixa.
            float pixelsPerUnit = chest ? frames[0].Width / 1.1f : maxHeight / ((dragon ? 3.8f : worldPoi ? 1.95f : 1.45f) * ObliqueProjection.HeightScale);
            Sprites = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                var f = frames[i];
                Sprites[i] = Sprite.Create(Texture, f.Rect, f.Pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                Sprites[i].name = resource + " / " + i;
            }
        }
        public void Draw(Rect area, int frame, Color tint, bool flipHorizontal = false)
        {
            Rect source = Sprites[frame].rect;
            float scale = Math.Min(area.width / source.width, area.height / source.height);
            var destination = new Rect(area.center.x - source.width * scale / 2, area.yMax - source.height * scale, source.width * scale, source.height * scale);
            var uv = new Rect(source.x / Texture.width, source.y / Texture.height, source.width / Texture.width, source.height / Texture.height);
            if (flipHorizontal) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
            Color previous = GUI.color; GUI.color = tint; GUI.DrawTextureWithTexCoords(destination, Texture, uv, true); GUI.color = previous;
        }
        public void Dispose() { foreach (var sprite in Sprites) UnityEngine.Object.Destroy(sprite); }
    }

    public sealed class PixelSpriteLibrary : IDisposable
    {
        private readonly Dictionary<string, PixelSpriteAtlas> atlases = new Dictionary<string, PixelSpriteAtlas>();
        public Material Material { get; }
        public PixelSpriteLibrary()
        {
            var shader = Resources.Load<Shader>("PixelCharacter");
            if (shader == null) throw new InvalidOperationException("Shader PixelCharacter ausente.");
            Material = new Material(shader) {name = "Sprites pixel art / profundidade"};
        }
        public PixelSpriteAtlas Atlas(string name)
        {
            if (!atlases.TryGetValue(name, out var atlas)) { atlas = new PixelSpriteAtlas(name); atlases.Add(name, atlas); }
            return atlas;
        }
        public PixelSpriteAtlas Party(string id) => Atlas(id == "lia" ? "LiaWalk" : id == "bram" ? "BramWalk" : "ArenWalk");
        public Transform Create(Transform anchor, string atlasName, int frame = 0, bool animated = false)
        {
            GameObject visual = new GameObject("Sprite pixel art / " + atlasName);
            visual.transform.SetParent(anchor, false);
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = Material;
            var diagonals = animated && atlasName == "ArenWalk" ? Atlas("ArenDiagonalWalk") : null;
            visual.AddComponent<PixelCharacter>().Initialize(anchor, renderer, Atlas(atlasName), frame, animated, diagonals);
            return visual.transform;
        }
        public static int NpcFrame(string name)
        {
            switch (name)
            {
                case "Camponesa": return 1; case "Jardineiro": return 2; case "Exploradora": return 3;
                case "Loja de itens": return 4; case "Loja de equipamentos": return 5; case "Loja de materias": return 6;
                case "Hospedeira": return 7; case "Lenhador": return 8; case "Viajante": return 9;
                case "Guarda do tirano": return 10; case "Tirano": return 11; default: return 0;
            }
        }
        public void Dispose() { foreach (var atlas in atlases.Values) atlas.Dispose(); UnityEngine.Object.Destroy(Material); }
    }
}
