using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class BattleEntryUnityChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool condition, string reason)
    { checks++; if (!condition) throw new Exception("Battle entry: " + reason); }

    public static void Run()
    {
        try
        {
            ValidateSequence();
            ValidateController();
            ValidateShader();
            Debug.Log("BATTLE_ENTRY_UNITY_CHECKS_OK: " + checks + " checks.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void ValidateSequence()
    {
        var sequence = new BattleEntrySequence();
        sequence.Begin();
        Check(sequence.IsPlaying && !sequence.ShowsBattle && sequence.SwirlProgress == 0, "starts with the exploration picture");
        sequence.MarkBattleReady(); // A premature notification must not reveal an unpainted battle.
        sequence.Advance(.75f);
        Check(!sequence.ShowsBattle && Mathf.Abs(sequence.SwirlProgress - .5f) < .001f, "full swirl precedes battle visibility");
        sequence.Advance(100);
        Check(sequence.IsPlaying && sequence.ShowsBattle && sequence.BlackOpacity == 1, "slow loading holds at full black");
        sequence.Advance(100);
        Check(sequence.IsPlaying && sequence.BlackOpacity == 1, "waiting cannot time out and expose missing art");
        sequence.MarkBattleReady();
        sequence.Advance(BattleEntrySequence.CoveredDuration);
        Check(sequence.BlackOpacity > .999f, "complete battle repaint remains covered");
        sequence.Advance(BattleEntrySequence.RevealDuration / 2);
        Check(Mathf.Abs(sequence.BlackOpacity - .5f) < .001f && sequence.IsPlaying, "battle fades in with input still blocked");
        sequence.Advance(BattleEntrySequence.RevealDuration);
        Check(!sequence.IsPlaying, "completion releases the battle");
        sequence.Begin(); sequence.Advance(-1);
        Check(sequence.Elapsed == 0 && !sequence.ShowsBattle, "repeated encounters reset timing and reject negative deltas");
        sequence.Cancel();
        Check(!sequence.IsPlaying && sequence.Elapsed == 0, "cancellation clears transition state");
    }

    private static void ValidateController()
    {
        var root = new GameObject("Battle entry / controller checks");
        try
        {
            var hero = root.AddComponent<PlayerMovement>();
            typeof(PlayerMovement).GetMethod("Awake", Private).Invoke(hero, null);
            var dialogue = root.AddComponent<DialogueController>();
            var controller = root.AddComponent<PrototypeBattleController>();
            controller.Initialize(hero, dialogue, new ExplorationProgress());
            controller.StartBattle();
            Check(controller.IsActive && controller.EntryTransition != null, "every encounter owns a transition");
            controller.EntryTransition.Sequence.Begin();
            BattleSession session = controller.Session;
            int turns = session.Combatants.Sum(member => member.TurnsTaken);
            int hp = session.Combatants.Sum(member => member.Hp);
            Check(controller.IsEnteringBattle && controller.IsBusy, "transition blocks exploration and other encounters");
            Check(!(bool)typeof(PlayerMovement).GetField("movementEnabled", Private).GetValue(hero), "movement stays disabled");
            controller.StartBattle(); controller.StartCastleBattle();
            Check(controller.Session == session && !controller.StartRandomBattle(), "repeated triggers cannot replace an entering encounter");
            Check(!controller.TryPlayerAction(PrototypeBattleFactory.Attack, session.Combatants.Last()), "attacks are rejected during the swirl");
            Check(!controller.TryPlayerItem(null, session.Combatants.First()), "items are rejected before accessing or consuming them");
            for (int i = 0; i < 10; i++) typeof(PrototypeBattleController).GetMethod("Update", Private).Invoke(controller, null);
            Check(session.Combatants.Sum(member => member.TurnsTaken) == turns && session.Combatants.Sum(member => member.Hp) == hp,
                "neither party nor enemy turns advance behind the overlay");
            controller.EntryTransition.Sequence.Advance(10);
            controller.EntryTransition.Sequence.MarkBattleReady();
            controller.EntryTransition.Sequence.Advance(BattleEntrySequence.CoveredDuration + BattleEntrySequence.RevealDuration / 2);
            Check(controller.IsEnteringBattle && !controller.TryPlayerAction(PrototypeBattleFactory.Attack, session.Combatants.Last()),
                "input remains blocked through the reveal");
            controller.EntryTransition.enabled = false;
            typeof(BattleEntryTransition).GetMethod("OnDisable", Private).Invoke(controller.EntryTransition, null);
            Check(!controller.IsEnteringBattle, "disabling cleans up without leaving an input lock");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ValidateShader()
    {
        Shader shader = Resources.Load<Shader>("BattleEntrySwirl");
        Check(shader != null && shader.isSupported, "vortex shader is included and supported");
        var material = new Material(shader);
        string output = Environment.GetEnvironmentVariable("DQ_BATTLE_ENTRY_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath("Temp/BattleEntryPreview");
        Directory.CreateDirectory(output);
        try
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 800), new Vector2Int(960, 720), new Vector2Int(1920, 810) })
            {
                var source = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                var pixels = new Color32[size.x * size.y];
                for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                    pixels[y * size.x + x] = new Color32((byte)(50 + x * 180 / size.x), (byte)(50 + y * 180 / size.y),
                        (byte)(70 + ((x / 64 + y / 64) % 2) * 120), 255);
                source.SetPixels32(pixels); source.Apply();
                source.wrapMode = TextureWrapMode.Clamp;
                var target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
                var readback = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    material.SetFloat("_Aspect", (float)size.x / size.y);
                    Graphics.Blit(source, target);
                    Color32[] reference = Read(target, readback);
                    Color32[] start = Render(source, target, readback, material, 0);
                    Check(Difference(reference, start) < 1, "first frame preserves image orientation at " + size);
                    Color32[] turning = Render(source, target, readback, material, .35f);
                    Check(Difference(start, turning) > 25, "the screen actually twists and rotates at " + size);
                    Check(turning.All(p => p.r > 10 || p.g > 10 || p.b > 10), "rotating corners never reveal empty borders at " + size);
                    Color32[] later = Render(source, target, readback, material, .65f);
                    Check(Difference(turning, later) > 20, "rotation progresses over time at " + size);
                    Color32[] end = Render(source, target, readback, material, 1);
                    Check(end.All(p => p.r == 0 && p.g == 0 && p.b == 0), "the entire viewport closes to black at " + size);

                    Texture map = Resources.Load<Texture2D>("PixelArt/WorldGroundMap");
                    if (size.x == 1280 && size.y == 720 && map != null)
                        foreach (float progress in new[] { 0f, .25f, .5f, .75f, 1f })
                        {
                            Render(map, target, readback, material, progress);
                            File.WriteAllBytes(Path.Combine(output, "swirl-" + progress.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png"), readback.EncodeToPNG());
                        }
                }
                finally
                {
                    RenderTexture.active = previous; target.Release();
                    UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(readback);
                }
            }
            Check(!ShaderUtil.GetShaderMessages(shader).Any(message => message.severity.ToString() == "Error"), "GPU compilation has no shader errors");
        }
        finally { UnityEngine.Object.DestroyImmediate(material); }
    }

    private static Color32[] Render(Texture source, RenderTexture target, Texture2D readback, Material material, float progress)
    { material.SetFloat("_Progress", progress); Graphics.Blit(source, target, material); return Read(target, readback); }
    private static Color32[] Read(RenderTexture target, Texture2D readback)
    { RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); readback.Apply(); return readback.GetPixels32(); }
    private static double Difference(Color32[] first, Color32[] second)
    {
        long delta = 0;
        for (int i = 0; i < first.Length; i++) delta += Math.Abs(first[i].r - second[i].r) + Math.Abs(first[i].g - second[i].g) + Math.Abs(first[i].b - second[i].b);
        return delta / (double)(first.Length * 3);
    }
}
