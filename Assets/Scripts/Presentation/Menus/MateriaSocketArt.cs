using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public static class MateriaSocketArt
    {
        // Pixel rectangles in MateriaSockets.png, measured from its bottom-left corner.
        private static readonly Rect single = new Rect(77, 672, 499, 488);
        private static readonly Rect linked = new Rect(78, 94, 1099, 486);
        private static readonly Dictionary<Texture2D, Rect> materiaBounds = new Dictionary<Texture2D, Rect>();
        private static Texture2D atlas;

        private static Texture2D Atlas => atlas != null ? atlas : atlas = Resources.Load<Texture2D>("PixelArt/Inventory/MateriaSockets")
            ?? throw new InvalidOperationException("Sprite de encaixes de materia ausente.");

        public static void DrawSingle(Rect rect) => DrawPart(rect, single);

        public static void DrawLinked(Rect left, Rect right)
        {
            // Keep the rings circular; only stretch the bridge when the menu uses a different spacing.
            const float cap = 499;
            DrawPart(left, new Rect(linked.x, linked.y, cap, linked.height));
            DrawPart(right, new Rect(linked.xMax - cap, linked.y, cap, linked.height));
            float gap = right.x - left.xMax;
            if (gap > 0) DrawPart(new Rect(left.xMax, left.y, gap, left.height),
                new Rect(linked.x + cap, linked.y, linked.width - cap * 2, linked.height));
        }

        private static void DrawPart(Rect rect, Rect pixels)
        {
            Texture2D texture = Atlas;
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(pixels.x / texture.width, pixels.y / texture.height,
                pixels.width / texture.width, pixels.height / texture.height), true);
        }

        public static void DrawMateria(Rect rect, MateriaDefinition definition)
        {
            Texture2D texture = InventoryPixelArt.Texture(definition);
            if (texture == null) return;
            if (!materiaBounds.TryGetValue(texture, out Rect uv))
            {
                Color32[] pixels = texture.GetPixels32();
                int left = texture.width, bottom = texture.height, right = -1, top = -1;
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a < 16) continue;
                    left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y);
                    right = Mathf.Max(right, x); top = Mathf.Max(top, y);
                }
                uv = right < left ? new Rect(0, 0, 1, 1) : new Rect((float)left / texture.width, (float)bottom / texture.height,
                    (float)(right - left + 1) / texture.width, (float)(top - bottom + 1) / texture.height);
                materiaBounds.Add(texture, uv);
            }
            float aspect = uv.width * texture.width / (uv.height * texture.height);
            float width = Mathf.Min(rect.width, rect.height * aspect), height = width / aspect;
            GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - width / 2, rect.center.y - height / 2, width, height), texture, uv, true);
        }

        public static void DrawEquippedMateria(Rect socket, MateriaInstance materia)
        {
            float inset = socket.width * .18f;
            DrawMateria(new Rect(socket.x + inset, socket.y + inset, socket.width - inset * 2, socket.height - inset * 2), materia?.Definition);
        }
    }
}
