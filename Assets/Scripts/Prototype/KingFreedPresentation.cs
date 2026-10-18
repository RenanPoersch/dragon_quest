using System;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class KingFreedPresentation : IDisposable
    {
        public const string SpeechResource = "PixelArt/Endings/KingFreedSpeech";
        public const string ProsperityResource = "PixelArt/Endings/KingdomProsperity";
        public const float TextColumnWidth = 350;
        private readonly string[] speechLines = EndingNarrative.KingFreedSpeech.Split('\n');
        private readonly string[] epilogueLines = EndingNarrative.KingFreedEpilogue.Split('\n');
        private readonly Texture2D speech, prosperity, shade;
        private GUIStyle story, caption;
        private bool hasDrawn;
        public KingFreedSequence Sequence { get; }

        public KingFreedPresentation()
        {
            Sequence = new KingFreedSequence(speechLines.Length, epilogueLines.Length);
            speech = Load(SpeechResource); prosperity = Load(ProsperityResource);
            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false)
            { name = "King freed / narrative shade", wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[128];
            for (int x = 0; x < colors.Length; x++)
                colors[x] = new Color(.035f, .04f, .03f, .92f * (1 - Mathf.SmoothStep(0, 1, x / 127f)));
            shade.SetPixels(colors); shade.Apply(false, true);
        }
        public void Update(float unscaledDeltaTime)
        { if (hasDrawn) Sequence.Advance(unscaledDeltaTime); }
        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));
        public void Draw(Rect viewport)
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            hasDrawn = true; EnsureStyles();
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
                GUI.BeginGroup(new Rect(0, 30, 1280, 660));
                GUI.color = Color.white;
                GUI.DrawTexture(ArtBounds(Sequence.SpeechProgress, speech.width / (float)speech.height, false), speech);
                if (Sequence.ProsperityOpacity > 0)
                {
                    GUI.color = new Color(1, 1, 1, Sequence.ProsperityOpacity);
                    GUI.DrawTexture(ArtBounds(Sequence.ProsperityProgress, prosperity.width / (float)prosperity.height, true), prosperity);
                }
                DrawNarrative(speechLines, "A VOZ DO REI", Sequence.SpeechTextOffset, Sequence.SpeechTextOpacity);
                DrawNarrative(epilogueLines, "UM REINO EM PAZ", Sequence.EpilogueTextOffset, Sequence.EpilogueTextOpacity);
                GUI.EndGroup();
                GUI.matrix = Matrix4x4.identity;
                Fill(viewport, new Color(0, 0, 0, Sequence.BlackOpacity));
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        public static Rect ArtBounds(float progress, float aspect, bool isProsperity)
        {
            float t = Mathf.Clamp01(progress);
            float width = Mathf.Max(1320 + 24 * t, 660 * aspect), height = width / aspect;
            // Anchor the speech at the top so the crown stays visible throughout the slow push.
            return new Rect(-20 - 16 * t, isProsperity ? (660 - height) / 2 : 0, width, height);
        }
        private void DrawNarrative(string[] lines, string heading, float offset, float opacity)
        {
            if (opacity <= 0) return;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(new Rect(0, 40, 520, 540), shade);
            GUI.color = new Color(.96f, .8f, .5f, opacity);
            GUI.Label(new Rect(60, 60, TextColumnWidth, 30), heading, caption);
            GUI.BeginGroup(new Rect(60, 104, TextColumnWidth, KingFreedSequence.TextWindowHeight));
            for (int i = 0; i < lines.Length; i++)
            {
                float y = offset + i * KingFreedSequence.LineHeight;
                if (y < -34 || y > KingFreedSequence.TextWindowHeight) continue;
                float edge = Mathf.Clamp01(y / 24) * Mathf.Clamp01((KingFreedSequence.TextWindowHeight - y - 20) / 24);
                GUI.color = new Color(0, 0, 0, opacity * edge);
                GUI.Label(new Rect(2, y + 2, TextColumnWidth, 34), lines[i], story);
                GUI.color = new Color(1, .96f, .86f, opacity * edge);
                GUI.Label(new Rect(0, y, TextColumnWidth, 34), lines[i], story);
            }
            GUI.EndGroup();
        }
        private void EnsureStyles()
        {
            if (story != null) return;
            story = PrototypeTypography.Style(GUI.skin.label, 28);
            caption = PrototypeTypography.Style(GUI.skin.label, 20, true);
            foreach (GUIStyle style in new[] { story, caption })
            { style.normal.textColor = Color.white; style.padding = new RectOffset(); style.wordWrap = false; }
        }
        private static Texture2D Load(string resource)
        {
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) throw new InvalidOperationException("Arte do final bom ausente: " + resource);
            return texture;
        }
        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }
        public void Dispose()
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(shade);
            else UnityEngine.Object.DestroyImmediate(shade);
        }
    }
}
