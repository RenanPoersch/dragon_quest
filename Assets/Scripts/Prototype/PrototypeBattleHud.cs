using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private PrototypeBattleSprites battleSprites;
        private readonly SpellAnimation spellAnimation = new SpellAnimation();
        private readonly HeroCastAnimation heroCastAnimation = new HeroCastAnimation();
        private readonly SummonAnimation summonAnimation = new SummonAnimation();
        private Texture2D pixel, hand, dragonPortrait, spiritPortrait, kingPortrait, background;
        private GUIStyle title, text, small, centered, button, number;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private BattleSession observedBattle;
        private CombatantState observedActor;
        private AbilityDefinition selected;
        private ConsumableDefinition selectedItem;
        private bool itemMode;
        private AbilityMenuGroup menuGroup;
        private List<BattleMenuCommand> rootCommands = new List<BattleMenuCommand>();
        private List<AbilityDefinition> groupAbilities = new List<AbilityDefinition>();
        private int rootIndex;
        private int commandIndex, targetIndex;
        private Vector2 actionsScroll, targetsScroll;
        private readonly Dictionary<CombatantState, int> seenChanges = new Dictionary<CombatantState, int>();
        private readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
        private sealed class FloatingNumber
        {
            public CombatantState Target;
            public CombatVitalChange Change;
            public float Started;
            public int Lane;
        }

        public void Initialize(PrototypeBattleController battleController, PixelSpriteLibrary sprites = null)
        { controller = battleController; pixelSprites = sprites; }

        private void ObserveBattle()
        {
            BattleSession battle = controller.Session;
            if (observedBattle != battle)
            {
                observedBattle = battle; observedActor = null; seenChanges.Clear(); numbers.Clear();
                // Optional map-specific art; the menu and fighter anchors do not depend on the background.
                background = Resources.Load<Texture2D>("BattleBackgrounds/" + controller.BattleLocationId);
            }
            if (observedActor != battle.CurrentActor)
            {
                observedActor = battle.CurrentActor; selected = null; selectedItem = null;
                itemMode = false; menuGroup = AbilityMenuGroup.Direct; rootIndex = commandIndex = targetIndex = 0;
                rootCommands = observedActor == null ? new List<BattleMenuCommand>() : BattleCommandMenu.Root(observedActor);
                groupAbilities.Clear(); actionsScroll = targetsScroll = Vector2.zero;
            }
            foreach (CombatantState participant in battle.Combatants)
            {
                seenChanges.TryGetValue(participant, out int seen);
                foreach (CombatVitalChange change in participant.VitalChanges)
                {
                    if (change.Sequence <= seen) continue;
                    int lane = 0;
                    foreach (FloatingNumber existing in numbers) if (existing.Target == participant) lane++;
                    numbers.Add(new FloatingNumber { Target = participant, Change = change,
                        Started = controller.IsPresentingAction ? controller.ActionImpactAt : Time.unscaledTime, Lane = lane % 3 });
                    seenChanges[participant] = change.Sequence;
                }
            }
            numbers.RemoveAll(n => Time.unscaledTime - n.Started > 1.4f);
        }

        private void Update()
        {
            if (controller == null || !controller.IsActive) { numbers.Clear(); return; }
            ObserveBattle();
            if (controller.IsPresentingAction) return;
            Keyboard keys = Keyboard.current;
            if (keys == null || !Application.isFocused) return;
            if (observedBattle.Outcome != BattleOutcome.None)
            {
                if (keys.enterKey.wasPressedThisFrame) controller.ReturnToCity();
                return;
            }
            if (observedActor == null || observedActor.Team != CombatTeam.Party) return;
            if (keys.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            bool targeting = selected != null || selectedItem != null;
            int count = targeting ? TargetRowCount() : CommandCount();
            int delta = keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame ? 1
                : keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame ? -1 : 0;
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
            if (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame)
            {
                if (targeting && count > 0) ExecuteTarget(Targets()[targetIndex]);
                else if (!targeting) ChooseCommand(commandIndex);
            }
        }

        private bool InSubmenu => itemMode || menuGroup != AbilityMenuGroup.Direct;
        private int CommandCount() => itemMode ? observedBattle.Inventory.Items.Count + 1
            : InSubmenu ? groupAbilities.Count + 1 : rootCommands.Count;
        private AbilityDefinition CommandAbility(int index) => InSubmenu
            ? index < groupAbilities.Count ? groupAbilities[index] : null : rootCommands[index].Ability;
        private string CommandLabel(int index)
        {
            if (InSubmenu && index == CommandCount() - 1) return "Voltar";
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
            if (InSubmenu && index == CommandCount() - 1) return true;
            if (itemMode) return observedBattle.Inventory.Items[index].Quantity > 0;
            AbilityDefinition ability = CommandAbility(index);
            return ability == null || observedActor.Mp >= ability.MpCost && (!ability.UsesLimit || observedActor.Limit.IsReady);
        }
        private IReadOnlyList<CombatantState> Targets() => selectedItem != null ? observedBattle.GetValidItemTargets(selectedItem)
            : observedBattle.GetValidTargets(selected);
        private int TargetRowCount() => selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies)
            ? Mathf.Min(1, Targets().Count) : Targets().Count;
        private void Cancel()
        {
            if (selected != null || selectedItem != null) { selected = null; selectedItem = null; targetIndex = 0; }
            else
            {
                itemMode = false; menuGroup = AbilityMenuGroup.Direct; groupAbilities.Clear();
                commandIndex = rootIndex;
                actionsScroll = new Vector2(0, BattleCommandMenu.KeepRowVisible(0, commandIndex, CommandCount(), 120));
            }
        }
        private void ChooseCommand(int index)
        {
            if (index < 0 || index >= CommandCount()) return;
            commandIndex = index;
            if (InSubmenu && index == CommandCount() - 1) { Cancel(); return; }
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
                if (ability.Targets == TargetRule.Self) controller.TryPlayerAction(ability, observedActor);
                else selected = ability;
            }
            targetIndex = 0; targetsScroll = Vector2.zero;
        }
        private void ExecuteTarget(CombatantState target)
        {
            bool accepted = selectedItem != null ? controller.TryPlayerItem(selectedItem, target)
                : controller.TryPlayerAction(selected, target);
            if (accepted) { selected = null; selectedItem = null; }
        }

        private void OnGUI()
        {
            if (controller == null || !controller.IsActive) return;
            EnsureStyles(); ObserveBattle();
            RectBox(new Rect(0, 0, Screen.width, Screen.height), new Color(.015f, .02f, .045f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            DrawArena();
            Panel(new Rect(20, 426, 1240, 60));
            GUI.Label(new Rect(40, 438, 1200, 40), observedBattle.LastMessage, text);
            // Equal halves: all commands/target choices stay on the left, party status stays on the right.
            Panel(new Rect(20, 496, 620, 204)); Panel(new Rect(640, 496, 620, 204));
            DrawPartyStatus();
            if (controller.IsPresentingAction)
            {
                GUI.Label(new Rect(40, 512, 570, 30), controller.PresentingActor.Definition.Name, title);
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
                AbilityDefinition intention = EnemyAI.ChooseAbility(observedBattle);
                GUI.Label(new Rect(40, 556, 570, 55), "Prepara " + (intention == null ? "sua acao" : intention.Name) + "...", text);
            }
            else if (observedActor != null) DrawActions();
            if (!string.IsNullOrEmpty(controller.ErrorMessage)) GUI.Label(new Rect(40, 668, 560, 25), controller.ErrorMessage, small);
            GUI.matrix = previous;
        }

        private void DrawArena()
        {
            Rect arena = new Rect(20, 20, 1240, 396);
            Color sky = new Color(.10f, .17f, .20f), ground = new Color(.18f, .23f, .18f);
            switch (controller.BattleLocationId)
            {
                case PrototypeTravel.Volcano: sky = new Color(.20f, .08f, .06f); ground = new Color(.28f, .12f, .08f); break;
                case PrototypeTravel.Castle: sky = new Color(.10f, .09f, .17f); ground = new Color(.20f, .19f, .25f); break;
                case PrototypeTravel.EastForest: case PrototypeTravel.WestForest: sky = new Color(.06f, .15f, .12f); ground = new Color(.13f, .23f, .14f); break;
                case PrototypeTravel.Ruins: sky = new Color(.17f, .16f, .13f); ground = new Color(.25f, .23f, .18f); break;
            }
            RectBox(arena, sky); RectBox(new Rect(20, 240, 1240, 176), ground);
            if (background != null) GUI.DrawTexture(arena, background, ScaleMode.ScaleAndCrop);
            IReadOnlyList<CombatantState> targets = Targets();
            foreach (CombatantState participant in observedBattle.Combatants)
            {
                Rect figure = FigureRect(participant);
                float castStep = controller.IsPresentingAction && controller.PresentingActor == participant
                    && HeroCastAnimation.IsCasting(controller.PresentingAbility)
                    ? HeroCastAnimation.Step(Time.unscaledTime - controller.ActionStartedAt, controller.ActionDuration) : 0;
                RectBox(new Rect(figure.x + castStep - 8, figure.yMax - 8, figure.width + 16, 10), new Color(0, 0, 0, .25f));
                DrawFighter(figure, participant);
                if (participant == (controller.IsPresentingAction ? controller.PresentingActor : observedActor))
                    DrawTurnArrow(figure.center.x + castStep, figure.y - 30 + Mathf.Sin(Time.unscaledTime * 5) * 3);
                bool group = selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != participant) continue;
                    if (group || i == targetIndex) DrawHand(new Rect(figure.x - 60, figure.center.y - 18, 56, 36));
                    if (GUI.Button(figure, GUIContent.none, GUIStyle.none)) { targetIndex = i; ExecuteTarget(participant); }
                }
                if (participant.IsCharging) GUI.Label(new Rect(figure.center.x - 140, figure.y - 53, 280, 24), "PREPARANDO ATAQUE!", centered);
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
            }
            if (controller.IsPresentingAction && controller.IsPresentingSpell)
                foreach (CombatantState target in controller.PresentingTargets)
                    spellAnimation.Draw(FigureRect(target), controller.PresentingAbility,
                        Time.unscaledTime - controller.ActionStartedAt);
            foreach (FloatingNumber floating in numbers)
            {
                Rect figure = FigureRect(floating.Target);
                float elapsed = Time.unscaledTime - floating.Started;
                if (elapsed < 0) continue;
                Color color = floating.Change.Amount > 0 ? new Color(.30f, 1f, .40f)
                    : floating.Change.IsCritical ? new Color(1f, .88f, .10f) : Color.white;
                color.a = Mathf.Clamp01((1.4f - elapsed) / .35f);
                string label = (floating.Change.Amount > 0 ? "+" : "") + System.Math.Abs(floating.Change.Amount)
                    + (floating.Change.IsMp ? " MP" : "");
                Rect rect = new Rect(figure.center.x - 90 + floating.Lane * 16, figure.y - 10 - elapsed * 40 - floating.Lane * 22, 180, 45);
                number.normal.textColor = new Color(0, 0, 0, color.a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, number);
                number.normal.textColor = color; GUI.Label(rect, label, number);
            }
            if (controller.IsCastleEncounter && observedBattle.Outcome == BattleOutcome.None)
                GUI.Label(new Rect(40, 379, 1180, 30), controller.CastleRules.Warning(observedBattle), small);
        }

        private Rect FigureRect(CombatantState participant)
        {
            int index = 0;
            foreach (CombatantState other in observedBattle.Combatants)
            { if (other == participant) break; if (other.Team == participant.Team) index++; }
            if (participant.Team == CombatTeam.Party) return new Rect(925 + (index % 2) * 60, 103 + index * 83, 64, 90);
            if (participant.Id == DragonProgress.EnemyId) return new Rect(160, 110, 350, 250);
            if (participant.Id == CastleBattleRules.SpiritId) return new Rect(365, 104, 170, 175);
            return new Rect(260 + index * 145, 155 + index * 72, 80, 110);
        }

        private void DrawActions()
        {
            bool targeting = selected != null || selectedItem != null;
            GUI.Label(new Rect(40, 506, 555, 30), observedActor.Definition.Name + " / " +
                (itemMode ? "Itens" : menuGroup == AbilityMenuGroup.Magic ? "Magic" : menuGroup == AbilityMenuGroup.Summon ? "Summon" : "Comandos"), title);
            actionsScroll = GUI.BeginScrollView(new Rect(38, 542, 280, 120), actionsScroll, new Rect(0, 0, 253, CommandCount() * 36));
            for (int i = 0; i < CommandCount(); i++)
            {
                if (CommandButton(new Rect(0, i * 36, 253, 34), CommandLabel(i), !targeting && i == commandIndex,
                    CommandEnabled(i) && !targeting)) { ChooseCommand(i); break; }
            }
            GUI.EndScrollView();
            if (!targeting) return;
            IReadOnlyList<CombatantState> targets = Targets();
            targetIndex = Mathf.Clamp(targetIndex, 0, Mathf.Max(0, targets.Count - 1));
            GUI.Label(new Rect(337, 510, 278, 30), "Escolha o alvo", text);
            bool group = selected != null && (selected.Targets == TargetRule.AllEnemies || selected.Targets == TargetRule.AllAllies);
            targetsScroll = GUI.BeginScrollView(new Rect(333, 542, 280, 88), targetsScroll, new Rect(0, 0, 253, (group ? 1 : targets.Count) * 36));
            for (int i = 0; i < (group ? Mathf.Min(1, targets.Count) : targets.Count); i++)
                if (CommandButton(new Rect(0, i * 36, 253, 34), group ? "Todos os alvos (" + targets.Count + ")" : targets[i].Definition.Name,
                    group || targetIndex == i, true)) { targetIndex = i; ExecuteTarget(targets[i]); break; }
            GUI.EndScrollView();
            if (targets.Count == 0) GUI.Label(new Rect(337, 546, 270, 78), "Nenhum alvo valido para esta acao.", small);
            if (CommandButton(new Rect(337, 632, 260, 32), "Voltar", false, true)) Cancel();
        }

        private void DrawPartyStatus()
        {
            GUI.Label(new Rect(665, 508, 190, 28), "PERSONAGEM", small);
            GUI.Label(new Rect(850, 508, 170, 28), "HP", centered);
            GUI.Label(new Rect(1030, 508, 120, 28), "MP", centered);
            GUI.Label(new Rect(1155, 508, 80, 28), "LIMIT", centered);
            int i = 0;
            foreach (CombatantState participant in observedBattle.Combatants)
            {
                if (participant.Team != CombatTeam.Party) continue;
                float y = 544 + i++ * 48;
                if (participant == observedActor) RectBox(new Rect(650, y - 2, 600, 45), new Color(.16f, .29f, .65f));
                GUI.Label(new Rect(665, y, 180, 25), participant.Definition.Name, text);
                string condition = !participant.IsAlive ? "DERROTADO" : participant.IsDefending ? "DEFESA" : participant.PoisonTurns > 0 ? "VENENO" : participant.BoundActions > 0 ? "CORRENTES" : participant.ProtectedBy != null ? "PROTEGIDO" : "";
                GUI.Label(new Rect(665, y + 23, 180, 18), condition, small);
                DrawBar(new Rect(850, y + 4, 170, 25), participant.Hp, participant.Definition.MaxHp, new Color(.18f, .60f, .47f));
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
        private bool CommandButton(Rect rect, string label, bool focused, bool enabled)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, label, button); GUI.enabled = previous;
            if (focused) DrawHand(new Rect(rect.x + 2, rect.center.y - 15, 44, 30));
            return clicked;
        }
        private void DrawTurnArrow(float x, float y)
        {
            RectBox(new Rect(x - 7, y, 14, 12), Color.white);
            for (int row = 0; row < 7; row++) RectBox(new Rect(x - 13 + row * 2, y + 12 + row * 2, 26 - row * 4, 2), Color.white);
        }
        private void DrawFighter(Rect rect, CombatantState participant)
        {
            if (participant.Team == CombatTeam.Party && battleSprites != null)
            {
                bool attacking = controller.IsPresentingAction && controller.PresentingActor == participant;
                if (attacking && HeroCastAnimation.IsCasting(controller.PresentingAbility)
                    && heroCastAnimation.Draw(rect, participant.Id, controller.PresentingAbility,
                        Time.unscaledTime - controller.ActionStartedAt, controller.ActionDuration,
                        participant.IsAlive ? Color.white : new Color(.4f, .4f, .4f))) return;
                bool casting = attacking && HeroCastAnimation.IsCasting(controller.PresentingAbility);
                int pose = attacking && !casting ? controller.AttackPose : 0;
                if (battleSprites.Draw(rect, participant.Id, pose, participant.IsAlive ? Color.white : new Color(.4f, .4f, .4f),
                    attacking && !casting ? controller.PresentingAbility : null)) return;
            }
            if (pixelSprites != null && participant.Id == DragonProgress.EnemyId)
            {
                bool attacking = controller.IsPresentingAction && controller.IsPresentingDragonAttack;
                string abilityId = controller.PresentingAbility?.Id;
                string resource = attacking ? DragonAttackAnimation.Resource(abilityId) : "Dragon";
                int frame = attacking ? DragonAttackAnimation.Frame(abilityId, Time.unscaledTime - controller.ActionStartedAt) : 0;
                pixelSprites.Atlas(resource).Draw(rect, frame,
                    participant.IsAlive ? Color.white : new Color(.4f, .4f, .4f)); return;
            }
            if (pixelSprites != null && participant.Team == CombatTeam.Party)
            {
                pixelSprites.Party(participant.Id).Draw(rect, (int)PixelDirection.Left,
                    participant.IsAlive ? Color.white : new Color(0.4f, 0.4f, 0.4f)); return;
            }
            if (pixelSprites != null && participant.Team == CombatTeam.Enemy && participant.Id != DragonProgress.EnemyId && participant.Id != CastleBattleRules.SpiritId)
            {
                pixelSprites.Atlas("NpcRoster").Draw(rect, participant.Id == CastleBattleRules.TyrantId ? 11 : 10,
                    participant.IsAlive ? Color.white : new Color(0.4f, 0.4f, 0.4f)); return;
            }
            Texture2D portrait=participant.Id==DragonProgress.EnemyId?dragonPortrait:participant.Id==CastleBattleRules.SpiritId?spiritPortrait:participant.Id==CastleBattleRules.TyrantId?kingPortrait:null;
            if(portrait!=null)
            {
                Color previous=GUI.color;GUI.color=participant.IsAlive?Color.white:new Color(0.4f,0.4f,0.4f);
                GUI.DrawTexture(rect,portrait,ScaleMode.ScaleToFit);GUI.color=previous;return;
            }
            Color clothes = participant.Team == CombatTeam.Enemy ? new Color(0.69f, 0.29f, 0.27f)
                : participant.Id == "lia" ? new Color(0.8f, 0.86f, 0.74f)
                : participant.Id == "bram" ? new Color(0.37f, 0.58f, 0.7f) : new Color(0.85f, 0.68f, 0.28f);
            if (!participant.IsAlive) clothes = new Color(0.3f, 0.32f, 0.34f);
            RectBox(new Rect(rect.x, rect.y + rect.height * 0.30f, rect.width, rect.height * 0.45f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.25f, rect.y, rect.width * 0.5f, rect.height * 0.28f),
                participant.IsAlive ? new Color(0.9f, 0.77f, 0.59f) : new Color(0.39f, 0.4f, 0.41f));
            RectBox(new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.58f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
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
            battleSprites = new PrototypeBattleSprites();
            dragonPortrait = Resources.Load<Texture2D>("DragonPortrait");
            spiritPortrait = Resources.Load<Texture2D>("SpiritPortrait"); kingPortrait = Resources.Load<Texture2D>("KingPortrait");
            title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            centered = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
            number = new GUIStyle(centered) { fontSize = 30, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(50, 4, 2, 2) };
            foreach (GUIStyle style in new[] { title, text, small, centered, button })
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
        { if (pixel != null) Destroy(pixel); }
    }
}
