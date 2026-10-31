using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.Common
{
    public sealed class NarrativePanel : IDisposable
    {
        public sealed class Heading
        {
            public string Text;
            public Rect Bounds;
            public int FontSize;
            public bool Emphasized;
            public Color Color;
            public Vector2 ShadowOffset;
            internal GUIStyle Style;
        }

        public sealed class Layout
        {
            public string ShadeName;
            public Color ShadeColor;
            public Rect ShadeBounds;
            public Heading Title, Chapter;
            public Rect DividerBounds;
            public Color DividerColor;
            public Rect TextWindow;
            public int TextSize;
            public Color TextColor;
            public float LineHeight;
            public Vector2 TextShadowOffset;
            public float CullAbove;
            public float BottomFadeInset;
        }

        private readonly Layout layout;
        private Texture2D shade;
        private GUIStyle story;

        public NarrativePanel(Layout layout)
        {
            this.layout = layout;
            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false)
            { name = layout.ShadeName, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[shade.width];
            for (int x = 0; x < colors.Length; x++)
                colors[x] = new Color(layout.ShadeColor.r, layout.ShadeColor.g, layout.ShadeColor.b,
                    layout.ShadeColor.a * (1 - Mathf.SmoothStep(0, 1, x / 127f)));
            shade.SetPixels(colors); shade.Apply(false, true);
        }

        public void EnsureStyles()
        {
            if (story != null) return;
            story = Style(layout.TextSize, false);
            layout.Title.Style = Style(layout.Title.FontSize, layout.Title.Emphasized);
            if (layout.Chapter != null)
                layout.Chapter.Style = Style(layout.Chapter.FontSize, layout.Chapter.Emphasized);
        }

        public void Draw(IReadOnlyList<string> lines, string title, float offset, float opacity)
        {
            if (opacity <= 0) return;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(layout.ShadeBounds, shade);
            if (layout.Chapter != null) DrawHeading(layout.Chapter, layout.Chapter.Text, opacity);
            DrawHeading(layout.Title, title, opacity);
            if (layout.DividerBounds.width > 0)
            {
                GUI.color = WithOpacity(layout.DividerColor, opacity);
                GUI.DrawTexture(layout.DividerBounds, Texture2D.whiteTexture);
            }
            GUI.BeginGroup(layout.TextWindow);
            for (int i = 0; i < lines.Count; i++)
            {
                float y = offset + i * layout.LineHeight;
                if (y < -layout.CullAbove || y > layout.TextWindow.height) continue;
                float edge = Mathf.Clamp01(y / 24) * Mathf.Clamp01((layout.TextWindow.height - y - layout.BottomFadeInset) / 24);
                GUI.color = new Color(0, 0, 0, edge * opacity);
                GUI.Label(new Rect(layout.TextShadowOffset.x, y + layout.TextShadowOffset.y,
                    layout.TextWindow.width, 34), lines[i], story);
                GUI.color = WithOpacity(layout.TextColor, edge * opacity);
                GUI.Label(new Rect(0, y, layout.TextWindow.width, 34), lines[i], story);
            }
            GUI.EndGroup();
        }

        private static void DrawHeading(Heading heading, string text, float opacity)
        {
            if (heading.ShadowOffset != Vector2.zero)
            {
                GUI.color = new Color(0, 0, 0, opacity);
                Rect shadow = heading.Bounds;
                shadow.position += heading.ShadowOffset;
                GUI.Label(shadow, text, heading.Style);
            }
            GUI.color = WithOpacity(heading.Color, opacity);
            GUI.Label(heading.Bounds, text, heading.Style);
        }

        private static Color WithOpacity(Color color, float opacity)
            => new Color(color.r, color.g, color.b, color.a * opacity);

        private static GUIStyle Style(int size, bool emphasized)
        {
            GUIStyle style = GameTypography.Style(GUI.skin.label, size, emphasized);
            style.normal.textColor = Color.white;
            style.padding = new RectOffset();
            style.wordWrap = false;
            return style;
        }

        public void Dispose()
        {
            // This generated texture is the panel's only owned Unity object; fonts remain imported assets.
            if (shade == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(shade);
            else UnityEngine.Object.DestroyImmediate(shade);
            shade = null;
        }
    }
}
