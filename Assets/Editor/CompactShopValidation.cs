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

public static class CompactShopValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Compact Shops")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_COMPACT_SHOP_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/CompactShopPreview"));
        Directory.CreateDirectory(output);
        foreach (var layout in new[] { CompactShopLayout.Items,CompactShopLayout.Materia }) Validate(layout,output);
        CheckDelivery();
        Assert(Resources.Load<Texture2D>("PixelArt/ShopPropsAtlas") == null && Resources.Load<Texture2D>("PixelArt/MateriaShopAtlas") == null
            && Resources.Load<Shader>("MateriaShop") == null,"retired shop assets have been removed");
        File.WriteAllText(Path.Combine(output,"result.txt"),"COMPACT_SHOPS_VALIDATION_OK: two full paintings, smaller rooms, depth, collisions, connected floor, movement, trade, delivery, saves, travel, retired assets and three aspect ratios.");
        Debug.Log("COMPACT_SHOPS_VALIDATION_OK: item and materia shops, navigation, trade, delivery and three display aspects.");
        string previous = Environment.GetEnvironmentVariable("DQ_WEAPON_SHOP_OUTPUT");
        try { Environment.SetEnvironmentVariable("DQ_WEAPON_SHOP_OUTPUT",Path.Combine(output,"weapon-regression")); WeaponShopValidation.Run(); }
        finally { Environment.SetEnvironmentVariable("DQ_WEAPON_SHOP_OUTPUT",previous); }
    }
    private static void Validate(CompactShopLayout layout,string output)
    {
        var scene = EditorSceneManager.NewPreviewScene(); PixelSpriteLibrary people = null;
        try
        {
            var root = new GameObject(layout.Name+" / validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<CompactShopPixelScenery>().Build(layout);
            var painting = Resources.Load<Texture2D>("PixelArt/"+layout.Resource);
            Assert(painting != null && painting.width == 1448 && painting.height == 1086,"original 4:3 artwork: "+layout.MapId);
            Assert(painting.filterMode == FilterMode.Point && painting.mipmapCount == 1,"sharp pixels: "+layout.MapId);
            float relativeArea = CompactShopLayout.CameraSize*CompactShopLayout.CameraSize*CompactShopLayout.ArtAspect
                /(WeaponShopLayout.CameraSize*WeaponShopLayout.CameraSize*WeaponShopLayout.ArtAspect);
            Assert(relativeArea < .4f,"shop occupies less than 40 percent of weapon shop area");
            Assert(root.GetComponentsInChildren<SpriteRenderer>().Length == 0 && root.GetComponentsInChildren<MeshRenderer>().Length == 2,"room is one full painting with counter depth");
            Assert(root.GetComponentsInChildren<PolygonCollider2D>().Length == 1,"painted counter has a physical footprint");
            foreach (Vector2[] outline in layout.Foregrounds)
                Assert(PaintedInteriorScenery.Triangulate(outline).Length == (outline.Length-2)*3,"counter contour triangulates");
            CheckNavigation(layout); CheckTravel(layout); CheckTrade(layout);
            var cameraObject = new GameObject(layout.Name+" / camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var follow = cameraObject.AddComponent<CameraFollow>(); follow.SetArtworkFrame(true,true,CompactShopLayout.ArtAspect);
            people = new PixelSpriteLibrary();
            Transform hero = Person(root.transform,people,"Heroi",layout.Spawn,"ArenWalk",1);
            Person(root.transform,people,"Vendedor",layout.Merchant,"NpcRoster",layout == CompactShopLayout.Items ? 4 : 6);
            var service = new GameObject("Atendimento no balcao"); service.transform.SetParent(root.transform,false);
            service.transform.localPosition = layout.Counter;
            var interaction = service.AddComponent<MerchantInteraction>(); interaction.Initialize(layout.MapId,layout.Name);
            string opened = null;
            interaction.Interact(new InteractionContext(null,new ExplorationProgress(),_=>{},openShop:id=>opened=id));
            Assert(opened == layout.MapId,"counter opens original shop");
            foreach (Vector2Int resolution in new[] { new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720) })
            {
                var target = new RenderTexture(resolution.x,resolution.y,24);
                try
                {
                    camera.targetTexture = target; camera.aspect = (float)resolution.x/resolution.y;
                    follow.Initialize(hero,layout.Bounds,CompactShopLayout.CameraSize,true);
                    foreach (Vector2 corner in new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right })
                    {
                        Vector3 viewport = camera.WorldToViewportPoint(layout.Ground(corner.x,corner.y));
                        Assert(viewport.x >= -.001f && viewport.x <= 1.001f && viewport.y >= -.001f && viewport.y <= 1.001f,"whole compact painting fits display");
                    }
                    camera.Render(); camera.Render(); Save(target,Path.Combine(output,layout.MapId+"-"+resolution.x+"x"+resolution.y+".png"));
                    if (resolution.x == 1280 && resolution.y == 720)
                    {
                        foreach (var place in new[] { ("counter",layout.Counter),("left",layout.Ground(.28f,.68f)),("right",layout.Ground(.75f,.70f)) })
                        {
                            Assert(layout.IsWalkable(place.Item2),"preview location is walkable: "+layout.MapId+" "+place.Item1);
                            hero.localPosition = place.Item2; camera.Render(); Save(target,Path.Combine(output,layout.MapId+"-"+place.Item1+".png"));
                        }
                        hero.localPosition = layout.Spawn;
                    }
                }
                finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
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
    private static void CheckNavigation(CompactShopLayout layout)
    {
        Assert(layout.IsWalkable(layout.Spawn) && layout.IsWalkable(layout.Exit),"entrance and exit are clear: "+layout.MapId);
        Assert(layout.SafeSavedPosition(layout.Ground(.50f,.48f)) == layout.Spawn,"saved position inside counter recovers at entrance");
        Assert(layout.SafeSavedPosition(layout.Spawn) == layout.Spawn,"valid saved position is retained");
        Assert(!layout.IsWalkable(layout.Ground(.1f,.3f)) && !layout.IsWalkable(layout.Ground(.50f,.48f)),"shelves and counter block movement");
        var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
        Vector2 startUV = layout.ImagePoint(layout.Spawn);
        var start = new Vector2Int(Mathf.RoundToInt(startUV.x*200),Mathf.RoundToInt(startUV.y*200)); reached.Add(start); queue.Enqueue(start);
        var directions = new[] { Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int delta in directions)
            {
                Vector2Int n = p+delta;
                if (reached.Contains(n) || !layout.IsWalkable(layout.Ground(n.x/200f,n.y/200f))) continue;
                reached.Add(n); queue.Enqueue(n);
            }
        }
        foreach (Vector2 target in new[] { layout.Exit,layout.Counter,layout.Ground(.28f,.68f),layout.Ground(.75f,.70f) })
        {
            Vector2 uv = layout.ImagePoint(target);
            Assert(reached.Any(p=>Vector2.Distance(new Vector2(p.x/200f,p.y/200f),uv)<.009f),"connected route: "+layout.MapId+" "+uv);
        }
        foreach (Vector2 uv in new[] { new Vector2(.50f,.65f),new Vector2(.30f,.65f),new Vector2(.70f,.68f) })
            foreach (Vector2 delta in new[] { new Vector2(8,0),new Vector2(-8,0),new Vector2(0,8),new Vector2(0,-8) })
                Assert(layout.IsWalkable(layout.ConstrainMove(layout.Ground(uv.x,uv.y),delta)),"large movement stays on floor");
    }
    private static void CheckTravel(CompactShopLayout layout)
    {
        var travel = new PrototypeTravel(); Vector2 arrival = default;
        Assert(travel.TryTravel(PrototypeTravel.City,out _) && travel.TryTravel(layout.MapId,out arrival) && arrival == layout.Spawn,"city door leads to compact shop entrance");
        Assert(!travel.TryEnter(arrival,false,out _),"arrival does not immediately exit");
        Assert(travel.TryRestore(layout.MapId,new Vector2(0,-1.89f)),"legacy shop save still loads");
        Assert(travel.TryEnter(layout.Exit,false,out _) && travel.Current.Id == PrototypeTravel.City,"painted threshold returns to city");
    }
    private static void CheckTrade(CompactShopLayout layout)
    {
        var service = PrototypeShopFactory.Create(PrototypePartyFactory.Create(),new ExplorationProgress(1000));
        var shop = service.FindShop(layout.MapId); bool items = layout == CompactShopLayout.Items;
        var product = shop.Products.First(p=>p.Id==(items ? "potion" : "fire")); int price=items ? 10 : 50;
        int stock = product.Stock;
        Assert(service.TryBuy(shop,product,1,out _) && service.Progress.Gold == 1000-price && product.Stock == stock-1,"purchase charges gold and grants product");
        var sale = service.SaleEntries(shop).First(e=>e.CanSell && (items ? e.Item == PrototypePartyFactory.Potion
            : e.Materia != null && e.Materia.Definition == PrototypePartyFactory.FireMateria));
        Assert(service.TrySell(shop,sale,1,out _) && service.Progress.Gold == 1000-price/2,"sale returns half of price");
    }
    private static void CheckDelivery()
    {
        var service = PrototypeShopFactory.Create(PrototypePartyFactory.Create(),new ExplorationProgress(1000));
        var quest = new DeliveryQuest(service);
        Assert(quest.TryInteract(service.FindShop("item-shop"),out _) && quest.Stage == DeliveryStage.CarryingPackage,"item vendor starts delivery");
        Assert(quest.TryInteract(service.FindShop("materia-shop"),out _) && quest.Stage == DeliveryStage.Delivered,"materia vendor receives delivery");
        Assert(quest.TryInteract(service.FindShop("item-shop"),out _) && quest.Stage == DeliveryStage.Completed && service.Progress.Gold == 1030,"return trip rewards and completes delivery");
    }
    private static Transform Person(Transform root,PixelSpriteLibrary people,string name,Vector2 ground,string atlas,int frame)
    {
        var anchor = new GameObject(name); anchor.transform.SetParent(root,false); anchor.transform.localPosition = ground;
        Transform visual = people.Create(anchor.transform,atlas,frame); visual.GetComponent<PixelCharacter>().enabled = false;
        visual.localRotation = Quaternion.LookRotation(new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0),PixelSpriteLayout.UprightUp);
        visual.localScale = new Vector3(1,PixelSpriteLayout.UprightScaleY,1); return anchor.transform;
    }
    private static void Assert(bool ok,string reason) { if (!ok) throw new Exception("Compact shop validation: "+reason); }
    private static void Save(RenderTexture target,string path)
    {
        RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous; }
    }
}
