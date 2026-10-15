using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Keep the font in Resources so builds use the same lettering as the Editor.
    public static class PrototypeTypography
    {
        private static Font regular, bold;

        private static Font Typeface(bool emphasized)
        {
            Font font = emphasized ? bold : regular;
            if (font == null)
            {
                string resource = emphasized ? "Fonts/PixelOperator-Bold" : "Fonts/PixelOperator";
                font = Resources.Load<Font>(resource);
                if (font == null) throw new InvalidOperationException("Fonte do jogo ausente: " + resource);
                if (emphasized) bold = font;
                else regular = font;
            }
            return font;
        }

        public static GUIStyle Style(GUIStyle template, int size, bool emphasized = false)
            => new GUIStyle(template)
            {
                font = Typeface(emphasized),
                fontSize = size,
                // The bold file supplies the weight; synthetic bold blurs the pixel shapes.
                fontStyle = FontStyle.Normal
            };
    }
}
