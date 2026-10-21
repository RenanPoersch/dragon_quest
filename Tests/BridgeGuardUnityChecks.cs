using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class BridgeGuardUnityChecks
{
    private static int checks;
    private static object Call(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static void ResetBattle(PrototypeBattleController battle, DialogueController dialogue)
    {
        battle.GetType().GetField("<IsActive>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, false);
        battle.Party.RecoverAtInn(); dialogue.Close();
    }

    [MenuItem("Tools/Dragon Quest/Validate Bridge Guards %#F8")]
    public static void Run()
    {
        GameObject fixture = null;
        FieldInfo focus = typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic);
        object previousFocus = focus.GetValue(null);
        try
        {
            focus.SetValue(null, (Func<bool>)(() => true));
            fixture = new GameObject("Bridge guard checks");
            var player = fixture.AddComponent<PlayerMovement>(); Call(player, "Awake");
            var dialogue = fixture.AddComponent<DialogueController>();
            var progress = new ExplorationProgress(50);
            var battle = fixture.AddComponent<PrototypeBattleController>();
            battle.Initialize(player, dialogue, progress);
            battle.SetLocationProvider(() => PrototypeTravel.City);
            var patrol = new GameObject("Bridge patrol"); patrol.transform.SetParent(fixture.transform);
            patrol.transform.position = PrototypeCityLayout.Guard;
            var barrier = new GameObject("Bridge blockade"); barrier.transform.SetParent(patrol.transform);
            barrier.transform.position = PrototypeCityLayout.Bridge;
            var blocker = barrier.AddComponent<BoxCollider2D>(); blocker.size = new Vector2(3.7f, .7f);
            var guards = patrol.AddComponent<BattleInteraction>();
            guards.Initialize(player, dialogue, battle, () => progress);

            player.Teleport(PrototypeCityLayout.Spawn); Call(guards, "LateUpdate");
            Check(!dialogue.IsOpen && !battle.IsActive, "The patrol leaves the city entrance alone.");
            foreach (float y in new[] {5.4f, 7f, 7.6f, 8.4f, 9f, 10.3f, 11.4f})
            {
                player.Teleport(new Vector2(0, y)); Call(guards, "LateUpdate");
                Check(!dialogue.IsOpen && !battle.IsActive, "Old approach positions remain clear before the deeper bridge trigger: " + y);
            }
            player.Teleport(new Vector2(0, 11.5f)); Call(guards, "LateUpdate");
            Check(dialogue.IsOpen && !battle.IsActive, "The new warning starts well inside the bridge at y=11.5.");
            guards.RestoreState(); dialogue.Close();
            player.Teleport(new Vector2(2.2f, 11.5f));
            dialogue.Open("Another resident", "Busy"); Call(guards, "LateUpdate");
            Check(dialogue.Title == "Another resident", "The warning does not replace another conversation.");
            dialogue.Close(); Call(guards, "LateUpdate");
            Check(dialogue.IsOpen && dialogue.Title == "Guarda Real" && !dialogue.HasChoices && !battle.IsActive,
                "Approaching the side of the bridge automatically opens the mandatory warning before combat.");
            Check(!(bool)typeof(PlayerMovement).GetField("movementEnabled", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player), "The warning stops movement.");
            Call(guards, "LateUpdate"); Check(!battle.IsActive, "Combat waits for the warning to close.");
            dialogue.Close(); Call(guards, "LateUpdate");
            Check(battle.IsActive && battle.Session.Combatants.Count(c => c.Team == CombatTeam.Enemy) == 2,
                "Cancelling the warning still starts the two-soldier encounter.");
            var first = battle.Session;
            Call(guards, "LateUpdate"); Check(battle.Session == first && !dialogue.IsOpen, "The encounter starts only once.");

            Call(guards, "CompleteEncounter", BattleOutcome.None);
            Check(patrol.activeSelf && !progress.CastleGuardsDefeated && blocker.enabled,
                "An encounter without a victory never removes the guards or unlocks the bridge.");
            ResetBattle(battle, dialogue);
            player.Teleport(new Vector2(-2.2f, 11.5f)); Call(guards, "LateUpdate");
            Check(dialogue.IsOpen, "The other side of the approach cannot bypass the warning.");
            dialogue.Advance(); Call(guards, "LateUpdate");
            Check(battle.IsActive, "Finishing the warning normally also starts combat.");
            Call(guards, "CompleteEncounter", BattleOutcome.Defeat);
            Check(patrol.activeSelf && !progress.CastleGuardsDefeated, "Defeat keeps the patrol in place.");
            ResetBattle(battle, dialogue);

            // A victory in a different encounter must not clear the bridge.
            battle.StartBattle();
            Call(guards, "CompleteEncounter", BattleOutcome.Victory);
            Check(!progress.CastleGuardsDefeated && patrol.activeSelf, "Unrelated victories do not remove the patrol.");
            ResetBattle(battle, dialogue);
            player.Teleport(PrototypeCityLayout.Bridge); Call(guards, "LateUpdate");
            Check(dialogue.IsOpen, "An old save already on the bridge still triggers the guards.");
            dialogue.Advance(); Call(guards, "LateUpdate");

            // Complete the real combat model, then let the controller emit its normal completion event.
            for (int turns = 0; battle.Session.Outcome == BattleOutcome.None && turns < 500; turns++)
            {
                CombatantState actor = battle.Session.CurrentActor;
                AbilityDefinition ability = actor.Team == CombatTeam.Party
                    ? actor.Definition.Abilities.First(a => a.Id == "attack" || a.SourceAbilityId == "attack")
                    : actor.Definition.Abilities.First(a => a.Effect == AbilityEffect.Defend);
                CombatantState target = actor.Team == CombatTeam.Party
                    ? battle.Session.Combatants.First(c => c.Team == CombatTeam.Enemy && c.IsAlive) : actor;
                bool accepted = battle.Session.TryAct(actor, ability, target, out string message);
                Check(accepted, "A valid combat turn resolves: " + message);
            }
            Check(battle.Session.Outcome == BattleOutcome.Victory, "The patrol can be defeated in combat.");
            Call(battle, "AwardIfComplete");
            Check(progress.CastleGuardsDefeated && !patrol.activeSelf && !blocker.gameObject.activeInHierarchy,
                "Victory removes both the patrol and its physical barrier.");
            ResetBattle(battle, dialogue); guards.RestoreState();
            Check(!patrol.activeSelf, "Reentering a cleared map keeps the guards absent.");
            progress = new ExplorationProgress(); guards.RestoreState();
            Check(patrol.activeSelf && blocker.gameObject.activeInHierarchy, "Loading an unbeaten journey restores the patrol and barrier.");
            UnityEngine.Object.DestroyImmediate(fixture); fixture = null;
            string result = "BRIDGE_GUARD_UNITY_CHECKS_OK: " + checks + " checks passed.";
            string output = System.IO.Path.Combine(Application.dataPath, "../Temp/BridgeGuardChecks.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            System.IO.File.WriteAllText(output, result);
            Debug.Log(result);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
        finally { focus.SetValue(null, previousFocus); }
    }
}
