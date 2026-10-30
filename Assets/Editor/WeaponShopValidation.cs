using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeaponShopValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Weapon Shop")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_WEAPON_SHOP_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/WeaponShopPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene(); PixelSpriteLibrary people = null;
        try
        {
            var root = new GameObject("Loja de armas / validacao da pintura completa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<WeaponShopPixelScenery>().Build();
            var painting = Resources.Load<Texture2D>("PixelArt/WeaponShopInterior");
            Assert(painting != null && painting.width == 1447 && painting.height == 1087,"complete weapon shop artwork is available");
            Assert(painting.filterMode == FilterMode.Point && painting.mipmapCount == 1,"painting keeps sharp pixels");
            Assert(root.GetComponentsInChildren<SpriteRenderer>().Length == 0,"room uses the complete painting");
            Assert(root.GetComponentsInChildren<MeshRenderer>().Length == 4,"painting and three depth contours are present");
            Assert(root.GetComponentsInChildren<PolygonCollider2D>().Length == 3,"counter, anvil and chest footprints match the painting");
            foreach (Vector2[] outline in WeaponShopPixelScenery.Foregrounds)
                Assert(PaintedInteriorScenery.Triangulate(outline).Length == (outline.Length-2)*3,"all depth contours triangulate");
            Assert(WeaponShopLayout.CameraSize == CompactShopLayout.CameraSize && WeaponShopLayout.ArtAspect == CompactShopLayout.ArtAspect,"weapon shop matches compact shop dimensions");
            CheckNavigation(); CheckTravel(); CheckTrade();

            var cameraObject = new GameObject("Loja de armas / camera de validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var follow = cameraObject.AddComponent<CameraFollow>(); follow.SetArtworkFrame(true,true,WeaponShopLayout.ArtAspect);
            people = new PixelSpriteLibrary();
            Transform hero = Person(root.transform,people,"Heroi",WeaponShopLayout.Spawn,"ArenWalk",1);
            Person(root.transform,people,"Armeiro",WeaponShopLayout.Merchant,"NpcRoster",5);
            var service = new GameObject("Balcao de armas"); service.transform.SetParent(root.transform,false);
            service.transform.localPosition = WeaponShopLayout.Counter;
            //var interaction = service.AddComponent<MerchantInteraction>(); interaction.Initialize(WeaponShopLayout.MapId,"Loja de equipamentos");
            string opened = null;
            //interaction.Interact(new InteractionContext(null,new ExplorationProgress(),_=>{},openShop:id=>opened=id));
            Assert(opened == WeaponShopLayout.MapId,"counter opens the existing equipment shop");

            foreach (Vector2Int resolution in new[] { new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720) })
            {
                var target = new RenderTexture(resolution.x,resolution.y,24);
                try
                {
                    camera.targetTexture = target; camera.aspect = (float)resolution.x/resolution.y;
                    follow.Initialize(hero,WeaponShopLayout.Bounds,WeaponShopLayout.CameraSize,true);
                    foreach (Vector2 corner in new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right })
                    {
                        Vector3 viewport = camera.WorldToViewportPoint(WeaponShopLayout.Ground(corner.x,corner.y));
                        Assert(viewport.x >= -.001f && viewport.x <= 1.001f && viewport.y >= -.001f && viewport.y <= 1.001f,"entire painting fits display");
                    }
                    camera.Render(); camera.Render(); Save(target,Path.Combine(output,"weapon-shop-"+resolution.x+"x"+resolution.y+".png"));
                    if (resolution.x == 1280 && resolution.y == 720)
                    {
                        foreach (var place in new[] { ("counter",WeaponShopLayout.Counter),("forge",WeaponShopLayout.Ground(.29f,.59f)),("displays",WeaponShopLayout.Ground(.79f,.68f)) })
                        {
                            Assert(WeaponShopLayout.IsWalkable(place.Item2),"preview position is walkable: "+place.Item1);
                            hero.localPosition = place.Item2; camera.Render(); Save(target,Path.Combine(output,"weapon-shop-"+place.Item1+".png"));
                        }
                        hero.localPosition = WeaponShopLayout.Spawn;
                    }
                }
                finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"WEAPON_SHOP_VALIDATION_OK: full painting, depth, collisions, navigation, movement limits, merchant, purchases, sales, saves and three aspect ratios.");
            Debug.Log("WEAPON_SHOP_VALIDATION_OK: complete room, navigation, trade and three display aspects.");
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
        string previous = Environment.GetEnvironmentVariable("DQ_INN_OUTPUT");
        try { Environment.SetEnvironmentVariable("DQ_INN_OUTPUT",Path.Combine(output,"inn-regression")); InnValidation.Run(); }
        finally { Environment.SetEnvironmentVariable("DQ_INN_OUTPUT",previous); }
    }

    private static void CheckNavigation()
    {
        Assert(WeaponShopLayout.IsWalkable(WeaponShopLayout.Spawn) && WeaponShopLayout.IsWalkable(WeaponShopLayout.Exit),"entrance and exit are clear");
        Assert(WeaponShopLayout.SafeSavedPosition(WeaponShopLayout.Ground(.56f,.47f)) == WeaponShopLayout.Spawn,"old saves inside the counter recover at entrance");
        Assert(WeaponShopLayout.SafeSavedPosition(WeaponShopLayout.Spawn) == WeaponShopLayout.Spawn,"valid save position is retained");
        Assert(!WeaponShopLayout.IsWalkable(WeaponShopLayout.Ground(.19f,.46f)) && !WeaponShopLayout.IsWalkable(WeaponShopLayout.Ground(.87f,.50f)),"anvil and weapon racks block movement");
        var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(100,150); reached.Add(start); queue.Enqueue(start);
        var directions = new[] { Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int delta in directions)
            {
                Vector2Int n = p + delta;
                if (reached.Contains(n) || !WeaponShopLayout.IsWalkable(WeaponShopLayout.Ground(n.x/200f,n.y/200f))) continue;
                reached.Add(n); queue.Enqueue(n);
            }
        }
        foreach (Vector2 target in new[] { WeaponShopLayout.Exit,WeaponShopLayout.Counter,WeaponShopLayout.Ground(.29f,.59f),WeaponShopLayout.Ground(.30f,.70f),WeaponShopLayout.Ground(.79f,.68f) })
        {
            Vector2 uv = WeaponShopLayout.ImagePoint(target);
            Assert(reached.Any(p=>Vector2.Distance(new Vector2(p.x/200f,p.y/200f),uv)<.009f),"entrance has connected route to "+uv);
        }
        foreach (Vector2 uv in new[] { new Vector2(.50f,.70f),new Vector2(.30f,.60f),new Vector2(.70f,.65f) })
            foreach (Vector2 delta in new[] { new Vector2(8,0),new Vector2(-8,0),new Vector2(0,8),new Vector2(0,-8) })
                Assert(WeaponShopLayout.IsWalkable(WeaponShopLayout.ConstrainMove(WeaponShopLayout.Ground(uv.x,uv.y),delta)),"large movement stays on walkable floor");
    }
    private static void CheckTravel()
    {
        var travel = new PrototypeTravel(); Vector2 arrival = default;
        Assert(travel.TryTravel(PrototypeTravel.City,out _) && travel.TryTravel(WeaponShopLayout.MapId,out arrival) && arrival == WeaponShopLayout.Spawn,"city door leads to new weapon shop entrance");
        Assert(!travel.TryEnter(arrival,false,out _),"arrival does not immediately exit");
        Assert(travel.TryRestore(WeaponShopLayout.MapId,new Vector2(0,-1.89f)),"legacy shop saves still load");
        Assert(!WeaponShopLayout.Bounds.Contains(new Vector2(9,0)) && travel.TryRestore(WeaponShopLayout.MapId,new Vector2(9,0)) && WeaponShopLayout.SafeSavedPosition(new Vector2(9,0)) == WeaponShopLayout.Spawn,"saves from the former wide painting recover at compact entrance");
        Assert(travel.TryEnter(WeaponShopLayout.Exit,false,out _) && travel.Current.Id == PrototypeTravel.City,"painted threshold returns to city");
    }
    private static void CheckTrade()
    {
        var service = PrototypeShopFactory.Create(PrototypePartyFactory.Create(),new ExplorationProgress(1000));
        var shop = service.FindShop(WeaponShopLayout.MapId); var sword = shop.Products.First(p=>p.Id=="iron-sword");
        Assert(service.TryBuy(shop,sword,1,out _) && service.Progress.Gold == 940 && sword.Stock == 1,"existing purchase charges gold, updates stock and grants weapon");
        var sale = service.SaleEntries(shop).First(e=>e.Equipment != null && e.Equipment.Definition == PrototypePartyFactory.IronSword);
        Assert(service.TrySell(shop,sale,1,out _) && service.Progress.Gold == 970,"existing weapon sale remains functional");
    }
    private static Transform Person(Transform root,PixelSpriteLibrary people,string name,Vector2 ground,string atlas,int frame)
    {
        var anchor = new GameObject(name); anchor.transform.SetParent(root,false); anchor.transform.localPosition = ground;
        Transform visual = people.Create(anchor.transform,atlas,frame); visual.GetComponent<PixelCharacter>().enabled = false;
        visual.localRotation = Quaternion.LookRotation(new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0),PixelSpriteLayout.UprightUp);
        visual.localScale = new Vector3(1,PixelSpriteLayout.UprightScaleY,1); return anchor.transform;
    }
    private static void Assert(bool ok,string reason) { if (!ok) throw new Exception("Weapon shop validation: "+reason); }
    private static void Save(RenderTexture target,string path)
    {
        RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous; }
    }
}
