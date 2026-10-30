using System;
using DragonQuest.Equipment;
using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Prototype
{
    public sealed class EquipmentMenuPresentation
    {
        private static readonly string[] categories = { "Armas", "Armaduras", "Acessorios" };
        private static readonly string[] singular = { "arma", "armadura", "acessorio" };
        private static readonly string[] stats = { "Ataque", "Defesa", "Magia", "HP max.", "MP max.", "Velocidade", "Encaixes" };
        private static readonly Color up = new Color(.22f, 1, .38f), down = new Color(1, .25f, .3f);
        private static readonly Color equippedText = new Color(.55f, .57f, .64f);
        private GUIStyle text, heading, small, cyan, number;
        private Vector2 scroll, pointer = new Vector2(-1, -1);
        private int revealed = -1, activatedFrame = -1, lastMember = -1;
        private EquipmentMenuCategory lastCategory;
        public void Reset() { scroll = Vector2.zero; pointer = new Vector2(-1, -1); revealed = lastMember = -1; }
        private void EnsureStyles()
        {
            if (text != null) return;
            text = PrototypeTypography.Style(GUI.skin.label, 30); text.padding = new RectOffset(0, 0, 0, 0);
            text.normal.textColor = Color.white; text.alignment = TextAnchor.MiddleLeft;
            heading = new GUIStyle(text) { fontSize = 38 }; small = new GUIStyle(text) { fontSize = 22 };
            cyan = new GUIStyle(small); cyan.normal.textColor = new Color(.1f, 1, .91f);
            number = new GUIStyle(text) { alignment = TextAnchor.MiddleRight };
        }
        public void Draw(InventoryMenuPresentation theme, EquipmentMenuSession session, PartyProgress party, bool canInteract, Action back)
        {
            EnsureStyles(); session.Refresh(party);
            theme.Panel(new Rect(0, 0, 960, 72)); theme.Panel(new Rect(960, 0, 320, 72));
            theme.Panel(new Rect(0, 76, 1280, 68)); theme.Panel(new Rect(0, 148, 600, 572));
            theme.Panel(new Rect(600, 148, 680, 392)); theme.Panel(new Rect(600, 540, 680, 180));
            for (int i = 0; i < categories.Length; i++)
            {
                Rect tab = new Rect(52 + i * 292, 10, 280, 48);
                if (Click(tab, canInteract, MenuSound.Navigate)) session.SelectCategory((EquipmentMenuCategory)i, party);
                Label(tab, categories[i], heading, (int)session.Category == i ? cyan.normal.textColor : Color.white);
                if ((int)session.Category == i) Fill(new Rect(tab.x, 58, heading.CalcSize(new GUIContent(categories[i])).x, 2), cyan.normal.textColor);
            }
            Label(new Rect(982, 10, 164, 48), "Equip", heading);
            if (Button(new Rect(1158, 14, 108, 44), "Voltar", text, canInteract, MenuSound.Cancel)) back();
            var selected = session.SelectedItem;
            FitLabel(new Rect(28, 86, 1224, 48), session.Message, text);
            var member = session.Member;
            if (member != null)
            {
                theme.DrawPortrait(new Rect(48, 174, 140, 140), member);
                Label(new Rect(212, 174, 260, 42), member.Name, heading);
                Equipped(new Rect(212, 222, 356, 28), "Wpn:", member.Weapon, session.Category == EquipmentMenuCategory.Weapons);
                Equipped(new Rect(212, 252, 356, 28), "Arm:", member.Armor, session.Category == EquipmentMenuCategory.Armor);
                Equipped(new Rect(212, 282, 356, 28), "Acc:", member.Accessory, session.Category == EquipmentMenuCategory.Accessories);
                Label(new Rect(238, 330, 96, 32), "Atual", small);
                Label(new Rect(350, 330, 96, 32), "Novo", small);
                Label(new Rect(474, 330, 100, 32), "Dif.", small);
                var current = session.CurrentStats; var preview = session.PreviewStats;
                for (int i = 0; i < EquipmentMenuStats.Count; i++)
                {
                    float y = 372 + i * 44; int delta = preview[i] - current[i];
                    Color color = delta > 0 ? up : delta < 0 ? down : Color.white;
                    Label(new Rect(32, y, 196, 38), stats[i], text, cyan.normal.textColor);
                    FitLabel(new Rect(234, y, 90, 38), current[i].ToString(), number);
                    FitLabel(new Rect(344, y, 90, 38), preview[i].ToString(), number, color);
                    FitLabel(new Rect(482, y, 92, 38), (delta > 0 ? "+" : "") + delta, number,
                        delta == 0 ? new Color(.62f, .66f, .75f) : color);
                }
            }
            if (lastMember != session.MemberIndex || lastCategory != session.Category)
            { scroll = Vector2.zero; revealed = -1; lastMember = session.MemberIndex; lastCategory = session.Category; }
            Vector2 mouse = Event.current.mousePosition;
            bool moved = pointer.x >= 0 && (mouse - pointer).sqrMagnitude > .1f;
            Rect viewport = new Rect(610, 158, 654, 368); const float rowHeight = 64;
            float rowY = session.ItemIndex * rowHeight;
            if (session.HasSelection && revealed != session.ItemIndex)
            {
                if (rowY < scroll.y) scroll.y = rowY;
                if (rowY + rowHeight > scroll.y + viewport.height) scroll.y = rowY + rowHeight - viewport.height;
                revealed = session.ItemIndex;
            }
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, 628, Mathf.Max(viewport.height, session.Items.Count * rowHeight)));
            Vector2 localMouse = Event.current.mousePosition;
            for (int i = 0; i < session.Items.Count; i++)
            {
                var item = session.Items[i]; Rect row = new Rect(64, i * rowHeight, 556, rowHeight - 2);
                bool equipped = party.IsEquipped(item);
                if (canInteract && moved && row.Contains(localMouse))
                { int before = session.ItemIndex; session.SelectItem(i); MenuSounds.Navigate(before, session.ItemIndex); }
                if (Click(row, canInteract, MenuSound.Navigate)) session.SelectItem(i);
                InventoryPixelArt.Draw(new Rect(72, row.y + 8, 48, 48), InventoryPixelArt.Texture(item?.Definition));
                FitLabel(new Rect(132, row.y, 474, 36), item?.Definition.Name
                    ?? "Remover " + singular[(int)session.Category], text, equipped ? equippedText : Color.white);
                if (equipped) FitLabel(new Rect(132, row.y + 34, 474, 24), party.LocationOf(item), small, equippedText);
            }
            if (session.HasSelection) theme.DrawHand(new Rect(70, session.ItemIndex * rowHeight, 44, rowHeight - 2));
            else Label(new Rect(34, 28, 570, 40), "--", text);
            GUI.EndScrollView();
            selected = session.SelectedItem;
            InventoryPixelArt.Draw(new Rect(626, 558, 92, 92), InventoryPixelArt.Texture(selected?.Definition));
            FitLabel(new Rect(736, 556, 502, 40), selected?.Definition.Name ?? "--", text);
            if (selected != null)
            {
                for (int i = 0; i < selected.Sockets.Count; i += 2)
                    MateriaSocketArt.DrawLinked(new Rect(736 + i * 44, 630, 36, 36), new Rect(780 + i * 44, 630, 36, 36));
                if (selected == session.CurrentItem)
                    for (int i = 0; i < selected.Sockets.Count; i++) MateriaSocketArt.DrawEquippedMateria(new Rect(736 + i * 44, 630, 36, 36), selected.Sockets[i]);
            }
            if (session.HasSelection && Button(new Rect(1050, 658, 202, 46), session.CanApply
                ? selected == null ? "Remover" : "Equipar" : selected == null ? "Vazio" : "Equipado", text, canInteract && session.CanApply)) session.Apply(party);
            if (Event.current.type == EventType.Repaint) pointer = mouse;
        }
        private void Equipped(Rect rect, string title, EquipmentLoadout item, bool active)
        {
            Label(new Rect(rect.x, rect.y, 64, rect.height), title, cyan);
            FitLabel(new Rect(rect.x + 66, rect.y, rect.width - 66, rect.height), item?.Definition.Name ?? "--", small,
                active ? cyan.normal.textColor : Color.white);
        }
        private bool Click(Rect rect, bool enabled, MenuSound sound = MenuSound.Confirm)
        {
            EventType type = Event.current.type; bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = previous;
            if (!clicked || type != EventType.MouseUp || activatedFrame == Time.frameCount) return false;
            activatedFrame = Time.frameCount; MenuSounds.Play(sound); return true;
        }
        private bool Button(Rect rect, string value, GUIStyle style, bool enabled, MenuSound sound = MenuSound.Confirm)
        { Label(rect, value, style, enabled ? Color.white : new Color(.52f, .56f, .66f)); return Click(rect, enabled, sound); }
        private static void FitLabel(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            int size = style.fontSize; float width = style.CalcSize(new GUIContent(value)).x;
            if (width > rect.width) style.fontSize = Mathf.Max(14, Mathf.FloorToInt(size * rect.width / width));
            Label(rect, value, style, color); style.fontSize = size;
        }
        private static void Label(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            Color previous = GUI.color, previousText = style.normal.textColor;
            style.normal.textColor = color ?? previousText; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style); style.normal.textColor = previousText;
        }
        private static void Fill(Rect rect, Color color)
        { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    }
}
