using DragonQuest.Presentation.Menus;
using DragonQuest.Presentation.Common;
using DragonQuest.Presentation.Cinematics.Opening;
using DragonQuest.Gameplay.Achievements;
using System;
using System.IO;
using System.Reflection;
using DragonQuest.Persistence;
using DragonQuest.Progression;
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
        string endingSource = File.ReadAllText("Assets/Scripts/Presentation/Cinematics/Endings/EndingController.cs");
        Assert(endingSource.Contains("AchievementService.Complete(ending);"), "Endings save the profile before returning.");
        Assert(endingSource.Split(new[] { "SceneManager.LoadScene(" }, StringSplitOptions.None).Length == 2,
            "There is one shared scene reload path.");
        string titleSource = File.ReadAllText("Assets/Scripts/Presentation/Menus/FrontEndPresentation.cs");
        Assert(titleSource.Contains("achievementScreen.Draw();"),
            "The title uses persistent achievement progress.");
        Debug.Log("ENDING_ACHIEVEMENT_UNITY_OK: three imported icons, pixel settings, transparency, font widths, layout and ending/menu hooks.");
    }

    private static void ValidateNotifications()
    {
        const BindingFlags hidden = BindingFlags.Static | BindingFlags.NonPublic;
        var storeField = typeof(AchievementService).GetField("store", hidden);
        var pendingField = typeof(AchievementService).GetField("pendingNotification", hidden);
        var reset = typeof(AchievementService).GetMethod("ResetSession", hidden);
        object originalStore = storeField.GetValue(null), originalPending = pendingField.GetValue(null);
        const string legacyMarker = "DragonQuest.EndingAchievements.NotifiedMask.v1";
        bool hadLegacyMarker = PlayerPrefs.HasKey(legacyMarker);
        int originalMarker = PlayerPrefs.GetInt(legacyMarker);
        string directory = Path.Combine(Path.GetTempPath(), "DragonQuestAchievementNotifications-" + Guid.NewGuid().ToString("N"));
        string profileDirectory = directory;
        Action restart = () =>
        {
            reset.Invoke(null, null);
            storeField.SetValue(null, new EndingAchievementStore(profileDirectory));
        };
        try
        {
            var counterField = typeof(EndingAchievementPresentation).GetField("counter", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var order in new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } })
            {
                profileDirectory = Path.Combine(directory, string.Join("", order));
                restart();
                Assert(AchievementService.Progress.Counter == "0/3", "Each order starts with an empty profile.");
                for (int i = 0; i < order.Length; i++)
                {
                    var achievement = EndingAchievements.All[order[i]];
                    AchievementService.Complete(achievement.Ending);
                    var notification = AchievementService.TakeNotification();
                    Assert(notification == achievement, "The completed ending keeps its own name and icon in every order.");
                    string expectedCounter = (i + 1) + "/3";
                    var presentation = new EndingAchievementPresentation();
                    presentation.Show(notification, AchievementService.Progress.Counter);
                    Assert((string)counterField.GetValue(presentation) == expectedCounter,
                        "The notification shows 1/3, 2/3 and 3/3 in completion order.");
                    Assert(AchievementService.TakeNotification() == null, "Each completion announces once.");
                    restart();
                    Assert(AchievementService.Progress.Counter == expectedCounter
                        && AchievementService.TakeNotification() == null, "Restart preserves the count without replaying notices.");
                    for (int completed = 0; completed <= i; completed++)
                    {
                        var previous = EndingAchievements.All[order[completed]];
                        AchievementService.Complete(previous.Ending);
                        Assert(AchievementService.TakeNotification() == previous
                            && AchievementService.Progress.Counter == expectedCounter, "Repeating any ending keeps the unique count.");
                    }
                }
            }
            profileDirectory = directory;
            PlayerPrefs.DeleteKey(legacyMarker);
            restart();
            Assert(AchievementService.TakeNotification() == null, "A fresh startup has no announcement.");
            AchievementService.Complete(GameEnding.KingFreed);
            Assert(AchievementService.TakeNotification() == EndingAchievements.All[0], "A newly unlocked endgame announces its achievement.");
            Assert(AchievementService.TakeNotification() == null, "A new achievement cannot announce twice.");
            var saved = new EndingAchievementStore(directory);
            foreach (var achievement in EndingAchievements.All)
                Assert(saved.TryUnlock(achievement.Ending, out _), "Prepare previously completed endings.");
            restart();
            Assert(AchievementService.Progress.Count == EndingAchievements.Total, "Saved achievements remain unlocked.");
            Assert(AchievementService.TakeNotification() == null, "Saved endings never announce themselves on startup.");
            foreach (var achievement in EndingAchievements.All)
            {
                AchievementService.Complete(achievement.Ending);
                Assert(AchievementService.TakeNotification() == achievement, "A completed endgame announces its matching achievement.");
                Assert(AchievementService.TakeNotification() == null, "The announcement is consumed once, even when interrupted.");
                restart();
                Assert(AchievementService.TakeNotification() == null, "Restarting or returning to the title cannot replay old announcements.");
            }
            AchievementService.Complete(GameEnding.None);
            AchievementService.Complete((GameEnding)99);
            Assert(AchievementService.TakeNotification() == null, "No announcement for defeat or an unknown ending.");
            AchievementService.Complete(GameEnding.KingFreed);
            restart();
            Assert(AchievementService.TakeNotification() == null, "Announcements do not survive a new play session.");
            Assert(AchievementService.Progress.Count == EndingAchievements.Total, "Notification consumption preserves saved progress.");
            Debug.Log("ENDING_ACHIEVEMENT_NOTIFICATIONS_OK: six completion orders, displayed counters, repeat endings, startup, saved endings, one-shot consumption, restart and invalid endings.");
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
        var style = GameTypography.Style(GUI.skin.label, 56, true); style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 100, 1280, 96), IntroSequence.Title, style);
        if (count > 0) presentation.Show(EndingAchievements.All[count - 1], progress.Counter);
        else presentation.Dismiss();
        presentation.Draw();
        GUI.color = color; GUI.matrix = previous;
    }
}
