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
        foreach (string resource in new[] { HeroPossessedPresentation.ThroneResource, HeroPossessedPresentation.SpiritResource,
            HeroPossessedPresentation.LightningAResource, HeroPossessedPresentation.LightningBResource })
        {
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null, "artwork exists: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp,
                "Point/Clamp preserves crisp edges: " + resource);
            Assert(!importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "no mipmaps or compression: " + resource);
            if (resource != HeroPossessedPresentation.ThroneResource) ValidateSpriteAlpha(texture);
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
            float crownY=locked.y+locked.height*.127f;
            Assert(crownY>220 && crownY<340 && locked.y+locked.height*.28f<660,
                "the higher frame keeps Aren's face and the throne below the expanded black recess");
            float ghostTop=1-HeroPossessedPresentation.SpiritRect.y-HeroPossessedPresentation.SpiritRect.w;
            Assert(locked.y+locked.height*(ghostTop+.15f*HeroPossessedPresentation.SpiritRect.w)>20
                && locked.y+locked.height*(ghostTop+.30f*HeroPossessedPresentation.SpiritRect.w)<crownY,
                "the frontal ghost's whole face fits above the crown");
            Rect composite=HeroPossessedPresentation.CompositeBounds(locked);
            Assert(composite.y<0 && Mathf.Approximately(composite.yMax,locked.yMax),
                "black sky extends beyond the painting while its perspective stays aligned");
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
        Debug.Log("HERO_POSSESSED_VALIDATION_OK: frontal transparent ghost, two alternating ray sprites, expanded sky, higher locked composition, foreground protection, narrative and fade.");
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

    private static void ValidateSpriteAlpha(Texture2D imported)
    {
        var source=new Texture2D(2,2);
        try
        {
            Assert(source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(imported))),"source sprite loads");
            Assert(source.width==imported.width && source.height==imported.height,"sprite keeps native resolution");
            int clear=0,visible=0;
            foreach(Color32 pixel in source.GetPixels32()) { if(pixel.a==0) clear++; if(pixel.a>230) visible++; }
            Assert(clear>source.width*source.height/10 && visible>1000,"real transparent alpha and a substantial sprite");
            Assert(source.GetPixel(0,0).a==0 && source.GetPixel(source.width-1,source.height-1).a==0,"no opaque rectangular matte");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    private static void ValidateLightning(Shader shader)
    {
        var throne=Resources.Load<Texture2D>(HeroPossessedPresentation.ThroneResource);
        var material=new Material(shader);
        material.SetTexture("_SpiritTex",Resources.Load<Texture2D>(HeroPossessedPresentation.SpiritResource));
        material.SetFloat("_SkyPadding",HeroPossessedPresentation.SkyPadding);
        material.SetVector("_SpiritRect",HeroPossessedPresentation.SpiritRect);
        material.SetTexture("_LightningTex",Resources.Load<Texture2D>(HeroPossessedPresentation.LightningAResource));
        int height=Mathf.RoundToInt(throne.height*(1+HeroPossessedPresentation.SkyPadding));
        var target=new RenderTexture(throne.width,height,0,RenderTextureFormat.ARGB32);
        var readback=new Texture2D(throne.width,height,TextureFormat.RGBA32,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            material.SetFloat("_Lightning",0); Graphics.Blit(throne,target,material);
            RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            Color32[] dark=readback.GetPixels32();
            material.SetFloat("_Lightning",1); Graphics.Blit(throne,target,material);
            RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            Color32[] lit=readback.GetPixels32();
            material.SetTexture("_LightningTex",Resources.Load<Texture2D>(HeroPossessedPresentation.LightningBResource));
            Graphics.Blit(throne,target,material);
            RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            Color32[] second=readback.GetPixels32();
            int changed=0,alternate=0,visibleFace=0,visibleSky=0;
            for(int y=0;y<target.height;y++) for(int x=0;x<target.width;x++)
            {
                int i=y*target.width+x;
                bool same=dark[i].Equals(lit[i]);
                if(!same) changed++;
                if(!lit[i].Equals(second[i])) alternate++;
                bool face=x>target.width*.435f && x<target.width*.565f && y>throne.height*.72f && y<throne.height*.79f;
                bool torso=x>target.width*.38f && x<target.width*.62f && y>throne.height*.52f && y<throne.height*.70f;
                if(face || torso)
                    Assert(same && dark[i].Equals(second[i]),"both lightning frames remain behind Aren's face and clothing");
                if(!same && x>target.width*.45f && x<target.width*.55f && y>throne.height*.89f && y<throne.height*.97f) visibleFace++;
                if(!same && y>throne.height) visibleSky++;
            }
            Assert(changed>1000,"lightning reveals a substantial spirit silhouette in the black recess");
            Assert(alternate>100,"the two lightning sprites produce distinct rendered frames");
            Assert(visibleFace>500 && visibleSky>100,"the frontal face and upper horns appear in the expanded black recess");
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
        if (GUILayout.Button("Raio A",EditorStyles.toolbarButton)) Jump(HeroPossessedSequence.FirstLightningAt+.08f);
        if (GUILayout.Button("Raio B",EditorStyles.toolbarButton)) Jump(HeroPossessedSequence.FirstLightningAt+.48f);
        if (GUILayout.Button("Fade",EditorStyles.toolbarButton)) Jump(presentation.Sequence.FadeOutAt+1.5f);
        if (GUILayout.Button("Preto",EditorStyles.toolbarButton)) Jump(presentation.Sequence.BlackAt);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0")+" s",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0,24,position.width,position.height-24));
    }
}
