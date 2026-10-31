using DragonQuest.Gameplay.Battle;
using DragonQuest.Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonQuest.Presentation.Battle
{
    // The reveal waits for a complete battle repaint, including its lazily loaded artwork.
    public sealed class BattleEntrySequence
    {
        public const float SwirlDuration = 3.2f, CoveredDuration = .12f, RevealDuration = .32f;
        public const float KingFadeOutDuration = 2f, KingBlackDuration = 3f, KingFadeInDuration = 2f;
        public float Elapsed { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsFadeOnly { get; private set; }
        private float CoverAt => IsFadeOnly ? KingFadeOutDuration : SwirlDuration;
        private float HoldDuration => IsFadeOnly ? KingBlackDuration : CoveredDuration;
        private float UncoverDuration => IsFadeOnly ? KingFadeInDuration : RevealDuration;
        public bool ShowsBattle => IsPlaying && Elapsed >= CoverAt;
        public bool CanStartFinalMusic => IsPlaying && IsFadeOnly && blackDrawn;
        public float SwirlProgress => Mathf.Clamp01(Elapsed / SwirlDuration);
        public float BlackOpacity => IsFadeOnly && Elapsed < CoverAt
            ? Mathf.SmoothStep(0, 1, Elapsed / CoverAt)
            : 1 - Mathf.SmoothStep(0, 1, (Elapsed - CoverAt - HoldDuration) / UncoverDuration);
        private bool battleReady, blackDrawn, musicReady;

        public void Begin(bool fadeOnly = false)
        { Elapsed = 0; battleReady = blackDrawn = musicReady = false; IsFadeOnly = fadeOnly; IsPlaying = true; }
        public void MarkBlackDrawn() { if (ShowsBattle && BlackOpacity >= 1) blackDrawn = true; }
        public void MarkMusicReady() { if (CanStartFinalMusic) musicReady = true; }
        public void MarkBattleReady() { if (ShowsBattle) battleReady = true; }
        public void Advance(float delta)
        {
            if (!IsPlaying) return;
            Elapsed += Mathf.Max(0, delta);
            if (!battleReady || IsFadeOnly && (!blackDrawn || !musicReady)) Elapsed = Mathf.Min(Elapsed, CoverAt);
            if (Elapsed >= CoverAt + HoldDuration + UncoverDuration) Cancel();
        }
        public void Cancel() { IsPlaying = IsFadeOnly = false; Elapsed = 0; battleReady = blackDrawn = musicReady = false; }
    }

    public sealed class BattleEntryTransition : MonoBehaviour
    {
        public BattleEntrySequence Sequence { get; } = new BattleEntrySequence();
        public bool IsPlaying => Sequence.IsPlaying;
        public bool HidesBattle => IsPlaying && !Sequence.ShowsBattle;
        private RenderTexture snapshot, frame;
        private Material swirl;

        public void Begin()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            BeginFromCamera(Camera.main);
        }

        private void BeginFromCamera(Camera view)
        {
            Cancel();
            if (view == null) return;
            bool fadeOnly = GetComponent<BattleController>()?.IsCastleEncounter == true;
            if (!fadeOnly)
            {
                Shader shader = Resources.Load<Shader>("BattleEntrySwirl");
                if (shader == null || !shader.isSupported) return;
                swirl = new Material(shader) { name = "Battle entry / swirl", hideFlags = HideFlags.HideAndDontSave };
            }
            CaptureExploration(view);
            Sequence.Begin(fadeOnly);
            if (!fadeOnly) RenderSwirl();
        }

        private void CaptureExploration(Camera view)
        {
            int width = view.targetTexture != null ? view.targetTexture.width : Screen.width;
            int height = view.targetTexture != null ? view.targetTexture.height : Screen.height;
            snapshot = CreateTexture("Battle entry / exploration", width, height, 24);
            frame = CreateTexture("Battle entry / frame", width, height, 0);
            RenderTexture previousTarget = view.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            float previousAspect = view.aspect;
            float previousFieldOfView = view.fieldOfView;
            Rect previousViewport = view.rect;
            RenderTexture artwork = null;
            try
            {
                RenderTexture destination = snapshot;
                /* Temporariamente desativado para testar a captura normal, sem esticar o salao.
                if (!view.orthographic && GetComponent<BattleController>()?.IsCastleEncounter == true)
                {
                    // Capture the painting's full frame, then stretch it to the display.
                    // Otherwise the exploration camera's letterbox becomes black spiral bands.
                    artwork = CreateTexture("Battle entry / full artwork", Mathf.RoundToInt(height * ThroneRoomLayout.ArtAspect), height, 24);
                    view.rect = new Rect(0, 0, 1, 1);
                    view.aspect = ThroneRoomLayout.ArtAspect;
                    view.fieldOfView = ThroneRoomLayout.FieldOfView;
                    destination = artwork;
                }
                */
                // Render the exploration camera directly. A screen backbuffer can be empty in
                // the Editor or while changing views, and ScreenCapture is an optional module.
                RenderTexture.active = destination;
                GL.Clear(true, true, Color.black);
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    var request = new RenderPipeline.StandardRequest { destination = destination };
                    RenderPipeline.SubmitRenderRequest(view, request);
                }
                else
                {
                    view.targetTexture = destination;
                    view.Render();
                }
                // if (artwork != null) Graphics.Blit(artwork, snapshot);
            }
            finally
            {
                view.targetTexture = previousTarget;
                view.aspect = previousAspect;
                view.fieldOfView = previousFieldOfView;
                view.rect = previousViewport;
                RenderTexture.active = previousActive;
                if (artwork != null) { artwork.Release(); DestroyOwned(artwork); }
            }
        }

        private static RenderTexture CreateTexture(string label, int width, int height, int depth)
        {
            var texture = new RenderTexture(Mathf.Max(1, width), Mathf.Max(1, height), depth, RenderTextureFormat.ARGB32)
                { name = label, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
            return texture;
        }

        private void RenderSwirl()
        {
            swirl.SetFloat("_Progress", Sequence.SwirlProgress);
            swirl.SetFloat("_Aspect", (float)snapshot.width / snapshot.height);
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(snapshot, frame, swirl); }
            finally { RenderTexture.active = previous; }
        }

        private void Update()
        {
            if (!IsPlaying) return;
            Sequence.Advance(Time.unscaledDeltaTime);
            if (!IsPlaying) { ReleaseTextures(); GameInput.ConsumeGamepadFrame(); }
            else if (!Sequence.ShowsBattle && !Sequence.IsFadeOnly) RenderSwirl();
        }

        public void MarkBattleReady()
        {
            if (Event.current?.type == EventType.Repaint) Sequence.MarkBattleReady();
        }

        private void OnGUI()
        {
            if (!IsPlaying) return;
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -10000; GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
                var viewport = new Rect(0, 0, Screen.width, Screen.height);
                if (!Sequence.ShowsBattle)
                {
                    GUI.DrawTexture(viewport, Sequence.IsFadeOnly ? snapshot : frame, ScaleMode.StretchToFill, false);
                }
                if (Sequence.ShowsBattle || Sequence.IsFadeOnly)
                {
                    GUI.color = new Color(0, 0, 0, Sequence.BlackOpacity);
                    GUI.DrawTexture(viewport, Texture2D.whiteTexture);
                    Sequence.MarkBlackDrawn();
                }
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        private void OnDisable() => Cancel();
        public void Cancel()
        {
            Sequence.Cancel();
            ReleaseTextures();
        }
        private void ReleaseTextures()
        {
            if (snapshot != null) { snapshot.Release(); DestroyOwned(snapshot); snapshot = null; }
            if (frame != null) { frame.Release(); DestroyOwned(frame); frame = null; }
            if (swirl != null) { DestroyOwned(swirl); swirl = null; }
        }
        private static void DestroyOwned(Object asset)
        {
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }
    }
}
