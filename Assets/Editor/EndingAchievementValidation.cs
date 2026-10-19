using System;
using System.IO;
using System.Reflection;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class EndingAchievementValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Ending Achievements")]
    public static void Run()
    {
        ValidateNotifications();
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
            Rect icon = EndingAchievementPresentation.IconBounds, label = EndingAchievementPresentation.NameBounds;
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
        Assert(titleSource.Contains("achievementScreen.Draw();"),
            "The title uses persistent achievement progress.");
        Debug.Log("ENDING_ACHIEVEMENT_UNITY_OK: three imported icons, pixel settings, transparency, font widths, layout and ending/menu hooks.");
    }

    private static void ValidateNotifications()
    {
        const BindingFlags hidden = BindingFlags.Static | BindingFlags.NonPublic;
        var storeField = typeof(PrototypeAchievements).GetField("store", hidden);
        var pendingField = typeof(PrototypeAchievements).GetField("pendingNotification", hidden);
        var reset = typeof(PrototypeAchievements).GetMethod("ResetSession", hidden);
        object originalStore = storeField.GetValue(null), originalPending = pendingField.GetValue(null);
        const string legacyMarker = "DragonQuest.EndingAchievements.NotifiedMask.v1";
        bool hadLegacyMarker = PlayerPrefs.HasKey(legacyMarker);
        int originalMarker = PlayerPrefs.GetInt(legacyMarker);
        string directory = Path.Combine(Path.GetTempPath(), "DragonQuestAchievementNotifications-" + Guid.NewGuid().ToString("N"));
        Action restart = () =>
        {
            reset.Invoke(null, null);
            storeField.SetValue(null, new EndingAchievementStore(directory));
        };
        try
        {
            PlayerPrefs.DeleteKey(legacyMarker);
            restart();
            Assert(PrototypeAchievements.TakeNotification() == null, "A fresh startup has no announcement.");
            PrototypeAchievements.Complete(GameEnding.KingFreed);
            Assert(PrototypeAchievements.TakeNotification() == EndingAchievements.All[0], "A newly unlocked endgame announces its achievement.");
            Assert(PrototypeAchievements.TakeNotification() == null, "A new achievement cannot announce twice.");
            var saved = new EndingAchievementStore(directory);
            foreach (var achievement in EndingAchievements.All)
                Assert(saved.TryUnlock(achievement.Ending, out _), "Prepare previously completed endings.");
            restart();
            Assert(PrototypeAchievements.Progress.Count == EndingAchievements.Total, "Saved achievements remain unlocked.");
            Assert(PrototypeAchievements.TakeNotification() == null, "Saved endings never announce themselves on startup.");
            foreach (var achievement in EndingAchievements.All)
            {
                PrototypeAchievements.Complete(achievement.Ending);
                Assert(PrototypeAchievements.TakeNotification() == achievement, "A completed endgame announces its matching achievement.");
                Assert(PrototypeAchievements.TakeNotification() == null, "The announcement is consumed once, even when interrupted.");
                restart();
                Assert(PrototypeAchievements.TakeNotification() == null, "Restarting or returning to the title cannot replay old announcements.");
            }
            PrototypeAchievements.Complete(GameEnding.None);
            PrototypeAchievements.Complete((GameEnding)99);
            Assert(PrototypeAchievements.TakeNotification() == null, "No announcement for defeat or an unknown ending.");
            PrototypeAchievements.Complete(GameEnding.KingFreed);
            restart();
            Assert(PrototypeAchievements.TakeNotification() == null, "Announcements do not survive a new play session.");
            Assert(PrototypeAchievements.Progress.Count == EndingAchievements.Total, "Notification consumption preserves saved progress.");
            Debug.Log("ENDING_ACHIEVEMENT_NOTIFICATIONS_OK: startup, saved endings, all completions, one-shot consumption, restart and invalid endings.");
        }
        finally
        {
            if (hadLegacyMarker) PlayerPrefs.SetInt(legacyMarker, originalMarker);
            else PlayerPrefs.DeleteKey(legacyMarker);
            storeField.SetValue(null, originalStore);
            pendingField.SetValue(null, originalPending);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
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
        var sunset = Resources.Load<Texture2D>("PixelArt/Intro/Sunset");
        if (sunset != null) GUI.DrawTexture(new Rect(0, 0, 1280, 720), sunset, ScaleMode.ScaleToFit, true);
        var style = PrototypeTypography.Style(GUI.skin.label, 56, true); style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 100, 1280, 96), IntroSequence.Title, style);
        if (count > 0) presentation.Show(EndingAchievements.All[count - 1], progress.Counter);
        else presentation.Dismiss();
        presentation.Draw();
        GUI.color = color; GUI.matrix = previous;
    }
}
