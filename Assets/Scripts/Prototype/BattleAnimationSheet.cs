using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Reads generated atlases without changing their colors or alpha.
    public sealed class BattleAnimationSheet
    {
        private readonly Texture2D texture;
        private readonly Rect[] uv;
        private readonly Vector2[] size;
        private readonly float[] anchor;
        private readonly float[] feet;
        private readonly float[] frameScale;
        private readonly Texture2D[] frameTextures;
        private readonly float referenceHeight;

        public BattleAnimationSheet(string resource, int columns, int rows, bool useIdleHeight)
        {
            texture = Resources.Load<Texture2D>(resource);
            uv = new Rect[columns * rows]; size = new Vector2[uv.Length]; anchor = new float[uv.Length];
            feet = new float[uv.Length]; frameScale = new float[uv.Length]; frameTextures = new Texture2D[uv.Length];
            for (int i = 0; i < frameScale.Length; i++) frameScale[i] = 1;
            if (texture == null) return;
            texture.filterMode = FilterMode.Point;
            Color32[] pixels = texture.GetPixels32();
            float maxHeight = 0;
            for (int row = 0; row < rows; row++)
            {
                int bottomEdge = texture.height * (rows - row - 1) / rows;
                int topEdge = texture.height * (rows - row) / rows;
                int[] density = new int[texture.width];
                for (int y = bottomEdge; y < topEdge; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a >= 128) density[x]++;
                int[] boundaries = new int[columns + 1]; boundaries[columns] = texture.width;
                for (int col = 1; col < columns; col++)
                {
                    int expected = texture.width * col / columns;
                    int radius = texture.width / (columns * 3);
                    int best = expected; float score = float.MaxValue;
                    for (int x = expected - radius; x <= expected + radius; x++)
                    {
                        float candidate = density[x] + Math.Abs(x - expected) * .03f;
                        if (candidate < score) { score = candidate; best = x; }
                    }
                    boundaries[col] = best;
                }
                for (int col = 0; col < columns; col++)
                {
                    int left = boundaries[col + 1], right = boundaries[col] - 1;
                    int bottom = topEdge, top = bottomEdge - 1;
                    for (int y = bottomEdge; y < topEdge; y++)
                        for (int x = boundaries[col]; x < boundaries[col + 1]; x++)
                            if (pixels[y * texture.width + x].a >= 128)
                            {
                                left = Math.Min(left, x); right = Math.Max(right, x);
                                bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                            }
                    if (right < left) continue;
                    int index = row * columns + col;
                    size[index] = new Vector2(right - left + 1, top - bottom + 1);
                    maxHeight = Math.Max(maxHeight, size[index].y);
                    uv[index] = new Rect((float)left / texture.width, (float)bottom / texture.height,
                        size[index].x / texture.width, size[index].y / texture.height);
                    int footLeft = right, footRight = left;
                    for (int y = bottom; y <= Math.Min(top, bottom + Math.Max(2, (top - bottom) / 12)); y++)
                        for (int x = left; x <= right; x++)
                            if (pixels[y * texture.width + x].a >= 128)
                            { footLeft = Math.Min(footLeft, x); footRight = Math.Max(footRight, x); }
                    anchor[index] = useIdleHeight ? (footLeft + footRight + 1) / 2f - left : size[index].x / 2;
                    feet[index] = (footLeft + footRight + 1) / 2f - left;
                }
            }
            referenceHeight = useIdleHeight ? size[0].y : maxHeight;
        }

        // A complete attack image replaces a clipped atlas frame. All other poses
        // retain their original pixels, scale, timing and positions.
        public bool ReplaceFrame(int frame, string resource)
        {
            if (frame < 0 || frame >= uv.Length || size[frame].y <= 0) return false;
            Texture2D replacement = Resources.Load<Texture2D>(resource);
            if (replacement == null) return false;
            replacement.filterMode = FilterMode.Point;
            Color32[] pixels = replacement.GetPixels32();
            int left = replacement.width, right = -1, bottom = replacement.height, top = -1;
            for (int y = 0; y < replacement.height; y++) for (int x = 0; x < replacement.width; x++)
                if (pixels[y * replacement.width + x].a >= 128)
                { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
            if (right < left) return false;
            float originalHeight = size[frame].y;
            float originalFootOffset = feet[frame] - anchor[frame];
            int footLeft = right, footRight = left;
            for (int y = bottom; y <= Math.Min(top, bottom + Math.Max(2, (top-bottom) / 12)); y++)
                for (int x = left; x <= right; x++)
                    if (pixels[y * replacement.width + x].a >= 128)
                    { footLeft = Math.Min(footLeft, x); footRight = Math.Max(footRight, x); }
            size[frame] = new Vector2(right-left+1, top-bottom+1);
            feet[frame] = (footLeft + footRight + 1) / 2f - left;
            frameScale[frame] = originalHeight / size[frame].y;
            anchor[frame] = feet[frame] - originalFootOffset / frameScale[frame];
            uv[frame] = new Rect((float)left / replacement.width, (float)bottom / replacement.height,
                size[frame].x / replacement.width, size[frame].y / replacement.height);
            frameTextures[frame] = replacement;
            return true;
        }

        public bool Draw(Vector2 foot, float height, int frame, Color tint)
        {
            if (texture == null || referenceHeight <= 0) return false;
            frame = Math.Max(0, Math.Min(frame, uv.Length - 1));
            if (size[frame].y <= 0) return false;
            float scale = height / referenceHeight * frameScale[frame];
            Rect rect = new Rect(foot.x - anchor[frame] * scale, foot.y - size[frame].y * scale,
                size[frame].x * scale, size[frame].y * scale);
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, frameTextures[frame] ?? texture, uv[frame], true);
            GUI.color = previous;
            return true;
        }
    }
}
