using DragonQuest.Infrastructure;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Presentation.Battle.Sprites;
using DragonQuest.Presentation.Battle.Animations;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Content;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.Battle
{
    public sealed class BattleArenaPresentation
    {
        private readonly BattleHudGraphics graphics;
        private readonly BattleMenuPresenter commands;
        private BattleController controller;
        private PixelSpriteLibrary pixelSprites;
        private readonly DragonBattleSprites dragonSprites = new DragonBattleSprites();
        private BattleSprites battleSprites;
        private readonly SpellAnimation spellAnimation = new SpellAnimation();
        private readonly HeroCastAnimation heroCastAnimation = new HeroCastAnimation();
        private readonly HeroKnockoutSprites knockoutSprites = new HeroKnockoutSprites();
        private readonly HeroKnockoutPresentation heroKnockouts = new HeroKnockoutPresentation();
        private readonly UltimateAnimation ultimateAnimation = new UltimateAnimation();
        private readonly SummonAnimation summonAnimation = new SummonAnimation();
        private readonly SpiritAttackAnimation spiritAttacks = new SpiritAttackAnimation();
        private readonly ChainPrisonAnimation chainPrisons = new ChainPrisonAnimation();
        private readonly CastleSpellAnimation castleSpells = new CastleSpellAnimation();
        private readonly CastleCastCounter castCounter = new CastleCastCounter();
        private Texture2D dragonPortrait, spiritPortrait, kingPortrait, background;
        private BattleSession observedBattle;
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
            public int DisplayAmount;
            public float Started;
            public int Lane;
        }

        private float spiritFloatStartedAt;

        public BattleArenaPresentation(BattleHudGraphics graphics, BattleMenuPresenter commands)
        { this.graphics = graphics; this.commands = commands; }

        public void Initialize(BattleController battleController, PixelSpriteLibrary sprites)
        { controller = battleController; pixelSprites = sprites; }

        public void Prepare()
        {
            if (battleSprites != null) return;
            battleSprites = new BattleSprites();
            dragonPortrait = GameAssets.Optional<Texture2D>("DragonPortrait");
            spiritPortrait = GameAssets.Optional<Texture2D>("PixelArt/SpiritBattle"); kingPortrait = GameAssets.Optional<Texture2D>("KingPortrait");
        }

        public void ClearNumbers() => numbers.Clear();

        public void Draw()
        {
            Matrix4x4 previous = GUI.matrix;
            try
            {
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
            }
            finally { GUI.matrix = previous; }
        }

        public void ObserveBattle()
        {
            BattleSession battle = controller.Session;
            if (observedBattle != battle)
            {
                observedBattle = battle; seenChanges.Clear(); numbers.Clear();
                defeatedEnemies.Reset(); castCounter.Reset(); enemyPoses.Clear();
                heroKnockouts.Reset();
                ultimateAnimation.Prepare();
                spiritFloatStartedAt = Time.unscaledTime;
                // Optional map-specific art; the menu and fighter anchors do not depend on the background.
                background = GameAssets.Optional<Texture2D>("BattleBackgrounds/" + controller.BattleLocationId);
                if (controller.IsCastleEncounter) { castleSpells.Prepare(); spiritAttacks.Prepare(); }
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
                        DisplayAmount = CastleMeteorPresentation.DisplayedAmount(change,
                            controller.IsPresentingAction
                            && CastleMeteorPresentation.IsMeteor(controller.PresentingActor.Id, controller.PresentingAbility)
                            && controller.CastleRules?.Ending == GameEnding.CastleDestroyed),
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
                case WorldNavigation.Volcano: sky = new Color(.20f, .08f, .06f); ground = new Color(.28f, .12f, .08f); break;
                case WorldNavigation.Castle: sky = new Color(.10f, .09f, .17f); ground = new Color(.20f, .19f, .25f); break;
                case WorldNavigation.EastForest: sky = new Color(.06f, .15f, .12f); ground = new Color(.13f, .23f, .14f); break;
                case WorldNavigation.WestForest: sky = new Color(.25f, .24f, .21f); ground = new Color(.34f, .32f, .27f); break;
                case WorldNavigation.Ruins: sky = new Color(.17f, .16f, .13f); ground = new Color(.25f, .23f, .18f); break;
            }
            graphics.RectBox(arena, sky); graphics.RectBox(new Rect(20, 240, 1240, 176), ground);
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
            IReadOnlyList<CombatantState> targets = controller.IsEscaping ? new CombatantState[0] : commands.Targets();
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
                    graphics.RectBox(new Rect(shadowFigure.x + castStep - 8, groundFigure.yMax - 8, figure.width + 16, 10),
                        new Color(0, 0, 0, .25f * defeatedEnemies.Alpha(participant, Time.unscaledTime)));
                DrawFighter(figure, participant);
                chainPrisons.Draw(figure, controller.ChainsVisible(participant), Time.unscaledTime, controller.ChainBreakAge(participant));
                if (participant.IsAlive)
                    graphics.StatusIcons.DrawOverhead(figure, participant.MindControlTurns > 0
                        || controller.IsPresentingAction && controller.IsPresentingConfusedAction && controller.PresentingActor == participant,
                        participant.IsSilenced, Time.unscaledTime, participant.TauntTurns > 0);
                if (!controller.IsEscaping && !ultimate && participant.IsAlive && participant == (controller.IsPresentingAction ? controller.PresentingActor : commands.Actor))
                    graphics.DrawTurnArrow(figure.center.x + castStep, figure.y - 30 + Mathf.Sin(Time.unscaledTime * 5) * 3);
                bool group = commands.SelectedAbility != null && (commands.SelectedAbility.Targets == TargetRule.AllEnemies || commands.SelectedAbility.Targets == TargetRule.AllAllies);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != participant) continue;
                    if (group || i == commands.TargetIndex) graphics.DrawHand(new Rect(figure.x - 60, figure.center.y - 18, 56, 36));
                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = previousEnabled && commands.CanAcceptMenuInput;
                    bool clicked = GUI.Button(figure, GUIContent.none, GUIStyle.none);
                    GUI.enabled = previousEnabled;
                    if (clicked) { GameInput.ConsumeFrame(); commands.ChooseTarget(i, participant); }
                }
            }
            if (controller.IsPresentingAction && controller.IsPresentingSummon)
            {
                float elapsed = Time.unscaledTime - controller.ActionStartedAt;
                float impactDelay = SummonAnimation.ImpactDelayFor(controller.PresentingAbility);
                summonAnimation.Draw(controller.PresentingAbility, elapsed);
                AbilityDefinition impact = SummonAnimation.ImpactEffect(controller.PresentingAbility);
                if (elapsed >= impactDelay - .15f)
                    foreach (CombatantState target in controller.PresentingTargets)
                        spellAnimation.Draw(FigureRect(target), impact,
                            elapsed - impactDelay + SpellAnimation.ImpactDelayFor(impact));
                if (controller.PresentingAbility.PartyEffect == PartyEffect.RestoreAndRevive
                    && elapsed >= impactDelay - SpellAnimation.ImpactDelayFor(AbilityCatalog.Heal))
                    foreach (CombatantState ally in controller.PresentingPartyTargets)
                        spellAnimation.Draw(FigureRect(ally), AbilityCatalog.Heal,
                            elapsed - impactDelay + SpellAnimation.ImpactDelayFor(AbilityCatalog.Heal));
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
                    castleSpells.DrawMeteor(FigureRect(controller.PresentingActor), target,
                        rules.MeteorAbsorbed, Time.unscaledTime - controller.ActionStartedAt);
                }
            }
            if (ultimate)
                ultimateAnimation.DrawEffects(controller.PresentingAbility, ultimateElapsed, ultimateCaster, ultimateFocus,
                    ultimateTargets, ultimateAllies, ultimateRevived);
            DrawFloatingNumbers();
            if (controller.IsCastleEncounter && (controller.CastleRules.Ending == GameEnding.None || meteorAction)
                && castCounter.IsVisible(Time.unscaledTime))
                foreach (CombatantState member in observedBattle.Combatants)
                    if (member.Id == CastleBattleRules.TyrantId) { DrawCastNumber(FigureRect(member)); break; }
            GUI.EndGroup();
            GUI.matrix = arenaMatrix;
        }

        public void DrawFloatingNumbers(bool meteorOnly = false)
        {
            foreach (FloatingNumber floating in numbers)
            {
                if (meteorOnly && (floating.DisplayAmount != -CastleMeteorPresentation.MeteorDamageDisplay
                    || floating.Change.IsMp || floating.Change.IsDamageOverTime
                    || floating.Started < controller.ActionImpactAt - .01f)) continue;
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
                string label = (floating.DisplayAmount > 0 ? "+" : "") + System.Math.Abs(floating.DisplayAmount)
                    + (floating.Change.IsMp ? " MP" : "");
                Rect rect = new Rect(figure.center.x - 90 + floating.Lane * (swordHit ? 40 : 16),
                    figure.y - 10 - elapsed * (swordHit ? 95 : 40) - floating.Lane * 22, 180, 45);
                graphics.Number.normal.textColor = new Color(0, 0, 0, color.a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, graphics.Number);
                graphics.Number.normal.textColor = color; GUI.Label(rect, label, graphics.Number);
            }
        }

        private void DrawCastNumber(Rect figure)
        {
            float elapsed = Time.unscaledTime - castCounter.StartedAt;
            float alpha = Mathf.Clamp01((CastleMeteorPresentation.TurnNumberDuration - elapsed) / .35f);
            // Mesmo movimento, sombra e fade dos danos; acima deles para nao sobrepor os digitos.
            Rect rect = new Rect(figure.center.x - 90, figure.y - 56 - elapsed * 40, 180, 54);
            string label = castCounter.DisplayValue.ToString();
            graphics.CastNumber.normal.textColor = new Color(0, 0, 0, alpha);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, graphics.CastNumber);
            graphics.CastNumber.normal.textColor = new Color(1, 1, 1, alpha);
            GUI.Label(rect, label, graphics.CastNumber);
        }

        private Rect FigureRect(CombatantState participant, bool floating = true)
        {
            int index = 0;
            foreach (CombatantState other in observedBattle.Combatants)
            { if (other == participant) break; if (other.Team == participant.Team) index++; }
            float escapeOffset = controller.IsEscaping && participant.IsAlive
                ? 18 * Mathf.Clamp01((Time.unscaledTime - controller.EscapeStartedAt) / .2f) + 420 * controller.EscapeDeparture : 0;
            if (participant.Team == CombatTeam.Party)
            {
                Rect ground = new Rect(925 + (index % 2) * 60 + escapeOffset,
                    background != null ? 125 + index * 78 : 103 + index * 83, 64, 90);
                return floating && IsMeteorAbsorber(participant)
                    ? MeteorAbsorptionAnimation.BearerRect(ground, Time.unscaledTime - controller.ActionStartedAt) : ground;
            }
            if (participant.Id == DragonProgress.EnemyId) return new Rect(160, 110, 350, 250);
            if (participant.Id == CastleBattleRules.SpiritId)
            {
                float lift = floating ? 175 * SpiritFloatAnimation.HeightAt(Time.unscaledTime - spiritFloatStartedAt) : 0;
                return new Rect(215, 90 - lift, 170, 175);
            }
            if (participant.Id == CastleBattleRules.TyrantId) return new Rect(410, 169, 80, 110);
            return new Rect(260 + index * 145, 155 + index * 72, 80, 110);
        }

        private void DrawFighter(Rect rect, CombatantState participant)
        {
            Color tint = FighterTint(participant);
            bool acting = controller.IsPresentingAction && controller.PresentingActor == participant;
            if (heroKnockouts.IsKnockedOut(participant) && knockoutSprites.Draw(rect, participant.Id)) return;
            if (IsMeteorAbsorber(participant) && castleSpells.DrawAbsorber(rect, participant.Id,
                Time.unscaledTime - controller.ActionStartedAt, tint)) return;
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
                    if (target == participant && !IsMeteorAbsorber(participant)
                        && !controller.IsChainBreakTarget(participant) && sinceImpact >= 0 && sinceImpact < .22f
                        && (controller.PresentingAbility.Effect == AbilityEffect.PhysicalDamage
                            || controller.PresentingAbility.Effect == AbilityEffect.MagicDamage
                            || controller.PresentingAbility.Effect == AbilityEffect.Drain))
                    { rect.x += Mathf.Sin(sinceImpact * 75f) * (controller.IsPresentingUltimate ? 9 : 5); break; }
            }
            if (controller.IsEscaping && participant.Team == CombatTeam.Party && participant.IsAlive && !acting && pixelSprites != null)
            {
                float elapsed = Time.unscaledTime - controller.EscapeStartedAt;
                int frame = PixelSpriteLayout.WalkFrame(PixelDirection.Right,
                    elapsed * PixelSpriteLayout.WalkPoseDistance * 12, true);
                rect.y -= Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 12)) * 3;
                pixelSprites.Party(participant.Id).Draw(rect, frame, tint);
                return;
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
                if (meteorPresentation && !controller.CastleRules.MeteorAbsorbed
                    && castleSpells.DrawMeteorCaster(rect, Time.unscaledTime - controller.ActionStartedAt, tint)) return;
                if (meteorPresentation && controller.CastleRules.MeteorAbsorbed
                    && castleSpells.DrawAbsorbingCaster(rect, Time.unscaledTime - controller.ActionStartedAt, tint)) return;
                // Shield termina sobre a postura normal; so entao aparece o sprite de conjuracao.
                if (protectPresentation && DrawEnemySprite(rect, participant, 0, tint)) return;
                if (CastleMeteorPresentation.HoldCastPose(controller.CastleRules, meteorPresentation, protectPresentation)
                    && DrawEnemySprite(rect, participant, CastleMeteorPresentation.ChargePose(controller.CastleRules),
                        tint, CastleBattleRules.PrepareMeteor)) return;
            }
            // Every action has readable movement, even when it has no dedicated pose sheet.
            if (acting && !HeroCastAnimation.IsCasting(controller.PresentingAbility)
                && !BattleSprites.HasAnimation(participant.Id, controller.PresentingAbility)
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
            graphics.RectBox(new Rect(rect.x, rect.y + rect.height * 0.30f, rect.width, rect.height * 0.45f), clothes);
            graphics.RectBox(new Rect(rect.x + rect.width * 0.25f, rect.y, rect.width * 0.5f, rect.height * 0.28f),
                new Color(0.9f, 0.77f, 0.59f) * tint);
            graphics.RectBox(new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
            graphics.RectBox(new Rect(rect.x + rect.width * 0.58f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
        }

        private bool IsMeteorAbsorber(CombatantState participant)
            => controller.IsPresentingAction
                && CastleMeteorPresentation.IsMeteor(controller.PresentingActor.Id, controller.PresentingAbility)
                && controller.CastleRules.MeteorAbsorbed && participant.Id == controller.CastleRules.MeteorAbsorberId;


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

    }
}
