using System;
using DragonQuest.Audio;
using DragonQuest.Input;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Presentation.Battle
{
    // Shared by arena and panels; only resources created by this view are destroyed here.
    public sealed class BattleHudGraphics : IDisposable
    {
        private Texture2D pixel, hand;
        private GUIStyle title, text, small, centered, button, itemButton, number, castNumber;
        private GUISkin battleSkin;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        public BattleStatusIcons StatusIcons { get; } = new BattleStatusIcons();
        public GUISkin Skin => battleSkin;
        public GUIStyle Title => title;
        public GUIStyle Text => text;
        public GUIStyle Small => small;
        public GUIStyle Centered => centered;
        public GUIStyle Number => number;
        public GUIStyle CastNumber => castNumber;

        public void DrawBar(Rect rect, int value, int maximum, Color color)
        {
            RectBox(rect, new Color(.015f, .035f, .14f));
            RectBox(new Rect(rect.x, rect.y, rect.width * (maximum == 0 ? 0 : Mathf.Clamp01((float)value / maximum)), rect.height), color);
            GUI.Label(rect, value + "/" + maximum, centered);
        }

        public bool CommandButton(Rect rect, string label, bool focused, bool enabled, bool canAcceptMenuInput, Texture2D icon = null, bool limitReady = false)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled && canAcceptMenuInput;
            bool clicked = LimitCommandGlow.Button(rect, label,
                limitReady,
                icon == null ? button : itemButton);
            InventoryPixelArt.Draw(new Rect(rect.x + 50, rect.center.y - 14, 28, 28), icon);
            GUI.enabled = previous;
            if (focused) DrawHand(new Rect(rect.x + 2, rect.center.y - 15, 44, 30));
            if (clicked) { MenuSounds.Play(MenuSound.Confirm); GameInput.ConsumeGamepadFrame(); }
            return clicked;
        }

        public void DrawTurnArrow(float x, float y)
        {
            RectBox(new Rect(x - 7, y, 14, 12), Color.white);
            for (int row = 0; row < 7; row++) RectBox(new Rect(x - 13 + row * 2, y + 12 + row * 2, 26 - row * 4, 2), Color.white);
        }

        public void Panel(Rect rect)
        {
            RectBox(rect, new Color(.06f, .07f, .18f));
            RectBox(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(.82f, .86f, .97f));
            Rect inner = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);
            for (int row = 0; row < 24; row++) RectBox(new Rect(inner.x, inner.y + inner.height * row / 24, inner.width, inner.height / 24 + 1),
                Color.Lerp(new Color(.09f, .20f, .58f), new Color(.015f, .04f, .23f), row / 23f));
        }

        public void RectBox(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }

        public void EnsureCreated()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            battleSkin = UnityEngine.Object.Instantiate(GUI.skin);
            battleSkin.verticalScrollbar = new GUIStyle { fixedWidth = 12, stretchHeight = true };
            battleSkin.verticalScrollbarUpButton = new GUIStyle { fixedWidth = 12 };
            battleSkin.verticalScrollbarDownButton = new GUIStyle { fixedWidth = 12 };
            battleSkin.verticalScrollbarThumb = new GUIStyle { fixedWidth = 12, stretchHeight = true };
            foreach (GUIStyleState state in new[] { battleSkin.verticalScrollbarThumb.normal,
                battleSkin.verticalScrollbarThumb.hover, battleSkin.verticalScrollbarThumb.active,
                battleSkin.verticalScrollbarThumb.focused, battleSkin.verticalScrollbarThumb.onNormal,
                battleSkin.verticalScrollbarThumb.onHover, battleSkin.verticalScrollbarThumb.onActive,
                battleSkin.verticalScrollbarThumb.onFocused }) state.background = pixel;
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            text = PrototypeTypography.Style(GUI.skin.label, 24); text.wordWrap = true;
            small = PrototypeTypography.Style(GUI.skin.label, 16); small.wordWrap = true;
            centered = PrototypeTypography.Style(GUI.skin.label, 24); centered.alignment = TextAnchor.MiddleCenter;
            number = PrototypeTypography.Style(centered, 40, true);
            castNumber = PrototypeTypography.Style(number, number.fontSize + CastleMeteorPresentation.TurnNumberFontIncrease, true);
            button = PrototypeTypography.Style(GUI.skin.label, 24);
            button.alignment = TextAnchor.MiddleLeft; button.padding = new RectOffset(50, 4, 2, 2);
            itemButton = InventoryPixelArt.ButtonStyle(button, 86);
            foreach (GUIStyle style in new[] { title, text, small, centered, button, itemButton })
            { style.normal.textColor = Color.white; style.hover.textColor = Color.white; style.active.textColor = Color.white; }
            LoadHandSprite();
        }

        private void LoadHandSprite()
        {
            hand = Resources.Load<Texture2D>("PixelArt/BattleGloveCursor");
            if (hand == null) return;
            // Keep the original PNG intact; trim transparent margins only through drawing UVs.
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

        public void DrawHand(Rect area)
        {
            if (hand == null) return;
            float width = Mathf.Min(area.width, area.height * handAspect);
            float height = width / handAspect;
            GUI.DrawTextureWithTexCoords(new Rect(area.x, area.center.y - height / 2, width, height), hand, handUv, true);
        }

        public void Dispose()
        {
            StatusIcons.Dispose();
            if (battleSkin != null) UnityEngine.Object.Destroy(battleSkin);
            if (pixel != null) UnityEngine.Object.Destroy(pixel);
        }
    }
}
