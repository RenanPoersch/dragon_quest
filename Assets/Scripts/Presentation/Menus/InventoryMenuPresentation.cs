using System;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Coordinates use the same 1280 x 720 canvas as the other game menus.
    public sealed class InventoryMenuPresentation : IDisposable
    {
        public static readonly string[] Commands = { "Item", "Materia", "Equip", "Status", "Magic", "Config", "Save" };
        public static readonly Rect PartyRect = new Rect(32, 72, 1050, 574);
        public static Rect CommandRect(int index) => new Rect(1070, 62 + index * 48, 164, 46);
        private readonly Texture2D gradient, hand;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private GUIStyle name, label, value, cyan, caption, right, condition;
        public GUIStyle CommandStyle { get; private set; }

        public InventoryMenuPresentation()
        {
            gradient = new Texture2D(96, 96, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < gradient.height; y++)
                for (int x = 0; x < gradient.width; x++)
                {
                    float u = x / (float)(gradient.width - 1), v = y / (float)(gradient.height - 1);
                    Color upper = Color.Lerp(new Color(.055f, .055f, .68f), new Color(.015f, .015f, .32f), u);
                    Color lower = Color.Lerp(new Color(.015f, .015f, .31f), new Color(.005f, .005f, .12f), u);
                    gradient.SetPixel(x, y, Color.Lerp(lower, upper, v));
                }
            gradient.Apply();
            hand = Resources.Load<Texture2D>("PixelArt/BattleGloveCursor");
            if (hand == null) return;
            Color32[] pixels = hand.GetPixels32();
            int left = hand.width, bottom = hand.height, right = -1, top = -1;
            for (int y = 0; y < hand.height; y++) for (int x = 0; x < hand.width; x++)
            {
                if (pixels[y * hand.width + x].a <= 16) continue;
                left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y);
                right = Mathf.Max(right, x); top = Mathf.Max(top, y);
            }
            if (right < left) return;
            handUv = new Rect((float)left / hand.width, (float)bottom / hand.height,
                (float)(right - left + 1) / hand.width, (float)(top - bottom + 1) / hand.height);
            handAspect = (float)(right - left + 1) / (top - bottom + 1);
        }

        public void EnsureStyles()
        {
            if (name != null) return;
            name = Style(38); label = Style(32); value = Style(32, true);
            caption = Style(28); right = Style(32, true); right.alignment = TextAnchor.UpperRight;
            cyan = Style(32); cyan.normal.textColor = new Color(.1f, 1, .91f);
            condition = Style(24); condition.normal.textColor = new Color(1, .79f, .27f);
            CommandStyle = Style(38); CommandStyle.alignment = TextAnchor.MiddleLeft;
        }

        private static GUIStyle Style(int size, bool bold = false)
        {
            var style = PrototypeTypography.Style(GUI.skin.label, size, bold);
            style.padding = new RectOffset(0, 0, 0, 0);
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = Color.white;
            style.normal.background = style.hover.background = style.active.background = style.focused.background = null;
            return style;
        }

        public void DrawOverview(PartyProgress party, long seconds, int gold, string location)
        {
            EnsureStyles();
            Panel(PartyRect);
            for (int i = 0; i < party.Members.Count; i++) DrawMember(party.Members[i], 100 + i * 174);
            Panel(new Rect(1034, 40, 214, 376));
            Panel(new Rect(1034, 514, 214, 116));
            Text(new Rect(1052, 530, 72, 34), "Time", label);
            DrawValue(new Rect(1120, 530, 110, 34), FormatTime(seconds), 28);
            Text(new Rect(1052, 576, 72, 34), "Gold", label);
            DrawValue(new Rect(1114, 576, 116, 34), gold.ToString(), 32);
            Panel(new Rect(658, 630, 590, 66));
            var locationRect = new Rect(680, 648, 546, 36);
            int size = caption.fontSize;
            caption.fontSize = Mathf.Min(size, Mathf.FloorToInt(size * locationRect.width /
                Mathf.Max(locationRect.width, caption.CalcSize(new GUIContent(location)).x)));
            Text(locationRect, location, caption); caption.fontSize = size;
        }

        public static string FormatTime(long seconds)
        {
            seconds = Math.Max(0, seconds);
            return (seconds / 3600) + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private void DrawMember(PartyMember member, float y)
        {
            DrawPortrait(new Rect(88, y + 3, 140, 140), member);
            Text(new Rect(278, y, 306, 42), member.Name, name);
            Text(new Rect(278, y + 42, 54, 34), "LV", cyan);
            Text(new Rect(338, y + 42, 74, 34), member.Level.ToString(), value);
            DrawVital(new Rect(278, y + 78, 300, 34), "HP", member.Hp, member.MaxHp, new Color(.08f, .68f, .92f));
            DrawVital(new Rect(278, y + 114, 300, 34), "MP", member.Mp, member.MaxMp, new Color(.15f, .85f, .60f));
            Text(new Rect(656, y + 20, 290, 32), "next level", label);
            int earned = member.Progression.ExperienceInLevel, remaining = member.Progression.ExperienceToNextLevel;
            Gauge(new Rect(656, y + 59, 270, 18), remaining == 0 ? 1 : earned / (float)(earned + remaining),
                new Color(.94f, .78f, .29f));
            Text(new Rect(656, y + 88, 160, 32), "Limit level 1", label);
            Gauge(new Rect(656, y + 127, 270, 18), member.Limit.Charge / 100f, new Color(.92f, .32f, .66f));
            string status = !member.IsAlive ? "KO" : member.IsPoisoned ? "Veneno" : member.BoundActions > 0 ? "Preso" : "";
            if (status.Length > 0) Text(new Rect(418, y + 47, 162, 28), status, condition);
        }

        public void DrawPortrait(Rect rect, PartyMember member)
        {
            Fill(rect, new Color(.83f, .84f, .79f));
            Color previous = GUI.color;
            GUI.color = member.IsAlive ? Color.white : new Color(.5f, .5f, .5f);
            HeroPortraitCatalog.Draw(rect, member.Id);
            GUI.color = previous;
        }

        public void DrawItemMember(Rect rect, PartyMember member)
        {
            EnsureStyles();
            DrawPortrait(new Rect(rect.x, rect.y, 154, 154), member);
            float x = rect.x + 186;
            Text(new Rect(x, rect.y, 310, 42), member.Name, name);
            Text(new Rect(x, rect.y + 42, 58, 34), "LV", cyan);
            Text(new Rect(x + 62, rect.y + 42, 80, 34), member.Level.ToString(), value);
            DrawVital(new Rect(x, rect.y + 78, 310, 34), "HP", member.Hp, member.MaxHp, new Color(.08f, .68f, .92f));
            DrawVital(new Rect(x, rect.y + 114, 310, 34), "MP", member.Mp, member.MaxMp, new Color(.15f, .85f, .60f));
            string status = !member.IsAlive ? "KO" : member.IsPoisoned ? "Veneno" : member.BoundActions > 0 ? "Preso" : "";
            if (status.Length > 0) Text(new Rect(x + 156, rect.y + 47, 150, 28), status, condition);
        }

        private void DrawVital(Rect rect, string title, int current, int maximum, Color color)
        {
            Text(new Rect(rect.x, rect.y, 56, rect.height), title, cyan);
            DrawValue(new Rect(rect.x + 60, rect.y, rect.width - 60, rect.height), current + "/ " + maximum, 32);
            Gauge(new Rect(rect.x + 60, rect.yMax - 2, rect.width - 60, 6), maximum == 0 ? 0 : current / (float)maximum, color);
        }

        private void DrawValue(Rect rect, string content, int size)
        {
            right.fontSize = size;
            float width = right.CalcSize(new GUIContent(content)).x;
            if (width > rect.width) right.fontSize = Mathf.Max(16, Mathf.FloorToInt(size * rect.width / width));
            Text(rect, content, right);
        }

        public void DrawHand(int selected)
            => DrawHand(CommandRect(selected));

        public void DrawHand(Rect rect)
        {
            if (hand == null) { Fill(new Rect(rect.x - 24, rect.center.y - 6, 12, 12), new Color(.1f, 1, .91f)); return; }
            float width = Mathf.Min(58, 34 * handAspect), height = width / handAspect;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x - width - 7, rect.center.y - height / 2, width, height), hand, handUv, true);
        }

        public void Panel(Rect rect)
        {
            Fill(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(0, 0, 0, .8f));
            Fill(rect, new Color(.13f, .14f, .17f));
            Fill(Inset(rect, 2), new Color(.61f, .64f, .66f));
            Fill(Inset(rect, 4), new Color(.94f, .94f, .87f));
            Fill(Inset(rect, 6), new Color(.29f, .3f, .34f));
            GUI.DrawTexture(Inset(rect, 8), gradient);
        }

        private static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount,
            rect.width - 2 * amount, rect.height - 2 * amount);
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void Text(Rect rect, string content, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), content, style);
            GUI.color = previous; GUI.Label(rect, content, style);
        }
        private static void Gauge(Rect rect, float fraction, Color color)
        {
            Fill(rect, new Color(.75f, .76f, .73f));
            Rect inside = Inset(rect, rect.height <= 8 ? 1 : 3); Fill(inside, new Color(.16f, .16f, .23f));
            inside.width *= Mathf.Clamp01(fraction); Fill(inside, color);
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(gradient);
        }
    }
}
