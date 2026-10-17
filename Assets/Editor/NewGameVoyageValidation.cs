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
        using (var layers = new VoyageShipLayers(ship))
        {
            Color32[] rear = layers.BehindParty.GetPixels32(), front = layers.InFrontOfParty.GetPixels32();
            int frontCount = 0, rearCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (front[i].a > 0) frontCount++;
                if (rear[i].a > 0) rearCount++;
                if (front[i].a > 0 && rear[i].a > 0) throw new Exception("Voyage layers overlap at " + i);
                if (pixels[i].a == 0) continue;
                Color32 reconstructed = front[i].a > 0 ? front[i] : rear[i];
                if (reconstructed.r != pixels[i].r || reconstructed.g != pixels[i].g || reconstructed.b != pixels[i].b
                    || reconstructed.a != pixels[i].a) throw new Exception("Voyage layers lost source artwork at " + i);
            }
            if (frontCount == 0 || rearCount == 0) throw new Exception("Both sides of the ship must contain artwork.");
            float height = NewGameVoyageSequence.ShipWidth * ship.height / ship.width;
            for (int hero = 0; hero < 3; hero++) for (int step = 0; step <= 40; step++)
            {
                var walk = new NewGameVoyageSequence();
                walk.Advance(NewGameVoyageSequence.WalkStart + hero * NewGameVoyageSequence.HeroDelay
                    + step / 40f * NewGameVoyageSequence.WalkDuration);
                float x = walk.HeroLocalX(hero), y = VoyageShipLayers.DeckHeightAt(x);
                int px = Mathf.Clamp((int)(x * ship.width), 0, ship.width - 1);
                int py = Mathf.Clamp((int)((1 - y) * ship.height), 0, ship.height - 1);
                if (front[py * ship.width + px].a < 220)
                    throw new Exception("Hero " + hero + " has no opaque deck below the feet at walk sample " + step);
                float occlusion = height * (y - VoyageShipLayers.RailHeightAt(x));
                if (occlusion < 12 || occlusion > NewGameVoyageSequence.HeroSize * .55f)
                    throw new Exception("Deck should conceal the lower body without sinking hero " + hero);
            }
        }
        var complete = new NewGameVoyageSequence(); complete.Advance(NewGameVoyageSequence.CompleteAt);
        var panorama = Resources.Load<Texture2D>("PixelArt/RuinsPaintedPanorama");
        if (complete.BackgroundOffset >= panorama.width * 720f / panorama.height - 1280)
            throw new Exception("Panorama must cover the entire rightward drift without tiling or exposed edges.");
        var font = Resources.Load<Font>("Fonts/PixelOperator");
        for (int i = 0; i < NewGameVoyageSequence.LineCount; i++)
        {
            string line = NewGameVoyageSequence.Line(i);
            font.RequestCharactersInTexture(line, 26, FontStyle.Normal);
            int width = 0;
            foreach (char letter in line)
            {
                if (!font.GetCharacterInfo(letter, out CharacterInfo info, 26, FontStyle.Normal))
                    throw new Exception("Voyage font missing character: " + letter);
                width += info.advance;
            }
            if (width >= 404) throw new Exception("Voyage line exceeds the narrative column: " + line);
        }
        Debug.Log("NEW_GAME_VOYAGE_ASSETS_OK: close foredeck, rear/front reconstruction, hero occlusion, narrative font and three walk atlases.");
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
        if (GUILayout.Button("Chegada", EditorStyles.toolbarButton)) Jump(NewGameVoyageSequence.ArrivalStart + NewGameVoyageSequence.ArrivalDuration - .2f);
        if (GUILayout.Button("Convés", EditorStyles.toolbarButton)) Jump(12);
        if (GUILayout.Button("Partida", EditorStyles.toolbarButton)) Jump(NewGameVoyageSequence.DepartureAt);
        if (GUILayout.Button("Fade", EditorStyles.toolbarButton)) Jump(NewGameVoyageSequence.FadeOutAt + 1.5f);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0") + " s", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0, 24, position.width, position.height - 24));
    }
}
