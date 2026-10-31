using DragonQuest.Presentation.World;
using DragonQuest.Presentation.Cinematics.Opening;
using DragonQuest.Presentation.Cinematics.Exploration;
using DragonQuest.Presentation.Cinematics.Endings;
using DragonQuest.Presentation.Battle;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Exploration;
using DragonQuest.Content;
using DragonQuest.Presentation.Menus;
using System;
using System.Linq;
using System.Reflection;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Interactions;
using DragonQuest.Progression;
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
    private static SaveController saves;
    private static BattleController battle;
    private static MusicController music;
    private static WorldLocationController maps;
    private static IntroPresentation intro;
    private static DragonInteraction dragon;
    private static DialogueController dialogue;
    private static AudioSource preludeVoice, lairVoice, fadeVoice;
    private static float fadeVolume;
    private static InnRestController rest;
    private static EndingController endings;

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
                MusicCue.Village => 157.1527f,
                MusicCue.Dragon => 152.1371f,
                MusicCue.FinalBoss => 440.4825f,
                MusicCue.Forest => 171.1543f,
                MusicCue.Ruins => 134.1417f,
                MusicCue.Quarry => 167.1140f,
                MusicCue.Voyage => 190.1714f,
                MusicCue.Sleep => 9.1951f,
                MusicCue.Defeat => 36.1302f,
                MusicCue.DragonFreed => 299.1427f,
                MusicCue.GoodEnding => 155.1557f,
                MusicCue.MindControlEnding => 196.1622f,
                MusicCue.MeteorEnding => 241.1160f,
                MusicCue.Throne => 242.1609f,
                MusicCue.DragonLair => 8.267211f,
                _ => throw new InvalidOperationException("Missing source duration for " + cue)
            };
            Check(clip.channels == 2 && clip.frequency == 44100 && Math.Abs(clip.length-expectedSeconds)<.05f,
                resource + " preserves the intended recording segment in stereo at 44.1 kHz");
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming
                && importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM
                && !importer.defaultSampleSettings.preloadAudioData && !importer.forceToMono,
                resource + " streams the original PCM audio without lossy recompression or full preloading");
        }
        Check(MusicCuePolicy.Select(true,true,false,false,false,false,false,false,"world") == MusicCue.Intro, "intro takes priority over front end");
        Check(MusicCuePolicy.Select(false,true,false,false,false,false,false,false,"world") == MusicCue.Silence, "title and load stay silent");
        Check(Enum.GetValues(typeof(MusicCue)).Cast<MusicCue>().Count(MusicCuePolicy.IsAvailable) == 19, "all nineteen supplied recordings are enabled");
        Check(Resources.LoadAll<AudioClip>("Music").Length == 19, "only supplied recordings are present");
        Check(!MusicCuePolicy.ShouldLoop(MusicCue.Sleep),"sleep jingle plays once");
        Check(MusicCuePolicy.Select(false,true,false,false,false,false,false,false,"world",story:new MusicStoryState(voyage:true)) == MusicCue.Voyage,
            "the voyage has music while title and normal load selection stay silent");
        Check(MusicCuePolicy.Select(false,false,false,true,false,false,false,true,"world") == MusicCue.Defeat,
            "defeat has its own continue-screen theme");
        Check(MusicCuePolicy.Select(false,false,false,false,false,false,false,false,"volcano",story:new MusicStoryState(dragonDefeated:true)) == MusicCue.DragonFreed,
            "liberated dragon lair has its own aftermath theme");
        Check(MusicCuePolicy.Select(false,false,false,false,false,false,false,false,"castle") == MusicCue.Throne,
            "throne room has exploration music separate from its battle");
        foreach (GameEnding result in new[] { GameEnding.KingFreed,GameEnding.HeroPossessed,GameEnding.CastleDestroyed })
            Check(MusicCuePolicy.Select(false,false,true,false,false,false,false,false,"castle",story:new MusicStoryState(ending:result)) == MusicCuePolicy.EndingCue(result),
                "ending music follows " + result);
        Check(MusicCuePolicy.Select(false,false,false,false,false,false,false,false,"volcano") == MusicCue.DragonLair,
            "exploring the dragon lair uses the trimmed ambience");
        Check(MusicCuePolicy.Select(false,false,false,false,false,false,false,false,"volcano",true) == MusicCue.Dragon,
            "the dragon conversation replaces the lair ambience with the battle theme");
        Check(MusicCuePolicy.Select(false,true,false,false,false,false,false,false,"volcano",true) == MusicCue.Silence,
            "front-end silence takes priority over a dragon prelude");
        foreach (string map in new[] { "city","inn","item-shop","materia-shop","equipment-shop" })
            Check(MusicCuePolicy.Select(false,false,false,false,false,false,false,false,map) == MusicCue.Village,
                "village recording follows the town and its interiors in " + map);
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
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
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
        var type = typeof(JourneyMenuPage);
        Call(saves,"OpenPage",Enum.Parse(type,page));
    }
    private static AudioSource IntroVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "intro");
    private static AudioSource BattleVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "battle" && s.isPlaying);
    private static AudioSource VictoryVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "victory" && s.isPlaying);
    private static AudioSource WorldVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "world" && s.isPlaying);
    private static AudioSource VillageVoice => music.GetComponentsInChildren<AudioSource>().First(s => s.clip?.name == "village" && s.isPlaying);
    private static AudioSource Voice(MusicCue cue) => music.GetComponentsInChildren<AudioSource>()
        .First(s => s.clip?.name == cue.ToString().ToLowerInvariant() && s.isPlaying);
    private static float VoiceDuration(MusicCue cue) => Resources.Load<AudioClip>(MusicCuePolicy.ResourceName(cue)).length;
    private static void CuePlays(MusicCue cue)
    {
        Check(music.CurrentCue == cue,"expected " + cue + " at step " + step);
        AudioSource voice = Voice(cue);
        Check(voice.timeSamples > 0 && voice.loop == MusicCuePolicy.ShouldLoop(cue) && voice.volume > 0 && voice.spatialBlend == 0,
            cue + " plays the supplied recording with its intended loop setting");
        Check(music.GetComponentsInChildren<AudioSource>().All(s => s == voice || !s.isPlaying),
            "previous music stops after switching to " + cue);
    }
    private static void NearEndOfDragonSpeech()
    {
        while (dialogue.PageNumber < dialogue.PageCount) { dialogue.RevealAll(); dialogue.Advance(); }
        Check(dialogue.PageNumber == dialogue.PageCount && dialogue.RevealSecondsRemaining > 0,
            "the final dragon page is still revealing, including a short outburst");
        ((DialogueTextReveal)Field(dialogue,"reveal")).Tick(Mathf.Max(0, dialogue.RevealSecondsRemaining - .75f));
        Check(dialogue.IsTyping && dragon.IsBattlePrelude,"dragon prelude starts before the last line finishes typing");
    }
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
    private static void VillagePlays()
    {
        Check(!battle.IsActive && music.CurrentCue == MusicCue.Village,"village theme follows the town at step " + step);
        Check(VillageVoice.timeSamples > 0 && VillageVoice.loop && VillageVoice.volume > 0 && VillageVoice.spatialBlend == 0,
            "supplied village recording plays and advances in a 2D loop");
        Check(music.GetComponentsInChildren<AudioSource>().All(s => s.clip?.name == "village" || !s.isPlaying),
            "other music stops after entering the village");
    }
    private static void Restore(string map)
    {
        var definition = new WorldNavigation().Maps.First(m => m.Id == map);
        maps.Restore(map,definition.Spawn);
    }
    private static void Silent()
    {
        Check(music.CurrentCue == MusicCue.Silence, "expected silence at step " + step);
        Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying), "both music sources stop at step " + step);
    }
    private static object FrontEnd => Field(saves, "frontEnd");
    private static NewGameVoyageSequence VoyageClock => ((NewGameVoyagePresentation)Field(FrontEnd,"voyage")).Sequence;
    private static KingFreedSequence GoodClock => ((KingFreedPresentation)Field(endings,"liberation")).Sequence;
    private static HeroPossessedSequence MindClock => ((HeroPossessedPresentation)Field(endings,"possession")).Sequence;
    private static CastleDestroyedEndingPresentation MeteorScene => (CastleDestroyedEndingPresentation)Field(endings,"aftermath");
    private static CastleDestroyedSequence MeteorClock => MeteorScene.Aftermath.Sequence;
    private static bool ExpectsAudibleMusic => music.CurrentCue == MusicCue.Voyage ? saves.VoyageMusicGain > 0
        : music.CurrentCue == MusicCue.GoodEnding || music.CurrentCue == MusicCue.MindControlEnding || music.CurrentCue == MusicCue.MeteorEnding
            ? endings == null || endings.MusicGain > 0 : true;
    private static void TrackFade(MusicCue cue)
    {
        fadeVoice = Voice(cue); fadeVolume = fadeVoice.volume;
        Check(fadeVolume > .5f,"cinematic music reaches normal volume before its final fade");
        fadeVoice.timeSamples = fadeVoice.clip.frequency*15;
    }
    private static void CheckFade(MusicCue cue, float fraction)
    {
        Check(music.CurrentCue == cue,"cinematic cue remains selected during its fade");
        Check(Voice(cue) == fadeVoice && fadeVoice.timeSamples > fadeVoice.clip.frequency*15,
            "cinematic fade preserves the same playing source and advances without restarting");
        Check(Math.Abs(fadeVoice.volume-fadeVolume*fraction) < .005f,
            cue+" actual source volume reaches "+fraction+" of the original gain ("+fadeVoice.volume+")");
    }

    private static void ClearEnding()
    {
        // Reuse the real scene's ending controller without returning to the title between fixtures.
        foreach (string field in new[] { "aftermath","possession","liberation" })
        {
            ((IDisposable)Field(endings,field))?.Dispose();
            typeof(EndingController).GetField(field,Private).SetValue(endings,null);
        }
        typeof(EndingController).GetField("pages",Private).SetValue(endings,null);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        double now = EditorApplication.timeSinceStartup;
        if (began == 0) { began = now; nextAt = now + 2; }
        if (now - began > 240) { Finish(new Exception("Music playback timed out at step " + step)); return; }
        // Resume only this isolated test if startup diagnostics trigger Console Error Pause.
        if (EditorApplication.isPaused) { EditorApplication.isPaused = false; nextAt = now + 1; return; }
        if (now < nextAt || Time.frameCount < nextFrame) return;
        // Streaming can start asynchronously after a first import. Wait briefly for the
        // selected recording to advance before sampling it; failed playback still fails.
        if (music != null && music.CurrentCue != MusicCue.Silence && music.CurrentCue != MusicCue.Sleep && ExpectsAudibleMusic && now < nextAt + 5
            && !music.GetComponentsInChildren<AudioSource>().Any(s => s.clip?.name == music.CurrentCue.ToString().ToLowerInvariant()
                && s.isPlaying && s.timeSamples > 0 && s.volume > 0)) return;
        try
        {
            switch (step)
            {
                case 0:
                    saves = UnityEngine.Object.FindAnyObjectByType<SaveController>();
                    battle = UnityEngine.Object.FindAnyObjectByType<BattleController>();
                    music = UnityEngine.Object.FindAnyObjectByType<MusicController>();
                    maps = UnityEngine.Object.FindAnyObjectByType<WorldLocationController>();
                    Check(saves != null && battle != null && music != null && maps != null, "scene wires music dependencies");
                    Check(music.CurrentCue == MusicCue.Intro, "intro is selected in the real scene");
                    Check(music.GetComponentsInChildren<AudioSource>().Length == 2, "only two music sources");
                    intro = (IntroPresentation)Field(FrontEnd,"intro");
                    // Batch mode has no visible Game view: simulate its first rendered frame.
                    typeof(IntroPresentation).GetField("hasDrawn",Private).SetValue(intro,true);
                    break;
                case 1:
                    Check(IntroVoice.isPlaying && IntroVoice.timeSamples > 0, "supplied track actually plays and advances");
                    Check(IntroVoice.loop && IntroVoice.spatialBlend == 0 && IntroVoice.volume > 0, "audible 2D loop follows intro fade");
                    IntroVoice.timeSamples = IntroVoice.clip.samples - IntroVoice.clip.frequency / 3;
                    break;
                case 2:
                    Check(IntroVoice.isPlaying && IntroVoice.timeSamples < IntroVoice.clip.frequency * 2, "full-length clip wraps to its beginning");
                    intro.Sequence.Advance(IntroSequence.FadeInDuration);
                    Check(intro.Sequence.TrySkip(), "intro can be skipped during playback");
                    intro.Sequence.Advance(IntroSequence.FadeOutDuration/2);
                    Call(music,"LateUpdate");
                    Check(IntroVoice.isPlaying && IntroVoice.volume > 0 && IntroVoice.volume < .3f,
                        "title intro keeps its existing musical fade when skipped");
                    break;
                case 3: Silent(); Page("Continue"); break;
                case 4: Silent(); Page("Title"); Call(saves,"StartNewGameVoyage"); break;
                case 5:
                    CuePlays(MusicCue.Voyage);
                    Check(saves.IsVoyagePlaying,"voyage music follows the actual boat introduction");
                    Check(saves.IsFrontEndVisible, "new game voyage remains a front-end screen");
                    ((NewGameVoyagePresentation)Field(FrontEnd,"voyage")).Dispose();
                    FrontEnd.GetType().GetField("voyage",Private).SetValue(FrontEnd,null);
                    typeof(SaveController).GetField("<IsMenuVisible>k__BackingField",Private).SetValue(saves,false);
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
                    VillagePlays();
                    Check(VillageVoice.timeSamples < VillageVoice.clip.frequency*2,"entering the village starts its recording from the beginning");
                    VillageVoice.timeSamples = VillageVoice.clip.samples - VillageVoice.clip.frequency/3;
                    break;
                case 17:
                    VillagePlays();
                    Check(VillageVoice.timeSamples < VillageVoice.clip.frequency*2,"full village recording loops to its beginning");
                    VillageVoice.timeSamples = VillageVoice.clip.frequency*15;
                    Restore("inn");
                    break;
                case 18:
                    VillagePlays();
                    Check(maps.LocationId == "inn" && VillageVoice.timeSamples > VillageVoice.clip.frequency*15,
                        "entering the inn continues the same village track");
                    Restore("city");
                    break;
                case 19:
                    VillagePlays();
                    Check(maps.LocationId == "city" && VillageVoice.timeSamples > VillageVoice.clip.frequency*16,
                        "returning from the inn continues village music");
                    Restore("world");
                    break;
                case 20:
                    WorldPlays();
                    Check(WorldVoice.timeSamples < WorldVoice.clip.frequency*2,"returning from the village restarts world music");
                    Check(battle.StartRandomBattle(),"world random encounter starts");
                    break;
                case 21:
                    BattlePlays();
                    Check(BattleVoice.timeSamples < BattleVoice.clip.frequency*2,"each new encounter restarts battle music");
                    ClearCombat(); Restore("west-forest");
                    Check(battle.StartRandomBattle(),"quarry random encounter starts");
                    break;
                case 22:
                    BattlePlays(); ClearCombat(); Restore("east-forest");
                    Check(battle.StartRandomBattle(),"forest random encounter starts");
                    break;
                case 23:
                    BattlePlays(); Set(battle.Session,"Outcome",BattleOutcome.Defeat);
                    break;
                case 24:
                    CuePlays(MusicCue.Defeat); ClearCombat(); Restore("west-forest");
                    break;
                case 25:
                    CuePlays(MusicCue.Quarry);
                    Check(Voice(MusicCue.Quarry).timeSamples < Voice(MusicCue.Quarry).clip.frequency*2,"quarry music starts from the beginning");
                    Voice(MusicCue.Quarry).timeSamples = Voice(MusicCue.Quarry).clip.samples - Voice(MusicCue.Quarry).clip.frequency/3;
                    break;
                case 26:
                    CuePlays(MusicCue.Quarry);
                    Check(Voice(MusicCue.Quarry).timeSamples < Voice(MusicCue.Quarry).clip.frequency*2,"full quarry recording loops");
                    Restore("east-forest");
                    break;
                case 27:
                    CuePlays(MusicCue.Forest);
                    Check(Voice(MusicCue.Forest).timeSamples < Voice(MusicCue.Forest).clip.frequency*2,"forest music starts from the beginning");
                    Voice(MusicCue.Forest).timeSamples = Voice(MusicCue.Forest).clip.samples - Voice(MusicCue.Forest).clip.frequency/3;
                    break;
                case 28:
                    CuePlays(MusicCue.Forest);
                    Check(Voice(MusicCue.Forest).timeSamples < Voice(MusicCue.Forest).clip.frequency*2,"full forest recording loops");
                    Restore("ancient-ruins");
                    break;
                case 29:
                    CuePlays(MusicCue.Ruins);
                    Check(Voice(MusicCue.Ruins).timeSamples < Voice(MusicCue.Ruins).clip.frequency*2,"ruins music starts from the beginning");
                    Voice(MusicCue.Ruins).timeSamples = Voice(MusicCue.Ruins).clip.samples - Voice(MusicCue.Ruins).clip.frequency/3;
                    break;
                case 30:
                    CuePlays(MusicCue.Ruins);
                    Check(Voice(MusicCue.Ruins).timeSamples < Voice(MusicCue.Ruins).clip.frequency*2,"full ruins recording loops");
                    Call(Field(battle,"secrets"),"Restore",true,true,3,true,true);
                    Restore("volcano");
                    dragon = music.GetComponentInChildren<DragonInteraction>(true);
                    dialogue = music.GetComponent<DialogueController>(); dialogue.enabled = false;
                    break;
                case 31:
                    CuePlays(MusicCue.DragonLair);
                    Check(!battle.IsActive && !dragon.IsBattlePrelude,"lair ambience is separate from dragon battle music");
                    lairVoice = Voice(MusicCue.DragonLair);
                    Check(lairVoice.timeSamples < lairVoice.clip.frequency*2,"entering the lair starts on the bass pulse");
                    lairVoice.timeSamples = lairVoice.clip.samples - lairVoice.clip.frequency/3;
                    dragon.Interact(new InteractionContext(dialogue,null,_ => { }));
                    Check(dialogue.IsOpen && dialogue.PageNumber == 1,"real dragon dialogue opens before combat");
                    break;
                case 32:
                    CuePlays(MusicCue.DragonLair);
                    Check(Voice(MusicCue.DragonLair) == lairVoice && lairVoice.timeSamples < lairVoice.clip.frequency*2,
                        "the trimmed bass cycle loops while the dragon dialogue is open");
                    lairVoice.timeSamples = lairVoice.clip.frequency*2;
                    ((DialogueTextReveal)Field(dialogue,"reveal")).Tick(.1f);
                    Check(dialogue.IsTyping && !dragon.IsBattlePrelude,"early dragon speech keeps the lair ambience");
                    break;
                case 33:
                    CuePlays(MusicCue.DragonLair);
                    Check(Voice(MusicCue.DragonLair) == lairVoice && lairVoice.timeSamples > lairVoice.clip.frequency*2,
                        "ongoing lair exploration and speech do not restart the ambient cycle");
                    NearEndOfDragonSpeech();
                    Check(dialogue.IsTyping && dragon.IsBattlePrelude,"music is requested before final text finishes");
                    break;
                case 34:
                    CuePlays(MusicCue.Dragon);
                    Check(dialogue.IsOpen && dialogue.IsTyping && !battle.IsActive,"dragon music begins during the last spoken page");
                    dialogue.Close();
                    break;
                case 35:
                    CuePlays(MusicCue.DragonLair);
                    Check(Voice(MusicCue.DragonLair).timeSamples < Voice(MusicCue.DragonLair).clip.frequency*2,
                        "cancelling the speech returns to a fresh ambient cycle");
                    Check(!battle.IsActive && !dragon.IsBattlePrelude,"cancelling the speech cancels the musical prelude");
                    dialogue.Open("Outra conversa","Primeira pagina.","Ultima pagina."); dialogue.Advance(); dialogue.RevealAll();
                    break;
                case 36:
                    CuePlays(MusicCue.DragonLair); Check(!dragon.IsBattlePrelude,"unrelated dialogue cannot reactivate the dragon prelude");
                    dialogue.Close(); dragon.Interact(new InteractionContext(dialogue,null,_ => { }));
                    NearEndOfDragonSpeech();
                    break;
                case 37:
                    CuePlays(MusicCue.Dragon);
                    Check(Voice(MusicCue.Dragon).timeSamples < Voice(MusicCue.Dragon).clip.frequency*2,"a new dragon approach restarts the recording");
                    preludeVoice = Voice(MusicCue.Dragon); preludeVoice.timeSamples = preludeVoice.clip.frequency*10;
                    dialogue.RevealAll(); dialogue.Advance();
                    Check(!dialogue.IsOpen && battle.IsActive && battle.IsDragonEncounter,"completing the real dialogue starts the dragon battle");
                    break;
                case 38:
                    CuePlays(MusicCue.Dragon);
                    Check(Voice(MusicCue.Dragon) == preludeVoice && preludeVoice.timeSamples > preludeVoice.clip.frequency*10,
                        "the same audio source continues from speech into combat without restarting");
                    preludeVoice.timeSamples = preludeVoice.clip.samples - preludeVoice.clip.frequency/3;
                    break;
                case 39:
                    CuePlays(MusicCue.Dragon);
                    Check(preludeVoice.timeSamples < preludeVoice.clip.frequency*2,"full dragon recording loops during combat");
                    foreach (var fighter in battle.Session.Combatants) if (fighter.Team == CombatTeam.Enemy) Set(fighter,"Hp",0);
                    Set(battle.Session,"Outcome",BattleOutcome.Victory); Call(battle,"AwardIfComplete"); Call(battle,"Update");
                    break;
                case 40:
                    VictoryPlays();
                    Check(battle.Dragon.IsDefeated,"dragon victory marks the liberated essence");
                    Set(battle,"PostBattleStartedAt",Time.unscaledTime-10); battle.ReturnToCity();
                    break;
                case 41:
                    CuePlays(MusicCue.DragonFreed);
                    Check(Voice(MusicCue.DragonFreed).timeSamples < Voice(MusicCue.DragonFreed).clip.frequency*2,"dragon aftermath begins after victory results");
                    dialogue.Close(); dragon.Interact(new InteractionContext(dialogue,null,_ => { }));
                    Check(dialogue.Title == "Essência liberta" && !dragon.IsBattlePrelude,"liberated essence conversation keeps the aftermath track");
                    Voice(MusicCue.DragonFreed).timeSamples = Voice(MusicCue.DragonFreed).clip.samples - Voice(MusicCue.DragonFreed).clip.frequency/3;
                    break;
                case 42:
                    CuePlays(MusicCue.DragonFreed);
                    Check(Voice(MusicCue.DragonFreed).timeSamples < Voice(MusicCue.DragonFreed).clip.frequency*2,"dragon aftermath recording loops");
                    dialogue.Close(); ClearCombat(); Restore("castle");
                    break;
                case 43:
                    CuePlays(MusicCue.Throne);
                    Check(!battle.IsActive,"throne music plays before starting combat");
                    Voice(MusicCue.Throne).timeSamples = Voice(MusicCue.Throne).clip.samples - Voice(MusicCue.Throne).clip.frequency/3;
                    break;
                case 44:
                    CuePlays(MusicCue.Throne);
                    Check(Voice(MusicCue.Throne).timeSamples < Voice(MusicCue.Throne).clip.frequency*2,"throne room recording loops");
                    dialogue.Open(new DialogueDefinition(DialogueCatalog.KingConfrontation.Id, "Rei Rubro", "Antes da ameaca final.", "O proprio ceu caira sobre voces!"));
                    Call(music,"LateUpdate");
                    Check(music.CurrentCue == MusicCue.Silence,"opening the royal dialogue fades the current track to silence");
                    Call(music,"Advance",2f);
                    Check(music.GetComponentsInChildren<AudioSource>().All(s => !s.isPlaying),"the audience is silent after the fade");
                    dialogue.Advance(); Call(music,"LateUpdate");
                    Check(music.CurrentCue == MusicCue.Silence,"the last speech also stays silent");
                    dialogue.Close(); battle.StartCastleBattle();
                    battle.EntryTransition.enabled = false;
                    battle.EntryTransition.Sequence.Begin(true);
                    Call(music,"LateUpdate");
                    Check(music.CurrentCue == MusicCue.Silence && battle.EntryTransition.HidesBattle,"the fade to black remains silent");
                    battle.EntryTransition.Sequence.Advance(100);
                    Call(music,"LateUpdate");
                    Check(music.CurrentCue == MusicCue.Silence,"reaching black logically is insufficient until a black frame is drawn");
                    battle.EntryTransition.Sequence.MarkBlackDrawn();
                    Call(music,"LateUpdate");
                    Check(music.CurrentCue == MusicCue.FinalBoss && battle.EntryTransition.Sequence.BlackOpacity == 1,
                        "final music starts on a rendered full-black frame");
                    Check(Voice(MusicCue.FinalBoss).volume > .5f,"the final track is audible before the battle fades in");
                    break;
                case 45:
                    CuePlays(MusicCue.FinalBoss);
                    Check(battle.EntryTransition.Sequence.BlackOpacity == 1,"final music plays while combat stays covered in black");
                    var finalVoice = Voice(MusicCue.FinalBoss);
                    int finalPosition = finalVoice.timeSamples;
                    Check(finalPosition < finalVoice.clip.frequency*3,"tyrant battle starts at the recording's first audible passage");
                    battle.EntryTransition.Sequence.MarkBlackDrawn();
                    battle.EntryTransition.Sequence.MarkBattleReady();
                    battle.EntryTransition.Sequence.Advance(BattleEntrySequence.KingBlackDuration + BattleEntrySequence.KingFadeInDuration + .01f);
                    battle.EntryTransition.enabled = true;
                    Call(music,"LateUpdate");
                    Check(!battle.IsEnteringBattle && Voice(MusicCue.FinalBoss) == finalVoice && finalVoice.timeSamples >= finalPosition,
                        "revealing the final battle keeps the same audio source and position");
                    finalVoice.timeSamples = finalVoice.clip.samples - finalVoice.clip.frequency/3;
                    break;
                case 46:
                    CuePlays(MusicCue.FinalBoss);
                    Check(Voice(MusicCue.FinalBoss).timeSamples < Voice(MusicCue.FinalBoss).clip.frequency*2,"full tyrant recording loops during battle");
                    Set(battle.Session,"Outcome",BattleOutcome.Defeat);
                    break;
                case 47:
                    CuePlays(MusicCue.Defeat);
                    Voice(MusicCue.Defeat).timeSamples = Voice(MusicCue.Defeat).clip.samples - Voice(MusicCue.Defeat).clip.frequency/3;
                    break;
                case 48:
                    CuePlays(MusicCue.Defeat);
                    Check(Voice(MusicCue.Defeat).timeSamples < Voice(MusicCue.Defeat).clip.frequency*2,"defeat theme loops while awaiting continue");
                    battle.ReturnToCity();
                    break;
                case 49:
                    VillagePlays();
                    Check(maps.LocationId == "inn" && !battle.IsActive,"continuing after defeat returns to the inn theme");
                    dialogue.Close();
                    rest = music.GetComponent<InnRestController>(); rest.enabled = false;
                    Check(Math.Abs(rest.SleepMusicSeconds-VoiceDuration(MusicCue.Sleep)) < .05f,"rest timing uses the full supplied jingle duration");
                    rest.Begin();
                    for (int i=0;i<100 && rest.IsPlaying && !rest.IsSleeping;i++) Call(rest,"Advance",.25f);
                    Check(rest.IsPlaying && rest.IsSleeping,"party walks to bed before sleep music starts");
                    break;
                case 50:
                    CuePlays(MusicCue.Sleep);
                    Check(!Voice(MusicCue.Sleep).loop,"sleep jingle is a one-shot");
                    Call(rest,"Advance",InnRestController.FadeDuration+.1f);
                    break;
                case 51:
                    CuePlays(MusicCue.Sleep);
                    Call(rest,"Advance",InnRestController.NightDuration+.1f);
                    Check(rest.IsSleeping && rest.BlackOpacity >= 1,"night stays black while the longer jingle plays");
                    Voice(MusicCue.Sleep).timeSamples = Voice(MusicCue.Sleep).clip.samples - Voice(MusicCue.Sleep).clip.frequency/3;
                    break;
                case 52:
                    Check(music.CurrentCue == MusicCue.Sleep && !music.GetComponentsInChildren<AudioSource>().Any(s => s.isPlaying),
                        "sleep jingle finishes without looping or restarting");
                    Call(rest,"Advance",rest.SleepMusicSeconds);
                    Check(rest.IsPlaying && !rest.IsSleeping,"waking begins after the complete rest interval");
                    break;
                case 53:
                    VillagePlays(); Call(rest,"Advance",InnRestController.FadeDuration+.1f);
                    Check(!rest.IsPlaying,"rest finishes and restores exploration");
                    Set(saves,"IsMenuVisible",true); Call(saves,"StartNewGameVoyage");
                    break;
                case 54:
                    CuePlays(MusicCue.Voyage);
                    Check(Voice(MusicCue.Voyage).timeSamples < Voice(MusicCue.Voyage).clip.frequency*2,"a new boat introduction restarts its track");
                    Voice(MusicCue.Voyage).timeSamples = Voice(MusicCue.Voyage).clip.samples - Voice(MusicCue.Voyage).clip.frequency/3;
                    break;
                case 55:
                    CuePlays(MusicCue.Voyage);
                    Check(Voice(MusicCue.Voyage).timeSamples < Voice(MusicCue.Voyage).clip.frequency*2,"complete boat recording loops during its introduction");
                    TrackFade(MusicCue.Voyage); VoyageClock.Advance(NewGameVoyageSequence.FadeOutAt);
                    break;
                case 56:
                    CheckFade(MusicCue.Voyage,1);
                    VoyageClock.Advance(NewGameVoyageSequence.FadeOutDuration/2);
                    break;
                case 57:
                    CheckFade(MusicCue.Voyage,.5f);
                    VoyageClock.Advance(NewGameVoyageSequence.FadeOutDuration/2);
                    break;
                case 58:
                    CheckFade(MusicCue.Voyage,0);
                    VoyageClock.Advance(NewGameVoyageSequence.BlackHold + NewGameVoyageSequence.RevealDuration/2);
                    break;
                case 59:
                    CheckFade(MusicCue.Voyage,0);
                    Check(VoyageClock.Elapsed > NewGameVoyageSequence.RevealAt,"map reveal cannot bring back the faded boat music");
                    VoyageClock.Advance(NewGameVoyageSequence.CompleteAt); Call(saves,"Update");
                    break;
                case 60:
                    VillagePlays(); Check(!saves.IsVoyagePlaying && !saves.IsMenuVisible,"completing the voyage restores map music");
                    endings = music.GetComponent<EndingController>(); endings.enabled = false;
                    battle.RestoreEnding(GameEnding.KingFreed);
                    break;
                case 61:
                    CuePlays(MusicCue.GoodEnding);
                    Check(Voice(MusicCue.GoodEnding).volume == 0, "good-ending music waits for its first cinematic repaint");
                    GoodClock.Advance(KingFreedSequence.FadeInDuration);
                    Check(endings.CurrentEnding == GameEnding.KingFreed,"good-ending cue follows the actual ending presentation");
                    Voice(MusicCue.GoodEnding).timeSamples = Voice(MusicCue.GoodEnding).clip.samples - Voice(MusicCue.GoodEnding).clip.frequency/3;
                    break;
                case 62:
                    CuePlays(MusicCue.GoodEnding);
                    Check(Voice(MusicCue.GoodEnding).timeSamples < Voice(MusicCue.GoodEnding).clip.frequency*2,"good-ending track loops");
                    TrackFade(MusicCue.GoodEnding); GoodClock.Advance(GoodClock.DissolveAt + KingFreedSequence.DissolveDuration/2);
                    break;
                case 63:
                    CheckFade(MusicCue.GoodEnding,1);
                    Check(GoodClock.ProsperityOpacity > 0 && GoodClock.ProsperityOpacity < 1,"internal good-ending dissolve keeps full music");
                    GoodClock.Advance(GoodClock.FadeOutAt-GoodClock.Elapsed);
                    break;
                case 64:
                    CheckFade(MusicCue.GoodEnding,1); GoodClock.Advance(KingFreedSequence.FadeOutDuration/2);
                    break;
                case 65:
                    CheckFade(MusicCue.GoodEnding,.5f); GoodClock.Advance(KingFreedSequence.FadeOutDuration/2);
                    break;
                case 66:
                    CheckFade(MusicCue.GoodEnding,0); GoodClock.Advance(KingFreedSequence.BlackHold/2);
                    break;
                case 67:
                    CheckFade(MusicCue.GoodEnding,0); ClearEnding(); battle.RestoreEnding(GameEnding.HeroPossessed);
                    break;
                case 68:
                    CuePlays(MusicCue.MindControlEnding);
                    Check(Voice(MusicCue.MindControlEnding).volume == 0, "mind-control music waits for its first cinematic repaint");
                    MindClock.Advance(HeroPossessedSequence.FadeInDuration);
                    Check(endings.CurrentEnding == GameEnding.HeroPossessed,"mind-control cue follows its ending presentation");
                    Voice(MusicCue.MindControlEnding).timeSamples = Voice(MusicCue.MindControlEnding).clip.samples - Voice(MusicCue.MindControlEnding).clip.frequency/3;
                    break;
                case 69:
                    CuePlays(MusicCue.MindControlEnding);
                    Check(Voice(MusicCue.MindControlEnding).timeSamples < Voice(MusicCue.MindControlEnding).clip.frequency*2,"mind-control ending track loops");
                    TrackFade(MusicCue.MindControlEnding); MindClock.Advance(MindClock.FadeOutAt);
                    break;
                case 70:
                    CheckFade(MusicCue.MindControlEnding,1); MindClock.Advance(HeroPossessedSequence.FadeOutDuration/2);
                    break;
                case 71:
                    CheckFade(MusicCue.MindControlEnding,.5f); MindClock.Advance(HeroPossessedSequence.FadeOutDuration/2);
                    break;
                case 72:
                    CheckFade(MusicCue.MindControlEnding,0); MindClock.Advance(HeroPossessedSequence.BlackHold/2);
                    break;
                case 73:
                    CheckFade(MusicCue.MindControlEnding,0); ClearEnding(); battle.RestoreEnding(GameEnding.CastleDestroyed);
                    break;
                case 74:
                    CuePlays(MusicCue.MeteorEnding);
                    Check(Voice(MusicCue.MeteorEnding).volume == 0, "meteor music waits for its first cinematic repaint");
                    MeteorScene.Impact.Sequence.Advance(CastleImpactSequence.FadeInDuration);
                    Check(endings.CurrentEnding == GameEnding.CastleDestroyed,"meteor cue follows its ending presentation");
                    Voice(MusicCue.MeteorEnding).timeSamples = Voice(MusicCue.MeteorEnding).clip.samples - Voice(MusicCue.MeteorEnding).clip.frequency/3;
                    break;
                case 75:
                    CuePlays(MusicCue.MeteorEnding);
                    Check(Voice(MusicCue.MeteorEnding).timeSamples < Voice(MusicCue.MeteorEnding).clip.frequency*2,"meteor ending track loops");
                    TrackFade(MusicCue.MeteorEnding); MeteorScene.Impact.Sequence.Advance(CastleImpactSequence.ImpactAt + CastleImpactSequence.FlashDuration/2);
                    break;
                case 76:
                    CheckFade(MusicCue.MeteorEnding,1);
                    Check(MeteorScene.Impact.Sequence.WhiteOpacity > 0,"meteor impact flash does not fade music");
                    MeteorScene.Impact.Sequence.Advance(CastleImpactSequence.CompleteAt);
                    break;
                case 77:
                    CheckFade(MusicCue.MeteorEnding,1); MeteorClock.Advance(MeteorClock.FadeOutAt);
                    break;
                case 78:
                    CheckFade(MusicCue.MeteorEnding,1); MeteorClock.Advance(CastleDestroyedSequence.FadeOutDuration/2);
                    break;
                case 79:
                    CheckFade(MusicCue.MeteorEnding,.5f); MeteorClock.Advance(CastleDestroyedSequence.FadeOutDuration/2);
                    break;
                case 80:
                    CheckFade(MusicCue.MeteorEnding,0); MeteorClock.Advance(CastleDestroyedSequence.BlackHold/2);
                    break;
                case 81:
                    CheckFade(MusicCue.MeteorEnding,0); ClearEnding(); ClearCombat(); Set(saves,"IsMenuVisible",true); Page("Title");
                    break;
                case 82:
                    Silent(); music.enabled = false;
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
        else Debug.Log("MUSIC PLAYBACK PASS: " + checks + " checks for all nineteen recordings, regions, combat, boat, one-shot sleep, defeat, bass ambience, continuous dragon prelude, dragon aftermath, throne room and synchronized cinematic fade-outs for the voyage and all three endings.");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
