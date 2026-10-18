using System;
using DragonQuest.Equipment;
using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Prototype
{
    public sealed class MateriaMenuPresentation
    {
        public static Rect SocketRect(int member, EquipmentSlot equipment, int socket) => new Rect(282 + socket * 44,
            234 + member * 180 + (equipment == EquipmentSlot.Armor ? 64 : 0), 36, 36);
        private const float RowHeight = 60;
        private Vector2 scroll, pointer = new Vector2(-1, -1);
        private int revealedIndex = -1, activatedFrame = -1;
        private GUIStyle text, heading, small, cyan, muted, mutedSmall;
        public void Reset() { scroll = Vector2.zero; pointer = new Vector2(-1, -1); revealedIndex = -1; }
        private void EnsureStyles()
        {
            if (text != null) return;
            text = PrototypeTypography.Style(GUI.skin.label, 32); text.padding = new RectOffset(0, 0, 0, 0);
            text.normal.textColor = Color.white; text.alignment = TextAnchor.MiddleLeft;
            heading = new GUIStyle(text) { fontSize = 38 };
            small = new GUIStyle(text) { fontSize = 22 };
            cyan = new GUIStyle(small); cyan.normal.textColor = new Color(.1f, 1, .91f);
            muted = new GUIStyle(text); muted.normal.textColor = new Color(.55f, .57f, .64f);
            mutedSmall = new GUIStyle(small); mutedSmall.normal.textColor = muted.normal.textColor;
        }
        public void Draw(InventoryMenuPresentation theme, MateriaMenuSession session, PartyProgress party, bool canInteract, Action back)
        {
            EnsureStyles(); session.Refresh(party);
            theme.Panel(new Rect(0, 0, 960, 72)); theme.Panel(new Rect(960, 0, 320, 72));
            theme.Panel(new Rect(0, 76, 1280, 68));
            theme.Panel(new Rect(0, 148, 600, 572)); theme.Panel(new Rect(600, 148, 680, 572));
            if (Button(new Rect(66, 10, 164, 48), "Equip", canInteract)) session.CancelSocket();
            if (Button(new Rect(276, 10, 190, 48), "Remove", canInteract)) session.BeginRemove(party);
            if (Button(new Rect(522, 10, 238, 48), "Arrange", canInteract))
            { session.Arrange(party); scroll = Vector2.zero; revealedIndex = -1; }
            if (Button(new Rect(982, 10, 164, 48), "Materia", canInteract, sound: MenuSound.Cancel)) back();
            if (Button(new Rect(1158, 14, 108, 44), "Voltar", canInteract, text, MenuSound.Cancel))
            { if (session.ChoosingSocket) session.CancelSocket(); else back(); }
            var described = session.Removing ? session.SelectedSocket?.Equipment.Sockets[session.SelectedSocket.Index] : session.SelectedMateria;
            FitLabel(new Rect(28, 86, 1224, 48), session.Message.Length > 0 ? session.Message : described?.Definition.Description ?? "", text);
            Vector2 mouse = Event.current.mousePosition;
            bool moved = pointer.x >= 0 && (mouse - pointer).sqrMagnitude > .1f;
            for (int i = 0; i < party.Members.Count; i++)
            {
                var member = party.Members[i]; float y = 174 + i * 180;
                theme.DrawPortrait(new Rect(64, y, 154, 154), member);
                Label(new Rect(250, y, 316, 36), member.Name, heading);
                EquipmentRow(i, EquipmentSlot.Weapon, member.Weapon, y + 38, session, party, canInteract, moved);
                EquipmentRow(i, EquipmentSlot.Armor, member.Armor, y + 102, session, party, canInteract, moved);
            }
            if (session.ChoosingSocket && session.SelectedSocket != null)
            {
                var target = session.SelectedSocket; int member = 0;
                while (member < party.Members.Count && party.Members[member] != target.Member) member++;
                theme.DrawHand(SocketRect(member, target.Equipment.Definition.Slot, target.Index));
            }
            Rect viewport = new Rect(610, 158, 654, 544);
            float rowY = session.MateriaIndex * RowHeight;
            if (session.Materias.Count > 0 && revealedIndex != session.MateriaIndex)
            {
                if (rowY < scroll.y) scroll.y = rowY;
                if (rowY + RowHeight > scroll.y + viewport.height) scroll.y = rowY + RowHeight - viewport.height;
                revealedIndex = session.MateriaIndex;
            }
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, 628, Mathf.Max(viewport.height, session.Materias.Count * RowHeight)));
            Vector2 localMouse = Event.current.mousePosition;
            for (int i = 0; i < session.Materias.Count; i++)
            {
                var materia = session.Materias[i]; Rect row = new Rect(64, i * RowHeight, 556, RowHeight - 2);
                bool equipped = party.IsEquipped(materia);
                if (canInteract && moved && !session.ChoosingSocket && row.Contains(localMouse))
                { int before = session.MateriaIndex; session.SelectMateria(i); MenuSounds.Navigate(before, session.MateriaIndex); }
                if (Click(row, canInteract)) { session.SelectMateria(i); session.BeginEquip(party); }
                MateriaSocketArt.DrawMateria(new Rect(72, row.y + 10, 40, 40), materia.Definition);
                FitLabel(new Rect(130, row.y, 476, 34), materia.Definition.Name, equipped ? muted : text);
                if (equipped) FitLabel(new Rect(130, row.y + 32, 476, 24), party.LocationOf(materia), mutedSmall);
            }
            if (!session.ChoosingSocket && session.Materias.Count > 0) theme.DrawHand(new Rect(70, rowY, 44, RowHeight - 2));
            GUI.EndScrollView();
            if (Event.current.type == EventType.Repaint) pointer = mouse;
        }
        private void EquipmentRow(int member, EquipmentSlot kind, EquipmentLoadout gear, float y,
            MateriaMenuSession session, PartyProgress party, bool enabled, bool moved)
        {
            Label(new Rect(250, y, 58, 24), kind == EquipmentSlot.Weapon ? "Wpn:" : "Arm:", cyan);
            FitLabel(new Rect(310, y, 260, 24), gear?.Definition.Name ?? "--", small);
            if (gear == null) return;
            for (int i = 0; i < gear.Sockets.Count; i++)
            {
                Rect socket = SocketRect(member, kind, i);
                int pair = gear.LinkedSocket(i);
                if (pair < 0 || pair >= gear.Sockets.Count) MateriaSocketArt.DrawSingle(socket);
                else if (pair > i) MateriaSocketArt.DrawLinked(socket, SocketRect(member, kind, pair));
            }
            for (int i = 0; i < gear.Sockets.Count; i++)
            {
                Rect rect = SocketRect(member, kind, i);
                MateriaSocketArt.DrawEquippedMateria(rect, gear.Sockets[i]);
                if (!session.ChoosingSocket) continue;
                int target = -1;
                for (int j = 0; j < session.Sockets.Count; j++)
                    if (session.Sockets[j].Equipment == gear && session.Sockets[j].Index == i) { target = j; break; }
                if (enabled && moved && rect.Contains(Event.current.mousePosition))
                { int before = session.SocketIndex; session.SelectSocket(target); MenuSounds.Navigate(before, session.SocketIndex); }
                if (Click(rect, enabled)) { session.SelectSocket(target); session.ConfirmSocket(party); }
            }
        }
        private bool Click(Rect rect, bool enabled, MenuSound sound = MenuSound.Confirm)
        {
            EventType type = Event.current.type;
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = previous;
            if (!clicked || type != EventType.MouseUp || activatedFrame == Time.frameCount) return false;
            activatedFrame = Time.frameCount; MenuSounds.Play(sound); return true;
        }
        private bool Button(Rect rect, string value, bool enabled, GUIStyle style = null, MenuSound sound = MenuSound.Confirm)
        { Label(rect, value, style ?? heading); return Click(rect, enabled, sound); }
        private static void FitLabel(Rect rect, string value, GUIStyle style)
        {
            int size = style.fontSize; float width = style.CalcSize(new GUIContent(value)).x;
            if (width > rect.width) style.fontSize = Mathf.Max(14, Mathf.FloorToInt(size * rect.width / width));
            Label(rect, value, style); style.fontSize = size;
        }
        private static void Label(Rect rect, string value, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style);
        }
    }
}
