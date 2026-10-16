using System.Text;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Animated rainbow and pulsing halo for the ready LIMIT battle command.
    public static class LimitCommandGlow
    {
        private static readonly Vector2[] HaloOffsets =
        {
            Vector2.left, Vector2.right, Vector2.up, Vector2.down
        };
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
                // A faint one-pixel halo preserves the regular font's thin strokes.
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                        previousColor.a * (.035f + pulse * .035f));
                    var content = new GUIContent(label);
                    foreach (Vector2 offset in HaloOffsets)
                    {
                        Rect glow = rect;
                        glow.position += offset;
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
