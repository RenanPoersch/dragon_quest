using System;
using System.Collections.Generic;
using System.IO;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class MeteorAbsorptionValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Comet Absorption")]
    public static void Run()
    {
        foreach (string resource in new[] { "ArenMeteorAbsorb", "TyrantFireballRelease", "MeteorFireRibbons" })
        {
            Texture2D texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            Assert(texture != null, "Missing absorption sprite: " + resource);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert(importer.isReadable && !importer.mipmapEnabled && texture.filterMode == FilterMode.Point
                && texture.wrapMode == TextureWrapMode.Clamp && importer.npotScale == TextureImporterNPOTScale.None
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.alphaIsTransparency, "Preserve transparent pixel sprites: " + resource);
            Assert(texture.GetPixel(0, 0).a == 0 && texture.GetPixel(texture.width - 1, texture.height - 1).a == 0,
                "Transparent margins: " + resource);
        }

        Rect king = new Rect(410, 169, 80, 110), ground = new Rect(925, 125, 64, 90);
        var drawn = new List<string>();
        var bounds = new List<Rect>();
        var spells = new CastleSpellAnimation((texture, rect, uv, tint) => {
            Assert(uv.width > 0 && uv.height > 0 && rect.width > 0 && rect.height > 0, "Nonempty sprite draw.");
            drawn.Add(texture.name); bounds.Add(rect);
        });
        for (int frame = 0; frame < 216; frame++)
        {
            float time = frame / 60f;
            drawn.Clear(); bounds.Clear();
            Rect bearer = MeteorAbsorptionAnimation.BearerRect(ground, time);
            Assert(spells.DrawAbsorber(bearer, "aren", time, Color.white), "Hero pose exists throughout absorption.");
            Assert(spells.DrawAbsorbingCaster(king, time, Color.white), "Empty palm replaces the baked casting fireball.");
            spells.DrawMeteor(king, bearer, true, time);
            Assert(!drawn.Contains("TyrantFlamingMeteor") && !drawn.Contains("MeteorAbsorption"),
                "Absorption draws neither comet nor old armor emblem.");
            bool streaming = time >= CastleMeteorPresentation.AbsorptionStart && time < CastleMeteorPresentation.AbsorptionEnd;
            Assert(drawn.Contains("MeteorFireRibbons") == streaming, "Fire strips follow the transfer window.");
            Assert(drawn.FindAll(name => name == "TyrantFireballRelease").Count ==
                (time < CastleMeteorPresentation.AbsorptionEnd - .02f ? 2 : 1), "The casting orb is consumed once.");
            if (streaming)
            {
                int first = drawn.IndexOf("MeteorFireRibbons"), last = drawn.LastIndexOf("MeteorFireRibbons");
                Assert(first >= 0 && last - first == 63, "Transfer uses only 64 contiguous sprite sections.");
                Assert(bounds[first].center.x < bounds[last].center.x
                    && bounds[last].center.y < bounds[first].center.y, "Fire flows forward and upward into the airborne chest.");
            }
        }
        drawn.Clear();
        spells.DrawMeteor(king, ground, false, CastleMeteorPresentation.ImpactDelay);
        Assert(drawn.Count == 1 && drawn[0] == "TyrantFlamingMeteor", "Destructive impact keeps its existing sprite.");
        Rect arrived = MeteorAbsorptionAnimation.BearerRect(ground, 1.5f);
        Rect landed = MeteorAbsorptionAnimation.BearerRect(ground, CastleMeteorPresentation.Duration);
        Assert(arrived.x < ground.x - 200 && arrived.y < ground.y - 95 && landed == ground,
            "Forward flight and landing preserve the formation.");

        string output = Environment.GetEnvironmentVariable("DQ_COMET_OUTPUT");
        if (!string.IsNullOrEmpty(output)) RenderPreview(output, king, ground);
        Debug.Log("COMET_ABSORPTION_UNITY_OK: sprite imports, 216 rendered frames, fire transfer and preserved destructive impact.");
    }

    private static void RenderPreview(string output, Rect king, Rect ground)
    {
        Directory.CreateDirectory(output);
        var target = new RenderTexture(1280, 420, 0, RenderTextureFormat.ARGB32);
        var material = new Material(Shader.Find("Sprites/Default"));
        var previous = RenderTexture.active;
        try
        {
            var spells = new CastleSpellAnimation((texture, rect, uv, tint) =>
                Graphics.DrawTexture(rect, texture, uv, 0, 0, 0, 0, tint, material));
            Texture2D background = Resources.Load<Texture2D>("BattleBackgrounds/castle");
            float[] times = { .05f, .7f, 1.25f, 2.1f, 2.85f, 3.5f };
            foreach (float time in times)
            {
                RenderTexture.active = target;
                GL.Clear(true, true, new Color(.1f, .09f, .17f));
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, 1280, 420, 0);
                Graphics.DrawTexture(new Rect(0, 0, 1280, 420), background,
                    new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.white, material);
                Rect bearer = MeteorAbsorptionAnimation.BearerRect(ground, time);
                spells.DrawAbsorbingCaster(king, time, Color.white);
                spells.DrawAbsorber(bearer, "aren", time, Color.white);
                spells.DrawMeteor(king, bearer, true, time);
                GL.PopMatrix();
                var capture = new Texture2D(1280, 420, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, 1280, 420), 0, 0); capture.Apply();
                File.WriteAllBytes(Path.Combine(output, "comet-" + time.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }
        finally
        {
            RenderTexture.active = previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(material);
        }
    }

    private static void Assert(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
