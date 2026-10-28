using System;
using System.Reflection;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

// Runs with the real Input System in Test-ControllerInput.ps1's disposable project.
public static class MenuInputUnityChecks
{
    private static int checks;
    private static Gamepad pad;
    private static Keyboard keys;
    private static FieldInfo Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static void Call(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static void State(GamepadState state, params Key[] pressed)
    {
        typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, (Func<bool>)(() => true));
        InputSystem.QueueStateEvent(pad, new GamepadState());
        InputSystem.QueueStateEvent(keys, new KeyboardState()); InputSystem.Update();
        InputSystem.QueueStateEvent(pad, state);
        InputSystem.QueueStateEvent(keys, new KeyboardState(pressed)); InputSystem.Update();
    }
    public static void Run(Gamepad gamepad, Keyboard keyboard)
    {
        pad = gamepad; keys = keyboard;
        foreach (var control in keys.allControls)
            if (control is ButtonControl button) { bool prime = button.wasPressedThisFrame; }
        State(new GamepadState { leftStick = Vector2.down });
        Check(GameInput.Navigation == MenuDirection.Down, "The stick moves immediately.");
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
        Check(GameInput.Navigation == MenuDirection.Up, "A fresh input update cannot reuse a stale direction from the same render frame.");
        GameInput.ConsumeGamepadFrame();
        Check(GameInput.Navigation == MenuDirection.None, "Consumed navigation does not reach another menu.");
        // Simulate the next render before the repeat delay, without resetting the held input.
        typeof(GameInput).GetField("consumedFrame", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1);
        typeof(GameInput).GetField("navigationFrame", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1);
        Check(GameInput.Navigation == MenuDirection.None, "Consumption preserves the repeat delay instead of restarting the held direction.");
        foreach (var key in new[] { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.W, Key.S, Key.A, Key.D })
        {
            State(new GamepadState(), key);
            MenuDirection expected = key == Key.UpArrow || key == Key.W ? MenuDirection.Up
                : key == Key.DownArrow || key == Key.S ? MenuDirection.Down
                : key == Key.LeftArrow || key == Key.A ? MenuDirection.Left : MenuDirection.Right;
            Check(GameInput.Navigation == expected, key + " uses the shared menu navigation.");
        }
        var fixture = new GameObject("Menu input regression checks");
        var movement = fixture.AddComponent<PlayerMovement>(); Call(movement, "Awake");
        var dialogue = fixture.AddComponent<DialogueController>();
        var saves = fixture.AddComponent<PrototypeSaveController>();
        var cheats = fixture.AddComponent<PrototypeCheatMenu>();
        var battle = fixture.AddComponent<PrototypeBattleController>();
        var progress = new ExplorationProgress(50);
        battle.Initialize(movement, dialogue, progress);
        cheats.Initialize(movement, battle, saves, progress);
        // Supply an empty temporary store and skip artwork/fade; never initialize real saves.
        string saveDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DQMenuInput-" + Guid.NewGuid());
        var saveStore = new DragonQuest.Persistence.SaveStore(saveDirectory);
        Field(saves, "store").SetValue(saves, saveStore);
        Field(saves, "player").SetValue(saves, movement);
        Field(saves, "dialogue").SetValue(saves, dialogue);
        Field(saves, "battle").SetValue(saves, battle);
        Field(saves, "menuHasDrawn").SetValue(saves, true);
        Field(saves, "menuFadeInElapsed").SetValue(saves, 2f);
        Call(saves, "RefreshSlots");
        var optionEnabled = typeof(PrototypeSaveController).GetMethod("OptionEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(!(bool)optionEnabled.Invoke(saves, new object[] { 1 }), "Continue is disabled when all three slots are empty.");
        foreach (var key in new[] { Key.DownArrow, Key.UpArrow, Key.RightArrow, Key.LeftArrow, Key.S, Key.W })
        {
            State(new GamepadState(), key); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 0, key + " skips disabled Continue.");
        }
        foreach (var button in new[] { GamepadButton.DpadDown, GamepadButton.DpadUp, GamepadButton.DpadLeft, GamepadButton.DpadRight })
        {
            State(new GamepadState().WithButton(button)); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 0, button + " skips disabled Continue.");
        }
        foreach (var direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
        {
            State(new GamepadState { leftStick = direction }); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 0, "The stick skips disabled Continue in direction " + direction);
        }
        Field(saves, "selectedOption").SetValue(saves, 1);
        State(new GamepadState(), Key.Enter); Call(saves, "Update");
        Check(Field(saves, "page").GetValue(saves).ToString() == "Title", "Enter cannot activate a stale disabled Continue selection.");
        State(new GamepadState().WithButton(GamepadButton.South)); Call(saves, "Update");
        Check(Field(saves, "page").GetValue(saves).ToString() == "Title", "Gamepad confirmation cannot activate disabled Continue.");
        System.IO.Directory.CreateDirectory(saveDirectory);
        System.IO.File.WriteAllText(saveStore.SlotPath(3), "corrupt save");
        Call(saves, "RefreshSlots");
        Check(!(bool)optionEnabled.Invoke(saves, new object[] { 1 })
            && (int)Field(saves, "selectedOption").GetValue(saves) == 0,
            "An unreadable save cannot enable Continue, and refreshing restores the New game selection.");
        var party = PrototypePartyFactory.Create();
        var savedProgress = new ExplorationProgress(50);
        var savedShops = PrototypeShopFactory.Create(party, savedProgress);
        var snapshot = DragonQuest.Persistence.JourneySnapshot.Capture(party, savedProgress,
            new DragonQuest.Progression.AncientSecrets(), new DragonQuest.Progression.DragonProgress(),
            savedShops, PrototypeTravel.World, 0, -7);
        Check(saveStore.TryWrite(2, snapshot, out _) && saveStore.TryWrite(2, snapshot, out _), "Create a valid save with a recovery backup in slot 2.");
        System.IO.File.WriteAllText(saveStore.SlotPath(2), "corrupt primary");
        Call(saves, "RefreshSlots");
        Check((bool)optionEnabled.Invoke(saves, new object[] { 1 }), "A recoverable backup enables Continue even when the primary save is corrupt.");
        foreach (var key in new[] { Key.DownArrow, Key.UpArrow, Key.RightArrow, Key.LeftArrow, Key.S, Key.W })
        {
            Field(saves, "selectedOption").SetValue(saves, 0);
            State(new GamepadState(), key); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 1, key + " moves the initial menu in Update without an IMGUI event.");
        }
        foreach (var button in new[] { GamepadButton.DpadDown, GamepadButton.DpadUp, GamepadButton.DpadLeft, GamepadButton.DpadRight })
        {
            Field(saves, "selectedOption").SetValue(saves, 0);
            State(new GamepadState().WithButton(button)); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 1, button + " moves the initial menu.");
        }
        foreach (var direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
        {
            Field(saves, "selectedOption").SetValue(saves, 0);
            State(new GamepadState { leftStick = direction }); Call(saves, "Update");
            Check((int)Field(saves, "selectedOption").GetValue(saves) == 1, "The initial menu accepts stick direction " + direction);
        }
        State(new GamepadState(), Key.Enter); Call(saves, "Update");
        Check(Field(saves, "page").GetValue(saves).ToString() == "Continue", "Enter confirms Continue through the Input System.");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(saves, "Update");
        Check(Field(saves, "page").GetValue(saves).ToString() == "Title", "Circle returns from Continue to the initial menu.");
        System.IO.File.Delete(saveStore.SlotPath(2));
        System.IO.File.Delete(saveStore.SlotPath(2) + ".bak");
        System.IO.File.Delete(saveStore.SlotPath(3));
        Call(saves, "RefreshSlots");
        Check(!(bool)optionEnabled.Invoke(saves, new object[] { 1 }), "Removing the last loadable save disables Continue again.");
        Field(saves, "<IsMenuVisible>k__BackingField").SetValue(saves, false);
        var inventory = fixture.AddComponent<PrototypeMateriaHud>();
        inventory.Initialize(battle, new DragonQuest.Progression.AncientSecrets());
        State(new GamepadState().WithButton(GamepadButton.North)); Call(battle, "Update");
        Call(inventory, "CheckOpened"); Field(inventory, "openedFrame").SetValue(inventory, -1);
        Check(battle.IsMenuOpen, "Triangle opens the inventory before navigating.");
        State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(inventory, "Update");
        Check((int)Field(inventory, "selectedMenu").GetValue(inventory) == 1, "The D-pad navigates the inventory overview.");
        State(new GamepadState(), Key.DownArrow); Call(inventory, "Update");
        Check((int)Field(inventory, "selectedMenu").GetValue(inventory) == 2, "Keyboard arrows navigate the inventory overview.");
        State(new GamepadState { leftStick = Vector2.down }); Call(inventory, "Update");
        Check((int)Field(inventory, "selectedMenu").GetValue(inventory) == 3, "The stick navigates the inventory overview.");
        foreach (int command in new[] { 0, 1, 2, 3 })
        {
            Field(inventory, "selectedMenu").SetValue(inventory, command);
            State(new GamepadState().WithButton(GamepadButton.South)); Call(inventory, "Update");
            State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(inventory, "Update");
            int cursor = command == 0 ? ((DragonQuest.Inventory.ItemMenuSession)Field(inventory, "itemMenu").GetValue(inventory)).ItemIndex
                : command == 1 ? ((DragonQuest.Equipment.MateriaMenuSession)Field(inventory, "materiaMenu").GetValue(inventory)).MateriaIndex
                : command == 2 ? ((DragonQuest.Equipment.EquipmentMenuSession)Field(inventory, "equipmentMenu").GetValue(inventory)).ItemIndex
                : (int)Field(inventory, "memberIndex").GetValue(inventory);
            Check(cursor > 0, "Inventory command " + command + " accepts directional navigation in its actual controller.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        }
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        Check(!battle.IsMenuOpen, "Circle closes the inventory after submenu navigation.");
        State(new GamepadState().WithButton(GamepadButton.Select));
        Check(GameInput.CheatsPressed && !GameInput.SavePressed, "Select opens cheats and cannot also request a save.");
        Call(cheats, "Update"); Call(saves, "Update");
        Check(cheats.IsOpen && !saves.IsMenuVisible && !GameInput.CheatsPressed, "Select opens the actual cheat controller without opening saves or leaking its press.");
        State(new GamepadState(), Key.DownArrow); Call(cheats, "Update");
        Check((int)Field(cheats, "selected").GetValue(cheats) == 1, "Keyboard arrows navigate cheats in Update.");
        State(new GamepadState { leftStick = Vector2.down }); Call(cheats, "Update");
        Check((int)Field(cheats, "selected").GetValue(cheats) == 2, "The stick navigates cheats.");
        State(new GamepadState().WithButton(GamepadButton.Select)); Call(cheats, "Update");
        Check(!cheats.IsOpen && !GameInput.SavePressed, "Select closes cheats without opening a save menu.");
        State(new GamepadState().WithButton(GamepadButton.Select)); Call(cheats, "Update");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(cheats, "Update");
        Check(!cheats.IsOpen, "Circle still closes cheats.");
        State(new GamepadState(), Key.F5);
        Check(GameInput.SavePressed && !GameInput.CheatsPressed, "F5 keeps manual saving separate from cheats.");
        State(new GamepadState());
        UnityEngine.Object.DestroyImmediate(fixture);
        Debug.Log("MENU_INPUT_UNITY_CHECKS_OK: " + checks + " input updates, keyboard, directional pad, analog stick, title, Continue and Select cheat checks.");
    }
}
