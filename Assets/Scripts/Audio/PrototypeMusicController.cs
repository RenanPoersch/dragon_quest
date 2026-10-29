using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Interactions;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Audio
{
    // Sources belong to the scene: returning to the title cannot leave an old soundtrack behind.
    [DisallowMultipleComponent]
    public sealed class PrototypeMusicController : MonoBehaviour
    {
        private const float MusicVolume = .52f;
        private readonly Dictionary<MusicCue, AudioClip> clips = new Dictionary<MusicCue, AudioClip>();
        private readonly AudioSource[] sources = new AudioSource[2];
        private readonly float[] from = new float[2], target = new float[2];
        private PrototypeSaveController saves;
        private PrototypeLocations locations;
        private PrototypeBattleController battle;
        private DragonInteraction dragon;
        private InnRestController rest;
        private PrototypeEndingController endings;
        private int current = -1;
        private float elapsed, duration;
        public MusicCue CurrentCue { get; private set; }

        public void Initialize(PrototypeSaveController menus, PrototypeLocations maps, PrototypeBattleController combat)
        {
            if (battle != null) battle.EncounterStarted -= OnEncounterStarted;
            saves = menus; locations = maps; battle = combat;
            if (battle != null) battle.EncounterStarted += OnEncounterStarted;
            dragon = GetComponentInChildren<DragonInteraction>(true);
            rest = GetComponent<InnRestController>();
            endings = GetComponent<PrototypeEndingController>();
            if (sources[0] != null) return;
            if (rest != null)
            {
                var sleep = Resources.Load<AudioClip>(MusicCuePolicy.ResourceName(MusicCue.Sleep));
                if (sleep != null) { clips[MusicCue.Sleep] = sleep; rest.ConfigureSleepMusic(sleep.length); }
            }
            for (int i = 0; i < sources.Length; i++)
            {
                var voice = new GameObject("Music " + (i + 1));
                voice.transform.SetParent(transform, false);
                sources[i] = voice.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].loop = true;
                sources[i].spatialBlend = 0;
                sources[i].dopplerLevel = 0;
                sources[i].priority = 0;
                sources[i].volume = 0;
            }
        }

        private void OnEncounterStarted()
        {
            if (!isActiveAndEnabled || sources[0] == null || !battle.IsCastleEncounter) return;
            TransitionTo(MusicCue.FinalBoss);
            Advance(0);
        }

        private void LateUpdate()
        {
            if (saves == null || locations == null || battle == null || sources[0] == null) return;
            if (endings != null && endings.IsFadingBattle)
            { Advance(Time.unscaledDeltaTime); return; }
            var cue = MusicCuePolicy.Select(saves.IsIntroPlaying, saves.IsFrontEndVisible, battle.HasEnded,
                battle.IsActive, battle.IsDragonEncounter, battle.IsCastleEncounter, battle.IsPostBattleOpen,
                battle.Session?.Outcome == BattleOutcome.Defeat && !battle.IsPresentingAction, locations.LocationId,
                dragon != null && dragon.IsBattlePrelude,
                new MusicStoryState(saves.IsVoyagePlaying, rest != null && rest.IsSleeping,
                    battle.Dragon != null && battle.Dragon.IsDefeated,
                    endings != null ? endings.CurrentEnding : DragonQuest.Progression.GameEnding.None));
            if (!MusicCuePolicy.IsAvailable(cue)) cue = MusicCue.Silence;
            TransitionTo(cue);
            Advance(Time.unscaledDeltaTime);
        }

        private void TransitionTo(MusicCue cue)
        {
            if (cue == CurrentCue) return;
            CurrentCue = cue;
            for (int i = 0; i < 2; i++) { from[i] = sources[i].volume; target[i] = 0; }
            elapsed = 0;
            duration = cue == MusicCue.GoodEnding || cue == MusicCue.MindControlEnding || cue == MusicCue.MeteorEnding || cue == MusicCue.FinalBoss ? 0
                : cue == MusicCue.DragonLair ? .08f
                : cue == MusicCue.Silence || cue == MusicCue.Sleep ? .12f : cue == MusicCue.Victory ? .18f : .65f;
            if (cue == MusicCue.Silence) { current = -1; return; }
            if (!clips.TryGetValue(cue, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(MusicCuePolicy.ResourceName(cue));
                clips.Add(cue, clip);
                if (clip == null) Debug.LogError("Trilha ausente: " + MusicCuePolicy.ResourceName(cue), this);
            }
            if (clip == null) { current = -1; return; }
            int next = current == 0 ? 1 : current == 1 ? 0 : sources[0].volume <= sources[1].volume ? 0 : 1;
            AudioSource voice = sources[next];
            voice.Stop(); voice.clip = clip; voice.volume = from[next] = 0;
            voice.loop = MusicCuePolicy.ShouldLoop(cue);
            // Skip the final recording's silent lead-in so its first hit joins the entry swirl.
            voice.timeSamples = cue == MusicCue.FinalBoss ? Mathf.RoundToInt(clip.frequency * .66f) : 0;
            target[next] = MusicVolume;
            current = next;
            voice.Play();
        }

        private void Advance(float delta)
        {
            elapsed += delta;
            float blend = duration <= 0 ? 1 : Mathf.SmoothStep(0, 1, elapsed / duration);
            for (int i = 0; i < 2; i++)
            {
                float gain = Mathf.Lerp(from[i], target[i], blend);
                // Cinematic clocks synchronize the opening/final fades; internal cuts keep playing.
                if (i == current)
                {
                    if (endings != null && endings.IsFadingBattle) gain *= endings.BattleMusicGain;
                    else if (CurrentCue == MusicCue.Intro) gain *= saves.IntroMusicGain;
                    else if (CurrentCue == MusicCue.Voyage) gain *= saves.VoyageMusicGain;
                    else if (CurrentCue == MusicCue.GoodEnding || CurrentCue == MusicCue.MindControlEnding
                        || CurrentCue == MusicCue.MeteorEnding) gain *= endings != null ? endings.MusicGain : 1;
                }
                sources[i].volume = gain;
                if (blend >= 1 && target[i] == 0 && sources[i].isPlaying) sources[i].Stop();
            }
        }

        private void OnDestroy()
        {
            if (battle != null) battle.EncounterStarted -= OnEncounterStarted;
        }

        private void OnDisable()
        {
            foreach (AudioSource source in sources) if (source != null) source.Stop();
            CurrentCue = MusicCue.Silence; current = -1;
        }
    }
}
