using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Coordinates are measured from the generated source; PNG pixels and alpha stay unchanged.
    public sealed class SpiritBossSpriteSheet
    {
        [Serializable] public sealed class Frame
        {
            public int x, y, width, height;
            public float pivotX;
        }
        [Serializable] public sealed class Atlas
        {
            public int width, height;
            public float casterHeight, effectHeight;
            public Frame[] frames;
        }
        private readonly Texture2D texture;
        private readonly Action<Texture2D, Rect, Rect, Color> draw;
        public Atlas Coordinates { get; }
        public Texture2D Texture => texture;
        public SpiritBossSpriteSheet(string resource, Action<Texture2D, Rect, Rect, Color> draw)
        {
            texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            var coordinates = Resources.Load<TextAsset>("PixelArt/" + resource + "Frames");
            if (texture == null || coordinates == null) throw new InvalidOperationException("Missing spirit skill atlas: " + resource);
            Coordinates = JsonUtility.FromJson<Atlas>(coordinates.text);
            if (Coordinates?.frames == null || Coordinates.frames.Length != 12 || Coordinates.casterHeight <= 0
                || Coordinates.effectHeight <= 0 || Coordinates.width != texture.width || Coordinates.height != texture.height)
                throw new InvalidOperationException("Invalid spirit skill coordinates: " + resource);
            texture.filterMode = FilterMode.Point;
            this.draw = draw;
        }
        private void Draw(Rect destination, int frame, Color tint)
        {
            Frame cell = Coordinates.frames[frame];
            Rect uv = new Rect((float)cell.x / Coordinates.width, 1f - (float)(cell.y + cell.height) / Coordinates.height,
                (float)cell.width / Coordinates.width, (float)cell.height / Coordinates.height);
            draw(texture, destination, uv, tint);
        }
        public void DrawActor(Rect home, int pose, Color tint)
        {
            pose = Mathf.Clamp(pose, 0, 3);
            Frame cell = Coordinates.frames[pose];
            float scale = home.height / Coordinates.casterHeight;
            Draw(new Rect(home.center.x - cell.pivotX * scale, home.yMax - cell.height * scale,
                cell.width * scale, cell.height * scale), pose, tint);
        }
        public void DrawEffect(Vector2 center, float height, int pose, Color tint)
        {
            pose = 4 + Mathf.Clamp(pose, 0, 7);
            Frame cell = Coordinates.frames[pose];
            float scale = height / Coordinates.effectHeight;
            Draw(new Rect(center.x - cell.width * scale / 2, center.y - cell.height * scale / 2,
                cell.width * scale, cell.height * scale), pose, tint);
        }
    }
}
