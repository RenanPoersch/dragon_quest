using System.Collections.Generic;
using DragonQuest.Input;
using UnityEngine;

namespace DragonQuest.Audio
{
    public enum MenuSound { Navigate, Confirm, Cancel, NewGame }
    public enum MenuSoundContext { Other, InitialMenu, SaveSlots, DialogueChoices }

    public static class MenuSounds
    {
        public static void Play(MenuSound sound, MenuSoundContext context = MenuSoundContext.Other)
            => MenuSoundController.Instance?.Play(sound, context);
        public static void Navigate<T>(T before, T after, MenuSoundContext context = MenuSoundContext.Other)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after)) Play(MenuSound.Navigate, context);
        }
        public static void Select(ref int selection, int next, MenuSoundContext context = MenuSoundContext.Other)
        { Navigate(selection, next, context); selection = next; }
    }

    // A separate scene object keeps short UI effects independent of the music sources.
    [DisallowMultipleComponent]
    public sealed class MenuSoundController : MonoBehaviour
    {
        internal static MenuSoundController Instance { get; private set; }
        private readonly AudioClip[] clips = new AudioClip[4];
        private AudioSource voice;
        private int playedFrame = -1;
        public MenuSound? CurrentSound { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.loop = false;
            voice.spatialBlend = 0; voice.dopplerLevel = 0;
            voice.priority = 32; voice.volume = .6f;
            foreach (MenuSound sound in System.Enum.GetValues(typeof(MenuSound)))
            {
                clips[(int)sound] = Resources.Load<AudioClip>("MenuSounds/" + sound.ToString().ToLowerInvariant());
                if (clips[(int)sound] == null) Debug.LogError("Som de menu ausente: " + sound, this);
            }
        }

        public void Play(MenuSound sound, MenuSoundContext context = MenuSoundContext.Other)
        {
            // Only these UI flows may emit cursor movement audio. Other menus
            // keep their existing confirm/cancel sounds and navigate silently.
            if (sound == MenuSound.Navigate && context != MenuSoundContext.InitialMenu
                && context != MenuSoundContext.SaveSlots && context != MenuSoundContext.DialogueChoices) return;
            if (!isActiveAndEnabled || !GameInput.IsFocused || voice == null) return;
            // Confirmation wins over movement, cancellation over confirmation, and
            // New Game over all three. Repeated IMGUI passes cannot restart a sound.
            if (playedFrame == Time.frameCount && CurrentSound.HasValue && (int)sound <= (int)CurrentSound.Value) return;
            AudioClip clip = clips[(int)sound];
            if (clip == null) return;
            playedFrame = Time.frameCount; CurrentSound = sound;
            voice.Stop(); voice.clip = clip; voice.Play();
        }

        private void OnDisable() { if (voice != null) voice.Stop(); CurrentSound = null; playedFrame = -1; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
