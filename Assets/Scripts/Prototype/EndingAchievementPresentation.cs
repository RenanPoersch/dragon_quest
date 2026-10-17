using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Coordinates share the title menu's centered 1280 x 720 canvas.
    public sealed class EndingAchievementPresentation
    {
        public static readonly Rect Bounds = new Rect(910, 248, 326, 300);
        private readonly Texture2D[] icons = new Texture2D[EndingAchievements.Total];
        private GUIStyle counterStyle, nameStyle;
        private static readonly Color Gold = new Color(.96f, .8f, .48f);
        private static readonly Color Muted = new Color(.50f, .53f, .59f);

        public static Rect IconRect(int index) => new Rect(922, 300 + index * 78, 78, 78);
        public static Rect NameRect(int index) => new Rect(1012, 324 + index * 78, 212, 30);

        public void Draw(EndingAchievements progress)
        {
            if (progress.Count == 0) return;
            EnsureStyles();
            Color previous = GUI.color;
            GUI.color = new Color(.055f, .065f, .085f);
            GUI.DrawTexture(Bounds, Texture2D.whiteTexture);
            GUI.color = new Color(.32f, .26f, .15f);
            GUI.DrawTexture(new Rect(Bounds.x, Bounds.y, Bounds.width, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(930, 261, 286, 32), progress.Counter, counterStyle);
            for (int i = 0; i < EndingAchievements.Total; i++)
            {
                var achievement = EndingAchievements.All[i];
                bool unlocked = progress.Has(achievement.Ending);
                GUI.color = unlocked ? Color.white : new Color(.30f, .30f, .30f);
                if (icons[i] != null) GUI.DrawTexture(IconRect(i), icons[i], ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                nameStyle.normal.textColor = unlocked ? Gold : Muted;
                GUI.Label(NameRect(i), achievement.Name, nameStyle);
            }
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (counterStyle != null) return;
            counterStyle = PrototypeTypography.Style(GUI.skin.label, 28, true);
            counterStyle.normal.textColor = Gold;
            counterStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle = PrototypeTypography.Style(GUI.skin.label, 28, true);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = Resources.Load<Texture2D>(EndingAchievements.All[i].IconResource);
                if (icons[i] != null) icons[i].filterMode = FilterMode.Point;
            }
        }
    }
}
