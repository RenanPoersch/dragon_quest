using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Partition the original drawing along the curved near gunwale, without repainting it.
    // Both layers keep identical registration, so the party can stand inside the hull.
    public sealed class VoyageShipLayers : IDisposable
    {
        public Texture2D BehindParty { get; }
        public Texture2D InFrontOfParty { get; }
        public int Width => BehindParty.width;
        public int Height => BehindParty.height;
        private static readonly Vector2[] Rail =
        {
            new Vector2(0, .62f), new Vector2(.09f, .688f), new Vector2(.15f, .716f),
            new Vector2(.22f, .727f), new Vector2(.28f, .739f), new Vector2(.4f, .746f),
            new Vector2(.56f, .752f), new Vector2(.7f, .75f), new Vector2(.76f, .735f),
            new Vector2(.8f, .68f), new Vector2(.88f, .62f), new Vector2(1, .59f)
        };

        public VoyageShipLayers(Texture2D source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Color32[] original = source.GetPixels32();
            var rear = new Color32[original.Length];
            var front = new Color32[original.Length];
            for (int x = 0; x < source.width; x++)
            {
                float edge = RailHeightAt((x + .5f) / source.width);
                for (int y = 0; y < source.height; y++)
                {
                    int index = y * source.width + x;
                    if (1 - (y + .5f) / source.height >= edge) front[index] = original[index];
                    else rear[index] = original[index];
                }
            }
            BehindParty = Layer("Voyage / sails and far side", source.width, source.height, rear);
            InFrontOfParty = Layer("Voyage / near gunwale and hull", source.width, source.height, front);
        }

        public static float RailHeightAt(float x)
        {
            for (int i = 1; i < Rail.Length; i++)
                if (x <= Rail[i].x)
                    return Mathf.Lerp(Rail[i - 1].y, Rail[i].y, Mathf.InverseLerp(Rail[i - 1].x, Rail[i].x, x));
            return Rail[Rail.Length - 1].y;
        }

        public static float DeckHeightAt(float x) => RailHeightAt(x) + .018f;

        private static Texture2D Layer(string name, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            // Keep this small runtime texture readable for the Editor's reconstruction check.
            texture.SetPixels32(pixels); texture.Apply(false, false); return texture;
        }

        public void Dispose() { DestroyOwned(BehindParty); DestroyOwned(InFrontOfParty); }
        private static void DestroyOwned(UnityEngine.Object asset)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
            else UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
