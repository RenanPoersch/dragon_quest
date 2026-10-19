using System;
using UnityEngine;
using DragonQuest.Exploration;
using DragonQuest.Persistence;

namespace DragonQuest.Prototype
{
    public sealed class SaveSelectionPresentation : IDisposable
    {
        public static readonly Rect BackRect = new Rect(1140, 8, 132, 56);
        public static Rect SlotRect(int index) => new Rect(0, 80 + index * 212, 1280, 212);
        private readonly Texture2D pixel, gradient;
        private GUIStyle heading, text, number, right, level, empty, note;
        public GUIStyle BackStyle { get; private set; }
        private int lastSlot;

        public SaveSelectionPresentation()
        {
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            gradient = new Texture2D(128, 64, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < gradient.height; y++) for (int x = 0; x < gradient.width; x++)
            {
                float u = x / (float)(gradient.width - 1), v = y / (float)(gradient.height - 1);
                Color top = Color.Lerp(new Color(.025f, .23f, .62f), new Color(.015f, .015f, .27f), u);
                Color bottom = Color.Lerp(new Color(.01f, .035f, .42f), new Color(.005f, .005f, .15f), u);
                gradient.SetPixel(x, y, Color.Lerp(bottom, top, v));
            }
            gradient.Apply();
        }

        private void EnsureStyles()
        {
            if (heading != null) return;
            heading = PrototypeTypography.Style(GUI.skin.label, 48);
            text = PrototypeTypography.Style(GUI.skin.label, 44);
            number = PrototypeTypography.Style(text, 44, true);
            right = new GUIStyle(number) { alignment = TextAnchor.UpperRight };
            level = new GUIStyle(text); level.normal.textColor = new Color(0, .95f, .88f);
            empty = new GUIStyle(text); empty.normal.textColor = new Color(1, .97f, .08f);
            note = PrototypeTypography.Style(GUI.skin.label, 24); note.wordWrap = true;
            BackStyle = PrototypeTypography.Style(GUI.skin.label, 28); BackStyle.alignment = TextAnchor.MiddleCenter;
            foreach (GUIStyle style in new[] { heading, text, number, right, note, BackStyle })
            {
                style.normal.textColor = Color.white; style.hover.textColor = Color.white;
                style.active.textColor = Color.white; style.padding = new RectOffset(0, 0, 0, 0);
            }
            level.padding = empty.padding = new RectOffset(0, 0, 0, 0);
        }

        public void DrawHeader(bool loading, int selected)
        {
            EnsureStyles();
            if (selected >= 0 && selected < SaveStore.SlotCount) lastSlot = selected;
            Panel(new Rect(0, 0, 674, 72)); Panel(new Rect(674, 0, 286, 72)); Panel(new Rect(960, 0, 320, 72));
            ShadowLabel(new Rect(16, 10, 650, 52), loading ? "Select a save game." : "Select a save slot.", heading);
            ShadowLabel(new Rect(690, 10, 140, 52), "GAME", empty);
            ShadowLabel(new Rect(830, 10, 120, 52), (lastSlot + 1).ToString("00"), heading);
            ShadowLabel(new Rect(988, 10, 146, 52), loading ? "Load" : "Save", heading);
        }

        public void DrawSlot(int index, RestoredJourney journey, bool occupied, string warning, string location)
        {
            EnsureStyles();
            Rect row = SlotRect(index); Panel(row);
            if (journey == null)
            {
                ShadowLabel(new Rect(158, row.y + 88, 650, 58), occupied ? "UNAVAILABLE" : "EMPTY", empty);
                if (occupied) ShadowLabel(new Rect(690, row.y + 112, 560, 72), warning, note);
                return;
            }
            for (int member = 0; member < journey.Party.Members.Count; member++)
            {
                string id = journey.Party.Members[member].Id;
                Rect face = new Rect(68 + member * 176, row.y + 16, 164, 152);
                Fill(face, new Color(.87f, .86f, .80f));
                HeroPortraitCatalog.Draw(face, id);
            }
            var leader = journey.Party.Members[0];
            ShadowLabel(new Rect(690, row.y + 14, 260, 50), leader.Name, text);
            ShadowLabel(new Rect(704, row.y + 74, 150, 50), "Level", level);
            ShadowLabel(new Rect(854, row.y + 74, 100, 50), leader.Level.ToString(), number);
            Panel(new Rect(964, row.y + 10, 316, 118));
            ShadowLabel(new Rect(982, row.y + 18, 120, 48), "Time", text);
            DrawValue(new Rect(1100, row.y + 18, 170, 48), FormatTime(journey.Data.PlaySeconds));
            ShadowLabel(new Rect(982, row.y + 66, 120, 48), "Gold", text);
            DrawValue(new Rect(1100, row.y + 66, 170, 48), journey.Progress.Gold.ToString());
            Panel(new Rect(686, row.y + 132, 594, 80));
            ShadowLabel(new Rect(706, row.y + 146, 550, 50), location, text);
        }

        public static string FormatTime(long? seconds) => seconds.HasValue
            ? (seconds.Value / 3600).ToString("00") + ":" + (seconds.Value / 60 % 60).ToString("00") : "--:--";

        private void DrawValue(Rect rect, string value)
        {
            right.fontSize = 44;
            float width = right.CalcSize(new GUIContent(value)).x;
            if (width > rect.width) right.fontSize = Mathf.Max(16, Mathf.FloorToInt(44 * rect.width / width));
            ShadowLabel(rect, value, right);
        }

        public void Panel(Rect area)
        {
            Fill(area, new Color(.15f, .16f, .18f));
            Fill(Inset(area, 2), new Color(.62f, .65f, .65f));
            Fill(Inset(area, 4), new Color(.92f, .93f, .86f));
            Fill(Inset(area, 6), new Color(.29f, .31f, .32f));
            GUI.DrawTexture(Inset(area, 8), gradient);
        }
        private static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount,
            rect.width - amount * 2, rect.height - amount * 2);
        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }
        private static void ShadowLabel(Rect rect, string value, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.05f, .06f, .13f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style);
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(pixel); UnityEngine.Object.Destroy(gradient);
        }
    }
}
