using System;
using System.IO;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DragonLairValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Dragon Lair")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_LAIR_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DragonLairPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        PixelSpriteLibrary people = null;
        try
        {
            var root = new GameObject("Dragon lair validation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var lair = root.AddComponent<DragonLairScenery>(); lair.Build();
            var anchor = new GameObject("Dragon interaction / snout"); anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = PrototypeVolcanoLayout.Dragon;
            var animation = lair.CreateDragon(anchor.transform);
            var essence = lair.CreateEssence(anchor.transform);
            var progress = new DragonProgress();
            int battles = 0;
            var interaction = anchor.AddComponent<DragonInteraction>();
            interaction.Initialize(progress, () => battles++, animation.gameObject, essence);
            var dialogue = root.AddComponent<DialogueController>();
            var context = new InteractionContext(dialogue, new ExplorationProgress(), _ => { });
            Assert(animation.State == DragonRestState.Sleeping, "dragon starts asleep before interaction");
            animation.Advance(10);
            Assert(animation.State == DragonRestState.Sleeping && !essence.activeSelf, "time alone cannot wake dragon or reveal essence");

            var cameraObject = new GameObject("Dragon lair side camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.05f, .03f, .07f);
            var follow = cameraObject.AddComponent<CameraFollow>();
            follow.SetSideView(true, PrototypeVolcanoLayout.CameraHeight);
            var hero = new GameObject("Hero / lateral pose"); hero.transform.SetParent(root.transform, false);
            people = new PixelSpriteLibrary();
            var visual = people.Create(hero.transform, "ArenWalk", 2);
            visual.GetComponent<PixelCharacter>().enabled = false;
            visual.localRotation = Quaternion.identity; visual.localScale = Vector3.one;

            foreach (float x in new[] { -17.5f, -5f, 5.8f, 12.7f })
            {
                hero.transform.localPosition = new Vector2(x, PrototypeVolcanoLayout.FloorAt(x));
                Capture(camera, follow, hero.transform, 1280, 720, Path.Combine(output, "lair-walk-" + x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png"));
            }
            Assert(Vector2.Distance(hero.transform.position, anchor.transform.position) < 1.6f, "snout is reachable at end of lateral path");
            interaction.Interact(context);
            Assert(animation.State == DragonRestState.Waking && dialogue.IsOpen && battles == 0, "E wakes dragon and opens dialogue before battle");
            animation.Advance(.4f);
            Assert(animation.CurrentFrame == 1, "head-lifting pose is visible");
            Capture(camera, follow, hero.transform, 1280, 720, Path.Combine(output, "lair-waking.png"));
            animation.Advance(2);
            Assert(animation.State == DragonRestState.Awake && animation.CurrentFrame == 2, "dragon unfolds wings");
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1280,800), new Vector2Int(960,720) })
                Capture(camera, follow, hero.transform, size.x, size.y, Path.Combine(output, "lair-awake-" + size.x + "x" + size.y + ".png"));
            dialogue.Advance(); dialogue.Advance();
            Assert(battles == 1 && !dialogue.IsOpen, "completed dialogue starts a single battle");

            Reenter(anchor);
            Assert(animation.State == DragonRestState.Sleeping, "return visit restores sleeping pose");
            interaction.Interact(context); dialogue.Close(); animation.Advance(2);
            Assert(battles == 1 && animation.State == DragonRestState.Awake, "Esc cancels encounter and leaves dragon awake");
            interaction.Interact(context); dialogue.Advance(); dialogue.Advance();
            Assert(battles == 2 && animation.State == DragonRestState.Awake, "reinteraction can start the encounter");
            Reenter(anchor);
            interaction.Interact(context); dialogue.Advance(); dialogue.Advance();
            Assert(battles == 3 && animation.CurrentFrame == 2, "fast dialogue still ends on awake pose");

            var party = PrototypePartyFactory.Create();
            var fighter = party.Members[0].CreateCombatant();
            var enemy = new CombatantState(DragonProgress.EnemyId,
                new CombatantDefinition("Dragao", "Boss", 1, 0, 0, 0, 0, 1, PrototypeBattleFactory.Attack), CombatTeam.Enemy);
            var victory = new BattleSession(fighter, enemy);
            Assert(victory.TryAct(fighter, fighter.Definition.Abilities.Single(a => a.Id == "attack"), enemy, out _), "victory fixture finishes combat");
            Assert(progress.TryComplete(victory, party), "dragon victory grants armor");
            interaction.SendMessage("Update");
            Assert(!animation.gameObject.activeSelf && essence.activeSelf, "victory replaces body with liberated pixel essence");
            interaction.Interact(context);
            Assert(dialogue.Title == "Essencia liberta" && battles == 3, "defeated dragon cannot restart encounter");
            Capture(camera, follow, hero.transform, 1280, 720, Path.Combine(output, "lair-essence.png"));
            Assert(!progress.TryComplete(victory, party), "reward cannot be claimed twice");
            Debug.Log("DRAGON_LAIR_VALIDATION_OK: lateral scenery, sleeping and waking artwork, reachable E interaction, dialogue, cancellation, repeat visit, fast advance, defeated essence and three display aspects.");
        }
        finally
        {
            if (people != null)
            {
                foreach (Sprite sprite in people.Atlas("ArenWalk").Sprites)
                    if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(people.Material);
            }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Reenter(GameObject anchor)
    {
        anchor.SetActive(false); anchor.SetActive(true);
        // Preview scenes are in edit mode: invoke the runtime lifecycle handler explicitly.
        anchor.SendMessage("OnEnable", SendMessageOptions.RequireReceiver);
    }

    private static void Capture(Camera camera, CameraFollow follow, Transform hero, int width, int height, string path)
    {
        var target = new RenderTexture(width, height, 24);
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.aspect = (float)width / height;
            follow.Initialize(hero, PrototypeVolcanoLayout.Bounds, PrototypeVolcanoLayout.CameraSize);
            Assert(Quaternion.Angle(camera.transform.rotation, Quaternion.identity) < .001f, "camera has ruins-style horizontal orientation");
            camera.Render(); camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception("Dragon lair: " + message); }
}
