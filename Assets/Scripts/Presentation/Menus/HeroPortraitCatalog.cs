using DragonQuest.Infrastructure;
using System.Collections.Generic;
using UnityEngine;

// Localiza e desenha os retratos dos personagens do grupo.
namespace DragonQuest.Presentation.Menus
{
    // Shared by party menus, shops and save slots; portraits are Resources-owned textures.
    public static class HeroPortraitCatalog
    {
        // Reutiliza as texturas carregadas para cada chave de retrato.
        private static readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();

        // Resolve o caminho do retrato a partir do identificador do personagem.
        public static string ResourceFor(string memberId)
        {
            switch (memberId)
            {
                case "lia": return "PixelArt/HeroPortraits/lia";
                case "bram": return "PixelArt/HeroPortraits/bram";
                default: return "PixelArt/DialoguePortraits/aren";
            }
        }

        public static Texture2D Load(string memberId)
        {
            string resource = ResourceFor(memberId);
            if (!portraits.TryGetValue(resource, out var portrait) || portrait == null)
            {
                portrait = GameAssets.Optional<Texture2D>(resource);
                if (portrait != null) portraits[resource] = portrait;
            }
            return portrait;
        }

        public static void Draw(Rect rect, string memberId)
        {
            Texture2D portrait = Load(memberId);
            if (portrait != null) GUI.DrawTexture(rect, portrait, ScaleMode.ScaleToFit, true);
        }
    }
}
