using System;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleDestroyedPresentation : IDisposable
    {
        public const string BackgroundResource = "PixelArt/Endings/CastleAftermath";
        public const string ForegroundResource = "PixelArt/Endings/SeatedElderHill";
        private readonly string[] lines = EndingNarrative.CastleDestroyedEpilogue.Split('\n');
        private readonly Texture2D background, foreground;
        private readonly EndingArtworkRenderer backgroundMotion = new EndingArtworkRenderer();
        private readonly EndingArtworkRenderer foregroundMotion = new EndingArtworkRenderer();
        private GUIStyle story, caption;
        private bool hasDrawn;
        private readonly bool enterFromWhite;
        public CastleDestroyedSequence Sequence { get; }

        public CastleDestroyedPresentation(bool enterFromWhite = false)
        {
            this.enterFromWhite = enterFromWhite;
            Sequence = new CastleDestroyedSequence(lines.Length);
            background = Load(BackgroundResource);
            foreground = Load(ForegroundResource);
        }

        public void Update(float unscaledDeltaTime)
        {
            // Loading the art must not consume the foreground's entrance before its first frame.
            if (hasDrawn) Sequence.Advance(unscaledDeltaTime);
        }

        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));
        public void Draw(Rect viewport)
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            hasDrawn = true;
            EnsureStyles();
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
                float panoramaWidth = 1280 + CastleDestroyedSequence.BackgroundDrift + 24;
                float panoramaHeight = panoramaWidth * background.height / background.width;
                backgroundMotion.Draw(background, new Rect(-12 + Sequence.BackgroundX, (660 - panoramaHeight) / 2,
                    panoramaWidth, panoramaHeight), new Rect(0, 0, 1280, 660));
                DrawWaterAndAsh();
                DrawForeground();
                DrawNarrative();
                GUI.EndGroup();
                GUI.matrix = Matrix4x4.identity;
                Color fade = enterFromWhite && Sequence.Elapsed < CastleDestroyedSequence.FadeInDuration
                    ? Color.white : Color.black;
                fade.a = Sequence.BlackOpacity;
                Fill(viewport, fade);
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        private void DrawForeground()
        {
            float x = Sequence.LandX;
            float height = CastleDestroyedSequence.LandWidth * foreground.height / foreground.width;
            // One painted layer keeps the seated man's palm, hips and extended boot on the curved soil.
            // Its lower hillside continues below the frame, while the entire pose remains visible.
            var bounds = new Rect(x, 180, CastleDestroyedSequence.LandWidth, height);
            GUI.color = Color.white;
            foregroundMotion.Draw(foreground, bounds, new Rect(0, 0, 1280, 660));
        }

        private void DrawWaterAndAsh()
        {
            float t = Sequence.Elapsed;
            for (int row = 0; row < 4; row++) for (int i = 0; i < 9; i++)
            {
                float x = Mathf.Repeat(i * 157 + row * 43 + t * (4 + row * 2), 1320) - 20;
                float alpha = .05f + .06f * (.5f + .5f * Mathf.Sin(t * 1.2f + i + row));
                Fill(new Rect(Mathf.Round(x / 2) * 2 + Sequence.BackgroundX, 502 + row * 35,
                    10 + i % 4 * 6, 2), new Color(.5f, .7f, .67f, alpha));
            }
            for (int i = 0; i < 18; i++)
            {
                float x = 720 + Mathf.Repeat(i * 73 - t * (3 + i % 3), 560);
                float y = 448 - Mathf.Repeat(i * 41 + t * (9 + i % 4), 360);
                Fill(new Rect(Mathf.Round(x / 2) * 2 + Sequence.BackgroundX, Mathf.Round(y / 2) * 2, 2, 2),
                    new Color(.94f, .48f, .2f, .18f + .16f * Mathf.Sin(i + t)));
            }
        }

        private void DrawNarrative()
        {
            float opacity = Mathf.Clamp01((Sequence.Elapsed - CastleDestroyedSequence.TextStart) / 1.4f)
                * (1 - Mathf.Clamp01((Sequence.Elapsed - Sequence.TextEndAt) / 1.2f));
            if (opacity <= 0) return;
            GUI.color = new Color(0, 0, 0, opacity);
            GUI.Label(new Rect(72, 62, 450, 30), "CINZAS DO CASTELO", caption);
            GUI.color = new Color(.96f, .76f, .47f, opacity);
            GUI.Label(new Rect(70, 60, 450, 30), "CINZAS DO CASTELO", caption);
            GUI.BeginGroup(new Rect(70, 104, 470, CastleDestroyedSequence.TextWindowHeight));
            for (int i = 0; i < lines.Length; i++)
            {
                float y = Sequence.TextOffset + i * CastleDestroyedSequence.LineHeight;
                if (y < -34 || y > CastleDestroyedSequence.TextWindowHeight) continue;
                float edge = Mathf.Clamp01(y / 24) * Mathf.Clamp01((CastleDestroyedSequence.TextWindowHeight - y - 20) / 24);
                GUI.color = new Color(0, 0, 0, opacity * edge);
                GUI.Label(new Rect(2, y + 2, 470, 34), lines[i], story);
                GUI.color = new Color(1, .94f, .83f, opacity * edge);
                GUI.Label(new Rect(0, y, 470, 34), lines[i], story);
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
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null) throw new InvalidOperationException("Arte do FinalOne ausente: " + resource);
            return texture;
        }
        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }
        public void Dispose()
        {
            backgroundMotion.Dispose(); foregroundMotion.Dispose();
        }
    }
}
