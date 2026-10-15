using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InnValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Inn")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_INN_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/InnPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        PixelSpriteLibrary people = null;
        try
        {
            var root = new GameObject("Pousada / validacao isolada");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<InnPixelScenery>().Build();
            Assert(root.GetComponentsInChildren<SpriteRenderer>().Length == 16, "all sixteen furniture/decor sprites are present");
            Assert(root.GetComponentsInChildren<BoxCollider2D>().Length == PrototypeInnLayout.Blocks.Count(b => b.Solid), "physical fixtures match the room layout");
            Texture2D atlas = Resources.Load<Texture2D>("PixelArt/InnPropsAtlas");
            Assert(atlas.filterMode == FilterMode.Point && atlas.mipmapCount == 1, "source keeps sharp pixels without mipmaps");
            Color32[] pixels = InnPixelScenery.SampleAtlas(atlas.GetPixels32(), atlas.width, atlas.height);
            for (int i = 0; i < 8; i++)
            {
                RectInt bounds = InnPixelScenery.SpriteBounds(i, pixels);
                Assert(bounds.width > 25 && bounds.height > 50, "each prop has a complete silhouette");
                Assert(bounds.xMin % 128 > 0 && bounds.xMax % 128 != 0 && bounds.yMin % 128 > 0 && bounds.yMax % 128 != 0, "transparent gutters separate props");
            }
            Assert(PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Spawn) && PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Exit), "arrival and return are clear");
            Assert(PrototypeInnLayout.SafeSavedPosition(new Vector2(4.4f, -1.25f)) == PrototypeInnLayout.Spawn, "old saves inside new furniture recover safely");
            Vector2 free = new Vector2(-1, -1);
            Assert(PrototypeInnLayout.SafeSavedPosition(free) == free, "valid saves keep their position");
            CheckNavigation();

            var cameraObject = new GameObject("Pousada / camera de validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var follow = cameraObject.AddComponent<CameraFollow>();
            people = new PixelSpriteLibrary();
            Transform hero = Person(root.transform, people, "Heroi", PrototypeInnLayout.Spawn, "ArenWalk", 1);
            Transform keeper = Person(root.transform, people, "Hospedeira", PrototypeInnLayout.Keeper, "NpcRoster", 7);
            keeper.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(.55f, .6f);
            var interaction = keeper.gameObject.AddComponent<InnInteraction>();
            int saves = 0; interaction.Initialize(() => saves++);
            var dialogue = root.AddComponent<DialogueController>();
            var context = new InteractionContext(dialogue, new ExplorationProgress(), _ => { });
            interaction.Interact(context);
            Assert(dialogue.HasChoices, "keeper still opens the save conversation");
            dialogue.Choose(0); Assert(saves == 1, "save callback survives scenery replacement");

            foreach (Vector2Int resolution in new[] { new Vector2Int(1280,720), new Vector2Int(1280,800), new Vector2Int(960,720) })
            {
                var target = new RenderTexture(resolution.x, resolution.y, 24);
                try
                {
                    camera.targetTexture = target;
                    camera.aspect = (float)resolution.x / resolution.y;
                    follow.Initialize(hero, PrototypeInnLayout.Bounds, PrototypeInnLayout.CameraSize, true);
                    foreach (Vector3 vertex in InnPixelScenery.Shell().Vertices)
                    {
                        Vector3 viewport = camera.WorldToViewportPoint(vertex);
                        Assert(viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1, "the complete room fits the viewport");
                    }
                    camera.Render(); camera.Render();
                    Save(target, Path.Combine(output, "inn-" + resolution.x + "x" + resolution.y + ".png"));
                }
                finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "INN_VALIDATION_OK: sprites, alpha, collisions, navigation, save interaction, legacy saves, 16:9, 16:10 and 4:3.");
            Debug.Log("INN_VALIDATION_OK: scenery, navigation, save interaction and three display aspects.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (people != null)
            {
                foreach (string atlas in new[] { "ArenWalk", "NpcRoster" })
                    foreach (Sprite sprite in people.Atlas(atlas).Sprites) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(people.Material);
            }
        }
    }

    private static void CheckNavigation()
    {
        // Mesma folga do corpo do heroi; verifica caminhos reais ate a recepcao e as camas.
        const float step = .2f;
        var reached = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(0, -13); reached.Add(start); queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int delta in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int n = p + delta;
                if (reached.Contains(n) || !PrototypeInnLayout.IsWalkable((Vector2)n * step)) continue;
                reached.Add(n); queue.Enqueue(n);
            }
        }
        foreach (Vector2 target in new[] { PrototypeInnLayout.Exit, new Vector2(-4,.8f), new Vector2(0,.8f), new Vector2(4,.8f), new Vector2(2,-2) })
            Assert(reached.Any(p => Vector2.Distance((Vector2)p * step, target) < .25f), "entrance connects to beds, reception and exit: " + target);
        Assert(Vector2.Distance(new Vector2(2,-2), PrototypeInnLayout.Keeper) < 1.6f, "keeper is within interaction range from reception");
    }

    private static Transform Person(Transform root, PixelSpriteLibrary people, string name, Vector2 ground, string atlas, int frame)
    {
        var anchor = new GameObject(name); anchor.transform.SetParent(root, false); anchor.transform.localPosition = ground;
        Transform visual = people.Create(anchor.transform, atlas, frame);
        visual.GetComponent<PixelCharacter>().enabled = false;
        visual.localRotation = Quaternion.LookRotation(new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0), PixelSpriteLayout.UprightUp);
        visual.localScale = new Vector3(1,PixelSpriteLayout.UprightScaleY,1);
        return anchor.transform;
    }

    private static void Assert(bool ok, string reason) { if (!ok) throw new Exception("Inn validation: " + reason); }
    private static void Save(RenderTexture target, string path)
    {
        RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous; }
    }
}
