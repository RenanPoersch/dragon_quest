using System;
using System.Collections.Generic;
using DragonQuest.Interactions;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Every conversation, including choices and narrated discoveries, uses this layout.
    public sealed class DialoguePresentation : IDisposable
    {
        public const float ReferencePanelHeight = 180f;
        public const float PortraitOverhang = .20f;
        public const float PortraitBottomInset = 8f;

        private readonly Texture2D gradient;
        private readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        private const float ChoiceTextInset = 48;
        private Texture2D hand;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private GUIStyle title, body, choice, notice;
        private Vector2 lastPointer;
        private bool hasPointer;
        private int lastSelectedChoice = -1;
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
            return new Rect(panel.x + 8, panel.yMax - PortraitBottomInset - size, size, size);
        }

        public void Draw(DialogueController dialogue, float width, float height)
        {
            EnsureStyles();
            if (dialogue.IsNotice)
            {
                float panelWidth = Mathf.Min(480, width - 32);
                float panelHeight = Mathf.Min(height - 32,
                    Mathf.Max(104, notice.CalcHeight(new GUIContent(dialogue.Text), panelWidth - 40) + 40));
                var noticePanel = new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
                Panel(noticePanel);
                ShadowLabel(Inset(noticePanel, 20), dialogue.Text, notice);
                return;
            }
            if (lastContentVersion != dialogue.ContentVersion)
            {
                scroll = Vector2.zero;
                lastContentVersion = dialogue.ContentVersion;
                lastVisibleLength = -1;
                lastSelectedChoice = -1;
                hasPointer = false;
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
            Rect selectedRow = default;
            if (dialogue.ChoicesReady)
            {
                contentHeight = choicesTop;
                for (int i = 0; i < options.Count; i++)
                {
                    if (i > 0) contentHeight += 4;
                    float rowHeight = ChoiceHeight(options[i], contentWidth);
                    if (i == dialogue.SelectedChoice) selectedRow = new Rect(0, contentHeight, contentWidth, rowHeight);
                    contentHeight += rowHeight;
                }
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
            if (dialogue.ChoicesReady && lastSelectedChoice != dialogue.SelectedChoice)
            {
                if (selectedRow.y < scroll.y) scroll.y = selectedRow.y;
                else if (selectedRow.yMax > scroll.y + viewport.height) scroll.y = selectedRow.yMax - viewport.height;
                scroll.y = Mathf.Clamp(scroll.y, 0, Mathf.Max(0, contentHeight - viewport.height));
                lastSelectedChoice = dialogue.SelectedChoice;
            }
            if (Event.current.type == EventType.Repaint && GameInput.IsFocused && Gamepad.current != null)
                scroll.y = Mathf.Clamp(scroll.y - Gamepad.current.rightStick.ReadValue().y * 260 * Time.unscaledDeltaTime,
                    0, Mathf.Max(0, contentHeight - viewport.height));
            // An idle mouse must not pull selection away from arrow-key navigation.
            Vector2 pointer = Event.current.mousePosition;
            bool pointerMoved = Event.current.type == EventType.Repaint && hasPointer
                && (pointer - lastPointer).sqrMagnitude > .01f;
            if (Event.current.type == EventType.Repaint) { lastPointer = pointer; hasPointer = true; }
            bool pointerInViewport = viewport.Contains(pointer);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, contentHeight), false, false);
            try
            {
                ShadowLabel(new Rect(0, 0, contentWidth, bodyHeight), visibleText, body);
                float y = choicesTop;
                if (dialogue.ChoicesReady)
                {
                    if (pointerMoved && pointerInViewport)
                    {
                        float hoverY = choicesTop;
                        for (int i = 0; i < options.Count; i++)
                        {
                            float hoverHeight = ChoiceHeight(options[i], contentWidth);
                            if (new Rect(0, hoverY, contentWidth, hoverHeight).Contains(Event.current.mousePosition))
                            { dialogue.SelectChoice(i); break; }
                            hoverY += hoverHeight + 4;
                        }
                    }
                    // Callbacks may replace or close the conversation. Finish this GUI pass immediately.
                    for (int i = 0; i < options.Count; i++)
                    {
                        float optionHeight = ChoiceHeight(options[i], contentWidth);
                        Rect row = new Rect(0, y, contentWidth, optionHeight);

                        bool selected = i == dialogue.SelectedChoice;
                        Color previous = GUI.color;
                        if (selected) GUI.color = new Color(1, .94f, .66f);
                        ShadowLabel(new Rect(ChoiceTextInset, y, Mathf.Max(20, contentWidth - ChoiceTextInset), optionHeight), options[i], choice);
                        GUI.color = previous;
                        if (selected) DrawChoiceHand(row);
                        // Invisible hit area preserves clicking without a button background or border.
                        if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { dialogue.Choose(i); break; }
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

        private float ChoiceHeight(string text, float width)
            => Mathf.Max(32, choice.CalcHeight(new GUIContent(text), Mathf.Max(20, width - ChoiceTextInset)));

        private void LoadChoiceHand()
        {
            hand = Resources.Load<Texture2D>("PixelArt/BattleGloveCursor");
            if (hand == null) throw new InvalidOperationException("Cursor de conversa ausente: BattleGloveCursor");
            // Use the existing menu artwork; transparent margins are trimmed only by drawing UVs.
            Color32[] pixels = hand.GetPixels32();
            int left = hand.width, right = -1, bottom = hand.height, top = -1;
            for (int y = 0; y < hand.height; y++) for (int x = 0; x < hand.width; x++)
            {
                if (pixels[y * hand.width + x].a <= 16) continue;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
            if (right < left || top < bottom) return;
            int width = right - left + 1, height = top - bottom + 1;
            handUv = new Rect((float)left / hand.width, (float)bottom / hand.height,
                (float)width / hand.width, (float)height / hand.height);
            handAspect = (float)width / height;
        }

        private void DrawChoiceHand(Rect row)
        {
            float width = Mathf.Min(36, 26 * handAspect), height = width / handAspect;
            GUI.DrawTextureWithTexCoords(new Rect(0, row.center.y - height / 2, width, height), hand, handUv, true);
        }

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
            notice = PrototypeTypography.Style(GUI.skin.label, 28);
            notice.alignment = TextAnchor.MiddleCenter;
            foreach (GUIStyle style in new[] { title, body, choice, notice })
            {
                style.wordWrap = true;
                style.padding = new RectOffset(0, 0, 0, 0);
                style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
            }

            choice.padding = new RectOffset(0, 0, 3, 3);
            choice.alignment = TextAnchor.MiddleLeft;
            choice.hover.textColor = choice.active.textColor = new Color(1, .94f, .66f);
            LoadChoiceHand();
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
