using System;
using System.IO;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class UltimateValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Ultimates")]
    public static void Run()
    {
        foreach(string resource in UltimateSpriteTimeline.Atlases)
        {
            string path="PixelArt/Ultimates/"+resource;
            var texture=Resources.Load<Texture2D>(path);
            if(texture==null || texture.filterMode!=FilterMode.Point) throw new InvalidOperationException("Ultimate pixel sprite missing: "+resource);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            if(importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed)
                throw new InvalidOperationException("Ultimate sprite must preserve source pixels: "+resource);
            if(new UltimateSpriteSheet(resource).FrameCount!=16) throw new InvalidOperationException("Ultimate needs sixteen frames: "+resource);
            var atlas=JsonUtility.FromJson<UltimateSpriteSheet.Atlas>(Resources.Load<TextAsset>(path+"Frames").text);
            foreach(var frame in atlas.frames)
                if(frame.x<0 || frame.y<0 || frame.width<1 || frame.height<1 || frame.x+frame.width>texture.width
                    || frame.y+frame.height>texture.height || frame.pivotX<0 || frame.pivotX>frame.width
                    || frame.pivotY<0 || frame.pivotY>frame.height)
                    throw new InvalidOperationException("Frame/foot anchor escapes atlas: "+resource);
        }
        Rect home=new Rect(925,125,64,90), target=new Rect(260,155,80,110);
        Rect airborne=UltimateAnimation.ActorRect(home,target,UltimateTimeline.HangStart+.1f);
        Rect held=UltimateAnimation.ActorRect(home,target,UltimateTimeline.DiveStart-.05f);
        Rect landed=UltimateAnimation.ActorRect(home,target,UltimateTimeline.FinalImpact);
        if(airborne!=held || airborne.y>=target.y || landed.y<=held.y)
            throw new InvalidOperationException("The final cut must jump high, hold the sword behind the hero and descend.");
        if(UltimateAnimation.ActorVisible(.98f) || !UltimateAnimation.ActorVisible(UltimateTimeline.CutStart+.01f)
            || UltimateAnimation.ActorVisible(UltimateTimeline.CutStart+.14f))
            throw new InvalidOperationException("Teleport cuts must alternate disappearance and reappearance.");
        Rect bramHome=new Rect(925,281,64,90);
        Vector2 firing=UltimateAnimation.BramRect(bramHome,2).position;
        for(float time=2;time<5.65f;time+=.05f)
            if(Vector2.Distance(firing,UltimateAnimation.BramRect(bramHome,time).position)>1.7f)
                throw new InvalidOperationException("Bram must stay fixed with only a slight tremor until the beam ends.");
        Debug.Log("ULTIMATE_VALIDATION_OK: 128 sprite frames, native imports, atlas/foot anchors, teleport cuts, aerial finish and cinematic timing.");
    }

    [MenuItem("Tools/Dragon Quest/Preview Ultimates")]
    public static void Preview()
    {
        var window=EditorWindow.GetWindow<UltimatePreviewWindow>();
        window.titleContent=new GUIContent("Ultimates"); window.minSize=new Vector2(800,520); window.Show();
    }

    // Offscreen GPU frames use the same sprite draw commands and clock as the in-game animation.
    [MenuItem("Tools/Dragon Quest/Export Ultimate Sprite Preview")]
    public static void Capture()
    {
        Run();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/UltimateSpritePreview"));
        Directory.CreateDirectory(output);
        var material=new Material(Shader.Find("Sprites/Default"));
        var target=new RenderTexture(1280,420,0,RenderTextureFormat.ARGB32);
        var pixels=new Texture2D(1280,420,TextureFormat.RGBA32,false);
        var previous=RenderTexture.active;
        Action<Texture2D,Rect,Rect,Color> draw=(texture,rect,uv,tint)=>
        {
            material.mainTexture=texture; material.SetPass(0);
            GL.Begin(GL.QUADS); GL.Color(tint);
            GL.TexCoord2(uv.x,uv.yMax); GL.Vertex3(rect.x,rect.y,0);
            GL.TexCoord2(uv.xMax,uv.yMax); GL.Vertex3(rect.xMax,rect.y,0);
            GL.TexCoord2(uv.xMax,uv.y); GL.Vertex3(rect.xMax,rect.yMax,0);
            GL.TexCoord2(uv.x,uv.y); GL.Vertex3(rect.x,rect.yMax,0);
            GL.End();
        };
        var animation=new UltimateAnimation(draw,false); animation.Prepare();
        var party=new[]{new Rect(925,125,64,90),new Rect(985,203,64,90),new Rect(925,281,64,90)};
        var enemies=new[]{new Rect(260,155,80,110),new Rect(405,227,80,110)};
        var idle=new[]{new UltimateSpriteSheet("ArenOmnislash",draw),new UltimateSpriteSheet("LiaGospel",draw),new UltimateSpriteSheet("BramCatastrophe",draw)};
        var abilities=new[]{HeroUltimates.Omnislash,HeroUltimates.Salvation,HeroUltimates.Aegis};
        var background=Resources.Load<Texture2D>("BattleBackgrounds/city");
        var opponents=new[]{Resources.Load<Texture2D>("PixelArt/GolemBattle"),Resources.Load<Texture2D>("PixelArt/WolfBattle")};
        var opponentUvs=new Rect[2];
        var opponentAreas=new Rect[2];
        for(int enemy=0;enemy<2;enemy++)
        {
            Texture2D texture=opponents[enemy]; var colors=texture.GetPixels32();
            int left=texture.width,right=0,bottom=texture.height,top=0;
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width/4;x++)
                if(colors[y*texture.width+x].a>=128)
                { left=Mathf.Min(left,x); right=Mathf.Max(right,x); bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y); }
            opponentUvs[enemy]=new Rect((float)left/texture.width,(float)bottom/texture.height,
                (float)(right-left+1)/texture.width,(float)(top-bottom+1)/texture.height);
            float width=enemies[enemy].height*(right-left+1)/(top-bottom+1);
            opponentAreas[enemy]=new Rect(enemies[enemy].center.x-width*.5f,enemies[enemy].y,width,enemies[enemy].height);
        }
        try
        {
            target.Create(); RenderTexture.active=target;
            for(int hero=0;hero<abilities.Length;hero++)
            {
                string folder=Path.Combine(output,new[]{"Aren","Lia","Bram"}[hero]); Directory.CreateDirectory(folder);
                var ability=abilities[hero];
                for(int index=0;index<Mathf.CeilToInt(UltimateTimeline.Duration(ability)*24);index++)
                {
                    float time=index/24f;
                    GL.Clear(true,true,new Color(.025f,.04f,.08f)); GL.PushMatrix(); GL.LoadPixelMatrix(0,1280,420,0);
                    if(background!=null)
                    {
                        float crop=Mathf.Min(1,(float)background.width/background.height/(1240f/396));
                        draw(background,new Rect(20,20,1240,396),new Rect(0,(1-crop)*.5f,1,crop),Color.white);
                    }
                    animation.DrawBackdrop(ability,time,party[hero],party);
                    for(int member=0;member<3;member++)
                    {
                        if(member==hero) animation.DrawActor(party[member],enemies[0],"preview",ability,time,Color.white,null,null);
                        else idle[member].DrawActor(party[member],member==1?15:0,Color.white);
                    }
                    for(int enemy=0;enemy<2;enemy++) draw(opponents[enemy],opponentAreas[enemy],opponentUvs[enemy],Color.white);
                    animation.DrawEffects(ability,time,party[hero],enemies[0],enemies,party,new[]{true,false,false});
                    GL.PopMatrix();
                    pixels.ReadPixels(new Rect(0,0,1280,420),0,0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,index.ToString("D4")+".png"),pixels.EncodeToPNG());
                }
            }
            Debug.Log("ULTIMATE_SPRITE_CAPTURES_OK: "+output);
        }
        finally
        {
            RenderTexture.active=previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(material);
        }
    }

}

public sealed class UltimatePreviewWindow : EditorWindow
{
    private UltimatePreviewRenderer renderer;
    private int hero;
    private float elapsed;
    private bool playing;
    private double last;
    private AbilityDefinition Ability => hero==0?HeroUltimates.Omnislash:hero==1?HeroUltimates.Salvation:HeroUltimates.Aegis;
    private void OnEnable() { last=EditorApplication.timeSinceStartup; EditorApplication.update+=Tick; }
    private void OnDisable() { EditorApplication.update-=Tick; }
    private void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        if(playing) { elapsed+=(float)(now-last); if(elapsed>UltimateTimeline.Duration(Ability)) { elapsed=0; playing=false; } Repaint(); }
        last=now;
    }
    private void OnGUI()
    {
        int selected=GUI.Toolbar(new Rect(10,8,360,28),hero,new[]{"Aren / Omnislash","Lia / Aurora","Bram / Bastiao"});
        if(selected!=hero) { hero=selected; elapsed=0; }
        if(GUI.Button(new Rect(385,8,100,28),playing?"Pausar":"Reproduzir")) { if(!playing) elapsed=0; playing=!playing; }
        elapsed=GUI.HorizontalSlider(new Rect(505,18,Mathf.Max(80,position.width-535),20),elapsed,0,UltimateTimeline.Duration(Ability));
        float scale=Mathf.Min(position.width/1280f,(position.height-48)/720f);
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=Matrix4x4.TRS(new Vector3((position.width-1280*scale)*.5f,45,0),Quaternion.identity,new Vector3(scale,scale,1));
        if(renderer==null) renderer=new UltimatePreviewRenderer();
        renderer.Draw(Ability,elapsed); GUI.matrix=old;
    }
}
