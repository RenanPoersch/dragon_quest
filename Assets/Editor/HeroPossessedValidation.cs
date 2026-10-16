using System;
using System.IO;
using DragonQuest.Prototype;
using DragonQuest.Progression;
using UnityEditor;
using UnityEngine;

public static class HeroPossessedValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Possessed Ending")]
    public static void Run()
    {
        foreach (string resource in new[] { HeroPossessedPresentation.ThroneResource, HeroPossessedPresentation.SpiritResource })
        {
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null, "artwork exists: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp,
                "Point/Clamp preserves crisp edges: " + resource);
            Assert(!importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "no mipmaps or compression: " + resource);
        }
        var shader = Resources.Load<Shader>(HeroPossessedPresentation.CompositeResource);
        Assert(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "the lightning compositor compiles");
        ValidateLightning(shader);
        var font = Resources.Load<Font>("Fonts/PixelOperator");
        font.RequestCharactersInTexture(EndingNarrative.HeroPossessedEpilogue, 28, FontStyle.Normal);
        foreach (string line in EndingNarrative.HeroPossessedEpilogue.Split('\n'))
        {
            int width = 0;
            foreach (char character in line)
            {
                Assert(font.GetCharacterInfo(character, out CharacterInfo info, 28, FontStyle.Normal), "font covers: " + character);
                width += info.advance;
            }
            Assert(width < 350, "narrative fits the side column: " + line);
        }
        using (var presentation = new HeroPossessedPresentation())
        {
            var sequence = presentation.Sequence;
            sequence.Advance(HeroPossessedSequence.CameraStopAt);
            Rect locked = HeroPossessedPresentation.CameraBounds(sequence, 2f/3);
            Assert(Mathf.Approximately(locked.center.x, 640), "the hero and throne stay centered");
            // Coordinates encompass the crown/throne top through the seated hero's boots.
            Assert(locked.y + locked.height * .127f > 0 && locked.y + locked.height * .523f < 660,
                "the final composition contains the throne and entire seated hero");
            sequence.Advance(4);
            Assert(HeroPossessedPresentation.CameraBounds(sequence, 2f/3) == locked, "lightning never moves the camera");
            sequence.Advance(sequence.TextEndAt-sequence.Elapsed);
            Assert(sequence.BlackOpacity == 0 && !sequence.IsComplete, "the complete text precedes the fade");
            sequence.Advance(sequence.CompleteAt);
            Assert(sequence.BlackOpacity == 1 && sequence.IsComplete, "the sequence ends at black before returning to menu");
        }
        Assert(EndingNarrative.Pages(GameEnding.KingFreed).Length == 3
            && EndingNarrative.Pages(GameEnding.CastleDestroyed).Length == 3
            && EndingNarrative.Pages(GameEnding.HeroPossessed).Length == 3, "ending/save compatibility remains intact");
        Debug.Log("HERO_POSSESSED_VALIDATION_OK: art, shader, font, centered locked composition, complete narrative and standard fade.");
    }
    [MenuItem("Tools/Dragon Quest/Preview Possessed Ending")]
    public static void Preview()
    {
        Run();
        var window = EditorWindow.GetWindow<HeroPossessedPreview>();
        window.titleContent = new GUIContent("Final / Possessão");
        window.minSize = new Vector2(640,400); window.Show();
    }
    private static void Assert(bool value, string message)
    { if (!value) throw new InvalidOperationException("Possessed ending: " + message); }

    private static void ValidateLightning(Shader shader)
    {
        var throne=Resources.Load<Texture2D>(HeroPossessedPresentation.ThroneResource);
        var material=new Material(shader);
        material.SetTexture("_SpiritTex",Resources.Load<Texture2D>(HeroPossessedPresentation.SpiritResource));
        var target=new RenderTexture(throne.width,throne.height,0,RenderTextureFormat.ARGB32);
        var readback=new Texture2D(throne.width,throne.height,TextureFormat.RGBA32,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            material.SetFloat("_Lightning",0); Graphics.Blit(throne,target,material);
            RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            Color32[] dark=readback.GetPixels32();
            material.SetFloat("_Lightning",1); Graphics.Blit(throne,target,material);
            RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            Color32[] lit=readback.GetPixels32();
            int changed=0;
            for(int y=0;y<target.height;y++) for(int x=0;x<target.width;x++)
            {
                int i=y*target.width+x;
                bool same=dark[i].Equals(lit[i]);
                if(!same) changed++;
                bool face=x>target.width*.435f && x<target.width*.565f && y>target.height*.72f && y<target.height*.79f;
                bool torso=x>target.width*.38f && x<target.width*.62f && y>target.height*.52f && y<target.height*.70f;
                if(face || torso)
                    Assert(same,"lightning remains behind Aren's face and clothing");
            }
            Assert(changed>1000,"lightning reveals a substantial spirit silhouette in the black recess");
        }
        finally
        {
            RenderTexture.active=previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(material);
        }
    }
}

public sealed class HeroPossessedPreview : EditorWindow
{
    private HeroPossessedPresentation presentation;
    private double previousTime;
    private bool playing;
    private void OnEnable() { previousTime=EditorApplication.timeSinceStartup; EditorApplication.update+=Tick; }
    private void OnDisable() { EditorApplication.update-=Tick; presentation?.Dispose(); presentation=null; }
    private void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        if (playing && presentation != null) presentation.Update((float)(now-previousTime));
        previousTime=now; Repaint();
    }
    private void Jump(float seconds)
    { presentation?.Dispose(); presentation=new HeroPossessedPresentation(); presentation.Sequence.Advance(seconds); playing=false; }
    private void OnGUI()
    {
        if (presentation==null) Jump(HeroPossessedSequence.CameraStopAt);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing?"Pausar":"Animar",EditorStyles.toolbarButton)) playing=!playing;
        if (GUILayout.Button("Tapete",EditorStyles.toolbarButton)) Jump(1.4f);
        if (GUILayout.Button("Escadas",EditorStyles.toolbarButton)) Jump(15);
        if (GUILayout.Button("Trono",EditorStyles.toolbarButton)) Jump(HeroPossessedSequence.CameraStopAt);
        if (GUILayout.Button("Relâmpago",EditorStyles.toolbarButton)) Jump(HeroPossessedSequence.FirstLightningAt+.08f);
        if (GUILayout.Button("Fade",EditorStyles.toolbarButton)) Jump(presentation.Sequence.FadeOutAt+1.5f);
        if (GUILayout.Button("Preto",EditorStyles.toolbarButton)) Jump(presentation.Sequence.BlackAt);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0")+" s",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0,24,position.width,position.height-24));
    }
}
