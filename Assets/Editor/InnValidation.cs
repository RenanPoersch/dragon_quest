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
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/InnPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene(); PixelSpriteLibrary people = null;
        try
        {
            var root = new GameObject("Pousada / validacao da pintura completa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<InnPixelScenery>().Build();
            Texture2D painting = Resources.Load<Texture2D>("PixelArt/InnInterior");
            Assert(painting != null && painting.width == 1447 && painting.height == 1087,"complete panorama is available");
            Assert(painting.filterMode == FilterMode.Point && painting.mipmapCount == 1,"painting keeps sharp pixels");
            Assert(root.GetComponentsInChildren<SpriteRenderer>().Length == 0,"room is a single painting without the old prop sprites");
            Assert(root.GetComponentsInChildren<MeshRenderer>().Length == 2,"painting includes registered depth contours");
            Assert(root.GetComponentsInChildren<PolygonCollider2D>().Length == 3,"two beds and counter footprints use the painted plan");
            foreach (Vector2[] outline in InnPixelScenery.Foregrounds)
                Assert(InnPixelScenery.Triangulate(outline).Length == (outline.Length-2)*3,"concave occlusion outlines triangulate completely");
            Assert(PrototypeInnLayout.CameraSize == CompactShopLayout.CameraSize && PrototypeInnLayout.ArtAspect == CompactShopLayout.ArtAspect,"inn matches compact shop dimensions");
            CheckNavigation();
            CheckTravel();

            var cameraObject = new GameObject("Pousada / camera de validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var follow = cameraObject.AddComponent<CameraFollow>();
            follow.SetArtworkFrame(true,true,PrototypeInnLayout.ArtAspect);
            people = new PixelSpriteLibrary();
            Transform hero = Person(root.transform,people,"Heroi",PrototypeInnLayout.Spawn,"ArenWalk",1);
            Person(root.transform,people,"Hospedeira",PrototypeInnLayout.Keeper,"NpcRoster",7);
            var service = new GameObject("Atendimento na frente do balcao"); service.transform.SetParent(root.transform,false);
            service.transform.localPosition = PrototypeInnLayout.Reception;
            var interaction = service.AddComponent<InnInteraction>(); int saves = 0; interaction.Initialize(() => saves++);
            var dialogue = root.AddComponent<DialogueController>();
            interaction.Interact(new InteractionContext(dialogue,new ExplorationProgress(),_=>{}));
            Assert(dialogue.HasChoices,"keeper opens save conversation from the counter");
            dialogue.Choose(0); Assert(saves == 1,"keeper save callback remains functional");

            foreach (Vector2Int resolution in new[] { new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720) })
            {
                var target = new RenderTexture(resolution.x,resolution.y,24);
                try
                {
                    camera.targetTexture = target; camera.aspect = (float)resolution.x/resolution.y;
                    follow.Initialize(hero,PrototypeInnLayout.Bounds,PrototypeInnLayout.CameraSize,true);
                    foreach (Vector2 corner in new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right })
                    {
                        Vector3 viewport = camera.WorldToViewportPoint(PrototypeInnLayout.Ground(corner.x,corner.y));
                        Assert(viewport.x >= -.001f && viewport.x <= 1.001f && viewport.y >= -.001f && viewport.y <= 1.001f,"complete image fits without cropping");
                    }
                    camera.Render(); camera.Render(); Save(target,Path.Combine(output,"inn-"+resolution.x+"x"+resolution.y+".png"));
                    if (resolution.y == 720 && resolution.x == 1280)
                    {
                        foreach (var place in new[] { ("reception",PrototypeInnLayout.Reception),("beds",PrototypeInnLayout.Ground(.38f,.55f)),("hearth",PrototypeInnLayout.Ground(.27f,.62f)) })
                        {
                            Assert(PrototypeInnLayout.IsWalkable(place.Item2),"preview position is reachable: "+place.Item1);
                            hero.localPosition = place.Item2; camera.Render(); Save(target,Path.Combine(output,"inn-"+place.Item1+".png"));
                        }
                        hero.localPosition = PrototypeInnLayout.Spawn;
                    }
                }
                finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"INN_VALIDATION_OK: full painting, depth contours, navigation, movement limits, saves, travel and complete framing at 16:9, 16:10 and 4:3.");
            Debug.Log("INN_VALIDATION_OK: complete painted inn, navigation, depth, save interaction and three display aspects.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (people != null)
            {
                foreach (string atlas in new[] { "ArenWalk","NpcRoster" }) foreach (Sprite sprite in people.Atlas(atlas).Sprites) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(people.Material);
            }
        }
    }

    private static void CheckNavigation()
    {
        Assert(PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Spawn) && PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Exit),"entrance and exit are clear");
        Assert(PrototypeInnLayout.SafeSavedPosition(PrototypeInnLayout.Ground(.75f,.43f)) == PrototypeInnLayout.Spawn,"saves inside the new counter return to entrance");
        Assert(PrototypeInnLayout.SafeSavedPosition(PrototypeInnLayout.Spawn) == PrototypeInnLayout.Spawn,"valid saved positions stay intact");
        Assert(!PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Ground(.15f,.2f)) && !PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Ground(.60f,.24f)),"walls and beds cannot be walked through");
        var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(100,150); reached.Add(start); queue.Enqueue(start);
        var directions = new[] { Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int delta in directions)
            {
                Vector2Int n = p + delta;
                if (reached.Contains(n) || !PrototypeInnLayout.IsWalkable(PrototypeInnLayout.Ground(n.x/200f,n.y/200f))) continue;
                reached.Add(n); queue.Enqueue(n);
            }
        }
        foreach (Vector2 target in new[] { PrototypeInnLayout.Exit,PrototypeInnLayout.Reception,PrototypeInnLayout.Ground(.38f,.55f),
            PrototypeInnLayout.Ground(.27f,.62f),PrototypeInnLayout.Ground(.78f,.70f) })
        {
            Vector2 uv = PrototypeInnLayout.ImagePoint(target);
            Assert(reached.Any(p=>Vector2.Distance(new Vector2(p.x/200f,p.y/200f),uv)<.009f),"connected walking route: "+uv);
        }
        foreach (Vector2 startUV in new[] { new Vector2(.50f,.70f),new Vector2(.30f,.65f),new Vector2(.73f,.61f) })
        {
            Vector2 p = PrototypeInnLayout.Ground(startUV.x,startUV.y);
            foreach (Vector2 delta in new[] { new Vector2(8,0),new Vector2(-8,0),new Vector2(0,8),new Vector2(0,-8) })
                Assert(PrototypeInnLayout.IsWalkable(PrototypeInnLayout.ConstrainMove(p,delta)),"large movement remains on accessible floor");
        }
    }

    private static void CheckTravel()
    {
        var travel = new PrototypeTravel();
        Vector2 arrival = default;
        Assert(travel.TryTravel(PrototypeTravel.City,out _) && travel.TryTravel(PrototypeTravel.Inn,out arrival) && arrival == PrototypeInnLayout.Spawn,"city door reaches painted inn entrance");
        Assert(!travel.TryEnter(arrival,false,out _),"arrival does not immediately exit");
        Assert(!PrototypeInnLayout.Bounds.Contains(new Vector2(9,0)) && travel.TryRestore(PrototypeTravel.Inn,new Vector2(9,0)) && PrototypeInnLayout.SafeSavedPosition(new Vector2(9,0)) == PrototypeInnLayout.Spawn,"saves from the former wide inn recover at compact entrance");
        Assert(travel.TryEnter(PrototypeInnLayout.Exit,false,out _) && travel.Current.Id == PrototypeTravel.City,"painted threshold returns to city");
    }
    private static Transform Person(Transform root,PixelSpriteLibrary people,string name,Vector2 ground,string atlas,int frame)
    {
        var anchor = new GameObject(name); anchor.transform.SetParent(root,false); anchor.transform.localPosition = ground;
        Transform visual = people.Create(anchor.transform,atlas,frame); visual.GetComponent<PixelCharacter>().enabled = false;
        visual.localRotation = Quaternion.LookRotation(new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0),PixelSpriteLayout.UprightUp);
        visual.localScale = new Vector3(1,PixelSpriteLayout.UprightScaleY,1); return anchor.transform;
    }
    private static void Assert(bool ok,string reason) { if (!ok) throw new Exception("Inn validation: "+reason); }
    private static void Save(RenderTexture target,string path)
    {
        RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous; }
    }
}
