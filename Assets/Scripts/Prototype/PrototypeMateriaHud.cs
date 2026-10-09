using UnityEngine;
using DragonQuest.Equipment;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeMateriaHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private Texture2D pixel;
        private GUIStyle title, text, small, button;
        private int memberIndex;
        private MateriaInstance selected;
        private EquipmentLoadout selectedEquipment;
        private int selectedSlot = -1;
        private string message = "Escolha uma materia na lista e clique no encaixe de destino.";
        private Vector2 inventoryScroll, commandsScroll;

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
            GUI.Label(new Rect(48, 30, 850, 30), "EQUIPAMENTOS / MATERIAS", title);
            GUI.Label(new Rect(48, 66, 890, 26), "Escolha uma materia, depois um encaixe. Materias equipadas podem ser transferidas ou trocadas.", small);
            if (GUI.Button(new Rect(1000, 34, 225, 43), "Fechar / I ou Esc", button)) controller.CloseMenu();

            for (int i = 0; i < party.Members.Count; i++)
                if (GUI.Button(new Rect(30 + i * 220, 115, 205, 40), (memberIndex == i ? "> " : "") + party.Members[i].Name, button))
                { memberIndex = i; selectedEquipment = null; selectedSlot = -1; commandsScroll = Vector2.zero; }
            PartyMember member = party.Members[memberIndex];
            Panel(new Rect(30, 168, 1220, 65));
            GUI.Label(new Rect(48, 176, 1140, 25), member.Name + " / " + member.BaseStats.Role + "  |  " + member.AffinityDescription, text);
            GUI.Label(new Rect(48, 204, 1140, 24), "Ataque " + member.Attack + "  |  Defesa " + member.Defense
                + "  |  Magia " + member.BaseStats.Magic + "  |  Limit " + member.Limit.Charge + "%", small);

            Panel(new Rect(30, 245, 615, 209));
            DrawEquipment(member, member.Weapon, 254, party);
            DrawEquipment(member, member.Armor, 332, party);
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
            GUI.enabled = selectedEquipment != null && selectedSlot >= 0 && selectedEquipment.Sockets[selectedSlot] != null;
            if (GUI.Button(new Rect(405, 650, 220, 31), "Remover do encaixe", button))
            {
                party.TryUnequip(member, selectedEquipment, selectedSlot, out message);
                selected = null;
            }
            GUI.enabled = enabled;

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
                    + materia.Definition.Name + " / " + party.LocationOf(materia), button))
                { selected = materia; message = "Agora clique no encaixe de destino, em qualquer personagem."; }
                GUI.backgroundColor = previousColor;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(690, 620, 535, 65), selected == null
                ? "Os encaixes unidos por -- formam um par. Combos funcionam somente dentro do mesmo par. All e Infusao sozinhas nao concedem acao."
                : selected.Definition.Description, small);
            GUI.matrix = previous;
        }

        private void DrawEquipment(PartyMember member, EquipmentLoadout equipment, float y, PartyProgress party)
        {
            GUI.Label(new Rect(48, y, 570, 23), equipment.Definition.Name + " / base ATQ +" + equipment.Definition.AttackBonus
                + " DEF +" + equipment.Definition.DefenseBonus, small);
            for (int i = 0; i < equipment.Sockets.Count; i++)
            {
                int column = i % 2, row = i / 2;
                MateriaInstance materia = equipment.Sockets[i];
                Rect socket = new Rect(48 + column * 290, y + 27 + row * 37, 260, 31);
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
                if (column == 0) GUI.Label(new Rect(310, socket.y + 3, 26, 23), "--", small);
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
            button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            title.normal.textColor = new Color(0.96f, 0.8f, 0.48f);
            text.normal.textColor = Color.white; small.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }

        private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
