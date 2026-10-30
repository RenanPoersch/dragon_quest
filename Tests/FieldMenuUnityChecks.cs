using DragonQuest.Presentation.Menus;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Inventory;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class FieldMenuUnityChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static int checks;
    private static Gamepad pad;
    private static Keyboard keys;
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, Private);
    private static void Put(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static void Property(object target, string name, object value) => Put(target, "<" + name + ">k__BackingField", value);
    private static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Private).Invoke(target, args);
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
            foreach (var control in pad.allControls.OfType<UnityEngine.InputSystem.Controls.ButtonControl>()) { bool prime = control.wasPressedThisFrame; }
            foreach (var control in keys.allControls.OfType<UnityEngine.InputSystem.Controls.ButtonControl>()) { bool prime = control.wasPressedThisFrame; }
            State();
            fixture = new GameObject("Field menu checks");
            var player = fixture.AddComponent<PlayerMovement>(); Call(player, "Awake");
            var dialogue = fixture.AddComponent<DialogueController>(); var progress = new ExplorationProgress(50);
            var secrets = new AncientSecrets(); var dragon = new DragonProgress();
            var battle = fixture.AddComponent<PrototypeBattleController>();
            battle.Initialize(player, dialogue, progress, ancientSecrets: secrets);
            var shop = fixture.AddComponent<PrototypeShopController>(); shop.Initialize(player, dialogue, battle, progress);
            var locations = fixture.AddComponent<PrototypeLocations>(); var travel = new PrototypeTravel(); Put(locations, "navigation", travel);
            var saves = fixture.AddComponent<PrototypeSaveController>();
            var store = new SaveStore(Path.Combine(Path.GetTempPath(), "DragonQuestFieldMenuSlots-" + Guid.NewGuid().ToString("N")));
            Put(saves, "store", store); Put(saves, "battle", battle); Put(saves, "shops", shop); Put(saves, "locations", locations);
            Put(saves, "player", player); Put(saves, "dialogue", dialogue); Put(saves, "progress", progress);
            Put(saves, "secrets", secrets); Put(saves, "dragon", dragon); Property(saves, "IsMenuVisible", false);
            var inventoryHost = fixture.AddComponent<PrototypeMateriaHud>(); inventoryHost.Initialize(battle, secrets); var inventory = inventoryHost.Presenter;
            Check(InventoryMenuPresentation.Commands.Count(c => c == "Materia") == 1
                && InventoryMenuPresentation.Commands.Count(c => c == "Magic") == 1
                && InventoryMenuPresentation.Commands.Count(c => c == "Save") == 1
                && InventoryMenuPresentation.Commands.Count(c => c == "Sair") == 1, "Overview exposes independent Materia, Magic, Save and Sair commands.");
            Check(InventoryMenuPresentation.CommandRect(InventoryMenuPresentation.Commands.Length - 1).yMax < 464, "All eight commands fit inside the expanded command panel.");
            Property(battle, "IsMenuOpen", true); Call(inventory, "CheckOpened"); Put(inventory, "openedFrame", -1);
            int magicCommand = Array.IndexOf(InventoryMenuPresentation.Commands, "Magic");
            Call(inventory, "OpenCommand", magicCommand);
            var magic = (MagicMenuSession)Field(inventory, "magicMenu").GetValue(inventory);
            State(default, Key.DownArrow); Call(inventory, "Update");
            Check(magic.CasterIndex == 1 && magic.ChoosingCaster, "Keyboard arrows select the Magic caster.");
            State(new GamepadState().WithButton(GamepadButton.South));
            Check(GameInput.ConfirmPressed, "Virtual gamepad confirmation is available before Magic update.");
            Call(inventory, "Update");
            Check(!magic.ChoosingCaster && !magic.ChoosingTarget, "Gamepad confirmation opens the caster's spells.");
            int mp = battle.Party.Members[1].Mp;
            State(default, Key.Enter); Call(inventory, "Update");
            Check(magic.ChoosingTarget && battle.Party.Members[1].Mp == mp, "Keyboard confirmation selects a spell without spending MP.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
            Check(!magic.ChoosingTarget && !magic.ChoosingCaster && battle.IsMenuOpen, "Circle cancels the Magic target one level.");
            State(default, Key.Escape); Call(battle, "Update");
            Check(magic.ChoosingCaster && battle.IsMenuOpen, "Escape returns to Magic caster selection.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
            Check((int)Field(inventory, "page").GetValue(inventory) == -1 && battle.IsMenuOpen, "Circle returns to the overview from Magic.");

            int saveCommand = Array.IndexOf(InventoryMenuPresentation.Commands, "Save");
            Check(saves.CanSaveManually && (bool)Call(inventory, "CommandEnabled", saveCommand), "World map enables Save while the party menu is open.");
            Call(inventory, "OpenCommand", saveCommand);
            Check(saves.IsMenuVisible && !battle.IsMenuOpen && Field(saves, "page").GetValue(saves).ToString() == "Save",
                "Save transfers from the party menu into the existing slot selection screen.");
            Check(!(bool)Field(player, "movementEnabled").GetValue(player) && !store.Exists(1), "Opening slots blocks movement and does not write a save.");
            Call(saves, "Back"); Check(!saves.IsMenuVisible && !store.Exists(1), "Cancelling slots does not write a save.");
            Check(saves.OpenManualSaveMenu(), "Save selection can reopen after cancelling.");
            Call(saves, "SelectSlot", 1);
            Check(store.TryRead(1, out var loaded, out _) && loaded.Party.Members[1].Mp == mp && !saves.IsMenuVisible && saves.ActiveSlot == 1,
                "A confirmed slot writes the journey and closes selection.");
            Check(saves.OpenManualSaveMenu(), "An existing active slot still opens the slot selector.");
            Call(saves, "SelectSlot", 1);
            Check((int)Field(saves, "overwriteSlot").GetValue(saves) == 1 && saves.IsMenuVisible, "An occupied slot requires existing overwrite confirmation.");
            Call(saves, "CancelOverwrite"); Call(saves, "Back");
            travel.TryTravel(PrototypeTravel.City, out _);
            Property(battle, "IsMenuOpen", true); Put(inventory, "page", -1);
            Check(!saves.CanSaveManually && !(bool)Call(inventory, "CommandEnabled", saveCommand), "City disables Save.");
            Call(inventory, "OpenCommand", saveCommand);
            Check(!saves.IsMenuVisible && battle.IsMenuOpen && (int)Field(inventory, "page").GetValue(inventory) == -1, "Direct confirmation cannot bypass disabled Save.");
            Put(inventory, "selectedMenu", 5); State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(inventory, "Update");
            int exitCommand = Array.IndexOf(InventoryMenuPresentation.Commands, "Sair");
            Check((int)Field(inventory, "selectedMenu").GetValue(inventory) == exitCommand
                && saves.CanReturnToTitle, "Gamepad navigation skips disabled Save and reaches Sair even in the city.");
            State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(inventory, "Update");
            Check((int)Field(inventory, "selectedMenu").GetValue(inventory) == 0, "Navigation wraps after Sair.");
            travel.TryTravel(PrototypeTravel.Inn, out _);
            Check(saves.CanSaveManually, "The inn enables Save.");
            dialogue.Open("Check", "Busy"); Check(!saves.CanSaveManually && !saves.OpenManualSaveMenu(), "Dialogue blocks Save and its opening API."); dialogue.Close();
            Property(battle, "IsActive", true); Check(!saves.CanSaveManually && !saves.CanReturnToTitle, "Combat blocks journey menu actions."); Property(battle, "IsActive", false);
            Property(battle, "HasEnded", true); Check(!saves.CanSaveManually, "Endings block Save."); Property(battle, "HasEnded", false);
            battle.CloseMenu(); shop.Open("item-shop"); Check(shop.IsOpen && !saves.CanSaveManually, "Shopping blocks Save."); shop.Close();
            Check(saves.OpenManualSaveMenu() && !saves.CanSaveManually && !saves.CanReturnToTitle && !saves.OpenManualSaveMenu(), "An open save screen cannot reopen itself.");
            Call(saves, "Back");
            UnityEngine.Object.DestroyImmediate(fixture); fixture = null;
            Debug.Log("FIELD_MENU_UNITY_CHECKS_OK: " + checks + " checks passed."); EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture); EditorApplication.Exit(1); }
    }
}
