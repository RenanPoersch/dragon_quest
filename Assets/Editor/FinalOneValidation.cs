using System;
using System.IO;
using DragonQuest.Prototype;
using DragonQuest.Progression;
using UnityEditor;
using UnityEngine;

public static class FinalOneValidation
{
    [MenuItem("Tools/Dragon Quest/Validate FinalOne")]
    public static void Run()
    {
        foreach (string resource in new[] { CastleDestroyedPresentation.BackgroundResource,
            CastleDestroyedPresentation.ForegroundResource })
        {
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null, "artwork exists: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp,
                "Point/Clamp preserves crisp edges: " + resource);
            Assert(!importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "no mipmaps or compression: " + resource);
        }
        var ground = Resources.Load<Texture2D>(CastleDestroyedPresentation.ForegroundResource);
        var source = new Texture2D(2, 2);
        try
        {
            source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(ground)));
            int clear = 0, solid = 0;
            foreach (Color32 pixel in source.GetPixels32())
            { if (pixel.a < 8) clear++; if (pixel.a > 240) solid++; }
            Assert(clear > source.width * source.height / 3 && solid > source.width * source.height / 3,
                "the seated figure and hill have real transparent air and substantial visible artwork");
            int grounded = 0;
            for (int x = 0; x < source.width; x++) if (source.GetPixel(x, 0).a > .9f) grounded++;
            Assert(grounded > source.width / 2, "the hill continues through the lower edge");
            int crest = SilhouetteTop(source, .4f);
            Assert(SilhouetteTop(source, .12f) - crest > source.height * .1f
                && SilhouetteTop(source, .98f) - crest > source.height * .1f,
                "the terrain rises toward its crest and curves down on both sides instead of forming a straight ledge");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        var font = Resources.Load<Font>("Fonts/PixelOperator");
        font.RequestCharactersInTexture(EndingNarrative.CastleDestroyedEpilogue, 28, FontStyle.Normal);
        foreach (string line in EndingNarrative.CastleDestroyedEpilogue.Split('\n'))
        {
            int width = 0;
            foreach (char letter in line)
            {
                Assert(font.GetCharacterInfo(letter, out CharacterInfo info, 28, FontStyle.Normal), "font covers narrative character: " + letter);
                width += info.advance;
            }
            Assert(width < 470, "narrative fits the left column: " + line);
        }
        using (var presentation = new CastleDestroyedPresentation())
        {
            var sequence = presentation.Sequence;
            sequence.Advance(CastleDestroyedSequence.ArrivalEnd);
            Assert(Mathf.Approximately(sequence.LandX, CastleDestroyedSequence.LandStopX),
                "the seated figure reaches the composition together with the hillside");
            sequence.Advance(sequence.TextEndAt - sequence.Elapsed);
            Assert(sequence.BlackOpacity == 0 && !sequence.IsComplete, "the entire narrative finishes before fading");
            sequence.Advance(sequence.CompleteAt);
            Assert(sequence.BlackOpacity == 1 && sequence.IsComplete, "the ending finishes at black");
        }
        Assert(EndingNarrative.Pages(GameEnding.CastleDestroyed).Length == 3
            && EndingNarrative.Pages(GameEnding.KingFreed).Length == 3
            && EndingNarrative.Pages(GameEnding.HeroPossessed).Length == 3, "the other endings retain their pages");
        Debug.Log("FINAL_ONE_VALIDATION_OK: rounded hill and seated gardener, transparent air, opposing drift, narrative and fade.");
    }

    [MenuItem("Tools/Dragon Quest/Preview FinalOne")]
    public static void Preview()
    {
        Run();
        var window = EditorWindow.GetWindow<FinalOnePreview>();
        window.titleContent = new GUIContent("FinalOne / Cinzas");
        window.minSize = new Vector2(640, 400); window.Show();
    }
    private static void Assert(bool value, string message)
    { if (!value) throw new InvalidOperationException("FinalOne: " + message); }
    private static int SilhouetteTop(Texture2D source, float fraction)
    {
        int x = Mathf.RoundToInt((source.width - 1) * fraction);
        for (int y = source.height - 1; y >= 0; y--)
            if (source.GetPixel(x, y).a > .9f) return source.height - 1 - y;
        throw new InvalidOperationException("FinalOne: empty terrain silhouette column.");
    }
}

public sealed class FinalOnePreview : EditorWindow
{
    private CastleDestroyedPresentation presentation;
    private double previousTime;
    private bool playing;
    private void OnEnable() { previousTime = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
    private void OnDisable() { EditorApplication.update -= Tick; presentation?.Dispose(); presentation = null; }
    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        if (playing && presentation != null) presentation.Update((float)(now - previousTime));
        previousTime = now; Repaint();
    }
    private void Jump(float seconds)
    { presentation?.Dispose(); presentation = new CastleDestroyedPresentation(); presentation.Sequence.Advance(seconds); playing = false; }
    private void OnGUI()
    {
        if (presentation == null) Jump(CastleDestroyedSequence.TextStart + 9);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing ? "Pausar" : "Animar", EditorStyles.toolbarButton)) playing = !playing;
        if (GUILayout.Button("Início", EditorStyles.toolbarButton)) Jump(0);
        if (GUILayout.Button("Chegada", EditorStyles.toolbarButton)) Jump(CastleDestroyedSequence.ArrivalEnd);
        if (GUILayout.Button("Colina / velho sentado", EditorStyles.toolbarButton)) Jump(CastleDestroyedSequence.TextStart + 9);
        if (GUILayout.Button("Fim do texto", EditorStyles.toolbarButton)) Jump(presentation.Sequence.TextEndAt);
        if (GUILayout.Button("Fade", EditorStyles.toolbarButton)) Jump(presentation.Sequence.FadeOutAt + 1.5f);
        if (GUILayout.Button("Preto", EditorStyles.toolbarButton)) Jump(presentation.Sequence.BlackAt);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0") + " s", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0, 24, position.width, position.height - 24));
    }
}
