using DragonQuest.Presentation.Battle;
using DragonQuest.Gameplay.Battle;
using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using UnityEditor;
using UnityEngine;

// Runs in the disposable project created by Tools/Test-MeteorRelease.ps1.
public static class MeteorReleaseUnityChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Call(object target, string method)
    { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void Age(BattleController controller, float elapsed)
    {
        controller.ActionPresentation.GetType().GetField("<ActionStartedAt>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller.ActionPresentation, Time.unscaledTime - elapsed);
    }
    private static void BeforeKing(BattleSession battle)
    {
        int guard = 0;
        while (battle.CurrentActor.Id != CastleBattleRules.TyrantId && guard++ < 20)
        {
            var actor = battle.CurrentActor;
            Check(battle.TryAct(actor, actor.Definition.Abilities.Single(a => a.Effect == AbilityEffect.Defend), actor, out _),
                "Party has a complete turn between casting steps.");
        }
        Check(battle.CurrentActor.Id == CastleBattleRules.TyrantId, "King's next turn is reached.");
    }
    private static void CheckController(bool longFrame)
    {
        var fixture = new GameObject("Meteor release validation");
        try
        {
            var movement = fixture.AddComponent<PlayerMovement>(); Call(movement, "Awake");
            var dialogue = fixture.AddComponent<DialogueController>();
            var controller = fixture.AddComponent<BattleController>();
            int endings = 0, completions = 0;
            controller.Initialize(movement, dialogue, new ExplorationProgress(), endingPresentation: ending => {
                Check(ending == GameEnding.CastleDestroyed, "Automatic cut selects the meteor cinematic."); endings++;
            });
            controller.EncounterCompleted += outcome => {
                Check(outcome == BattleOutcome.Completed, "Completion is recorded once at the impact."); completions++;
            };
            controller.StartCastleBattle();
            var battle = controller.Session;
            var king = battle.Combatants.Single(c => c.Id == CastleBattleRules.TyrantId);
            var damage = typeof(CombatantState).GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] damageArgs = damage.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (object)480).ToArray();
            damage.Invoke(king, damageArgs);
            controller.CastleRules.AfterAction(battle);
            for (int turn = 0; turn < CastleBattleRules.MeteorCastTurns; turn++)
            {
                BeforeKing(battle);
                Check(EnemyAI.ChooseAbility(battle) != CastleBattleRules.Meteor && EnemyAI.TryTakeTurn(battle, out _),
                    "All five countdown turns are preparation.");
            }
            Check(controller.CastleRules.MeteorTurnsElapsed == 5 && controller.CastleRules.Ending == GameEnding.None,
                "The displayed one leaves the battle open.");
            BeforeKing(battle);
            int[] hp = battle.Combatants.Select(c => c.Hp).ToArray();
            var hud = fixture.AddComponent<BattleHud>(); hud.Initialize(controller);
            // Only observe combat events here; the isolated project omits unrelated Ultimate atlases.
            hud.Arena.GetType().GetField("observedBattle", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.Arena, battle);
            Call(hud, "ObserveBattle");
            Call(controller, "Update");
            typeof(BattleController).GetField("enemyActionTime", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, Time.unscaledTime - 1);
            Call(controller, "Update");
            Check(controller.IsPresentingAction && controller.IsDestructiveMeteor, "The sixth turn starts the charge animation.");
            Check(hp.SequenceEqual(battle.Combatants.Select(c => c.Hp)) && completions == 0
                && controller.MeteorWhiteOpacity == 0, "Starting the animation cannot damage or flash early.");
            if (!longFrame)
            {
                Age(controller, CastleMeteorPresentation.ThrowReleaseAt + .05f); Call(controller, "Update");
                Check(hp.SequenceEqual(battle.Combatants.Select(c => c.Hp)) && controller.MeteorWhiteOpacity == 0,
                    "Releasing the sphere cannot damage before it reaches the center.");
                Age(controller, CastleMeteorPresentation.ThrowImpactAt - .02f); Call(controller, "Update");
                Check(hp.SequenceEqual(battle.Combatants.Select(c => c.Hp)) && controller.MeteorWhiteOpacity == 0,
                    "HP and the screen remain intact throughout the growing fireball.");
                Age(controller, CastleMeteorPresentation.ThrowImpactAt + .01f); Call(controller, "Update");
                Check(battle.Combatants.All(c => !c.IsAlive) && controller.MeteorWhiteOpacity == 1,
                    "Damage and full-screen white begin together after the charge.");
                Check(completions == 1 && endings == 0 && controller.IsActive, "The white frame precedes the automatic cut.");
                Call(hud, "ObserveBattle");
                var displayed = ((System.Collections.IEnumerable)hud.Arena.GetType()
                    .GetField("numbers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud.Arena)).Cast<object>();
                Check(displayed.Count(value => (int)value.GetType().GetField("DisplayAmount").GetValue(value)
                    == -CastleMeteorPresentation.MeteorDamageDisplay) == battle.Combatants.Count,
                    "Every target has a visible 9999 number ready to draw over the white screen.");
            }
            Age(controller, CastleMeteorPresentation.CinematicCutAt + .01f); Call(controller, "Update");
            Check(battle.Combatants.All(c => !c.IsAlive) && completions == 1 && endings == 1
                && !controller.IsActive && controller.HasEnded,
                "The cinematic starts without confirmation, including across a long frame.");
            Call(controller, "Update");
            Check(endings == 1 && completions == 1, "The cut and damage cannot run twice.");
        }
        finally { UnityEngine.Object.DestroyImmediate(fixture); }
    }
    public static void Run()
    {
        try
        {
            typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, (Func<bool>)(() => true));
            MeteorAbsorptionValidation.Run();
            CheckController(false); CheckController(true);
            Debug.Log("METEOR_RELEASE_UNITY_OK: " + checks + " controller checks, damage/white synchronization, automatic cinematic and sprite previews.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
