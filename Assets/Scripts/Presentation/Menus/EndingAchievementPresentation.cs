using DragonQuest.Presentation.Common;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    // A single temporary notification on the title menu's 1280 x 720 canvas.
    public sealed class EndingAchievementPresentation
    {
        public const float Duration = 6f;
        private const float FadeOutDuration = .5f;
        public static readonly Rect Bounds = new Rect(910, 580, 326, 104);
        public static readonly Rect IconBounds = new Rect(922, 593, 78, 78);
        public static readonly Rect NameBounds = new Rect(1012, 596, 212, 32);
        public static readonly Rect CounterBounds = new Rect(1012, 636, 212, 30);
        private EndingAchievement achievement;
        private Texture2D icon;
        private string counter;
        private float elapsed;
        private bool hasDrawn;
        private GUIStyle counterStyle, nameStyle;
        public bool IsVisible => achievement != null;
        public EndingAchievement Achievement => achievement;

        public void Show(EndingAchievement unlocked, string total)
        {
            achievement = unlocked;
            counter = total;
            elapsed = 0;
            hasDrawn = false;
            icon = unlocked == null ? null : Resources.Load<Texture2D>(unlocked.IconResource);
            if (icon != null) icon.filterMode = FilterMode.Point;
        }

        public bool Update(float unscaledDeltaTime)
        {
            if (!IsVisible || !hasDrawn) return false;
            elapsed += Mathf.Max(0, unscaledDeltaTime);
            if (elapsed < Duration) return false;
            Dismiss();
            return true;
        }

        public void Dismiss()
        { achievement = null; icon = null; }

        public void Draw()
        {
            if (!IsVisible) return;
            EnsureStyles();
            Color previous = GUI.color;
            float alpha = Mathf.Clamp01((Duration - elapsed) / FadeOutDuration) * previous.a;
            GUI.color = new Color(.055f, .065f, .085f, .96f * alpha);
            GUI.DrawTexture(Bounds, Texture2D.whiteTexture);
            GUI.color = new Color(.32f, .26f, .15f, alpha);
            GUI.DrawTexture(new Rect(Bounds.x, Bounds.y, Bounds.width, 2), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, alpha);
            if (icon != null) GUI.DrawTexture(IconBounds, icon, ScaleMode.ScaleToFit, true);
            GUI.Label(NameBounds, achievement.Name, nameStyle);
            GUI.Label(CounterBounds, counter, counterStyle);
            GUI.color = previous;
            if (Event.current.type == EventType.Repaint) hasDrawn = true;
        }

        private void EnsureStyles()
        {
            if (counterStyle != null) return;
            counterStyle = GameTypography.Style(GUI.skin.label, 24, true);
            counterStyle.normal.textColor = Color.white;
            counterStyle.alignment = TextAnchor.MiddleLeft;
            nameStyle = GameTypography.Style(GUI.skin.label, 28, true);
            nameStyle.normal.textColor = new Color(.96f, .8f, .48f);
        }
    }
}
