using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DragonQuest.Prototype;
using DragonQuest.Exploration;

public static class RuinsSideValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Ruins")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_RUINS_OUTPUT");
        if (string.IsNullOrEmpty(output)) output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/RuinsPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Ruins validation");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        var scenery = root.AddComponent<RuinsSideScenery>();
        var altar = scenery.Build(out GameObject flame);
        Assert(HasChild(altar,"Livro de pedra / inscricoes antigas"), "carved stone book on altar");
        Assert(HasChild(root.transform,"Panorama desenhado / mar e montanhas"), "painted pixel art panorama");
        Assert(Mathf.Abs(altar.localScale.x-.65f)<.001f,"smaller altar");
        Assert(Mathf.Abs(altar.position.x - PrototypeRuinsLayout.AltarX) < .001f, "altar position");
        Assert(PrototypeRuinsLayout.FloorAt(-25) == 0, "flat approach");
        Assert(Mathf.Abs(PrototypeRuinsLayout.FloorAt(-29) - 1.05f) < .001f, "three steps");
        float previous = 0;
        for (float x=PrototypeRuinsLayout.MaxX;x>=PrototypeRuinsLayout.MinX;x-=.05f)
        {
            float floor=PrototypeRuinsLayout.FloorAt(x);
            Assert(floor >= previous && floor - previous < .045f, "continuous ascending path");
            previous = floor;
        }
        Assert(PrototypeRuinsLayout.SafeSavedPosition(new Vector2(0,4)) == PrototypeRuinsLayout.Spawn, "legacy save recovery");
        var travel = new PrototypeTravel();
        Assert(travel.TryRestore(PrototypeTravel.Ruins,new Vector2(-3,-2.5f)), "legacy save accepted");
        Assert(!travel.Current.FixedCamera, "scrolling camera");
        Assert(travel.TryTravel(PrototypeTravel.World,out Vector2 arrival), "return route");

        var cameraObject = new GameObject("Ruins preview camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
        camera.orthographic=true; camera.orthographicSize=PrototypeRuinsLayout.CameraSize;
        camera.aspect=16f/9; camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.22f,.24f,.25f);
        var hero = new GameObject("Hero preview"); hero.transform.SetParent(root.transform,false);
        var library = new PixelSpriteLibrary();
        var visual = library.Create(hero.transform,"ArenWalk",1);
        visual.GetComponent<PixelCharacter>().enabled=false;
        visual.localRotation=Quaternion.identity;visual.localScale=Vector3.one;
        var target = new RenderTexture(1280,720,24);
        camera.targetTexture=target;
        // Warm up renderer registration before capturing the first preview.
        camera.transform.position=new Vector3(-6,PrototypeRuinsLayout.CameraHeight,-30);
        scenery.UpdateParallax(-6);camera.Render();
        foreach(float x in new[]{-2.5f,-12f,-23f,-30f})
        {
            hero.transform.position=new Vector3(x,PrototypeRuinsLayout.FloorAt(x),0);
            float cameraX=Mathf.Clamp(x,-34,-6);
            camera.transform.position=new Vector3(cameraX,PrototypeRuinsLayout.CameraHeight,-30);
            camera.transform.rotation=Quaternion.identity;
            scenery.UpdateParallax(cameraX);
            camera.Render();
            Save(target,Path.Combine(output,"ruins-"+x.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+".png"));
        }
        // Place the hero directly behind a foreground shaft; the depth buffer must conceal it.
        camera.transform.position=new Vector3(-12,PrototypeRuinsLayout.CameraHeight,-30);
        scenery.UpdateParallax(-12);
        hero.transform.position=new Vector3(-14+12*.18f,0,0);
        camera.Render(); Save(target,Path.Combine(output,"ruins-occlusion.png"));
        flame.SetActive(true);hero.transform.position=new Vector3(-30,1.05f,0);
        camera.transform.position=new Vector3(-30,PrototypeRuinsLayout.CameraHeight,-30);scenery.UpdateParallax(-30);
        camera.Render();Save(target,Path.Combine(output,"ruins-altar-lit.png"));
        flame.SetActive(false);camera.orthographicSize=2.2f;
        camera.transform.position=new Vector3(-31,3.05f,-30);scenery.UpdateParallax(-31);
        camera.Render();Save(target,Path.Combine(output,"ruins-stone-book.png"));
        camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
        library.Dispose();UnityEngine.Object.DestroyImmediate(library.Material);
        EditorSceneManager.ClosePreviewScene(scene);
        Debug.Log("RUINS_SIDE_VALIDATION_OK: path, stairs, legacy saves, travel, sea, mountains, stone book, parallax, screenshots.");
    }
    private static void Assert(bool condition,string message) { if(!condition) throw new Exception("Ruins: "+message); }
    private static bool HasChild(Transform parent,string name)
    { foreach(Transform child in parent) if(child.name==name) return true; return false; }
    private static void Save(RenderTexture target,string path)
    {
        var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=previous;
    }
}
