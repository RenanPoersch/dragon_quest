using System;
using System.IO;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class CastleImpactValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Castle Impact")]
    public static void Run()
    {
        foreach (string resource in new[] { CastleImpactPresentation.CastleResource, CastleImpactPresentation.MeteorResource })
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
        Debug.Log("CASTLE_IMPACT_VALIDATION_OK: imported art, alpha, shader, first frame, cut overflow and full epilogue.");
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
