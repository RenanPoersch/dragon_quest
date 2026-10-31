using DragonQuest.Gameplay.Battle;
using System.Collections.Generic;
using DragonQuest.Input;
using UnityEngine;

namespace DragonQuest.Audio
{
    [DisallowMultipleComponent]
    public sealed class BattleSoundController : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly AudioSource[] voices = new AudioSource[VoiceCount];
        private readonly double[] occupiedUntil = new double[VoiceCount];
        private BattleController battle;
        public string CurrentSound { get; private set; }
        public int PlayCount { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = new GameObject("Battle SFX " + (i + 1));
                voice.transform.SetParent(transform, false);
                voices[i] = voice.AddComponent<AudioSource>();
                voices[i].playOnAwake = false; voices[i].loop = false;
                voices[i].spatialBlend = 0; voices[i].dopplerLevel = 0;
                voices[i].priority = 32; voices[i].volume = .85f;
            }
            // Import/preload before combat so the first hit does not wait for disk/decode.
            foreach (string key in BattleSoundCatalog.ResourceKeys)
            {
                var clip = Resources.Load<AudioClip>("CombatSounds/" + key);
                clips.Add(key, clip);
                if (clip == null) Debug.LogError("Som de combate ausente: " + key, this);
            }
        }

        public void Initialize(BattleController controller)
        {
            Unsubscribe(); StopAll(); battle = controller;
            if (isActiveAndEnabled && battle != null) battle.ActionPresented += OnActionPresented;
        }

        private void OnActionPresented()
        {
            if (!isActiveAndEnabled || battle == null || !battle.IsActive || !GameInput.IsFocused) return;
            var cue = BattleSoundCatalog.Resolve(battle.PresentingActor?.Id, battle.PresentingAbility,
                battle.CastleRules?.MeteorAbsorbed == true);
            if (!cue.IsValid || !clips.TryGetValue(cue.Key, out var clip) || clip == null) return;
            float elapsed = Time.unscaledTime - battle.ActionStartedAt;
            float delay = Mathf.Max(0, cue.StartOffset(battle.ActionImpactAt - battle.ActionStartedAt) - elapsed);
            double dsp = AudioSettings.dspTime;
            int chosen = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (occupiedUntil[i] <= dsp) { chosen = i; break; }
                if (occupiedUntil[i] < occupiedUntil[chosen]) chosen = i;
            }
            var voice = voices[chosen];
            voice.Stop(); voice.clip = clip;
            // Keep the approved PCM unchanged: no resampling, pitch shift or clipping of tails.
            voice.pitch = 1;
            if (delay > 0) voice.PlayScheduled(dsp + delay);
            else voice.Play();
            occupiedUntil[chosen] = dsp + delay + clip.length;
            CurrentSound = cue.Key; PlayCount++;
        }

        private void LateUpdate()
        {
            if (!GameInput.IsFocused || battle == null || !battle.IsActive && !battle.HasEnded) StopAll();
        }

        public void StopAll()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (occupiedUntil[i] > 0 && voices[i] != null) voices[i].Stop();
                occupiedUntil[i] = 0;
            }
            CurrentSound = null;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) StopAll(); }
        private void Unsubscribe() { if (battle != null) battle.ActionPresented -= OnActionPresented; }
        private void OnEnable() { if (battle != null) battle.ActionPresented += OnActionPresented; }
        private void OnDisable() { Unsubscribe(); StopAll(); }
        private void OnDestroy() { Unsubscribe(); }
    }
}
