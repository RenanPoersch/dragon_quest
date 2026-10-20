using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Input;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CombatSoundValidation
{
    private const string Pending = "DragonQuest.CombatSounds.Validation";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] private sealed class Manifest { public Row[] sounds; }
    [Serializable] private sealed class Row
    { public string id, source, actor, element, sha256; public float duration; }
    private static Row[] rows;
    private static int checks, index, phase, playCount;
    private static double due, began;
    private static GameObject fixture, listener;
    private static PrototypeBattleController battle;
    private static BattleSoundController sounds;
    private static AudioSource observed, music;
    private static Dictionary<string, AbilityDefinition> definitions;
    static CombatSoundValidation() { if (SessionState.GetBool(Pending, false)) EditorApplication.update += Tick; }

    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception("Combat sound " + index + ": " + message); }
    private static void Put(object target, string field, object value)
        => target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Property(object target, string name, object value)
        => Put(target, "<" + name + ">k__BackingField", value);
    private static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static AudioSource[] Voices => (AudioSource[])typeof(BattleSoundController).GetField("voices", Private).GetValue(sounds);
    private static void Focus(bool value) => typeof(GameInput).GetField("hasFocus", BindingFlags.Static | BindingFlags.NonPublic)
        .SetValue(null, (Func<bool>)(() => value));
    private static Row[] ReadRows() => JsonUtility.FromJson<Manifest>("{\"sounds\":"
        + File.ReadAllText("Docs/CombatSoundManifest.json") + "}").sounds;

    public static void RunPlayback()
    {
        try
        {
            rows = ReadRows(); checks = 0;
            Check(rows.Length == 47 && BattleSoundCatalog.ResourceKeys.Count == 47, "all 47 approved recordings are present");
            Check(rows.Select(r => r.id).OrderBy(x => x).SequenceEqual(BattleSoundCatalog.ResourceKeys), "resource catalogue exactly matches approval manifest");
            foreach (Row row in rows)
            {
                string path = "Assets/Resources/CombatSounds/" + row.id + ".wav";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                using (var hash = SHA256.Create())
                    Check(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() == row.sha256,
                        row.id + " is byte-identical to the installed approval manifest");
                var clip = Resources.Load<AudioClip>("CombatSounds/" + row.id);
                Check(clip != null && clip.channels == 2 && clip.frequency == 48000 && Math.Abs(clip.length - row.duration) < .001f,
                    row.id + " preserves stereo, rate and duration");
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Check(importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM
                    && importer.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad
                    && importer.defaultSampleSettings.preloadAudioData && !importer.forceToMono,
                    row.id + " imports preloaded PCM without mono conversion or normalization");
                var samples = new float[clip.samples * clip.channels];
                Check(clip.GetData(samples, 0) && samples.Any(x => Math.Abs(x) > .01f) && samples.All(x => Math.Abs(x) < .81f),
                    row.id + " contains valid, unclipped audio samples");
            }
            Debug.Log("COMBAT SOUND ASSETS PASS: " + checks + " checks.");
            SessionState.SetInt(Pending + ".Checks", checks); SessionState.SetBool(Pending, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Setup()
    {
        checks = SessionState.GetInt(Pending + ".Checks", 0); rows = ReadRows(); began = EditorApplication.timeSinceStartup;
        Application.runInBackground = true;
        AudioListener.pause = false;
        Focus(true);
        listener = new GameObject("Combat SFX validation listener"); listener.AddComponent<AudioListener>();
        fixture = new GameObject("Combat SFX validation");
        var hero = fixture.AddComponent<PlayerMovement>();
        var dialogue = fixture.AddComponent<DialogueController>();
        battle = fixture.AddComponent<PrototypeBattleController>();
        battle.Initialize(hero, dialogue, new ExplorationProgress());
        battle.enabled = false; // Advance the presentation fixtures explicitly, without enemy AI input.
        fixture.GetComponent<PrototypePostBattleHud>().enabled = false;
        Check(fixture.GetComponent<BattleSoundController>() == null, "battle logic also works in scenes without audio presentation");
        sounds = fixture.AddComponent<BattleSoundController>(); sounds.Initialize(battle);
        Check(sounds != null && Voices.Length == 8, "scene composition installs one bounded voice pool");
        battle.Initialize(hero, dialogue, new ExplorationProgress());
        sounds.Initialize(battle);
        Check(fixture.GetComponents<BattleSoundController>().Length == 1 && Voices.Length == 8
            && fixture.GetComponentsInChildren<AudioSource>().Length == 8, "save reinitialization does not duplicate components or voices");
        music = new GameObject("Independent background music").AddComponent<AudioSource>();
        music.transform.SetParent(fixture.transform);
        var musicClip = AudioClip.Create("Background continuity fixture", 48000 * 4, 1, 48000, false);
        var musicData = new float[musicClip.samples];
        for (int i = 0; i < musicData.Length; i++) musicData[i] = Mathf.Sin(i * Mathf.PI * 2 * 220 / 48000) * .02f;
        musicClip.SetData(musicData, 0); music.clip = musicClip; music.loop = true; music.Play();
        definitions = new Dictionary<string, AbilityDefinition>();
        foreach (Type type in new[] { typeof(PrototypeBattleFactory), typeof(PrototypePartyFactory), typeof(PrototypeCastleBattleFactory), typeof(CastleBattleRules), typeof(HeroUltimates) })
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType == typeof(AbilityDefinition))
                {
                    var ability = (AbilityDefinition)field.GetValue(null);
                    if (!definitions.ContainsKey(ability.SourceAbilityId)) definitions.Add(ability.SourceAbilityId, ability);
                }
        ValidateAcceptedAction();
        index = 0; phase = 0;
    }

    private static void ValidateAcceptedAction()
    {
        battle.StartBattle(); battle.EntryTransition.Cancel();
        var actor = battle.Session.CurrentActor;
        var victim = battle.Session.Combatants.First(c => c.Team == CombatTeam.Enemy);
        Check(actor != null && actor.Team == CombatTeam.Party, "real combat fixture starts on a player action");
        int count = sounds.PlayCount;
        Check(!battle.TryPlayerAction(null, victim) && sounds.PlayCount == count, "a rejected action produces no sound");
        var attack = actor.Definition.Abilities.First(a => a.SourceAbilityId == "attack");
        Check(battle.TryPlayerAction(attack, victim) && sounds.PlayCount == count + 1,
            "an accepted player action emits exactly one playback event");
        Check(!battle.TryPlayerAction(attack, victim) && sounds.PlayCount == count + 1,
            "repeated input during the animation does not replay audio");
        Check(!BattleSoundCatalog.Resolve("aren", new AbilityDefinition("use-item", "Pocao", "", 0, 0,
            AbilityEffect.Defend, TargetRule.Self)).IsValid, "items never fall through to a defend or attack sound");
        Check(!BattleSoundCatalog.Resolve("aren", null).IsValid, "missing actions remain silent");
        Check(BattleSoundCatalog.Resolve("soldier", PrototypeBattleFactory.Attack).Key == "attack-guard", "soldiers share the guard recording");
        Check(BattleSoundCatalog.Resolve("wolf-3", PrototypeBattleFactory.Attack).Key == "attack-wolf", "numbered regional enemies retain their family recording");
        Check(BattleSoundCatalog.Resolve("aren", HeroUltimates.Omnislash).StartOffset(5.32f) == 0
            && BattleSoundCatalog.Resolve("lia", HeroUltimates.Salvation).StartOffset(3.65f) == 0
            && BattleSoundCatalog.Resolve("bram", HeroUltimates.Aegis).StartOffset(2.85f) == 0,
            "all three limits retain their authored timeline, including all 13 cuts");
        Check(BattleSoundCatalog.Resolve("aren", PrototypePartyFactory.Thunder).Key == "thunder-all",
            "single-target spells use the approved All recording");
        Check(!BattleSoundCatalog.Resolve("aren", definitions["prepare-meteor"]).IsValid,
            "meteor preparation is intentionally silent");
        sounds.StopAll();
    }

    private static AbilityDefinition Ability(Row row)
    {
        if (row.id == "confused-attack") return BattleSession.MindControlAttack;
        if (row.id.StartsWith("infuse-", StringComparison.Ordinal))
        {
            string element = row.id.Substring(7);
            var value = element == "thunder" ? DamageElement.Lightning : (DamageElement)Enum.Parse(typeof(DamageElement), element, true);
            return new AbilityDefinition("attack", "Ataque", "", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy, element: value);
        }
        var ability = definitions[row.source];
        if (!row.id.EndsWith("-all", StringComparison.Ordinal)) return ability;
        return new AbilityDefinition("materia-instance/" + ability.Id, ability.Name + " + All", "", ability.MpCost, ability.Power,
            ability.Effect, ability.Targets == TargetRule.Ally ? TargetRule.AllAllies : TargetRule.AllEnemies,
            sourceAbilityId: ability.SourceAbilityId, element: ability.Element, menuGroup: AbilityMenuGroup.Magic);
    }

    private static void Present(Row row)
    {
        sounds.StopAll();
        var ability = Ability(row);
        string actorId = row.actor == "guard/soldier" ? "guard" : row.actor == "dragon" ? DragonProgress.EnemyId
            : string.IsNullOrEmpty(row.actor) ? "aren" : row.actor;
        var actor = new CombatantState(actorId, new CombatantDefinition("Fixture", "", 100, 100, 20, 5, 20, 10,
            ElementalAffinities.Neutral, 0, 0, 0, PrototypeBattleFactory.Attack), CombatTeam.Party);
        Property(battle, "IsActive", true);
        var session = PrototypeCastleBattleFactory.Create(battle.Party);
        Property(session.Rules, "MeteorAbsorbed", row.id == "meteor-absorbed");
        Property(battle, "Session", session);
        Put(battle, "pendingMeteorImpact", row.id == "flaming-meteor");
        int before = sounds.PlayCount;
        Call(battle, "PresentAction", actor, ability, new CombatantState[0]);
        Check(sounds.CurrentSound == row.id && sounds.PlayCount == before + 1, row.id + " resolves from the real presentation event exactly once");
        var cue = BattleSoundCatalog.Resolve(actorId, ability, row.id == "meteor-absorbed");
        float offset = cue.StartOffset(battle.ActionImpactAt - battle.ActionStartedAt);
        observed = Voices.First(v => v.clip != null && v.clip.name == row.id);
        Check(observed.spatialBlend == 0 && observed.pitch == 1 && !observed.loop && Math.Abs(observed.volume - .85f) < .001f,
            row.id + " preserves stereo/pitch and uses the battle effect gain");
        due = AudioSettings.dspTime + offset + .09;
        playCount = sounds.PlayCount;
    }

    private static void Lifecycle()
    {
        // Two full sequences overlap; a later short action cannot clip the first tail.
        Present(rows.First(r => r.id == "omnislash"));
        var previous = observed;
        // Layer another full sequence without clearing the first source.
        Call(battle, "PresentAction", battle.PresentingActor, HeroUltimates.Salvation, new CombatantState[0]);
        Check(previous.isPlaying && Voices.Any(v => v != previous && v.isPlaying && v.clip.name == "lia-salvation")
            && music.isPlaying, "a new action preserves the previous tail and independent background music");
        Focus(false); Call(sounds, "LateUpdate");
        Check(Voices.All(v => !v.isPlaying) && sounds.CurrentSound == null, "focus loss cancels active and scheduled effects");
        int count = sounds.PlayCount;
        Call(battle, "PresentAction", battle.PresentingActor, PrototypeBattleFactory.Attack, new CombatantState[0]);
        Check(sounds.PlayCount == count, "unfocused actions are silent and are not queued for later");
        Focus(true); sounds.Initialize(battle);
        Call(battle, "PresentAction", battle.PresentingActor, HeroUltimates.Omnislash, new CombatantState[0]);
        Check(sounds.PlayCount == count + 1, "reinitialization retains a single action subscription");
        sounds.enabled = false;
        Check(Voices.All(v => !v.isPlaying), "disabling playback stops all battle voices");
        sounds.enabled = true;
        Call(battle, "PresentAction", battle.PresentingActor, HeroUltimates.Salvation, new CombatantState[0]);
        Property(battle, "IsActive", false); Call(sounds, "LateUpdate");
        Check(Voices.All(v => !v.isPlaying), "leaving battle cancels pending playback");
        Check(music.isPlaying && music.timeSamples > 0, "effects never restart or stop the music source");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.isPaused) { EditorApplication.isPaused = false; return; }
        try
        {
            if (fixture == null) Setup();
            if (EditorApplication.timeSinceStartup - began > 150) throw new TimeoutException("Combat sound validation timed out.");
            if (index >= rows.Length) { Lifecycle(); Finish(null); return; }
            if (EditorApplication.timeSinceStartup - began < .4) return;
            if (phase == 0) { Present(rows[index]); phase = 1; return; }
            if (AudioSettings.dspTime < due) return;
            Check(observed.isPlaying && observed.timeSamples > 0, rows[index].id + " plays and advances real Unity audio samples"
                + " (playing=" + observed.isPlaying + ", sample=" + observed.timeSamples + ", dsp=" + AudioSettings.dspTime
                + ", due=" + due + ", current=" + sounds.CurrentSound + ", focused=" + GameInput.IsFocused + ")");
            Check(sounds.PlayCount == playCount, "idle rendering and update frames do not duplicate playback");
            index++; phase = 0;
            if (index % 8 == 0) Debug.Log("COMBAT SOUND PLAYBACK PROGRESS: " + index + "/" + rows.Length);
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
        if (fixture != null) UnityEngine.Object.Destroy(fixture);
        if (listener != null) UnityEngine.Object.Destroy(listener);
        if (error != null) Debug.LogException(error);
        else Debug.Log("COMBAT SOUNDS PLAYBACK PASS: " + checks + " checks; all 47 approved PCM recordings, presentation timing, real sample advancement, rejected/duplicate actions, focus, lifecycle and music continuity.");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
