using System.Text;
using UnityEngine;

namespace DragonQuest.Presentation.Battle
{
    // A single rainbow text pass keeps the ready LIMIT command's strokes thin.
    public static class LimitCommandGlow
    {
        public static bool Button(Rect rect, string text, bool ready, GUIStyle style)
        {
            if (!ready) return GUI.Button(rect, text, style);

            bool clicked = GUI.Button(rect, GUIContent.none, style);
            Label(rect, text, true, style);
            return clicked;
        }

        public static void Label(Rect rect, string text, bool ready, GUIStyle style)
        {
            if (!ready) { GUI.Label(rect, text, style); return; }

            float time = Time.unscaledTime;
            float pulse = .5f + .5f * Mathf.Sin(time * 3f);
            var rainbow = new StringBuilder(text.Length * 26);
            for (int i = 0; i < text.Length; i++)
            {
                float hue = Mathf.Repeat(time * .18f + i * .85f / text.Length, 1f);
                Color color = Color.Lerp(Color.HSVToRGB(hue, .8f, 1f), Color.white, .08f + pulse * .16f);
                rainbow.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
                    .Append(text[i]).Append("</color>");
            }
            string label = rainbow.ToString();
            bool previousRichText = style.richText;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            style.richText = true;
            try
            {
                GUI.backgroundColor = Color.clear;
                GUI.color = previousColor;
                GUI.Label(rect, label, style);
            }
            finally
            {
                style.richText = previousRichText;
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackground;
            }
        }
    }
}
