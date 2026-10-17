using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Coordinates share the title menu's centered 1280 x 720 canvas.
    public sealed class EndingAchievementPresentation
    {
        public static readonly Rect Bounds = new Rect(910, 248, 326, 300);
        private readonly Texture2D[] icons = new Texture2D[EndingAchievements.Total];
        private GUIStyle headingStyle, counterStyle, nameStyle, stateStyle, warningStyle;
        private static readonly Color Gold = new Color(.96f, .8f, .48f);
        private static readonly Color Muted = new Color(.50f, .53f, .59f);

        public static Rect IconRect(int index) => new Rect(922, 300 + index * 78, 78, 78);
        public static Rect NameRect(int index) => new Rect(1012, 312 + index * 78, 212, 30);

        public void Draw(EndingAchievements progress, string error = null)
        {
            if (progress.Count == 0 && string.IsNullOrEmpty(error)) return;
            EnsureStyles();
            Color previous = GUI.color;
            GUI.color = new Color(.055f, .065f, .085f);
            GUI.DrawTexture(Bounds, Texture2D.whiteTexture);
            GUI.color = new Color(.32f, .26f, .15f);
            GUI.DrawTexture(new Rect(Bounds.x, Bounds.y, Bounds.width, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(930, 261, 230, 32), "ACHIEVEMENTS", headingStyle);
            GUI.Label(new Rect(1162, 261, 54, 32), progress.Counter, counterStyle);
            for (int i = 0; i < EndingAchievements.Total; i++)
            {
                var achievement = EndingAchievements.All[i];
                bool unlocked = progress.Has(achievement.Ending);
                GUI.color = unlocked ? Color.white : new Color(.30f, .30f, .30f);
                if (icons[i] != null) GUI.DrawTexture(IconRect(i), icons[i], ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                nameStyle.normal.textColor = unlocked ? Gold : Muted;
                stateStyle.normal.textColor = unlocked ? new Color(.68f, .82f, .63f) : Muted;
                GUI.Label(NameRect(i), achievement.Name, nameStyle);
                GUI.Label(new Rect(1012, 342 + i * 78, 212, 24), unlocked ? "Liberado" : "Bloqueado", stateStyle);
            }
            if (!string.IsNullOrEmpty(error))
                GUI.Label(new Rect(910, 554, 326, 60), "Falha ao guardar conquistas. Consulte o log.", warningStyle);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (headingStyle != null) return;
            headingStyle = PrototypeTypography.Style(GUI.skin.label, 24, true);
            headingStyle.normal.textColor = Gold;
            counterStyle = PrototypeTypography.Style(headingStyle, 28, true);
            counterStyle.alignment = TextAnchor.MiddleRight;
            nameStyle = PrototypeTypography.Style(GUI.skin.label, 28, true);
            stateStyle = PrototypeTypography.Style(GUI.skin.label, 20);
            warningStyle = PrototypeTypography.Style(stateStyle, 18);
            warningStyle.wordWrap = true;
            warningStyle.normal.textColor = new Color(.95f, .64f, .49f);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = Resources.Load<Texture2D>(EndingAchievements.All[i].IconResource);
                if (icons[i] != null) icons[i].filterMode = FilterMode.Point;
            }
        }
    }
}
