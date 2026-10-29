using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

// Runs in the isolated project created by Tools/Test-AutomaticEndings.ps1.
public static class AutomaticEndingUnityChecks
{
    private static int checks;
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Call(object target, string method)
    { target.GetType().GetMethod(method, Hidden).Invoke(target, null); }
    private static void Age(PrototypeBattleController controller, float elapsed)
    { typeof(PrototypeBattleController).GetField("<ActionStartedAt>k__BackingField", Hidden).SetValue(controller, Time.unscaledTime - elapsed); }
    private static void FinishEntry(PrototypeBattleController controller)
    {
        controller.EntryTransition.enabled = false;
        Call(controller.EntryTransition, "OnDisable");
        Call(controller, "Update");
    }
    private static void Damage(CombatantState target, int amount)
    {
        var method = typeof(CombatantState).GetMethod("TakeDamage", Hidden);
        object[] args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (object)amount).ToArray();
        method.Invoke(target, args);
    }
    private sealed class Fixture : IDisposable
    {
        private readonly GameObject root = new GameObject("Automatic ending checks");
        public readonly PrototypeBattleController Controller;
        public readonly PlayerMovement Movement;
        public int Endings, Completions;
        public GameEnding Ending;
        public Fixture(bool armor = false)
        {
            Movement = root.AddComponent<PlayerMovement>(); Call(Movement, "Awake");
            var party = PrototypePartyFactory.Create();
            if (armor)
            {
                var equipment = new EquipmentLoadout(DragonProgress.Armor);
                Check(party.TryAddEquipments(equipment) && party.TryEquipEquipment(party.Members[2], equipment, out _), "Equip the draconic armor.");
            }
            Controller = root.AddComponent<PrototypeBattleController>();
            Controller.Initialize(Movement, root.AddComponent<DialogueController>(), new ExplorationProgress(),
                restoredParty: party, endingPresentation: ending => { Ending = ending; Endings++; });
            Controller.EncounterCompleted += outcome => Completions++;
        }
        public void Dispose() { UnityEngine.Object.DestroyImmediate(root); }
        public void CheckFinished(GameEnding expected)
        {
            Check(Endings == 1 && Ending == expected && Controller.HasEnded && !Controller.IsActive,
                "The correct cinematic starts automatically: " + expected);
            Check(!Controller.IsPostBattleOpen, "The ending cannot wait in the experience screen.");
            Check(!(bool)typeof(PlayerMovement).GetField("movementEnabled", Hidden).GetValue(Movement), "Exploration remains blocked during the ending.");
            Check(Completions == 1, "The battle completion is recorded once.");
            Call(Controller, "Update"); Controller.ReturnToCity(); Call(Controller, "Update");
            Check(Endings == 1 && Completions == 1, "Further updates cannot replay the transition or completion.");
        }
    }
    private static void CheckMeteorPhaseEntry()
    {
        using (var fixture = new Fixture())
        {
            var controller = fixture.Controller;
            controller.Party.SetInfiniteLimit(true);
            controller.StartCastleBattle(); FinishEntry(controller);
            var king = controller.Session.Combatants.Single(c => c.Id == CastleBattleRules.TyrantId);
            Damage(king, king.Hp - (CastleBattleRules.MeteorHpThreshold + 50));
            var limit = controller.Session.CurrentActor.Definition.Abilities.Single(a => a.UsesLimit);
            Check(controller.TryPlayerAction(limit, king), "Reproduce Omnislash at 500 HP through the actual controller.");
            Check(king.IsAlive && king.Hp == CastleBattleRules.MeteorHpThreshold && controller.CastleRules.MeteorStarted
                && controller.Session.Outcome == BattleOutcome.None && fixture.Completions == 0,
                "The threshold hit enters the meteor phase instead of completing the possession ending.");
            Age(controller, controller.ActionEndsAt - controller.ActionStartedAt + 1); Call(controller, "Update");
            Check(fixture.Endings == 0 && controller.IsActive && !controller.HasEnded,
                "Finishing the LIMIT animation cannot hide the king or start a cinematic.");
            BeforeKing(controller.Session);
            Check(EnemyAI.ChooseAbility(controller.Session) == CastleBattleRules.Protect
                && EnemyAI.TryTakeTurn(controller.Session, out _) && controller.CastleRules.ProtectCast,
                "The living king reaches Protect on his own turn.");
        }
    }
    private static void CheckFinalBlow(GameEnding expected, bool armor, bool both = false, bool longFrame = false)
    {
        using (var fixture = new Fixture(armor))
        {
            var controller = fixture.Controller;
            if (both)
            {
                var cleave = new MateriaInstance("automatic-ending-cleave", PrototypePartyFactory.CleaveMateria);
                Check(controller.Party.TryAddMaterias(cleave) && controller.Party.TryEquip(controller.Party.Members[0],
                    controller.Party.Members[0].Armor, 2, cleave, out _), "Equip an area attack to test simultaneous boss deaths.");
            }
            controller.StartCastleBattle(); FinishEntry(controller);
            Check(fixture.Endings == 0 && controller.IsActive, "An unfinished castle battle cannot start a cinematic.");
            if (expected == GameEnding.HeroPossessed)
            {
                var king = controller.Session.Combatants.Single(c => c.Id == CastleBattleRules.TyrantId);
                Damage(king, king.Hp - CastleBattleRules.MeteorHpThreshold);
                controller.CastleRules.AfterAction(controller.Session);
                BeforeKing(controller.Session);
                Check(EnemyAI.TryTakeTurn(controller.Session, out _), "Start Protect before testing a later fatal blow.");
                Call(king, "ClearConditions");
            }
            var target = controller.Session.Combatants.Single(c => c.Id == (expected == GameEnding.KingFreed ? CastleBattleRules.SpiritId : CastleBattleRules.TyrantId));
            Damage(target, target.Hp - 1);
            if (both)
            {
                var spirit = controller.Session.Combatants.Single(c => c.Id == CastleBattleRules.SpiritId);
                Damage(spirit, spirit.Hp - 1);
            }
            int experience = controller.Party.Members.Sum(m => m.Experience);
            var attack = controller.Session.CurrentActor.Definition.Abilities.Single(a => both ? a.SourceAbilityId == "cleave" : a.Id == "attack");
            Check(controller.TryPlayerAction(attack, target), "The final blow is accepted.");
            Check(controller.Session.Outcome == BattleOutcome.Completed && controller.CastleRules.Ending == expected, "The specific final condition is met.");
            Check(fixture.Endings == 0 && controller.IsPresentingAction, "The cinematic waits for the final action animation.");
            if (!longFrame)
            {
                float duration = controller.ActionEndsAt - controller.ActionStartedAt;
                Age(controller, duration - .02f); Call(controller, "Update");
                Check(fixture.Endings == 0 && !controller.IsPostBattleOpen, "The final hit and enemy disappearance finish before the cinematic.");
            }
            Age(controller, controller.ActionEndsAt - controller.ActionStartedAt + 1); Call(controller, "Update");
            fixture.CheckFinished(expected);
            int awarded = controller.Party.Members.Sum(m => m.Experience);
            Check(expected == GameEnding.KingFreed ? awarded > experience : awarded == experience, "The original ending reward policy is preserved.");
            Call(controller, "Update");
            Check(controller.Party.Members.Sum(m => m.Experience) == awarded, "Automatic updates do not duplicate experience.");
        }
    }
    private static void BeforeKing(BattleSession battle)
    {
        for (int turn = 0; battle.CurrentActor?.Id != CastleBattleRules.TyrantId && turn < 20; turn++)
        {
            var actor = battle.CurrentActor;
            Check(actor != null && (actor.Team == CombatTeam.Enemy ? EnemyAI.TryTakeTurn(battle, 0, out _)
                : battle.TryAct(actor, actor.Definition.Abilities.Single(a => a.Effect == AbilityEffect.Defend), actor, out _)),
                "Advance a turn during the meteor countdown.");
        }
        Check(battle.CurrentActor?.Id == CastleBattleRules.TyrantId, "The tyrant reaches his next turn.");
    }
    private static void CheckMeteor(bool longFrame)
    {
        using (var fixture = new Fixture())
        {
            var controller = fixture.Controller;
            controller.StartCastleBattle(); FinishEntry(controller);
            var battle = controller.Session;
            var king = battle.Combatants.Single(c => c.Id == CastleBattleRules.TyrantId);
            Damage(king, king.Hp - 120); controller.CastleRules.AfterAction(battle);
            for (int turn = 0; turn < CastleBattleRules.MeteorCastTurns; turn++)
            { BeforeKing(battle); Check(EnemyAI.TryTakeTurn(battle, out _), "Complete a countdown turn."); }
            BeforeKing(battle); Call(controller, "Update");
            typeof(PrototypeBattleController).GetField("enemyActionTime", Hidden).SetValue(controller, Time.unscaledTime - 1);
            Call(controller, "Update");
            Check(controller.IsDestructiveMeteor && fixture.Endings == 0, "The meteor animation starts before the cinematic.");
            if (!longFrame)
            {
                Age(controller, controller.ActionImpactAt - controller.ActionStartedAt + .01f); Call(controller, "Update");
                Check(battle.Combatants.All(c => !c.IsAlive) && controller.MeteorWhiteOpacity == 1 && fixture.Endings == 0,
                    "The destructive impact and white hold precede the cinematic.");
            }
            Age(controller, CastleMeteorPresentation.CinematicCutAt + .01f); Call(controller, "Update");
            fixture.CheckFinished(GameEnding.CastleDestroyed);
        }
    }
    private static void CheckOrdinaryVictory()
    {
        using (var fixture = new Fixture())
        {
            var controller = fixture.Controller;
            controller.StartBattle(); FinishEntry(controller);
            var enemies = controller.Session.Combatants.Where(c => c.Team == CombatTeam.Enemy).ToArray();
            foreach (var enemy in enemies.Skip(1)) Damage(enemy, enemy.Hp);
            Damage(enemies[0], enemies[0].Hp - 1);
            Check(controller.TryPlayerAction(controller.Session.CurrentActor.Definition.Abilities.Single(a => a.Id == "attack"), enemies[0]), "Finish an ordinary encounter.");
            Age(controller, controller.ActionEndsAt - controller.ActionStartedAt + 1); Call(controller, "Update");
            Check(controller.Session.Outcome == BattleOutcome.Victory && controller.IsActive && controller.IsPostBattleOpen
                && !controller.HasEnded && fixture.Endings == 0, "Ordinary victories retain their experience screen and do not trigger an ending.");
        }
    }
    public static void Run()
    {
        try
        {
            typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Func<bool>)(() => false));
            CheckMeteorPhaseEntry();
            CheckFinalBlow(GameEnding.KingFreed, true);
            CheckFinalBlow(GameEnding.KingFreed, true, longFrame: true);
            CheckFinalBlow(GameEnding.HeroPossessed, false);
            CheckFinalBlow(GameEnding.HeroPossessed, true, both: true, longFrame: true);
            CheckMeteor(false); CheckMeteor(true); CheckOrdinaryVictory();
            Debug.Log("AUTOMATIC_ENDINGS_UNITY_OK: " + checks + " checks; all endings without player input, action timing, single completion, rewards and ordinary victory.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
