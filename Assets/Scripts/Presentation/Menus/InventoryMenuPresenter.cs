using DragonQuest.Prototype;
using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using DragonQuest.Input;
using UnityEngine.InputSystem;

namespace DragonQuest.Presentation.Menus
{
    public sealed class InventoryMenuPresenter : System.IDisposable
    {
        private PrototypeBattleController controller;
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
        private GUIStyle title, small, button;
        private int memberIndex;
        private int page = -1, selectedMenu, openedFrame;
        private bool wasOpen, acceptsInput;
        private Vector2 lastPointer;

        private readonly System.Func<PrototypeSaveController> getSaves;
        private readonly System.Func<int> getGold;
        private readonly System.Func<string> getLocationName;
        public InventoryMenuPresenter(PrototypeBattleController battleController, AncientSecrets ancientSecrets,
            System.Func<PrototypeSaveController> getSaves, System.Func<int> getGold, System.Func<string> getLocationName)
        {
            controller = battleController; secrets = ancientSecrets; this.getSaves = getSaves;
            this.getGold = getGold; this.getLocationName = getLocationName;
            controller.TryCancelInventorySelection = ReturnFromInventoryMenu;
        }

        private bool ReturnFromInventoryMenu()
        {
            if (page >= 0) MenuSounds.Play(MenuSound.Cancel);
            if (page == 0)
            {
                if (materiaMenu.ChoosingSocket) materiaMenu.ShowEquip(); else page = -1;
                return true;
            }
            if (page == 5) { if (!magicMenu.Cancel()) page = -1; return true; }
            if (page == 1 || page == 3 || page == 4) { page = -1; return true; }
            if (page != 2) return false;
            if (itemMenu.ChoosingTarget) itemMenu.CancelTarget(); else page = -1;
            return true;
        }

        public void Update()
        {
            if (controller == null || !controller.IsMenuOpen)
            { if (wasOpen) MenuSounds.Play(MenuSound.Cancel); wasOpen = false; return; }
            CheckOpened();
            if (controller.IsCheatMenuBlocking || !GameInput.IsFocused || openedFrame == Time.frameCount) return;
            if (page == 0) { UpdateMateriaInput(); return; }
            if (page == 1) { UpdateEquipmentInput(); return; }
            if (page == 2) { UpdateItemInput(); return; }
            if (page == 3) { UpdateStatusInput(); return; }
            if (page == 5) { UpdateMagicInput(); return; }
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
            if (step != 0)
            {
                int next = selectedMenu;
                do { next = (next + step + InventoryMenuPresentation.Commands.Length) % InventoryMenuPresentation.Commands.Length; }
                while (!CommandEnabled(next) && next != selectedMenu);
                MenuSounds.Select(ref selectedMenu, next);
            }
            if (GameInput.ConfirmPressed) { OpenCommand(selectedMenu); GameInput.ConsumeGamepadFrame(); }
        }

        private void CheckOpened()
        {
            if (wasOpen) return;
            MenuSounds.Play(MenuSound.Confirm);
            wasOpen = true; page = -1; selectedMenu = 0; openedFrame = Time.frameCount;
            memberIndex = 0;
            lastPointer = new Vector2(-1, -1);
        }

        private void OpenCommand(int index)
        {
            if (!CommandEnabled(index)) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (index == 6)
            {
                getSaves().OpenManualSaveMenu();
                return;
            }
            if (index == 7)
            {
                getSaves().ReturnToTitle();
                return;
            }
            page = index == 0 ? 2 : index == 1 ? 0 : index == 2 ? 1 : index == 4 ? 5 : index == 5 ? 4 : index;
            if (page == 0) { materiaMenu.Reset(controller.Party); materiaScreen.Reset(); }
            if (page == 1) { equipmentMenu.Reset(controller.Party, memberIndex); equipmentScreen.Reset(); }
            if (page == 2) { itemMenu.Reset(controller.Party); itemScreen.Reset(); }
            if (page == 3) statusScreen.Reset();
            if (page == 5) { magicMenu.Reset(controller.Party); magicScreen.Reset(); }
        }

        private bool CommandEnabled(int index) => index >= 0 && index < InventoryMenuPresentation.Commands.Length
            && (index != 6 || getSaves()?.CanSaveManually == true)
            && (index != 7 || getSaves()?.CanReturnToTitle == true);

        private void UpdateMagicInput()
        {
            magicMenu.Refresh(controller.Party);
            if (!GameInput.IsFrameAvailable || GameInput.CancelPressed || GameInput.InventoryPressed) return;
            MenuDirection direction = GameInput.Navigation;
            Keyboard keys = Keyboard.current;
            int step = direction == MenuDirection.Up || direction == MenuDirection.Left ? -1
                : direction == MenuDirection.Down || direction == MenuDirection.Right ? 1 : 0;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) step = -1;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0)
            {
                var before = (magicMenu.CasterIndex, magicMenu.SpellIndex, magicMenu.TargetIndex);
                magicMenu.Move(step, controller.Party);
                MenuSounds.Navigate(before, (magicMenu.CasterIndex, magicMenu.SpellIndex, magicMenu.TargetIndex));
            }
            if (!GameInput.ConfirmPressed) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (magicMenu.ChoosingCaster) magicMenu.ConfirmCaster();
            else if (magicMenu.ChoosingTarget) magicMenu.ConfirmTarget(controller.Party);
            else magicMenu.BeginUse(controller.Party);
            GameInput.ConsumeGamepadFrame();
        }

        private void ChangeStatusMember(int step)
        {
            int count = controller.Party.Members.Count;
            MenuSounds.Select(ref memberIndex, (memberIndex + step + count) % count);
            statusScreen.Reset();
        }

        private void UpdateStatusInput()
        {
            if (GameInput.CancelPressed || GameInput.InventoryPressed) return;
            MenuDirection direction = GameInput.Navigation;
            int step = direction == MenuDirection.Left || direction == MenuDirection.Up ? -1
                : direction == MenuDirection.Right || direction == MenuDirection.Down ? 1 : 0;
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) step = -1;
                else if (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0) ChangeStatusMember(step);
        }

        private void UpdateItemInput()
        {
            itemMenu.Refresh(controller.Party);
            // The controller owns cancellation so the same press cannot close the screen as well.
            if (!GameInput.IsFrameAvailable || GameInput.CancelPressed || GameInput.InventoryPressed) return;
            MenuDirection direction = GameInput.Navigation;
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) direction = MenuDirection.Up;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) direction = MenuDirection.Down;
                else if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame) direction = MenuDirection.Left;
                else if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) direction = MenuDirection.Right;
            }
            bool horizontal = direction == MenuDirection.Left || direction == MenuDirection.Right;
            if (horizontal && !itemMenu.ChoosingTarget)
            {
                int previous = itemMenu.SubmenuIndex;
                itemMenu.MoveSubmenu(direction == MenuDirection.Left ? -1 : 1, controller.Party);
                MenuSounds.Navigate(previous, itemMenu.SubmenuIndex);
                GameInput.ConsumeGamepadFrame(); return;
            }
            int step = direction == MenuDirection.Up || direction == MenuDirection.Left ? -1
                : direction == MenuDirection.Down || direction == MenuDirection.Right ? 1 : 0;
            if (step != 0)
            {
                itemMenu.FocusContent();
                var before = (itemMenu.ItemIndex, itemMenu.TargetIndex);
                itemMenu.Move(step, controller.Party);
                MenuSounds.Navigate(before, (itemMenu.ItemIndex, itemMenu.TargetIndex));
            }
            if (!GameInput.ConfirmPressed) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (itemMenu.BrowsingSubmenus) itemMenu.FocusContent();
            else if (itemMenu.ChoosingTarget) itemMenu.ConfirmTarget(controller.Party);
            else itemMenu.BeginUse(controller.Party);
            GameInput.ConsumeGamepadFrame();
        }

        private void UpdateMateriaInput()
        {
            materiaMenu.Refresh(controller.Party);
            if (!GameInput.IsFrameAvailable || GameInput.CancelPressed || GameInput.InventoryPressed) return;
            Keyboard keys = Keyboard.current;
            if (keys?.deleteKey.wasPressedThisFrame == true || keys?.backspaceKey.wasPressedThisFrame == true
                || Gamepad.current?.buttonNorth.wasPressedThisFrame == true)
            { materiaMenu.BeginRemove(controller.Party); MenuSounds.Play(MenuSound.Confirm); GameInput.ConsumeGamepadFrame(); return; }
            MenuDirection direction = GameInput.Navigation;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) direction = MenuDirection.Up;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) direction = MenuDirection.Down;
                else if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame) direction = MenuDirection.Left;
                else if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) direction = MenuDirection.Right;
            }
            bool horizontal = direction == MenuDirection.Left || direction == MenuDirection.Right;
            if (horizontal && (materiaMenu.BrowsingSubmenus || !materiaMenu.ChoosingSocket))
            {
                int previous = materiaMenu.SubmenuIndex;
                materiaMenu.MoveSubmenu(direction == MenuDirection.Left ? -1 : 1, controller.Party);
                MenuSounds.Navigate(previous, materiaMenu.SubmenuIndex);
                GameInput.ConsumeGamepadFrame(); return;
            }
            var before = (materiaMenu.MateriaIndex, materiaMenu.SocketIndex);
            if (direction == MenuDirection.Up || direction == MenuDirection.Down)
            {
                materiaMenu.FocusContent();
                materiaMenu.MoveVertical(direction == MenuDirection.Up ? -1 : 1);
            }
            else if (direction == MenuDirection.Left || direction == MenuDirection.Right)
                materiaMenu.Move(direction == MenuDirection.Left ? -1 : 1);
            MenuSounds.Navigate(before, (materiaMenu.MateriaIndex, materiaMenu.SocketIndex));
            if (!GameInput.ConfirmPressed) return;
            MenuSounds.Play(MenuSound.Confirm);
            if (materiaMenu.BrowsingSubmenus) materiaMenu.FocusContent();
            else if (materiaMenu.ChoosingSocket) materiaMenu.ConfirmSocket(controller.Party);
            else if (materiaMenu.SubmenuIndex == 1) materiaMenu.BeginRemove(controller.Party);
            else materiaMenu.BeginEquip(controller.Party);
            GameInput.ConsumeGamepadFrame();
        }

        private void UpdateEquipmentInput()
        {
            equipmentMenu.Refresh(controller.Party);
            if (!GameInput.IsFrameAvailable || GameInput.CancelPressed || GameInput.InventoryPressed) return;
            Keyboard keys = Keyboard.current; Gamepad pad = Gamepad.current;
            int memberStep = keys?.qKey.wasPressedThisFrame == true || keys?.pageUpKey.wasPressedThisFrame == true
                || pad?.leftShoulder.wasPressedThisFrame == true ? -1
                : keys?.rKey.wasPressedThisFrame == true || keys?.pageDownKey.wasPressedThisFrame == true
                || pad?.rightShoulder.wasPressedThisFrame == true ? 1 : 0;
            if (memberStep != 0)
            {
                int previous = equipmentMenu.MemberIndex; equipmentMenu.ChangeMember(memberStep, controller.Party);
                MenuSounds.Navigate(previous, equipmentMenu.MemberIndex); GameInput.ConsumeGamepadFrame(); return;
            }
            MenuDirection direction = GameInput.Navigation;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) direction = MenuDirection.Up;
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) direction = MenuDirection.Down;
                else if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame) direction = MenuDirection.Left;
                else if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) direction = MenuDirection.Right;
            }
            var before = (equipmentMenu.ItemIndex, equipmentMenu.Category);
            if (direction == MenuDirection.Up || direction == MenuDirection.Down) equipmentMenu.Move(direction == MenuDirection.Up ? -1 : 1);
            else if (direction == MenuDirection.Left || direction == MenuDirection.Right)
                equipmentMenu.ChangeCategory(direction == MenuDirection.Left ? -1 : 1, controller.Party);
            MenuSounds.Navigate(before, (equipmentMenu.ItemIndex, equipmentMenu.Category));
            if (!GameInput.ConfirmPressed) return;
            equipmentMenu.Apply(controller.Party); MenuSounds.Play(MenuSound.Confirm); GameInput.ConsumeGamepadFrame();
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
            if (page == 0)
            {
                materiaScreen.Draw(presentation, materiaMenu, party, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == 1)
            {
                equipmentScreen.Draw(presentation, equipmentMenu, party, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == 2)
            {
                itemScreen.Draw(presentation, itemMenu, party, secrets != null && secrets.HasScale, acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == 3)
            {
                statusScreen.Draw(presentation, party.Members[memberIndex], acceptsInput, BackToOverview);
                GUI.matrix = previous; GUI.enabled = previousEnabled; return;
            }
            if (page == 5)
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
                { selectedMenu = i; OpenCommand(i); GameInput.ConsumeGamepadFrame(); }
            }
            if (Event.current.type == EventType.Repaint) lastPointer = pointer;
            presentation.DrawHand(selectedMenu);
        }

        private void BackToOverview()
        { page = -1; MenuSounds.Play(MenuSound.Cancel); }

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
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            small = PrototypeTypography.Style(GUI.skin.label, 20); small.wordWrap = true;
            button = PrototypeTypography.Style(GUI.skin.button, 22); button.wordWrap = true;
            button.normal.background = button.hover.background = button.active.background = button.focused.background = null;
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = Color.white;
            title.normal.textColor = Color.white;
            small.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }

        public void Dispose()
        {
            if (controller != null && controller.TryCancelInventorySelection == ReturnFromInventoryMenu)
                controller.TryCancelInventorySelection = null;
            presentation?.Dispose(); if (pixel != null) UnityEngine.Object.Destroy(pixel);
        }
    }
}
