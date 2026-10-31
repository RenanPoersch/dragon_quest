using DragonQuest.Presentation.World.Animations;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.World.Scenery
{
    public sealed class DragonLairScenery : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Material material;

        public void Build()
        {
            Shader shader = Resources.Load<Shader>("RuinsSide");
            Texture2D painting = Resources.Load<Texture2D>("PixelArt/DragonLairPanorama");
            if (shader == null || painting == null) throw new InvalidOperationException("Pixel art do covil ausente.");
            material = new Material(shader) { name = "Covil / pixel art lateral" }; owned.Add(material);
            // Preserve the generated source and sample only in memory, like the ruins panorama.
            const int height = 320;
            int width = Mathf.RoundToInt(height * (float)painting.width / painting.height);
            Color32[] source = painting.GetPixels32(), pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = source[Math.Min(painting.height - 1, (int)((y + .5f) * painting.height / height))
                        * painting.width + Math.Min(painting.width - 1, (int)((x + .5f) * painting.width / width))];
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Covil / panorama em grade de pixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true); owned.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero,
                width / 44f, 0, SpriteMeshType.FullRect); owned.Add(sprite);
            var panorama = new GameObject("Covil / basalto, pilares antigos e rios de lava");
            panorama.transform.SetParent(transform, false);
            panorama.transform.localPosition = new Vector3(-22, -4.95f, 8);
            var renderer = panorama.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
        }

        public DragonLairAnimation CreateDragon(Transform anchor)
        {
            var visual = new GameObject("Dragao / sono e despertar em pixel art");
            visual.transform.SetParent(anchor, false);
            // The interaction sits at the snout; the larger body occupies the chamber behind it.
            visual.transform.localPosition = new Vector3(2.25f, 0, 0);
            var animation = visual.AddComponent<DragonLairAnimation>(); animation.Initialize(material); return animation;
        }

        public GameObject CreateEssence(Transform anchor)
        {
            var sprite = new GameObject("Essencia liberta / chama pixel art");
            sprite.transform.SetParent(anchor, false); sprite.transform.localPosition = new Vector3(0, .6f, -.1f);
            sprite.AddComponent<RuinsFlameAnimation>().Initialize(material);
            sprite.SetActive(false); return sprite;
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object asset in owned)
                if (asset != null) { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }
        }
    }
}
