using System;
using System.IO;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class SpiritAttackValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Spirit Attacks")]
    public static void Run()
    {
        var texture = Resources.Load<Texture2D>("PixelArt/SpiritAttacks");
        if (texture == null) throw new InvalidOperationException("Missing spirit attack sprites.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        if (!importer.isReadable || importer.mipmapEnabled || texture.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency)
            throw new InvalidOperationException("Spirit atlas must preserve readable native pixels and alpha.");
        var source = new Texture2D(2, 2);
        try
        {
            source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));
            if (source.width != texture.width || source.height != texture.height)
                throw new InvalidOperationException("Spirit atlas was resized during import.");
            var pixels = texture.GetPixels32();
            int transparent = 0;
            foreach (var pixel in pixels) if (pixel.a == 0) transparent++;
            if (transparent < pixels.Length / 10) throw new InvalidOperationException("Spirit atlas has no transparent gutters.");
            for (int row = 0; row < 5; row++) for (int col = 0; col < 4; col++)
            {
                int opaque = 0;
                for (int y = texture.height * row / 5; y < texture.height * (row + 1) / 5; y++)
                    for (int x = texture.width * col / 4; x < texture.width * (col + 1) / 4; x++)
                        if (pixels[y * texture.width + x].a >= 128) opaque++;
                if (opaque < 300) throw new InvalidOperationException("Empty spirit animation frame.");
            }
            new SpiritAttackAnimation().Prepare();
            Debug.Log("SPIRIT_ATTACK_VALIDATION_OK: 20 poses, five actions, native Point imports and transparent alpha.");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }
}
