using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuarryRuneValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Animated Quarry Runes")]
    public static void Run()
    {
        QuarryValidation.Run();
        string output = Environment.GetEnvironmentVariable("DQ_QUARRY_OUTPUT");
        if (string.IsNullOrEmpty(output)) output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/QuarryPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Animated rune validation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var progress = new AncientSecrets(); var targets = new List<Interactable>();
            root.AddComponent<QuarryPixelScenery>().Build(progress,targets);
            QuarryRuneAnimation[] runes = root.GetComponentsInChildren<QuarryRuneAnimation>(true);
            Check(runes.Length==3,"three independent rune animations");
            var faces = new Dictionary<QuarryRuneAnimation,Vector3>();
            foreach (QuarryRuneAnimation rune in runes)
            {
                Check(!rune.gameObject.activeSelf && rune.CurrentFrame==0,"dormant stones remain static");
                faces.Add(rune,rune.transform.localPosition);
                Check(!rune.IsFloating && rune.transform.localScale==Vector3.one,"unsolved runes remain in the stone");
                Check(rune.Source.filterMode==FilterMode.Point && rune.Source.mipmapCount==1,"pixel sprites use point sampling without mipmaps");
                Transform dormant=rune.transform.parent.Find("Simbolo gravado");
                Check(dormant.GetComponent<SpriteRenderer>().sprite==rune.DormantSprite,"engraving uses the drawn first frame");
                Check(dormant.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_Desaturate")==1,"engraving is stone colored");
                Check(dormant.GetComponent<SpriteRenderer>().enabled,"dormant engraving is visible");
            }
            progress.Activate(MonolithSymbol.Moon); Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes)
            {
                Check(rune.gameObject.activeSelf==(rune.Symbol==MonolithSymbol.Moon),"only the accepted symbol animates");
                Check(!rune.IsFloating && rune.transform.localPosition==faces[rune],"partial sequence does not trigger floating symbols");
                Check(rune.transform.parent.Find("Simbolo gravado").GetComponent<SpriteRenderer>().enabled!=rune.gameObject.activeSelf,
                    "animated art replaces the resting engraving without duplicated sparks");
            }
            foreach (QuarryRuneAnimation rune in runes) if (rune.Symbol==MonolithSymbol.Moon) rune.ShowAt(.3f);
            progress.Activate(MonolithSymbol.Sun); Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes)
            {
                Check(!rune.gameObject.activeSelf && rune.CurrentFrame==0,"incorrect sequence stops and rewinds the animation");
                Check(!rune.IsFloating && rune.transform.localScale==Vector3.one,"wrong order cannot produce enlarged symbols");
            }
            progress.Activate(MonolithSymbol.Moon); progress.Activate(MonolithSymbol.Star); progress.Activate(MonolithSymbol.Sun);
            Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes)
            {
                Check(rune.IsFloating,"all three symbols celebrate the completed sequence together");
                rune.ShowCompletionAt(QuarryRuneAnimation.RevealDuration/2);
                Check(rune.transform.localScale.x>1 && rune.transform.localScale.x<QuarryRuneAnimation.FloatingScale,
                    "reveal smoothly enlarges the symbols");
                Check(rune.transform.localPosition.y>faces[rune].y,"reveal raises each symbol from its carving");
                rune.ShowCompletionAt(QuarryRuneAnimation.RevealDuration);
                CheckFloating(rune);
                float firstHeight=rune.transform.localPosition.y;
                rune.ShowCompletionAt(QuarryRuneAnimation.RevealDuration+1.4f);
                Check(Mathf.Abs(rune.transform.localPosition.y-firstHeight)>.02f,"symbols gently bob above their own stone");
                rune.ShowCompletionAt(QuarryRuneAnimation.RevealDuration);
            }
            var cameraObject = new GameObject("Rune detail camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic=true; camera.orthographicSize=1.25f; camera.nearClipPlane=.1f; camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            camera.transform.rotation=Quaternion.AngleAxis(ObliqueProjection.YawDegrees,Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees,0,0);
            var target=new RenderTexture(384,384,24); camera.targetTexture=target;
            try
            {
                foreach (QuarryRuneAnimation rune in runes)
                {
                    Check(rune.isActiveAndEnabled,"solved totems stay animated");
                    Check(rune.transform.parent.Find("Simbolo gravado").GetComponent<SpriteRenderer>().enabled,"completed stones retain their quiet engraving below the floating rune");
                    camera.transform.position=rune.transform.position-camera.transform.forward*60;
                    SpriteRenderer renderer=rune.GetComponent<SpriteRenderer>();
                    Vector3 size=renderer.sprite.bounds.size;
                    var seen=new HashSet<Sprite>();
                    for (int frame=0;frame<QuarryRuneAnimation.FrameCount;frame++)
                    {
                        rune.ShowAt((frame+.1f)/QuarryRuneAnimation.Rate(rune.Symbol));
                        Check(rune.CurrentFrame==frame && seen.Add(renderer.sprite),"all eight unique sprites play in order");
                        Check(Vector3.Distance(size,renderer.sprite.bounds.size)<.0001f,"pixel scale stays fixed through the loop");
                        camera.Render(); camera.Render();
                        Save(target,Path.Combine(output,"rune-"+rune.Symbol+"-"+frame+".png"));
                    }
                    rune.ShowAt(8.01f/QuarryRuneAnimation.Rate(rune.Symbol));
                    Check(rune.CurrentFrame==0,"seamless repeat begins at first sprite");
                }
            }
            finally { camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            // Save restoration updates the existing progress object without its Changed event.
            var restore = typeof(AncientSecrets).GetMethod("Restore",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            root.SetActive(false);
            restore.Invoke(progress,new object[] {false,false,0,false,false});
            root.SetActive(true); Refresh(targets);
            progress.Activate(MonolithSymbol.Moon); Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes)
                if (rune.Symbol==MonolithSymbol.Moon)
                    Check(!rune.IsFloating && rune.transform.localPosition==faces[rune] && rune.transform.localScale==Vector3.one,
                        "restoring an incomplete save puts the next activated rune back on the stone");
            root.SetActive(false);
            restore.Invoke(progress,new object[] {false,false,3,false,false});
            root.SetActive(true); Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes) CheckFloating(rune);
            root.SetActive(false); root.SetActive(true); Refresh(targets);
            foreach (QuarryRuneAnimation rune in runes) CheckFloating(rune);
            Debug.Log("QUARRY_FLOATING_VALIDATION_OK: completion reveal, enlarged hovering symbols, partial and incorrect sequences, map reentry and save restoration.");
            Debug.Log("QUARRY_RUNES_VALIDATION_OK: three drawn animations, 24 frames, pixel sampling, stable scale, activation, reset and completed challenge.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    private static void CheckFloating(QuarryRuneAnimation rune)
    {
        Check(rune.IsFloating && rune.isActiveAndEnabled,"completed or restored symbols float actively");
        Check(Mathf.Abs(rune.transform.localScale.x-QuarryRuneAnimation.FloatingScale)<.001f,"floating symbols have a larger consistent size");
        float stoneTop=rune.transform.parent.GetComponent<SpriteRenderer>().sprite.bounds.max.y;
        Check(rune.transform.localPosition.y>stoneTop+.5f,"floating symbol is visibly above the top of its own stone");
    }
    private static void Refresh(List<Interactable> targets)
    { foreach (Interactable target in targets) target.SendMessage("Update",SendMessageOptions.DontRequireReceiver); }
    private static void Check(bool ok,string reason) { if (!ok) throw new Exception("Rune validation: "+reason); }
    private static void Save(RenderTexture target,string path)
    {
        RenderTexture previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); RenderTexture.active=previous; }
    }
}
