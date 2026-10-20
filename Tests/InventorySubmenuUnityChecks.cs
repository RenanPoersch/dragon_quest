using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InventorySubmenuUnityChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Gamepad pad;
    private static Keyboard keys;
    private static int checks;
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, Private);
    private static object Call(object target, string name, params object[] args)
        => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static void State(GamepadState state = default, params Key[] pressed)
    {
        typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Func<bool>)(() => true));
        InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.QueueStateEvent(keys, new KeyboardState()); InputSystem.Update();
        InputSystem.QueueStateEvent(pad, state); InputSystem.QueueStateEvent(keys, new KeyboardState(pressed)); InputSystem.Update();
    }
    public static void Run()
    {
        GameObject fixture = null;
        try
        {
            foreach (var device in InputSystem.devices.ToArray()) InputSystem.RemoveDevice(device);
            var manager = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            manager.GetType().GetProperty("runPlayerUpdatesInEditMode").SetValue(manager, true);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            pad = InputSystem.AddDevice<Gamepad>(); keys = InputSystem.AddDevice<Keyboard>();
            foreach (var control in pad.allControls.Concat(keys.allControls).OfType<UnityEngine.InputSystem.Controls.ButtonControl>())
            { bool prime = control.wasPressedThisFrame; }
            State();
            fixture = new GameObject("Inventory submenu checks");
            var player = fixture.AddComponent<PlayerMovement>(); Call(player, "Awake");
            var dialogue = fixture.AddComponent<DialogueController>();
            var battle = fixture.AddComponent<PrototypeBattleController>();
            battle.Initialize(player, dialogue, new ExplorationProgress(50));
            var hud = fixture.AddComponent<PrototypeMateriaHud>(); hud.Initialize(battle, new AncientSecrets());
            Field(battle, "<IsMenuOpen>k__BackingField").SetValue(battle, true);
            Call(hud, "CheckOpened"); Field(hud, "openedFrame").SetValue(hud, -1);
            Call(hud, "OpenCommand", 0);
            var items = (ItemMenuSession)Field(hud, "itemMenu").GetValue(hud);
            var selected = items.SelectedItem; int stock = battle.Party.Consumables.QuantityOf(selected);
            State(default, Key.RightArrow); Call(hud, "Update");
            Check(items.SubmenuIndex == 1 && items.SelectedItem == selected, "Right activates Arrange without changing the selected item.");
            State(new GamepadState().WithButton(GamepadButton.DpadRight)); Call(hud, "Update");
            Check(items.ShowingKeyItems && items.SubmenuIndex == 2, "D-pad Right changes the Item content to Key Items.");
            State(new GamepadState { leftStick = Vector2.right }); Call(hud, "Update");
            Check(!items.ShowingKeyItems && items.SubmenuIndex == 0, "Stick Right wraps back to Use.");
            State(default, Key.LeftArrow); Call(hud, "Update");
            Check(items.ShowingKeyItems, "Left wraps Use to Key Items.");
            State(default, Key.Enter); Call(hud, "Update");
            Check(!items.ChoosingTarget && battle.Party.Consumables.QuantityOf(selected) == stock,
                "Confirming Key Items cannot start using or consume a consumable.");
            State(default, Key.RightArrow); Call(hud, "Update");
            State(default, Key.DownArrow); Call(hud, "Update");
            Check(items.ItemIndex != Array.IndexOf(items.Items.Select(s => s.Definition).ToArray(), selected),
                "Down still moves within the item list after submenu navigation.");
            State(default, Key.Enter); Call(hud, "Update");
            Check(items.ChoosingTarget, "Confirm still selects an item target after entering the list.");
            State(default, Key.RightArrow); Call(hud, "Update");
            Check(items.SubmenuIndex == 0 && items.ChoosingTarget, "Horizontal target navigation stays in Use.");

            Call(hud, "OpenCommand", 1);
            var materia = (MateriaMenuSession)Field(hud, "materiaMenu").GetValue(hud);
            State(default, Key.RightArrow); Call(hud, "Update");
            var socket = materia.SelectedSocket; var equipped = socket.Equipment.Sockets[socket.Index];
            Check(materia.Removing && materia.SubmenuIndex == 1, "Right changes Materia to Remove.");
            State(new GamepadState().WithButton(GamepadButton.DpadRight)); Call(hud, "Update");
            Check(materia.SubmenuIndex == 2 && !materia.ChoosingSocket, "Repeated horizontal navigation reaches Arrange from Remove.");
            State(new GamepadState { leftStick = Vector2.left }); Call(hud, "Update");
            Check(materia.Removing && materia.SubmenuIndex == 1, "Stick Left restores Remove.");
            State(default, Key.Enter); Call(hud, "Update");
            Check(!materia.BrowsingSubmenus && socket.Equipment.Sockets[socket.Index] == equipped,
                "Entering Remove content cannot remove the highlighted materia on the same press.");
            int before = materia.SocketIndex;
            State(default, Key.RightArrow); Call(hud, "Update");
            Check(materia.SubmenuIndex == 1 && materia.SocketIndex != before, "Right navigates sockets once Remove content has focus.");
            Call(hud, "ReturnFromInventoryMenu");
            Check(materia.SubmenuIndex == 0 && !materia.Removing, "Cancel returns from removal to the Equip context.");
            State(default, Key.Enter); Call(hud, "Update");
            before = materia.SocketIndex;
            State(new GamepadState().WithButton(GamepadButton.DpadRight)); Call(hud, "Update");
            Check(materia.ChoosingSocket && materia.SubmenuIndex == 0 && materia.SocketIndex != before,
                "D-pad Right preserves horizontal equipment socket navigation.");
            State(default, Key.RightArrow); GameInput.ConsumeGamepadFrame(); before = materia.SocketIndex; Call(hud, "Update");
            Check(materia.SocketIndex == before, "Consumed input cannot navigate a second time.");
            Debug.Log("INVENTORY_SUBMENU_UNITY_CHECKS_OK: " + checks + " checks.");
            UnityEngine.Object.DestroyImmediate(fixture); EditorApplication.Exit(0);
        }
        catch (Exception error)
        { Debug.LogException(error); if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture); EditorApplication.Exit(1); }
    }
}
