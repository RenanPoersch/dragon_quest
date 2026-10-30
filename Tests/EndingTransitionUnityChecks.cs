using System;
using System.Collections.Generic;
using System.Reflection;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

// Runs with real cinematic resources and AudioSources in a disposable Unity project.
[InitializeOnLoad]
public static class EndingTransitionUnityChecks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Pending = "DragonQuest.EndingTransitionChecks";
    private static int checks;
    static EndingTransitionUnityChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    private static object Field(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
    private static void Put(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
    private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);
    private static void Check(bool value, string reason)
    { checks++; if (!value) throw new Exception("Ending transition: " + reason); }
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .002f;

    public static void Run()
    { SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode(); }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        try
        {
            foreach (GameEnding ending in new[] { GameEnding.KingFreed, GameEnding.HeroPossessed, GameEnding.CastleDestroyed })
            { Validate(ending, false); Validate(ending, true); }
            Debug.Log("ENDING_TRANSITIONS_UNITY_OK: " + checks + " checks; all three endings, battle visibility, repaint/load barriers, restored saves, synchronized source volumes, internal cuts and final fades.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void Validate(GameEnding ending, bool restored)
    {
        var root = new GameObject("Ending transition checks / " + ending);
        var hero = new GameObject("Hero");
        var map = new GameObject("Battle map");
        var camera = new GameObject("Camera");
        AudioClip clip = null;
        try
        {
            var movement = hero.AddComponent<PlayerMovement>();
            var follow = camera.AddComponent<CameraFollow>();
            var endings = root.AddComponent<PrototypeEndingController>(); endings.enabled = false;
            endings.Initialize(new Dictionary<string, GameObject> { { "castle", map } }, movement, follow);
            var battle = root.AddComponent<PrototypeBattleController>(); battle.enabled = false;
            battle.Initialize(movement, root.AddComponent<DialogueController>(), new ExplorationProgress(), endingPresentation: endings.Show);
            var saves = root.AddComponent<PrototypeSaveController>(); saves.enabled = false;
            Set(saves, "IsMenuVisible", false);
            var locations = root.AddComponent<PrototypeLocations>(); locations.enabled = false;
            var music = root.AddComponent<PrototypeMusicController>();
            music.Initialize(saves, locations, battle);
            clip = AudioClip.Create("Transition tone", 96000, 1, 48000, false);
            var samples = new float[96000];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * Mathf.PI * 2 * 220 / 48000) * .01f;
            clip.SetData(samples, 0);
            var clips = (Dictionary<MusicCue, AudioClip>)Field(music, "clips");
            clips[MusicCue.FinalBoss] = clips[MusicCuePolicy.EndingCue(ending)] = clip;
            var voices = (AudioSource[])Field(music, "sources");
            Call(music, "TransitionTo", MusicCue.FinalBoss); Call(music, "Advance", 1f);
            var battleVoice = voices[(int)Field(music, "current")];
            Check(Near(battleVoice.volume, .52f), "battle music starts at full volume");

            if (restored)
            {
                battle.RestoreEnding(ending);
                Check(!endings.IsFadingBattle && endings.CurrentEnding == ending,
                    "a restored ending starts from black without showing a nonexistent battle");
            }
            else
            {
                battle.StartCastleBattle(); battle.EntryTransition.Cancel();
                Set(battle.CastleRules, "Ending", ending); Set(battle.Session, "Outcome", BattleOutcome.Completed);
                Call(battle, "FinishCastleBattle");
                Check(battle.HasEnded && battle.IsActive && endings.IsFadingBattle,
                    "the completed battle stays visible during the outgoing fade");
                Check(map.activeSelf && hero.activeSelf && follow.enabled && endings.CurrentEnding == GameEnding.None,
                    "scene and cinematic cue do not change before black");
                Check(!(bool)Field(movement, "movementEnabled"), "movement stays blocked during the fade");
                int turns = battle.Session.Combatants[0].TurnsTaken;
                Call(battle, "Update"); endings.Show(GameEnding.KingFreed);
                Check(battle.Session.Combatants[0].TurnsTaken == turns && Near(endings.BattleBlackOpacity, 0),
                    "repeated completion/input cannot advance turns or restart the fade");
                Call(endings, "AdvanceBattleFade", 100f);
                Check(Near(endings.BattleBlackOpacity, 0), "a loading stall cannot consume the first battle repaint");
                ((BattleExitSequence)Field(endings, "battleExit")).MarkDrawn();
                Call(endings, "AdvanceBattleFade", BattleExitSequence.FadeOutDuration / 2);
                Call(music, "LateUpdate");
                Check(Near(endings.BattleBlackOpacity, .5f) && Near(battleVoice.volume, .26f)
                    && music.CurrentCue == MusicCue.FinalBoss, "battle picture and the original music fade together");
                Call(endings, "AdvanceBattleFade", 100f); Call(music, "LateUpdate");
                Check(endings.IsFadingBattle && endings.BattleBlackOpacity == 1 && battleVoice.volume == 0
                    && map.activeSelf, "a long frame holds black and silence until the covering repaint");
                ((BattleExitSequence)Field(endings, "battleExit")).MarkDrawn(); Call(endings, "AdvanceBattleFade", 0f);
                Call(battle, "Update");
                Check(!battle.IsActive && battle.HasEnded && !endings.IsFadingBattle && endings.CurrentEnding == ending,
                    "black releases the battle and starts the correct cinematic exactly once");
            }
            Check(!map.activeSelf && !hero.activeSelf && !follow.enabled, "cinematic setup happens behind black");
            Call(music, "LateUpdate");
            var voice = voices[(int)Field(music, "current")];
            Check(music.CurrentCue == MusicCuePolicy.EndingCue(ending) && voice.volume == 0 && endings.MusicGain == 0,
                "the correct ending track starts silent at the cinematic's first frame");
            Check(!battleVoice.isPlaying, "outgoing battle audio cannot bleed into the ending");
            object sequence = ending == GameEnding.KingFreed ? (object)((KingFreedPresentation)Field(endings, "liberation")).Sequence
                : ending == GameEnding.HeroPossessed ? ((HeroPossessedPresentation)Field(endings, "possession")).Sequence
                : ((CastleDestroyedEndingPresentation)Field(endings, "aftermath")).Impact.Sequence;
            Call(endings, "Update");
            Check((float)sequence.GetType().GetProperty("Elapsed").GetValue(sequence) == 0,
                "preloading does not consume the cinematic's opening before its first repaint");
            Action<float> advance = delta => sequence.GetType().GetMethod("Advance").Invoke(sequence, new object[] { delta });
            advance(.7f); Call(music, "Advance", 0f);
            Check(Near(endings.MusicGain, .5f) && Near(voice.volume, .26f), "ending music and artwork reach half strength together");
            advance(.7f); Call(music, "Advance", 0f);
            Check(Near(voice.volume, .52f), "the opening fade reaches full music volume");
            if (ending == GameEnding.CastleDestroyed)
            {
                var scene = (CastleDestroyedEndingPresentation)Field(endings, "aftermath");
                scene.Advance(CastleImpactSequence.CompleteAt - scene.Elapsed + .7f);
                sequence = scene.Aftermath.Sequence;
                Call(music, "Advance", 0f);
                Check(Near(voice.volume, .52f), "the internal white transition does not dip or restart meteor music");
            }
            if (ending == GameEnding.KingFreed)
            {
                var king = (KingFreedSequence)sequence;
                advance(king.DissolveAt + KingFreedSequence.DissolveDuration / 2 - king.Elapsed);
                Call(music, "Advance", 0f);
                Check(Near(voice.volume, .52f), "the internal prosperity dissolve keeps full music volume");
            }
            float elapsed = (float)sequence.GetType().GetProperty("Elapsed").GetValue(sequence);
            float fadeOutAt = (float)sequence.GetType().GetProperty("FadeOutAt").GetValue(sequence);
            advance(fadeOutAt - elapsed + 1.5f); Call(music, "Advance", 0f);
            Check(Near(voice.volume, .26f), "the existing final fade still reaches half volume");
            advance(1.5f); Call(music, "Advance", 0f);
            Check(voice.volume == 0, "the final black hold remains silent");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(hero);
            UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(camera);
            if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
