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
            Assert(Mathf.Approximately(portrait.yMax,panel.yMax-DialoguePresentation.PortraitBottomInset),
                "portrait bottom clears the panel border: " + resolution);
            float maximumPortraitHeight = panel.height / (1-DialoguePresentation.PortraitOverhang);
            Assert(portrait.height <= maximumPortraitHeight+.001f,
                "portrait size preserves the 20 percent overhang before border offset: " + resolution);
            if (Mathf.Approximately(portrait.height,maximumPortraitHeight))
                Assert(Mathf.Abs(panel.y-DialoguePresentation.PortraitBottomInset-portrait.y-portrait.height*.20f)<.001f,
                    "wide layout keeps the 20 percent overhang plus border offset: " + resolution);
            Assert(portrait.xMax+20 < panel.xMax-60 && portrait.y >= 0,
                "overlapping portrait leaves readable text space and stays on screen: " + resolution);
        }
        ValidateDialogueFlow();
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
            + " original portraits; compact fixed panel, portraits above the bottom border, 20% sizing plus border offset, reveal/continue/choices/callbacks, native resolution, transparent alpha, Point/Clamp, no compression/mipmaps/readable copy; "
            + checkedPixels + " source pixels checked.";
        File.WriteAllText(Path.Combine(output, "result.txt"), result);
        Debug.Log(result);
    }

    private static void ValidateDialogueFlow()
    {
        var root = new GameObject("Dialogue / flow validation");
        try
        {
            var dialogue = root.AddComponent<DragonQuest.Interactions.DialogueController>();
            int completed = 0;
            dialogue.OpenWithCompletion("Aldeao",()=>completed++,"Igual","Igual");
            int version = dialogue.ContentVersion;
            Assert(dialogue.IsTyping && dialogue.VisibleText == "", "speech starts with progressive writing");
            dialogue.Continue();
            Assert(dialogue.VisibleText == "Igual" && dialogue.PageNumber == 1 && completed == 0,
                "first continue reveals the page without advancing or calling completion");
            dialogue.Continue();
            Assert(dialogue.PageNumber == 2 && dialogue.IsTyping && dialogue.VisibleText == "" && dialogue.ContentVersion != version,
                "identical consecutive pages restart writing and scroll identity");
            dialogue.Continue(); dialogue.Continue(); dialogue.Continue();
            Assert(!dialogue.IsOpen && completed == 1, "completion runs exactly once after a separate continue");
            dialogue.OpenWithCompletion("Aldeao",()=>completed++,"Cancelado");
            dialogue.Close();
            Assert(!dialogue.IsTyping && dialogue.VisibleText == "" && completed == 1,
                "cancel clears writing without invoking completion");
            int selected = 0;
            dialogue.OpenChoices("Hospedeira","Sua jornada",new[]{"Salvar","Voltar"},i=>
            {
                selected++;
                dialogue.Open("Hospedeira","Resposta");
            });
            Assert(dialogue.HasChoices && !dialogue.ChoicesReady, "choices wait for the speech");
            dialogue.Continue(); dialogue.Continue();
            Assert(dialogue.ChoicesReady && selected == 0, "continuation reveals choices without selecting them");
            Assert(dialogue.Choose(0) && selected == 1 && dialogue.IsTyping && dialogue.Text == "Resposta",
                "choice callback can open a new progressively written conversation");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Dialogue portraits: " + message); }
}
