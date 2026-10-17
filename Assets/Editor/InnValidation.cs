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
            var saveObject=new GameObject("Cristal de salvamento / validacao");saveObject.transform.SetParent(root.transform,false);
            saveObject.transform.localPosition=PrototypeInnLayout.SavePoint;
            var savePoint=saveObject.AddComponent<InnSavePoint>();int menuOpens=0;
            savePoint.Initialize(hero.gameObject.AddComponent<PlayerMovement>(),()=>false,()=>menuOpens++);
            Assert(savePoint.Source.filterMode==FilterMode.Point&&savePoint.Source.mipmapCount==1&&savePoint.Frames.Count==8,"save crystal has eight sharp animation frames");
            Assert(saveObject.GetComponents<Collider2D>().Length==0&&PrototypeInnLayout.IsWalkable(PrototypeInnLayout.SavePoint),"save crystal does not block the floor");
            Assert(Vector2.Distance(PrototypeInnLayout.SavePoint,PrototypeInnLayout.SaveApproach)>InnSavePoint.ActivationRadius,"approaching the crystal does not open its menu");
            var pixels=savePoint.Source.GetPixels32();var signatures=new HashSet<uint>();float spriteScale=savePoint.Frames[0].pixelsPerUnit;
            for(int i=0;i<InnSavePoint.FrameCount;i++)
            {
                savePoint.ShowAt((i+.25f)/InnSavePoint.FramesPerSecond);
                Assert(savePoint.CurrentFrame==i&&savePoint.Frames[i].pixelsPerUnit==spriteScale,"animation advances with one stable pixel scale");
                Assert(savePoint.Frames[i].pivot.x>0&&savePoint.Frames[i].pivot.x<savePoint.Frames[i].rect.width
                    &&savePoint.Frames[i].pivot.y>0&&savePoint.Frames[i].pivot.y<savePoint.Frames[i].rect.height,"save crystal has registered floor anchor");
                signatures.Add(FrameSignature(savePoint.Frames[i],pixels,savePoint.Source.width));
            }
            Assert(signatures.Count==8&&saves==1&&menuOpens==0,"all eight frames are distinct and animation never opens the menu or saves");
            savePoint.ShowAt(InnSavePoint.FrameCount/InnSavePoint.FramesPerSecond);Assert(savePoint.CurrentFrame==0,"animation wraps to its first frame");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SaveApproach,false);
            Assert(menuOpens==0,"walking nearby does not open the save menu");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==1&&saves==1,"stepping onto the circle opens the menu without saving");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,true);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==1,"remaining on the circle after saving or cancelling does not reopen the menu");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SaveApproach,false);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==2,"leaving and stepping onto the circle opens the menu again");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SaveApproach,false);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,true);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==2,"dialogue, inventory, combat, menus and lost focus prevent activation");
            savePoint.enabled=false;
            savePoint.CheckPlayerStep(PrototypeInnLayout.SaveApproach,false);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==2,"inactive save points cannot open menus");
            savePoint.enabled=true;
            var movement=hero.GetComponent<PlayerMovement>();InvokePrivate(movement,"Awake");InvokePrivate(savePoint,"LateUpdate");
            movement.Teleport(PrototypeInnLayout.SavePoint);InvokePrivate(savePoint,"LateUpdate");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==2,"loading or teleporting onto the crystal does not reopen the menu");
            savePoint.CheckPlayerStep(PrototypeInnLayout.SaveApproach,false);
            savePoint.CheckPlayerStep(PrototypeInnLayout.SavePoint,false);
            Assert(menuOpens==3,"walking back after loading can open the menu");
            hero.localPosition=PrototypeInnLayout.Spawn;
            CheckSaveSelection(root,movement,dialogue,output);
            savePoint.ShowAt(0);

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
                        hero.localPosition = PrototypeInnLayout.SaveApproach;
                        for(int i=0;i<InnSavePoint.FrameCount;i++)
                        {
                            savePoint.ShowAt((i+.25f)/InnSavePoint.FramesPerSecond);camera.Render();
                            Save(target,Path.Combine(output,"inn-save-frame-"+i.ToString("D2")+".png"));
                        }
                        savePoint.ShowAt(0);hero.localPosition = PrototypeInnLayout.Spawn;
                    }
                }
                finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"INN_VALIDATION_OK: full painting, depth contours, navigation, movement limits, animated save crystal, step-on save menu without automatic writes, travel and complete framing at 16:9, 16:10 and 4:3.");
            Debug.Log("INN_VALIDATION_OK: complete painted inn, eight-frame save crystal, navigation, depth, step-on save menu and three display aspects.");
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

    private static void CheckSaveSelection(GameObject root,PlayerMovement hero,DialogueController dialogue,string output)
    {
        var controller=root.AddComponent<PrototypeSaveController>();
        var combat=root.AddComponent<PrototypeBattleController>();
        var maps=root.AddComponent<PrototypeLocations>();
        var travel=new PrototypeTravel();travel.TryTravel(PrototypeTravel.City,out _);travel.TryTravel(PrototypeTravel.Inn,out _);
        var store=new DragonQuest.Persistence.SaveStore(Path.Combine(output,"step-on-save-checks"));
        SetPrivate(maps,"navigation",travel);
        SetPrivate(controller,"store",store);SetPrivate(controller,"battle",combat);
        SetPrivate(controller,"locations",maps);SetPrivate(controller,"player",hero);SetPrivate(controller,"dialogue",dialogue);
        SetPrivate(controller,"<ActiveSlot>k__BackingField",2);
        SetPrivate(controller,"<IsMenuVisible>k__BackingField",false);
        controller.OpenInnSaveMenu();
        Assert(controller.IsMenuVisible && controller.ActiveSlot==2,"crystal opens slot selection even when a current slot is assigned");
        Assert(!store.Exists(1)&&!store.Exists(2)&&!store.Exists(3),"opening slot selection never writes a save");
        InvokePrivate(controller,"CloseSaveMenu");
        dialogue.Open("Hospedeira","Conversa em andamento.");controller.OpenInnSaveMenu();
        Assert(!controller.IsMenuVisible,"dialogue blocks the save selection");dialogue.Close();
        travel.TryTravel(PrototypeTravel.City,out _);controller.OpenInnSaveMenu();
        Assert(!controller.IsMenuVisible,"save crystal menu is restricted to the inn");
    }
    private static void InvokePrivate(object target,string method)
        => target.GetType().GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(target,null);
    private static void SetPrivate(object target,string field,object value)
        => target.GetType().GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(target,value);

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
            PrototypeInnLayout.Ground(.27f,.62f),PrototypeInnLayout.Ground(.78f,.70f),PrototypeInnLayout.SavePoint,PrototypeInnLayout.SaveApproach })
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
    private static uint FrameSignature(Sprite sprite,Color32[] pixels,int stride)
    {
        Rect rect=sprite.rect;uint hash=2166136261;
        unchecked
        {
            for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
            {
                Color32 p=pixels[y*stride+x];
                uint value=p.a<128?0u:(uint)(p.r|(p.g<<8)|(p.b<<16));hash=(hash^value)*16777619;
            }
        }
        return hash;
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
