using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ThroneRoomValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Throne Room")]
    public static void Run()
    {
        string output=Environment.GetEnvironmentVariable("DQ_THRONE_OUTPUT");
        if(string.IsNullOrEmpty(output)) output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/ThroneRoomPreview"));
        Directory.CreateDirectory(output);
        CheckNavigation();
        var scene=EditorSceneManager.NewPreviewScene(); PixelSpriteLibrary people=null;
        try
        {
            var root=new GameObject("Sala do trono / validacao");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var travel=new PrototypeTravel(); var roots=new Dictionary<string,GameObject>();
            foreach(var map in travel.Maps)
            { var obj=new GameObject(map.Id);obj.transform.SetParent(root.transform,false);roots.Add(map.Id,obj); }
            roots[PrototypeTravel.Castle].AddComponent<ThroneRoomPixelScenery>().Build();
            var painting=Resources.Load<Texture2D>("PixelArt/ThroneRoomInterior");
            Assert(painting!=null&&painting.width==1672&&painting.height==941&&painting.filterMode==FilterMode.Point&&painting.mipmapCount==1,"chosen painting is imported without resizing or smoothing");
            var cameraObject=new GameObject("Camera de perspectiva / validacao");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var follow=cameraObject.AddComponent<CameraFollow>();
            people=new PixelSpriteLibrary();
            Transform hero=Person(root.transform,people,"Heroi",ThroneRoomLayout.Spawn,"ArenWalk",3);
            hero.gameObject.AddComponent<Rigidbody2D>();hero.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(.55f,.6f);
            var player=hero.gameObject.AddComponent<PlayerMovement>();player.SendMessage("Awake");
            Transform king=Person(roots[PrototypeTravel.Castle].transform,people,"Rei Rubro",ThroneRoomLayout.King,"TyrantSeated");
            king.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(.55f,.6f);
            king.gameObject.AddComponent<ThroneRoomDepth>().Refresh();
            Transform spirit=Person(roots[PrototypeTravel.Castle].transform,people,"Espirito",ThroneRoomLayout.Spirit,"SpiritBattle");
            spirit.gameObject.AddComponent<ThroneRoomDepth>().Refresh();spirit.gameObject.SetActive(false);
            CheckTyrant(root,king,spirit);
            var locations=root.AddComponent<PrototypeLocations>();locations.Initialize(roots,player,follow,travel,()=>false);
            locations.TravelTo(PrototypeTravel.City);locations.TravelTo(PrototypeTravel.Castle);
            Assert(!camera.orthographic&&player.IsFrontalView&&!player.IsSideView,"entering castle activates perspective camera and frontal controls");
            Assert((Vector2)hero.position==ThroneRoomLayout.Spawn&&!travel.TryEnter(hero.position,false,out _),"arrival is safe and does not trigger return door");
            Assert(Vector2.Distance(ThroneRoomLayout.Audience,ThroneRoomLayout.King)<1.6f,"king interaction is reachable from top stair");
            var report=new List<string>{"position,worldY,elevation,heroHeightPixels"};
            foreach(Vector2Int resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720)})
            {
                var target=new RenderTexture(resolution.x,resolution.y,24);
                try
                {
                    camera.targetTexture=target;follow.Initialize(hero,ThroneRoomLayout.Bounds,9,true);
                    foreach(Vector2 corner in new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right})
                    {
                        Vector3 viewport=camera.WorldToViewportPoint(ThroneRoomLayout.ImagePlane(corner,60));
                        Assert(viewport.x>=-.001f&&viewport.x<=1.001f&&viewport.y>=-.001f&&viewport.y<=1.001f,"whole painting fits aspect ratio");
                    }
                    Move(player,ThroneRoomLayout.Spawn);camera.Render();camera.Render();Save(target,Path.Combine(output,"throne-room-"+resolution.x+"x"+resolution.y+".png"));
                    if(resolution.x!=1280||resolution.y!=720) continue;
                    float previous=float.MaxValue,nearHeight=0,farHeight=0;
                    foreach(var place in new[]{("entrance",ThroneRoomLayout.Spawn),("middle",new Vector2(0,-.5f)),("stairs",new Vector2(0,3)),("audience",ThroneRoomLayout.Audience)})
                    {
                        Assert(ThroneRoomLayout.IsWalkable(place.Item2),"preview floor is walkable: "+place.Item1);
                        Move(player,place.Item2);
                        var renderer=hero.GetComponentInChildren<SpriteRenderer>();
                        Vector3 bottom=renderer.transform.TransformPoint(new Vector3(0,renderer.sprite.bounds.min.y,0));
                        Vector3 top=renderer.transform.TransformPoint(new Vector3(0,renderer.sprite.bounds.max.y,0));
                        float height=(camera.WorldToViewportPoint(top).y-camera.WorldToViewportPoint(bottom).y)*resolution.y;
                        Assert(height<previous&&height>25,"character shrinks monotonically with distance: "+place.Item1);
                        if(nearHeight==0) nearHeight=height;farHeight=height;previous=height;
                        report.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3:F2}",place.Item1,place.Item2.y,ThroneRoomLayout.FloorHeight(place.Item2),height));
                        Assert(Math.Abs(renderer.transform.localScale.x-1)<.0001f&&player.GetComponent<BoxCollider2D>().size==new Vector2(.55f,.6f),"perspective does not resize sprite transform or physical collider");
                        camera.Render();Save(target,Path.Combine(output,"throne-room-"+place.Item1+".png"));
                    }
                    Assert(farHeight<nearHeight*.65f,"distance produces substantial natural size reduction");
                    spirit.gameObject.SetActive(true);spirit.GetComponent<ThroneRoomDepth>().Refresh();camera.Render();Save(target,Path.Combine(output,"throne-room-spirit.png"));spirit.gameObject.SetActive(false);
                }
                finally {camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
            }
            locations.TravelTo(PrototypeTravel.City);
            hero.GetComponent<ThroneRoomDepth>().SendMessage("OnDisable");
            Assert(camera.orthographic&&!player.IsFrontalView&&Math.Abs(hero.GetChild(0).localPosition.z+.075f)<.001f,"leaving restores orthographic camera, controls and original sprite elevation");
            locations.Restore(PrototypeTravel.Castle,new Vector2(8,6));
            Assert((Vector2)hero.position==ThroneRoomLayout.Spawn&&!camera.orthographic,"old save inside blocked castle geometry recovers at entrance");
            File.WriteAllLines(Path.Combine(output,"perspective-scale.csv"),report);
            File.WriteAllText(Path.Combine(output,"result.txt"),"THRONE_ROOM_VALIDATION_OK: selected painting, true perspective, monotonic size reduction, stairs, physical footprint, navigation, king dialogue, battle callback, revealed spirit, travel, saves and three display aspects.");
            Debug.Log("THRONE_ROOM_VALIDATION_OK: perspective, navigation, depth, king, spirit, saves and three display aspects.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if(people!=null)
            {foreach(string name in new[]{"ArenWalk","TyrantSeated","SpiritBattle"})foreach(Sprite sprite in people.Atlas(name).Sprites)UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(people.Material);}
        }
        string previousOutput=Environment.GetEnvironmentVariable("DQ_COMPACT_SHOP_OUTPUT");
        try{Environment.SetEnvironmentVariable("DQ_COMPACT_SHOP_OUTPUT",Path.Combine(output,"shop-regression"));CompactShopValidation.Run();}
        finally{Environment.SetEnvironmentVariable("DQ_COMPACT_SHOP_OUTPUT",previousOutput);}
    }
    private static void CheckNavigation()
    {
        Assert(ThroneRoomLayout.IsWalkable(ThroneRoomLayout.Spawn)&&ThroneRoomLayout.IsWalkable(ThroneRoomLayout.Exit),"spawn and exit are clear");
        Assert(!ThroneRoomLayout.IsWalkable(new Vector2(6,4))&&!ThroneRoomLayout.IsWalkable(ThroneRoomLayout.King),"side of dais and throne block walking");
        var reached=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();
        var start=new Vector2Int(0,-46);reached.Add(start);queue.Enqueue(start);
        while(queue.Count>0)
        {
            var p=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {var n=p+d;if(reached.Contains(n)||!ThroneRoomLayout.IsWalkable(new Vector2(n.x*.1f,n.y*.1f)))continue;reached.Add(n);queue.Enqueue(n);}
        }
        foreach(Vector2 target in new[]{ThroneRoomLayout.Exit,ThroneRoomLayout.Audience,new Vector2(-5,-1),new Vector2(5,-1)})
            Assert(reached.Any(p=>Vector2.Distance(new Vector2(p.x*.1f,p.y*.1f),target)<.11f),"connected walking route: "+target);
        foreach(Vector2 startPoint in new[]{ThroneRoomLayout.Spawn,new Vector2(-3,0),ThroneRoomLayout.Audience})
            foreach(Vector2 delta in new[]{new Vector2(-30,0),new Vector2(30,0),new Vector2(0,-30),new Vector2(0,30)})
                Assert(ThroneRoomLayout.IsWalkable(ThroneRoomLayout.ConstrainMove(startPoint,delta)),"movement cannot cross walls or leave painting");
    }
    private static void CheckTyrant(GameObject root,Transform king,Transform spirit)
    {
        var party=PrototypePartyFactory.Create();int started=0;
        var tyrant=king.gameObject.AddComponent<TyrantInteraction>();tyrant.Initialize(()=>party,()=>started++,spirit.gameObject);
        tyrant.SendMessage("Update");Assert(!spirit.gameObject.activeSelf,"spirit stays hidden without draconic armor");
        var armor=new EquipmentLoadout(DragonProgress.Armor);
        Assert(party.TryAddEquipments(armor)&&party.TryEquipEquipment(party.Members[0],armor,out _),"test party equips draconic armor");
        tyrant.SendMessage("Update");Assert(spirit.gameObject.activeSelf,"draconic armor reveals spirit in new room");
        var dialogue=root.AddComponent<DialogueController>();tyrant.Interact(new InteractionContext(dialogue,new ExplorationProgress(),_=>{}));
        Assert(dialogue.IsOpen&&dialogue.Title=="Rei Rubro","king retains encounter dialogue");
        while(dialogue.IsOpen)dialogue.Advance();Assert(started==1,"dialogue starts existing castle battle exactly once");
        spirit.gameObject.SetActive(false);
    }
    private static Transform Person(Transform root,PixelSpriteLibrary people,string name,Vector2 ground,string atlas,int frame=0)
    {
        var obj=new GameObject(name);obj.transform.SetParent(root,false);obj.transform.localPosition=ground;
        var visual=people.Create(obj.transform,atlas,frame);visual.GetComponent<PixelCharacter>().enabled=false;
        visual.localPosition=Vector3.back*.075f;visual.localRotation=Quaternion.LookRotation(Vector3.up,Vector3.back);
        visual.localScale=new Vector3(1,PixelSpriteLayout.UprightScaleY,1);return obj.transform;
    }
    private static void Move(PlayerMovement player,Vector2 position){player.Teleport(position);player.GetComponent<ThroneRoomDepth>().Refresh();Physics2D.SyncTransforms();}
    private static void Assert(bool ok,string reason){if(!ok)throw new Exception("Throne room validation: "+reason);}
    private static void Save(RenderTexture target,string path)
    {
        RenderTexture previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try{pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
        finally{UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=previous;}
    }
}
