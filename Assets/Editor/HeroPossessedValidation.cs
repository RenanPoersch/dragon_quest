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
            HeroPossessedPresentation.LightningAResource, HeroPossessedPresentation.LightningBResource,
            HeroPossessedPresentation.LightningCResource, HeroPossessedPresentation.LightningDResource })
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
        ValidateRain(shader);
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
        Assert(!string.IsNullOrWhiteSpace(EndingNarrative.KingFreedEpilogue)
            && !string.IsNullOrWhiteSpace(EndingNarrative.CastleDestroyedEpilogue)
            && !string.IsNullOrWhiteSpace(EndingNarrative.HeroPossessedEpilogue), "the current ending narratives remain available");
        Debug.Log("HERO_POSSESSED_VALIDATION_OK: animated background rain, four storm sprites, natural spirit RGB with slight transparency, expanded sky, locked composition, foreground protection, narrative and fade.");
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
        material.SetFloat("_SpiritOpacity",HeroPossessedPresentation.SpiritOpacity);
        material.SetFloat("_RoomAspect",throne.width/(float)throne.height);
        material.SetVector("_LightningRect",new Vector4(.28f,HeroPossessedSequence.LightningTopY-HeroPossessedSequence.LightningHeight/2,
            HeroPossessedSequence.LightningWidth,HeroPossessedSequence.LightningHeight));
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
            Color32[] first=null;
            foreach(string resource in new[]{HeroPossessedPresentation.LightningAResource,HeroPossessedPresentation.LightningBResource,
                HeroPossessedPresentation.LightningCResource,HeroPossessedPresentation.LightningDResource})
            {
                material.SetTexture("_LightningTex",Resources.Load<Texture2D>(resource));
                material.SetFloat("_Lightning",1); Graphics.Blit(throne,target,material);
                RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
                Color32[] lit=readback.GetPixels32();
                var stopped=new HeroPossessedSequence(28);stopped.Advance(HeroPossessedSequence.CameraStopAt);
                Rect camera=HeroPossessedPresentation.CameraBounds(stopped,throne.width/(float)throne.height);
                int topRow=Mathf.RoundToInt(throne.height*(1+(camera.y+30)/camera.height));
                int changed=0,alternate=0,visibleFace=0,visibleSky=0,visibleTop=0;
                for(int y=0;y<target.height;y++) for(int x=0;x<target.width;x++)
                {
                    int i=y*target.width+x;
                    bool same=dark[i].Equals(lit[i]);
                    if(!same) changed++;
                    if(first!=null && !lit[i].Equals(first[i])) alternate++;
                    bool face=x>target.width*.435f && x<target.width*.565f && y>throne.height*.72f && y<throne.height*.79f;
                    bool torso=x>target.width*.38f && x<target.width*.62f && y>throne.height*.52f && y<throne.height*.70f;
                    if(face || torso) Assert(same,"all four lightning forms remain behind Aren's face and clothing");
                    if(!same && x>target.width*.45f && x<target.width*.55f && y>throne.height*.89f && y<throne.height*.97f) visibleFace++;
                    if(!same && y>throne.height) visibleSky++;
                    if(Math.Abs(y-topRow)<=1 && x>target.width*.18f && x<target.width*.38f && lit[i].r>dark[i].r+20) visibleTop++;
                }
                Assert(changed>1000,"lightning reveals the naturally colored spirit");
                Assert(first==null || alternate>100,"each new lightning shape produces a distinct rendered frame");
                Assert(visibleFace>500 && visibleSky>100,"the frontal face and upper horns appear in the expanded black recess");
                Assert(visibleTop>0,"each lightning channel reaches the top edge without its transparent padding leaving a gap");
                if(first==null) first=lit;
            }
            ValidateNaturalColor(material,throne,target,readback);
        }
        finally
        {
            RenderTexture.active=previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(material);
        }
    }
    private static void ValidateRain(Shader shader)
    {
        var throne=Resources.Load<Texture2D>(HeroPossessedPresentation.ThroneResource);
        var material=new Material(shader);
        material.SetFloat("_SkyPadding",HeroPossessedPresentation.SkyPadding);
        material.SetFloat("_SpiritOpacity",0);
        material.SetTexture("_LightningTex",Texture2D.blackTexture);
        int height=Mathf.RoundToInt(throne.height*(1+HeroPossessedPresentation.SkyPadding));
        var target=new RenderTexture(throne.width,height,0,RenderTextureFormat.ARGB32);
        var readback=new Texture2D(throne.width,height,TextureFormat.RGBA32,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            material.SetFloat("_RainOpacity",0);
            Color32[] dry=ReadComposite(material,throne,target,readback);
            material.SetFloat("_RainOpacity",HeroPossessedPresentation.RainOpacity);
            material.SetFloat("_RainTime",HeroPossessedSequence.CameraStopAt);
            Color32[] rainy=ReadComposite(material,throne,target,readback);
            material.SetFloat("_RainTime",HeroPossessedSequence.CameraStopAt+.2f);
            Color32[] moving=ReadComposite(material,throne,target,readback);
            Color32[] repeated=ReadComposite(material,throne,target,readback);
            material.SetFloat("_RainTime",HeroPossessedSequence.CameraStopAt);
            material.SetFloat("_Lightning",1);
            Color32[] lit=ReadComposite(material,throne,target,readback);
            int rainPixels=0,movingPixels=0,skyPixels=0,litPixels=0;
            for(int y=0;y<target.height;y++) for(int x=0;x<target.width;x++)
            {
                int i=y*target.width+x;
                bool changed=!dry[i].Equals(rainy[i]);
                bool face=x>target.width*.435f && x<target.width*.565f && y>throne.height*.72f && y<throne.height*.79f;
                bool torso=x>target.width*.38f && x<target.width*.62f && y>throne.height*.52f && y<throne.height*.70f;
                if(face || torso || y<throne.height*.52f || Math.Max(dry[i].r,Math.Max(dry[i].g,dry[i].b))>16)
                    Assert(!changed && dry[i].Equals(moving[i]),"rain stays behind Aren, the throne, floor and bright architecture");
                Assert(moving[i].Equals(repeated[i]),"the same sequence clock reproduces the rain regardless of render count");
                if(changed)
                {
                    rainPixels++;
                    Assert(rainy[i].b-dry[i].b+1>=rainy[i].g-dry[i].g && rainy[i].g-dry[i].g+1>=rainy[i].r-dry[i].r,
                        "rain keeps a subtle blue-grey color");
                    if(y>throne.height) skyPixels++;
                    if(lit[i].b>rainy[i].b+2) litPixels++;
                }
                if(!moving[i].Equals(rainy[i])) movingPixels++;
            }
            Assert(rainPixels>1000 && skyPixels>200,"rain is visible in the black recess and expanded sky without lightning");
            Assert(movingPixels>rainPixels/2,"rain continues falling between lightning flashes");
            Assert(litPixels>rainPixels/2,"lightning illuminates the falling rain");
        }
        finally
        {
            RenderTexture.active=previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(material);
        }
    }
    private static Color32[] ReadComposite(Material material,Texture2D room,RenderTexture target,Texture2D readback)
    {
        Graphics.Blit(room,target,material); RenderTexture.active=target;
        readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
        return readback.GetPixels32();
    }
    private static void ValidateNaturalColor(Material material,Texture2D room,RenderTexture target,Texture2D readback)
    {
        var color=new Texture2D(1,1,TextureFormat.RGBA32,false);
        try
        {
            color.SetPixel(0,0,new Color(.12f,.42f,.90f,1)); color.Apply();
            material.SetTexture("_SpiritTex",color); material.SetTexture("_LightningTex",Texture2D.blackTexture);
            int x=target.width/2,y=Mathf.RoundToInt(room.height*1.02f);
            material.SetFloat("_SpiritOpacity",1);
            Color opaque=SampleColor(material,room,target,readback,x,y);
            material.SetFloat("_SpiritOpacity",HeroPossessedPresentation.SpiritOpacity);
            Color translucent=SampleColor(material,room,target,readback,x,y);
            Assert(opaque.b>opaque.r*3 && opaque.g>opaque.r*2,"the compositor keeps the original RGB instead of replacing it with grey");
            Assert(translucent.b<opaque.b*.95f && translucent.b>opaque.b*.55f,"the spirit is slightly transparent, not opaque or almost invisible");
        }
        finally { UnityEngine.Object.DestroyImmediate(color); }
    }
    private static Color SampleColor(Material material,Texture2D room,RenderTexture target,Texture2D readback,int x,int y)
    {
        Graphics.Blit(room,target,material); RenderTexture.active=target;
        readback.ReadPixels(new Rect(x,y,1,1),x,y); readback.Apply(); return readback.GetPixel(x,y);
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
    { presentation?.Dispose(); presentation=new HeroPossessedPresentation(HeroPossessedSequence.DefaultStormSeed); presentation.Sequence.Advance(seconds); playing=false; }
    private void OnGUI()
    {
        if (presentation==null) Jump(HeroPossessedSequence.CameraStopAt);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(playing?"Pausar":"Animar",EditorStyles.toolbarButton)) playing=!playing;
        if (GUILayout.Button("Tapete",EditorStyles.toolbarButton)) Jump(1.4f);
        if (GUILayout.Button("Escadas",EditorStyles.toolbarButton)) Jump(15);
        if (GUILayout.Button("Trono",EditorStyles.toolbarButton)) Jump(HeroPossessedSequence.CameraStopAt);
        if (GUILayout.Button("Rajada",EditorStyles.toolbarButton)) Jump(presentation.Sequence.StormFlashPeakAt(0));
        if (GUILayout.Button("Outro local",EditorStyles.toolbarButton)) Jump(presentation.Sequence.StormFlashPeakAt(Math.Min(5,presentation.Sequence.StormFlashCount-1)));
        if (GUILayout.Button("Fade",EditorStyles.toolbarButton)) Jump(presentation.Sequence.FadeOutAt+1.5f);
        if (GUILayout.Button("Preto",EditorStyles.toolbarButton)) Jump(presentation.Sequence.BlackAt);
        GUILayout.Label(presentation.Sequence.Elapsed.ToString("0.0")+" s",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        presentation.Draw(new Rect(0,24,position.width,position.height-24));
    }
}
