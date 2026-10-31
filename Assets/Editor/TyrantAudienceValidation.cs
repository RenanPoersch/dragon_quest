using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Presentation.Cinematics.Exploration;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Content;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TyrantAudienceValidation
{
    private static int checks;
    private static readonly MethodInfo Advance = typeof(TyrantAudience).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);

    [MenuItem("Tools/Dragon Quest/Validate Tyrant Audience")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_AUDIENCE_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/TyrantAudiencePreview"));
        Directory.CreateDirectory(output); checks = 0;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Audiencia / validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ThroneRoomPixelScenery>().Build();
            var cameraObject = Child(root.transform, "Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.orthographic = false; camera.fieldOfView = ThroneRoomLayout.FieldOfView;
            camera.transform.position = ThroneRoomLayout.CameraPosition; camera.transform.rotation = ThroneRoomLayout.CameraRotation;
            var scenery = new ObliqueScenery();
            var hero = Child(root.transform, "Heroi");
            hero.AddComponent<Rigidbody2D>(); hero.AddComponent<BoxCollider2D>().size = new Vector2(.55f, .6f);
            var movement = hero.AddComponent<PlayerMovement>(); Call(movement, "Awake"); movement.SetFrontalView(true);
            scenery.PixelPerson(hero.transform, "ArenWalk", animated: true); hero.AddComponent<ThroneRoomDepth>();
            var king = Child(root.transform, "Rei Rubro"); king.transform.position = ThroneRoomLayout.King;
            scenery.PixelPerson(king.transform, "TyrantSeated"); king.AddComponent<ThroneRoomDepth>().Refresh();
            var party = PartySetup.Create();
            var audience = Child(root.transform, "Party diante do Rei Rubro").AddComponent<TyrantAudience>();
            audience.Initialize(movement, () => party, scenery);
            var dialogue = root.AddComponent<DialogueController>(); int battles = 0;
            var tyrant = king.AddComponent<TyrantInteraction>();
            tyrant.Initialize(() => party, () => battles++, null, conversation => audience.Begin(king.transform, conversation));
            var context = new InteractionContext(dialogue, new ExplorationProgress(), _ => { });
            movement.Teleport(ThroneRoomLayout.Audience);
            tyrant.Interact(context);
            Assert(dialogue.IsOpen && battles == 0, "party exit opens with the existing dialogue, before battle");
            Assert(audience.transform.childCount == 2, "Lia and Bram emerge without duplicating Aren");
            foreach (Transform companion in audience.transform)
                Assert((Vector2)companion.position == (Vector2)hero.transform.position && !companion.gameObject.activeSelf,
                    "each companion starts inside the hero");
            Assert(Vector2.Dot(movement.FacingDirection, (ThroneRoomLayout.King - (Vector2)hero.transform.position).normalized) > .999f,
                "hero turns toward tyrant when approached from any direction");
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
            try
            {
                Step(audience, .25f); Save(camera, target, root, Path.Combine(output, "audience-emerging.png"));
                Transform lia = audience.transform.GetChild(0), bram = audience.transform.GetChild(1);
                Assert(lia.gameObject.activeSelf && bram.gameObject.activeSelf && Vector2.Distance(lia.position, hero.transform.position) > .1f,
                    "companions walk out visibly during dialogue");
                Assert(Vector2.Distance(lia.position, bram.position) > .1f, "staggered departures separate the sprites");
                Step(audience, 2); CheckFormation(audience, hero.transform, king.transform, scenery);
                Assert(dialogue.PageNumber == 1 && battles == 0, "animation never advances dialogue or battle");
                Save(camera, target, root, Path.Combine(output, "audience-facing-tyrant.png"));
                for (int x = -15; x <= 15; x += 3)
                    for (int y = 34; y <= 43; y += 3)
                    {
                        var origin = new Vector2(x / 10f, y / 10f);
                        if (!ThroneRoomLayout.IsWalkable(origin) || Vector2.Distance(origin, ThroneRoomLayout.King) > 1.6f) continue;
                        dialogue.Close(); Step(audience, 0); movement.Teleport(origin); tyrant.Interact(context);
                        for (int frame = 0; frame < 60; frame++)
                        {
                            Step(audience, 1f / 30);
                            foreach (Transform companion in audience.transform)
                                Assert(ThroneRoomLayout.IsWalkable(companion.position), "departure stays on the floor, including side approaches");
                        }
                        CheckFormation(audience, hero.transform, king.transform, scenery);
                    }
                Assert(battles == 0, "cancelling and reopening conversations never starts battle");
                dialogue.Close(); Step(audience, 0);
                Assert(audience.transform.childCount == 0, "cancelling removes all temporary companions");
                movement.Teleport(ThroneRoomLayout.Audience); tyrant.Interact(context); Step(audience, .05f);
                dialogue.Close(); Step(audience, 0);
                Assert(audience.transform.childCount == 0, "cancellation during departure also cleans up");
                tyrant.Interact(context); Step(audience, 2);
                while (dialogue.IsOpen) dialogue.Advance(); Step(audience, 0);
                Assert(battles == 1 && audience.transform.childCount == 0, "finishing dialogue starts one battle and removes exploration companions");
                tyrant.Interact(context); Step(audience, .2f); movement.Teleport(ThroneRoomLayout.Spawn); Step(audience, 0);
                Assert(audience.transform.childCount == 0, "restoring or travelling cannot leave companions behind");
                dialogue.Close(); movement.Teleport(ThroneRoomLayout.Audience); tyrant.Interact(context); Step(audience, .2f);
                Call(audience, "OnDisable");
                Assert(audience.transform.childCount == 0, "disabling the presentation cleans up");
                Assert(party.Members.Count == 3 && party.Members.All(member => member.Hp == member.MaxHp), "presentation preserves party membership and HP");
            }
            finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            string result = "TYRANT_AUDIENCE_VALIDATION_OK: " + checks + " checks; animated departure, formation, floor, facing, cancellation, repeat, battle callback, travel and unchanged party.";
            File.WriteAllText(Path.Combine(output, "result.txt"), result); Debug.Log(result);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void CheckFormation(TyrantAudience audience, Transform hero, Transform king, ObliqueScenery scenery)
    {
        Assert(audience.transform.childCount == 2, "both companions have valid audience positions");
        Transform first = audience.transform.GetChild(0), second = audience.transform.GetChild(1);
        Assert(Vector2.Distance(first.position, second.position) >= 1.05f, "companions stand apart");
        foreach (Transform companion in audience.transform)
        {
            Assert(Vector2.Distance(companion.position, hero.position) >= 1.05f && ThroneRoomLayout.IsWalkable(companion.position), "companion stands apart from hero on walkable floor");
            var renderer = companion.GetComponentInChildren<SpriteRenderer>();
            string id = companion == first ? "lia" : "bram";
            var direction = PixelSpriteLayout.Facing(ObliqueProjection.GroundDirection((Vector2)(king.position - companion.position)));
            Assert(renderer.sprite == scenery.PixelSprites.Party(id).Sprites[(int)direction], "companion stops walking and faces the tyrant");
            Assert(Math.Abs(renderer.transform.localPosition.z + .075f + ThroneRoomLayout.FloorHeight(companion.position)) < .001f,
                "sprite follows the height of the throne room stairs");
        }
    }
    private static GameObject Child(Transform parent, string name)
    { var child = new GameObject(name); child.transform.SetParent(parent, false); return child; }
    private static void Step(TyrantAudience audience, float deltaTime) => Advance.Invoke(audience, new object[] { deltaTime });
    private static void Call(object component, string method)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    private static void Assert(bool valid, string reason)
    { checks++; if (!valid) throw new Exception("Tyrant audience: " + reason); }
    private static void Save(Camera camera, RenderTexture target, GameObject root, string path)
    {
        foreach (PixelCharacter character in root.GetComponentsInChildren<PixelCharacter>())
        {
            Call(character, "LateUpdate");
            character.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
            character.transform.localScale = new Vector3(1, PixelSpriteLayout.UprightScaleY, 1);
        }
        foreach (ThroneRoomDepth depth in root.GetComponentsInChildren<ThroneRoomDepth>()) depth.Refresh();
        camera.Render(); camera.Render();
        RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try { pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous; }
    }
}
