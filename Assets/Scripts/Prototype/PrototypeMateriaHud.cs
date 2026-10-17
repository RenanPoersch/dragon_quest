using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using DragonQuest.Input;
using UnityEngine.InputSystem;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeMateriaHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private AncientSecrets secrets;
        private readonly ControllerMenu navigation = new ControllerMenu();
        private Texture2D pixel;
        private InventoryMenuPresentation presentation;
        private readonly ItemMenuSession itemMenu = new ItemMenuSession();
        private readonly ItemMenuPresentation itemScreen = new ItemMenuPresentation();
        private GUIStyle title, text, small, button, iconButton, socketButton;
        private int memberIndex;
        private int page = -1, selectedMenu, openedFrame;
        private bool wasOpen, acceptsInput;
        private Vector2 lastPointer;
        private MateriaInstance selected;
        private EquipmentLoadout pendingGear;
        private EquipmentLoadout selectedEquipment;
        private int selectedSlot = -1;
        private string message = "Escolha uma materia na lista e clique no encaixe de destino.";
        private Vector2 inventoryScroll, commandsScroll, socketsScroll, gearScroll;

        public void Initialize(PrototypeBattleController battleController, AncientSecrets ancientSecrets)
        {
            controller = battleController; secrets = ancientSecrets;
            controller.TryCancelInventorySelection = ReturnFromItemMenu;
        }

        private bool ReturnFromItemMenu()
        {
            if (page != 2) return false;
            if (itemMenu.ChoosingTarget) itemMenu.CancelTarget(); else page = -1;
            return true;
        }

        private void Update()
        {
            if (controller == null || !controller.IsMenuOpen) { wasOpen = false; return; }
            CheckOpened();
            if (controller.IsCheatMenuBlocking || !GameInput.IsFocused || openedFrame == Time.frameCount) return;
            if (page == 2) { UpdateItemInput(); return; }
            if (page >= 0) return;
            MenuDirection direction = GameInput.Navigation;
            Keyboard keys = Keyboard.current;
            int step = direction == MenuDirection.Up || direction == MenuDirection.Left ? -1
                : direction == MenuDirection.Down || direction == MenuDirection.Right ? 1 : 0;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) step = -1;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) step = 1;
            }
            selectedMenu = (selectedMenu + step + InventoryMenuPresentation.Commands.Length) % InventoryMenuPresentation.Commands.Length;
            if (GameInput.ConfirmPressed) { OpenCommand(selectedMenu); GameInput.ConsumeGamepadFrame(); }
        }

        private void CheckOpened()
        {
            if (wasOpen) return;
            wasOpen = true; page = -1; selectedMenu = 0; openedFrame = Time.frameCount;
            selected = null; pendingGear = null; memberIndex = 0;
            message = ""; ResetSocketSelection();
            lastPointer = new Vector2(-1, -1);
        }

        private void OpenCommand(int index)
        {
            page = index == 0 ? 2 : index == 1 ? 0 : index == 2 ? 1 : index;
            selected = null; pendingGear = null; ResetSocketSelection();
            inventoryScroll = gearScroll = commandsScroll = Vector2.zero;
            message = "";
            if (page == 2) { itemMenu.Reset(controller.Party); itemScreen.Reset(); }
        }

        private void UpdateItemInput()
        {
            itemMenu.Refresh(controller.Party);
            // The controller owns cancellation so the same press cannot close the screen as well.
            if (GameInput.CancelPressed || GameInput.InventoryPressed) return;
            MenuDirection direction = GameInput.Navigation;
            Keyboard keys = Keyboard.current;
            int step = direction == MenuDirection.Up || direction == MenuDirection.Left ? -1
                : direction == MenuDirection.Down || direction == MenuDirection.Right ? 1 : 0;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) step = -1;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0) itemMenu.Move(step, controller.Party);
            if (!GameInput.ConfirmPressed) return;
            if (itemMenu.ChoosingTarget) itemMenu.ConfirmTarget(controller.Party);
            else itemMenu.BeginUse(controller.Party);
            GameInput.ConsumeGamepadFrame();
        }

        private void OnGUI()
        {
            if (controller == null || !controller.IsMenuOpen) { wasOpen = false; return; }
            CheckOpened();
            EnsureStyles();
            Box(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            acceptsInput = previousEnabled && !controller.IsCheatMenuBlocking && GameInput.IsFocused;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            PartyProgress party = controller.Party;
            if (page < 0)
            {
                DrawOverview(party);
                GUI.matrix = previous; GUI.enabled = previousEnabled;
                return;
            }
            if (page == 2)
            {
                itemScreen.Draw(presentation, itemMenu, party, secrets != null && secrets.HasScale, acceptsInput, () => page = -1);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            navigation.Begin(!acceptsInput);
            Panel(new Rect(30, 20, 1220, 82));
            string heading = page == 0 ? "Magic" : page == 1 ? "Equip" : page == 2 ? "Item" : page == 3 ? "Status" : "Config";
            GUI.Label(new Rect(48, 30, 850, 30), heading, title);
            GUI.Label(new Rect(48, 66, 890, 26), page == 0
                ? "Escolha uma materia, depois um encaixe. Role os encaixes para ver equipamentos com mais pares."
                : page == 1 ? "Trocar ou remover equipamento devolve suas materias ao inventario. Os novos encaixes ficam vazios."
                : page == 2 ? "Escolha um personagem e um item para usar. HP, MP e condicoes permanecem entre encontros."
                : page == 3 ? "Atributos e condicoes atuais da equipe." : "Ajuste o audio e a exibicao do jogo.", small);
            if (NavigationButton(new Rect(1000, 34, 225, 43), "Voltar", button)) page = -1;

            if (page == 4)
            {
                DrawConfig(); navigation.End(); GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            for (int i = 0; i < party.Members.Count; i++)
                if (NavigationButton(new Rect(30 + i * 220, 115, 205, 40), (memberIndex == i ? "> " : "") + party.Members[i].Name, button))
                { memberIndex = i; pendingGear = null; ResetSocketSelection(); commandsScroll = Vector2.zero; }
            PartyMember member = party.Members[memberIndex];
            Panel(new Rect(30, 168, 1220, 65));
            GUI.Label(new Rect(48, 176, 1140, 25), member.Name + " / " + member.BaseStats.Role + "  |  " + member.AffinityDescription, text);
            GUI.Label(new Rect(48, 204, 1140, 24), "Ataque " + member.Attack + "  |  Defesa " + member.Defense
                + "  |  HP " + member.Hp + "/" + member.MaxHp + "  |  MP " + member.Mp + "/" + member.MaxMp + "  |  Magia " + member.Magic + "  |  Limit " + member.Limit.Charge + "%", small);

            if (page == 0) DrawMaterias(member, party);
            else if (page == 1) DrawGear(member, party);
            else if (page == 3) DrawStatus(member);
            navigation.End();
            GUI.matrix = previous; GUI.enabled = previousEnabled;
        }

        private void DrawOverview(PartyProgress party)
        {
            var saves = GetComponent<PrototypeSaveController>();
            var shops = GetComponent<PrototypeShopController>();
            var locations = GetComponent<PrototypeLocations>();
            presentation.DrawOverview(party, saves != null ? saves.PlaySeconds : 0,
                shops?.Service?.Progress.Gold ?? 0, locations != null ? locations.LocationName : "ILHA / MAPA GLOBAL");
            Vector2 pointer = Event.current.mousePosition;
            bool moved = lastPointer.x >= 0 && (pointer - lastPointer).sqrMagnitude > .1f;
            for (int i = 0; i < InventoryMenuPresentation.Commands.Length; i++)
            {
                Rect rect = InventoryMenuPresentation.CommandRect(i);
                if (acceptsInput && moved && rect.Contains(pointer)) selectedMenu = i;
                bool enabled = GUI.enabled; GUI.enabled = enabled && acceptsInput;
                bool clicked = GUI.Button(rect, GUIContent.none, presentation.CommandStyle);
                GUI.enabled = enabled;
                GUI.Label(rect, InventoryMenuPresentation.Commands[i], presentation.CommandStyle);
                if (clicked)
                { selectedMenu = i; OpenCommand(i); GameInput.ConsumeGamepadFrame(); }
            }
            if (Event.current.type == EventType.Repaint) lastPointer = pointer;
            presentation.DrawHand(selectedMenu);
            GUI.Label(new Rect(34, 676, 610, 28), "I / Esc: fechar     Setas: selecionar     Enter: abrir", small);
        }

        private void DrawStatus(PartyMember member)
        {
            Panel(new Rect(30, 245, 615, 450)); Panel(new Rect(670, 245, 580, 450));
            presentation.DrawPortrait(new Rect(60, 276, 150, 150), member);
            GUI.Label(new Rect(238, 276, 380, 34), member.Name + " / Nivel " + member.Level, title);
            GUI.Label(new Rect(238, 320, 380, 100), "HP " + member.Hp + " / " + member.MaxHp
                + "\nMP " + member.Mp + " / " + member.MaxMp + "\nLimit " + member.Limit.Charge + "%", text);
            GUI.Label(new Rect(60, 452, 548, 170), "Ataque    " + member.Attack + "\nDefesa    " + member.Defense
                + "\nMagia     " + member.Magic + "\nVelocidade    " + member.Speed
                + "\nCritico    " + (member.CriticalChance * 100).ToString("0.#") + "%", text);
            GUI.Label(new Rect(60, 632, 548, 40), !member.IsAlive ? "Condicao: KO"
                : member.IsPoisoned ? "Condicao: Veneno" : member.BoundActions > 0 ? "Condicao: Preso" : "Condicao: Normal", small);
            GUI.Label(new Rect(694, 266, 530, 34), "Experiencia", title);
            GUI.Label(new Rect(694, 310, 530, 78), "EXP total: " + member.Experience + "\nProximo nivel: "
                + (member.Level == CharacterProgression.MaxLevel ? "MAX" : member.Progression.ExperienceToNextLevel.ToString()), text);
            GUI.Label(new Rect(694, 406, 530, 32), "Equipamento", title);
            GUI.Label(new Rect(694, 450, 530, 75), (member.Weapon?.Definition.Name ?? "Sem arma")
                + "\n" + (member.Armor?.Definition.Name ?? "Sem armadura"), text);
            GUI.Label(new Rect(694, 544, 530, 115), member.Affinities.Description + "\n" + member.AffinityDescription, small);
        }

        private void DrawConfig()
        {
            Panel(new Rect(30, 124, 1220, 571));
            GUI.Label(new Rect(64, 158, 700, 40), "Volume    " + Mathf.RoundToInt(AudioListener.volume * 100) + "%", title);
            if (NavigationButton(new Rect(750, 154, 205, 44), "Diminuir", button))
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume - .1f);
            if (NavigationButton(new Rect(975, 154, 205, 44), "Aumentar", button))
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + .1f);
            GUI.Label(new Rect(64, 246, 650, 40), "Tela cheia    " + (Screen.fullScreen ? "Ativada" : "Desativada"), title);
            if (NavigationButton(new Rect(750, 242, 430, 44), "Alternar tela cheia", button)) Screen.fullScreen = !Screen.fullScreen;
            GUI.Label(new Rect(64, 360, 1100, 36), "Teclado: WASD / setas movem; E confirma; I abre o inventario; Esc fecha.", text);
            GUI.Label(new Rect(64, 420, 1100, 72), "Controle: direcional / analogico seleciona; A / X confirma; B / Circulo fecha.\nX / Quadrado ou Start / Options abre o inventario.", text);
            GUI.Label(new Rect(64, 588, 1100, 54), "Os ajustes de audio e tela valem para esta sessao.", small);
        }

        private void DrawMaterias(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 245, 615, 209));
            float contentHeight = EquipmentHeight(member.Weapon) + EquipmentHeight(member.Armor);
            socketsScroll = navigation.BeginScrollView(new Rect(45, 252, 585, 192), socketsScroll, new Rect(0, 0, 550, contentHeight));
            DrawEquipment(member, member.Weapon, EquipmentSlot.Weapon, 0, party);
            DrawEquipment(member, member.Armor, EquipmentSlot.Armor, EquipmentHeight(member.Weapon), party);
            navigation.EndScrollView();
            DrawPreview(member, party);

            Panel(new Rect(670, 245, 580, 450));
            GUI.Label(new Rect(690, 255, 530, 28), "Materias da equipe", title);
            GUI.Label(new Rect(690, 286, 530, 26), "Verde: magia / Azul: suporte / Vermelha: summon / Roxa: fisico", small);
            inventoryScroll = navigation.BeginScrollView(new Rect(687, 318, 545, 290), inventoryScroll,
                new Rect(0, 0, 515, party.Materias.Count * 45));
            for (int i = 0; i < party.Materias.Count; i++)
            {
                MateriaInstance materia = party.Materias[i];
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = MateriaColor(materia.Definition.Type);
                if (NavigationButton(new Rect(0, i * 45, 509, 40), (selected == materia ? "> " : "")
                    + materia.Definition.Name + "\n" + party.LocationOf(materia), iconButton))
                { selected = materia; message = "Agora clique no encaixe de destino, em qualquer personagem."; }
                GUI.backgroundColor = previousColor;
                InventoryPixelArt.Draw(new Rect(8, i * 45 + 6, 28, 28), InventoryPixelArt.Texture(materia.Definition));
            }
            navigation.EndScrollView();
            GUI.Label(new Rect(690, 620, 535, 65), selected == null
                ? "Os encaixes unidos por -- formam um par. Combos funcionam somente dentro do mesmo par. All e Infusao sozinhas nao concedem acao."
                : selected.Definition.Description, small);
        }

        private void DrawPreview(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 467, 615, 228));
            GUI.Label(new Rect(48, 475, 550, 28), "Acoes disponiveis na proxima batalha", title);
            var commands = MateriaResolver.Resolve(member);
            commandsScroll = navigation.BeginScrollView(new Rect(45, 512, 585, 123), commandsScroll,
                new Rect(0, 0, 555, commands.Count * 28));
            for (int i = 0; i < commands.Count; i++)
                GUI.Label(new Rect(0, i * 28, 550, 27), commands[i].Name + "  |  " + commands[i].MpCost + " MP"
                    + (commands[i].UsesLimit ? "  |  Limit 100%" : "  |  Poder " + commands[i].Power)
                    + (commands[i].Element == DamageElement.None ? "" : "  |  " + ElementalAffinities.NameOf(commands[i].Element)), small);
            navigation.EndScrollView();
            GUI.Label(new Rect(48, 642, 345, 44), message, small);
            bool enabled = GUI.enabled;
            if (page == 0)
            {
                GUI.enabled = selectedEquipment != null && selectedSlot >= 0 && selectedEquipment.IsValidSocket(selectedSlot)
                    && (selectedEquipment == member.Weapon || selectedEquipment == member.Armor) && selectedEquipment.Sockets[selectedSlot] != null;
                if (NavigationButton(new Rect(405, 650, 220, 31), "Remover do encaixe", button))
                { party.TryUnequip(member, selectedEquipment, selectedSlot, out message); selected = null; }
            }
            else
            {
                GUI.enabled = pendingGear != null && pendingGear != member.Weapon && pendingGear != member.Armor;
                if (NavigationButton(new Rect(405, 650, 220, 31), "Equipar selecionado", button))
                { party.TryEquipEquipment(member, pendingGear, out message); ResetSocketSelection(); selected = null; }
            }
            GUI.enabled = enabled;
        }

        private void DrawGear(PartyMember member, PartyProgress party)
        {
            Panel(new Rect(30, 245, 615, 209));
            InventoryPixelArt.Draw(new Rect(48, 250, 32, 32), InventoryPixelArt.Texture(member.Weapon?.Definition));
            InventoryPixelArt.Draw(new Rect(48, 284, 32, 32), InventoryPixelArt.Texture(member.Armor?.Definition));
            GUI.Label(new Rect(90, 252, 528, 29), "Arma: " + (member.Weapon?.Definition.Name ?? "nenhuma"), text);
            GUI.Label(new Rect(90, 286, 528, 29), "Armadura: " + (member.Armor?.Definition.Name ?? "nenhuma"), text);
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
            if (NavigationButton(new Rect(48, 411, 255, 31), "Remover arma", button))
            { party.TryRemoveEquipment(member, EquipmentSlot.Weapon, out message); ResetSocketSelection(); selected = null; }
            GUI.enabled = member.Armor != null;
            if (NavigationButton(new Rect(338, 411, 260, 31), "Remover armadura", button))
            { party.TryRemoveEquipment(member, EquipmentSlot.Armor, out message); ResetSocketSelection(); selected = null; }
            GUI.enabled = enabled;
            DrawPreview(member, party);

            Panel(new Rect(670, 245, 580, 450));
            GUI.Label(new Rect(690, 255, 530, 28), "Equipamentos da equipe", title);
            GUI.Label(new Rect(690, 286, 530, 26), "Uso livre por classe. Equipamento de outro aliado pode ser trocado.", small);
            gearScroll = navigation.BeginScrollView(new Rect(687, 318, 545, 290), gearScroll, new Rect(0, 0, 515, party.Equipments.Count * 52));
            for (int i = 0; i < party.Equipments.Count; i++)
            {
                EquipmentLoadout item = party.Equipments[i];
                if (NavigationButton(new Rect(0, i * 52, 509, 47), (pendingGear == item ? "> " : "") + item.Definition.Name
                    + " / " + (item.Definition.Slot == EquipmentSlot.Weapon ? "Arma" : "Armadura") + "\n"
                    + party.LocationOf(item) + " / ATQ +" + item.Definition.AttackBonus + " DEF +" + item.Definition.DefenseBonus
                    + " MAG +" + item.Definition.MagicBonus + " / " + item.Sockets.Count + " encaixes", iconButton)) pendingGear = item;
                InventoryPixelArt.Draw(new Rect(6, i * 52 + 5, 38, 38), InventoryPixelArt.Texture(item.Definition));
            }
            navigation.EndScrollView();
            if (pendingGear != null) InventoryPixelArt.Draw(new Rect(690, 618, 64, 64), InventoryPixelArt.Texture(pendingGear.Definition));
            GUI.Label(new Rect(pendingGear == null ? 690 : 770, 620, pendingGear == null ? 535 : 455, 65), pendingGear == null
                ? "Selecione um equipamento para comparar atributos. A troca devolve as materias."
                : pendingGear.Definition.Affinities.Description + (pendingGear.Definition.IsStoryItem ? " / Item de historia: protegido contra venda." : ""), small);
        }

        private void ResetSocketSelection() { selectedEquipment = null; selectedSlot = -1; socketsScroll = Vector2.zero; }
        private static float EquipmentHeight(EquipmentLoadout equipment) => equipment == null || equipment.Sockets.Count == 0
            ? 45 : 41 + equipment.Sockets.Count / 2 * 37;

        private void DrawEquipment(PartyMember member, EquipmentLoadout equipment, EquipmentSlot slot, float y, PartyProgress party)
        {
            string kind = slot == EquipmentSlot.Weapon ? "Arma" : "Armadura";
            InventoryPixelArt.Draw(new Rect(0, y, 24, 24), InventoryPixelArt.Texture(equipment?.Definition));
            GUI.Label(new Rect(32, y, 508, 24), kind + ": " + (equipment?.Definition.Name ?? "nenhuma")
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
                if (NavigationButton(socket, label, socketButton))
                {
                    selectedEquipment = equipment; selectedSlot = i;
                    if (selected != null)
                    { party.TryEquip(member, equipment, i, selected, out message); selected = null; }
                    else message = "Encaixe selecionado. Escolha uma materia ou remova a atual.";
                }
                GUI.backgroundColor = previous;
                InventoryPixelArt.Draw(new Rect(socket.x + 6, socket.y + 3, 24, 24), InventoryPixelArt.Texture(materia?.Definition));
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

        private void Panel(Rect rect) => presentation.Panel(rect);
        private bool NavigationButton(Rect rect, string label, GUIStyle style)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && acceptsInput;
            bool clicked = navigation.Button(rect, label, style);
            GUI.enabled = previous; return clicked;
        }
        private void Box(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            presentation = new InventoryMenuPresentation(); presentation.EnsureStyles();
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            text = PrototypeTypography.Style(GUI.skin.label, 24); text.wordWrap = true;
            small = PrototypeTypography.Style(GUI.skin.label, 20); small.wordWrap = true;
            button = PrototypeTypography.Style(GUI.skin.button, 22); button.wordWrap = true;
            button.normal.background = button.hover.background = button.active.background = button.focused.background = null;
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = Color.white;
            iconButton = InventoryPixelArt.ButtonStyle(PrototypeTypography.Style(button, 16));
            socketButton = InventoryPixelArt.ButtonStyle(button, 38);
            title.normal.textColor = Color.white;
            text.normal.textColor = Color.white; small.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }

        private void OnDestroy()
        {
            if (controller != null && controller.TryCancelInventorySelection == ReturnFromItemMenu)
                controller.TryCancelInventorySelection = null;
            presentation?.Dispose(); if (pixel != null) Destroy(pixel);
        }
    }
}
