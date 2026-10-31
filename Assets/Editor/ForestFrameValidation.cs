using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World;
using DragonQuest.Exploration.Layouts;
using System;
using System.IO;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class ForestFrameValidation
{
    public static void Run()
    {
        string output=Environment.GetEnvironmentVariable("DQ_FOREST_FRAME_OUTPUT");
        if(string.IsNullOrEmpty(output))throw new Exception("Missing output folder");
        Directory.CreateDirectory(output);
        var scene=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("Forest frame preview");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        root.AddComponent<ForestPixelScenery>().Build();
        var cameraObject=new GameObject("Forest preview camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var follow=cameraObject.AddComponent<CameraFollow>();follow.SetArtworkFrame(true);
        foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(960,720)})
        {
            var target=new RenderTexture(resolution.x,resolution.y,24);
            camera.targetTexture=target;
            follow.Initialize(root.transform,WorldLayout.ForestBounds,8,true);
            float aspect=(float)resolution.x/resolution.y;
            float halfWidth=camera.orthographicSize*aspect;
            if(camera.orthographicSize>8.001f || halfWidth>8f*16f/9f+.001f)
                throw new Exception("Artwork does not cover the viewport");
            if(camera.rect!=new Rect(0,0,1,1) || Mathf.Abs(camera.aspect-aspect)>.001f)
                throw new Exception("Viewport must fill the screen without distortion");
            var previous=RenderTexture.active;RenderTexture.active=target;GL.Clear(true,true,Color.black);
            camera.Render();
            var pixels=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(output,"forest-"+resolution.x+"x"+resolution.y+".png"),pixels.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);
        }
        EditorSceneManager.ClosePreviewScene(scene);
        Debug.Log("FOREST_FRAME_VALIDATION_OK: full-screen proportional cover at 16:9, 16:10 and 4:3.");
    }
}
