using DragonQuest.Progression;
using DragonQuest.Presentation.Common;
using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Presentation.Menus
{
    // A read-only character sheet, using the same values and commands as combat.
    public sealed class StatusMenuPresentation
    {
        public static readonly Rect BodyRect = new Rect(4, 38, 1272, 654);
        private GUIStyle text, cyan, number, right, condition;
        private Vector2 commandsScroll;

        public void Reset() { commandsScroll = Vector2.zero; }

        private void EnsureStyles()
        {
            if (text != null) return;
            text = GameTypography.Style(GUI.skin.label, 34);
            text.padding = new RectOffset(0, 0, 0, 0);
            text.normal.textColor = Color.white;
            cyan = new GUIStyle(text); cyan.normal.textColor = new Color(.05f, 1, .91f);
            number = GameTypography.Style(text, 34, true);
            right = new GUIStyle(number) { alignment = TextAnchor.UpperRight };
            condition = GameTypography.Style(text, 28); condition.normal.textColor = new Color(1, .4f, .72f);
        }

        public void Draw(InventoryMenuPresentation theme, PartyMember member, bool canInteract, Action back)
        {
            EnsureStyles(); theme.Panel(BodyRect);
            theme.DrawPortrait(new Rect(34, 54, 168, 146), member);
            Label(new Rect(218, 62, 280, 40), member.Name, text);
            Label(new Rect(218, 102, 56, 34), "LV", cyan);
            Value(new Rect(278, 102, 58, 34), member.Level.ToString());
            string status = !member.IsAlive ? "KO" : member.IsPoisoned && member.BoundActions > 0 ? "Veneno / Preso"
                : member.IsPoisoned ? "Veneno" : member.BoundActions > 0 ? "Preso" : "Normal";
            FitLabel(new Rect(350, 106, 144, 32), status, condition);
            Vital(new Rect(218, 136, 276, 30), "HP", member.Hp, member.MaxHp, new Color(.1f, .7f, .95f));
            Vital(new Rect(218, 170, 276, 30), "MP", member.Mp, member.MaxMp, new Color(.13f, .9f, .6f));

            Label(new Rect(516, 56, 180, 40), "EXP:", text);
            Value(new Rect(702, 56, 244, 40), member.Experience + "p");
            int earned = member.Progression.ExperienceInLevel, remaining = member.Progression.ExperienceToNextLevel;
            Gauge(new Rect(718, 104, 228, 20), remaining == 0 ? 1 : earned / (float)(earned + remaining), new Color(.89f, .42f, .5f));
            Label(new Rect(516, 136, 242, 40), "next level:", text);
            Value(new Rect(1048, 136, 166, 40), remaining + "p");
            Label(new Rect(516, 170, 332, 40), "Limit level 1", text);
            Gauge(new Rect(884, 176, 214, 20), member.Limit.Charge / 100f, new Color(.94f, .31f, .57f));
            theme.Panel(new Rect(976, 38, 300, 68));
            Label(new Rect(1028, 54, 220, 48), "Status", text);
            if (Click(new Rect(976, 38, 300, 68), canInteract)) back();

            string[] baseLabels = { "Base attack", "Speed", "Base defense", "Base magic", "Critical", "Class" };
            string[] baseValues = { member.AttackWith(null, null, null).ToString(), member.Speed.ToString(),
                member.DefenseWith(null, null, null).ToString(), member.MagicWith(null, null, null).ToString(),
                (member.CriticalChance * 100).ToString("0.#") + "%", member.BaseStats.Role };
            for (int i = 0; i < baseLabels.Length; i++) Stat(218 + i * 32, baseLabels[i], baseValues[i]);
            string[] finalLabels = { "Attack", "Defense", "Magic atk", "Magic def", "Max HP", "Max MP" };
            string[] finalValues = { member.Attack.ToString(), member.Defense.ToString(), member.Magic.ToString(),
                member.Defense.ToString(), member.MaxHp.ToString(), member.MaxMp.ToString() };
            for (int i = 0; i < finalLabels.Length; i++) Stat(446 + i * 36, finalLabels[i], finalValues[i]);

            DrawCommands(theme, member);
            EquipmentRow(442, "Wpn:", member.Weapon);
            EquipmentRow(538, "Arm:", member.Armor);
            Label(new Rect(644, 620, 132, 40), "Acc:", cyan);
            InventoryPixelArt.Draw(new Rect(774, 620, 40, 40), InventoryPixelArt.Texture(member.Accessory?.Definition));
            FitLabel(new Rect(824, 620, 386, 40), member.Accessory?.Definition.Name ?? "--", text);
        }

        private void Stat(float y, string title, string value)
        {
            Label(new Rect(120, y, 290, 40), title, cyan);
            Value(new Rect(408, y, 100, 40), value);
        }

        private void DrawCommands(InventoryMenuPresentation theme, PartyMember member)
        {
            theme.Panel(new Rect(624, 230, 364, 168));
            var commands = BattleCommandMenu.Root(member.CreateCombatant());
            float height = Mathf.Max(134, Mathf.CeilToInt(commands.Count / 2f) * 34);
            commandsScroll = GUI.BeginScrollView(new Rect(642, 246, 328, 134), commandsScroll, new Rect(0, 0, 308, height));
            for (int i = 0; i < commands.Count; i++)
                FitLabel(new Rect(i % 2 * 154, i / 2 * 34, 150, 34), commands[i].Label, text);
            GUI.EndScrollView();
        }

        private void EquipmentRow(float y, string title, EquipmentLoadout equipment)
        {
            Label(new Rect(644, y, 132, 40), title, cyan);
            FitLabel(new Rect(784, y, 428, 40), equipment?.Definition.Name ?? "--", text);
            if (equipment == null) return;
            for (int i = 0; i < equipment.Sockets.Count; i++)
            {
                Rect socket = new Rect(768 + i * 54, y + 44, 40, 40);
                int pair = equipment.LinkedSocket(i);
                if (pair < 0 || pair >= equipment.Sockets.Count) MateriaSocketArt.DrawSingle(socket);
                else if (pair > i) MateriaSocketArt.DrawLinked(socket, new Rect(768 + pair * 54, y + 44, 40, 40));
            }
            for (int i = 0; i < equipment.Sockets.Count; i++)
                MateriaSocketArt.DrawEquippedMateria(new Rect(768 + i * 54, y + 44, 40, 40), equipment.Sockets[i]);
        }

        private void Vital(Rect rect, string label, int current, int maximum, Color color)
        {
            Label(new Rect(rect.x, rect.y, 62, rect.height), label, cyan);
            Value(new Rect(rect.x + 64, rect.y, rect.width - 64, rect.height), current + "/" + maximum);
            Gauge(new Rect(rect.x + 64, rect.yMax, rect.width - 64, 5), maximum == 0 ? 0 : current / (float)maximum, color);
        }

        private void Value(Rect rect, string value) => FitLabel(rect, value, right);
        private static void FitLabel(Rect rect, string value, GUIStyle style)
        {
            int size = style.fontSize;
            float width = style.CalcSize(new GUIContent(value)).x;
            if (width > rect.width) style.fontSize = Mathf.Max(14, Mathf.FloorToInt(size * rect.width / width));
            Label(rect, value, style); style.fontSize = size;
        }
        private static void Label(Rect rect, string value, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style);
        }
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void Gauge(Rect rect, float fraction, Color color)
        {
            Fill(rect, new Color(.8f, .81f, .77f));
            float inset = rect.height < 8 ? 1 : 3;
            Rect inner = new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2, rect.height - inset * 2);
            Fill(inner, new Color(.14f, .14f, .21f)); inner.width *= Mathf.Clamp01(fraction); Fill(inner, color);
        }
        private static bool Click(Rect rect, bool enabled, MenuSound sound = MenuSound.Navigate)
        {
            EventType type = Event.current.type;
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = previous;
            if (clicked && type == EventType.MouseUp) { MenuSounds.Play(sound); return true; }
            return false;
        }
    }
}
