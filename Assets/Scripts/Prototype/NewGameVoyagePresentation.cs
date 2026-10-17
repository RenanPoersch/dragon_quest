using System;
using UnityEngine;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    public sealed class NewGameVoyagePresentation : IDisposable
    {
        public readonly NewGameVoyageSequence Sequence = new NewGameVoyageSequence();
        private readonly Texture2D panorama, shade;
        private readonly VoyageShipLayers ship;
        private readonly PixelSpriteAtlas[] heroes = new PixelSpriteAtlas[3];
        private readonly float[] heroScales = new float[3];
        private GUIStyle heading, chapter, story;
        private bool hasDrawn;
        public bool IsPlaying => Sequence.IsPlaying;

        public NewGameVoyagePresentation()
        {
            panorama = PixelTexture("PixelArt/RuinsPaintedPanorama", 193);
            Texture2D shipSource = PixelTexture("PixelArt/Intro/VoyageCarrack", 256, true);
            ship = new VoyageShipLayers(shipSource);
            DestroyOwned(shipSource);
            heroes[0] = new PixelSpriteAtlas("ArenWalk");
            heroes[1] = new PixelSpriteAtlas("LiaWalk");
            heroes[2] = new PixelSpriteAtlas("BramWalk");
            for (int hero = 0; hero < heroes.Length; hero++)
            {
                float tallest = 1;
                for (int row = 0; row < 3; row++)
                    tallest = Mathf.Max(tallest, heroes[hero].Sprites[row * 4 + (int)PixelDirection.Left].rect.height);
                heroScales[hero] = NewGameVoyageSequence.HeroSize / tallest;
            }
            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false)
                { name = "Voyage / narrative shade", wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[shade.width];
            for (int x = 0; x < colors.Length; x++)
                // Same color, strength and smooth horizontal falloff as the ending.
                colors[x] = new Color(.035f, .025f, .03f, .82f * (1 - Mathf.SmoothStep(0, 1, x / 127f)));
            shade.SetPixels(colors); shade.Apply(false, true);
        }

        public void Update(float unscaledDeltaTime)
        {
            // Synchronous asset loading cannot consume the arrival or the opening fade.
            if (hasDrawn) Sequence.Advance(unscaledDeltaTime);
        }

        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));

        // The same renderer is used by the game and the Editor's voyage preview.
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
                GUI.matrix = Matrix4x4.identity; GUI.depth = -110;
                if (Sequence.ShowsScene)
                {
                    Fill(viewport, Color.black);
                    float scale = Mathf.Min(viewport.width / 1280f, viewport.height / 720f);
                    var frame = new Rect(viewport.x + (viewport.width - 1280 * scale) / 2,
                        viewport.y + (viewport.height - 720 * scale) / 2, 1280 * scale, 720 * scale);
                    GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y, 0),
                        Quaternion.identity, new Vector3(scale, scale, 1));
                    GUI.BeginGroup(new Rect(0, 0, 1280, 720));
                    DrawSea();
                    DrawShipAndParty();
                    DrawNarrative();
                    GUI.EndGroup();
                    // Rotation and fractional scaling can leak a hull pixel past an IMGUI clip.
                    // Mask the complete letterbox last, outside that clip, in screen coordinates.
                    GUI.matrix = Matrix4x4.identity;
                    MaskFrame(viewport, new Rect(frame.x, frame.y + 24 * scale, frame.width, 672 * scale));
                }
                GUI.matrix = Matrix4x4.identity;
                Fill(viewport, new Color(0, 0, 0, Sequence.BlackOpacity));
            }
            finally
            {
                GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth;
            }
        }

        private void DrawSea()
        {
            float t = Sequence.Elapsed;
            // Preserve the ruins panorama's proportions; its very slow drift adds depth.
            float width = panorama.width * 720f / panorama.height;
            // Use the panorama's spare width rather than wrapping unmatched mountain edges.
            float left = Mathf.Min(0, 1280 - width + Sequence.BackgroundOffset);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(left, 0, width, 720), panorama);
            // Small irregular wave glints at three speeds, all on a crisp pixel grid.
            for (int row = 0; row < 6; row++)
                for (int i = 0; i < 12; i++)
                {
                    float x = Mathf.Repeat(i * 117 + row * 49 + Sequence.SailingDistance * (.75f + row * .3f), 1380) - 50;
                    float y = 502 + row * 31 + Mathf.Sin(i * 1.7f + t * 1.2f) * 2;
                    float alpha = .13f + .12f * Mathf.Sin(t * 2 + i * 2.3f + row);
                    Fill(new Rect(Mathf.Round(x / 2) * 2, Mathf.Round(y / 2) * 2, 8 + i % 4 * 4, 2),
                        new Color(.65f, .86f, .9f, alpha));
                }
        }

        private void DrawShipAndParty()
        {
            float t = Sequence.Elapsed;
            float height = NewGameVoyageSequence.ShipWidth * ship.Height / ship.Width;
            // Close view of the bow and foredeck; stern, mast tops and keel continue beyond the frame.
            var bounds = new Rect(Sequence.ShipX, -245 + Mathf.Sin(t * 1.25f) * 3,
                NewGameVoyageSequence.ShipWidth, height);
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Sin(t * .8f) * .22f, new Vector2(bounds.x + bounds.width * .34f, 550));
            GUI.color = Color.white;
            GUI.DrawTexture(bounds, ship.BehindParty);
            for (int i = 2; i >= 0; i--)
            {
                float progress = Sequence.HeroWalkProgress(i);
                float localX = Sequence.HeroLocalX(i);
                bool walking = progress > 0 && progress < 1;
                int frame = PixelSpriteLayout.WalkFrame(PixelDirection.Left, progress * 6.4f, walking);
                float foot = bounds.y + bounds.height * VoyageShipLayers.DeckHeightAt(localX);
                DrawGroundedHero(i, frame, new Vector2(bounds.x + localX * bounds.width, foot));
            }
            // The curved near side covers their lower legs instead of a flat rectangular strip.
            GUI.color = Color.white;
            GUI.DrawTexture(bounds, ship.InFrontOfParty);
            GUI.matrix = previous;
            float speed = Sequence.IsDeparting ? Mathf.Clamp01((t - NewGameVoyageSequence.DepartureAt) / 2) : 1 - Mathf.Clamp01(t / 7);
            for (int i = 0; i < 5; i++)
                Fill(new Rect(Mathf.Round(bounds.xMax - 78 + i * 23 + Mathf.Sin(t * 3 + i) * 3),
                    Mathf.Round(bounds.yMax - 15 + i * 3), 24 - i * 3, 2), new Color(.8f, .94f, 1, .32f * speed));
        }

        private void DrawGroundedHero(int hero, int frame, Vector2 foot)
        {
            var atlas = heroes[hero];
            Sprite pose = atlas.Sprites[frame];
            Rect source = pose.rect;
            float scale = heroScales[hero];
            // Preserve the pose's foot pivot and one scale throughout the walk cycle.
            // Resizing and centering each crop separately makes the party slide above the deck.
            var destination = new Rect(foot.x - pose.pivot.x * scale,
                foot.y - (source.height - pose.pivot.y) * scale, source.width * scale, source.height * scale);
            var uv = new Rect(source.x / atlas.Texture.width, source.y / atlas.Texture.height,
                source.width / atlas.Texture.width, source.height / atlas.Texture.height);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, atlas.Texture, uv, true);
        }

        private void DrawNarrative()
        {
            float opacity = Sequence.NarrativeOpacity;
            if (opacity <= 0) return;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(new Rect(0, 64, 600, 540), shade);
            GUI.color = new Color(.88f, .74f, .45f, opacity);
            GUI.Label(new Rect(72, 81, 400, 26), "PRÓLOGO", chapter);
            GUI.color = new Color(1, .94f, .78f, opacity);
            GUI.Label(new Rect(72, 113, 420, 48), "A TRAVESSIA", heading);
            Fill(new Rect(72, 172, 56, 2), new Color(.86f, .71f, .43f, .8f * opacity));
            GUI.BeginGroup(new Rect(72, 198, 404, NewGameVoyageSequence.TextWindowHeight));
            for (int i = 0; i < NewGameVoyageSequence.LineCount; i++)
            {
                float y = Sequence.TextOffset + i * NewGameVoyageSequence.LineHeight;
                float edgeAlpha = Mathf.Clamp01(y / 24) * Mathf.Clamp01((NewGameVoyageSequence.TextWindowHeight - y - 16) / 24);
                if (y < -32 || y > NewGameVoyageSequence.TextWindowHeight) continue;
                GUI.color = new Color(0, 0, 0, edgeAlpha * opacity);
                GUI.Label(new Rect(1, y + 2, 404, 34), NewGameVoyageSequence.Line(i), story);
                GUI.color = new Color(.98f, .94f, .83f, edgeAlpha * opacity);
                GUI.Label(new Rect(0, y, 404, 34), NewGameVoyageSequence.Line(i), story);
            }
            GUI.EndGroup();
        }

        private void EnsureStyles()
        {
            if (story != null) return;
            story = PrototypeTypography.Style(GUI.skin.label, 26);
            heading = PrototypeTypography.Style(GUI.skin.label, 38, true);
            chapter = PrototypeTypography.Style(GUI.skin.label, 18);
            foreach (GUIStyle style in new[] { story, heading, chapter })
            { style.normal.textColor = Color.white; style.padding = new RectOffset(); style.wordWrap = false; }
        }

        private static void Fill(Rect area, Color color)
        { GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); }

        private static void MaskFrame(Rect viewport, Rect picture)
        {
            // Round inward so even a fractional edge cannot leave an uncovered pixel row.
            float left = Mathf.Ceil(picture.xMin), right = Mathf.Floor(picture.xMax);
            float top = Mathf.Ceil(picture.yMin), bottom = Mathf.Floor(picture.yMax);
            Fill(new Rect(viewport.xMin, viewport.yMin, viewport.width, top - viewport.yMin), Color.black);
            Fill(new Rect(viewport.xMin, bottom, viewport.width, viewport.yMax - bottom), Color.black);
            if (left > viewport.xMin)
                Fill(new Rect(viewport.xMin, top, left - viewport.xMin, bottom - top), Color.black);
            if (right < viewport.xMax)
                Fill(new Rect(right, top, viewport.xMax - right, bottom - top), Color.black);
        }

        private static Texture2D PixelTexture(string resource, int height, bool readable = false)
        {
            Texture2D source = Resources.Load<Texture2D>(resource);
            if (source == null) throw new InvalidOperationException("Arte da travessia ausente: " + resource);
            int width = Mathf.RoundToInt(height * source.width / (float)source.height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = resource + " / pixel grid", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color32[] original = source.GetPixels32(), pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                pixels[y * width + x] = original[Mathf.Min(source.height - 1, (int)((y + .5f) * source.height / height)) * source.width
                    + Mathf.Min(source.width - 1, (int)((x + .5f) * source.width / width))];
            texture.SetPixels32(pixels); texture.Apply(false, !readable); return texture;
        }

        public void Dispose()
        {
            foreach (var hero in heroes)
                if (hero != null) foreach (var frame in hero.Sprites) DestroyOwned(frame);
            DestroyOwned(panorama); ship.Dispose(); DestroyOwned(shade);
        }

        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
