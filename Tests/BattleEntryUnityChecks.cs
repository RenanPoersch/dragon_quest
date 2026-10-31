using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World;
using DragonQuest.Presentation.Battle;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Content;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
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
            ValidateFullScreenRotation();
            ValidateLiquidFlow();
            ValidateCameraCapture();
            ValidateFinalBattleFraming();
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
        sequence.Advance(1.5f);
        Check(!sequence.ShowsBattle && sequence.SwirlProgress < .5f, "the old entry cutoff now occurs before the midpoint of the slower swirl");
        sequence.Begin();
        sequence.Advance(BattleEntrySequence.SwirlDuration / 2);
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
            var controller = root.AddComponent<BattleController>();
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
            Check(!controller.TryPlayerAction(AbilityCatalog.Attack, session.Combatants.Last()), "attacks are rejected during the swirl");
            Check(!controller.TryPlayerItem(null, session.Combatants.First()), "items are rejected before accessing or consuming them");
            for (int i = 0; i < 10; i++) typeof(BattleController).GetMethod("Update", Private).Invoke(controller, null);
            Check(session.Combatants.Sum(member => member.TurnsTaken) == turns && session.Combatants.Sum(member => member.Hp) == hp,
                "neither party nor enemy turns advance behind the overlay");
            controller.EntryTransition.Sequence.Advance(10);
            controller.EntryTransition.Sequence.MarkBattleReady();
            controller.EntryTransition.Sequence.Advance(BattleEntrySequence.CoveredDuration + BattleEntrySequence.RevealDuration / 2);
            Check(controller.IsEnteringBattle && !controller.TryPlayerAction(AbilityCatalog.Attack, session.Combatants.Last()),
                "input remains blocked through the reveal");
            controller.EntryTransition.enabled = false;
            typeof(BattleEntryTransition).GetMethod("OnDisable", Private).Invoke(controller.EntryTransition, null);
            Check(!controller.IsEnteringBattle, "disabling cleans up without leaving an input lock");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ValidateCameraCapture()
    {
        Check(GraphicsSettings.currentRenderPipeline != null, "capture runs with the game's URP renderer");
        Texture2D map = Resources.Load<Texture2D>("PixelArt/WorldGroundMap");
        Check(map != null, "exploration artwork is available to the camera");
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Battle entry / live exploration camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        var camera = root.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        var picture = new GameObject("Exploration artwork"); picture.transform.SetParent(root.transform, false);
        picture.transform.position = Vector3.zero;
        var renderer = picture.AddComponent<SpriteRenderer>();
        var sprite = Sprite.Create(map, new Rect(0, 0, map.width, map.height), new Vector2(.5f, .5f), map.height / 10f);
        renderer.sprite = sprite;
        var unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        renderer.sharedMaterial = unlit;
        var transition = root.AddComponent<BattleEntryTransition>();
        var begin = typeof(BattleEntryTransition).GetMethod("BeginFromCamera", Private);
        var render = typeof(BattleEntryTransition).GetMethod("RenderSwirl", Private);
        var snapshotField = typeof(BattleEntryTransition).GetField("snapshot", Private);
        var frameField = typeof(BattleEntryTransition).GetField("frame", Private);
        string output = Environment.GetEnvironmentVariable("DQ_BATTLE_ENTRY_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath("Temp/BattleEntryPreview");
        Directory.CreateDirectory(output);
        RenderTexture previous = RenderTexture.active;
        try
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 800), new Vector2Int(960, 720), new Vector2Int(1920, 810) })
            {
                var originalTarget = new RenderTexture(size.x, size.y, 24);
                var readback = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                camera.targetTexture = originalTarget; camera.aspect = (float)size.x / size.y;
                picture.transform.localScale = new Vector3(camera.aspect / ((float)map.width / map.height), 1, 1);
                Rect viewport = camera.rect;
                float aspect = camera.aspect;
                try
                {
                    camera.Render(); // Warm the same renderer used by the game's camera stack.
                    Color32[] reference = Read(originalTarget, readback);
                    Check(reference.Count(p => p.r > 25 || p.g > 25 || p.b > 25) > reference.Length * .7,
                        "the real camera sees the exploration map at " + size);
                    RenderTexture.active = previous;
                    begin.Invoke(transition, new object[] { camera });
                    var captured = (RenderTexture)snapshotField.GetValue(transition);
                    var frame = (RenderTexture)frameField.GetValue(transition);
                    Check(transition.IsPlaying && captured != null && frame != null, "capture exists before animation starts");
                    Check(camera.targetTexture == originalTarget && camera.rect == viewport && Mathf.Approximately(camera.aspect, aspect)
                        && RenderTexture.active == previous, "capture restores camera and render state");
                    Color32[] mapCapture = Read(captured, readback);
                    Check(Difference(reference, mapCapture) < 2, "transition captures actual exploration pixels instead of a blank buffer");
                    Color32[] first = Read(frame, readback);
                    Check(Difference(mapCapture, first) < 2, "the first animation frame contains the captured map");
                    transition.Sequence.Advance(BattleEntrySequence.SwirlDuration * .4f); render.Invoke(transition, null);
                    Color32[] winding = Read(frame, readback);
                    Check(Difference(first, winding) > 10 && winding.Count(p => p.r > 25 || p.g > 25 || p.b > 25) > winding.Length * .7,
                        "the runtime animation visibly twists map pixels before closing");
                    File.WriteAllBytes(Path.Combine(output, "camera-swirl-" + size.x + "x" + size.y + ".png"), readback.EncodeToPNG());
                    transition.Sequence.Advance(BattleEntrySequence.SwirlDuration * .4f); render.Invoke(transition, null);
                    Check(Difference(winding, Read(frame, readback)) > 10, "the runtime clock advances the vortex");
                    transition.Cancel();
                    Check(!transition.IsPlaying && snapshotField.GetValue(transition) == null && frameField.GetValue(transition) == null,
                        "capture resources are released between repeated encounters");
                }
                finally
                {
                    camera.targetTexture = null; RenderTexture.active = previous;
                    originalTarget.Release(); UnityEngine.Object.DestroyImmediate(originalTarget); UnityEngine.Object.DestroyImmediate(readback);
                }
            }
        }
        finally
        {
            transition.Cancel(); RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(unlit); UnityEngine.Object.DestroyImmediate(sprite);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void ValidateFinalBattleFraming()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("King entry / fade without distortion");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<ThroneRoomPixelScenery>().Build();
        var cameraObject = new GameObject("King entry camera"); cameraObject.transform.SetParent(root.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = ThroneRoomLayout.CameraPosition; camera.transform.rotation = ThroneRoomLayout.CameraRotation;
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var heroObject = new GameObject("Party"); heroObject.transform.SetParent(root.transform, false);
        var hero = heroObject.AddComponent<PlayerMovement>(); typeof(PlayerMovement).GetMethod("Awake", Private).Invoke(hero, null);
        var battle = root.AddComponent<BattleController>();
        battle.Initialize(hero, root.AddComponent<DialogueController>(), new ExplorationProgress());
        var transition = battle.EntryTransition;
        var begin = typeof(BattleEntryTransition).GetMethod("BeginFromCamera", Private);
        var snapshotField = typeof(BattleEntryTransition).GetField("snapshot", Private);
        var swirlField = typeof(BattleEntryTransition).GetField("swirl", Private);
        var previous = RenderTexture.active;
        try
        {
            battle.StartCastleBattle();
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720)})
            {
                var target = new RenderTexture(size.x,size.y,24);
                var readback = new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                try
                {
                    camera.targetTexture=target; camera.aspect=(float)size.x/size.y;
                    camera.fieldOfView=CameraFollow.PerspectiveFieldOfView(ThroneRoomLayout.FieldOfView,camera.aspect,ThroneRoomLayout.ArtAspect);
                    camera.rect=new Rect(0,0,1,1); camera.Render();
                    var expected=Read(target,readback);
                    begin.Invoke(transition,new object[]{camera});
                    Check(transition.Sequence.IsFadeOnly && swirlField.GetValue(transition)==null,"king entry does not create or use the swirl shader");
                    Check(Difference(expected,Read((RenderTexture)snapshotField.GetValue(transition),readback))<2,"king fade preserves the exploration camera without stretching");
                    Check(transition.Sequence.BlackOpacity==0,"the captured throne room starts visible");
                    transition.Sequence.Advance(BattleEntrySequence.KingFadeOutDuration/2);
                    Check(Mathf.Abs(transition.Sequence.BlackOpacity-.5f)<.001f,"the throne room fades to black normally");
                    transition.Sequence.Advance(100);
                    Check(transition.Sequence.BlackOpacity==1&&!transition.Sequence.CanStartFinalMusic,"a black repaint must precede music");
                    transition.Sequence.MarkBlackDrawn();transition.Sequence.MarkMusicReady();transition.Sequence.MarkBattleReady();
                    transition.Sequence.Advance(BattleEntrySequence.KingBlackDuration+BattleEntrySequence.KingFadeInDuration+.01f);
                    Check(!transition.IsPlaying,"the battle is released after music and fade-in");
                }
                finally { transition.Cancel();camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(readback); }
            }
        }
        finally { RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void ValidateFullScreenRotation()
    {
        var material = new Material(Resources.Load<Shader>("BattleEntrySwirl"));
        var previous = RenderTexture.active;
        try
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 800), new Vector2Int(960, 720), new Vector2Int(1920, 810) })
            {
                var source = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                var target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
                var readback = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                float aspect = (float)size.x / size.y;
                material.SetFloat("_Aspect", aspect);
                try
                {
                    source.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), size.x * size.y).ToArray()); source.Apply();
                    var turning = Render(source, target, readback, material, .75f);
                    Check(turning.All(p => p.r > 250 && p.g > 250 && p.b > 250),
                        "the whole screen stays filled through the rotation without a closing iris at " + size);
                    var fading = Render(source, target, readback, material, .9f);
                    int darkest = fading.Min(p => (int)p.r), brightest = fading.Max(p => (int)p.r);
                    Check(darkest > 0 && brightest < 250 && brightest - darkest <= 1,
                        "the transition fades the centre and every edge evenly at " + size);

                    // A circular marker must keep its visible area as the scene rotates.
                    // This catches an unwanted zoom even when mirrored edges fill the screen.
                    var pixels = new Color32[size.x * size.y];
                    for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                    {
                        float dx = ((x + .5f) / size.x - .5f) * aspect, dy = (y + .5f) / size.y - .5f;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        pixels[y * size.x + x] = distance > .18f && distance < .24f
                            ? new Color32(255, 255, 255, 255) : new Color32(40, 70, 100, 255);
                    }
                    source.filterMode = FilterMode.Point; source.SetPixels32(pixels); source.Apply();
                    int originalArea = MarkerArea(Render(source, target, readback, material, 0), size);
                    foreach (float progress in new[] { .25f, .55f, .78f })
                    {
                        int area = MarkerArea(Render(source, target, readback, material, progress), size);
                        Check(Math.Abs(area - originalArea) < originalArea * .02f,
                            "visible image scale stays constant during rotation at " + progress + " / " + size + "; area " + area + " / " + originalArea);
                    }
                }
                finally
                {
                    RenderTexture.active = previous; target.Release();
                    UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback);
                }
            }
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(material); }
    }
    private static int MarkerArea(Color32[] pixels, Vector2Int size)
    {
        int count = 0;
        float aspect = (float)size.x / size.y;
        for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
        {
            if (Math.Abs(((x + .5f) / size.x - .5f) * aspect) > .3f || Math.Abs((y + .5f) / size.y - .5f) > .3f) continue;
            Color32 pixel = pixels[y * size.x + x];
            if (pixel.r > 220 && pixel.g > 220 && pixel.b > 220) count++;
        }
        return count;
    }

    private static void ValidateLiquidFlow()
    {
        const int width = 1280, height = 720;
        var source = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var material = new Material(Resources.Load<Shader>("BattleEntrySwirl"));
        var previous = RenderTexture.active;
        try
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                pixels[y * width + x] = (x / 32 + y / 32) % 2 == 0
                    ? new Color32(230, 50, 40, 255) : new Color32(40, 100, 230, 255);
            source.SetPixels32(pixels); source.Apply();
            material.SetFloat("_Aspect", (float)width / height);
            Color32[] first = Render(source, target, readback, material, 0);
            Check(first.Count(p => p.r > 45 && p.r < 225) < first.Length * .01,
                "the initial image preserves crisp exploration detail");
            Color32[] fluid = Render(source, target, readback, material, .65f);
            Check(fluid.Count(p => p.r > 45 && p.r < 225) > fluid.Length * .15,
                "details visibly blend along the liquid vortex instead of only rotating rigidly");
        }
        finally
        {
            RenderTexture.active = previous; target.Release();
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(material);
        }
    }

    private static void ValidateShader()
    {
        Shader shader = Resources.Load<Shader>("BattleEntrySwirl");
        Check(shader != null && shader.isSupported, "vortex shader is included and supported");
        var material = new Material(shader);
        Check(!material.HasProperty("_WhiteShadows"), "the vortex has no black-to-white color replacement");
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
