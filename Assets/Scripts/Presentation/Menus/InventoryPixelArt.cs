using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public static class InventoryPixelArt
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        public static Texture2D Texture(EquipmentDefinition definition) => Load(InventoryIconCatalog.Key(definition));
        public static Texture2D Texture(MateriaDefinition definition) => Load(InventoryIconCatalog.Key(definition));
        public static Texture2D Texture(ConsumableDefinition definition) => Load(InventoryIconCatalog.Key(definition));

        private static Texture2D Load(string key)
        {
            if (key == null) return null;
            if (!textures.TryGetValue(key, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>("PixelArt/Inventory/" + key);
                textures.Add(key, texture);
            }
            return texture;
        }

        public static void Draw(Rect rect, Texture2D texture)
        {
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        }

        public static GUIStyle ButtonStyle(GUIStyle original, int padding = 48) => new GUIStyle(original)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(padding, 12, original.padding.top, original.padding.bottom)
        };
    }
}
