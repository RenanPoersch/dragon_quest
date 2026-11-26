using DragonQuest.Infrastructure;
using System;
using UnityEngine;

// Centraliza as fontes do jogo e cria estilos de texto a partir dos modelos da interface.
namespace DragonQuest.Presentation.Common
{
    // Keep the font in Resources so builds use the same lettering as the Editor.
    public static class GameTypography
    {
        // Guarda as versões normal e destacada da fonte para reutilização.
        private static Font regular, bold;

        private static Font Typeface(bool emphasized)
        {
            Font font = emphasized ? bold : regular;
            if (font == null)
            {
                string resource = emphasized ? "Fonts/PixelOperator-Bold" : "Fonts/PixelOperator";
                font = GameAssets.Required<Font>(resource);
                if (emphasized) bold = font;
                else regular = font;
            }
            return font;
        }

        // Cria um estilo com o tamanho solicitado a partir de um modelo da interface.
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
