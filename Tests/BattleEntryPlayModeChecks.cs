using DragonQuest.Presentation.Battle;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Content;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BattleEntryPlayModeChecks
{
    private const string Pending = "DragonQuest.BattleEntryPlayModeChecks.Pending";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static BattleController battle;
    private static BattleEntryTransition transition;
    private static Color32[] initial;
    private static float deadline, coveredAt = -1;
    private static int checks, originalHp, originalTurns;
    private static bool sampled, ready;
    static BattleEntryPlayModeChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception("Battle entry Play Mode: " + message); }
    public static void Run()
    {
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        try
        {
            Check(Application.isPlaying, "checks use the real player loop");
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"), false, "Battle transition validation", false);
            Time.timeScale = 0;
            var root = new GameObject("Battle entry / runtime regression");
            var cameraObject = new GameObject("Exploration camera"); cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 5;
            camera.aspect = (float)Screen.width / Screen.height;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var map = Resources.Load<Texture2D>("PixelArt/WorldGroundMap");
            var picture = new GameObject("Map visible to the exploration camera"); picture.transform.SetParent(root.transform, false);
            var renderer = picture.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(map, new Rect(0, 0, map.width, map.height), new Vector2(.5f, .5f), map.height / 10f);
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            picture.transform.localScale = new Vector3(camera.aspect / ((float)map.width / map.height), 1, 1);
            var warm = new RenderTexture(Screen.width, Screen.height, 24);
            camera.targetTexture = warm; camera.Render(); camera.targetTexture = null;
            warm.Release(); UnityEngine.Object.Destroy(warm);
            var hero = root.AddComponent<PlayerMovement>();
            var dialogue = root.AddComponent<DialogueController>();
            battle = root.AddComponent<BattleController>();
            battle.Initialize(hero, dialogue, new ExplorationProgress());
            battle.StartBattle();
            transition = battle.EntryTransition;
            Check(battle.IsEnteringBattle, "public battle entry starts the transition");
            var snapshot = Texture("snapshot");
            Check(snapshot != null && Texture("frame") != null, "the camera capture is ready before the first animation frame");
            initial = Read(snapshot);
            Check(initial.Count(p => p.r > 25 || p.g > 25 || p.b > 25) > initial.Length * .7,
                "public battle entry captures the visible map instead of black");
            originalHp = battle.Session.Combatants.Sum(member => member.Hp);
            originalTurns = battle.Session.Combatants.Sum(member => member.TurnsTaken);
            deadline = Time.realtimeSinceStartup + 12;
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Fail(error); }
    }
    private static RenderTexture Texture(string name) => (RenderTexture)typeof(BattleEntryTransition).GetField(name, Private).GetValue(transition);
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.Step();
            Check(Time.realtimeSinceStartup < deadline, "animation deadline; frame=" + Time.frameCount + ", elapsed=" + transition.Sequence.Elapsed + ", enabled=" + transition.isActiveAndEnabled);
            Check(battle.Session.Combatants.Sum(member => member.Hp) == originalHp
                && battle.Session.Combatants.Sum(member => member.TurnsTaken) == originalTurns, "no turns run during entry");
            if (!transition.IsPlaying)
            {
                Check(sampled && ready, "the real loop advances from map to vortex, covered battle and reveal");
                Check(Texture("snapshot") == null && Texture("frame") == null, "completion releases GPU resources");
                Debug.Log("BATTLE_ENTRY_PLAYMODE_CHECKS_OK: " + checks + " checks; actual StartBattle, camera capture, animated frames, unscaled time and input lock.");
                EditorApplication.update -= Tick; Time.timeScale = 1; EditorApplication.Exit(0); return;
            }
            if (!sampled && transition.Sequence.SwirlProgress >= .25f && !transition.Sequence.ShowsBattle)
            {
                Color32[] winding = Read(Texture("frame"), "runtime-vortex.png");
                long delta = 0;
                for (int i = 0; i < initial.Length; i++) delta += Math.Abs(initial[i].r - winding[i].r)
                    + Math.Abs(initial[i].g - winding[i].g) + Math.Abs(initial[i].b - winding[i].b);
                Check(delta / (double)(initial.Length * 3) > 10, "Update visibly rotates the captured map while timeScale is zero");
                Check(!battle.TryPlayerAction(AbilityCatalog.Attack, battle.Session.Combatants.Last()), "commands stay blocked during the real animation");
                sampled = true;
            }
            if (transition.Sequence.ShowsBattle && !ready)
            {
                if (coveredAt < 0) coveredAt = Time.realtimeSinceStartup;
                Check(transition.Sequence.BlackOpacity == 1, "the fully covered phase waits for battle readiness");
                // IMGUI does not repaint in an invisible batch Editor. Supply the same signal
                // the battle HUD sends after its first complete repaint, then verify the reveal.
                if (Time.realtimeSinceStartup - coveredAt > .15f) { transition.Sequence.MarkBattleReady(); ready = true; }
            }
        }
        catch (Exception error) { Fail(error); }
    }
    private static Color32[] Read(RenderTexture target, string file = null)
    {
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            if (file != null) File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("DQ_BATTLE_ENTRY_OUTPUT"), file), texture.EncodeToPNG());
            return texture.GetPixels32();
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    private static void Fail(Exception error)
    { EditorApplication.update -= Tick; Time.timeScale = 1; Debug.LogException(error); EditorApplication.Exit(1); }
}
