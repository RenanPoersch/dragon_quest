using System;
using System.Collections.Generic;
using System.IO;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuarryValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Quarry")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_QUARRY_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/QuarryPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        PixelSpriteLibrary people = null;
        var ownedPeople = new List<UnityEngine.Object>();
        try
        {
            var root = new GameObject("Quarry validation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var secrets = new AncientSecrets();
            var targets = new List<Interactable>();
            root.AddComponent<QuarryPixelScenery>().Build(secrets,targets);
            Assert(targets.Count == 4, "three totems and the inscription are interactive");
            foreach (CityBlock block in QuarryLayout.Monoliths)
            {
                Transform anchor = root.transform.Find(block.Name);
                Assert(anchor != null && anchor.GetComponent<BoxCollider2D>() != null, "totem footprint matches interaction");
                Assert(anchor.GetComponentInChildren<SpriteRenderer>() != null, "totem uses pixel artwork");
                Assert(!Glow(anchor).activeSelf, "unlit totem remains dark");
            }
            var cameraObject = new GameObject("Quarry preview camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var follow = cameraObject.AddComponent<CameraFollow>(); follow.SetArtworkFrame(true);
            people = new PixelSpriteLibrary();
            ownedPeople.Add(people.Material);
            Quaternion rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees,Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees,0,0);
            Quaternion upright = Quaternion.LookRotation(PixelSpriteLayout.HorizontalCameraForward(rotation*Vector3.forward),PixelSpriteLayout.UprightUp);
            Person(root.transform,people,"Heroi",QuarryLayout.Spawn,"ArenWalk",1,upright);
            ownedPeople.AddRange(people.Atlas("ArenWalk").Sprites);
            Person(root.transform,people,"Guardiao",QuarryLayout.Worker,"NpcRoster",8,upright);
            ownedPeople.AddRange(people.Atlas("NpcRoster").Sprites);
            foreach (string state in new[] { "unlit", "lit" })
            {
                if (state == "lit")
                {
                    secrets.Activate(MonolithSymbol.Moon); Refresh(targets);
                    Assert(Glow(root.transform.Find("Monolito - Lua")).activeSelf, "moon lights after activation");
                    secrets.Activate(MonolithSymbol.Sun); Refresh(targets);
                    Assert(!Glow(root.transform.Find("Monolito - Lua")).activeSelf, "wrong order extinguishes the rune");
                    secrets.Activate(MonolithSymbol.Moon); secrets.Activate(MonolithSymbol.Star); secrets.Activate(MonolithSymbol.Sun);
                    Refresh(targets);
                    foreach (CityBlock block in QuarryLayout.Monoliths) Assert(Glow(root.transform.Find(block.Name)).activeSelf, "completed symbols stay lit");
                    foreach (QuarryRuneAnimation rune in root.GetComponentsInChildren<QuarryRuneAnimation>(true))
                    {
                        rune.ShowCompletionAt(QuarryRuneAnimation.RevealDuration);
                        Assert(rune.IsFloating && rune.transform.localScale.x > 3,"completed symbols float enlarged above stones");
                    }
                }
                foreach (var resolution in new[] { new Vector2Int(1280,720), new Vector2Int(1280,800), new Vector2Int(960,720) })
                {
                    var target = new RenderTexture(resolution.x,resolution.y,24);
                    try
                    {
                        camera.targetTexture = target;
                        follow.Initialize(root.transform,PrototypeWorldLayout.ForestBounds,8,true);
                        Assert(Mathf.Abs(camera.aspect-(float)resolution.x/resolution.y)<.001f, "artwork keeps display aspect");
                        camera.Render(); camera.Render();
                        Save(target,Path.Combine(output,"quarry-"+resolution.x+"x"+resolution.y+"-"+state+".png"));
                        if (state == "lit" && resolution.x == 1280 && resolution.y == 720)
                        {
                            var star = root.transform.Find("Monolito - Estrela");
                            camera.orthographicSize = 2.6f;
                            camera.transform.position = star.position + rotation*Vector3.up*2.5f - camera.transform.forward*60;
                            camera.Render(); Save(target,Path.Combine(output,"quarry-totem-detail.png"));
                        }
                    }
                    finally { camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                }
            }
            Assert(secrets.MonolithsSolved && secrets.MonolithStep == 3, "preview challenge is completed");
            Debug.Log("QUARRY_VALIDATION_OK: scenery, totem artwork, collisions, reset, completed lights and three display aspects.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (UnityEngine.Object item in ownedPeople) if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }
    }

    private static void Person(Transform root, PixelSpriteLibrary people, string name, Vector2 ground, string atlas, int frame, Quaternion upright)
    {
        var anchor = new GameObject(name); anchor.transform.SetParent(root,false); anchor.transform.localPosition=ground;
        Transform visual = people.Create(anchor.transform,atlas,frame);
        visual.GetComponent<PixelCharacter>().enabled=false;
        visual.localRotation=upright; visual.localScale=new Vector3(1,PixelSpriteLayout.UprightScaleY,1);
    }
    private static GameObject Glow(Transform anchor)
    {
        foreach (Transform child in anchor.GetComponentsInChildren<Transform>(true))
            if (child.name.StartsWith("Luz do totem")) return child.gameObject;
        throw new Exception("Totem glow missing");
    }
    private static void Refresh(List<Interactable> targets)
    { foreach (Interactable target in targets) target.SendMessage("Update",SendMessageOptions.DontRequireReceiver); }
    private static void Assert(bool ok, string reason) { if (!ok) throw new Exception("Quarry validation: "+reason); }
    private static void Save(RenderTexture target, string path)
    {
        RenderTexture previous=RenderTexture.active; RenderTexture.active=target;
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active=previous; }
    }
}
