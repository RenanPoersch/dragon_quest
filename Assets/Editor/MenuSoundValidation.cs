using DragonQuest.Presentation.Menus;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class MenuSoundValidation
{
    private const string Pending = "DragonQuest.MenuSoundValidation";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static int checks, step, nextFrame;
    private static double nextAt, began;
    private static GameObject fixture, audioRoot, listener;
    private static MenuSoundController sounds;
    private static AudioSource voice, musicVoice;
    private static PrototypeSaveController saves;
    private static DialogueController dialogue;
    private static PrototypeBattleController battle;
    private static InventoryMenuPresenter inventory;
    private static PrototypeBattleHud battleHud;
    private static PrototypeShopController shops;
    private static PrototypeCheatMenu cheats;
    private static Keyboard keys;
    private static Gamepad pad;
    static MenuSoundValidation()
    {
        if (SessionState.GetBool(Pending,false)) EditorApplication.update += Tick;
    }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception("Menu sound step " + step + ": " + message); }
    private static object FrontEnd => Field(saves, "frontEnd").GetValue(saves);
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name,Private);
    private static void Put(object target, string name, object value) => Field(target,name).SetValue(target,value);
    private static void Property(object target, string name, object value) => Put(target,"<"+name+">k__BackingField",value);
    private static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method,Private).Invoke(target,args);

    public static void RunPlayback()
    {
        checks = 0;
        string[] hashes = {
            "48e4f82c389e08cfafbb5f5d8d80b29ed175e0b3c030ec3c5cdd899225c74754",
            "53d5cd57d0733ff00b3ecf367c0587c99c2e5b1f17903576f1fd33cf07921a88",
            "cd1620019e9398e5b8a2b34df3f4229b59c0101b991be18b4302c2215da2cd3e",
            "0bcc145990655bc8cdbb02271a25d8937802ff2c7134ed3b14dd0f98fd1becef" };
        float[] durations = { .165f,.185f,.145f,.46f };
        foreach (MenuSound sound in Enum.GetValues(typeof(MenuSound)))
        {
            string asset = "Assets/Resources/MenuSounds/"+sound.ToString().ToLowerInvariant()+".wav";
            AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceUpdate);
            var clip = Resources.Load<AudioClip>("MenuSounds/"+sound.ToString().ToLowerInvariant());
            Check(clip != null && clip.channels == 1 && clip.frequency == 48000 && Math.Abs(clip.length-durations[(int)sound]) < .001f,
                sound + " preserves the selected preview's duration and format");
            using (var sha = SHA256.Create())
                Check(BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(asset))).Replace("-","").ToLowerInvariant() == hashes[(int)sound],
                    sound + " is exactly the chosen WAV, including the octave-up New Game arpeggio");
            var importer = (AudioImporter)AssetImporter.GetAtPath(asset);
            Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad
                && importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM
                && importer.defaultSampleSettings.preloadAudioData, sound + " preloads short PCM audio without lossy compression");
        }
        Debug.Log("MENU SOUND ASSETS PASS: "+checks+" checks.");
        SessionState.SetInt(Pending+".Checks",checks); SessionState.SetBool(Pending,true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void State(GamepadState state = default, params Key[] pressed)
    {
        typeof(GameInput).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        Focus(true);
        InputSystem.QueueStateEvent(pad,new GamepadState());
        InputSystem.QueueStateEvent(keys,new KeyboardState()); InputSystem.Update();
        InputSystem.QueueStateEvent(pad,state);
        InputSystem.QueueStateEvent(keys,new KeyboardState(pressed)); InputSystem.Update();
    }
    private static void Focus(bool focused) => typeof(GameInput).GetField("hasFocus",BindingFlags.Static|BindingFlags.NonPublic)
        .SetValue(null,(Func<bool>)(() => focused));
    private static void Key(Key key) => State(default,key);
    private static void Sound(MenuSound expected)
    {
        Check(sounds.CurrentSound == expected && (int)Field(sounds,"playedFrame").GetValue(sounds) == Time.frameCount,
            "actual menu action emits "+expected);
        Check(voice.clip?.name == expected.ToString().ToLowerInvariant() && voice.isPlaying,
            expected+" plays on the real audio source");
    }
    private static void Quiet() => Check((int)Field(sounds,"playedFrame").GetValue(sounds) != Time.frameCount,
        "idle, blocked or repeated actions emit no new effect");
    private static void CancelInventory()
    { State(new GamepadState().WithButton(GamepadButton.East)); Call(battle,"Update"); Sound(MenuSound.Cancel); }
    private static void OpenInventoryCommand(int command)
    { Put(inventory,"selectedMenu",command); Key(UnityEngine.InputSystem.Key.Enter); Call(inventory,"Update"); Sound(MenuSound.Confirm); }

    private static void Setup()
    {
        checks = SessionState.GetInt(Pending+".Checks",0);
        keys = InputSystem.AddDevice<Keyboard>(); pad = InputSystem.AddDevice<Gamepad>();
        foreach (var control in keys.allControls.Concat(pad.allControls))
            if (control is ButtonControl button) { bool prime = button.wasPressedThisFrame; }
        State();
        listener = new GameObject("Menu validation listener"); listener.AddComponent<AudioListener>();
        audioRoot = new GameObject("Menu sounds"); sounds = audioRoot.AddComponent<MenuSoundController>();
        voice = audioRoot.GetComponent<AudioSource>();
        fixture = new GameObject("Menu sound checks");
        var movement = fixture.AddComponent<PlayerMovement>();
        dialogue = fixture.AddComponent<DialogueController>();
        saves = fixture.AddComponent<PrototypeSaveController>();
        battle = fixture.AddComponent<PrototypeBattleController>();
        cheats = fixture.AddComponent<PrototypeCheatMenu>();
        shops = fixture.AddComponent<PrototypeShopController>();
        var progress = new ExplorationProgress(50000);
        battle.Initialize(movement,dialogue,progress);
        cheats.Initialize(movement,battle,saves,progress);
        shops.Initialize(movement,dialogue,battle,progress);
        var inventoryHost = fixture.AddComponent<PrototypeMateriaHud>(); inventoryHost.Initialize(battle,new AncientSecrets()); inventory = inventoryHost.Presenter;
        battleHud = fixture.AddComponent<PrototypeBattleHud>(); battleHud.Initialize(battle);
        Put(saves,"store",new SaveStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DQMenuSound-"+Guid.NewGuid())));
        Put(saves,"player",movement); Put(saves,"dialogue",dialogue); Put(saves,"battle",battle);
        Put(FrontEnd,"menuHasDrawn",true); Put(FrontEnd,"menuFadeInElapsed",2f);
        foreach (var component in fixture.GetComponents<MonoBehaviour>()) component.enabled = false;
        var maps = fixture.AddComponent<PrototypeLocations>(); maps.enabled = false;
        var music = fixture.AddComponent<PrototypeMusicController>(); music.enabled = false; music.Initialize(saves,maps,battle);
        var voices = music.GetComponentsInChildren<AudioSource>();
        Check(voices.Length == 2 && voice.transform.parent == null,"UI audio stays separate from the two music sources");
        musicVoice = voices[0]; musicVoice.clip = AudioClip.Create("Background continuity fixture",48000,1,48000,false);
        musicVoice.loop = true; musicVoice.volume = 0; musicVoice.Play();
        Check(!voice.loop && voice.spatialBlend == 0 && voice.pitch == 1 && voice.volume > 0 && audioRoot.GetComponents<AudioSource>().Length == 1,
            "one non-spatial source plays short unpitched effects without stacking sources");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        double now = EditorApplication.timeSinceStartup;
        if (began == 0) { began = now; nextAt = now+1; }
        if (now-began > 90) { Finish(new Exception("Menu sound validation timeout at "+step)); return; }
        if (EditorApplication.isPaused) { EditorApplication.isPaused = false; return; }
        if (now < nextAt || Time.frameCount < nextFrame) return;
        try
        {
            switch (step)
            {
                case 0: Setup(); break;
                case 1: Key(UnityEngine.InputSystem.Key.DownArrow); Call(saves,"Update"); Sound(MenuSound.Navigate); break;
                case 2:
                    Check(voice.timeSamples > 0,"selected sound advances real audio samples");
                    State(); Call(saves,"Update"); Quiet(); break;
                case 3: Key(UnityEngine.InputSystem.Key.Enter); Call(saves,"Update"); Sound(MenuSound.Confirm); break;
                case 4: State(new GamepadState().WithButton(GamepadButton.East)); Call(saves,"Update"); Sound(MenuSound.Cancel); break;
                case 5: Key(UnityEngine.InputSystem.Key.Enter); Call(saves,"Update"); Sound(MenuSound.NewGame); break;
                case 6:
                    Call(saves,"NewGame"); Call(saves,"NewGame"); Quiet();
                    Check((bool)Field(FrontEnd,"startingNewGame").GetValue(FrontEnd),"New Game is guarded against repeated activations");
                    Put(FrontEnd,"startingNewGame",false); Property(saves,"IsMenuVisible",false); break;
                case 7: Property(battle,"IsMenuOpen",true); State(); Call(inventory,"Update"); Sound(MenuSound.Confirm); break;
                case 8: State(new GamepadState().WithButton(GamepadButton.DpadDown)); Call(inventory,"Update"); Quiet(); break;
                case 9: Key(UnityEngine.InputSystem.Key.Enter); Call(inventory,"Update"); Sound(MenuSound.Confirm); break;
                case 10: Key(UnityEngine.InputSystem.Key.DownArrow); Call(inventory,"Update"); Quiet(); break;
                case 11: CancelInventory(); break;
                case 12: OpenInventoryCommand(0); break;
                case 13: Key(UnityEngine.InputSystem.Key.DownArrow); Call(inventory,"Update"); Quiet(); break;
                case 14: Key(UnityEngine.InputSystem.Key.Enter); Call(inventory,"Update"); Sound(MenuSound.Confirm); break;
                case 15: State(new GamepadState { leftStick = Vector2.down }); Call(inventory,"Update"); Quiet(); break;
                case 16: CancelInventory(); break;
                case 17: CancelInventory(); break;
                case 18: OpenInventoryCommand(2); break;
                case 19: State(new GamepadState().WithButton(GamepadButton.RightShoulder)); Call(inventory,"Update"); Quiet(); break;
                case 20: Key(UnityEngine.InputSystem.Key.RightArrow); Call(inventory,"Update"); Quiet(); break;
                case 21: CancelInventory(); break;
                case 22: OpenInventoryCommand(3); break;
                case 23: Key(UnityEngine.InputSystem.Key.RightArrow); Call(inventory,"Update"); Quiet(); break;
                case 24: CancelInventory(); break;
                case 25:
                    State(new GamepadState().WithButton(GamepadButton.East)); Call(battle,"Update"); Call(inventory,"Update");
                    Sound(MenuSound.Cancel); Check(!battle.IsMenuOpen,"closing inventory emits cancellation"); break;
                case 26: State(); shops.Open("item-shop"); Sound(MenuSound.Confirm); break;
                case 27: Key(UnityEngine.InputSystem.Key.DownArrow); Call(shops,"Update"); Quiet(); break;
                case 28: Key(UnityEngine.InputSystem.Key.Enter); Call(shops,"Update"); Sound(MenuSound.Confirm); break;
                case 29: State(); shops.Back(); Sound(MenuSound.Cancel); break;
                case 30: State(); shops.ChooseAction(0); Sound(MenuSound.Confirm); break;
                case 31: Key(UnityEngine.InputSystem.Key.DownArrow); Call(shops,"Update"); Quiet(); break;
                case 32: Key(UnityEngine.InputSystem.Key.Enter); Call(shops,"Update"); Sound(MenuSound.Confirm); break;
                case 33: Key(UnityEngine.InputSystem.Key.RightArrow); Call(shops,"Update"); Quiet(); break;
                case 34: Key(UnityEngine.InputSystem.Key.Escape); Call(shops,"Update"); Sound(MenuSound.Cancel); break;
                case 35: State(); shops.Back(); Sound(MenuSound.Cancel); break;
                case 36: State(); shops.ChooseAction(2); Sound(MenuSound.Cancel); break;
                case 37: State(new GamepadState().WithButton(GamepadButton.Select)); Call(cheats,"Update"); Sound(MenuSound.Confirm); break;
                case 38: Key(UnityEngine.InputSystem.Key.DownArrow); Call(cheats,"Update"); Quiet(); break;
                case 39: Key(UnityEngine.InputSystem.Key.Enter); Call(cheats,"Update"); Sound(MenuSound.Confirm); break;
                case 40: Key(UnityEngine.InputSystem.Key.Escape); Call(cheats,"Update"); Sound(MenuSound.Cancel); break;
                case 41:
                    State(); battle.StartBattle();
                    var actor = battle.Session.CurrentActor;
                    Check(actor != null && actor.Team == CombatTeam.Party,"battle fixture starts on a party command");
                    Put(battleHud.Arena,"observedBattle",battle.Session); Put(battleHud.Menu,"observedBattle",battle.Session); Put(battleHud.Menu,"observedActor",actor);
                    Put(battleHud.Menu,"observedDefinition",actor.Definition); Put(battleHud.Menu,"observedLimitReady",actor.Limit.IsReady);
                    Put(battleHud.Menu,"rootCommands",BattleCommandMenu.Root(actor)); Put(battleHud.Menu,"defendCommand",BattleCommandMenu.Defend(actor));
                    break;
                case 42: Key(UnityEngine.InputSystem.Key.DownArrow); Call(battleHud,"Update"); Quiet(); break;
                case 43: Put(battleHud.Menu,"commandIndex",0); Key(UnityEngine.InputSystem.Key.Enter); Call(battleHud,"Update"); Sound(MenuSound.Confirm); break;
                case 44: Key(UnityEngine.InputSystem.Key.Escape); Call(battleHud,"Update"); Sound(MenuSound.Cancel); break;
                case 45: Key(UnityEngine.InputSystem.Key.RightArrow); Call(battleHud,"Update"); Quiet(); break;
                case 46: Key(UnityEngine.InputSystem.Key.Escape); Call(battleHud,"Update"); Sound(MenuSound.Cancel); break;
                case 47: Focus(false); MenuSounds.Play(MenuSound.Confirm); Quiet(); break;
                case 48:
                    State(); MenuSounds.Play(MenuSound.Navigate, MenuSoundContext.InitialMenu); MenuSounds.Play(MenuSound.Confirm);
                    MenuSounds.Play(MenuSound.Cancel); MenuSounds.Play(MenuSound.Navigate); Sound(MenuSound.Cancel); break;
                case 49:
                    State(); MenuSounds.Play(MenuSound.NewGame); Sound(MenuSound.NewGame);
                    Check(musicVoice.isPlaying && musicVoice.timeSamples > 0,"menu effects preserve background music playback"); break;
                case 50:
                    Check(voice.isPlaying && voice.timeSamples > 0,"New Game arpeggio continues without a redraw restart");
                    sounds.enabled = false; Check(!voice.isPlaying,"disabling scene effects stops playback");
                    MenuSounds.Play(MenuSound.Confirm); Quiet(); sounds.enabled = true; break;
                case 51: State(); MenuSounds.Play(MenuSound.Confirm); Sound(MenuSound.Confirm); break;
                case 52:
                    Property(saves,"IsMenuVisible",true); Property(battle,"IsActive",false);
                    Put(saves,"page",Enum.Parse(Field(saves,"page").FieldType,"Save")); Put(saves,"selectedOption",0);
                    Key(UnityEngine.InputSystem.Key.DownArrow); Call(saves,"Update"); Sound(MenuSound.Navigate);
                    Check((int)Field(saves,"selectedOption").GetValue(saves) == 1,"navigation between save slots remains audible"); break;
                case 53: State(); MenuSounds.Play(MenuSound.Navigate); Quiet(); break;
                case 54:
                    State(); dialogue.OpenChoices("Hospedeiro","Vai descansar?",new[] {"Sim","Nao"},_ => { });
                    dialogue.MoveChoice(1); Quiet();
                    Check(dialogue.SelectedChoice == 0,"typing dialogue does not move or sound a choice"); break;
                case 55:
                    dialogue.RevealAll(); dialogue.MoveChoice(1); Sound(MenuSound.Navigate);
                    Check(dialogue.SelectedChoice == 1,"keyboard/controller choice movement emits navigation"); break;
                case 56: dialogue.SelectChoice(1); Quiet(); break;
                case 57: dialogue.SelectChoice(0); Sound(MenuSound.Navigate); break;
                case 58: dialogue.Close(); dialogue.MoveChoice(1); Quiet(); break;
                case 59:
                    dialogue.Open("Aldeao","Conversa sem opcoes."); dialogue.RevealAll(); dialogue.MoveChoice(1); Quiet(); break;
                case 60:
                    dialogue.OpenChoices("Aldeao","Uma opcao.",new[] {"Continuar"},_ => { });
                    dialogue.RevealAll(); dialogue.MoveChoice(1); Quiet(); break;
                case 61:
                    dialogue.OpenChoices("Aldeao","Tres opcoes.",new[] {"Uma","Duas","Tres"},_ => { });
                    dialogue.RevealAll(); dialogue.MoveChoice(-1); Sound(MenuSound.Navigate);
                    Check(dialogue.SelectedChoice == 2,"wrapping dialogue choices emits navigation"); break;
                case 62:
                    int selection = 0; MenuSounds.Select(ref selection,1); Quiet();
                    Check(selection == 1,"silent navigation outside allowed contexts still changes the selection"); break;
                case 63: Focus(false); dialogue.SelectChoice(0); Quiet(); break;
                case 64:
                    State(); MenuSounds.Play(MenuSound.Confirm); Sound(MenuSound.Confirm); Finish(null); return;
            }
            step++; nextAt = now+.035; nextFrame = Time.frameCount+2;
        }
        catch (Exception error) { Finish(error); }
    }
    private static void Finish(Exception error)
    {
        SessionState.SetBool(Pending,false); EditorApplication.update -= Tick;
        if (fixture != null) UnityEngine.Object.Destroy(fixture);
        if (audioRoot != null) UnityEngine.Object.Destroy(audioRoot);
        if (listener != null) UnityEngine.Object.Destroy(listener);
        if (error != null) Debug.LogException(error);
        else Debug.Log("MENU SOUNDS PLAYBACK PASS: "+checks+" checks for approved PCM, navigation only in title/save/dialogue choices, quiet movement in other menus, confirm/cancel/New Game, focus, deduplication and music continuity.");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
