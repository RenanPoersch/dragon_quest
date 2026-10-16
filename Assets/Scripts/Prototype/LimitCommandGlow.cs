using System.Text;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Animated rainbow and pulsing halo for the ready LIMIT battle command.
    public static class LimitCommandGlow
    {
        public static bool Button(Rect rect, string text, bool ready, GUIStyle style)
        {
            if (!ready) return GUI.Button(rect, text, style);

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
                bool clicked = GUI.Button(rect, GUIContent.none, style);
                GUI.backgroundColor = Color.clear;
                // A small halo breathes without flashing or changing the name's layout.
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                        previousColor.a * (.14f + pulse * .12f));
                    var content = new GUIContent(label);
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                        {
                            if (x == 0 && y == 0) continue;
                            Rect glow = rect;
                            glow.position += new Vector2(x, y) * 1.5f;
                            style.Draw(glow, content, false, false, false, false);
                        }
                }
                GUI.color = previousColor;
                GUI.Label(rect, label, style);
                return clicked;
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
