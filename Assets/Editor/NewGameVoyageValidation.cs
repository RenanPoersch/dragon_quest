using System;
using UnityEditor;
using UnityEngine;
using DragonQuest.Prototype;

public static class NewGameVoyageValidation
{
    [MenuItem("Tools/Dragon Quest/Preview New Game Voyage")]
    public static void Preview()
    {
        ValidateAssets();
        var window = EditorWindow.GetWindow<NewGameVoyagePreview>();
        window.titleContent = new GUIContent("New Game / Travessia");
        window.minSize = new Vector2(640, 400);
        window.Show();
    }

    public static void ValidateAssets()
    {
        foreach (string resource in new[] { "RuinsPaintedPanorama", "Intro/VoyageCarrack", "ArenWalk", "LiaWalk", "BramWalk" })
        {
            var texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            if (texture == null || !texture.isReadable || texture.filterMode != FilterMode.Point)
                throw new Exception("Voyage requires readable point-filtered artwork: " + resource);
        }
        var ship = Resources.Load<Texture2D>("PixelArt/Intro/VoyageCarrack");
        Color32[] pixels = ship.GetPixels32();
        int transparent = 0, solid = 0;
        foreach (var pixel in pixels) { if (pixel.a < 8) transparent++; if (pixel.a > 240) solid++; }
        if (transparent < pixels.Length / 5 || solid < pixels.Length / 5)
            throw new Exception("The carrack must contain both a visible vessel and a transparent background.");
        Debug.Log("NEW_GAME_VOYAGE_ASSETS_OK: ruins panorama, transparent carrack, three walk atlases.");
    }
}

public sealed class NewGameVoyagePreview : EditorWindow
{
    private NewGameVoyagePresentation presentation;
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
    private void Jump(float time)
    {
        presentation?.Dispose(); presentation = new NewGameVoyagePresentation();
        presentation.Sequence.Advance(time); playing = false;
    }
    private void OnGUI()
    {
        if (presentation == null) Jump(12);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing ? "Pausar" : "Animar", EditorStyles.toolbarButton)) playing = !playing;
        if (GUILayout.Button("Início", EditorStyles.toolbarButton)) Jump(0);
        if (GUILayout.Button("Convés", EditorStyles.toolbarButton)) Jump(12);
        if (GUILayout.Button("Partida", EditorStyles.toolbarButton)) Jump(NewGameVoyageSequence.DepartureAt);
        if (GUILayout.Button("Fade", EditorStyles.toolbarButton)) Jump(NewGameVoyageSequence.BlackAt - 2.5f);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0") + " s", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0, 24, position.width, position.height - 24));
    }
}
