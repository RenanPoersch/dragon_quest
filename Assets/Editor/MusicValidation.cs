using System;
using System.Collections.Generic;
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
            float expectedSeconds = cue == MusicCue.Intro ? 171.1543f : 166.1156f;
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
        Check(Enum.GetValues(typeof(MusicCue)).Cast<MusicCue>().Count(MusicCuePolicy.IsAvailable) == 2, "only supplied intro and battle are enabled");
        Check(Resources.LoadAll<AudioClip>("Music").Length == 2, "rejected replacement compositions are absent");
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
    private static AudioSource CurrentVoice => ((AudioSource[])Field(music,"sources"))[(int)Field(music,"current")];
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
                    break;
                case 6: Silent(); battle.StartBattle(); break;
                case 7:
                    BattlePlays();
                    BattleVoice.timeSamples = BattleVoice.clip.samples - BattleVoice.clip.frequency/3;
                    break;
                case 8:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"full battle track loops to its beginning");
                    BattleVoice.timeSamples = BattleVoice.clip.frequency*15;
                    break;
                case 9:
                    BattlePlays();
                    Check(BattleVoice.timeSamples > BattleVoice.clip.frequency*15,"unchanged combat does not restart the track");
                    Set(battle,"IsPostBattleOpen",true);
                    break;
                case 10:
                    Silent(); ClearCombat(); Restore("world");
                    Check(battle.StartRandomBattle(),"world random encounter starts");
                    break;
                case 11:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"each new encounter restarts battle music");
                    ClearCombat(); Restore("west-forest");
                    Check(battle.StartRandomBattle(),"quarry random encounter starts");
                    break;
                case 12:
                    BattlePlays(); ClearCombat(); Restore("east-forest");
                    Check(battle.StartRandomBattle(),"forest random encounter starts");
                    break;
                case 13:
                    BattlePlays(); Set(battle.Session,"Outcome",BattleOutcome.Defeat);
                    break;
                case 14:
                    Silent(); ClearCombat();
                    Call(battle,"StartEncounter",PrototypeBattleFactory.CreateDragonEncounter(battle.Party),true);
                    break;
                case 15:
                    Silent(); ClearCombat();
                    Call(battle,"StartEncounter",PrototypeCastleBattleFactory.Create(battle.Party),false);
                    break;
                case 16:
                    Silent(); ClearCombat(); Set(battle,"HasEnded",true); music.enabled = false;
                    Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying), "disabling the controller stops audio");
                    // Reuse supplied audio only in this isolated test to exercise a world cue
                    // before its own recording is available. Drive transitions with routing disabled.
                    ((Dictionary<MusicCue,AudioClip>)Field(music,"clips"))[MusicCue.World] = Resources.Load<AudioClip>("Music/intro");
                    Call(music,"TransitionTo",MusicCue.World); Call(music,"Advance",1f);
                    break;
                case 17:
                    Check(CurrentVoice.isPlaying && CurrentVoice.timeSamples > 0,"world test track advances");
                    CurrentVoice.timeSamples = CurrentVoice.clip.frequency*15;
                    Call(music,"TransitionTo",MusicCue.Battle); Call(music,"Advance",1f);
                    break;
                case 18:
                    BattlePlays();
                    Call(music,"TransitionTo",MusicCue.World); Call(music,"Advance",1f);
                    break;
                case 19:
                    Check(music.CurrentCue == MusicCue.World && CurrentVoice.isPlaying && CurrentVoice.timeSamples > 0
                        && CurrentVoice.timeSamples < CurrentVoice.clip.frequency*2,"returning from battle restarts exploration music");
                    CurrentVoice.timeSamples = CurrentVoice.clip.frequency*15;
                    Call(music,"TransitionTo",MusicCue.Silence); Call(music,"Advance",1f); Silent();
                    Call(music,"TransitionTo",MusicCue.World); Call(music,"Advance",1f);
                    break;
                case 20:
                    Check(music.CurrentCue == MusicCue.World && CurrentVoice.isPlaying && CurrentVoice.timeSamples > 0
                        && CurrentVoice.timeSamples < CurrentVoice.clip.frequency*2,"returning after silence restarts exploration music");
                    Call(music,"TransitionTo",MusicCue.Silence); Call(music,"Advance",1f); Silent();
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
