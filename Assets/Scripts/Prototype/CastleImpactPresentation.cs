using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleImpactPresentation : IDisposable
    {
        public const string CastleResource = "PixelArt/Endings/CastleBeforeImpact";
        public const string MeteorResource = "PixelArt/Endings/EndingMeteor";
        public const string ShaderResource = "PixelArt/Endings/CastleImpactComposite";
        public const float FrameWidth = 1280, FrameHeight = 660;
        private readonly Texture2D castle;
        private readonly Material composite;
        private readonly RenderTexture frame;
        private float renderedAt = -1;
        public CastleImpactSequence Sequence { get; } = new CastleImpactSequence();

        public CastleImpactPresentation()
        {
            castle = Load(CastleResource);
            Texture2D meteor = Load(MeteorResource);
            Shader shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("Shader do impacto no castelo indisponível.");
            composite = new Material(shader)
                { name = "Castle impact / storm and falling meteor", hideFlags = HideFlags.HideAndDontSave };
            composite.SetTexture("_MeteorTex", meteor);
            composite.SetFloat("_CropY", FrameHeight / (FrameWidth * castle.height / castle.width));
            frame = new RenderTexture(1920, 990, 0, RenderTextureFormat.ARGB32)
                { name = "Castle impact / cinematic", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        }

        // Also used by the editor validator to inspect the actual GPU composite.
        public RenderTexture RenderFrame()
        {
            if (renderedAt == Sequence.Elapsed && frame.IsCreated()) return frame;
            float size = Sequence.MeteorSize;
            float x = Sequence.MeteorX - .73f * size, y = Sequence.MeteorY - .71f * size;
            composite.SetVector("_MeteorRect", new Vector4(x / FrameWidth, 1 - (y + size) / FrameHeight,
                size / FrameWidth, size / FrameHeight));
            composite.SetVector("_MeteorCenter", new Vector4(Sequence.MeteorX / FrameWidth,
                1 - Sequence.MeteorY / FrameHeight, 0, 0));
            composite.SetFloat("_CinematicTime", Sequence.Elapsed);
            composite.SetFloat("_MeteorOpacity", Sequence.MeteorOpacity);
            composite.SetFloat("_Heat", Sequence.Heat);
            composite.SetFloat("_CameraZoom", Sequence.CameraZoom);
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(castle, frame, composite); }
            finally { RenderTexture.active = previous; }
            renderedAt = Sequence.Elapsed;
            return frame;
        }

        public void Draw(Rect viewport)
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -150; GUI.matrix = Matrix4x4.identity;
                Fill(viewport, Color.black);
                float scale = Mathf.Min(viewport.width / 1280, viewport.height / 720);
                GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x + (viewport.width - 1280 * scale) / 2,
                    viewport.y + (viewport.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.BeginGroup(new Rect(0, 30, FrameWidth, FrameHeight));
                float shake = Sequence.ImpactShake, t = Sequence.Elapsed;
                GUI.color = Color.white;
                // Overscan prevents the brief impact tremor from uncovering the picture's edges.
                GUI.DrawTexture(new Rect(-5 + Mathf.Sin(t * 71) * shake, -5 + Mathf.Cos(t * 53) * shake,
                    FrameWidth + 10, FrameHeight + 10), RenderFrame());
                GUI.EndGroup();
                GUI.matrix = Matrix4x4.identity;
                Fill(viewport, new Color(0, 0, 0, Sequence.BlackOpacity));
                // Flash the entire viewport, including the letterbox bars.
                Fill(viewport, new Color(1, 1, 1, Sequence.WhiteOpacity));
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        private static Texture2D Load(string resource)
        {
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) throw new InvalidOperationException("Arte do impacto ausente: " + resource);
            return texture;
        }
        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }
        public void Dispose()
        { frame.Release(); DestroyOwned(frame); DestroyOwned(composite); }
        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
