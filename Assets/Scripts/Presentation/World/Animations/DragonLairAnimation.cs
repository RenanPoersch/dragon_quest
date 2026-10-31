using System;
using System.Collections.Generic;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.World.Animations
{
    public sealed class DragonLairAnimation : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly DragonAwakening awakening = new DragonAwakening();
        private SpriteRenderer body;
        private Sprite[] poses;
        public DragonRestState State => awakening.State;
        public int CurrentFrame => awakening.Frame;

        public void Initialize(Material material)
        {
            Texture2D source = Resources.Load<Texture2D>("PixelArt/DragonLairStates");
            if (source == null) throw new InvalidOperationException("Poses do dragao no covil ausentes.");
            Color32[] pixels = source.GetPixels32();
            Pose[] regions = SplitPoses(source.width, source.height, pixels, out int[] labels);
            int width = 1, bottom = source.height, top = 0;
            foreach (Pose region in regions)
            {
                width = Math.Max(width, region.Right - region.Left + 1);
                bottom = Math.Min(bottom, region.Bottom); top = Math.Max(top, region.Top);
            }
            int height = top - bottom + 1;
            // All poses use the same scale and anchor, keeping the body on the dais as it rises.
            poses = new Sprite[3];
            const int logicalHeight = 144;
            int logicalWidth = Mathf.RoundToInt(logicalHeight * (float)width / height);
            for (int frame = 0; frame < poses.Length; frame++)
            {
                var sampled = new Color32[logicalWidth * logicalHeight];
                for (int y = 0; y < logicalHeight; y++)
                    for (int x = 0; x < logicalWidth; x++)
                    {
                        Pose region = regions[frame];
                        int sx = (region.Left + region.Right + 1 - width) / 2
                            + Math.Min(width - 1, (int)((x + .5f) * width / logicalWidth));
                        int sy = bottom + Math.Min(height - 1, (int)((y + .5f) * height / logicalHeight));
                        int index = sy * source.width + sx;
                        if (sx >= 0 && sx < source.width && labels[index] == region.Label)
                            sampled[y * logicalWidth + x] = pixels[index];
                    }
                var texture = new Texture2D(logicalWidth, logicalHeight, TextureFormat.RGBA32, false)
                    { name = "Dragao / pose " + frame, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels32(sampled); texture.Apply(false, true); owned.Add(texture);
                poses[frame] = Sprite.Create(texture, new Rect(0, 0, logicalWidth, logicalHeight),
                    new Vector2(.5f, (float)(regions[frame].Bottom - bottom) / height),
                    logicalHeight / 5.4f, 0, SpriteMeshType.FullRect);
                owned.Add(poses[frame]);
            }
            body = gameObject.AddComponent<SpriteRenderer>(); body.sharedMaterial = material;
            ResetRest();
        }

        private sealed class Pose
        {
            public int Label, Left, Right, Bottom, Top, Count;
        }

        // Generated silhouettes can cross a nominal cell edge by a few pixels.
        // Read the three connected bodies, preserving horns and tail without leaking another pose.
        private static Pose[] SplitPoses(int width, int height, Color32[] pixels, out int[] labels)
        {
            labels = new int[pixels.Length];
            var pending = new int[pixels.Length];
            var regions = new List<Pose>();
            int label = 0;
            for (int first = 0; first < pixels.Length; first++)
            {
                if (pixels[first].a < 128 || labels[first] != 0) continue;
                var region = new Pose { Label = ++label, Left = width, Bottom = height };
                int count = 1; pending[0] = first; labels[first] = label;
                while (count > 0)
                {
                    int index = pending[--count], x = index % width, y = index / width;
                    region.Count++;
                    region.Left = Math.Min(region.Left, x); region.Right = Math.Max(region.Right, x);
                    region.Bottom = Math.Min(region.Bottom, y); region.Top = Math.Max(region.Top, y);
                    AddNeighbor(x > 0 ? index - 1 : -1, label, pixels, labels, pending, ref count);
                    AddNeighbor(x < width - 1 ? index + 1 : -1, label, pixels, labels, pending, ref count);
                    AddNeighbor(y > 0 ? index - width : -1, label, pixels, labels, pending, ref count);
                    AddNeighbor(y < height - 1 ? index + width : -1, label, pixels, labels, pending, ref count);
                }
                if (region.Count > pixels.Length / 100) regions.Add(region);
            }
            if (regions.Count != 3) throw new InvalidOperationException("O covil precisa de tres poses separadas do dragao.");
            regions.Sort((a, b) => a.Left.CompareTo(b.Left));
            return regions.ToArray();
        }

        private static void AddNeighbor(int index, int label, Color32[] pixels, int[] labels, int[] pending, ref int count)
        {
            if (index < 0 || labels[index] != 0 || pixels[index].a < 128) return;
            labels[index] = label; pending[count++] = index;
        }

        public void Wake() { awakening.Wake(); RenderPose(0); }
        public void FinishAwakening() { awakening.Finish(); RenderPose(0); }
        public void ResetRest() { awakening.Reset(); RenderPose(0); }
        public void Advance(float seconds) { awakening.Advance(seconds); RenderPose(Time.unscaledTime); }

        private void Update() => Advance(Time.unscaledDeltaTime);
        private void RenderPose(float seconds)
        {
            if (body == null || poses == null) return;
            body.sprite = poses[awakening.Frame];
            float breath = awakening.State == DragonRestState.Sleeping ? Mathf.Sin(seconds * 1.7f) * .012f : 0;
            transform.localScale = new Vector3(1 - breath * .2f, 1 + breath, 1);
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object asset in owned)
                if (asset != null) { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }
        }
    }
}
