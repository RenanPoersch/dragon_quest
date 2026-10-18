using System;
using System.IO;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;

public static class SpiritAttackValidation
{
    internal static readonly AbilityDefinition[] Skills = {
        new AbilityDefinition("mind-control", "Mind Control", "", 0, 1, AbilityEffect.MindControl, TargetRule.Enemy),
        new AbilityDefinition("silence", "Silence", "", 0, 1, AbilityEffect.Silence, TargetRule.Enemy),
        new AbilityDefinition("drain-life", "Sugar Vida", "", 0, 8, AbilityEffect.Drain, TargetRule.Enemy),
        new AbilityDefinition("chains", "Correntes", "", 0, 1, AbilityEffect.Bind, TargetRule.Enemy),
        new AbilityDefinition("chaos-magic", "Caos Magic", "", 0, 5, AbilityEffect.MagicDamage, TargetRule.AllEnemies) };
    [MenuItem("Tools/Dragon Quest/Validate Spirit Attacks")]
    public static void Run()
    {
        var validated = new System.Collections.Generic.HashSet<Texture2D>();
        foreach (string resource in SpiritAttackTimeline.Resources)
        {
            var sheet = new SpiritBossSpriteSheet(resource, (texture, rect, uv, color) => { });
            for (int index = 0; index < sheet.Coordinates.frames.Length; index++)
            {
                var texture = sheet.FrameTexture(index);
                if (validated.Add(texture)) ValidateTexture(texture, resource);
                var frame = sheet.Coordinates.frames[index];
                if (frame.width < 12 || frame.height < 12 || frame.x < 0 || frame.y < 0
                    || frame.x + frame.width > texture.width || frame.y + frame.height > texture.height
                    || frame.pivotX < 0 || frame.pivotX > frame.width)
                    throw new InvalidOperationException("Empty or clipped spirit frame: " + resource);
                if (index < 4 && !string.IsNullOrEmpty(sheet.Coordinates.casterResource))
                {
                    if (frame.x < 8 || frame.y < 8 || frame.x + frame.width > texture.width - 8
                        || frame.y + frame.height > texture.height - 8)
                        throw new InvalidOperationException("Caster chain is too close to its frame edge.");
                    for (int previous = 0; previous < index; previous++)
                    {
                        var other = sheet.Coordinates.frames[previous];
                        if (new Rect(frame.x,frame.y,frame.width,frame.height).Overlaps(
                            new Rect(other.x,other.y,other.width,other.height)))
                            throw new InvalidOperationException("Caster poses overlap in the source atlas.");
                    }
                }
            }
        }
        // A party-wide catastrophe draws one nova, rather than stacking three copies.
        int chaosDraws = 0;
        var renderer = new SpiritAttackAnimation((texture, rect, uv, tint) => chaosDraws++);
        renderer.DrawEffects(new Rect(215, 90, 170, 175), new[] { new Rect(925,125,64,90),
            new Rect(985,203,64,90), new Rect(925,281,64,90) }, Skills[4], SpiritAttackTimeline.Impact(Skills[4]));
        if (chaosDraws != 1) throw new InvalidOperationException("Chaos must have one shared arena impact.");
        Debug.Log("SPIRIT_ATTACK_VALIDATION_OK: five independent skills, 60 native sprite frames, measured pivots and one party-wide nova.");
    }
    private static void ValidateTexture(Texture2D texture, string resource)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        if (!importer.isReadable || importer.mipmapEnabled || texture.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency)
            throw new InvalidOperationException("Spirit atlas must preserve native pixels and alpha: " + resource);
        var source = new Texture2D(2, 2);
        try
        {
            source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));
            if (source.width != texture.width || source.height != texture.height)
                throw new InvalidOperationException("Spirit atlas was resized during import: " + resource);
            var pixels = texture.GetPixels32(); int transparent = 0;
            foreach (var pixel in pixels) if (pixel.a == 0) transparent++;
            if (transparent < pixels.Length / 12) throw new InvalidOperationException("No real transparent alpha: " + resource);
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }
    [MenuItem("Tools/Dragon Quest/Preview Spirit Boss Skills")]
    public static void Preview()
    {
        var window = EditorWindow.GetWindow<SpiritBossPreview>();
        window.titleContent = new GUIContent("Espirito / Boss final");
        window.minSize = new Vector2(900, 350); window.Show();
    }
    [MenuItem("Tools/Dragon Quest/Export Spirit Boss Preview")]
    public static void Capture()
    {
        Run();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/SpiritBossPreview"));
        Directory.CreateDirectory(output);
        var material = new Material(Shader.Find("Sprites/Default"));
        var target = new RenderTexture(1280, 416, 0, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1280, 416, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        Action<Texture2D, Rect, Rect, Color> draw = (texture, rect, uv, tint) =>
        {
            material.mainTexture = texture; material.SetPass(0);
            GL.Begin(GL.QUADS); GL.Color(tint);
            GL.TexCoord2(uv.x, uv.yMax); GL.Vertex3(rect.x, rect.y, 0);
            GL.TexCoord2(uv.xMax, uv.yMax); GL.Vertex3(rect.xMax, rect.y, 0);
            GL.TexCoord2(uv.xMax, uv.y); GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.TexCoord2(uv.x, uv.y); GL.Vertex3(rect.x, rect.yMax, 0);
            GL.End();
        };
        var animation = new SpiritAttackAnimation(draw); animation.Prepare();
        var scene = new SpiritBossPreviewScene(draw);
        int sequence = 0;
        try
        {
            target.Create(); RenderTexture.active = target;
            for (int skill = 0; skill < Skills.Length; skill++)
            {
                float duration = SpiritAttackTimeline.Duration(Skills[skill]);
                int count = Mathf.CeilToInt((duration + .6f) * 20);
                for (int frame = 0; frame < count; frame++)
                {
                    float time = frame / 20f;
                    GL.Clear(true, true, new Color(.035f, .025f, .065f, 1));
                    GL.PushMatrix(); GL.LoadPixelMatrix(0, 1280, 416, 0);
                    Vector2 shake = SpiritAttackAnimation.ScreenOffset(Skills[skill], time);
                    GL.MultMatrix(Matrix4x4.Translate(new Vector3(shake.x, shake.y, 0)));
                    scene.Draw(animation, Skills[skill], time);
                    GL.PopMatrix();
                    pixels.ReadPixels(new Rect(0, 0, 1280, 416), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(output, "frame" + (sequence++).ToString("D4") + ".png"), pixels.EncodeToPNG());
                    if (frame == Mathf.RoundToInt(SpiritAttackTimeline.Impact(Skills[skill]) * 20))
                        File.WriteAllBytes(Path.Combine(output, Skills[skill].Id + ".png"), pixels.EncodeToPNG());
                }
            }
            Debug.Log("SPIRIT_BOSS_PREVIEW_OK: " + sequence + " frames at 20 fps, output: " + output);
        }
        finally
        {
            RenderTexture.active = previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}

internal sealed class SpiritBossPreviewScene
{
    private readonly Action<Texture2D, Rect, Rect, Color> draw;
    private readonly Texture2D background;
    private readonly ChainPrisonAnimation chainPrisons;
    private readonly Texture2D[] actors = new Texture2D[4];
    private readonly Rect[] uv = new Rect[4];
    private readonly float[] aspect = new float[4];
    private readonly Rect caster = new Rect(215, 90, 170, 175);
    private readonly Rect[] heroes = { new Rect(925,125,64,90), new Rect(985,203,64,90), new Rect(925,281,64,90) };
    public SpiritBossPreviewScene(Action<Texture2D, Rect, Rect, Color> draw)
    {
        this.draw = draw;
        chainPrisons = new ChainPrisonAnimation(draw);
        background = Resources.Load<Texture2D>("BattleBackgrounds/castle");
        string[] resources = { "SpiritBattle", "ArenBattle", "LiaBattle", "BramBattle" };
        for (int i = 0; i < resources.Length; i++)
        {
            var texture = actors[i] = Resources.Load<Texture2D>("PixelArt/" + resources[i]);
            var pixels = texture.GetPixels32(); int left = texture.width, right = -1, top = -1, bottom = texture.height;
            int edge = i == 0 ? texture.width : texture.width / 4;
            for (int y = 0; y < texture.height; y++) for (int x = 0; x < edge; x++)
                if (pixels[y * texture.width + x].a >= 128)
                { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            uv[i] = new Rect((float)left / texture.width, (float)bottom / texture.height,
                (float)(right-left+1) / texture.width, (float)(top-bottom+1) / texture.height);
            aspect[i] = (float)(right-left+1) / (top-bottom+1);
        }
    }
    private void Actor(int index, Rect area)
    {
        float width = area.height * aspect[index];
        draw(actors[index], new Rect(area.center.x-width/2, area.y, width, area.height), uv[index], Color.white);
    }
    public void Draw(SpiritAttackAnimation animation, AbilityDefinition skill, float time, float breakAt = float.PositiveInfinity)
    {
        Rect arena = new Rect(20, 20, 1240, 396);
        if (background != null) draw(background, arena, new Rect(0,0,1,1), Color.white);
        bool active = time < SpiritAttackTimeline.Duration(skill);
        if (active) animation.DrawBackdrop(arena, skill, time);
        if (active) animation.Draw(caster, skill, time, Color.white); else Actor(0, caster);
        int selected = SpiritAttackTimeline.Skill(skill) % 3;
        var targets = skill.Targets == TargetRule.AllEnemies ? heroes : new[] { heroes[selected] };
        for (int hero = 0; hero < heroes.Length; hero++)
        {
            Rect home = heroes[hero];
            if (active && (skill.Targets == TargetRule.AllEnemies || hero == selected)) home.position += SpiritAttackAnimation.TargetOffset(skill, time);
            Actor(hero+1, home);
            if (SpiritAttackTimeline.Skill(skill) == 3 && hero == selected && time >= SpiritAttackTimeline.Impact(skill))
                chainPrisons.Draw(home, time < breakAt, time, time - breakAt);
        }
        if (active) animation.DrawEffects(caster, targets, skill, time);
    }
}

internal sealed class SpiritBossPreview : EditorWindow
{
    private SpiritAttackAnimation animation;
    private SpiritBossPreviewScene scene;
    private int selected;
    private double started;
    private void OnEnable() { started = EditorApplication.timeSinceStartup; EditorApplication.update += Repaint; }
    private void OnDisable() { EditorApplication.update -= Repaint; }
    private static void DrawTexture(Texture2D texture, Rect rect, Rect uv, Color tint)
    {
        Color previous = GUI.color; GUI.color = tint; GUI.DrawTextureWithTexCoords(rect, texture, uv, true); GUI.color = previous;
    }
    private void OnGUI()
    {
        int choice = GUILayout.Toolbar(selected, new[] { "Mind Control", "Silence", "Sugar Vida", "Correntes", "Caos Magic" });
        if (choice != selected) { selected = choice; started = EditorApplication.timeSinceStartup; }
        if (animation == null) { animation = new SpiritAttackAnimation(); animation.Prepare(); scene = new SpiritBossPreviewScene(DrawTexture); }
        var skill = SpiritAttackValidation.Skills[selected];
        float time = (float)((EditorApplication.timeSinceStartup-started) % (SpiritAttackTimeline.Duration(skill)+.8f));
        Matrix4x4 previous = GUI.matrix;
        float scale = position.width/1280;
        GUI.matrix = Matrix4x4.TRS(new Vector3(0,28,0), Quaternion.identity, new Vector3(scale,scale,1));
        Vector2 shake = SpiritAttackAnimation.ScreenOffset(skill,time);
        GUI.matrix *= Matrix4x4.Translate(new Vector3(shake.x,shake.y,0));
        scene.Draw(animation,skill,time); GUI.matrix = previous;
    }
}
