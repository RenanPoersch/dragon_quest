using System;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Prototype
{
    public sealed class ItemMenuPresentation
    {
        public static Rect MemberRect(int index) => new Rect(64, 174 + index * 180, 510, 158);
        private const float RowHeight = 60;
        private Vector2 scroll, pointer = new Vector2(-1, -1);
        private GUIStyle text, quantity, muted, heading;
        private Texture2D scale;
        private int activatedFrame = -1;
        private int revealedIndex = -1;

        public void Reset() { scroll = Vector2.zero; pointer = new Vector2(-1, -1); revealedIndex = -1; }

        private void EnsureStyles()
        {
            if (text != null) return;
            text = PrototypeTypography.Style(GUI.skin.label, 34);
            text.padding = new RectOffset(0, 0, 0, 0); text.alignment = TextAnchor.MiddleLeft;
            text.normal.textColor = Color.white;
            heading = new GUIStyle(text) { fontSize = 38 };
            quantity = new GUIStyle(text) { alignment = TextAnchor.MiddleRight };
            muted = new GUIStyle(text); muted.normal.textColor = new Color(.48f, .5f, .64f);
            scale = Resources.Load<Texture2D>("PixelArt/DragonScale");
        }

        public void Draw(InventoryMenuPresentation theme, ItemMenuSession session, PartyProgress party,
            bool hasScale, bool canInteract, Action back)
        {
            EnsureStyles(); session.Refresh(party);
            theme.Panel(new Rect(0, 0, 960, 72)); theme.Panel(new Rect(960, 0, 320, 72));
            theme.Panel(new Rect(0, 76, 1280, 68));
            theme.Panel(new Rect(0, 148, 600, 572)); theme.Panel(new Rect(600, 148, 680, 572));
            if (Button(new Rect(66, 10, 142, 48), "Use", canInteract)) session.ShowKeyItems(false);
            if (Button(new Rect(276, 10, 190, 48), "Arrange", canInteract)) { session.Arrange(party); scroll = Vector2.zero; }
            if (Button(new Rect(522, 10, 238, 48), "Key Items", canInteract)) { session.ShowKeyItems(true); scroll = Vector2.zero; }
            if (Button(new Rect(994, 10, 110, 48), "Item", canInteract, MenuSound.Cancel)) back();
            if (Button(new Rect(1128, 16, 124, 40), "Voltar", canInteract, MenuSound.Cancel))
            { if (session.ChoosingTarget) session.CancelTarget(); else back(); }
            Label(new Rect(28, 86, 1224, 48), session.Message.Length > 0 ? session.Message
                : session.ShowingKeyItems ? hasScale ? "Escama ancestral: objeto de historia, protegido contra venda." : "Nenhum objeto de historia."
                : session.SelectedItem?.Description ?? "Nenhum item no inventario.", text);

            Vector2 mouse = Event.current.mousePosition;
            bool moved = pointer.x >= 0 && (mouse - pointer).sqrMagnitude > .1f;
            for (int i = 0; i < party.Members.Count; i++)
            {
                Rect rect = MemberRect(i);
                theme.DrawItemMember(rect, party.Members[i]);
                if (!session.ChoosingTarget) continue;
                if (canInteract && moved && rect.Contains(mouse))
                { int before = session.TargetIndex; session.SelectTarget(i, party); MenuSounds.Navigate(before, session.TargetIndex); }
                if (Click(rect, canInteract)) { session.SelectTarget(i, party); session.ConfirmTarget(party); }
            }
            if (session.ChoosingTarget) theme.DrawHand(new Rect(MemberRect(session.TargetIndex).x + 6, MemberRect(session.TargetIndex).y + 20, 154, 110));

            Rect viewport = new Rect(610, 158, 654, 544);
            if (session.ShowingKeyItems)
            {
                if (hasScale)
                {
                    InventoryPixelArt.Draw(new Rect(688, 174, 46, 46), scale);
                    Label(new Rect(748, 174, 332, 46), "Escama ancestral", text);
                    Label(new Rect(1100, 174, 130, 46), ": 1", quantity);
                }
                else Label(new Rect(658, 180, 550, 50), "Nenhum objeto de historia.", muted);
            }
            else
            {
                float contentHeight = Mathf.Max(viewport.height, session.Items.Count * RowHeight);
                float rowY = session.ItemIndex * RowHeight;
                if (session.Items.Count > 0 && revealedIndex != session.ItemIndex)
                {
                    if (rowY < scroll.y) scroll.y = rowY;
                    if (rowY + RowHeight > scroll.y + viewport.height) scroll.y = rowY + RowHeight - viewport.height;
                    revealedIndex = session.ItemIndex;
                }
                scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, 628, contentHeight));
                Vector2 localMouse = Event.current.mousePosition;
                for (int i = 0; i < session.Items.Count; i++)
                {
                    ItemStack stack = session.Items[i];
                    Rect row = new Rect(64, i * RowHeight, 556, RowHeight - 2);
                    if (canInteract && moved && !session.ChoosingTarget && row.Contains(localMouse))
                    { int before = session.ItemIndex; session.SelectItem(i); MenuSounds.Navigate(before, session.ItemIndex); }
                    if (Click(row, canInteract)) { session.SelectItem(i); session.BeginUse(party); }
                    InventoryPixelArt.Draw(new Rect(72, row.y + 6, 44, 44), InventoryPixelArt.Texture(stack.Definition));
                    Label(new Rect(130, row.y, 316, row.height), stack.Definition.Name, stack.Quantity > 0 ? text : muted);
                    Label(new Rect(450, row.y, 164, row.height), ": " + stack.Quantity, quantity);
                }
                if (!session.ChoosingTarget && session.Items.Count > 0)
                    theme.DrawHand(new Rect(70, session.ItemIndex * RowHeight, 44, RowHeight - 2));
                if (session.Items.Count == 0) Label(new Rect(64, 12, 540, 50), "Nenhum item no inventario.", muted);
                GUI.EndScrollView();
            }
            if (Event.current.type == EventType.Repaint) pointer = mouse;
        }

        private bool Click(Rect rect, bool enabled, MenuSound sound = MenuSound.Confirm)
        {
            // Use normal press/release tracking; keyboard and gamepad confirmation run once in Update.
            EventType type = Event.current.type;
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = previous;
            if (!clicked || type != EventType.MouseUp || activatedFrame == Time.frameCount) return false;
            activatedFrame = Time.frameCount; MenuSounds.Play(sound); return true;
        }

        private bool Button(Rect rect, string value, bool enabled, MenuSound sound = MenuSound.Confirm)
        { Label(rect, value, heading); return Click(rect, enabled, sound); }

        private static void Label(Rect rect, string value, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style);
        }
    }
}
