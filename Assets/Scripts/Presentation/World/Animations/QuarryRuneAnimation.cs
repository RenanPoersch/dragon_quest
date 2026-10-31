using System;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.World.Animations
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class QuarryRuneAnimation : MonoBehaviour
    {
        public const int FrameCount = 8, Columns = 4, Rows = 2;
        private const float FaceWidth = .37f, FaceHeight = .46f;
        public const float FloatingScale = 4f, RevealDuration = 2f;
        private readonly Sprite[] frames = new Sprite[FrameCount];
        private SpriteRenderer view;
        private SpriteRenderer engraving;
        private float elapsed;
        private AncientSecrets completion;
        private Vector3 facePosition, faceScale;
        private float stoneTop, completionElapsed;
        private bool completionBound;
        public bool IsFloating { get; private set; }
        public MonolithSymbol Symbol { get; private set; }
        public int CurrentFrame { get; private set; }
        public Sprite DormantSprite => frames[0];
        public Texture2D Source { get; private set; }

        public static int Rate(MonolithSymbol symbol)
            => symbol == MonolithSymbol.Moon ? 4 : symbol == MonolithSymbol.Star ? 5
                : symbol == MonolithSymbol.Sun ? 6 : throw new ArgumentOutOfRangeException(nameof(symbol));
        public static string Resource(MonolithSymbol symbol)
            => symbol == MonolithSymbol.Moon ? "QuarryMoonRune" : symbol == MonolithSymbol.Star ? "QuarryStarRune"
                : symbol == MonolithSymbol.Sun ? "QuarrySunRune" : throw new ArgumentOutOfRangeException(nameof(symbol));
        public static int FrameAt(float seconds, MonolithSymbol symbol)
            => float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0
                : (int)(Math.Floor(Math.Max(0,(double)seconds)*Rate(symbol))%FrameCount);

        public void Initialize(MonolithSymbol symbol)
        {
            Symbol = symbol; view = GetComponent<SpriteRenderer>();
            Source = Resources.Load<Texture2D>("PixelArt/" + Resource(symbol));
            if (Source == null || !Source.isReadable || Source.width%Columns != 0 || Source.height%Rows != 0)
                throw new InvalidOperationException("Atlas da runa deve ser legivel, com quatro colunas e duas linhas: " + symbol);
            Color32[] pixels = Source.GetPixels32();
            int width = Source.width/Columns, height = Source.height/Rows;
            var pivots = new Vector2[FrameCount];
            float halfWidth = 1, halfHeight = 1;
            Vector2 registrationOffset = default;
            for (int frame = 0; frame < FrameCount; frame++)
            {
                int left = frame%Columns*width, bottom = (Rows-1-frame/Columns)*height;
                Rect body = LargestBody(pixels,Source.width,left,bottom,width,height,out Vector2 center);
                if (body.width < 2 || body.height < 2) throw new InvalidOperationException("Quadro vazio da runa: " + symbol + " / " + frame);
                if (frame == 0) registrationOffset = body.center-center;
                center += registrationOffset;
                pivots[frame] = new Vector2((center.x-left)/width,(center.y-bottom)/height);
                for (int y = bottom; y < bottom+height; y++)
                    for (int x = left; x < left+width; x++)
                        if (pixels[y*Source.width+x].a >= 128)
                        {
                            halfWidth = Math.Max(halfWidth,Math.Abs(x+.5f-center.x));
                            halfHeight = Math.Max(halfHeight,Math.Abs(y+.5f-center.y));
                        }
            }
            // One pixel scale for the whole loop; register the main body instead of moving sparkles.
            float pixelsPerUnit = Math.Max(halfWidth*2/FaceWidth,halfHeight*2/FaceHeight);
            for (int frame = 0; frame < FrameCount; frame++)
            {
                var rect = new Rect(frame%Columns*width,(Rows-1-frame/Columns)*height,width,height);
                frames[frame] = Sprite.Create(Source,rect,pivots[frame],pixelsPerUnit,0,SpriteMeshType.FullRect);
                frames[frame].name = "Runa desenhada / " + symbol + " / " + frame;
            }
            ShowAt(0);
        }

        public void ShowAt(float seconds)
        {
            if (view == null || frames[0] == null) return;
            CurrentFrame = FrameAt(seconds,Symbol);
            if (view.sprite != frames[CurrentFrame]) view.sprite = frames[CurrentFrame];
        }
        public void BindEngraving(SpriteRenderer resting)
        { engraving = resting; RefreshEngraving(); }

        public void BindCompletion(AncientSecrets progress, float stoneHeight)
        {
            if (completion != null) completion.Changed -= CompletionChanged;
            completion = progress; stoneTop = stoneHeight;
            facePosition = transform.localPosition; faceScale = transform.localScale;
            completionBound = true;
            if (completion != null) completion.Changed += CompletionChanged;
            SynchronizeCompletion(true);
        }

        private void CompletionChanged() => SynchronizeCompletion(false);

        private void SynchronizeCompletion(bool immediate)
        {
            if (!completionBound) return;
            bool solved = completion != null && completion.MonolithsSolved;
            if (solved != IsFloating)
            {
                IsFloating = solved;
                completionElapsed = solved && immediate ? RevealDuration : 0;
            }
            ApplyCompletion();
        }

        // The editor can sample the reveal and hover without advancing the puzzle or sprite clock.
        public void ShowCompletionAt(float seconds)
        {
            completionElapsed = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0 : Math.Max(0,seconds);
            ApplyCompletion();
        }

        private void ApplyCompletion()
        {
            if (completionBound)
            {
                float t = IsFloating ? Mathf.Clamp01(completionElapsed/RevealDuration) : 0;
                float reveal = 1-Mathf.Pow(1-t,3);
                float hover = Mathf.Sin(completionElapsed*(2*Mathf.PI/5.6f)+(int)Symbol*1.2f)*.10f;
                // FaceHeight bounds all opaque pixels, leaving daylight between every frame and the stone.
                var floating = new Vector3(0,stoneTop+.30f+FaceHeight*FloatingScale/2+hover,facePosition.z);
                transform.localPosition = Vector3.Lerp(facePosition,floating,reveal);
                transform.localScale = faceScale*Mathf.Lerp(1,FloatingScale,reveal);
            }
            RefreshEngraving();
        }

        private void RefreshEngraving()
        {
            if (engraving != null)
                engraving.enabled = !isActiveAndEnabled || (IsFloating && completionElapsed >= RevealDuration);
        }

        private void OnEnable()
        { elapsed = 0; ShowAt(0); SynchronizeCompletion(true); RefreshEngraving(); }
        private void OnDisable()
        {
            elapsed = 0; ShowAt(0); SynchronizeCompletion(true);
            if (IsFloating) completionElapsed = Math.Max(completionElapsed,RevealDuration);
            ApplyCompletion();
        }
        private void Update()
        {
            SynchronizeCompletion(true);
            elapsed = Mathf.Repeat(elapsed+Time.deltaTime,(float)FrameCount/Rate(Symbol));
            ShowAt(elapsed);
            if (IsFloating) { completionElapsed += Time.deltaTime; ApplyCompletion(); }
        }

        private static Rect LargestBody(Color32[] pixels, int stride, int left, int bottom, int width, int height, out Vector2 center)
        {
            var visited = new bool[width*height];
            var queue = new int[visited.Length];
            Rect largest = default; int largestCount = 0;
            center = default;
            for (int start = 0; start < visited.Length; start++)
            {
                int sx = start%width, sy = start/width;
                if (visited[start] || pixels[(bottom+sy)*stride+left+sx].a < 128) continue;
                int head = 0, tail = 1, minX = sx, maxX = sx, minY = sy, maxY = sy;
                double sumX = 0, sumY = 0;
                queue[0] = start; visited[start] = true;
                while (head < tail)
                {
                    int cell = queue[head++], x = cell%width, y = cell/width;
                    sumX += x+.5; sumY += y+.5;
                    minX=Math.Min(minX,x); maxX=Math.Max(maxX,x); minY=Math.Min(minY,y); maxY=Math.Max(maxY,y);
                    if (x > 0) Add(cell-1);
                    if (x+1 < width) Add(cell+1);
                    if (y > 0) Add(cell-width);
                    if (y+1 < height) Add(cell+width);
                }
                if (tail > largestCount)
                {
                    largestCount = tail; largest = new Rect(left+minX,bottom+minY,maxX-minX+1,maxY-minY+1);
                    center = new Vector2(left+(float)(sumX/tail),bottom+(float)(sumY/tail));
                }

                void Add(int cell)
                {
                    if (visited[cell] || pixels[(bottom+cell/width)*stride+left+cell%width].a < 128) return;
                    visited[cell] = true; queue[tail++] = cell;
                }
            }
            return largest;
        }

        private void OnDestroy()
        {
            if (completion != null) completion.Changed -= CompletionChanged;
            foreach (Sprite frame in frames)
                if (frame != null) { if (Application.isPlaying) Destroy(frame); else DestroyImmediate(frame); }
        }
    }
}
