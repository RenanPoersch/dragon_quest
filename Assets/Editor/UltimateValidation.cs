using System;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class UltimateValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Ultimates")]
    public static void Run()
    {
        foreach(string resource in new[]{"ArenBattle","LiaBattle","BramBattle","GolemBattle","WolfBattle","Cast/LiaCast"})
        {
            var texture=Resources.Load<Texture2D>("PixelArt/"+resource);
            if(texture==null || !texture.isReadable) throw new InvalidOperationException("Ultimate sprite missing or unreadable: "+resource);
        }
        Rect home=new Rect(925,125,64,90), target=new Rect(260,155,80,110);
        Rect airborne=UltimateAnimation.ActorRect(home,target,3.95f);
        Rect held=UltimateAnimation.ActorRect(home,target,4.2f);
        Rect landed=UltimateAnimation.ActorRect(home,target,UltimateTimeline.FinalImpact);
        if(airborne!=held || airborne.y>=target.y || UltimateAnimation.SwordPose(3.95f)!=1 || landed.y<=held.y)
            throw new InvalidOperationException("The final cut must jump high, hold the sword behind the hero and descend.");
        if(UltimateAnimation.ActorVisible(.95f) || !UltimateAnimation.ActorVisible(1.08f) || UltimateAnimation.ActorVisible(1.2f))
            throw new InvalidOperationException("Teleport cuts must alternate disappearance and reappearance.");
        Debug.Log("ULTIMATE_VALIDATION_OK: readable hero poses, teleport visibility, high jump, sword wind-up, airborne hold and dive.");
    }

    [MenuItem("Tools/Dragon Quest/Preview Ultimates")]
    public static void Preview()
    {
        var window=EditorWindow.GetWindow<UltimatePreviewWindow>();
        window.titleContent=new GUIContent("Ultimates"); window.minSize=new Vector2(800,520); window.Show();
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
