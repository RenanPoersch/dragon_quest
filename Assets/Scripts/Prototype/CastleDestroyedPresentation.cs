using System;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleDestroyedPresentation : IDisposable
    {
        public const string BackgroundResource = "PixelArt/Endings/CastleAftermath";
        public const string ForegroundResource = "PixelArt/Endings/ScorchedEarth";
        private readonly string[] lines = EndingNarrative.CastleDestroyedEpilogue.Split('\n');
        private readonly Texture2D background, foreground, shade;
        private readonly PixelSpriteAtlas elder;
        private GUIStyle story, caption;
        private bool hasDrawn;
        public CastleDestroyedSequence Sequence { get; }

        public CastleDestroyedPresentation()
        {
            Sequence = new CastleDestroyedSequence(lines.Length);
            background = Load(BackgroundResource);
            foreground = Load(ForegroundResource);
            elder = new PixelSpriteAtlas("NpcRoster");
            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false)
                { name = "FinalOne / narrative shade", wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[128];
            for (int x = 0; x < colors.Length; x++)
                colors[x] = new Color(.035f, .025f, .03f, .82f * (1 - Mathf.SmoothStep(0, 1, x / 127f)));
            shade.SetPixels(colors); shade.Apply(false, true);
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
                GUI.DrawTexture(new Rect(0, 0, 1280, 660), background, ScaleMode.ScaleAndCrop);
                DrawWaterAndAsh();
                DrawForeground();
                DrawNarrative();
                GUI.EndGroup();
                GUI.matrix = Matrix4x4.identity;
                Fill(viewport, new Color(0, 0, 0, Sequence.BlackOpacity));
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        private void DrawForeground()
        {
            // Both layers share the same displacement; the old man never slides off his ledge.
            float x = Mathf.Round(Sequence.LandX);
            float height = CastleDestroyedSequence.LandWidth * foreground.height / foreground.width;
            var bounds = new Rect(x, 452, CastleDestroyedSequence.LandWidth, height);
            GUI.color = Color.white; GUI.DrawTexture(bounds, foreground);
            // Use the existing grey-bearded villager, with his feet on the near soil lip.
            elder.Draw(new Rect(x + 635, bounds.y + height * .29f - 116, 90, 116),
                PixelSpriteLibrary.NpcFrame("Loja de equipamentos"), new Color(.9f, .78f, .66f));
        }

        private void DrawWaterAndAsh()
        {
            float t = Sequence.Elapsed;
            for (int row = 0; row < 4; row++) for (int i = 0; i < 9; i++)
            {
                float x = Mathf.Repeat(i * 157 + row * 43 + t * (4 + row * 2), 1320) - 20;
                float alpha = .05f + .06f * (.5f + .5f * Mathf.Sin(t * 1.2f + i + row));
                Fill(new Rect(Mathf.Round(x / 2) * 2, 502 + row * 35, 10 + i % 4 * 6, 2), new Color(.5f, .7f, .67f, alpha));
            }
            for (int i = 0; i < 18; i++)
            {
                float x = 720 + Mathf.Repeat(i * 73 - t * (3 + i % 3), 560);
                float y = 448 - Mathf.Repeat(i * 41 + t * (9 + i % 4), 360);
                Fill(new Rect(Mathf.Round(x / 2) * 2, Mathf.Round(y / 2) * 2, 2, 2),
                    new Color(.94f, .48f, .2f, .18f + .16f * Mathf.Sin(i + t)));
            }
        }

        private void DrawNarrative()
        {
            float opacity = Mathf.Clamp01((Sequence.Elapsed - CastleDestroyedSequence.TextStart) / 1.4f)
                * (1 - Mathf.Clamp01((Sequence.Elapsed - Sequence.TextEndAt) / 1.2f));
            if (opacity <= 0) return;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(new Rect(0, 40, 600, 410), shade);
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
            foreach (Sprite sprite in elder.Sprites) DestroyOwned(sprite);
            DestroyOwned(shade);
        }
        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
