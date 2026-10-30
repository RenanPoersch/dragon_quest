using System.Collections.Generic;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Input;
using DragonQuest.Inventory;
using DragonQuest.Prototype;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonQuest.Presentation.Battle
{
    public delegate bool BattleCommandButton(Rect rect, string label, bool focused, bool enabled,
        Texture2D icon = null, bool limitReady = false);

    // Owns command/target selection and input; the arena reads state and forwards target clicks.
    public sealed class BattleMenuPresenter
    {
        private PrototypeBattleController controller;
        private BattleSession observedBattle;
        private CombatantState observedActor;
        private CombatantState preferredEnemyTarget;
        private CombatantDefinition observedDefinition;
        private bool observedLimitReady;
        private AbilityDefinition selected;
        private ConsumableDefinition selectedItem;
        private bool itemMode, defendMode;
        private AbilityDefinition defendCommand;
        private AbilityMenuGroup menuGroup;
        private List<BattleMenuCommand> rootCommands = new List<BattleMenuCommand>();
        private List<AbilityDefinition> groupAbilities = new List<AbilityDefinition>();
        private int rootIndex;
        private int commandIndex, targetIndex;
        private Vector2 actionsScroll, targetsScroll;

        public CombatantState Actor => observedActor;
        public AbilityDefinition SelectedAbility => selected;
        public int TargetIndex => targetIndex;

        public void Initialize(PrototypeBattleController battleController) => controller = battleController;

        public void ObserveBattle()
        {
            BattleSession battle = controller.Session;
            if (observedBattle != battle)
            {
                observedBattle = battle; observedActor = null; preferredEnemyTarget = null;
            }
            if (observedActor != battle.CurrentActor || observedDefinition != battle.CurrentActor?.Definition
                || observedLimitReady != (battle.CurrentActor?.Limit.IsReady ?? false))
            {
                observedActor = battle.CurrentActor; selected = null; selectedItem = null;
                observedDefinition = observedActor?.Definition;
                observedLimitReady = observedActor?.Limit.IsReady ?? false;
                itemMode = defendMode = false; menuGroup = AbilityMenuGroup.Direct; rootIndex = commandIndex = targetIndex = 0;
                rootCommands = observedActor == null ? new List<BattleMenuCommand>() : BattleCommandMenu.Root(observedActor);
                defendCommand = observedActor == null ? null : BattleCommandMenu.Defend(observedActor);
                groupAbilities.Clear(); actionsScroll = targetsScroll = Vector2.zero;
            }
        }

        public void UpdateInput()
        {
            if (controller.HasEnded) return;
            if (controller.IsEnteringBattle) return;
            if (controller.IsCheatMenuBlocking || controller.IsEscaping || GameInput.EscapeHeld) return;
            if (controller.IsPresentingAction) return;
            Keyboard keys = Keyboard.current;
            if (!GameInput.IsFrameAvailable) return;
            if (observedBattle.Outcome != BattleOutcome.None)
            {
                if (observedBattle.Outcome != BattleOutcome.Defeat && GameInput.ConfirmPressed)
                { MenuSounds.Play(MenuSound.Confirm); controller.ReturnToCity(); GameInput.ConsumeGamepadFrame(); }
                return;
            }
            if (observedActor == null || observedActor.Team != CombatTeam.Party || observedActor.MindControlTurns > 0) return;
            if (GameInput.CancelPressed)
            { Cancel(); GameInput.ConsumeGamepadFrame(); return; }
            bool targeting = selected != null || selectedItem != null;
            MenuDirection navigation = GameInput.Navigation;
            bool leftPressed = navigation == MenuDirection.Left || keys?.leftArrowKey.wasPressedThisFrame == true || keys?.aKey.wasPressedThisFrame == true;
            bool rightPressed = navigation == MenuDirection.Right || keys?.rightArrowKey.wasPressedThisFrame == true || keys?.dKey.wasPressedThisFrame == true;
            if (leftPressed)
            {
                if (!targeting && CanOpenDefendMenu)
                {
                    defendMode = true; MenuSounds.Play(MenuSound.Navigate);
                }
                else if (targeting || InSubmenu) Cancel();
                GameInput.ConsumeGamepadFrame(); return;
            }
            if (rightPressed && !targeting && !InSubmenu && defendMode && commandIndex == 0)
            { defendMode = false; MenuSounds.Play(MenuSound.Navigate); GameInput.ConsumeGamepadFrame(); return; }
            int count = targeting ? TargetRowCount() : CommandCount();
            if (targeting) targetIndex = Mathf.Clamp(targetIndex, 0, Mathf.Max(0, count - 1));
            int delta = navigation == MenuDirection.Down || keys?.downArrowKey.wasPressedThisFrame == true || keys?.sKey.wasPressedThisFrame == true ? 1
                : navigation == MenuDirection.Up || keys?.upArrowKey.wasPressedThisFrame == true || keys?.wKey.wasPressedThisFrame == true ? -1 : 0;
            if (count > 0 && delta != 0)
            {
                if (count > 1) MenuSounds.Play(MenuSound.Navigate);
                if (targeting)
                {
                    targetIndex = (targetIndex + delta + count) % count;
                    targetsScroll.y = BattleCommandMenu.KeepRowVisible(targetsScroll.y, targetIndex, count, 88);
                }
                else
                {
                    commandIndex = (commandIndex + delta + count) % count;
                    actionsScroll.y = BattleCommandMenu.KeepRowVisible(actionsScroll.y, commandIndex, count, 120);
                }
            }
            if (GameInput.ConfirmPressed)
            {
                if (targeting && count > 0) ExecuteTarget(Targets()[targetIndex]);
                else if (!targeting) ChooseCommand(commandIndex);
                GameInput.ConsumeGamepadFrame();
            }
            else if (!targeting && rightPressed)
                ChooseCommand(commandIndex, navigateOnly: true);
        }

        public bool CanAcceptMenuInput => GameInput.IsFrameAvailable && !controller.HasEnded && !controller.IsEnteringBattle && !controller.IsCheatMenuBlocking
            && !controller.IsPresentingAction;
        private bool InSubmenu => itemMode || menuGroup != AbilityMenuGroup.Direct;
        private bool CanOpenDefendMenu => !InSubmenu && !defendMode && defendCommand != null
            && commandIndex == 0 && rootCommands.Count > 0
            && (rootCommands[commandIndex].Ability?.SourceAbilityId == "attack" || rootCommands[commandIndex].Ability?.UsesLimit == true);
        private int CommandCount() => itemMode ? observedBattle.Inventory.Items.Count
            : InSubmenu ? groupAbilities.Count : rootCommands.Count;
        private AbilityDefinition CommandAbility(int index) => !InSubmenu && defendMode && index == 0 ? defendCommand : InSubmenu
            ? index < groupAbilities.Count ? groupAbilities[index] : null : rootCommands[index].Ability;
        private string CommandLabel(int index)
        {
            if (!InSubmenu && defendMode && index == 0) return defendCommand.Name;
            if (itemMode)
            {
                ItemStack stack = observedBattle.Inventory.Items[index];
                return stack.Definition.Name + "  x" + stack.Quantity;
            }
            if (!InSubmenu) return rootCommands[index].Label;
            AbilityDefinition ability = groupAbilities[index];
            return ability.Name + (ability.MpCost > 0 ? "  " + ability.MpCost + " MP" : "");
        }
        private bool CommandEnabled(int index)
        {
            if (observedActor.MindControlTurns > 0) return false;
            if (!InSubmenu && observedActor.IsSilenced && (rootCommands[index].Kind == BattleCommandKind.Magic
                || rootCommands[index].Kind == BattleCommandKind.Summon)) return false;
            if (itemMode) return observedBattle.Inventory.Items[index].Quantity > 0;
            AbilityDefinition ability = CommandAbility(index);
            return ability == null || (!observedActor.IsSilenced || !ability.IsMagic)
                && observedActor.Mp >= ability.MpCost && (!ability.UsesLimit || observedActor.Limit.IsReady);
        }
        public IReadOnlyList<CombatantState> Targets() => selectedItem != null ? observedBattle.GetValidItemTargets(selectedItem)
            : observedBattle.GetValidTargets(selected);
        private int TargetRowCount() => selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies)
            ? Mathf.Min(1, Targets().Count) : Targets().Count;
        private void Cancel()
        {
            if (selected != null || selectedItem != null || InSubmenu || defendMode) MenuSounds.Play(MenuSound.Cancel);
            if (selected != null || selectedItem != null) { selected = null; selectedItem = null; targetIndex = 0; }
            else if (InSubmenu)
            {
                itemMode = false; menuGroup = AbilityMenuGroup.Direct; groupAbilities.Clear();
                commandIndex = rootIndex;
                actionsScroll = new Vector2(0, BattleCommandMenu.KeepRowVisible(0, commandIndex, CommandCount(), 120));
            }
            else defendMode = false;
        }
        private void ChooseCommand(int index, bool navigateOnly = false)
        {
            if (index < 0 || index >= CommandCount()) return;
            commandIndex = index;
            if (!CommandEnabled(index)) return;
            if (!InSubmenu && rootCommands[index].Kind != BattleCommandKind.Ability)
            {
                BattleCommandKind kind = rootCommands[index].Kind;
                rootIndex = index; itemMode = kind == BattleCommandKind.Items;
                menuGroup = kind == BattleCommandKind.Magic ? AbilityMenuGroup.Magic
                    : kind == BattleCommandKind.Summon ? AbilityMenuGroup.Summon : AbilityMenuGroup.Direct;
                groupAbilities = itemMode ? new List<AbilityDefinition>() : BattleCommandMenu.Group(observedActor, menuGroup);
                commandIndex = 0; actionsScroll = Vector2.zero;
                MenuSounds.Play(navigateOnly ? MenuSound.Navigate : MenuSound.Confirm); return;
            }
            if (itemMode)
            {
                ItemStack stack = observedBattle.Inventory.Items[index];
                if (stack.Quantity > 0) selectedItem = stack.Definition;
            }
            else
            {
                AbilityDefinition ability = CommandAbility(index);
                if (observedActor.Mp < ability.MpCost || ability.UsesLimit && !observedActor.Limit.IsReady) return;
                if (ability.Targets == TargetRule.Self)
                {
                    if (navigateOnly) return;
                    controller.TryPlayerAction(ability, observedActor);
                }
                else selected = ability;
            }
            MenuSounds.Play(navigateOnly ? MenuSound.Navigate : MenuSound.Confirm);
            targetIndex = 0; targetsScroll = Vector2.zero;
            if (selected != null && selected.Targets == TargetRule.Enemy && preferredEnemyTarget != null)
            {
                IReadOnlyList<CombatantState> valid = Targets();
                for (int i = 0; i < valid.Count; i++)
                    if (valid[i] == preferredEnemyTarget) { targetIndex = i; break; }
                targetsScroll.y = BattleCommandMenu.KeepRowVisible(0, targetIndex, valid.Count, 88);
            }
        }
        private void ExecuteTarget(CombatantState target)
        {
            CombatantState actualTarget = selected != null ? observedBattle.ResolveLivingEnemyTarget(selected, target) : target;
            bool accepted = selectedItem != null ? controller.TryPlayerItem(selectedItem, target)
                : controller.TryPlayerAction(selected, target);
            if (accepted)
            {
                MenuSounds.Play(MenuSound.Confirm);
                if (actualTarget != null && actualTarget.Team == CombatTeam.Enemy)
                    preferredEnemyTarget = actualTarget.IsAlive ? actualTarget : observedBattle.NextLivingEnemyAfter(actualTarget);
                selected = null; selectedItem = null;
            }
        }

        public void DrawActions(GUIStyle title, GUIStyle small, BattleCommandButton drawButton)
        {
            bool targeting = selected != null || selectedItem != null;
            GUI.Label(new Rect(40, 506, 555, 30), observedActor.Definition.Name, title);
            actionsScroll = GUI.BeginScrollView(new Rect(38, 542, 280, 120), actionsScroll, new Rect(0, 0, 253, CommandCount() * 36));
            for (int i = 0; i < CommandCount(); i++)
            {
                if (drawButton(new Rect(0, i * 36, 253, 34), CommandLabel(i), !targeting && i == commandIndex,
                    CommandEnabled(i) && !targeting, itemMode && i < observedBattle.Inventory.Items.Count
                        ? InventoryPixelArt.Texture(observedBattle.Inventory.Items[i].Definition) : null,
                    limitReady: !itemMode && CommandAbility(i)?.UsesLimit == true && observedActor.Limit.IsReady)) { ChooseCommand(i); break; }
            }
            GUI.EndScrollView();
            if (!targeting) return;
            IReadOnlyList<CombatantState> targets = Targets();
            targetIndex = Mathf.Clamp(targetIndex, 0, Mathf.Max(0, targets.Count - 1));
            bool group = selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies);
            targetsScroll = GUI.BeginScrollView(new Rect(333, 542, 280, 88), targetsScroll, new Rect(0, 0, 253, (group ? 1 : targets.Count) * 36));
            for (int i = 0; i < (group ? Mathf.Min(1, targets.Count) : targets.Count); i++)
                if (drawButton(new Rect(0, i * 36, 253, 34), group ? "Todos os alvos (" + targets.Count + ")" : (selected != null && (selected.Targets == TargetRule.Enemy || selected.Targets == TargetRule.AllEnemies)
                        && targets[i].Team == observedActor.Team && targets[i].IsBound ? "Correntes - " : "") + targets[i].Definition.Name,
                    group || targetIndex == i, true)) { targetIndex = i; ExecuteTarget(targets[i]); break; }
            GUI.EndScrollView();
            if (targets.Count == 0) GUI.Label(new Rect(337, 546, 270, 78), "Nenhum alvo valido para esta acao.", small);
        }

        public void ChooseTarget(int index, CombatantState target)
        {
            targetIndex = index;
            ExecuteTarget(target);
        }
    }
}
