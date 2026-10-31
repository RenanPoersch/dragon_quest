using DragonQuest.Presentation.Common;
using System;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.Cinematics.Endings
{
    public sealed class HeroPossessedPresentation : IDisposable
    {
        public const string ThroneResource = "PixelArt/Endings/PossessedThrone";
        public const string SpiritResource = "PixelArt/Endings/PossessedSpiritFront";
        public const string LightningAResource = "PixelArt/Endings/PossessionLightningA";
        public const string LightningBResource = "PixelArt/Endings/PossessionLightningB";
        public const string LightningCResource = "PixelArt/Endings/PossessionLightningC";
        public const string LightningDResource = "PixelArt/Endings/PossessionLightningD";
        public const string CompositeResource = "PixelArt/Endings/PossessedThroneComposite";
        public const float SkyPadding = .18f;
        public const float SpiritOpacity = .76f;
        public const float RainOpacity = .72f;
        public static readonly Vector4 SpiritRect = new Vector4(.22f, .49f, .56f, .56f);
        private readonly string[] lines = EndingNarrative.HeroPossessedEpilogue.Split('\n');
        private readonly Texture2D throne, shade;
        private readonly Texture2D[] lightning;
        private readonly Material composite;
        private readonly RenderTexture illuminatedThrone;
        private readonly EndingArtworkRenderer cameraMotion = new EndingArtworkRenderer();
        private GUIStyle story, caption;
        private bool hasDrawn;
        private float previousLightning = -1;
        private float previousRainTime = -1;
        private int previousLightningIndex = -2;
        public HeroPossessedSequence Sequence { get; }

        public HeroPossessedPresentation(int? stormSeed = null)
        {
            Sequence = new HeroPossessedSequence(lines.Length, stormSeed ?? Guid.NewGuid().GetHashCode());
            throne = Load(ThroneResource);
            var spirit = Load(SpiritResource);
            lightning = new[] { Load(LightningAResource), Load(LightningBResource), Load(LightningCResource), Load(LightningDResource) };
            Shader shader = Resources.Load<Shader>(CompositeResource);
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Shader do final de possessão indisponível.");
            composite = new Material(shader) { name = "Possession / lightning behind the throne", hideFlags = HideFlags.HideAndDontSave };
            composite.SetTexture("_SpiritTex", spirit);
            composite.SetTexture("_LightningTex", lightning[0]);
            composite.SetFloat("_SkyPadding", SkyPadding);
            composite.SetVector("_SpiritRect", SpiritRect);
            composite.SetFloat("_SpiritOpacity", SpiritOpacity);
            composite.SetFloat("_RoomAspect", throne.width/(float)throne.height);
            composite.SetFloat("_RainOpacity", RainOpacity);
            illuminatedThrone = new RenderTexture(throne.width, Mathf.RoundToInt(throne.height * (1 + SkyPadding)), 0, RenderTextureFormat.ARGB32)
            { name = "Possession / illuminated throne", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false)
            { name = "Possession / narrative shade", wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[128];
            for (int x = 0; x < colors.Length; x++)
                colors[x] = new Color(.035f, .025f, .03f, .82f * (1 - Mathf.SmoothStep(0, 1, x / 127f)));
            shade.SetPixels(colors); shade.Apply(false, true);
        }

        public void Update(float unscaledDeltaTime)
        { if (hasDrawn) Sequence.Advance(unscaledDeltaTime); }
        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));
        public void Draw(Rect viewport)
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            hasDrawn = true;
            EnsureStyles();
            RefreshComposite();
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
                GUI.BeginGroup(new Rect(0, 0, 1280, 690));
                Rect camera = CameraBounds(Sequence, throne.width / (float)throne.height);
                camera.y+=30;
                GUI.color = Color.white;
                cameraMotion.Draw(illuminatedThrone, CompositeBounds(camera), new Rect(0, 0, 1280, 690));
                // Keep the opening letterbox; once the camera locks, lightning
                // can continue through the black sky to the actual screen top.
                if(!Sequence.CameraStopped) Fill(new Rect(0,0,1280,30),Color.black);
                GUI.BeginGroup(new Rect(0,30,1280,660));
                DrawNarrative();
                GUI.EndGroup();
                GUI.EndGroup();
                GUI.matrix = Matrix4x4.identity;
                Fill(viewport, new Color(0, 0, 0, Sequence.BlackOpacity));
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        public static Rect CameraBounds(HeroPossessedSequence sequence, float artAspect)
        {
            float width = sequence.CameraImageWidth, height = width / artAspect;
            return new Rect(640 - width / 2, 330 - sequence.CameraFocusY * height, width, height);
        }
        public static Rect CompositeBounds(Rect room)
            => new Rect(room.x, room.y - room.height * SkyPadding, room.width, room.height * (1 + SkyPadding));
        private void RefreshComposite()
        {
            var light = Sequence.LightningState;
            float flash = light.Intensity;
            // The rain keeps moving through dark intervals, even with timeScale at zero.
            float rainTime = Sequence.Elapsed;
            if (light.FlashIndex == previousLightningIndex && Mathf.Approximately(flash, previousLightning)
                && rainTime == previousRainTime && illuminatedThrone.IsCreated()) return;
            composite.SetFloat("_Lightning", flash);
            composite.SetFloat("_RainTime", rainTime);
            if (light.Frame >= 0)
            {
                composite.SetTexture("_LightningTex", lightning[light.Frame]);
                composite.SetVector("_LightningRect",new Vector4(light.X,light.Y,light.Width,light.Height));
                composite.SetFloat("_LightningAngle",light.Angle*Mathf.Deg2Rad);
                composite.SetFloat("_LightningMirror",light.Mirrored?-1:1);
            }
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(throne, illuminatedThrone, composite); }
            finally { RenderTexture.active = previous; }
            previousLightning = flash;
            previousLightningIndex = light.FlashIndex;
            previousRainTime = rainTime;
        }
        private void DrawNarrative()
        {
            float opacity = Mathf.Clamp01((Sequence.Elapsed - HeroPossessedSequence.TextStart) / 1.4f)
                * (1 - Mathf.Clamp01((Sequence.Elapsed - Sequence.TextEndAt) / 1.2f));
            if (opacity <= 0) return;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(new Rect(0, 40, 470, 540), shade);
            GUI.color = new Color(.96f, .76f, .47f, opacity);
            GUI.Label(new Rect(60, 60, 350, 30), "O NOVO HOSPEDEIRO", caption);
            GUI.BeginGroup(new Rect(60, 104, 350, HeroPossessedSequence.TextWindowHeight));
            for (int i = 0; i < lines.Length; i++)
            {
                float y = Sequence.TextOffset + i * HeroPossessedSequence.LineHeight;
                if (y < -34 || y > HeroPossessedSequence.TextWindowHeight) continue;
                float edge = Mathf.Clamp01(y / 24) * Mathf.Clamp01((HeroPossessedSequence.TextWindowHeight - y - 20) / 24);
                GUI.color = new Color(0, 0, 0, opacity * edge);
                GUI.Label(new Rect(2, y + 2, 350, 34), lines[i], story);
                GUI.color = new Color(1, .94f, .83f, opacity * edge);
                GUI.Label(new Rect(0, y, 350, 34), lines[i], story);
            }
            GUI.EndGroup();
        }
        private void EnsureStyles()
        {
            if (story != null) return;
            story = GameTypography.Style(GUI.skin.label, 28);
            caption = GameTypography.Style(GUI.skin.label, 20, true);
            foreach (GUIStyle style in new[] { story, caption })
            { style.normal.textColor = Color.white; style.padding = new RectOffset(); style.wordWrap = false; }
        }
        private static Texture2D Load(string resource)
        {
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) throw new InvalidOperationException("Arte do final de possessão ausente: " + resource);
            return texture;
        }
        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }
        public void Dispose()
        {
            cameraMotion.Dispose();
            illuminatedThrone.Release(); DestroyOwned(illuminatedThrone);
            DestroyOwned(composite); DestroyOwned(shade);
        }
        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
