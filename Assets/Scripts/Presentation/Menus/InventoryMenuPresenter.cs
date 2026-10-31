using DragonQuest.Presentation.Common;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay;
using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using DragonQuest.Input;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DragonQuest.Presentation.Menus
{
    public sealed class InventoryMenuPresenter : System.IDisposable
    {
        private BattleController controller;
        private AncientSecrets secrets;
        private readonly ControllerMenu navigation = new ControllerMenu();
        private Texture2D pixel;
        private InventoryMenuPresentation presentation;
        private readonly ItemMenuSession itemMenu = new ItemMenuSession();
        private readonly ItemMenuPresentation itemScreen = new ItemMenuPresentation();
        private readonly MagicMenuSession magicMenu = new MagicMenuSession();
        private readonly MagicMenuPresentation magicScreen = new MagicMenuPresentation();
        private readonly StatusMenuPresentation statusScreen = new StatusMenuPresentation();
        private readonly MateriaMenuSession materiaMenu = new MateriaMenuSession();
        private readonly MateriaMenuPresentation materiaScreen = new MateriaMenuPresentation();
        private readonly EquipmentMenuSession equipmentMenu = new EquipmentMenuSession();
        private readonly EquipmentMenuPresentation equipmentScreen = new EquipmentMenuPresentation();
        private GUIStyle title, button;
        private int memberIndex;
        private InventoryPage page = InventoryPage.Overview;
        private int selectedMenu, openedFrame;
        private bool wasOpen, acceptsInput;
        private Vector2 lastPointer;

        private readonly System.Func<SaveController> getSaves;
        private bool IsInputBlocked => controller.IsCheatMenuBlocking
            || controller.Flow != null && controller.Flow.Owner != GameInputOwner.Inventory;
        private readonly System.Func<int> getGold;
        private readonly System.Func<string> getLocationName;
        public InventoryMenuPresenter(BattleController battleController, AncientSecrets ancientSecrets,
            System.Func<SaveController> getSaves, System.Func<int> getGold, System.Func<string> getLocationName)
        {
            controller = battleController; secrets = ancientSecrets; this.getSaves = getSaves;
            this.getGold = getGold; this.getLocationName = getLocationName;
            controller.TryCancelInventorySelection = ReturnFromInventoryMenu;
        }

        private bool ReturnFromInventoryMenu()
        {
            if (page != InventoryPage.Overview) MenuSounds.Play(MenuSound.Cancel);
            if (page == InventoryPage.Materia)
            {
                if (materiaMenu.ChoosingSocket) materiaMenu.ShowEquip(); else page = InventoryPage.Overview;
                return true;
            }
            if (page == InventoryPage.Magic) { if (!magicMenu.Cancel()) page = InventoryPage.Overview; return true; }
            if (page == InventoryPage.Equipment || page == InventoryPage.Status || page == InventoryPage.Config) { page = InventoryPage.Overview; return true; }
            if (page != InventoryPage.Items) return false;
            if (itemMenu.ChoosingTarget) itemMenu.CancelTarget(); else page = InventoryPage.Overview;
            return true;
        }

        public void Update()
        {
            if (controller == null || !controller.IsMenuOpen)
            { if (wasOpen) MenuSounds.Play(MenuSound.Cancel); wasOpen = false; return; }
            CheckOpened();
            if (IsInputBlocked || !GameInput.IsFrameAvailable || openedFrame == Time.frameCount) return;
            if (page == InventoryPage.Config) return;
            MenuInput input = GameInput.ReadMenu();
            if (page == InventoryPage.Materia) { UpdateMateriaInput(input); return; }
            if (page == InventoryPage.Equipment) { UpdateEquipmentInput(input); return; }
            if (page == InventoryPage.Items) { UpdateItemInput(input); return; }
            if (page == InventoryPage.Status) { UpdateStatusInput(input); return; }
            if (page == InventoryPage.Magic) { UpdateMagicInput(input); return; }
            if (page != InventoryPage.Overview) return;
            int step = input.Step;
            if (step != 0)
            {
                int next = selectedMenu;
                do { next = (next + step + InventoryMenuPresentation.Commands.Length) % InventoryMenuPresentation.Commands.Length; }
                while (!CommandEnabled(next) && next != selectedMenu);
                MenuSounds.Select(ref selectedMenu, next);
            }
            if (input.Confirm) { OpenCommand(selectedMenu); GameInput.ConsumeFrame(); }
        }

        private void CheckOpened()
        {
            if (wasOpen) return;
            MenuSounds.Play(MenuSound.Confirm);
            wasOpen = true; page = InventoryPage.Overview; selectedMenu = 0; openedFrame = Time.frameCount;
            memberIndex = 0;
            lastPointer = new Vector2(-1, -1);
        }

        private void OpenCommand(int index)
        {
            if (!CommandEnabled(index)) return;
            MenuSounds.Play(MenuSound.Confirm);
            var option = InventoryMenuPresentation.CommandAt(index);
            if (option.Command == InventoryCommand.Save)
            {
                getSaves().OpenManualSaveMenu();
                return;
            }
            if (option.Command == InventoryCommand.Exit)
            {
                getSaves().ReturnToTitle();
                return;
            }
            page = option.Page;
            if (page == InventoryPage.Materia) { materiaMenu.Reset(controller.Party); materiaScreen.Reset(); }
            if (page == InventoryPage.Equipment) { equipmentMenu.Reset(controller.Party, memberIndex); equipmentScreen.Reset(); }
            if (page == InventoryPage.Items) { itemMenu.Reset(controller.Party); itemScreen.Reset(); }
            if (page == InventoryPage.Status) statusScreen.Reset();
            if (page == InventoryPage.Magic) { magicMenu.Reset(controller.Party); magicScreen.Reset(); }
        }

        private bool CommandEnabled(int index)
        {
            if (index < 0 || index >= InventoryMenuPresentation.Commands.Length) return false;
            switch (InventoryMenuPresentation.CommandAt(index).Command)
            {
                case InventoryCommand.Save: return getSaves()?.CanSaveManually == true;
                case InventoryCommand.Exit: return getSaves()?.CanReturnToTitle == true;
                default: return true;
            }
        }

        private void UpdateMagicInput(MenuInput input)
        {
            magicMenu.Refresh(controller.Party);
            if (input.Cancel || GameInput.InventoryPressed) return;
            int step = input.Step;
            if (step != 0)
            {
                var before = (magicMenu.CasterIndex, magicMenu.SpellIndex, magicMenu.TargetIndex);
                magicMenu.Move(step, controller.Party);
                MenuSounds.Navigate(before, (magicMenu.CasterIndex, magicMenu.SpellIndex, magicMenu.TargetIndex));
            }
            if (!input.Confirm) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (magicMenu.ChoosingCaster) magicMenu.ConfirmCaster();
            else if (magicMenu.ChoosingTarget) magicMenu.ConfirmTarget(controller.Party);
            else magicMenu.BeginUse(controller.Party);
            GameInput.ConsumeFrame();
        }

        private void ChangeStatusMember(int step)
        {
            int count = controller.Party.Members.Count;
            MenuSounds.Select(ref memberIndex, (memberIndex + step + count) % count);
            statusScreen.Reset();
        }

        private void UpdateStatusInput(MenuInput input)
        {
            if (input.Cancel || GameInput.InventoryPressed) return;
            if (input.Step != 0) ChangeStatusMember(input.Step);
        }

        private void UpdateItemInput(MenuInput input)
        {
            itemMenu.Refresh(controller.Party);
            // The controller owns cancellation so the same press cannot close the screen as well.
            if (input.Cancel || GameInput.InventoryPressed) return;
            if (input.Horizontal != 0 && !itemMenu.ChoosingTarget)
            {
                int previous = itemMenu.SubmenuIndex;
                itemMenu.MoveSubmenu(input.Horizontal, controller.Party);
                MenuSounds.Navigate(previous, itemMenu.SubmenuIndex);
                GameInput.ConsumeFrame(); return;
            }
            int step = input.Step;
            if (step != 0)
            {
                itemMenu.FocusContent();
                var before = (itemMenu.ItemIndex, itemMenu.TargetIndex);
                itemMenu.Move(step, controller.Party);
                MenuSounds.Navigate(before, (itemMenu.ItemIndex, itemMenu.TargetIndex));
            }
            if (!input.Confirm) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (itemMenu.BrowsingSubmenus) itemMenu.FocusContent();
            else if (itemMenu.ChoosingTarget) itemMenu.ConfirmTarget(controller.Party);
            else itemMenu.BeginUse(controller.Party);
            GameInput.ConsumeFrame();
        }

        private void UpdateMateriaInput(MenuInput input)
        {
            materiaMenu.Refresh(controller.Party);
            if (input.Cancel || GameInput.InventoryPressed) return;
            if (GameInput.KeyPressed(Key.Delete) || GameInput.KeyPressed(Key.Backspace)
                || GameInput.GamepadButtonPressed(GamepadButton.North))
            { materiaMenu.BeginRemove(controller.Party); MenuSounds.Play(MenuSound.Confirm); GameInput.ConsumeFrame(); return; }
            if (input.Horizontal != 0 && (materiaMenu.BrowsingSubmenus || !materiaMenu.ChoosingSocket))
            {
                int previous = materiaMenu.SubmenuIndex;
                materiaMenu.MoveSubmenu(input.Horizontal, controller.Party);
                MenuSounds.Navigate(previous, materiaMenu.SubmenuIndex);
                GameInput.ConsumeFrame(); return;
            }
            var before = (materiaMenu.MateriaIndex, materiaMenu.SocketIndex);
            if (input.Vertical != 0)
            {
                materiaMenu.FocusContent();
                materiaMenu.MoveVertical(input.Vertical);
            }
            else if (input.Horizontal != 0) materiaMenu.Move(input.Horizontal);
            MenuSounds.Navigate(before, (materiaMenu.MateriaIndex, materiaMenu.SocketIndex));
            if (!input.Confirm) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (materiaMenu.BrowsingSubmenus) materiaMenu.FocusContent();
            else if (materiaMenu.ChoosingSocket) materiaMenu.ConfirmSocket(controller.Party);
            else if (materiaMenu.SubmenuIndex == 1) materiaMenu.BeginRemove(controller.Party);
            else materiaMenu.BeginEquip(controller.Party);
            GameInput.ConsumeFrame();
        }

        private void UpdateEquipmentInput(MenuInput input)
        {
            equipmentMenu.Refresh(controller.Party);
            if (input.Cancel || GameInput.InventoryPressed) return;
            int memberStep = GameInput.KeyPressed(Key.Q) || GameInput.KeyPressed(Key.PageUp)
                || GameInput.GamepadButtonPressed(GamepadButton.LeftShoulder) ? -1
                : GameInput.KeyPressed(Key.R) || GameInput.KeyPressed(Key.PageDown)
                || GameInput.GamepadButtonPressed(GamepadButton.RightShoulder) ? 1 : 0;
            if (memberStep != 0)
            {
                int previous = equipmentMenu.MemberIndex; equipmentMenu.ChangeMember(memberStep, controller.Party);
                MenuSounds.Navigate(previous, equipmentMenu.MemberIndex); GameInput.ConsumeFrame(); return;
            }
            var before = (equipmentMenu.ItemIndex, equipmentMenu.Category);
            if (input.Vertical != 0) equipmentMenu.Move(input.Vertical);
            else if (input.Horizontal != 0) equipmentMenu.ChangeCategory(input.Horizontal, controller.Party);
            MenuSounds.Navigate(before, (equipmentMenu.ItemIndex, equipmentMenu.Category));
            if (!input.Confirm) return;
            equipmentMenu.Apply(controller.Party); MenuSounds.Play(MenuSound.Confirm); GameInput.ConsumeFrame();
        }

        public void Draw()
        {
            if (controller == null || !controller.IsMenuOpen)
            { if (wasOpen) MenuSounds.Play(MenuSound.Cancel); wasOpen = false; return; }
            CheckOpened();
            EnsureStyles();
            Box(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            acceptsInput = previousEnabled && !IsInputBlocked && GameInput.IsFocused;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            PartyProgress party = controller.Party;
            if (page == InventoryPage.Overview)
            {
                DrawOverview(party);
                GUI.matrix = previous; GUI.enabled = previousEnabled;
                return;
            }
            if (page == InventoryPage.Materia)
            {
                materiaScreen.Draw(presentation, materiaMenu, party, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == InventoryPage.Equipment)
            {
                equipmentScreen.Draw(presentation, equipmentMenu, party, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == InventoryPage.Items)
            {
                itemScreen.Draw(presentation, itemMenu, party, secrets != null && secrets.HasScale, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == InventoryPage.Status)
            {
                statusScreen.Draw(presentation, party.Members[memberIndex], acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == InventoryPage.Magic)
            {
                magicScreen.Draw(presentation, magicMenu, party, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            navigation.Begin(!acceptsInput);
            Panel(new Rect(30, 20, 1220, 82));
            GUI.Label(new Rect(48, 30, 850, 30), "Config", title);
            if (NavigationButton(new Rect(1000, 34, 225, 43), "Voltar", button, MenuSound.Cancel)) BackToOverview();

            DrawConfig();
            navigation.End();
            GUI.matrix = previous; GUI.enabled = previousEnabled;
        }

        private void DrawOverview(PartyProgress party)
        {
            var saves = getSaves();
            presentation.DrawOverview(party, saves != null ? saves.PlaySeconds : 0,
                getGold(), getLocationName());
            Vector2 pointer = Event.current.mousePosition;
            bool moved = lastPointer.x >= 0 && (pointer - lastPointer).sqrMagnitude > .1f;
            for (int i = 0; i < InventoryMenuPresentation.Commands.Length; i++)
            {
                Rect rect = InventoryMenuPresentation.CommandRect(i);
                bool available = CommandEnabled(i);
                if (acceptsInput && available && moved && rect.Contains(pointer)) MenuSounds.Select(ref selectedMenu, i);
                bool enabled = GUI.enabled; GUI.enabled = enabled && acceptsInput && available;
                bool clicked = GUI.Button(rect, GUIContent.none, presentation.CommandStyle);
                GUI.enabled = enabled;
                Color color = GUI.color; if (!available) GUI.color = new Color(.48f, .5f, .64f);
                GUI.Label(rect, InventoryMenuPresentation.Commands[i], presentation.CommandStyle);
                GUI.color = color;
                if (clicked)
                { selectedMenu = i; OpenCommand(i); GameInput.ConsumeFrame(); }
            }
            if (Event.current.type == EventType.Repaint) lastPointer = pointer;
            presentation.DrawHand(selectedMenu);
        }

        private void BackToOverview()
        { page = InventoryPage.Overview; MenuSounds.Play(MenuSound.Cancel); }

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
        }

        private void Panel(Rect rect) => presentation.Panel(rect);
        private bool NavigationButton(Rect rect, string label, GUIStyle style, MenuSound sound = MenuSound.Confirm)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && acceptsInput;
            bool clicked = navigation.Button(rect, label, style, sound);
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
            title = GameTypography.Style(GUI.skin.label, 28, true);
            button = GameTypography.Style(GUI.skin.button, 22); button.wordWrap = true;
            button.normal.background = button.hover.background = button.active.background = button.focused.background = null;
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = Color.white;
            title.normal.textColor = Color.white;
        }

        public void Dispose()
        {
            if (controller != null && controller.TryCancelInventorySelection == ReturnFromInventoryMenu)
                controller.TryCancelInventorySelection = null;
            presentation?.Dispose(); if (pixel != null) UnityEngine.Object.Destroy(pixel);
        }
    }
}
