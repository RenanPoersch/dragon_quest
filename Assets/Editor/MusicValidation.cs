using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MusicValidation
{
    private const string Running = "DragonQuest.MusicPlaybackValidation";
    private const string CheckCount = Running + ".Checks";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks, step, nextFrame;
    private static double nextAt, began;
    private static PrototypeSaveController saves;
    private static PrototypeBattleController battle;
    private static PrototypeMusicController music;
    private static PrototypeLocations maps;
    private static PrototypeIntroPresentation intro;

    static MusicValidation()
    {
        if (!SessionState.GetBool(Running, false)) return;
        checks = SessionState.GetInt(CheckCount, 0);
        EditorApplication.update += Tick;
    }
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new InvalidOperationException("Music: " + message); }

    [MenuItem("Tools/Dragon Quest/Validate Music Assets")]
    public static void Run()
    {
        checks = 0;
        foreach (MusicCue cue in Enum.GetValues(typeof(MusicCue)))
        {
            if (!MusicCuePolicy.IsAvailable(cue)) continue;
            string resource = MusicCuePolicy.ResourceName(cue);
            AssetDatabase.ImportAsset("Assets/Resources/" + resource + ".wav", ImportAssetOptions.ForceUpdate);
            AudioClip clip = Resources.Load<AudioClip>(resource);
            Check(clip != null, resource + " exists");
            float expectedSeconds = cue switch
            {
                MusicCue.Intro => 171.1543f,
                MusicCue.Battle => 166.1156f,
                MusicCue.Victory => 54.1257f,
                MusicCue.World => 410.4591f,
                _ => throw new InvalidOperationException("Missing source duration for " + cue)
            };
            Check(clip.channels == 2 && clip.frequency == 44100 && Math.Abs(clip.length-expectedSeconds)<.05f,
                resource + " preserves the full supplied recording in stereo at 44.1 kHz");
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming
                && importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM
                && !importer.defaultSampleSettings.preloadAudioData && !importer.forceToMono,
                resource + " streams the original PCM audio without lossy recompression or full preloading");
        }
        Check(MusicCuePolicy.Select(true,true,false,false,false,false,false,false,"world") == MusicCue.Intro, "intro takes priority over front end");
        Check(MusicCuePolicy.Select(false,true,false,false,false,false,false,false,"world") == MusicCue.Silence, "title and load stay silent");
        Check(MusicCuePolicy.Select(false,false,true,true,false,true,false,false,"castle") == MusicCue.Silence, "ending stays silent");
        Check(Enum.GetValues(typeof(MusicCue)).Cast<MusicCue>().Count(MusicCuePolicy.IsAvailable) == 4, "only supplied intro, battle, victory and world are enabled");
        Check(Resources.LoadAll<AudioClip>("Music").Length == 4, "only supplied recordings are present");
        foreach (string map in new[] { "world","city","west-forest","east-forest","ancient-ruins" })
            Check(MusicCuePolicy.Select(false,false,false,true,false,false,false,false,map) == MusicCue.Battle,
                "normal battles share the supplied theme in " + map);
        Check(MusicCuePolicy.Select(false,false,false,true,true,false,false,false,"volcano") == MusicCue.Dragon,
            "dragon keeps its own cue");
        Check(MusicCuePolicy.Select(false,false,false,true,false,true,false,false,"castle") == MusicCue.FinalBoss,
            "final boss keeps its own cue");
        Debug.Log("MUSIC ASSETS PASS: " + checks + " checks.");
    }

    // Use the isolated project prepared by Tools/Test-Music.ps1. No saves are written.
    public static void RunPlayback()
    {
        Run();
        SessionState.SetInt(CheckCount, checks);
        SessionState.SetBool(Running, true);
        EditorSceneManager.OpenScene("Assets/Scenes/CidadePrototipo.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static object Field(object instance, string field) => instance.GetType().GetField(field, Private).GetValue(instance);
    private static void Call(object instance, string method, params object[] args)
        => instance.GetType().GetMethod(method, Private).Invoke(instance, args);
    private static void Set(object instance, string property, object value)
        => instance.GetType().GetField("<"+property+">k__BackingField",Private).SetValue(instance,value);
    private static void Page(string page)
    {
        var type = typeof(PrototypeSaveController).GetNestedType("MenuPage", BindingFlags.NonPublic);
        Call(saves,"OpenPage",Enum.Parse(type,page));
    }
    private static AudioSource IntroVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "intro");
    private static AudioSource BattleVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "battle" && s.isPlaying);
    private static AudioSource VictoryVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "victory" && s.isPlaying);
    private static AudioSource WorldVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "world" && s.isPlaying);
    private static void BattlePlays()
    {
        Check(music.CurrentCue == MusicCue.Battle, "battle theme is selected at step " + step);
        Check(BattleVoice.timeSamples > 0 && BattleVoice.loop && BattleVoice.volume > 0 && BattleVoice.spatialBlend == 0,
            "battle audio plays and advances in a 2D loop at step " + step + " (samples=" + BattleVoice.timeSamples
            + ", loop=" + BattleVoice.loop + ", volume=" + BattleVoice.volume + ", spatial=" + BattleVoice.spatialBlend
            + ", state=" + BattleVoice.clip.loadState + ")");
    }
    private static void ClearCombat()
    {
        Set(battle,"IsActive",false); Set(battle,"IsPostBattleOpen",false); Set(battle,"HasEnded",false);
    }
    private static void VictoryPlays()
    {
        Check(battle.IsPostBattleOpen && music.CurrentCue == MusicCue.Victory,"victory theme follows the real results screen");
        Check(VictoryVoice.timeSamples > 0 && VictoryVoice.loop && VictoryVoice.volume > 0 && VictoryVoice.spatialBlend == 0,
            "supplied victory recording plays and advances in a 2D loop");
        Check(music.GetComponentsInChildren<AudioSource>().All(s => s.clip?.name != "battle" || !s.isPlaying),
            "battle theme stops after transitioning to victory");
    }
    private static void WorldPlays()
    {
        Check(!battle.IsActive && maps.LocationId == "world" && music.CurrentCue == MusicCue.World,
            "world theme follows exploration at step " + step);
        Check(WorldVoice.timeSamples > 0 && WorldVoice.loop && WorldVoice.volume > 0 && WorldVoice.spatialBlend == 0,
            "supplied world recording plays and advances in a 2D loop");
        Check(music.GetComponentsInChildren<AudioSource>().All(s => s.clip?.name == "world" || !s.isPlaying),
            "other music stops after returning to the world");
    }
    private static void Restore(string map)
    {
        var definition = new PrototypeTravel().Maps.First(m => m.Id == map);
        maps.Restore(map,definition.Spawn);
    }
    private static void Silent()
    {
        Check(music.CurrentCue == MusicCue.Silence, "expected silence at step " + step);
        Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying), "both music sources stop at step " + step);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        double now = EditorApplication.timeSinceStartup;
        if (began == 0) { began = now; nextAt = now + 2; }
        if (now - began > 100) { Finish(new Exception("Music playback timed out")); return; }
        // Resume only this isolated test if startup diagnostics trigger Console Error Pause.
        if (EditorApplication.isPaused) { EditorApplication.isPaused = false; nextAt = now + 1; return; }
        if (now < nextAt || Time.frameCount < nextFrame) return;
        try
        {
            switch (step)
            {
                case 0:
                    saves = UnityEngine.Object.FindAnyObjectByType<PrototypeSaveController>();
                    battle = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleController>();
                    music = UnityEngine.Object.FindAnyObjectByType<PrototypeMusicController>();
                    maps = UnityEngine.Object.FindAnyObjectByType<PrototypeLocations>();
                    Check(saves != null && battle != null && music != null && maps != null, "scene wires music dependencies");
                    Check(music.CurrentCue == MusicCue.Intro, "intro is selected in the real scene");
                    Check(music.GetComponentsInChildren<AudioSource>().Length == 2, "only two music sources");
                    intro = (PrototypeIntroPresentation)Field(saves,"intro");
                    // Batch mode has no visible Game view: simulate its first rendered frame.
                    typeof(PrototypeIntroPresentation).GetField("hasDrawn",Private).SetValue(intro,true);
                    break;
                case 1:
                    Check(IntroVoice.isPlaying && IntroVoice.timeSamples > 0, "supplied track actually plays and advances");
                    Check(IntroVoice.loop && IntroVoice.spatialBlend == 0 && IntroVoice.volume > 0, "audible 2D loop follows intro fade");
                    IntroVoice.timeSamples = IntroVoice.clip.samples - IntroVoice.clip.frequency / 3;
                    break;
                case 2:
                    Check(IntroVoice.isPlaying && IntroVoice.timeSamples < IntroVoice.clip.frequency * 2, "full-length clip wraps to its beginning");
                    Check(intro.Sequence.TrySkip(), "intro can be skipped during playback");
                    break;
                case 3: Silent(); Page("Continue"); break;
                case 4: Silent(); Page("Title"); Call(saves,"StartNewGameVoyage"); break;
                case 5:
                    Silent();
                    Check(saves.IsFrontEndVisible, "new game voyage remains a front-end screen");
                    ((NewGameVoyagePresentation)Field(saves,"voyage")).Dispose();
                    typeof(PrototypeSaveController).GetField("voyage",Private).SetValue(saves,null);
                    typeof(PrototypeSaveController).GetField("<IsMenuVisible>k__BackingField",Private).SetValue(saves,false);
                    saves.enabled = false; battle.enabled = false;
                    maps.enabled = false; battle.SetRandomEncountersEnabled(true);
                    Restore("world");
                    break;
                case 6:
                    WorldPlays();
                    Check(WorldVoice.timeSamples < WorldVoice.clip.frequency*2,"entering the world starts its music from the beginning");
                    WorldVoice.timeSamples = WorldVoice.clip.samples - WorldVoice.clip.frequency/3;
                    break;
                case 7:
                    WorldPlays();
                    Check(WorldVoice.timeSamples < WorldVoice.clip.frequency*2,"full world recording loops to its beginning");
                    WorldVoice.timeSamples = WorldVoice.clip.frequency*15;
                    break;
                case 8:
                    WorldPlays();
                    Check(WorldVoice.timeSamples > WorldVoice.clip.frequency*15,"ongoing exploration does not restart world music");
                    battle.StartBattle();
                    break;
                case 9:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"battle replaces the world theme from the beginning");
                    BattleVoice.timeSamples = BattleVoice.clip.samples - BattleVoice.clip.frequency/3;
                    break;
                case 10:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"full battle track loops to its beginning");
                    BattleVoice.timeSamples = BattleVoice.clip.frequency*15;
                    break;
                case 11:
                    BattlePlays();
                    Check(BattleVoice.timeSamples > BattleVoice.clip.frequency*15,"unchanged combat does not restart the track");
                    Set(battle.Session,"Outcome",BattleOutcome.Victory);
                    Call(battle,"AwardIfComplete"); Call(battle,"Update");
                    Check(battle.PostBattle.IsComplete && battle.IsPostBattleOpen,"winning opens the real post-battle results");
                    break;
                case 12:
                    VictoryPlays();
                    Check(VictoryVoice.timeSamples < VictoryVoice.clip.frequency*2,"post-victory music starts from the beginning");
                    VictoryVoice.timeSamples = VictoryVoice.clip.samples - VictoryVoice.clip.frequency/3;
                    break;
                case 13:
                    VictoryPlays();
                    Check(VictoryVoice.timeSamples < VictoryVoice.clip.frequency*2,"full victory recording loops to its beginning");
                    VictoryVoice.timeSamples = VictoryVoice.clip.frequency*15;
                    break;
                case 14:
                    VictoryPlays();
                    Check(VictoryVoice.timeSamples > VictoryVoice.clip.frequency*15,"remaining on results does not restart victory music");
                    Set(battle,"PostBattleStartedAt",Time.unscaledTime-10);
                    battle.ReturnToCity();
                    Check(!battle.IsActive && !battle.IsPostBattleOpen,"continuing closes the results screen");
                    break;
                case 15:
                    WorldPlays();
                    Check(WorldVoice.timeSamples < WorldVoice.clip.frequency*2,"returning from victory restarts the original world recording");
                    Restore("city");
                    break;
                case 16:
                    Silent(); Restore("world");
                    break;
                case 17:
                    WorldPlays();
                    Check(WorldVoice.timeSamples < WorldVoice.clip.frequency*2,"reentering the world after silence restarts its music");
                    Check(battle.StartRandomBattle(),"world random encounter starts");
                    break;
                case 18:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"each new encounter restarts battle music");
                    ClearCombat(); Restore("west-forest");
                    Check(battle.StartRandomBattle(),"quarry random encounter starts");
                    break;
                case 19:
                    BattlePlays(); ClearCombat(); Restore("east-forest");
                    Check(battle.StartRandomBattle(),"forest random encounter starts");
                    break;
                case 20:
                    BattlePlays(); Set(battle.Session,"Outcome",BattleOutcome.Defeat);
                    break;
                case 21:
                    Silent(); ClearCombat();
                    Call(battle,"StartEncounter",PrototypeBattleFactory.CreateDragonEncounter(battle.Party),true);
                    break;
                case 22:
                    Silent(); ClearCombat();
                    Call(battle,"StartEncounter",PrototypeCastleBattleFactory.Create(battle.Party),false);
                    break;
                case 23:
                    Silent(); ClearCombat(); Set(battle,"HasEnded",true); music.enabled = false;
                    Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying), "disabling the controller stops audio");
                    Finish(null); return;
            }
            // Editor updates can run while synchronous resources finish loading. Give the
            // game loop actual frames to apply the new cue before inspecting the sources.
            step++; nextAt = now + 1.15; nextFrame = Time.frameCount + 3;
        }
        catch (Exception error) { Finish(error); }
    }
    private static void Finish(Exception error)
    {
        SessionState.SetBool(Running,false); EditorApplication.update -= Tick;
        if (error != null) Debug.LogException(error);
        else Debug.Log("MUSIC PLAYBACK PASS: " + checks + " checks for supplied intro, normal encounters, loops, results, defeat, separate bosses and restarting tracks after transitions.");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
