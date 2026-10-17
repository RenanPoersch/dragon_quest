using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;

// Runs only in the disposable project created by Test-ControllerInput.ps1 -UnityIntegration.
public static class ControllerUnityChecks
{
    private static int checks;
    private static Gamepad pad;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static void Call(object target, string method)
    { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void Reset()
    {
        typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Focus(true);
    }
    private static void Focus(bool focused)
    { typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Func<bool>)(() => focused)); }
    private static void State(GamepadState state)
    {
        Reset(); InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
        InputSystem.QueueStateEvent(pad, state); InputSystem.Update();
    }
    private static void State(Keyboard keyboard, params Key[] keys)
    {
        Reset();
        InputSystem.QueueStateEvent(pad, new GamepadState());
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
    }
    private static void CheckDialogueNavigation(Keyboard keyboard, PlayerInteraction interaction,
        DialogueController dialogue, PlayerMovement movement)
    {
        foreach (ButtonControl button in keyboard.allControls.OfType<ButtonControl>()) { bool prime = button.wasPressedThisFrame; }
        int chosen = -1, activations = 0;
        string[] options = { "Um", "Dois", "Tres", "Quatro", "Cinco" };
        Action<int> select = index => { chosen = index; activations++; };
        dialogue.OpenChoices("Seletor", "Escolha uma resposta.", options, select);
        State(keyboard, Key.S); Call(interaction, "Update");
        Check(dialogue.IsTyping && dialogue.SelectedChoice == 0 && chosen == -1,
            "Navigation waits for the speech to finish.");
        State(keyboard, Key.Enter); Call(interaction, "Update");
        Check(dialogue.ChoicesReady && chosen == -1, "Revealing the speech cannot choose a response.");
        foreach (Key key in new[] { Key.W, Key.A, Key.UpArrow, Key.LeftArrow })
        {
            dialogue.SelectChoice(0);
            State(keyboard, key); Call(interaction, "Update"); Call(movement, "Update");
            Check(dialogue.SelectedChoice == 4 && chosen == -1,
                key + " wraps to the previous response without confirming.");
            Check(!movement.HasMovementInput, key + " navigates the conversation without moving the hero.");
        }
        foreach (Key key in new[] { Key.S, Key.D, Key.DownArrow, Key.RightArrow })
        {
            dialogue.SelectChoice(4);
            State(keyboard, key); Call(interaction, "Update"); Call(movement, "Update");
            Check(dialogue.SelectedChoice == 0 && chosen == -1,
                key + " wraps to the next response without confirming.");
            Check(!movement.HasMovementInput, key + " navigates the conversation without moving the hero.");
        }
        for (int i = 1; i <= options.Length; i++)
        {
            State(keyboard, Key.S); Call(interaction, "Update");
            Check(dialogue.SelectedChoice == i % options.Length, "Every response is reachable, including beyond numeric shortcuts.");
        }
        State(keyboard); // Release the keyboard before checking each controller direction.
        foreach (GamepadButton button in new[] { GamepadButton.DpadUp, GamepadButton.DpadLeft, GamepadButton.DpadDown, GamepadButton.DpadRight })
        {
            dialogue.SelectChoice(0);
            State(new GamepadState().WithButton(button)); Call(interaction, "Update");
            int expected = button == GamepadButton.DpadUp || button == GamepadButton.DpadLeft ? 4 : 1;
            Check(dialogue.SelectedChoice == expected && chosen == -1, button + " navigates dialogue responses.");
        }
        foreach (Vector2 direction in new[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right })
        {
            dialogue.SelectChoice(0);
            State(new GamepadState { leftStick = direction }); Call(interaction, "Update");
            Check(dialogue.SelectedChoice == (direction.x < 0 || direction.y > 0 ? 4 : 1) && chosen == -1,
                "The left stick navigates responses in direction " + direction + ".");
        }
        State(keyboard, Key.S); Focus(false); Call(interaction, "Update");
        Check(dialogue.SelectedChoice == 1 && chosen == -1, "An unfocused conversation ignores keyboard navigation.");
        State(keyboard, Key.Enter); Call(interaction, "Update");
        Check(chosen == 1 && activations == 1 && !dialogue.IsOpen, "Enter confirms only the highlighted response.");
        foreach (Key key in new[] { Key.Space, Key.E })
        {
            dialogue.OpenChoices("Outro seletor", "Outra resposta.", options, select); dialogue.RevealAll();
            Check(dialogue.SelectedChoice == 0, "A new conversation resets its selector.");
            State(keyboard, Key.D); Call(interaction, "Update");
            State(keyboard, key); Call(interaction, "Update");
            Check(chosen == 1 && !dialogue.IsOpen, key + " confirms after WASD navigation.");
        }
        dialogue.OpenChoices("Uma resposta", "Teste", new[] { "Voltar" }, select); dialogue.RevealAll();
        State(keyboard, Key.A); Call(interaction, "Update");
        Check(dialogue.SelectedChoice == 0, "A selector with one response remains valid.");
        int beforeCancel = activations;
        State(keyboard, Key.Escape); Call(interaction, "Update");
        Check(!dialogue.IsOpen && activations == beforeCancel, "Cancel closes the selector without executing a response.");
        State(keyboard);
    }
    public static void Run()
    {
        try
        {
            Check(Application.isBatchMode, "Run integration checks in a disposable batch project.");
            // Batch mode has no OS focus; inject it at the same gate used by the live game.
            Reset();
            Check(GameInput.IsFocused, "The isolated fixture can simulate focus.");
            foreach (InputDevice device in InputSystem.devices.ToArray()) InputSystem.RemoveDevice(device);
            var manager = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            manager.GetType().GetProperty("runPlayerUpdatesInEditMode").SetValue(manager, true);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            pad = InputSystem.AddDevice<Gamepad>();
            foreach (ButtonControl button in pad.allControls.OfType<ButtonControl>()) { bool prime = button.wasPressedThisFrame; }
            Check(Keyboard.current == null, "Checks must work without any keyboard.");
            State(new GamepadState { leftStick = new Vector2(.05f, -.05f) });
            Check(GameInput.Movement == Vector2.zero, "The Input System dead zone filters stick drift.");
            State(new GamepadState { leftStick = new Vector2(.5f, 0) });
            Check(GameInput.Movement.x > 0 && GameInput.Movement.x < 1, "Partial analog input preserves walking speed.");
            State(new GamepadState { leftStick = Vector2.one });
            Check(GameInput.Movement.magnitude <= 1.0001f, "Analog diagonals do not exceed maximum speed.");
            State(new GamepadState().WithButton(GamepadButton.DpadDown).WithButton(GamepadButton.DpadRight));
            Check(GameInput.Movement.x > 0 && GameInput.Movement.y < 0 && GameInput.Movement.magnitude <= 1.0001f,
                "The D-pad supports clamped diagonal movement.");
            State(new GamepadState().WithButton(GamepadButton.South));
            Check(GameInput.InteractPressed && GameInput.ConfirmPressed, "South interacts and confirms.");
            Focus(false);
            Check(GameInput.Movement == Vector2.zero && !GameInput.ConfirmPressed && GameInput.Navigation == MenuDirection.None,
                "Losing focus blocks movement, confirmation and menu navigation.");
            Focus(true);
            GameInput.ConsumeGamepadFrame();
            Check(!GameInput.InteractPressed && !GameInput.ConfirmPressed, "A consumed press cannot activate another screen.");
            State(new GamepadState().WithButton(GamepadButton.East));
            Check(GameInput.CancelPressed, "East cancels.");
            State(new GamepadState().WithButton(GamepadButton.West)); Check(GameInput.InventoryPressed, "West opens inventory.");
            State(new GamepadState().WithButton(GamepadButton.Start)); Check(GameInput.InventoryPressed, "Start opens inventory.");
            State(new GamepadState().WithButton(GamepadButton.Select)); Check(GameInput.SavePressed, "Select/View saves.");
            State(new GamepadState().WithButton(GamepadButton.North)); Check(GameInput.PoemPressed, "North opens the poem.");
            State(new GamepadState().WithButton(GamepadButton.RightStick)); Check(GameInput.CheatsPressed, "R3 opens cheats.");
            var hero = new GameObject("Controller test hero");
            var movement = hero.AddComponent<PlayerMovement>(); Call(movement, "Awake");
            State(new GamepadState { leftStick = Vector2.right }); Call(movement, "Update");
            Check(movement.HasMovementInput, "PlayerMovement responds without a keyboard.");
            movement.SetMovementEnabled(false); Call(movement, "Update");
            Check(!movement.HasMovementInput, "Menus stop analog movement.");
            movement.SetMovementEnabled(true); movement.SetFrontalView(true); Call(movement, "Update");
            Check(movement.FacingDirection.x > .99f, "Frontal rooms preserve analog facing.");
            movement.SetLateralPath(x => 0, -5, 5); Call(movement, "Update");
            Check(movement.HasMovementInput && movement.FacingDirection == Vector2.right, "Side-view rooms support gamepad movement.");
            var dialogue = hero.AddComponent<DialogueController>();
            var interaction = hero.AddComponent<PlayerInteraction>();
            interaction.Initialize(new InteractionContext(dialogue, new ExplorationProgress(), _ => {}), new Interactable[0]);
            dialogue.Open("Teste", "Primeira pagina", "Segunda pagina");
            State(new GamepadState().WithButton(GamepadButton.South)); Call(interaction, "Update");
            Check(dialogue.IsOpen && !dialogue.IsTyping && dialogue.PageNumber == 1, "First confirmation finishes typing without skipping a page.");
            State(new GamepadState().WithButton(GamepadButton.South)); Call(interaction, "Update");
            Check(dialogue.PageNumber == 2, "A new press advances the dialogue.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(interaction, "Update");
            Check(!dialogue.IsOpen, "East closes dialogue.");
            int choice = -1;
            dialogue.OpenChoices("Escolha", "Teste", new[] { "Um", "Dois" }, index => choice = index); dialogue.RevealAll();
            State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(interaction, "Update");
            Check(dialogue.SelectedChoice == 1, "The D-pad moves dialogue choices.");
            State(new GamepadState().WithButton(GamepadButton.South)); Call(interaction, "Update");
            Check(choice == 1, "South activates the selected dialogue choice.");
            var battle = hero.AddComponent<PrototypeBattleController>();
            battle.Initialize(movement, dialogue, new ExplorationProgress());
            State(new GamepadState().WithButton(GamepadButton.West)); Call(battle, "Update");
            Check(battle.IsMenuOpen, "West opens the actual inventory controller.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
            Check(!battle.IsMenuOpen && !GameInput.CancelPressed, "Closing inventory consumes the cancel press.");
            var shop = hero.AddComponent<PrototypeShopController>(); shop.Initialize(movement, dialogue, battle, new ExplorationProgress());
            shop.Open("item-shop"); Check(shop.IsOpen, "Shop fixture opens.");
            State(new GamepadState().WithButton(GamepadButton.East)); Call(shop, "Update");
            Check(!shop.IsOpen && !GameInput.CancelPressed, "East closes the actual shop controller.");
            State(new GamepadState { leftStick = Vector2.right }); InputSystem.RemoveDevice(pad); Reset();
            Check(GameInput.Movement == Vector2.zero && !GameInput.ConfirmPressed, "Disconnecting a held controller clears input.");
            pad = InputSystem.AddDevice<Gamepad>();
            foreach (ButtonControl button in pad.allControls.OfType<ButtonControl>()) { bool prime = button.wasPressedThisFrame; } State(new GamepadState { leftStick = Vector2.left });
            Check(GameInput.Movement.x < 0, "Reconnecting resumes input without restarting.");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            State(new GamepadState()); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); InputSystem.Update();
            Check(GameInput.Movement.x > 0 && GameInput.Movement.y > 0 && GameInput.Movement.magnitude <= 1.0001f,
                "Keyboard movement still works with a controller connected.");
            CheckDialogueNavigation(keyboard, interaction, dialogue, movement);
            UnityEngine.Object.DestroyImmediate(hero);
            Debug.Log("CONTROLLER_UNITY_CHECKS_OK: " + checks + " virtual gamepad, movement, dialogue, inventory, shops, reconnection and keyboard checks.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
