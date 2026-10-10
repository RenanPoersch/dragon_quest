using UnityEngine;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeMateriaHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private Texture2D pixel;
        private GUIStyle title, text, small, button;
        private int memberIndex;
        private int page;
        private MateriaInstance selected;
        private EquipmentLoadout pendingGear;
        private ConsumableDefinition selectedInventoryItem;
        private EquipmentLoadout selectedEquipment;
        private int selectedSlot = -1;
        private string message = "Escolha uma materia na lista e clique no encaixe de destino.";
        private Vector2 inventoryScroll, commandsScroll, socketsScroll, gearScroll;

        public void Initialize(PrototypeBattleController battleController) => controller = battleController;

        private void OnGUI()
        {
            if (controller == null || !controller.IsMenuOpen) return;
            EnsureStyles();
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.045f, 0.07f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            PartyProgress party = controller.Party;
            Panel(new Rect(30, 20, 1220, 82));
            GUI.Label(new Rect(48, 30, 850, 30), "INVENTARIO / " + new[] { "MATERIAS", "EQUIPAMENTOS", "ITENS" }[page], title);
            GUI.Label(new Rect(48, 66, 890, 26), page == 0
                ? "Escolha uma materia, depois um encaixe. Role os encaixes para ver equipamentos com mais pares."
                : page == 1 ? "Trocar ou remover equipamento devolve suas materias ao inventario. Os novos encaixes ficam vazios."
                : "Confira seu estoque aqui. Itens sao usados na batalha e suas quantidades permanecem entre encontros.", small);
            if (GUI.Button(new Rect(1000, 34, 225, 43), "Fechar / I ou Esc", button)) controller.CloseMenu();

            for (int i = 0; i < party.Members.Count; i++)
                if (GUI.Button(new Rect(30 + i * 220, 115, 205, 40), (memberIndex == i ? "> " : "") + party.Members[i].Name, button))
                { memberIndex = i; ResetSocketSelection(); commandsScroll = Vector2.zero; }
            string[] tabs = { "Materias", "Equipamentos", "Itens" };
            for (int i = 0; i < tabs.Length; i++)
                if (GUI.Button(new Rect(710 + i * 181, 115, 173, 40), (page == i ? "> " : "") + tabs[i], button))
                { page = i; selected = null; pendingGear = null; ResetSocketSelection(); }
            PartyMember member = party.Members[memberIndex];
            Panel(new Rect(30, 168, 1220, 65));
            GUI.Label(new Rect(48, 176, 1140, 25), member.Name + " / " + member.BaseStats.Role + "  |  " + member.AffinityDescription, text);
            GUI.Label(new Rect(48, 204, 1140, 24), "Ataque " + member.Attack + "  |  Defesa " + member.Defense
                + "  |  Magia " + member.Magic + "  |  Limit " + member.Limit.Charge + "%", small);

            if (page == 0) DrawMaterias(member, party);
            else if (page == 1) DrawGear(member, party);
            else DrawConsumables(party);
            GUI.matrix = previous;
        }

        private void DrawMaterias(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 245, 615, 209));
            float contentHeight = EquipmentHeight(member.Weapon) + EquipmentHeight(member.Armor);
            socketsScroll = GUI.BeginScrollView(new Rect(45, 252, 585, 192), socketsScroll, new Rect(0, 0, 550, contentHeight));
            DrawEquipment(member, member.Weapon, EquipmentSlot.Weapon, 0, party);
            DrawEquipment(member, member.Armor, EquipmentSlot.Armor, EquipmentHeight(member.Weapon), party);
            GUI.EndScrollView();
            DrawPreview(member, party);

            Panel(new Rect(670, 245, 580, 450));
            GUI.Label(new Rect(690, 255, 530, 28), "Materias da equipe", title);
            GUI.Label(new Rect(690, 286, 530, 26), "Verde: magia / Azul: suporte / Vermelha: summon / Roxa: fisico", small);
            inventoryScroll = GUI.BeginScrollView(new Rect(687, 318, 545, 290), inventoryScroll,
                new Rect(0, 0, 515, party.Materias.Count * 45));
            for (int i = 0; i < party.Materias.Count; i++)
            {
                MateriaInstance materia = party.Materias[i];
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = MateriaColor(materia.Definition.Type);
                if (GUI.Button(new Rect(0, i * 45, 509, 40), (selected == materia ? "> " : "")
                    + materia.Definition.Name + "\n" + party.LocationOf(materia), button))
                { selected = materia; message = "Agora clique no encaixe de destino, em qualquer personagem."; }
                GUI.backgroundColor = previousColor;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(690, 620, 535, 65), selected == null
                ? "Os encaixes unidos por -- formam um par. Combos funcionam somente dentro do mesmo par. All e Infusao sozinhas nao concedem acao."
                : selected.Definition.Description, small);
        }

        private void DrawPreview(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 467, 615, 228));
            GUI.Label(new Rect(48, 475, 550, 28), "Acoes disponiveis na proxima batalha", title);
            var commands = MateriaResolver.Resolve(member);
            commandsScroll = GUI.BeginScrollView(new Rect(45, 512, 585, 123), commandsScroll,
                new Rect(0, 0, 555, commands.Count * 28));
            for (int i = 0; i < commands.Count; i++)
                GUI.Label(new Rect(0, i * 28, 550, 27), commands[i].Name + "  |  " + commands[i].MpCost + " MP"
                    + (commands[i].UsesLimit ? "  |  Limit 100%" : "  |  Poder " + commands[i].Power), small);
            GUI.EndScrollView();
            GUI.Label(new Rect(48, 642, 345, 44), message, small);
            bool enabled = GUI.enabled;
            if (page == 0)
            {
                GUI.enabled = selectedEquipment != null && selectedSlot >= 0 && selectedEquipment.IsValidSocket(selectedSlot)
                    && (selectedEquipment == member.Weapon || selectedEquipment == member.Armor) && selectedEquipment.Sockets[selectedSlot] != null;
                if (GUI.Button(new Rect(405, 650, 220, 31), "Remover do encaixe", button))
                { party.TryUnequip(member, selectedEquipment, selectedSlot, out message); selected = null; }
            }
            else
            {
                GUI.enabled = pendingGear != null && pendingGear != member.Weapon && pendingGear != member.Armor;
                if (GUI.Button(new Rect(405, 650, 220, 31), "Equipar selecionado", button))
                { party.TryEquipEquipment(member, pendingGear, out message); ResetSocketSelection(); selected = null; }
            }
            GUI.enabled = enabled;
        }

        private void DrawGear(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 245, 615, 209));
            GUI.Label(new Rect(48, 252, 570, 29), "Arma: " + (member.Weapon?.Definition.Name ?? "nenhuma"), text);
            GUI.Label(new Rect(48, 286, 570, 29), "Armadura: " + (member.Armor?.Definition.Name ?? "nenhuma"), text);
            if (pendingGear != null)
            {
                EquipmentLoadout weapon = pendingGear.Definition.Slot == EquipmentSlot.Weapon ? pendingGear : member.Weapon;
                EquipmentLoadout armor = pendingGear.Definition.Slot == EquipmentSlot.Armor ? pendingGear : member.Armor;
                GUI.Label(new Rect(48, 320, 565, 52), "Ao equipar: ATQ " + member.Attack + " > " + member.AttackWith(weapon, armor)
                    + " / DEF " + member.Defense + " > " + member.DefenseWith(weapon, armor)
                    + " / MAG " + member.Magic + " > " + member.MagicWith(weapon, armor), text);
            }
            GUI.Label(new Rect(48, 379, 560, 24), "A troca devolve as materias. Monte seus pares na aba Materias.", small);
            bool enabled = GUI.enabled;
            GUI.enabled = member.Weapon != null;
            if (GUI.Button(new Rect(48, 411, 255, 31), "Remover arma", button))
            { party.TryRemoveEquipment(member, EquipmentSlot.Weapon, out message); ResetSocketSelection(); selected = null; }
            GUI.enabled = member.Armor != null;
            if (GUI.Button(new Rect(338, 411, 260, 31), "Remover armadura", button))
            { party.TryRemoveEquipment(member, EquipmentSlot.Armor, out message); ResetSocketSelection(); selected = null; }
            GUI.enabled = enabled;
            DrawPreview(member, party);

            Panel(new Rect(670, 245, 580, 450));
            GUI.Label(new Rect(690, 255, 530, 28), "Equipamentos da equipe", title);
            GUI.Label(new Rect(690, 286, 530, 26), "Uso livre por classe. Equipamento de outro aliado pode ser trocado.", small);
            gearScroll = GUI.BeginScrollView(new Rect(687, 318, 545, 290), gearScroll, new Rect(0, 0, 515, party.Equipments.Count * 52));
            for (int i = 0; i < party.Equipments.Count; i++)
            {
                EquipmentLoadout item = party.Equipments[i];
                if (GUI.Button(new Rect(0, i * 52, 509, 47), (pendingGear == item ? "> " : "") + item.Definition.Name
                    + " / " + (item.Definition.Slot == EquipmentSlot.Weapon ? "Arma" : "Armadura") + "\n"
                    + party.LocationOf(item) + " / ATQ +" + item.Definition.AttackBonus + " DEF +" + item.Definition.DefenseBonus
                    + " MAG +" + item.Definition.MagicBonus + " / " + item.Sockets.Count + " encaixes", button)) pendingGear = item;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(690, 620, 535, 65), "Selecione um equipamento para comparar atributos. Clique em Equipar selecionado para confirmar a troca e devolver as materias.", small);
        }

        private void DrawConsumables(PartyProgress party)
        {
            Panel(new Rect(30, 245, 615, 450));
            Panel(new Rect(670, 245, 580, 450));
            GUI.Label(new Rect(48, 255, 560, 30), "Estoque compartilhado da equipe", title);
            for (int i = 0; i < party.Consumables.Items.Count; i++)
            {
                ItemStack stack = party.Consumables.Items[i];
                if (GUI.Button(new Rect(48, 305 + i * 54, 570, 45), (selectedInventoryItem == stack.Definition ? "> " : "")
                    + stack.Definition.Name + " / Quantidade: " + stack.Quantity, button)) selectedInventoryItem = stack.Definition;
            }
            GUI.Label(new Rect(48, 595, 560, 67), "O uso acontece na batalha: escolha Itens e depois um alvo. Abrir a lista ou cancelar nao gasta item nem turno.", text);
            GUI.Label(new Rect(690, 255, 530, 30), selectedInventoryItem?.Name ?? "Escolha um item", title);
            GUI.Label(new Rect(690, 310, 530, 135), selectedInventoryItem?.Description
                ?? "Pocao recupera HP, eter recupera MP, antidoto remove veneno e Pluma da Fenix revive.", text);
            GUI.Label(new Rect(690, 483, 530, 130), "Itens nao recebem bonus de classe ou combos de materia. O estoque nao e restaurado a cada batalha. Compras entram na proxima etapa.", text);
        }

        private void ResetSocketSelection() { selectedEquipment = null; selectedSlot = -1; socketsScroll = Vector2.zero; }
        private static float EquipmentHeight(EquipmentLoadout equipment) => equipment == null || equipment.Sockets.Count == 0
            ? 45 : 41 + equipment.Sockets.Count / 2 * 37;

        private void DrawEquipment(PartyMember member, EquipmentLoadout equipment, EquipmentSlot slot, float y, PartyProgress party)
        {
            string kind = slot == EquipmentSlot.Weapon ? "Arma" : "Armadura";
            GUI.Label(new Rect(0, y, 540, 24), kind + ": " + (equipment?.Definition.Name ?? "nenhuma")
                + (equipment == null ? "" : " / " + equipment.Sockets.Count + " encaixes"), small);
            if (equipment == null || equipment.Sockets.Count == 0) return;
            for (int i = 0; i < equipment.Sockets.Count; i++)
            {
                int column = i % 2, row = i / 2;
                MateriaInstance materia = equipment.Sockets[i];
                Rect socket = new Rect(column * 285, y + 27 + row * 37, 250, 31);
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = materia == null ? Color.gray : MateriaColor(materia.Definition.Type);
                string label = (selectedEquipment == equipment && selectedSlot == i ? "> " : "")
                    + (i + 1) + ": " + (materia == null ? "Vazio" : materia.Definition.Name);
                if (GUI.Button(socket, label, button))
                {
                    selectedEquipment = equipment; selectedSlot = i;
                    if (selected != null)
                    { party.TryEquip(member, equipment, i, selected, out message); selected = null; }
                    else message = "Encaixe selecionado. Escolha uma materia ou remova a atual.";
                }
                GUI.backgroundColor = previous;
                if (column == 0) GUI.Label(new Rect(253, socket.y + 3, 28, 23), "--", small);
            }
        }

        private static Color MateriaColor(MateriaType type)
        {
            if (type == MateriaType.Magic) return new Color(0.25f, 0.8f, 0.4f);
            if (type == MateriaType.Support) return new Color(0.25f, 0.55f, 1f);
            if (type == MateriaType.Summon) return new Color(1f, 0.32f, 0.3f);
            return new Color(0.8f, 0.35f, 0.95f);
        }

        private void Panel(Rect rect) => Box(rect, new Color(0.075f, 0.12f, 0.18f));
        private void Box(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };
            title.normal.textColor = new Color(0.96f, 0.8f, 0.48f);
            text.normal.textColor = Color.white; small.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }

        private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
