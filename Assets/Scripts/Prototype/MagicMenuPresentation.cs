using System;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class MagicMenuPresentation
    {
        private const float RowHeight = 60;
        private Vector2 scroll, pointer = new Vector2(-1, -1);
        private int revealedIndex = -1, activatedFrame = -1;
        private GUIStyle text, heading, right, muted;
        public void Reset() { scroll = Vector2.zero; pointer = new Vector2(-1, -1); revealedIndex = -1; }
        private void EnsureStyles()
        {
            if (text != null) return;
            text = PrototypeTypography.Style(GUI.skin.label, 32);
            text.padding = new RectOffset(0, 0, 0, 0); text.alignment = TextAnchor.MiddleLeft;
            text.normal.textColor = Color.white;
            heading = new GUIStyle(text) { fontSize = 38 };
            right = new GUIStyle(text) { alignment = TextAnchor.MiddleRight };
            muted = new GUIStyle(text); muted.normal.textColor = new Color(.48f, .5f, .64f);
        }

        public void Draw(InventoryMenuPresentation theme, MagicMenuSession session, PartyProgress party, bool canInteract, Action back)
        {
            EnsureStyles(); session.Refresh(party);
            theme.Panel(new Rect(0, 0, 960, 72)); theme.Panel(new Rect(960, 0, 320, 72));
            theme.Panel(new Rect(0, 76, 1280, 68));
            theme.Panel(new Rect(0, 148, 600, 572)); theme.Panel(new Rect(600, 148, 680, 572));
            var caster = session.Caster(party);
            Label(new Rect(64, 10, 220, 48), "Magic", heading);
            if (Click(new Rect(320, 10, 420, 48), canInteract)) session.SelectCaster(session.CasterIndex, party);
            Label(new Rect(320, 10, 420, 48), caster?.Name ?? "", heading);
            if (Click(new Rect(1128, 16, 124, 40), canInteract, MenuSound.Cancel))
            { if (!session.Cancel()) back(); }
            Label(new Rect(1128, 16, 124, 40), "Voltar", text);
            Label(new Rect(28, 86, 1224, 48), session.Message, text);
            Vector2 mouse = Event.current.mousePosition;
            bool moved = pointer.x >= 0 && (mouse - pointer).sqrMagnitude > .1f;
            for (int i = 0; i < party.Members.Count; i++)
            {
                Rect rect = ItemMenuPresentation.MemberRect(i);
                theme.DrawItemMember(rect, party.Members[i]);
                if (canInteract && moved && rect.Contains(mouse))
                {
                    int before = session.ChoosingTarget ? session.TargetIndex : session.CasterIndex;
                    if (session.ChoosingTarget) session.SelectTarget(i, party);
                    else if (session.ChoosingCaster) session.SelectCaster(i, party);
                    MenuSounds.Navigate(before, session.ChoosingTarget ? session.TargetIndex : session.CasterIndex);
                }
                if (Click(rect, canInteract))
                {
                    if (session.ChoosingTarget) { session.SelectTarget(i, party); session.ConfirmTarget(party); }
                    else { session.SelectCaster(i, party); session.ConfirmCaster(); Reset(); }
                }
            }
            if (session.ChoosingCaster && caster != null) theme.DrawHand(ItemMenuPresentation.MemberRect(session.CasterIndex));
            else if (session.ChoosingTarget)
            {
                for (int i = 0; i < party.Members.Count; i++)
                    if (session.SelectedSpell.Targets == TargetRule.AllAllies
                        ? PartyProgress.IsMagicTarget(session.SelectedSpell, party.Members[i]) : i == session.TargetIndex)
                        theme.DrawHand(ItemMenuPresentation.MemberRect(i));
            }
            Rect viewport = new Rect(610, 158, 654, 544);
            float rowY = session.SpellIndex * RowHeight;
            if (revealedIndex != session.SpellIndex)
            {
                if (rowY < scroll.y) scroll.y = rowY;
                if (rowY + RowHeight > scroll.y + viewport.height) scroll.y = rowY + RowHeight - viewport.height;
                revealedIndex = session.SpellIndex;
            }
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, 628, Mathf.Max(viewport.height, session.Spells.Count * RowHeight)));
            Vector2 localMouse = Event.current.mousePosition;
            for (int i = 0; i < session.Spells.Count; i++)
            {
                var spell = session.Spells[i]; Rect row = new Rect(64, i * RowHeight, 556, RowHeight - 2);
                if (canInteract && moved && !session.ChoosingCaster && !session.ChoosingTarget && row.Contains(localMouse)) session.SelectSpell(i);
                if (Click(row, canInteract && !session.ChoosingCaster)) { session.SelectSpell(i); session.BeginUse(party); }
                var style = session.CanUse(party, spell) ? text : muted;
                Label(new Rect(78, row.y, 370, row.height), spell.Name, style);
                Color previous = GUI.color; if (!session.CanUse(party, spell)) GUI.color = new Color(.48f, .5f, .64f);
                Label(new Rect(448, row.y, 164, row.height), spell.MpCost + " MP", right); GUI.color = previous;
            }
            if (!session.ChoosingCaster && !session.ChoosingTarget && session.Spells.Count > 0)
                theme.DrawHand(new Rect(70, rowY, 44, RowHeight - 2));
            if (session.Spells.Count == 0) Label(new Rect(64, 12, 540, 50), "Nenhuma magia disponivel.", muted);
            GUI.EndScrollView();
            if (Event.current.type == EventType.Repaint) pointer = mouse;
        }
        private bool Click(Rect rect, bool enabled, MenuSound sound = MenuSound.Confirm)
        {
            EventType type = Event.current.type;
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = previous;
            if (!clicked || type != EventType.MouseUp || activatedFrame == Time.frameCount) return false;
            activatedFrame = Time.frameCount; MenuSounds.Play(sound); DragonQuest.Input.GameInput.ConsumeGamepadFrame(); return true;
        }
        private static void Label(Rect rect, string value, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.06f, .06f, .15f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = previous; GUI.Label(rect, value, style);
        }
    }
}
