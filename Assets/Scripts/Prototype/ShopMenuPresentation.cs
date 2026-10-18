using System;
using DragonQuest.Equipment;
using DragonQuest.Shops;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class ShopMenuPresentation
    {
        private static readonly Color cyan = new Color(.1f, 1, .91f), muted = new Color(.51f, .53f, .65f);
        private static readonly Color gain = new Color(1, .94f, .12f), loss = new Color(1, .30f, .32f);
        private static readonly string[] filters = { "Armas", "Armaduras", "Acessorios" };
        private GUIStyle text, heading, small, number, description;
        private Vector2 scroll, pointer = new Vector2(-1, -1);
        private int revealed = -1, activatedFrame = -1;
        private ShopMenuPhase lastPhase;
        private ShopEquipmentFilter lastFilter;
        private bool lastSelling;

        public void Reset() { scroll = Vector2.zero; revealed = -1; pointer = new Vector2(-1, -1); }
        private void Styles()
        {
            if (text != null) return;
            text = PrototypeTypography.Style(GUI.skin.label, 36);
            text.padding = new RectOffset(0, 0, 0, 0); text.alignment = TextAnchor.MiddleLeft;
            text.normal.textColor = Color.white;
            heading = new GUIStyle(text) { fontSize = 40 };
            small = new GUIStyle(text) { fontSize = 27 };
            number = PrototypeTypography.Style(text, 36, true); number.alignment = TextAnchor.MiddleRight;
            description = new GUIStyle(text) { fontSize = 30, wordWrap = true };
        }
        public void Draw(InventoryMenuPresentation theme, ShopMenuSession menu, bool interactive,
            int actionIndex, Action<int> action, Action back)
        {
            Styles(); theme.EnsureStyles(); menu.Refresh();
            bool quantity = menu.Phase == ShopMenuPhase.Quantity;
            bool equipment = menu.HasEquipment;
            if (menu.Selling != lastSelling || menu.Filter != lastFilter)
            { scroll = Vector2.zero; revealed = -1; }
            if (quantity != (lastPhase == ShopMenuPhase.Quantity)) revealed = -1;
            lastSelling = menu.Selling; lastFilter = menu.Filter; lastPhase = menu.Phase;
            theme.Panel(new Rect(0, 0, 900, 76)); theme.Panel(new Rect(900, 0, 380, 76));
            string prompt = quantity ? menu.Selling ? "Quantos deseja vender?" : "Quantos deseja comprar?"
                : menu.Phase == ShopMenuPhase.Actions ? "O que deseja fazer?"
                : menu.Selling ? "O que deseja vender?" : "O que deseja comprar?";
            Label(new Rect(30, 10, 848, 54), prompt, heading);
            Fit(new Rect(930, 10, 320, 54), menu.Shop.Name, heading);
            theme.Panel(new Rect(0, 76, 1280, 76));
            string detail = menu.Message.Length > 0 ? menu.Message : menu.Selected?.BlockReason ?? menu.Selected?.Description ?? "";
            description.fontSize = detail.Length > 125 ? 25 : 30;
            Label(new Rect(30, 86, 1220, 54), detail, description);

            float bottom = equipment ? 444 : 652;
            Rect list = quantity ? new Rect(420, 152, 860, bottom - 152) : new Rect(0, 152, 888, bottom - 152);
            Rect info = quantity ? new Rect(0, 152, 420, bottom - 152) : new Rect(888, 152, 392, bottom - 152);
            theme.Panel(list); theme.Panel(info);
            float listTop = list.y + 14;
            if (equipment)
            {
                for (int i = 0; i < filters.Length; i++)
                {
                    Rect tab = new Rect(list.x + 34 + i * 266, list.y + 12, 250, 38);
                    if (Click(tab, interactive && !quantity)) menu.SelectFilter((ShopEquipmentFilter)i);
                    Label(tab, filters[i], small, (int)menu.Filter == i ? cyan : muted);
                    if ((int)menu.Filter == i) Fill(new Rect(tab.x, tab.yMax - 2, small.CalcSize(new GUIContent(filters[i])).x, 2), cyan);
                }
                listTop += 52;
            }
            DrawList(theme, menu, new Rect(list.x + 10, listTop, list.width - 24, bottom - listTop - 14), interactive && !quantity);
            if (quantity) DrawQuantity(theme, menu, info, interactive);
            else DrawOwnership(menu, info);
            if (equipment) DrawComparisons(theme, menu);
            theme.Panel(new Rect(0, 652, 1280, 68));
            if (quantity)
            {
                if (Button(new Rect(76, 664, 300, 42), menu.Selling ? "Vender" : "Comprar", text, interactive && menu.Maximum > 0)) menu.Confirm();
                if (Button(new Rect(1040, 664, 210, 42), "Cancelar", text, interactive)) back();
            }
            else
            {
                const int count = 3;
                for (int i = 0; i < count; i++)
                {
                    Rect rect = ActionRect(i);
                    string label = i == 0 ? "Comprar" : i == 1 ? "Vender" : "Sair";
                    bool active = i < 2 && (i == 1) == menu.Selling;
                    if (Button(rect, label, text, interactive, active ? cyan : Color.white)) action(i);
                    if (menu.Phase == ShopMenuPhase.Actions && actionIndex == i) theme.DrawHand(rect);
                }
            }
            if (Event.current.type == EventType.Repaint) pointer = Event.current.mousePosition;
        }
        private static Rect ActionRect(int index) => index == 0 ? new Rect(76, 664, 220, 42)
            : index == 1 ? new Rect(342, 664, 220, 42)
            : new Rect(1110, 664, 140, 42);
        private void DrawList(InventoryMenuPresentation theme, ShopMenuSession menu, Rect viewport, bool interactive)
        {
            const float rowHeight = 59;
            bool moved = pointer.x >= 0 && (Event.current.mousePosition - pointer).sqrMagnitude > .1f;
            float rowY = menu.Index * rowHeight;
            float contentHeight = Mathf.Max(viewport.height, menu.Entries.Count * rowHeight);
            if (revealed != menu.Index)
            {
                if (rowY < scroll.y) scroll.y = rowY;
                if (rowY + rowHeight > scroll.y + viewport.height) scroll.y = rowY + rowHeight - viewport.height;
                revealed = menu.Index;
            }
            scroll.y = Mathf.Clamp(scroll.y, 0, contentHeight - viewport.height);
            // The reference uses a plain silver track, without the engine's default scroll chrome.
            Event evt = Event.current;
            if (interactive && evt.type == EventType.ScrollWheel && viewport.Contains(evt.mousePosition))
            { scroll.y = Mathf.Clamp(scroll.y + evt.delta.y * 28, 0, contentHeight - viewport.height); evt.Use(); }
            Rect clip = new Rect(viewport.x, viewport.y, viewport.width - 22, viewport.height);
            GUI.BeginGroup(clip);
            for (int i = 0; i < menu.Entries.Count; i++)
            {
                var entry = menu.Entries[i]; Rect row = new Rect(0, i * rowHeight - scroll.y, clip.width, rowHeight);
                if (row.yMax < 0 || row.y >= clip.height) continue;
                if (interactive && menu.Phase == ShopMenuPhase.Browse && moved && row.Contains(Event.current.mousePosition)) menu.Select(i);
                if (Click(row, interactive))
                {
                    if (menu.Phase == ShopMenuPhase.Actions) menu.Browse(menu.Selling);
                    menu.Select(i); menu.BeginQuantity();
                }
                Color color = entry.Available == 0 || entry.BlockReason != null ? muted : Color.white;
                InventoryPixelArt.Draw(new Rect(65, row.y + 4, 48, 48), Texture(entry));
                Fit(new Rect(124, row.y, clip.width - 306, rowHeight), entry.Name, text, color);
                Fit(new Rect(clip.width - 166, row.y, 128, rowHeight), menu.Selling ? ": " + entry.Available : entry.Price.ToString(), number, color);
                if (i == menu.Index && menu.Phase != ShopMenuPhase.Actions) theme.DrawHand(new Rect(65, row.y, 44, rowHeight));
            }
            if (menu.Entries.Count == 0) Label(new Rect(32, 12, clip.width - 64, 56), "Nenhum objeto disponivel.", small, muted);
            GUI.EndGroup();
            if (contentHeight > viewport.height)
            {
                Rect track = new Rect(viewport.xMax - 16, viewport.y, 14, viewport.height);
                Fill(track, new Color(.16f, .17f, .23f));
                float thumb = Mathf.Max(30, viewport.height * viewport.height / contentHeight);
                float position = scroll.y / (contentHeight - viewport.height) * (track.height - thumb);
                Fill(new Rect(track.x + 1, track.y + position, track.width - 2, thumb), new Color(.70f, .72f, .72f));
                if (interactive && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && track.Contains(Event.current.mousePosition))
                { scroll.y = Mathf.Clamp01((Event.current.mousePosition.y - track.y - thumb / 2) / (track.height - thumb)) * (contentHeight - viewport.height); Event.current.Use(); }
            }
        }
        private void DrawOwnership(ShopMenuSession menu, Rect rect)
        {
            float spacing = Mathf.Min(140, (rect.height - 28) / 3);
            string[] labels = { "Ouro", "Possui", "Equipado" };
            int[] values = { menu.Service.Progress.Gold, menu.Owned, menu.Equipped };
            for (int i = 0; i < 3; i++)
            {
                float y = rect.y + 26 + i * spacing;
                Label(new Rect(rect.x + 42, y, rect.width - 84, 44), labels[i], text, cyan);
                Fit(new Rect(rect.x + 42, y + 40, rect.width - 84, 42), values[i].ToString(), number);
            }
        }
        private void DrawQuantity(InventoryMenuPresentation theme, ShopMenuSession menu, Rect rect, bool interactive)
        {
            float x = rect.x + 32, y = rect.y + 18;
            Label(new Rect(x, y, 190, 38), "Quantidade", small, cyan);
            if (Button(new Rect(x + 202, y, 32, 38), "-", text, interactive && menu.Quantity > 1)) menu.AdjustQuantity(-1);
            Fit(new Rect(x + 238, y, 72, 38), menu.Quantity.ToString(), number);
            if (Button(new Rect(x + 318, y, 32, 38), "+", text, interactive && menu.Quantity < menu.Maximum)) menu.AdjustQuantity(1);
            Label(new Rect(x, y + 48, 180, 34), "Total", small, cyan);
            Fit(new Rect(x + 170, y + 48, 180, 34), menu.Total.ToString(), number);
            Label(new Rect(x, y + 92, 215, 34), "Ouro restante", small, cyan);
            Fit(new Rect(x + 180, y + 132, 170, 34), menu.RemainingGold.ToString(), number);
            Label(new Rect(x, y + 178, 100, 32), "Possui", small, cyan);
            Fit(new Rect(x + 105, y + 178, 62, 32), menu.Owned.ToString(), small);
            Label(new Rect(x + 186, y + 178, 116, 32), "Equipado", small, cyan);
            Fit(new Rect(x + 314, y + 178, 38, 32), menu.Equipped.ToString(), small);
            if (rect.height > 310)
            {
                Label(new Rect(x, y + 246, 190, 38), "Ouro", text, cyan);
                Fit(new Rect(x + 170, y + 246, 180, 38), menu.Service.Progress.Gold.ToString(), number);
            }
            if (Button(new Rect(x + 245, rect.yMax - 48, 108, 32), "Maximo", small, interactive && menu.Maximum > 1)) menu.SetMaximum();
        }
        private void DrawComparisons(InventoryMenuPresentation theme, ShopMenuSession menu)
        {
            theme.Panel(new Rect(0, 444, 1280, 208));
            var members = menu.Service.Party.Members;
            for (int i = 0; i < members.Count && i < 3; i++)
            {
                var member = members[i]; float x = 30 + i * 420;
                theme.DrawPortrait(new Rect(x, 470, 88, 100), member);
                Label(new Rect(x + 102, 458, 286, 34), member.Name, small);
                var current = new EquipmentMenuStats(member, member.Weapon, member.Armor, member.Accessory);
                var preview = menu.Preview(member);
                Stat(new Rect(x + 102, 496, 286, 34), "A", current.Attack, preview.Attack);
                Stat(new Rect(x + 102, 538, 286, 34), "D", current.Defense, preview.Defense);
                Stat(new Rect(x + 102, 580, 286, 34), "M", current.Magic, preview.Magic);
            }
        }
        private void Stat(Rect rect, string name, int before, int after)
        {
            Label(new Rect(rect.x, rect.y, 40, rect.height), name, text, cyan);
            Fit(new Rect(rect.x + 44, rect.y, 82, rect.height), before.ToString(), number);
            // A small pixel arrow matches the reference's cyan stat arrows.
            float x = rect.x + 151, y = rect.y + 10;
            Fill(new Rect(x - 8, y + 5, 14, 6), cyan);
            for (int i = 0; i < 8; i++) Fill(new Rect(x + i, y + i, 2, 16 - i * 2), cyan);
            Fit(new Rect(rect.x + 178, rect.y, 90, rect.height), after.ToString(), number, after > before ? gain : after < before ? loss : Color.white);
        }
        private static Texture2D Texture(ShopMenuEntry entry) => entry.Item != null ? InventoryPixelArt.Texture(entry.Item)
            : entry.Equipment != null ? InventoryPixelArt.Texture(entry.Equipment) : InventoryPixelArt.Texture(entry.Materia);
        private bool Click(Rect rect, bool enabled)
        {
            if (!enabled) return false;
            EventType type = Event.current.type;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (!clicked || type != EventType.MouseUp || activatedFrame == Time.frameCount) return false;
            activatedFrame = Time.frameCount; return true;
        }
        private bool Button(Rect rect, string value, GUIStyle style, bool enabled, Color? color = null)
        { Fit(rect, value, style, enabled ? color ?? Color.white : muted); return Click(rect, enabled); }
        private static void Fit(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            int size = style.fontSize; float width = style.CalcSize(new GUIContent(value)).x;
            if (width > rect.width) style.fontSize = Mathf.Max(14, Mathf.FloorToInt(size * rect.width / width));
            Label(rect, value, style, color); style.fontSize = size;
        }
        private static void Label(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            Color previous = GUI.color, original = style.normal.textColor;
            style.normal.textColor = Color.white; GUI.color = new Color(.035f, .035f, .10f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), value, style);
            GUI.color = color ?? original; GUI.Label(rect, value, style);
            GUI.color = previous; style.normal.textColor = original;
        }
        private static void Fill(Rect rect, Color color)
        { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    }
}
