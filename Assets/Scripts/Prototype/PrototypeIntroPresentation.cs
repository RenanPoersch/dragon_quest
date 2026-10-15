using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonQuest.Prototype
{
    // The artwork, lettering and animated sprite flock remain separate layers.
    public sealed class PrototypeIntroPresentation : IDisposable
    {
        public const int BirdFrameCount = 6;
        public const int BirdCount = 8;
        private readonly IntroSequence sequence = new IntroSequence();
        private readonly Texture2D background;
        private readonly Sprite[] birdFrames = new Sprite[BirdFrameCount];
        private GUIStyle titleStyle, promptStyle;
        private int finishedFrame = -1;
        private bool awaitingRelease;
        private bool hasDrawn;

        // x, y, width and speed in the same 1280x720 coordinate space as the menu.
        private static readonly Vector4[] Flock =
        {
            new Vector4(1050, 280, 50, 26), new Vector4(1130, 236, 34, 22),
            new Vector4(1190, 304, 42, 29), new Vector4(1010, 345, 29, 19),
            new Vector4(942, 296, 21, 17), new Vector4(1220, 184, 18, 15),
            new Vector4(1100, 156, 15, 13), new Vector4(870, 248, 13, 12)
        };

        public bool IsPlaying => sequence.IsPlaying;
        public bool BlocksMenuInput => IsPlaying || awaitingRelease || Time.frameCount <= finishedFrame;
        public IntroSequence Sequence => sequence;

        public PrototypeIntroPresentation()
        {
            background = Resources.Load<Texture2D>("PixelArt/Intro/Sunset");
            Texture2D sheet = Resources.Load<Texture2D>("PixelArt/Intro/BirdFlight");
            if (background == null || sheet == null)
                throw new InvalidOperationException("Arte da abertura ausente em Resources/PixelArt/Intro.");
            background.filterMode = sheet.filterMode = FilterMode.Point;
            int width = sheet.width / BirdFrameCount;
            for (int frame = 0; frame < BirdFrameCount; frame++)
            {
                birdFrames[frame] = Sprite.Create(sheet, new Rect(frame * width, 0, width, sheet.height),
                    new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                birdFrames[frame].name = "Intro bird flap " + frame;
            }
        }

        public void Update(float unscaledDeltaTime)
        {
            bool wasPlaying = IsPlaying;
            // Begin after the first rendered frame; synchronous map loading must not eat the fade-in.
            if (hasDrawn && wasPlaying) sequence.Advance(unscaledDeltaTime);
            if (wasPlaying && !IsPlaying) finishedFrame = Time.frameCount;
            if (IsPlaying && (Gamepad.current?.buttonSouth.wasPressedThisFrame == true
                || Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)) Skip();
            if (awaitingRelease && !SkipControlHeld()) awaitingRelease = false;
        }

        private void Skip()
        {
            if (sequence.TrySkip()) awaitingRelease = true;
        }

        private static bool SkipControlHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed
                    || keyboard.spaceKey.isPressed || keyboard.escapeKey.isPressed)
                || Mouse.current?.leftButton.isPressed == true
                || Touchscreen.current?.primaryTouch.press.isPressed == true
                || Gamepad.current?.buttonSouth.isPressed == true;
        }

        public void Draw()
        {
            if (!IsPlaying) return;
            if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter
                    || Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.Escape
                    || Event.current.type == EventType.MouseDown && Event.current.button == 0) Skip();
                Event.current.Use();
            }
            if (Event.current.type != EventType.Repaint) return;
            hasDrawn = true;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.depth = -100;
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                    (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
                float alpha = sequence.Opacity;
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), background, ScaleMode.ScaleAndCrop);
                DrawBirds(alpha);
                DrawTitle(alpha * sequence.TitleOpacity);
                float promptAlpha = Mathf.Clamp01((sequence.Elapsed - 3.1f) / .7f)
                    * (.68f + .18f * Mathf.Sin(sequence.Elapsed * 2)) * alpha;
                GUI.color = new Color(.1f, .1f, .17f, promptAlpha);
                GUI.Label(new Rect(0, 657, 1280, 32), "ENTER / ESPAÇO PARA CONTINUAR", promptStyle);
                GUI.color = new Color(1, .94f, .8f, promptAlpha);
                GUI.Label(new Rect(0, 655, 1280, 32), "ENTER / ESPAÇO PARA CONTINUAR", promptStyle);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.depth = previousDepth;
            }
        }

        private void DrawBirds(float alpha)
        {
            for (int bird = 0; bird < Flock.Length; bird++)
            {
                Vector4 flight = Flock[bird];
                float t = sequence.Elapsed;
                float x = Mathf.Repeat(flight.x - t * flight.w + 80, 1440) - 80;
                float y = flight.y + Mathf.Sin(t * .95f + bird * 1.7f) * (3 + bird % 3);
                int frame = (Mathf.FloorToInt(t * (7.5f + bird * .23f)) + bird * 2) % BirdFrameCount;
                Sprite sprite = birdFrames[frame];
                Rect source = sprite.rect;
                Texture2D texture = sprite.texture;
                Rect uv = new Rect(source.x / texture.width, source.y / texture.height,
                    source.width / texture.width, source.height / texture.height);
                float height = flight.z * source.height / source.width;
                GUI.color = new Color(1, 1, 1, alpha * (bird < 4 ? 1 : .82f));
                GUI.DrawTextureWithTexCoords(new Rect(Mathf.Round(x), Mathf.Round(y - height / 2),
                    flight.z, height), texture, uv, true);
            }
        }

        private void DrawTitle(float alpha)
        {
            const float spacing = 5;
            string title = IntroSequence.Title;
            float width = (title.Length - 1) * spacing;
            for (int i = 0; i < title.Length; i++) width += titleStyle.CalcSize(new GUIContent(title[i].ToString())).x;
            float x = Mathf.Round((1280 - width) / 2);
            for (int i = 0; i < title.Length; i++)
            {
                string letter = title[i].ToString();
                float advance = titleStyle.CalcSize(new GUIContent(letter)).x;
                if (i < sequence.VisibleLetters)
                {
                    GUI.color = new Color(.12f, .12f, .2f, alpha);
                    GUI.Label(new Rect(x + 3, 112, advance + 8, 135), letter, titleStyle);
                    GUI.color = new Color(1, .94f, .79f, alpha);
                    GUI.Label(new Rect(x, 109, advance + 8, 135), letter, titleStyle);
                }
                x += advance + spacing;
            }
            float reveal = Mathf.Clamp01(sequence.VisibleLetters / (float)title.Length);
            GUI.color = new Color(.13f, .12f, .21f, alpha * .85f);
            GUI.DrawTexture(new Rect((1280 - width * reveal) / 2, 237, width * reveal, 3), Texture2D.whiteTexture);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = PrototypeTypography.Style(GUI.skin.label, 112, true);
            titleStyle.padding = new RectOffset();
            titleStyle.normal.textColor = Color.white;
            titleStyle.clipping = TextClipping.Overflow;
            promptStyle = PrototypeTypography.Style(GUI.skin.label, 24);
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
        }

        public void Dispose()
        {
            foreach (Sprite frame in birdFrames) if (frame != null) UnityEngine.Object.Destroy(frame);
        }
    }
}
