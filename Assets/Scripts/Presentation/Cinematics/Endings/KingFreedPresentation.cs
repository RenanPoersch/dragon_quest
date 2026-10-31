using DragonQuest.Infrastructure;
using DragonQuest.Presentation.Common;
using System;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.Cinematics.Endings
{
    public sealed class KingFreedPresentation : IDisposable
    {
        public const string SpeechResource = "PixelArt/Endings/KingFreedSpeech";
        public const string ProsperityResource = "PixelArt/Endings/KingdomProsperity";
        public const float TextColumnWidth = 350;
        private readonly string[] speechLines = EndingNarrative.KingFreedSpeech.Split('\n');
        private readonly string[] epilogueLines = EndingNarrative.KingFreedEpilogue.Split('\n');
        private readonly Texture2D speech, prosperity;
        private readonly NarrativePanel narrative;
        private readonly EndingArtworkRenderer speechMotion = new EndingArtworkRenderer();
        private readonly EndingArtworkRenderer prosperityMotion = new EndingArtworkRenderer();
        private bool hasDrawn;
        public KingFreedSequence Sequence { get; }

        public KingFreedPresentation()
        {
            Sequence = new KingFreedSequence(speechLines.Length, epilogueLines.Length);
            speech = Load(SpeechResource); prosperity = Load(ProsperityResource);
            narrative = new NarrativePanel(new NarrativePanel.Layout
            {
                ShadeName = "King freed / narrative shade", ShadeColor = new Color(0, 0, 0, .82f),
                ShadeBounds = new Rect(0, 40, 520, 540),
                Title = new NarrativePanel.Heading
                {
                    Bounds = new Rect(60, 60, TextColumnWidth, 30), FontSize = 20, Emphasized = true,
                    Color = new Color(.96f, .8f, .5f, 1), ShadowOffset = new Vector2(2, 2)
                },
                TextWindow = new Rect(60, 104, TextColumnWidth, KingFreedSequence.TextWindowHeight),
                TextSize = 28, TextColor = new Color(1, .96f, .86f, 1),
                LineHeight = KingFreedSequence.LineHeight, TextShadowOffset = new Vector2(2, 2),
                CullAbove = 34, BottomFadeInset = 20
            });
        }
        public void Update(float unscaledDeltaTime)
        { if (hasDrawn) Sequence.Advance(unscaledDeltaTime); }
        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));
        public void Draw(Rect viewport)
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            hasDrawn = true; narrative.EnsureStyles();
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
                speechMotion.Draw(speech, ArtBounds(Sequence.SpeechProgress, speech.width / (float)speech.height, false),
                    new Rect(0, 0, 1280, 660));
                if (Sequence.ProsperityOpacity > 0)
                {
                    GUI.color = new Color(1, 1, 1, Sequence.ProsperityOpacity);
                    prosperityMotion.Draw(prosperity, ArtBounds(Sequence.ProsperityProgress, prosperity.width / (float)prosperity.height, true),
                        new Rect(0, 0, 1280, 660));
                }
                narrative.Draw(speechLines, "A VOZ DO REI", Sequence.SpeechTextOffset, Sequence.SpeechTextOpacity);
                narrative.Draw(epilogueLines, "UM REINO EM PAZ", Sequence.EpilogueTextOffset, Sequence.EpilogueTextOpacity);
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
        private static Texture2D Load(string resource) => GameAssets.Required<Texture2D>(resource);
        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }
        public void Dispose()
        {
            speechMotion.Dispose(); prosperityMotion.Dispose();
            narrative.Dispose();
        }
    }
}
