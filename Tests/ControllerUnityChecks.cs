using DragonQuest.Presentation.Menus;
using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Input;
using DragonQuest.Combat;
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
    { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, null); }
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
    private static void CheckInventoryControls(Keyboard keyboard)
    {
        State(keyboard);
        var fixture = new GameObject("Inventory control checks");
        var movement = fixture.AddComponent<PlayerMovement>(); Call(movement, "Awake");
        var dialogue = fixture.AddComponent<DialogueController>();
        var secrets = new DragonQuest.Progression.AncientSecrets(); secrets.LearnPoem();
        var progress = new ExplorationProgress(50);
        var battle = fixture.AddComponent<PrototypeBattleController>();
        battle.Initialize(movement, dialogue, progress, ancientSecrets: secrets);
        var interaction = fixture.AddComponent<PlayerInteraction>();
        interaction.Initialize(new InteractionContext(dialogue, progress, _ => {},
            isBusy: () => battle.IsBusy, secrets: secrets), new Interactable[0]);
        var inventoryHost = fixture.AddComponent<PrototypeMateriaHud>(); inventoryHost.Initialize(battle, secrets); var inventory = inventoryHost.Presenter;
        var page = typeof(InventoryMenuPresenter).GetField("page", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var open = typeof(InventoryMenuPresenter).GetMethod("OpenCommand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        State(new GamepadState().WithButton(GamepadButton.North)); Call(interaction, "Update"); Call(battle, "Update");
        Check(battle.IsMenuOpen && !dialogue.IsOpen && !GameInput.InventoryPressed,
            "Triangle opens inventory after learning the poem and consumes its press without opening speech.");
        Call(inventory, "CheckOpened");
        for (int command = 0; command < InventoryMenuPresentation.Commands.Length; command++)
        {
            open.Invoke(inventory, new object[] { command });
            if (InventoryMenuPresentation.Commands[command] == "Save" || InventoryMenuPresentation.Commands[command] == "Sair")
            {
                Check(battle.IsMenuOpen && (int)page.GetValue(inventory) == -1,
                    "Journey actions cannot open without an initialized save controller.");
                continue;
            }
            State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
            Check(battle.IsMenuOpen && (int)page.GetValue(inventory) == -1 && !GameInput.CancelPressed,
                "Circle returns from " + InventoryMenuPresentation.Commands[command] + " to the inventory overview.");
        }
        open.Invoke(inventory, new object[] { 0 });
        var items = (DragonQuest.Inventory.ItemMenuSession)typeof(InventoryMenuPresenter)
            .GetField("itemMenu", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(inventory);
        Check(items.BeginUse(battle.Party), "An item can enter target selection.");
        int quantity = battle.Party.Consumables.QuantityOf(items.SelectedItem);
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        Check(!items.ChoosingTarget && (int)page.GetValue(inventory) == 2 && battle.IsMenuOpen
            && battle.Party.Consumables.QuantityOf(items.SelectedItem) == quantity,
            "Circle cancels the item target without consuming an item or leaving its list.");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        Check((int)page.GetValue(inventory) == -1 && battle.IsMenuOpen, "A second Circle returns from items to the overview.");
        open.Invoke(inventory, new object[] { 1 });
        var materia = (DragonQuest.Equipment.MateriaMenuSession)typeof(InventoryMenuPresenter)
            .GetField("materiaMenu", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(inventory);
        Check(materia.BeginEquip(battle.Party), "A materia can enter socket selection.");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        Check(!materia.ChoosingSocket && (int)page.GetValue(inventory) == 0 && battle.IsMenuOpen,
            "Circle cancels a socket selection while keeping the materia list open.");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battle, "Update");
        Check(!battle.IsMenuOpen && !GameInput.CancelPressed, "Circle closes the inventory only after returning to its overview.");
        State(keyboard, Key.P); Call(interaction, "Update");
        Check(!dialogue.IsOpen, "The removed poem shortcut no longer opens speech from the keyboard.");
        int chosen = -1;
        dialogue.OpenChoices("Cancel", "Choose", new[] { "One", "Two" }, index => chosen = index); dialogue.RevealAll();
        State(new GamepadState().WithButton(GamepadButton.East).WithButton(GamepadButton.South)); Call(interaction, "Update");
        Check(!dialogue.IsOpen && chosen == -1 && !GameInput.ConfirmPressed,
            "Circle cancels a dialogue choice even when confirmation is pressed together.");
        var shop = fixture.AddComponent<PrototypeShopController>(); shop.Initialize(movement, dialogue, battle, progress);
        shop.Open("item-shop"); shop.ChooseAction(0); shop.Menu.BeginQuantity();
        Check(shop.Menu.Phase.ToString() == "Quantity", "Shop quantity selection opens.");
        int gold = progress.Gold;
        foreach (string expected in new[] { "Browse", "Actions" })
        {
            State(new GamepadState().WithButton(GamepadButton.East)); Call(shop, "Update");
            Check(shop.IsOpen && shop.Menu.Phase.ToString() == expected && progress.Gold == gold && !GameInput.CancelPressed,
                "Circle returns one shop level to " + expected + " without a transaction.");
        }
        State(new GamepadState().WithButton(GamepadButton.East)); Call(shop, "Update");
        Check(!shop.IsOpen, "Circle closes the shop from its action menu.");
        State(keyboard);
        battle.StartBattle();
        var battleHud = fixture.AddComponent<PrototypeBattleHud>(); battleHud.Initialize(battle);
        battleHud.Arena.GetType().GetField("observedBattle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(battleHud.Arena, battle.Session);
        State(new GamepadState().WithButton(GamepadButton.South)); Call(battleHud, "Update");
        var commandMenu = battleHud.Menu;
        Check(commandMenu.SelectedAbility != null, "Battle attack enters target selection.");
        State(new GamepadState().WithButton(GamepadButton.East)); Call(battleHud, "Update");
        Check(commandMenu.SelectedAbility == null && battle.Session.CurrentActor.TurnsTaken == 0 && !GameInput.CancelPressed,
            "Circle cancels the battle target without spending a turn.");
        UnityEngine.Object.DestroyImmediate(fixture);
        State(keyboard);
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
    private static void Age(object target, string property, float seconds)
    {
        if (target is PrototypeBattleController battle && property == "ActionStartedAt") target = battle.ActionPresentation;
        target.GetType().GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SetValue(target, Time.unscaledTime - seconds);
    }
    private static void CheckBattleEscape(Keyboard keyboard, PrototypeBattleController battle, PlayerMovement movement)
    {
        State(keyboard, Key.Q); Check(!GameInput.EscapeHeld, "Q alone cannot request escape.");
        State(keyboard, Key.E); Check(!GameInput.EscapeHeld, "E alone keeps its existing behavior.");
        State(keyboard, Key.Q, Key.E); Check(GameInput.EscapeHeld, "Q and E together request escape.");
        Focus(false); Check(!GameInput.EscapeHeld, "Losing focus releases the escape chord.");
        State(keyboard);
        State(new GamepadState { leftTrigger = 1 }); Check(!GameInput.EscapeHeld, "L2 alone cannot request escape.");
        State(new GamepadState { rightTrigger = 1 }); Check(!GameInput.EscapeHeld, "R2 alone cannot request escape.");
        State(new GamepadState { leftTrigger = 1, rightTrigger = 1 });
        Check(GameInput.EscapeHeld, "Both triggers request escape without keyboard input.");
        InputSystem.QueueStateEvent(pad, new GamepadState { leftTrigger = 1, rightTrigger = 1 }); InputSystem.Update();
        Check(GameInput.EscapeHeld, "Escape uses continuous hold, not a one-frame press.");
        InputSystem.RemoveDevice(pad); Reset(); Check(!GameInput.EscapeHeld, "Disconnecting a held controller cancels escape input.");
        pad = InputSystem.AddDevice<Gamepad>();
        State(keyboard);
        battle.StartBattle();
        var actor = battle.Session.CurrentActor;
        State(keyboard, Key.Q, Key.E); Call(battle, "Update");
        Check(battle.IsEscaping && battle.Session.Outcome == BattleOutcome.None && actor.TurnsTaken == 0,
            "The real controller starts the run animation before advancing turns.");
        Check(!battle.TryPlayerAction(actor.Definition.Abilities[0], actor), "The chord cannot also confirm a battle command.");
        State(keyboard, Key.Q); Call(battle, "Update");
        Check(!battle.IsEscaping && actor.TurnsTaken == 0, "Releasing E cancels preparation without spending the turn.");
        State(new GamepadState { leftTrigger = 1, rightTrigger = 1 }); Call(battle, "Update");
        Check(battle.IsEscaping, "The battle controller accepts L2 and R2.");
        Focus(false); Call(battle, "Update"); Check(!battle.IsEscaping, "Focus loss cancels an actual pending battle escape.");
        State(keyboard);
        var defend = actor.Definition.Abilities.First(a => a.Effect == AbilityEffect.Defend);
        Check(battle.TryPlayerAction(defend, actor) && battle.IsPresentingAction, "Start a real action before requesting escape.");
        State(keyboard, Key.Q, Key.E); Call(battle, "Update");
        Check(battle.IsEscaping && battle.IsPresentingAction, "Escape can be requested while an existing animation finishes.");
        State(keyboard); Call(battle, "Update");
        Check(!battle.IsEscaping && battle.IsPresentingAction, "Release cancels escape even during an action presentation.");
        Age(battle, "ActionStartedAt", 100);
        var experience = battle.Party.Members.Select(m => m.Experience).ToArray();
        int completed = 0; battle.EncounterCompleted += outcome => { if (outcome == BattleOutcome.Escaped) completed++; };
        State(keyboard, Key.Q, Key.E); Call(battle, "Update"); Age(battle, "EscapeStartedAt", 2);
        for (int i = 0; i < 30 && battle.Session.Outcome == BattleOutcome.None; i++)
        {
            if (battle.IsPresentingAction) Age(battle, "ActionStartedAt", 100);
            if (battle.Session.CurrentActor?.Team == CombatTeam.Enemy)
            {
                typeof(PrototypeBattleController).GetField("pendingEnemy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(battle, battle.Session.CurrentActor);
                typeof(PrototypeBattleController).GetField("enemyActionTime", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(battle, Time.unscaledTime - 2);
            }
            Call(battle, "Update");
        }
        Check(battle.Session.Outcome == BattleOutcome.Escaped && battle.IsActive && battle.IsEscaping && completed == 1,
            "Holding escapes on the next allied turn and shows the final run offscreen exactly once.");
        Check(!battle.IsPostBattleOpen && !battle.PostBattle.IsComplete && !battle.Rewards.HasClaimed(battle.Session)
            && battle.Party.Members.Select(m => m.Experience).SequenceEqual(experience), "Escape bypasses XP, gold and victory screens.");
        foreach (var member in battle.Party.Members)
        {
            var state = battle.Session.Combatants.First(c => c.Id == member.Id);
            Check(member.Hp == state.Hp && member.Mp == state.Mp, "Escape captures remaining HP and MP.");
        }
        Age(battle, "EscapeCompletedAt", 1); Call(battle, "Update");
        Check(!battle.IsActive && !battle.IsBusy && completed == 1, "Run completion automatically restores exploration without confirmation.");
        State(keyboard, Key.D); Call(movement, "Update"); Check(movement.HasMovementInput, "Movement resumes after escaping.");
        State(keyboard);
        battle.StartCastleBattle(); actor = battle.Session.CurrentActor;
        State(keyboard, Key.Q, Key.E); Call(battle, "Update");
        Check(!battle.IsEscaping && !battle.Session.EscapePending && battle.Session.CurrentActor == actor
            && actor.TurnsTaken == 0 && battle.ErrorMessage.Contains("chefes"), "Boss attempts show feedback without running or spending a turn.");
        State(keyboard);
    }

    private static void CheckBattleConfirmation(Keyboard keyboard)
    {
        foreach (Key key in new[] { Key.Enter, Key.NumpadEnter, Key.Space, Key.E })
        {
            State(keyboard, key);
            Check(GameInput.ConfirmPressed, key + " is a fresh confirmation.");
            GameInput.ConsumeGamepadFrame();
            Check(!GameInput.ConfirmPressed && !GameInput.InteractPressed,
                key + " cannot confirm another screen after its frame was consumed.");

            var fixture = new GameObject("Battle confirmation " + key);
            var movement = fixture.AddComponent<PlayerMovement>(); Call(movement, "Awake");
            var dialogue = fixture.AddComponent<DialogueController>();
            var battle = fixture.AddComponent<PrototypeBattleController>();
            battle.Initialize(movement, dialogue, new ExplorationProgress());
            var hud = fixture.AddComponent<PrototypeBattleHud>(); hud.Initialize(battle);
            var interaction = fixture.AddComponent<PlayerInteraction>();
            interaction.Initialize(new InteractionContext(dialogue, new ExplorationProgress(), _ => {},
                battle.StartBattle, () => battle.IsBusy), new Interactable[0]);
            dialogue.OpenWithCompletion("Encontro", battle.StartBattle, "Entrar na batalha.");
            dialogue.RevealAll();
            State(keyboard, key); Call(interaction, "Update");
            // This input-only project has no artwork; skip the HUD's asset preloading.
            hud.Arena.GetType().GetField("observedBattle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(hud.Arena, battle.Session);
            Call(hud, "Update");
            var actor = battle.Session.CurrentActor;
            var commandMenu = hud.Menu;
            Check(battle.IsActive && actor.Team == DragonQuest.Combat.CombatTeam.Party
                && actor.TurnsTaken == 0 && commandMenu.SelectedAbility == null,
                key + " opens the battle without also selecting its first command.");
            State(keyboard); Call(hud, "Update"); Call(battle, "Update");
            Check(actor.TurnsTaken == 0 && !battle.IsPresentingAction,
                "A new battle waits for the player's command when input is idle.");
            State(keyboard, key); Call(hud, "Update");
            Check(commandMenu.SelectedAbility != null && actor.TurnsTaken == 0,
                "A fresh " + key + " selects attack and waits for target confirmation.");
            Call(hud, "Update");
            Check(actor.TurnsTaken == 0 && !battle.IsPresentingAction,
                "The same keyboard press cannot select attack and execute it in one frame.");
            Reset(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            Call(hud, "Update");
            Check(actor.TurnsTaken == 0, "Holding confirmation cannot execute the selected attack.");
            var enemy = battle.Session.Combatants.First(c => c.Team == DragonQuest.Combat.CombatTeam.Enemy);
            int hp = enemy.Hp;
            State(keyboard, key); Call(hud, "Update");
            Check(actor.TurnsTaken == 1 && enemy.Hp < hp && battle.IsPresentingAction,
                "Releasing and pressing " + key + " again executes exactly one chosen attack.");
            Call(hud, "Update");
            Check(actor.TurnsTaken == 1, "An accepted attack is not repeated during its presentation.");
            UnityEngine.Object.DestroyImmediate(fixture);
        }
        State(keyboard, Key.Escape); GameInput.ConsumeGamepadFrame();
        Check(!GameInput.CancelPressed, "A consumed keyboard cancel cannot close another screen.");
        State(keyboard, Key.I); GameInput.ConsumeGamepadFrame();
        Check(!GameInput.InventoryPressed, "A consumed inventory shortcut cannot reopen its menu.");
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
            State(new GamepadState().WithButton(GamepadButton.West)); Check(!GameInput.InventoryPressed, "Square does not open inventory.");
            State(new GamepadState().WithButton(GamepadButton.Start)); Check(GameInput.InventoryPressed, "Start opens inventory.");
            State(new GamepadState().WithButton(GamepadButton.Select)); Check(GameInput.CheatsPressed && !GameInput.SavePressed, "Select/View opens cheats without saving.");
            State(new GamepadState().WithButton(GamepadButton.North)); Check(GameInput.InventoryPressed && !GameInput.InteractPressed, "Triangle opens inventory without interacting.");
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
            State(new GamepadState().WithButton(GamepadButton.North)); Call(battle, "Update");
            Check(battle.IsMenuOpen, "Triangle opens the actual inventory controller.");
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
            MenuInputUnityChecks.Run(pad, keyboard);
            CheckInventoryControls(keyboard);
            CheckDialogueNavigation(keyboard, interaction, dialogue, movement);
            CheckBattleConfirmation(keyboard);
            CheckBattleEscape(keyboard, battle, movement);
            UnityEngine.Object.DestroyImmediate(hero);
            Debug.Log("CONTROLLER_UNITY_CHECKS_OK: " + checks + " virtual gamepad, movement, dialogue, inventory, shops, reconnection and keyboard checks.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
