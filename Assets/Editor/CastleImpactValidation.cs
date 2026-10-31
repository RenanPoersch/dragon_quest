using DragonQuest.Presentation.Cinematics.Endings;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CastleImpactValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Castle Impact")]
    public static void Run()
    {
        foreach (string resource in new[] { CastleImpactPresentation.CastleResource, CastleImpactPresentation.MeteorResource,
            CastleImpactPresentation.RedCloudResource })
        {
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null, "art exists: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.filterMode == FilterMode.Bilinear && texture.wrapMode == TextureWrapMode.Clamp,
                "smooth cinematic filtering and clamp: " + resource);
            Assert(!importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "uncompressed art without mipmaps: " + resource);
        }
        var meteor = Resources.Load<Texture2D>(CastleImpactPresentation.MeteorResource);
        var source = new Texture2D(2, 2);
        try
        {
            source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(meteor)));
            int clear=0, visible=0;
            foreach (Color32 pixel in source.GetPixels32())
            { if (pixel.a < 8) clear++; if (pixel.a > 128) visible++; }
            Assert(clear > source.width*source.height/4 && visible > source.width*source.height/5,
                "meteor has genuine transparency and substantial visible rock/flame");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        Shader shader = Resources.Load<Shader>(CastleImpactPresentation.ShaderResource);
        Assert(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "composite shader compiles");
        ValidateCloudColors(shader);
        using (var ending = new CastleDestroyedEndingPresentation())
        {
            ending.Update(4);
            Assert(ending.Elapsed==0, "preloading cannot consume the opening before its first frame");
            ending.Advance(CastleImpactSequence.CompleteAt-.1f);
            Assert(ending.Aftermath.Sequence.Elapsed==0 && !ending.IsComplete, "hill waits behind the full white hold");
            ending.Advance(.35f);
            Assert(Mathf.Abs(ending.Aftermath.Sequence.Elapsed-.25f)<.002f && !ending.IsComplete,
                "a frame crossing the cut carries only its remaining time into the hill");
            ending.Advance(ending.Aftermath.Sequence.CompleteAt);
            Assert(ending.IsComplete, "menu completion waits for the original epilogue and its final black hold");
        }
        using (var impact = new CastleImpactPresentation())
        {
            var target = impact.RenderFrame();
            Assert(target.IsCreated() && target.width==1920 && target.height==990, "GPU cinematic renders");
        }
        Debug.Log("CASTLE_IMPACT_VALIDATION_OK: cloud-only red artwork, original foreground colors, alpha, shader and complete epilogue.");
    }

    private static void ValidateCloudColors(Shader shader)
    {
        var castle = Resources.Load<Texture2D>(CastleImpactPresentation.CastleResource);
        var clouds = Resources.Load<Texture2D>(CastleImpactPresentation.RedCloudResource);
        Assert(clouds.width==castle.width && clouds.height==castle.height, "cloud variant aligns with the original painting");
        var material = new Material(shader);
        var target = RenderTexture.GetTemporary(castle.width,castle.height,0,RenderTextureFormat.ARGB32);
        var readback = new Texture2D(castle.width,castle.height,TextureFormat.RGBA32,false,true);
        var previous = RenderTexture.active;
        try
        {
            material.SetTexture("_RedCloudTex",clouds);
            material.SetVector("_MeteorRect",new Vector4(-10,-10,1,1));
            material.SetFloat("_CinematicTime",6);
            material.SetFloat("_CameraZoom",1); material.SetFloat("_CropY",1);
            material.SetFloat("_MeteorOpacity",0);
            Graphics.Blit(castle,target,material); RenderTexture.active=target;
            readback.ReadPixels(new Rect(0,0,castle.width,castle.height),0,0); readback.Apply();
            Color[] before = readback.GetPixels();
            material.SetFloat("_MeteorOpacity",1);
            Graphics.Blit(castle,target,material);
            readback.ReadPixels(new Rect(0,0,castle.width,castle.height),0,0); readback.Apply();
            Color[] after = readback.GetPixels();
            // Native artwork coordinates measured from the upper-left corner.
            foreach (Vector2 point in new[] { new Vector2(1470,300),new Vector2(1235,340),new Vector2(985,430),
                new Vector2(1320,420),new Vector2(650,520),new Vector2(100,830),new Vector2(800,810),new Vector2(1500,820) })
            {
                int pixel=(castle.height-1-(int)point.y)*castle.width+(int)point.x;
                Color difference=after[pixel]-before[pixel];
                Assert(Mathf.Abs(difference.r)+Mathf.Abs(difference.g)+Mathf.Abs(difference.b)<.015f,
                    "castle, roofs, mountains and water retain their original RGB: "+point);
            }
            foreach (Vector2 point in new[] { new Vector2(300,200),new Vector2(800,180),new Vector2(1100,110) })
            {
                int pixel=(castle.height-1-(int)point.y)*castle.width+(int)point.x;
                Assert(after[pixel].r>after[pixel].g*2 && after[pixel].r>after[pixel].b*2
                    && Mathf.Abs(after[pixel].r-before[pixel].r)+Mathf.Abs(after[pixel].g-before[pixel].g)>.05f,
                    "clouds turn red when the meteor appears: "+point);
            }
        }
        finally
        {
            RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(material);
        }
    }

    public static void RunAndCapture()
    {
        Run();
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-castleImpactCaptureDir");
        if (index < 0 || index+1 >= args.Length) throw new InvalidOperationException("Missing -castleImpactCaptureDir.");
        string directory = Path.GetFullPath(args[index+1]);
        Directory.CreateDirectory(directory);
        foreach (float time in new[] { 2f, 6f, 10f, 15.9f, 16.225f, 17.3f })
        {
            using (var impact = new CastleImpactPresentation())
            {
                impact.Sequence.Advance(time);
                var previous = RenderTexture.active;
                var capture = new Texture2D(1920, 990, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = impact.RenderFrame();
                    capture.ReadPixels(new Rect(0,0,1920,990),0,0); capture.Apply();
                    Color[] pixels = capture.GetPixels();
                    for (int p=0;p<pixels.Length;p++)
                        pixels[p] = Color.Lerp(Color.Lerp(pixels[p],Color.black,impact.Sequence.BlackOpacity),
                            Color.white,impact.Sequence.WhiteOpacity);
                    capture.SetPixels(pixels); capture.Apply();
                    File.WriteAllBytes(Path.Combine(directory,"impact-"+time.ToString("00.000",System.Globalization.CultureInfo.InvariantCulture)+".png"),
                        capture.EncodeToPNG());
                }
                finally { RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(capture); }
            }
        }
        Debug.Log("CASTLE_IMPACT_CAPTURE_OK: "+directory);
    }

    [MenuItem("Tools/Dragon Quest/Preview Castle Impact")]
    public static void Preview()
    {
        Run();
        var window = EditorWindow.GetWindow<CastleImpactPreview>();
        window.titleContent = new GUIContent("Meteoro / Cinzas");
        window.minSize = new Vector2(800,460); window.Show();
    }
    private static void Assert(bool value,string message)
    { if (!value) throw new InvalidOperationException("Castle impact: "+message); }
}

public sealed class CastleImpactPreview : EditorWindow
{
    private CastleDestroyedEndingPresentation presentation;
    private double previousTime;
    private bool playing;
    private void OnEnable() { previousTime=EditorApplication.timeSinceStartup; EditorApplication.update+=Tick; }
    private void OnDisable() { EditorApplication.update-=Tick; presentation?.Dispose(); presentation=null; }
    private void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        if (playing && presentation!=null) presentation.Update((float)(now-previousTime));
        if (presentation!=null && presentation.IsComplete) playing=false;
        previousTime=now; Repaint();
    }
    private void Jump(float seconds)
    { presentation?.Dispose(); presentation=new CastleDestroyedEndingPresentation(); presentation.Advance(seconds); playing=false; }
    private void OnGUI()
    {
        if (presentation==null) Jump(0);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing?"Pausar":"Animar",EditorStyles.toolbarButton)) playing=!playing;
        if (GUILayout.Button("Tempestade",EditorStyles.toolbarButton)) Jump(2);
        if (GUILayout.Button("Meteoro",EditorStyles.toolbarButton)) Jump(10);
        if (GUILayout.Button("Impacto",EditorStyles.toolbarButton)) Jump(CastleImpactSequence.ImpactAt);
        if (GUILayout.Button("Branco",EditorStyles.toolbarButton)) Jump(17);
        if (GUILayout.Button("Colina",EditorStyles.toolbarButton)) Jump(CastleImpactSequence.CompleteAt+CastleDestroyedSequence.TextStart+9);
        GUILayout.Label(presentation.Elapsed.ToString("0.00")+" s",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        float duration=CastleImpactSequence.CompleteAt+presentation.Aftermath.Sequence.CompleteAt;
        float positionOnTimeline=GUILayout.HorizontalSlider(presentation.Elapsed,0,duration);
        if (Mathf.Abs(positionOnTimeline-presentation.Elapsed)>.01f) Jump(positionOnTimeline);
        presentation.Draw(new Rect(0,42,position.width,position.height-42));
    }
}
