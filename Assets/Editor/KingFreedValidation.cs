using System;
using DragonQuest.Prototype;
using DragonQuest.Progression;
using UnityEditor;
using UnityEngine;

public static class KingFreedValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Good Ending")]
    public static void Run()
    {
        foreach (string resource in new[] { KingFreedPresentation.SpeechResource, KingFreedPresentation.ProsperityResource })
        {
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null && texture.width >= 1600 && texture.width > texture.height, "landscape artwork exists: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp
                && !importer.mipmapEnabled && !importer.isReadable && importer.npotScale == TextureImporterNPOTScale.None
                && importer.textureCompression == TextureImporterCompression.Uncompressed, "native uncompressed artwork: " + resource);
            ValidateOriginalColors(texture);
            for (int frame = 0; frame <= 100; frame++)
            {
                bool isProsperity = resource == KingFreedPresentation.ProsperityResource;
                Rect bounds = KingFreedPresentation.ArtBounds(frame / 100f, texture.width / (float)texture.height, isProsperity);
                Assert(bounds.xMin <= 0 && bounds.xMax >= 1280 && bounds.yMin <= 0 && bounds.yMax >= 660,
                    "camera always covers the entire picture: " + resource);
                if (!isProsperity) Assert(bounds.yMin == 0, "the speech keeps the crown inside the frame");
            }
        }
        var font = Resources.Load<Font>("Fonts/PixelOperator");
        Assert(font != null, "narrative font exists");
        foreach (string text in new[] { EndingNarrative.KingFreedSpeech, EndingNarrative.KingFreedEpilogue })
        {
            font.RequestCharactersInTexture(text, 28, FontStyle.Normal);
            foreach (string line in text.Split('\n'))
            {
                int width = 0;
                foreach (char letter in line)
                {
                    Assert(font.GetCharacterInfo(letter, out CharacterInfo info, 28, FontStyle.Normal), "font covers: " + letter);
                    width += info.advance;
                }
                Assert(width + 2 < KingFreedPresentation.TextColumnWidth, "line and shadow fit the left column: " + line);
            }
        }
        using (var presentation = new KingFreedPresentation())
        {
            presentation.Update(10);
            Assert(presentation.Sequence.Elapsed == 0, "loading artwork cannot consume the first frame");
            var sequence = presentation.Sequence;
            sequence.Advance(sequence.SpeechTextEndAt);
            Assert(sequence.ProsperityOpacity == 0 && sequence.BlackOpacity == 0, "the whole speech precedes the dissolve");
            sequence.Advance(sequence.EpilogueTextStart - sequence.Elapsed);
            Assert(sequence.ProsperityOpacity == 1 && sequence.BlackOpacity == 0, "the epilogue starts on the prosperity image");
            sequence.Advance(sequence.TextEndAt - sequence.Elapsed);
            Assert(sequence.BlackOpacity == 0 && !sequence.IsComplete, "the complete epilogue precedes the final fade");
            sequence.Advance(sequence.CompleteAt);
            Assert(sequence.IsComplete && sequence.BlackOpacity == 1, "the menu is reached only after black");
        }
        Assert(EndingNarrative.Pages(GameEnding.KingFreed).Length == 3, "legacy narrative API remains compatible");
        Debug.Log("GOOD_ENDING_VALIDATION_OK: original unfiltered painting colors, framing, complete speech, prosperity and black hold.");
    }

    private static void ValidateOriginalColors(Texture2D artwork)
    {
        var reference = RenderTexture.GetTemporary(artwork.width,artwork.height,0,RenderTextureFormat.ARGB32);
        var capture = new Texture2D(artwork.width,artwork.height,TextureFormat.RGBA32,false,true);
        var previous = RenderTexture.active;
        try
        {
            Graphics.Blit(artwork,reference); RenderTexture.active=reference;
            capture.ReadPixels(new Rect(0,0,artwork.width,artwork.height),0,0); capture.Apply();
            Color[] original = capture.GetPixels();
            using (var renderer = new EndingArtworkRenderer())
            {
                Rect full = new Rect(0,0,artwork.width,artwork.height);
                RenderTexture.active=renderer.RenderFrame(artwork,full,full);
                capture.ReadPixels(full,0,0); capture.Apply();
                Color[] rendered = capture.GetPixels();
                RenderTexture.active=reference;
                for (int y=artwork.height/8;y<artwork.height;y+=artwork.height/4)
                    for (int x=artwork.width/8;x<artwork.width;x+=artwork.width/4)
                    {
                        Color difference=rendered[y*artwork.width+x]-original[y*artwork.width+x];
                        Assert(Mathf.Abs(difference.r)+Mathf.Abs(difference.g)+Mathf.Abs(difference.b)<.015f,
                            "good ending keeps original RGB without a color filter: "+artwork.name);
                    }
            }
        }
        finally
        {
            RenderTexture.active=previous; RenderTexture.ReleaseTemporary(reference);
            UnityEngine.Object.DestroyImmediate(capture);
        }
    }

    [MenuItem("Tools/Dragon Quest/Preview Good Ending")]
    public static void Preview()
    {
        Run();
        var window = EditorWindow.GetWindow<KingFreedPreview>();
        window.titleContent = new GUIContent("Final bom / Reino livre");
        window.minSize = new Vector2(640, 400); window.Show();
    }
    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Final bom: " + message); }
}

public sealed class KingFreedPreview : EditorWindow
{
    private KingFreedPresentation presentation;
    private double previousTime;
    private bool playing;
    private void OnEnable() { previousTime = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
    private void OnDisable() { EditorApplication.update -= Tick; presentation?.Dispose(); presentation = null; }
    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        if (playing && presentation != null)
        {
            presentation.Update((float)(now - previousTime));
            if (presentation.Sequence.IsComplete) playing = false;
        }
        previousTime = now; Repaint();
    }
    private void Jump(float seconds)
    { presentation?.Dispose(); presentation = new KingFreedPresentation(); presentation.Sequence.Advance(seconds); playing = false; }
    private void OnGUI()
    {
        if (presentation == null) Jump(KingFreedSequence.TextStart + 14);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing ? "Pausar" : "Animar", EditorStyles.toolbarButton)) playing = !playing;
        if (GUILayout.Button("Início", EditorStyles.toolbarButton)) Jump(0);
        if (GUILayout.Button("Discurso", EditorStyles.toolbarButton)) Jump(KingFreedSequence.TextStart + 14);
        if (GUILayout.Button("Transição", EditorStyles.toolbarButton)) Jump(presentation.Sequence.DissolveAt + 1.5f);
        if (GUILayout.Button("Prosperidade", EditorStyles.toolbarButton)) Jump(presentation.Sequence.EpilogueTextStart + 14);
        if (GUILayout.Button("Fade", EditorStyles.toolbarButton)) Jump(presentation.Sequence.FadeOutAt + 1.5f);
        if (GUILayout.Button("Preto", EditorStyles.toolbarButton)) Jump(presentation.Sequence.BlackAt);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0") + " s", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0, 24, position.width, position.height - 24));
    }
}
