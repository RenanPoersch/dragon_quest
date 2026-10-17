using System;
using System.IO;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class EndingAchievementValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Ending Achievements")]
    public static void Run()
    {
        foreach (var achievement in EndingAchievements.All)
        {
            var icon = Resources.Load<Texture2D>(achievement.IconResource);
            Assert(icon != null, "Icon exists: " + achievement.Name);
            string path = AssetDatabase.GetAssetPath(icon);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert(icon.width == icon.height && icon.width >= 64, "Square artwork: " + achievement.Name);
            Assert(icon.filterMode == FilterMode.Point && icon.wrapMode == TextureWrapMode.Clamp
                && !importer.mipmapEnabled && importer.npotScale == TextureImporterNPOTScale.None
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.maxTextureSize >= icon.width, "Native point-filtered pixels: " + achievement.Name);
            var decoded = new Texture2D(2, 2);
            try
            {
                Assert(decoded.LoadImage(File.ReadAllBytes(path)), "PNG decodes.");
                Assert(decoded.GetPixel(0, 0).a == 0 && decoded.GetPixel(decoded.width - 1, decoded.height - 1).a == 0,
                    "Transparent margins: " + achievement.Name);
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }
        var font = Resources.Load<Font>("Fonts/PixelOperator-Bold");
        Assert(font != null, "Pixel font exists.");
        Rect scale = new Rect(440, 160, 400, 400);
        Rect newGame = new Rect(490, 562, 300, 48), resume = new Rect(490, 620, 300, 48);
        Assert(!EndingAchievementPresentation.Bounds.Overlaps(scale)
            && !EndingAchievementPresentation.Bounds.Overlaps(newGame)
            && !EndingAchievementPresentation.Bounds.Overlaps(resume), "Panel keeps title art and both buttons clear.");
        for (int i = 0; i < EndingAchievements.Total; i++)
        {
            Rect icon = EndingAchievementPresentation.IconRect(i), label = EndingAchievementPresentation.NameRect(i);
            Assert(Contains(EndingAchievementPresentation.Bounds, icon)
                && Contains(EndingAchievementPresentation.Bounds, label) && !icon.Overlaps(label), "Badge and name fit row.");
            string name = EndingAchievements.All[i].Name;
            font.RequestCharactersInTexture(name, 28, FontStyle.Normal);
            int width = 0;
            foreach (char letter in name)
            {
                Assert(font.GetCharacterInfo(letter, out var info, 28, FontStyle.Normal), "Font covers achievement title.");
                width += info.advance;
            }
            Assert(width + 8 < label.width, "Achievement name fits its column.");
        }
        // All return paths use the same completion hook, including the fallback narrative.
        string endingSource = File.ReadAllText("Assets/Scripts/Prototype/PrototypeEndingController.cs");
        Assert(endingSource.Contains("PrototypeAchievements.Complete(ending);"), "Endings save the profile before returning.");
        Assert(endingSource.Split(new[] { "SceneManager.LoadScene(" }, StringSplitOptions.None).Length == 2,
            "There is one shared scene reload path.");
        string titleSource = File.ReadAllText("Assets/Scripts/Prototype/PrototypeSaveController.cs");
        Assert(titleSource.Contains("achievementScreen.Draw(PrototypeAchievements.Progress);"),
            "The title uses persistent achievement progress.");
        Debug.Log("ENDING_ACHIEVEMENT_UNITY_OK: three imported icons, pixel settings, transparency, font widths, layout and ending/menu hooks.");
    }

    private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax
        && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    [MenuItem("Tools/Dragon Quest/Preview Ending Achievements")]
    public static void Preview()
    {
        var window = EditorWindow.GetWindow<EndingAchievementPreview>();
        window.titleContent = new GUIContent("Ending achievements");
        window.minSize = new Vector2(640, 400);
        window.Show();
    }
}

public sealed class EndingAchievementPreview : EditorWindow
{
    private readonly EndingAchievementPresentation presentation = new EndingAchievementPresentation();
    private int count = 1;
    private void OnGUI()
    {
        count = EditorGUILayout.IntSlider("Finais concluídos (prévia)", count, 0, 3);
        var progress = new EndingAchievements();
        for (int i = 0; i < count; i++) progress.Unlock(EndingAchievements.All[i].Ending);
        var previous = GUI.matrix;
        float scale = Mathf.Min(position.width / 1280, (position.height - 24) / 720);
        GUI.matrix = Matrix4x4.TRS(new Vector3((position.width - 1280 * scale) / 2, 24, 0),
            Quaternion.identity, new Vector3(scale, scale, 1));
        Color color = GUI.color; GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture); GUI.color = Color.white;
        var emblem = Resources.Load<Texture2D>("PixelArt/DragonScale");
        if (emblem != null) GUI.DrawTexture(new Rect(440, 160, 400, 400), emblem, ScaleMode.ScaleToFit, true);
        var style = PrototypeTypography.Style(GUI.skin.label, 36, true); style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(490, 562, 300, 48), "New game", style);
        GUI.Label(new Rect(490, 620, 300, 48), "Continue", style);
        presentation.Draw(progress);
        GUI.color = color; GUI.matrix = previous;
    }
}
