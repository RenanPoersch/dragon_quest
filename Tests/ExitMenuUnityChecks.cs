using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Presentation.Menus;
using System;
using System.IO;
using System.Reflection;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs in the disposable Unity project so scene reloads cannot interrupt the user's Editor.
[InitializeOnLoad]
public static class ExitMenuUnityChecks
{
    private const string Pending = "DragonQuest.ExitMenuChecks";
    private const string ScenePath = "Assets/ExitMenuChecks.unity";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static int step, checks, requestedFrame;
    private static double began;
    private static GameObject original;
    private static string saveDirectory;
    static ExitMenuUnityChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        began = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
    }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static void Put(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Title fixture").AddComponent<SaveController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        SessionState.SetBool(Pending, true);
        began = EditorApplication.timeSinceStartup;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Exit scene reload timed out.");
            if (!EditorApplication.isPlaying || !Application.isPlaying) return;
            if (step == 0)
            {
                original = GameObject.Find("Title fixture");
                var saves = original.GetComponent<SaveController>();
                saves.enabled = false;
                var player = original.AddComponent<PlayerMovement>();
                var dialogue = original.AddComponent<DialogueController>();
                var battle = original.AddComponent<BattleController>();
                battle.Initialize(player, dialogue, new ExplorationProgress());
                battle.enabled = false;
                var inventoryHost = original.AddComponent<InventoryHud>();
                inventoryHost.Initialize(battle, new AncientSecrets()); inventoryHost.enabled = false;
                var inventory = inventoryHost.Presenter;
                saveDirectory = Path.Combine(Path.GetTempPath(), "DragonQuestExitMenu-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(saveDirectory);
                File.WriteAllText(Path.Combine(saveDirectory, "keep.txt"), "existing save content");
                Put(saves, "store", new SaveStore(saveDirectory)); Put(saves, "battle", battle);
                Put(saves, "<IsMenuVisible>k__BackingField", false); Put(battle, "<IsMenuOpen>k__BackingField", true);
                Call(inventory, "CheckOpened"); Put(inventory, "openedFrame", -1);
                Put(inventory, "selectedMenu", Array.IndexOf(InventoryMenuPresentation.Commands, "Sair"));
                typeof(GameInput).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Func<bool>)(() => true));
                Check(GameInput.IsFrameAvailable && saves.CanReturnToTitle, "Sair is available from the open party menu.");
                requestedFrame = Time.frameCount;
                typeof(InventoryMenuPresenter).GetMethod("OpenCommand", Private).Invoke(inventory,
                    new object[] { Array.IndexOf(InventoryMenuPresentation.Commands, "Sair") });
                Check(!GameInput.IsFrameAvailable, "Sair consumes its confirmation before loading the initial scene.");
                step = 1;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            else if (Time.frameCount > requestedFrame && original == null)
            {
                var title = UnityEngine.Object.FindAnyObjectByType<SaveController>();
                Check(title != null && title.IsMenuVisible && title.IsFrontEndVisible, "Sair reloads the initial title state.");
                Check(SceneManager.GetActiveScene().path == ScenePath, "The current game scene reloads through the same return path used by endings.");
                Check(File.ReadAllText(Path.Combine(saveDirectory, "keep.txt")) == "existing save content"
                    && Directory.GetFiles(saveDirectory).Length == 1, "Sair leaves existing files unchanged and creates no automatic save.");
                SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
                Debug.Log("EXIT_MENU_UNITY_CHECKS_OK: " + checks + " checks passed."); EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
            Debug.LogException(error); EditorApplication.Exit(1);
        }
    }
}
