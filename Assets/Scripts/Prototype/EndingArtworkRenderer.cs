using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Move the crop in texture space. IMGUI only sees a stationary screen rectangle,
    // so even a pan slower than one screen pixel per frame keeps moving.
    public sealed class EndingArtworkRenderer : IDisposable
    {
        public const string ShaderResource = "PixelArt/Endings/EndingArtworkMotion";
        private readonly Material material;
        private RenderTexture frame;

        public EndingArtworkRenderer()
        {
            Shader shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("Shader de movimento dos finais indisponível.");
            material = new Material(shader)
                { name = "Ending / continuous artwork crop", hideFlags = HideFlags.HideAndDontSave };
        }

        public void Draw(Texture artwork, Rect artworkBounds, Rect viewport)
        {
            float scale = GUI.matrix.MultiplyVector(Vector3.right).magnitude;
            GUI.DrawTexture(viewport, RenderFrame(artwork, artworkBounds, viewport, scale));
        }

        // Rendering is also available to GPU checks without an OnGUI event.
        public RenderTexture RenderFrame(Texture artwork, Rect artworkBounds, Rect viewport, float scale = 1)
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(viewport.width * scale));
            int height = Mathf.Max(1, Mathf.CeilToInt(viewport.height * scale));
            if (frame == null || frame.width != width || frame.height != height)
            {
                ReleaseFrame();
                frame = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Ending / smooth artwork", filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                };
            }
            material.SetVector("_ArtRect", new Vector4((artworkBounds.x - viewport.x) / viewport.width,
                (artworkBounds.y - viewport.y) / viewport.height,
                artworkBounds.width / viewport.width, artworkBounds.height / viewport.height));
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(artwork, frame, material); }
            finally { RenderTexture.active = previous; }
            return frame;
        }

        private void ReleaseFrame()
        {
            if (frame == null) return;
            frame.Release(); DestroyOwned(frame); frame = null;
        }
        public void Dispose() { ReleaseFrame(); DestroyOwned(material); }
        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
