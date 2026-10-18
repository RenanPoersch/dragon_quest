using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Audio;
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
    private static int checks, step;
    private static double nextAt, began;
    private static PrototypeSaveController saves;
    private static PrototypeBattleController battle;
    private static PrototypeMusicController music;
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
            AssetDatabase.ImportAsset("Assets/Resources/" + resource + ".ogg", ImportAssetOptions.ForceUpdate);
            AudioClip clip = Resources.Load<AudioClip>(resource);
            Check(clip != null, resource + " exists");
            Check(clip.channels == 2 && clip.frequency == 44100 && clip.length > 160 && clip.length < 180,
                "the full supplied intro is imported in stereo at 44.1 kHz");
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming
                && !importer.defaultSampleSettings.preloadAudioData && !importer.forceToMono,
                "intro streams without preloading the full PCM clip");
        }
        Check(MusicCuePolicy.Select(true,true,false,false,false,false,false,false,"world") == MusicCue.Intro, "intro takes priority over front end");
        Check(MusicCuePolicy.Select(false,true,false,false,false,false,false,false,"world") == MusicCue.Silence, "title and load stay silent");
        Check(MusicCuePolicy.Select(false,false,true,true,false,true,false,false,"castle") == MusicCue.Silence, "ending stays silent");
        Check(Enum.GetValues(typeof(MusicCue)).Cast<MusicCue>().Count(MusicCuePolicy.IsAvailable) == 1, "only the supplied intro is enabled");
        Check(Resources.LoadAll<AudioClip>("Music").Length == 1, "rejected replacement compositions are absent");
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
    private static void Page(string page)
    {
        var type = typeof(PrototypeSaveController).GetNestedType("MenuPage", BindingFlags.NonPublic);
        Call(saves,"OpenPage",Enum.Parse(type,page));
    }
    private static AudioSource IntroVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "intro");
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
        if (now < nextAt) return;
        try
        {
            switch (step)
            {
                case 0:
                    saves = UnityEngine.Object.FindAnyObjectByType<PrototypeSaveController>();
                    battle = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleController>();
                    music = UnityEngine.Object.FindAnyObjectByType<PrototypeMusicController>();
                    Check(saves != null && battle != null && music != null, "scene wires music dependencies");
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
                    break;
                case 6: Silent(); battle.StartBattle(); break;
                case 7:
                    Silent(); music.enabled = false;
                    Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying), "disabling the controller stops audio");
                    Finish(null); return;
            }
            step++; nextAt = now + 1.15;
        }
        catch (Exception error) { Finish(error); }
    }
    private static void Finish(Exception error)
    {
        SessionState.SetBool(Running,false); EditorApplication.update -= Tick;
        if (error != null) Debug.LogException(error);
        else Debug.Log("MUSIC PLAYBACK PASS: " + checks + " checks for supplied intro, loop, fades and silent front end.");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
