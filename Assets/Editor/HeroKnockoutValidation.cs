using DragonQuest.Presentation.Battle.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroKnockoutValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Hero Knockout Sprites")]
    public static void Run()
    {
        foreach (string id in new[] { "aren", "lia", "bram" })
        {
            string resource = "PixelArt/Knockout/" + HeroKnockoutSprites.Resource(id);
            var texture = Resources.Load<Texture2D>(resource);
            Assert(texture != null, "Missing knockout sprite: " + id);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(texture.width == 1536 && texture.height == 1024, "Preserve native dimensions: " + id);
            Assert(importer.isReadable && texture.filterMode == FilterMode.Point
                && texture.wrapMode == TextureWrapMode.Clamp && !importer.mipmapEnabled
                && importer.npotScale == TextureImporterNPOTScale.None
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.alphaIsTransparency, "Readable point-filtered uncompressed alpha: " + id);
            Rect bounds = Bounds(texture, texture.width);
            Assert(bounds.width > 500 && bounds.height > 500 && bounds.xMin > 0 && bounds.yMin > 0
                && bounds.xMax < texture.width && bounds.yMax < texture.height, "Complete isolated pose: " + id);
            Assert(texture.GetPixel(0, 0).a == 0 && texture.GetPixel(texture.width - 1, texture.height - 1).a == 0,
                "Transparent corners: " + id);
        }
        Assert(HeroKnockoutSprites.Resource("unknown") == null, "Unknown characters retain their existing fallback.");
        string output = Environment.GetEnvironmentVariable("DQ_KNOCKOUT_OUTPUT");
        if (!string.IsNullOrEmpty(output)) RenderPreview(output);
        Debug.Log("HERO_KNOCKOUT_UNITY_OK: three native transparent poses imported with point filtering and no compression.");
    }

    private static Rect Bounds(Texture2D texture, int rightEdge)
    {
        var pixels = texture.GetPixels32();
        int left = rightEdge, right = -1, bottom = texture.height, top = -1;
        for (int y = 0; y < texture.height; y++) for (int x = 0; x < rightEdge; x++)
            if (pixels[y * texture.width + x].a >= 128)
            { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
        Assert(right >= left, "Empty sprite.");
        return new Rect(left, bottom, right - left + 1, top - bottom + 1);
    }

    // Render the imported textures on two scales: live battle size and twice that size.
    private static void RenderPreview(string output)
    {
        Directory.CreateDirectory(output);
        var objects = new List<UnityEngine.Object>();
        var target = new RenderTexture(1080, 400, 24, RenderTextureFormat.ARGB32);
        var cameraObject = new GameObject("Knockout sprite validation camera"); objects.Add(cameraObject);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 200; camera.aspect = 1080f / 400;
        camera.transform.position = new Vector3(540, 200, -10); camera.targetTexture = target;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f, .22f, .25f);
        var previous = RenderTexture.active;
        try
        {
            string[] ids = { "aren", "lia", "bram" };
            string[] names = { "Aren", "Lia", "Bram" };
            int[] idleEdges = { 512, 492, 554 };
            for (int i = 0; i < ids.Length; i++)
            {
                var idle = Resources.Load<Texture2D>("PixelArt/" + names[i] + "Battle");
                var knockout = Resources.Load<Texture2D>("PixelArt/Knockout/" + HeroKnockoutSprites.Resource(ids[i]));
                Assert(idle != null, "Idle reference exists: " + ids[i]);
                for (int row = 0; row < 2; row++)
                {
                    float height = row == 0 ? 90 : 180, floor = row == 0 ? 290 : 35;
                    AddSprite(idle, Bounds(idle, idleEdges[i] * idle.width / 2172), i * 360 + 90, floor, height, objects);
                    AddSprite(knockout, Bounds(knockout, knockout.width), i * 360 + 240, floor,
                        height * HeroKnockoutSprites.HeightRatio, objects);
                }
            }
            camera.Render(); RenderTexture.active = target;
            var readback = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false); objects.Add(readback);
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); readback.Apply();
            File.WriteAllBytes(Path.Combine(output, "HeroKnockout.preview.png"), readback.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            foreach (var item in objects) UnityEngine.Object.DestroyImmediate(item);
        }
    }
    private static void AddSprite(Texture2D texture, Rect bounds, float x, float y, float height, List<UnityEngine.Object> objects)
    {
        var sprite = Sprite.Create(texture, bounds, new Vector2(.5f, 0), 1); objects.Add(sprite);
        var figure = new GameObject(texture.name); objects.Add(figure);
        figure.transform.position = new Vector3(x, y, 0); figure.transform.localScale = Vector3.one * (height / bounds.height);
        figure.AddComponent<SpriteRenderer>().sprite = sprite;
    }
    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
