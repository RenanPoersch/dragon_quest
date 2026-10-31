using DragonQuest.Presentation.Cinematics.Opening;
using DragonQuest.Gameplay.Achievements;
using System;
using DragonQuest.Audio;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public enum FrontEndStep { Continue, Busy, StartVoyage, FinishVoyage }

    // Owns opening artwork, menu fades and voyage timing; it never reads or writes a save.
    public sealed class FrontEndPresentation : IDisposable
    {
        private readonly EndingAchievementPresentation achievementScreen = new EndingAchievementPresentation();
        private IntroPresentation intro;
        private NewGameVoyagePresentation voyage;
        private const float MenuFadeInDuration = 1.2f, NewGameFadeOutDuration = 1.2f;
        private bool startingNewGame, newGameBlackHasDrawn, menuHasDrawn;
        private float newGameFadeOutElapsed, menuFadeInElapsed;

        public bool IsIntroPlaying => intro != null && intro.IsPlaying;
        public bool IsVoyagePlaying => voyage != null && voyage.IsPlaying;
        public bool IsStartingNewGame => startingNewGame;
        public bool IsMenuFadingIn => menuFadeInElapsed < MenuFadeInDuration;
        public bool BlocksMenuInput => startingNewGame || IsMenuFadingIn || intro != null && intro.BlocksMenuInput;
        public bool AcceptsMenuInput => !BlocksMenuInput;
        public float IntroMusicGain => IsIntroPlaying ? intro.Sequence.Opacity : 0;
        public float VoyageMusicGain => IsVoyagePlaying ? voyage.Sequence.MusicGain : 0;

        public void Initialize(bool showIntro)
            => intro = showIntro ? new IntroPresentation() : null;

        public void NewGame()
        {
            if (voyage != null || startingNewGame) return;
            MenuSounds.Play(MenuSound.NewGame);
            startingNewGame = true;
            newGameFadeOutElapsed = 0;
            newGameBlackHasDrawn = false;
        }

        public void BeginVoyage()
        {
            voyage = new NewGameVoyagePresentation();
            startingNewGame = false;
        }

        public FrontEndStep Update(float delta, bool menuVisible, bool titlePage, bool focused)
        {
            intro?.Update(delta);
            bool titleVisible = menuVisible && titlePage && voyage == null && IsIntroPlaying;
            if (!titleVisible) achievementScreen.Dismiss();
            else if (focused)
            {
                if (!achievementScreen.IsVisible)
                {
                    var notification = AchievementService.TakeNotification();
                    if (notification != null) achievementScreen.Show(notification, AchievementService.Progress.Counter);
                }
                achievementScreen.Update(delta);
            }
            if (startingNewGame)
            {
                if (focused)
                {
                    // Load the voyage only after a fully black frame has actually been drawn.
                    if (newGameBlackHasDrawn) return FrontEndStep.StartVoyage;
                    newGameFadeOutElapsed = Mathf.Min(NewGameFadeOutDuration, newGameFadeOutElapsed + delta);
                }
                return FrontEndStep.Busy;
            }
            if (voyage != null)
            {
                if (focused) voyage.Update(delta);
                if (!voyage.IsPlaying)
                {
                    voyage.Dispose(); voyage = null;
                    return FrontEndStep.FinishVoyage;
                }
                return FrontEndStep.Busy;
            }
            if (menuVisible && !IsIntroPlaying && menuHasDrawn)
                menuFadeInElapsed = Mathf.Min(MenuFadeInDuration, menuFadeInElapsed + delta);
            return FrontEndStep.Continue;
        }

        public bool DrawStory()
        {
            if (IsIntroPlaying)
            {
                intro.Draw(); DrawAchievementNotification();
                return true;
            }
            if (voyage != null) { voyage.Draw(); return true; }
            return false;
        }

        public void DrawMenuFade(Texture2D pixel)
        {
            if (IsMenuFadingIn || startingNewGame)
            {
                Matrix4x4 previousMatrix = GUI.matrix;
                Color previousColor = GUI.color;
                int previousDepth = GUI.depth;
                try
                {
                    GUI.matrix = Matrix4x4.identity; GUI.depth = -101;
                    float opacity = startingNewGame
                        ? Mathf.SmoothStep(0, 1, newGameFadeOutElapsed / NewGameFadeOutDuration)
                        : Mathf.SmoothStep(1, 0, menuFadeInElapsed / MenuFadeInDuration);
                    GUI.color = new Color(0, 0, 0, opacity);
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
                    if (startingNewGame && newGameFadeOutElapsed >= NewGameFadeOutDuration
                        && Event.current.type == EventType.Repaint) newGameBlackHasDrawn = true;
                }
                finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
            }
            if (Event.current.type == EventType.Repaint) menuHasDrawn = true;
        }

        private void DrawAchievementNotification()
        {
            if (!achievementScreen.IsVisible) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                    (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.depth = -110; GUI.color = Color.white;
                achievementScreen.Draw();
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        public void Dispose() { voyage?.Dispose(); intro?.Dispose(); }
    }
}
