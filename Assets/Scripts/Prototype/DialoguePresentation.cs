using System;
using System.Collections.Generic;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Every conversation, including choices and narrated discoveries, uses this layout.
    public sealed class DialoguePresentation : IDisposable
    {
        public const float ReferencePanelHeight = 180f;
        public const float PortraitOverhang = .20f;

        private readonly Texture2D gradient;
        private readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        private GUIStyle title, body, choice;
        private float followLine, measuredWidth = -1;
        private Vector2 scroll;
        private int lastContentVersion = -1, lastVisibleLength = -1;

        public DialoguePresentation()
        {
            // Same palette and silver frame as the Load / New game panels.
            gradient = new Texture2D(128, 64, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < gradient.height; y++) for (int x = 0; x < gradient.width; x++)
            {
                float u = x / (float)(gradient.width - 1), v = y / (float)(gradient.height - 1);
                Color top = Color.Lerp(new Color(.025f, .23f, .62f), new Color(.015f, .015f, .27f), u);
                Color bottom = Color.Lerp(new Color(.01f, .035f, .42f), new Color(.005f, .005f, .15f), u);
                gradient.SetPixel(x, y, Color.Lerp(bottom, top, v));
            }
            gradient.Apply();
        }

        public static Rect PanelBounds(float width, float height)
        {
            float panelHeight = Mathf.Min(ReferencePanelHeight, height - 32);
            return new Rect(16, height - 16 - panelHeight, width - 32, panelHeight);
        }

        public static Rect PortraitBounds(float width, float height)
        {
            Rect panel = PanelBounds(width, height);
            float size = Mathf.Clamp(width * .30f, 96, panel.height / (1 - PortraitOverhang));
            return new Rect(panel.x + 20, panel.yMax - size, size, size);
        }

        public void Draw(DialogueController dialogue, float width, float height)
        {
            EnsureStyles();
            if (lastContentVersion != dialogue.ContentVersion)
            {
                scroll = Vector2.zero;
                lastContentVersion = dialogue.ContentVersion;
                lastVisibleLength = -1;
            }
            Rect panel = PanelBounds(width, height);
            Rect face = PortraitBounds(width, height);
            float left = face.xMax + 20;
            float textWidth = Mathf.Max(80, width - left - 36);
            // Reserve scrollbar width before measuring, so wrapping stays stable when it appears.
            float contentWidth = textWidth - 20;
            float titleHeight = title.CalcHeight(new GUIContent(dialogue.Title), contentWidth);
            string visibleText = dialogue.VisibleText;
            float bodyHeight = Mathf.Max(body.lineHeight, body.CalcHeight(new GUIContent(visibleText), contentWidth));
            float choicesTop = bodyHeight + 12;
            float contentHeight = bodyHeight;
            var options = dialogue.Choices;
            if (dialogue.ChoicesReady)
            {
                contentHeight = choicesTop;
                for (int i = 0; i < options.Count; i++)
                    contentHeight += ChoiceHeight(options[i], i, contentWidth) + (i > 0 ? 4 : 0);
            }
            Panel(panel);
            DrawPortrait(face, dialogue.Title);
            // The name stays in place while only the speech and responses scroll.
            ShadowLabel(new Rect(left, panel.y + 16, contentWidth, titleHeight), dialogue.Title, title);
            float textTop = panel.y + 16 + titleHeight + 8;
            Rect viewport = new Rect(left, textTop, textWidth, Mathf.Max(24, panel.yMax - 16 - textTop));
            bool textChanged = lastVisibleLength != visibleText.Length;
            if (textChanged || !Mathf.Approximately(measuredWidth, contentWidth))
            {
                followLine = dialogue.ChoicesReady ? choicesTop
                    : LineStart(new GUIContent(visibleText), contentWidth, bodyHeight, bodyHeight - viewport.height);
                measuredWidth = contentWidth;
            }
            if (!dialogue.ChoicesReady) contentHeight = Mathf.Max(bodyHeight, followLine + viewport.height);
            if (textChanged)
            {
                // Follow the newly written line. Once writing ends, wheel/drag scrolling is free.
                scroll.y = Mathf.Clamp(followLine, 0, Mathf.Max(0, contentHeight - viewport.height));
                lastVisibleLength = visibleText.Length;
            }
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, contentHeight), false, false);
            try
            {
                ShadowLabel(new Rect(0, 0, contentWidth, bodyHeight), visibleText, body);
                float y = choicesTop;
                if (dialogue.ChoicesReady)
                {
                    // Callbacks may replace or close the conversation. Finish this GUI pass immediately.
                    for (int i = 0; i < options.Count; i++)
                    {
                        float optionHeight = ChoiceHeight(options[i], i, contentWidth);
                        Rect button = new Rect(0, y, contentWidth, optionHeight);
                        Fill(button, new Color(.52f, .60f, .79f));
                        Fill(Inset(button, 1), new Color(.035f, .09f, .30f));
                        if (GUI.Button(button, (i + 1) + ". " + options[i], choice)) { dialogue.Choose(i); break; }
                        y += optionHeight + 4;
                    }
                }
            }
            finally { GUI.EndScrollView(); }
        }

        private float LineStart(GUIContent text, float width, float height, float minimum)
        {
            if (minimum <= 0) return 0;
            var area = new Rect(0, 0, width, height);
            int left = 0, right = text.text.Length;
            // Use actual wrapped-line positions, including paragraphs and fractional font metrics.
            while (left < right)
            {
                int middle = left + (right - left) / 2;
                if (body.GetCursorPixelPosition(area, text, middle).y < minimum) left = middle + 1;
                else right = middle;
            }
            return Mathf.Max(0, body.GetCursorPixelPosition(area, text, left).y);
        }

        private float ChoiceHeight(string text, int index, float width)
            => Mathf.Max(32, choice.CalcHeight(new GUIContent((index + 1) + ". " + text), width));

        private void DrawPortrait(Rect area, string speaker)
        {
            string resource = DialoguePortraitCatalog.ResourceFor(speaker);
            if (!portraits.TryGetValue(resource, out var portrait))
            {
                portrait = Resources.Load<Texture2D>(resource);
                if (portrait == null) throw new InvalidOperationException("Retrato de conversa ausente: " + resource);
                portraits.Add(resource, portrait);
            }
            float aspect = portrait.width / (float)portrait.height;
            float imageHeight = Mathf.Min(area.height, area.width / aspect);
            float imageWidth = imageHeight * aspect;
            var image = new Rect(area.center.x - imageWidth / 2, area.yMax - imageHeight, imageWidth, imageHeight);
            GUI.DrawTexture(image, portrait, ScaleMode.StretchToFill, true);
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            body = PrototypeTypography.Style(GUI.skin.label, 24);
            choice = PrototypeTypography.Style(GUI.skin.label, 22);
            foreach (GUIStyle style in new[] { title, body, choice })
            {
                style.wordWrap = true;
                style.padding = new RectOffset(0, 0, 0, 0);
                style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
            }

            choice.padding = new RectOffset(10, 10, 3, 3);
            choice.alignment = TextAnchor.MiddleLeft;
            choice.hover.textColor = choice.active.textColor = new Color(1, .94f, .66f);
        }

        private void Panel(Rect area)
        {
            Fill(area, new Color(.15f, .16f, .18f));
            Fill(Inset(area, 2), new Color(.62f, .65f, .65f));
            Fill(Inset(area, 4), new Color(.92f, .93f, .86f));
            Fill(Inset(area, 6), new Color(.29f, .31f, .32f));
            GUI.DrawTexture(Inset(area, 8), gradient);
        }
        private static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount,
            rect.width - amount * 2, rect.height - amount * 2);
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.05f, .06f, .13f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
            GUI.color = previous; GUI.Label(rect, text, style);
        }
        public void Dispose()
        {
            portraits.Clear();
            UnityEngine.Object.Destroy(gradient);
        }
    }
}
