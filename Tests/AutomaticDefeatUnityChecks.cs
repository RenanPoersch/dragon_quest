using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Uses a disposable project: the real game scene and the user's saves stay untouched.
[InitializeOnLoad]
public static class AutomaticDefeatUnityChecks
{
    private const string Pending = "DragonQuest.AutomaticDefeatChecks";
    private const string ScenePath = "Assets/AutomaticDefeatChecks.unity";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    private static int step, checks, requests, installed, requestedFrame;
    private static double began;
    private static GameObject original;
    private static SaveStore store;
    private static byte[] savedBytes;
    private static PartyProgress defeatedParty;

    static AutomaticDefeatUnityChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static object Get(object target, string field) => target.GetType().GetField(field, Hidden).GetValue(target);
    private static void Put(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    private static void Damage(CombatantState target, int amount)
    {
        var method = typeof(CombatantState).GetMethod("TakeDamage", Hidden);
        method.Invoke(target, method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (object)amount).ToArray());
    }
    private static void Age(PrototypeBattleController battle, float elapsed) => Put(battle, "<ActionStartedAt>k__BackingField", Time.unscaledTime - elapsed);
    private static void Focus(bool value) => typeof(GameInput).GetField("hasFocus", StaticHidden).SetValue(null, (Func<bool>)(() => value));
    private static bool LoadRequested => (bool)typeof(PrototypeSaveController).GetField("loadGameOnStart", StaticHidden).GetValue(null);
    private static string Page(PrototypeSaveController saves) => Get(saves, "page").ToString();

    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Load Game fixture").AddComponent<PrototypeSaveController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        SessionState.SetBool(Pending, true);
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.EnterPlaymode();
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        began = EditorApplication.timeSinceStartup;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    private static PrototypeSaveController InitializeTitle()
    {
        original = GameObject.Find("Load Game fixture");
        var saves = original.GetComponent<PrototypeSaveController>(); saves.enabled = false;
        var player = original.AddComponent<PlayerMovement>();
        var dialogue = original.AddComponent<DialogueController>();
        var battle = original.AddComponent<PrototypeBattleController>(); battle.enabled = false;
        var progress = new ExplorationProgress(50);
        Action goToLoad = () => { requests++; saves.ReturnToLoadGame(); };
        battle.Initialize(player, dialogue, progress, defeatPresentation: goToLoad);
        original.GetComponent<PrototypePostBattleHud>().enabled = false;
        saves.Initialize(player, battle, null, null, dialogue, progress, new AncientSecrets(), new DragonProgress(), journey =>
        {
            installed++;
            battle.Initialize(player, dialogue, journey.Progress, restoredParty: journey.Party, defeatPresentation: goToLoad);
        });
        Put(saves, "store", store); Put(saves, "selectedOption", 0); Call(saves, "RefreshSlots");
        return saves;
    }
    private static void CheckSavesUnchanged()
    {
        Check(File.ReadAllBytes(store.SlotPath(2)).SequenceEqual(savedBytes), "Defeat never overwrites the last saved journey.");
        Check(Directory.GetFiles(Path.GetDirectoryName(store.SlotPath(2))).Length == 1, "Defeat creates no automatic save or backup.");
    }
    private static void CheckEncounterRoutes()
    {
        for (int kind = 0; kind < 4; kind++)
        {
            var root = new GameObject("Defeat route " + kind);
            try
            {
                int loads = 0, endings = 0;
                var player = root.AddComponent<PlayerMovement>();
                var battle = root.AddComponent<PrototypeBattleController>(); battle.enabled = false;
                battle.Initialize(player, root.AddComponent<DialogueController>(), new ExplorationProgress(),
                    defeatPresentation: () => loads++, endingPresentation: _ => endings++);
                root.GetComponent<PrototypePostBattleHud>().enabled = false;
                if (kind == 1) Call(battle, "StartEncounter", PrototypeBattleFactory.CreateDragonEncounter(battle.Party), true);
                else if (kind == 2) battle.StartCastleBattle();
                else battle.StartBattle();
                battle.EntryTransition.Cancel();
                foreach (var member in battle.Session.Combatants.Where(c => kind == 3 ? c.Team == CombatTeam.Enemy : c.Team == CombatTeam.Party))
                    Damage(member, member.Hp);
                Call(battle.Session, "CheckOutcome"); Call(battle, "AwardIfComplete"); Call(battle, "Update");
                if (kind == 3)
                    Check(loads == 0 && battle.IsActive && battle.IsPostBattleOpen && !battle.HasEnded, "Victory retains its results screen.");
                else
                {
                    Check(battle.Session.Outcome == BattleOutcome.Defeat && loads == 1 && endings == 0,
                        "Ordinary, dragon and castle defeats all request Load Game rather than an ending: " + kind);
                    Check(!battle.IsActive && battle.HasEnded && !battle.IsPostBattleOpen && !battle.Party.HasLivingMembers,
                        "Defeat blocks exploration without reviving the party.");
                    Check(!(bool)Get(player, "movementEnabled"), "The defeated party cannot walk before scene reload.");
                    Call(battle, "Update"); battle.ReturnToCity(); Call(battle, "Update");
                    Check(loads == 1, "A defeat requests the menu only once.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Automatic defeat scene reload timed out.");
            if (!EditorApplication.isPlaying || !Application.isPlaying) return;
            if (step == 0)
            {
                typeof(GameInput).GetMethod("Reset", StaticHidden).Invoke(null, null); Focus(false);
                CheckEncounterRoutes();
                var party = PrototypePartyFactory.Create(); var progress = new ExplorationProgress(50);
                store = new SaveStore(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestSaves"));
                Check(store.TryWrite(2, JourneySnapshot.Capture(party, progress, new AncientSecrets(), new DragonProgress(),
                    PrototypeShopFactory.Create(party, progress), PrototypeTravel.World, 0, -7), out _), "Create a saved journey in the test project.");
                savedBytes = File.ReadAllBytes(store.SlotPath(2));
                // Skip artwork in this fixture; the real return request is exercised below.
                typeof(PrototypeSaveController).GetField("loadGameOnStart", StaticHidden).SetValue(null, true);
                var saves = InitializeTitle(); Put(saves, "<IsMenuVisible>k__BackingField", false);
                var battle = original.GetComponent<PrototypeBattleController>();
                battle.StartBattle(); battle.EntryTransition.Cancel();
                var hero = battle.Session.Combatants.First(c => c.Team == CombatTeam.Party);
                foreach (var ally in battle.Session.Combatants.Where(c => c.Team == CombatTeam.Party)) Damage(ally, ally.Hp - (ally == hero ? 1 : 0));
                while (battle.Session.CurrentActor?.Team == CombatTeam.Party)
                {
                    var actor = battle.Session.CurrentActor;
                    Check(battle.Session.TryAct(actor, actor.Definition.Abilities.Single(a => a.Effect == AbilityEffect.Defend), actor, out _), "Advance to the fatal enemy action.");
                }
                Call(battle, "Update"); Put(battle, "enemyActionTime", Time.unscaledTime - 1); Call(battle, "Update");
                Check(battle.Session.Outcome == BattleOutcome.Defeat && battle.IsPresentingAction && requests == 0, "The fatal action finishes before Load Game.");
                battle.ReturnToCity(); Call(battle, "Update");
                Check(requests == 0 && battle.IsActive, "Confirmation cannot skip the fatal animation.");
                float duration = battle.ActionEndsAt - battle.ActionStartedAt;
                Age(battle, duration - .01f); Call(battle, "Update");
                Check(requests == 0, "The final impact remains visible until its animation ends.");
                defeatedParty = battle.Party;
                Age(battle, duration + .1f); Call(battle, "Update");
                Check(requests == 1 && LoadRequested && !battle.IsActive && battle.HasEnded, "Defeat automatically requests the initial Load Game screen.");
                Check(!GameInput.IsFrameAvailable && !defeatedParty.HasLivingMembers, "The defeat transition consumes input without recovering the party.");
                Call(battle, "Update"); battle.ReturnToCity(); Check(requests == 1, "The transition is not replayed.");
                requestedFrame = Time.frameCount; step = 1; EditorApplication.QueuePlayerLoopUpdate();
            }
            else if (Time.frameCount > requestedFrame && original == null)
            {
                var saves = InitializeTitle();
                Check(saves.IsMenuVisible && saves.IsFrontEndVisible && Page(saves) == "Continue" && !saves.IsIntroPlaying,
                    "Scene reload opens Load Game directly without replaying the intro.");
                Check(!LoadRequested, "The startup request is consumed once.");
                CheckSavesUnchanged();
                if (step == 1)
                {
                    Check(installed == 0 && (int)Get(saves, "selectedOption") == 1, "Load Game selects the first available save without auto-loading it.");
                    Call(saves, "Continue", 2);
                    var battle = original.GetComponent<PrototypeBattleController>();
                    Check(installed == 1 && !saves.IsMenuVisible && saves.ActiveSlot == 2 && battle.Party.HasLivingMembers,
                        "Selecting a slot restores the saved party and leaves the menu.");
                    Check(battle.Party.Members.All(m => m.Hp == m.MaxHp) && !defeatedParty.HasLivingMembers,
                        "Loading restores an independent saved journey, not the defeated state.");
                    for (int i = 0; i < battle.Party.Members.Count; i++)
                        Call(battle.Party.Members[i], "RestoreVitals", i == 0 ? 1 : 0, 0, i == 0, 0);
                    Put(battle.Party, "poisonElapsed", PartyProgress.PoisonTickSeconds + 1); Focus(true);
                    Call(battle, "LateUpdate");
                    Check(requests == 2 && !battle.Party.HasLivingMembers && !original.GetComponent<DialogueController>().IsOpen,
                        "A poison party wipe also returns to Load Game without an inn dialogue or revival.");
                    Focus(false); requestedFrame = Time.frameCount; step = 2; EditorApplication.QueuePlayerLoopUpdate();
                }
                else
                {
                    Put(saves, "store", new SaveStore(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "EmptySaves")));
                    Put(saves, "selectedOption", 0); Call(saves, "RefreshSlots");
                    Check((int)Get(saves, "selectedOption") == SaveStore.SlotCount, "With no saves, Load Game selects Back rather than a disabled slot.");
                    Call(saves, "UpdateMenuInput", KeyCode.Escape);
                    Check(Page(saves) == "Title" && saves.IsMenuVisible, "An unsaved player can return to New Game.");
                    SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
                    Debug.Log("AUTOMATIC_DEFEAT_UNITY_OK: " + checks + " checks; fatal animation, normal/dragon/castle defeats, scene reload, saved slot selection, poison and empty saves.");
                    EditorApplication.Exit(0);
                }
            }
        }
        catch (Exception error)
        {
            SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
            Debug.LogException(error); EditorApplication.Exit(1);
        }
    }
}
