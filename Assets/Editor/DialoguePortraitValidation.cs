using System;
using System.IO;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class DialoguePortraitValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Dialogue Portraits")]
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DQ_DIALOGUE_PORTRAIT_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DialoguePortraitValidation"));
        Directory.CreateDirectory(output);
        foreach (Vector2 resolution in new[] {new Vector2(1280,720),new Vector2(960,720),new Vector2(540,960)})
        {
            float scale = Mathf.Clamp(resolution.y/720f,.65f,2f);
            float width = resolution.x/scale, height = resolution.y/scale;
            Rect panel = DialoguePresentation.PanelBounds(width,height);
            Rect portrait = DialoguePresentation.PortraitBounds(width,height);
            Assert(Mathf.Approximately(panel.height,DialoguePresentation.ReferencePanelHeight),
                "dialogue height is fixed independently of text: " + resolution);
            Assert(Mathf.Abs(panel.y-portrait.y-portrait.height*.20f)<.001f,
                "portrait extends exactly 20 percent above dialogue: " + resolution);
            Assert(portrait.xMax+20 < panel.xMax-60 && portrait.y >= 0,
                "overlapping portrait leaves readable text space and stays on screen: " + resolution);
        }
        int checkedPixels = 0;
        foreach (string character in DialoguePortraitCatalog.Characters)
        {
            string resource = DialoguePortraitCatalog.Resource(character);
            var portrait = Resources.Load<Texture2D>(resource);
            Assert(portrait != null, "portrait is available: " + character);
            string path = AssetDatabase.GetAssetPath(portrait);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert(portrait.filterMode == FilterMode.Point && portrait.wrapMode == TextureWrapMode.Clamp,
                "crisp pixels and clamped edges: " + character);
            Assert(portrait.mipmapCount == 1 && !importer.mipmapEnabled && !importer.isReadable,
                "no mipmaps or unnecessary readable copy: " + character);
            Assert(importer.textureCompression == TextureImporterCompression.Uncompressed && importer.npotScale == TextureImporterNPOTScale.None,
                "no compression or non-power-of-two scaling: " + character);
            var source = new Texture2D(2, 2);
            try
            {
                Assert(source.LoadImage(File.ReadAllBytes(path)), "original PNG is readable: " + character);
                Assert(source.width == portrait.width && source.height == portrait.height,
                    "native source resolution is preserved: " + character);
                Assert(Mathf.Min(source.width, source.height) >= 512 && Mathf.Abs(source.width / (float)source.height - 1) < .15f,
                    "high resolution square portrait: " + character);
                bool clear = false, solid = false;
                foreach (Color32 pixel in source.GetPixels32())
                { clear |= pixel.a == 0; solid |= pixel.a > 200; checkedPixels++; }
                Assert(clear && solid, "nonempty portrait with real transparent background: " + character);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }
        Assert(DialoguePortraitCatalog.ResourceFor("Essencia liberta") != DialoguePortraitCatalog.ResourceFor("Dragao corrompido"),
            "liberated essence has its own artwork");
        Assert(DialoguePortraitCatalog.ResourceFor("Guardiao da pedreira") == DialoguePortraitCatalog.ResourceFor("Lenhador"),
            "quarry guardian keeps the woodcutter identity");
        Assert(DialoguePortraitCatalog.ResourceFor("Altar antigo") == DialoguePortraitCatalog.ResourceFor("Aren"),
            "narrated discoveries use Aren's illustrated portrait");
        string result = "DIALOGUE_PORTRAITS_VALIDATION_OK: " + DialoguePortraitCatalog.Characters.Count
            + " original portraits; fixed panel height, exact 20% portrait overhang, native resolution, transparent alpha, Point/Clamp, no compression/mipmaps/readable copy; "
            + checkedPixels + " source pixels checked.";
        File.WriteAllText(Path.Combine(output, "result.txt"), result);
        Debug.Log(result);
    }

    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Dialogue portraits: " + message); }
}
