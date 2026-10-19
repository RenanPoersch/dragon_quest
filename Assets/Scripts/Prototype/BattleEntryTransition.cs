using System.Collections;
using DragonQuest.Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonQuest.Prototype
{
    // The reveal waits for a complete battle repaint, including its lazily loaded artwork.
    public sealed class BattleEntrySequence
    {
        public const float SwirlDuration = 1.5f, CoveredDuration = .12f, RevealDuration = .32f;
        public float Elapsed { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool ShowsBattle => IsPlaying && Elapsed >= SwirlDuration;
        public float SwirlProgress => Mathf.Clamp01(Elapsed / SwirlDuration);
        public float BlackOpacity => 1 - Mathf.SmoothStep(0, 1,
            (Elapsed - SwirlDuration - CoveredDuration) / RevealDuration);
        private bool battleReady;

        public void Begin() { Elapsed = 0; battleReady = false; IsPlaying = true; }
        public void MarkBattleReady() { if (ShowsBattle) battleReady = true; }
        public void Advance(float delta)
        {
            if (!IsPlaying) return;
            Elapsed += Mathf.Max(0, delta);
            if (!battleReady) Elapsed = Mathf.Min(Elapsed, SwirlDuration);
            if (Elapsed >= SwirlDuration + CoveredDuration + RevealDuration) Cancel();
        }
        public void Cancel() { IsPlaying = false; Elapsed = 0; battleReady = false; }
    }

    public sealed class BattleEntryTransition : MonoBehaviour
    {
        public BattleEntrySequence Sequence { get; } = new BattleEntrySequence();
        public bool IsPlaying => Sequence.IsPlaying;
        public bool HidesBattle => IsPlaying && !Sequence.ShowsBattle;
        private Coroutine capture;
        private RenderTexture snapshot, frame;
        private Material swirl;
        private bool captured;

        public void Begin()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            Cancel();
            Shader shader = Resources.Load<Shader>("BattleEntrySwirl");
            if (shader != null && shader.isSupported)
                swirl = new Material(shader) { name = "Battle entry / swirl", hideFlags = HideFlags.HideAndDontSave };
            Sequence.Begin();
            capture = StartCoroutine(CaptureExploration());
        }

        private IEnumerator CaptureExploration()
        {
            // The battle HUD stays hidden for this frame, so the capture contains the exploration view.
            yield return new WaitForEndOfFrame();
            snapshot = CreateTexture("Battle entry / exploration");
            frame = CreateTexture("Battle entry / frame");
            ScreenCapture.CaptureScreenshotIntoRenderTexture(snapshot);
            captured = true;
            capture = null;
        }

        private static RenderTexture CreateTexture(string label)
        {
            var texture = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 0, RenderTextureFormat.ARGB32)
                { name = label, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
            return texture;
        }

        private void Update()
        {
            if (!IsPlaying || !captured) return;
            Sequence.Advance(Time.unscaledDeltaTime);
            if (!IsPlaying) { ReleaseTextures(); GameInput.ConsumeGamepadFrame(); }
        }

        public void MarkBattleReady()
        {
            if (Event.current?.type == EventType.Repaint) Sequence.MarkBattleReady();
        }

        private void OnGUI()
        {
            if (!IsPlaying) return;
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (!captured || Event.current.type != EventType.Repaint) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -10000; GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
                var viewport = new Rect(0, 0, Screen.width, Screen.height);
                if (!Sequence.ShowsBattle)
                {
                    if (swirl != null)
                    {
                        swirl.SetFloat("_Progress", Sequence.SwirlProgress);
                        swirl.SetFloat("_Aspect", (float)snapshot.width / snapshot.height);
                        RenderTexture previous = RenderTexture.active;
                        try { Graphics.Blit(snapshot, frame, swirl); }
                        finally { RenderTexture.active = previous; }
                        GUI.DrawTexture(viewport, frame, ScaleMode.StretchToFill, false);
                    }
                    else
                    {
                        GUI.DrawTexture(viewport, snapshot, ScaleMode.StretchToFill, false);
                        GUI.color = new Color(0, 0, 0, Sequence.SwirlProgress);
                        GUI.DrawTexture(viewport, Texture2D.whiteTexture);
                    }
                }
                else
                {
                    GUI.color = new Color(0, 0, 0, Sequence.BlackOpacity);
                    GUI.DrawTexture(viewport, Texture2D.whiteTexture);
                }
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        private void OnDisable() => Cancel();
        public void Cancel()
        {
            if (capture != null) { StopCoroutine(capture); capture = null; }
            Sequence.Cancel();
            ReleaseTextures();
        }
        private void ReleaseTextures()
        {
            captured = false;
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
