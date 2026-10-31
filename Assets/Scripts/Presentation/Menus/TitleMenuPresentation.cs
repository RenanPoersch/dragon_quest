using DragonQuest.Infrastructure;
using DragonQuest.Presentation.Common;
using System;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public sealed class TitleMenuPresentation
    {
        private const float MenuOffsetY = 50f / 1.5f;
        private Texture2D scaleSprite;
        private GUIStyle buttonStyle;
        public GUIStyle ButtonStyle { get { EnsureStyles(); return buttonStyle; } }

        public static Rect OptionRect(int option)
            => new Rect(490, (option == 0 ? 562 : 620) - MenuOffsetY * 2, 300, 48);

        public void Draw(bool canContinue, Func<Rect, string, int, GUIStyle, bool> button,
            Action newGame, Action continueGame)
        {
            EnsureStyles();
            if (scaleSprite != null)
                GUI.DrawTexture(new Rect(540, 250, 200, 200), scaleSprite, ScaleMode.ScaleToFit, true);
            if (button(OptionRect(0), "New game", 0, buttonStyle)) newGame();
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = previousEnabled && canContinue;
                if (button(OptionRect(1), "Continue", 1, buttonStyle)) continueGame();
            }
            finally { GUI.enabled = previousEnabled; }
        }

        private void EnsureStyles()
        {
            if (buttonStyle != null) return;
            scaleSprite = GameAssets.Optional<Texture2D>("PixelArt/DragonScale");
            buttonStyle = GameTypography.Style(GUI.skin.label, 36, true);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = new Color(0.96f, 0.8f, 0.48f);
            buttonStyle.active.textColor = new Color(0.96f, 0.8f, 0.48f);
            buttonStyle.focused.textColor = Color.white;
        }
    }
}
