using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using DragonQuest.Combat;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private PixelSpriteLibrary pixelSprites;
        private readonly DragonBattleSprites dragonSprites = new DragonBattleSprites();
        private PrototypeBattleSprites battleSprites;
        private readonly SpellAnimation spellAnimation = new SpellAnimation();
        private readonly HeroCastAnimation heroCastAnimation = new HeroCastAnimation();
        private readonly HeroKnockoutSprites knockoutSprites = new HeroKnockoutSprites();
        private readonly HeroKnockoutPresentation heroKnockouts = new HeroKnockoutPresentation();
        private readonly UltimateAnimation ultimateAnimation = new UltimateAnimation();
        private readonly SummonAnimation summonAnimation = new SummonAnimation();
        private readonly SpiritAttackAnimation spiritAttacks = new SpiritAttackAnimation();
        private readonly ChainPrisonAnimation chainPrisons = new ChainPrisonAnimation();
        private readonly BattleStatusIcons statusIcons = new BattleStatusIcons();
        private readonly CastleSpellAnimation castleSpells = new CastleSpellAnimation();
        private readonly CastleCastCounter castCounter = new CastleCastCounter();
        private Texture2D pixel, hand, dragonPortrait, spiritPortrait, kingPortrait, background;
        private GUIStyle title, text, small, centered, button, itemButton, number, castNumber;
        private GUISkin battleSkin;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private BattleSession observedBattle;
        private CombatantState observedActor;
        private CombatantState preferredEnemyTarget;
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
        private readonly Dictionary<CombatantState, int> seenChanges = new Dictionary<CombatantState, int>();
        private readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
        private readonly DefeatedEnemyPresentation defeatedEnemies = new DefeatedEnemyPresentation();
        private readonly Dictionary<CombatantState, EnemySpritePose> enemyPoses = new Dictionary<CombatantState, EnemySpritePose>();
        private readonly struct EnemySpritePose
        {
            public readonly int Frame;
            public readonly AbilityDefinition Ability;
            public readonly string DragonResource;
            public EnemySpritePose(int frame, AbilityDefinition ability, string dragonResource)
            { Frame = frame; Ability = ability; DragonResource = dragonResource; }
        }
        private sealed class FloatingNumber
        {
            public CombatantState Target;
            public CombatVitalChange Change;
            public float Started;
            public int Lane;
        }

        public void Initialize(PrototypeBattleController battleController, PixelSpriteLibrary sprites = null)
        { controller = battleController; pixelSprites = sprites; }

        private float spiritFloatStartedAt;
        private CombatantDefinition observedDefinition;
        private bool observedLimitReady;

        private void ObserveBattle()
        {
            BattleSession battle = controller.Session;
            if (observedBattle != battle)
            {
                observedBattle = battle; observedActor = null; seenChanges.Clear(); numbers.Clear();
                defeatedEnemies.Reset(); castCounter.Reset(); enemyPoses.Clear();
                heroKnockouts.Reset();
                ultimateAnimation.Prepare();
                spiritFloatStartedAt = Time.unscaledTime;
                preferredEnemyTarget = null;
                // Optional map-specific art; the menu and fighter anchors do not depend on the background.
                background = Resources.Load<Texture2D>("BattleBackgrounds/" + controller.BattleLocationId);
                if (controller.IsCastleEncounter) { castleSpells.Prepare(); spiritAttacks.Prepare(); }
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
            foreach (CombatantState participant in battle.Combatants)
            {
                float deathTime = Time.unscaledTime;
                if (controller.IsPresentingAction)
                    foreach (CombatantState target in controller.PresentingTargets)
                        if (target == participant) { deathTime = controller.ActionImpactAt; break; }
                defeatedEnemies.Observe(participant, deathTime);
                float heroImpact = deathTime;
                if (controller.IsPresentingAction && controller.PresentingAbility.PartyEffect != PartyEffect.None)
                    foreach (CombatantState ally in controller.PresentingPartyTargets)
                        if (ally == participant) { heroImpact = controller.ActionImpactAt; break; }
                heroKnockouts.Observe(participant, heroImpact, Time.unscaledTime);
                seenChanges.TryGetValue(participant, out int seen);
                foreach (CombatVitalChange change in participant.VitalChanges)
                {
                    if (change.Sequence <= seen) continue;
                    int lane = 0;
                    foreach (FloatingNumber existing in numbers) if (existing.Target == participant) lane++;
                    numbers.Add(new FloatingNumber { Target = participant, Change = change,
                        Started = controller.IsPresentingAction
                            ? UltimateTimeline.IsOmnislash(controller.PresentingAbility) && change.ActionHitIndex > 0
                                ? controller.ActionStartedAt + UltimateTimeline.HitTime(change.ActionHitIndex) : controller.ActionImpactAt
                            : Time.unscaledTime, Lane = lane % 3 });
                    seenChanges[participant] = change.Sequence;
                }
            }
            numbers.RemoveAll(n => Time.unscaledTime - n.Started > 1.4f);
            if (controller.IsCastleEncounter)
                castCounter.Observe(controller.PresentingActor?.Id, controller.PresentingAbility,
                    controller.CastleRules.MeteorTurnsElapsed, controller.ActionStartedAt);
        }

        private void Update()
        {
            if (controller == null || !controller.IsActive || controller.IsPostBattleOpen) { numbers.Clear(); return; }
            ObserveBattle();
            if (controller.IsCheatMenuBlocking) return;
            if (controller.IsPresentingAction) return;
            Keyboard keys = Keyboard.current;
            if (!GameInput.IsFrameAvailable) return;
            if (observedBattle.Outcome != BattleOutcome.None)
            {
                if (GameInput.ConfirmPressed) { controller.ReturnToCity(); GameInput.ConsumeGamepadFrame(); }
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
                    defendMode = true;
                }
                else if (targeting || InSubmenu) Cancel();
                GameInput.ConsumeGamepadFrame(); return;
            }
            if (rightPressed && !targeting && !InSubmenu && defendMode && commandIndex == 0)
            { defendMode = false; GameInput.ConsumeGamepadFrame(); return; }
            int count = targeting ? TargetRowCount() : CommandCount();
            if (targeting) targetIndex = Mathf.Clamp(targetIndex, 0, Mathf.Max(0, count - 1));
            int delta = navigation == MenuDirection.Down || keys?.downArrowKey.wasPressedThisFrame == true || keys?.sKey.wasPressedThisFrame == true ? 1
                : navigation == MenuDirection.Up || keys?.upArrowKey.wasPressedThisFrame == true || keys?.wKey.wasPressedThisFrame == true ? -1 : 0;
            if (count > 0 && delta != 0)
            {
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

        private bool CanAcceptMenuInput => GameInput.IsFrameAvailable && !controller.IsCheatMenuBlocking
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
        private IReadOnlyList<CombatantState> Targets() => selectedItem != null ? observedBattle.GetValidItemTargets(selectedItem)
            : observedBattle.GetValidTargets(selected);
        private int TargetRowCount() => selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies)
            ? Mathf.Min(1, Targets().Count) : Targets().Count;
        private void Cancel()
        {
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
                commandIndex = 0; actionsScroll = Vector2.zero; return;
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
                if (actualTarget != null && actualTarget.Team == CombatTeam.Enemy)
                    preferredEnemyTarget = actualTarget.IsAlive ? actualTarget : observedBattle.NextLivingEnemyAfter(actualTarget);
                selected = null; selectedItem = null;
            }
        }

        private void OnGUI()
        {
            if (controller == null || !controller.IsActive || controller.IsPostBattleOpen) return;
            // Keyboard commands belong to Update; IMGUI must not confirm the new selection again.
            if (Event.current.isKey) Event.current.Use();
            EnsureStyles(); ObserveBattle();
            GUISkin previousSkin = GUI.skin;
            GUI.skin = battleSkin;
            RectBox(new Rect(0, 0, Screen.width, Screen.height), new Color(.015f, .02f, .045f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Matrix4x4 arenaMatrix = GUI.matrix;
            if (controller.IsPresentingAction && controller.IsPresentingUltimate)
            {
                float impactAge = Time.unscaledTime - controller.ActionImpactAt;
                if (impactAge >= 0 && impactAge < .3f && controller.PresentingAbility.Effect == AbilityEffect.PhysicalDamage)
                {
                    float strength = (1-impactAge/.3f)*7;
                    GUI.matrix *= Matrix4x4.Translate(new Vector3(Mathf.Sin(impactAge*103)*strength,Mathf.Cos(impactAge*79)*strength*.5f,0));
                }
            }
            DrawArena();
            GUI.matrix = arenaMatrix;
            Panel(new Rect(20, 426, 1240, 60));
            if (controller.IsPresentingAction && controller.IsPresentingUltimate)
                GUI.Label(new Rect(40, 426, 1200, 60), controller.PresentingAbility.Name, centered);
            else
                GUI.Label(new Rect(40, 438, 1200, 40), controller.PresentedMessage, text);
            // Equal halves: all commands/target choices stay on the left, party status stays on the right.
            Panel(new Rect(20, 496, 620, 204)); Panel(new Rect(640, 496, 620, 204));
            DrawPartyStatus();
            if (controller.IsPresentingAction)
            {
                GUI.Label(new Rect(40, 512, 570, 30), controller.PresentingActor.Definition.Name, title);
                if (!controller.IsCastleEncounter || controller.PresentingActor.Team == CombatTeam.Party)
                    GUI.Label(new Rect(40, 556, 570, 55), controller.PresentingAbility.Name, text);
            }
            else if (observedBattle.Outcome != BattleOutcome.None)
            {
                GUI.Label(new Rect(40, 508, 575, 30), observedBattle.Outcome == BattleOutcome.Completed ? "DESFECHO"
                    : observedBattle.Outcome == BattleOutcome.Victory ? "VITORIA!" : "DERROTA", title);
                GUI.Label(new Rect(40, 546, 575, 75), controller.OutcomeDescription, text);
                if (CommandButton(new Rect(40, 645, 565, 36), controller.ReturnLabel, true, true)) controller.ReturnToCity();
            }
            else if (observedActor != null && observedActor.Team == CombatTeam.Enemy)
            {
                GUI.Label(new Rect(40, 512, 570, 30), "Turno de " + observedActor.Definition.Name, title);
            }
            else if (observedActor != null) DrawActions();
            if (!string.IsNullOrEmpty(controller.ErrorMessage)) GUI.Label(new Rect(40, 668, 560, 25), controller.ErrorMessage, small);
            if (!string.IsNullOrEmpty(GUI.tooltip)) GUI.Label(new Rect(660, 683, 585, 18), GUI.tooltip, small);
            GUI.skin = previousSkin;
            GUI.matrix = previous;
        }

        private void DrawArena()
        {
            Matrix4x4 arenaMatrix = GUI.matrix;
            bool spiritAction = controller.IsPresentingAction && controller.PresentingActor.Id == CastleBattleRules.SpiritId;
            float spiritElapsed = Time.unscaledTime - controller.ActionStartedAt;
            if (spiritAction)
            {
                Vector2 shake = SpiritAttackAnimation.ScreenOffset(controller.PresentingAbility, spiritElapsed);
                GUI.matrix *= Matrix4x4.Translate(new Vector3(shake.x, shake.y, 0));
            }
            GUI.BeginGroup(new Rect(0, 0, 1280, 416));
            Rect arena = new Rect(20, 20, 1240, 396);
            Color sky = new Color(.10f, .17f, .20f), ground = new Color(.18f, .23f, .18f);
            switch (controller.BattleLocationId)
            {
                case PrototypeTravel.Volcano: sky = new Color(.20f, .08f, .06f); ground = new Color(.28f, .12f, .08f); break;
                case PrototypeTravel.Castle: sky = new Color(.10f, .09f, .17f); ground = new Color(.20f, .19f, .25f); break;
                case PrototypeTravel.EastForest: sky = new Color(.06f, .15f, .12f); ground = new Color(.13f, .23f, .14f); break;
                case PrototypeTravel.WestForest: sky = new Color(.25f, .24f, .21f); ground = new Color(.34f, .32f, .27f); break;
                case PrototypeTravel.Ruins: sky = new Color(.17f, .16f, .13f); ground = new Color(.25f, .23f, .18f); break;
            }
            RectBox(arena, sky); RectBox(new Rect(20, 240, 1240, 176), ground);
            if (background != null) GUI.DrawTexture(arena, background, ScaleMode.ScaleAndCrop);
            if (spiritAction) spiritAttacks.DrawBackdrop(arena, controller.PresentingAbility, spiritElapsed);
            bool ultimate = controller.IsPresentingAction && controller.IsPresentingUltimate;
            float ultimateElapsed = Time.unscaledTime - controller.ActionStartedAt;
            var ultimateAllies = new List<Rect>();
            var ultimateTargets = new List<Rect>();
            var ultimateRevived = new List<bool>();
            Rect ultimateCaster = ultimate ? FigureRect(controller.PresentingActor) : default;
            Rect ultimateFocus = ultimate && controller.PresentingTargets.Count > 0 ? FigureRect(controller.PresentingTargets[0]) : ultimateCaster;
            if (ultimate)
            {
                foreach (CombatantState ally in controller.PresentingPartyTargets)
                {
                    if (!ally.IsAlive) continue;
                    ultimateAllies.Add(FigureRect(ally));
                    bool revived = false;
                    foreach (CombatantState fallen in controller.PresentingRevivedTargets) if (fallen == ally) revived = true;
                    ultimateRevived.Add(revived);
                }
                foreach (CombatantState target in controller.PresentingTargets) ultimateTargets.Add(FigureRect(target));
                ultimateAnimation.DrawBackdrop(controller.PresentingAbility, ultimateElapsed, ultimateCaster, ultimateAllies);
            }
            IReadOnlyList<CombatantState> targets = Targets();
            foreach (CombatantState participant in observedBattle.Combatants)
            {
                if (!defeatedEnemies.IsVisible(participant, Time.unscaledTime)) continue;
                Rect groundFigure = FigureRect(participant, false);
                Rect figure = FigureRect(participant);
                float castStep = controller.IsPresentingAction && controller.PresentingActor == participant
                    && HeroCastAnimation.IsCasting(controller.PresentingAbility)
                    ? HeroCastAnimation.Step(Time.unscaledTime - controller.ActionStartedAt, controller.ActionDuration) : 0;
                bool omnislashActor = ultimate && UltimateTimeline.IsOmnislash(controller.PresentingAbility) && participant == controller.PresentingActor;
                Rect shadowFigure = ultimate && controller.PresentingAbility.SourceAbilityId == "bram-aegis"
                    && participant == controller.PresentingActor ? UltimateAnimation.BramRect(figure, ultimateElapsed) : figure;
                if (!omnislashActor)
                    RectBox(new Rect(shadowFigure.x + castStep - 8, groundFigure.yMax - 8, figure.width + 16, 10),
                        new Color(0, 0, 0, .25f * defeatedEnemies.Alpha(participant, Time.unscaledTime)));
                DrawFighter(figure, participant);
                chainPrisons.Draw(figure, controller.ChainsVisible(participant), Time.unscaledTime, controller.ChainBreakAge(participant));
                if (participant.IsAlive)
                    statusIcons.DrawOverhead(figure, participant.MindControlTurns > 0
                        || controller.IsPresentingAction && controller.IsPresentingConfusedAction && controller.PresentingActor == participant,
                        participant.IsSilenced, Time.unscaledTime);
                if (!ultimate && participant.IsAlive && participant == (controller.IsPresentingAction ? controller.PresentingActor : observedActor))
                    DrawTurnArrow(figure.center.x + castStep, figure.y - 30 + Mathf.Sin(Time.unscaledTime * 5) * 3);
                bool group = selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != participant) continue;
                    if (group || i == targetIndex) DrawHand(new Rect(figure.x - 60, figure.center.y - 18, 56, 36));
                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = previousEnabled && CanAcceptMenuInput;
                    bool clicked = GUI.Button(figure, GUIContent.none, GUIStyle.none);
                    GUI.enabled = previousEnabled;
                    if (clicked) { GameInput.ConsumeGamepadFrame(); targetIndex = i; ExecuteTarget(participant); }
                }
            }
            if (controller.IsPresentingAction && controller.IsPresentingSummon)
            {
                float elapsed = Time.unscaledTime - controller.ActionStartedAt;
                summonAnimation.Draw(controller.PresentingAbility, elapsed);
                AbilityDefinition impact = SummonAnimation.ImpactEffect(controller.PresentingAbility);
                if (elapsed >= SummonAnimation.ImpactDelay - .15f)
                    foreach (CombatantState target in controller.PresentingTargets)
                        spellAnimation.Draw(FigureRect(target), impact,
                            elapsed - SummonAnimation.ImpactDelay + SpellAnimation.ImpactDelay);
                if (controller.PresentingAbility.PartyEffect == PartyEffect.RestoreAndRevive
                    && elapsed >= SummonAnimation.ImpactDelay - .15f)
                    foreach (CombatantState ally in controller.PresentingPartyTargets)
                        spellAnimation.Draw(FigureRect(ally), PrototypeBattleFactory.Heal,
                            elapsed - SummonAnimation.ImpactDelay + SpellAnimation.ImpactDelay);
            }
            if (controller.IsPresentingAction && controller.IsPresentingSpell)
                foreach (CombatantState target in controller.PresentingTargets)
                    spellAnimation.Draw(FigureRect(target), controller.PresentingAbility,
                        Time.unscaledTime - controller.ActionStartedAt);
            if (spiritAction)
            {
                var spiritTargets = new List<Rect>();
                foreach (CombatantState target in controller.PresentingTargets) spiritTargets.Add(FigureRect(target));
                spiritAttacks.DrawEffects(FigureRect(controller.PresentingActor), spiritTargets,
                    controller.PresentingAbility, spiritElapsed);
            }
            bool meteorAction = controller.IsPresentingAction
                && CastleMeteorPresentation.IsMeteor(controller.PresentingActor.Id, controller.PresentingAbility);
            if (controller.IsCastleEncounter)
            {
                var rules = controller.CastleRules;
                if (controller.IsPresentingAction && controller.PresentingAbility == CastleBattleRules.Protect)
                    castleSpells.DrawProtect(FigureRect(controller.PresentingActor), Time.unscaledTime - controller.ActionStartedAt);
                if (meteorAction)
                {
                    Rect target = new Rect(740, 170, 160, 205);
                    foreach (CombatantState member in observedBattle.Combatants)
                        if (member.Id == rules.MeteorAbsorberId) { target = FigureRect(member); break; }
                    castleSpells.DrawMeteor(target, rules.MeteorAbsorbed, Time.unscaledTime - controller.ActionStartedAt);
                }
            }
            if (ultimate)
                ultimateAnimation.DrawEffects(controller.PresentingAbility, ultimateElapsed, ultimateCaster, ultimateFocus,
                    ultimateTargets, ultimateAllies, ultimateRevived);
            foreach (FloatingNumber floating in numbers)
            {
                Rect figure = FigureRect(floating.Target);
                float elapsed = Time.unscaledTime - floating.Started;
                if (elapsed < 0) continue;
                if (floating.Target.Id == CastleBattleRules.TyrantId && floating.Change.IsShielded
                    && !floating.Change.IsMp && floating.Change.Amount < 0)
                    castleSpells.DrawShieldHit(figure, elapsed);
                Color color = BattleStatusColors.VitalChange(floating.Change);
                bool swordHit = floating.Change.ActionHitIndex > 0;
                float numberDuration = swordHit ? .72f : 1.4f;
                if (elapsed >= numberDuration) continue;
                color.a = Mathf.Clamp01((numberDuration - elapsed) / .25f);
                string label = (floating.Change.Amount > 0 ? "+" : "") + System.Math.Abs(floating.Change.Amount)
                    + (floating.Change.IsMp ? " MP" : "");
                Rect rect = new Rect(figure.center.x - 90 + floating.Lane * (swordHit ? 40 : 16),
                    figure.y - 10 - elapsed * (swordHit ? 95 : 40) - floating.Lane * 22, 180, 45);
                number.normal.textColor = new Color(0, 0, 0, color.a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, number);
                number.normal.textColor = color; GUI.Label(rect, label, number);
            }
            if (controller.IsCastleEncounter && (controller.CastleRules.Ending == GameEnding.None || meteorAction)
                && castCounter.IsVisible(Time.unscaledTime))
                foreach (CombatantState member in observedBattle.Combatants)
                    if (member.Id == CastleBattleRules.TyrantId) { DrawCastNumber(FigureRect(member)); break; }
            GUI.EndGroup();
            GUI.matrix = arenaMatrix;
        }

        private void DrawCastNumber(Rect figure)
        {
            float elapsed = Time.unscaledTime - castCounter.StartedAt;
            float alpha = Mathf.Clamp01((CastleMeteorPresentation.TurnNumberDuration - elapsed) / .35f);
            // Mesmo movimento, sombra e fade dos danos; acima deles para nao sobrepor os digitos.
            Rect rect = new Rect(figure.center.x - 90, figure.y - 56 - elapsed * 40, 180, 54);
            string label = castCounter.DisplayValue.ToString();
            castNumber.normal.textColor = new Color(0, 0, 0, alpha);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, castNumber);
            castNumber.normal.textColor = new Color(1, 1, 1, alpha);
            GUI.Label(rect, label, castNumber);
        }

        private Rect FigureRect(CombatantState participant, bool floating = true)
        {
            int index = 0;
            foreach (CombatantState other in observedBattle.Combatants)
            { if (other == participant) break; if (other.Team == participant.Team) index++; }
            if (participant.Team == CombatTeam.Party) return new Rect(925 + (index % 2) * 60,
                background != null ? 125 + index * 78 : 103 + index * 83, 64, 90);
            if (participant.Id == DragonProgress.EnemyId) return new Rect(160, 110, 350, 250);
            if (participant.Id == CastleBattleRules.SpiritId)
            {
                float lift = floating ? 175 * SpiritFloatAnimation.HeightAt(Time.unscaledTime - spiritFloatStartedAt) : 0;
                return new Rect(215, 90 - lift, 170, 175);
            }
            if (participant.Id == CastleBattleRules.TyrantId) return new Rect(410, 169, 80, 110);
            return new Rect(260 + index * 145, 155 + index * 72, 80, 110);
        }

        private void DrawActions()
        {
            bool targeting = selected != null || selectedItem != null;
            GUI.Label(new Rect(40, 506, 555, 30), observedActor.Definition.Name, title);
            actionsScroll = GUI.BeginScrollView(new Rect(38, 542, 280, 120), actionsScroll, new Rect(0, 0, 253, CommandCount() * 36));
            for (int i = 0; i < CommandCount(); i++)
            {
                if (CommandButton(new Rect(0, i * 36, 253, 34), CommandLabel(i), !targeting && i == commandIndex,
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
                if (CommandButton(new Rect(0, i * 36, 253, 34), group ? "Todos os alvos (" + targets.Count + ")" : (selected != null && (selected.Targets == TargetRule.Enemy || selected.Targets == TargetRule.AllEnemies)
                        && targets[i].Team == observedActor.Team && targets[i].IsBound ? "Correntes - " : "") + targets[i].Definition.Name,
                    group || targetIndex == i, true)) { targetIndex = i; ExecuteTarget(targets[i]); break; }
            GUI.EndScrollView();
            if (targets.Count == 0) GUI.Label(new Rect(337, 546, 270, 78), "Nenhum alvo valido para esta acao.", small);
        }

        private void DrawPartyStatus()
        {
            GUI.Label(new Rect(850, 508, 170, 28), "HP", centered);
            GUI.Label(new Rect(1030, 508, 120, 28), "MP", centered);
            GUI.Label(new Rect(1155, 508, 80, 28), "LIMIT", centered);
            int i = 0;
            foreach (CombatantState participant in observedBattle.Combatants)
            {
                if (participant.Team != CombatTeam.Party) continue;
                float y = 544 + i++ * 48;
                if (participant == observedActor) RectBox(new Rect(650, y - 2, 600, 45), new Color(.16f, .29f, .65f));
                GUI.Label(new Rect(665, y, 95, 25), participant.Definition.Name, text);
                var beforeSalvation = controller.PendingSalvationState(participant);
                statusIcons.Draw(770, y + 2, beforeSalvation?.Statuses ?? CombatStatuses.Active(participant));
                DrawBar(new Rect(850, y + 4, 170, 25), beforeSalvation?.Hp ?? participant.Hp, participant.Definition.MaxHp, new Color(.18f, .60f, .47f));
                DrawBar(new Rect(1030, y + 4, 120, 25), participant.Mp, participant.Definition.MaxMp, new Color(.20f, .45f, .82f));
                DrawBar(new Rect(1160, y + 4, 75, 25), participant.Limit.Charge, 100, new Color(.62f, .28f, .75f));
            }
        }

        private void DrawBar(Rect rect, int value, int maximum, Color color)
        {
            RectBox(rect, new Color(.015f, .035f, .14f));
            RectBox(new Rect(rect.x, rect.y, rect.width * (maximum == 0 ? 0 : Mathf.Clamp01((float)value / maximum)), rect.height), color);
            GUI.Label(rect, value + "/" + maximum, centered);
        }
        private bool CommandButton(Rect rect, string label, bool focused, bool enabled, Texture2D icon = null, bool limitReady = false)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled && CanAcceptMenuInput;
            bool clicked = LimitCommandGlow.Button(rect, label,
                limitReady,
                icon == null ? button : itemButton);
            InventoryPixelArt.Draw(new Rect(rect.x + 50, rect.center.y - 14, 28, 28), icon);
            GUI.enabled = previous;
            if (focused) DrawHand(new Rect(rect.x + 2, rect.center.y - 15, 44, 30));
            if (clicked) GameInput.ConsumeGamepadFrame();
            return clicked;
        }
        private void DrawTurnArrow(float x, float y)
        {
            RectBox(new Rect(x - 7, y, 14, 12), Color.white);
            for (int row = 0; row < 7; row++) RectBox(new Rect(x - 13 + row * 2, y + 12 + row * 2, 26 - row * 4, 2), Color.white);
        }
        private void DrawFighter(Rect rect, CombatantState participant)
        {
            Color tint = FighterTint(participant);
            bool acting = controller.IsPresentingAction && controller.PresentingActor == participant;
            if (heroKnockouts.IsKnockedOut(participant) && knockoutSprites.Draw(rect, participant.Id)) return;
            if (controller.IsPresentingAction && controller.PresentingActor.Id == CastleBattleRules.SpiritId)
                foreach (CombatantState target in controller.PresentingTargets)
                    if (target == participant)
                    {
                        rect.position += SpiritAttackAnimation.TargetOffset(controller.PresentingAbility,
                            Time.unscaledTime - controller.ActionStartedAt);
                        break;
                    }
            if (controller.IsPresentingAction)
            {
                float sinceImpact = controller.IsPresentingUltimate
                    ? UltimateTimeline.HitAge(controller.PresentingAbility, Time.unscaledTime - controller.ActionStartedAt)
                    : Time.unscaledTime - controller.ActionImpactAt;
                foreach (CombatantState target in controller.PresentingTargets)
                    if (target == participant && !controller.IsChainBreakTarget(participant) && sinceImpact >= 0 && sinceImpact < .22f
                        && (controller.PresentingAbility.Effect == AbilityEffect.PhysicalDamage
                            || controller.PresentingAbility.Effect == AbilityEffect.MagicDamage
                            || controller.PresentingAbility.Effect == AbilityEffect.Drain))
                    { rect.x += Mathf.Sin(sinceImpact * 75f) * (controller.IsPresentingUltimate ? 9 : 5); break; }
            }
            if (acting && controller.IsPresentingUltimate && battleSprites != null)
            {
                Rect focus = controller.PresentingTargets.Count > 0 ? FigureRect(controller.PresentingTargets[0]) : rect;
                if (ultimateAnimation.DrawActor(rect, focus, participant.Id, controller.PresentingAbility,
                    Time.unscaledTime - controller.ActionStartedAt, tint, battleSprites, heroCastAnimation)) return;
            }
            bool meteorPresentation = controller.IsPresentingAction
                && CastleMeteorPresentation.IsMeteor(controller.PresentingActor.Id, controller.PresentingAbility);
            bool protectPresentation = controller.IsPresentingAction
                && controller.PresentingAbility == CastleBattleRules.Protect;
            if (participant.Id == CastleBattleRules.TyrantId && battleSprites != null)
            {
                // Shield termina sobre a postura normal; so entao aparece o sprite de conjuracao.
                if (protectPresentation && DrawEnemySprite(rect, participant, 0, tint)) return;
                if (CastleMeteorPresentation.HoldCastPose(controller.CastleRules, meteorPresentation, protectPresentation)
                    && DrawEnemySprite(rect, participant, CastleMeteorPresentation.ChargePose(controller.CastleRules),
                        tint, CastleBattleRules.PrepareMeteor)) return;
            }
            // Every action has readable movement, even when it has no dedicated pose sheet.
            if (acting && !HeroCastAnimation.IsCasting(controller.PresentingAbility)
                && !PrototypeBattleSprites.HasAnimation(participant.Id, controller.PresentingAbility)
                && !controller.IsPresentingDragonAttack)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - controller.ActionStartedAt) / controller.ActionDuration);
                rect.x += Mathf.Sin(progress * Mathf.PI) * (participant.Team == CombatTeam.Party ? -18 : 18);
                rect.y -= Mathf.Sin(progress * Mathf.PI) * 5;
            }
            if (participant.Id == CastleBattleRules.SpiritId && acting
                && spiritAttacks.Draw(rect, controller.PresentingAbility, Time.unscaledTime - controller.ActionStartedAt, tint)) return;
            if (participant.Team == CombatTeam.Enemy && battleSprites != null
                && DrawEnemySprite(rect, participant, acting ? controller.AttackPose : 0,
                    tint, acting ? controller.PresentingAbility : null)) return;
            if (participant.Team == CombatTeam.Party && battleSprites != null)
            {
                bool attacking = controller.IsPresentingAction && controller.PresentingActor == participant;
                if (attacking && HeroCastAnimation.IsCasting(controller.PresentingAbility)
                    && heroCastAnimation.Draw(rect, participant.Id, controller.PresentingAbility,
                        Time.unscaledTime - controller.ActionStartedAt, controller.ActionDuration,
                        tint)) return;
                bool casting = attacking && HeroCastAnimation.IsCasting(controller.PresentingAbility);
                int pose = attacking && !casting ? controller.AttackPose : 0;
                if (battleSprites.Draw(rect, participant.Id, pose, tint,
                    attacking && !casting ? controller.PresentingAbility : null)) return;
            }
            if (pixelSprites != null && participant.Id == DragonProgress.EnemyId)
            {
                bool attacking = controller.IsPresentingAction && controller.IsPresentingDragonAttack;
                string abilityId = controller.PresentingAbility?.Id;
                bool charged = participant.IsAlive && participant.IsCharging;
                string resource = attacking ? DragonAttackAnimation.Resource(abilityId) : charged ? "DragonCharge" : "Dragon";
                int frame = attacking ? DragonAttackAnimation.Frame(abilityId, Time.unscaledTime - controller.ActionStartedAt) : charged ? 5 : 0;
                if (DrawEnemySprite(rect, participant, frame, tint, dragonResource: resource)) return;
            }
            if (pixelSprites != null && participant.Team == CombatTeam.Party)
            {
                pixelSprites.Party(participant.Id).Draw(rect, (int)PixelDirection.Left,
                    tint); return;
            }
            if (pixelSprites != null && participant.Team == CombatTeam.Enemy && participant.Id != DragonProgress.EnemyId && participant.Id != CastleBattleRules.SpiritId)
            {
                pixelSprites.Atlas("NpcRoster").Draw(rect, participant.Id == CastleBattleRules.TyrantId ? 11 : 10,
                    tint); return;
            }
            Texture2D portrait=participant.Id==DragonProgress.EnemyId?dragonPortrait:participant.Id==CastleBattleRules.SpiritId?spiritPortrait:participant.Id==CastleBattleRules.TyrantId?kingPortrait:null;
            if(portrait!=null)
            {
                Color previous=GUI.color;GUI.color=tint;
                GUI.DrawTexture(rect,portrait,ScaleMode.ScaleToFit);GUI.color=previous;return;
            }
            Color clothes = participant.Team == CombatTeam.Enemy ? new Color(0.69f, 0.29f, 0.27f)
                : participant.Id == "lia" ? new Color(0.8f, 0.86f, 0.74f)
                : participant.Id == "bram" ? new Color(0.37f, 0.58f, 0.7f) : new Color(0.85f, 0.68f, 0.28f);
            clothes *= tint;
            RectBox(new Rect(rect.x, rect.y + rect.height * 0.30f, rect.width, rect.height * 0.45f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.25f, rect.y, rect.width * 0.5f, rect.height * 0.28f),
                new Color(0.9f, 0.77f, 0.59f) * tint);
            RectBox(new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.58f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
        }

        private bool DrawEnemySprite(Rect rect, CombatantState participant, int frame, Color tint,
            AbilityDefinition ability = null, string dragonResource = null)
        {
            // Defeat clears charge/cast state before the impact is drawn; keep the last visible pose.
            EnemySpritePose pose;
            if (participant.IsAlive || !enemyPoses.TryGetValue(participant, out pose))
                pose = new EnemySpritePose(frame, ability, dragonResource);
            bool drawn = pose.DragonResource != null
                ? dragonSprites.Draw(rect, pose.DragonResource, pose.Frame, tint)
                : battleSprites != null && battleSprites.Draw(rect, participant.Id, pose.Frame, tint, pose.Ability);
            if (drawn && participant.IsAlive) enemyPoses[participant] = pose;
            return drawn;
        }

        private Color FighterTint(CombatantState participant)
        {
            if (participant.Team == CombatTeam.Party)
            {
                var beforeSalvation = controller.PendingSalvationState(participant);
                if (beforeSalvation != null) return beforeSalvation.Tint;
                if (controller.IsPresentingAction && controller.IsPresentingUltimate && Time.unscaledTime < controller.ActionImpactAt)
                    foreach (CombatantState fallen in controller.PresentingRevivedTargets)
                        if (fallen == participant) return new Color(.4f, .4f, .4f);
                bool beforeMeteorImpact = controller.IsPresentingAction
                    && CastleMeteorPresentation.IsMeteor(controller.PresentingActor.Id, controller.PresentingAbility)
                    && Time.unscaledTime < controller.ActionImpactAt;
                return participant.IsAlive ? BattleStatusColors.Tint(participant) : beforeMeteorImpact ? Color.white : new Color(.4f, .4f, .4f);
            }
            Color tint = defeatedEnemies.IsRed(participant, Time.unscaledTime) ? new Color(1, .12f, .12f) : BattleStatusColors.Tint(participant);
            tint.a = defeatedEnemies.Alpha(participant, Time.unscaledTime);
            return tint;
        }


        private void Panel(Rect rect)
        {
            RectBox(rect, new Color(.06f, .07f, .18f));
            RectBox(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(.82f, .86f, .97f));
            Rect inner = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);
            for (int row = 0; row < 24; row++) RectBox(new Rect(inner.x, inner.y + inner.height * row / 24, inner.width, inner.height / 24 + 1),
                Color.Lerp(new Color(.09f, .20f, .58f), new Color(.015f, .04f, .23f), row / 23f));
        }
        private void RectBox(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }
        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            battleSkin = Instantiate(GUI.skin);
            battleSkin.verticalScrollbar = new GUIStyle { fixedWidth = 12, stretchHeight = true };
            battleSkin.verticalScrollbarUpButton = new GUIStyle { fixedWidth = 12 };
            battleSkin.verticalScrollbarDownButton = new GUIStyle { fixedWidth = 12 };
            battleSkin.verticalScrollbarThumb = new GUIStyle { fixedWidth = 12, stretchHeight = true };
            foreach (GUIStyleState state in new[] { battleSkin.verticalScrollbarThumb.normal,
                battleSkin.verticalScrollbarThumb.hover, battleSkin.verticalScrollbarThumb.active,
                battleSkin.verticalScrollbarThumb.focused, battleSkin.verticalScrollbarThumb.onNormal,
                battleSkin.verticalScrollbarThumb.onHover, battleSkin.verticalScrollbarThumb.onActive,
                battleSkin.verticalScrollbarThumb.onFocused }) state.background = pixel;
            battleSprites = new PrototypeBattleSprites();
            dragonPortrait = Resources.Load<Texture2D>("DragonPortrait");
            spiritPortrait = Resources.Load<Texture2D>("PixelArt/SpiritBattle"); kingPortrait = Resources.Load<Texture2D>("KingPortrait");
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            text = PrototypeTypography.Style(GUI.skin.label, 24); text.wordWrap = true;
            small = PrototypeTypography.Style(GUI.skin.label, 16); small.wordWrap = true;
            centered = PrototypeTypography.Style(GUI.skin.label, 24); centered.alignment = TextAnchor.MiddleCenter;
            number = PrototypeTypography.Style(centered, 40, true);
            castNumber = PrototypeTypography.Style(number, number.fontSize + CastleMeteorPresentation.TurnNumberFontIncrease, true);
            button = PrototypeTypography.Style(GUI.skin.label, 24);
            button.alignment = TextAnchor.MiddleLeft; button.padding = new RectOffset(50, 4, 2, 2);
            itemButton = InventoryPixelArt.ButtonStyle(button, 86);
            foreach (GUIStyle style in new[] { title, text, small, centered, button, itemButton })
            { style.normal.textColor = Color.white; style.hover.textColor = Color.white; style.active.textColor = Color.white; }
            LoadHandSprite();
        }
        private void LoadHandSprite()
        {
            hand = Resources.Load<Texture2D>("PixelArt/BattleGloveCursor");
            if (hand == null) return;
            // Keep the original PNG intact; trim transparent margins only through drawing UVs.
            Color32[] pixels = hand.GetPixels32();
            int left = hand.width, right = -1, bottom = hand.height, top = -1;
            for (int y = 0; y < hand.height; y++) for (int x = 0; x < hand.width; x++)
            {
                if (pixels[y * hand.width + x].a <= 16) continue;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
            if (right < left || top < bottom) return;
            int width = right - left + 1, height = top - bottom + 1;
            handUv = new Rect((float)left / hand.width, (float)bottom / hand.height,
                (float)width / hand.width, (float)height / hand.height);
            handAspect = (float)width / height;
        }

        private void DrawHand(Rect area)
        {
            if (hand == null) return;
            float width = Mathf.Min(area.width, area.height * handAspect);
            float height = width / handAspect;
            GUI.DrawTextureWithTexCoords(new Rect(area.x, area.center.y - height / 2, width, height), hand, handUv, true);
        }

        private void OnDestroy()
        {
            if (battleSkin != null) Destroy(battleSkin);
            if (pixel != null) Destroy(pixel);
        }
    }
}
