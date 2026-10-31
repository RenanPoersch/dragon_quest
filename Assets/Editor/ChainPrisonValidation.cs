using DragonQuest.Presentation.Battle.Animations;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChainPrisonValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Chain Prison")]
    public static void Run()
    {
        int draws = 0;
        var prison = new ChainPrisonAnimation((texture, destination, uv, tint) =>
        {
            draws++;
            if (uv.x < 0 || uv.y < 0 || uv.xMax > 1 || uv.yMax > 1 || destination.width <= 0)
                throw new InvalidOperationException("Chain prison frame is out of bounds.");
        });
        var source = prison.Prepare();
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source));
        if (!importer.isReadable || importer.mipmapEnabled || source.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency)
            throw new InvalidOperationException("Chain prison must preserve native pixels and transparent alpha.");
        var original = new Texture2D(2,2);
        try
        {
            original.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
            if (source.width != original.width || source.height != original.height)
                throw new InvalidOperationException("Chain prison atlas resized at import.");
        }
        finally { UnityEngine.Object.DestroyImmediate(original); }
        int transparent = 0;
        foreach (var pixel in source.GetPixels32()) if (pixel.a == 0) transparent++;
        if (transparent < source.width * source.height / 5) throw new InvalidOperationException("Missing transparent gaps around chains.");
        var hero = new Rect(925,125,64,90);
        prison.Draw(hero, false, 0);
        if (draws != 0) throw new InvalidOperationException("Prison drawn on free hero.");
        for (int frame = 0; frame < 8; frame++) prison.Draw(hero, true, frame * .2f);
        prison.Draw(hero, true, 120);
        if (draws != 9) throw new InvalidOperationException("Persistent prison expired with time.");
        for (int frame = 0; frame < 4; frame++) prison.Draw(hero, false, 0, frame * .15f);
        if (draws != 13) throw new InvalidOperationException("Missing prison break frames.");
        prison.Draw(hero, false, 0, ChainPrisonAnimation.BreakDuration);
        if (draws != 13) throw new InvalidOperationException("Broken chains never disappear.");
        Debug.Log("CHAIN_PRISON_VISUAL_OK: native transparent atlas, persistent idle and four impact-break frames.");
    }

    [MenuItem("Tools/Dragon Quest/Export Chain Prison Preview")]
    public static void Capture()
    {
        Run();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ChainPrisonPreview"));
        Directory.CreateDirectory(output);
        var material = new Material(Shader.Find("Sprites/Default"));
        var target = new RenderTexture(1280,416,0,RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1280,416,TextureFormat.RGBA32,false);
        var previous = RenderTexture.active;
        Action<Texture2D,Rect,Rect,Color> draw = (texture, rect, uv, tint) =>
        {
            material.mainTexture = texture; material.SetPass(0);
            GL.Begin(GL.QUADS); GL.Color(tint);
            GL.TexCoord2(uv.x,uv.yMax); GL.Vertex3(rect.x,rect.y,0);
            GL.TexCoord2(uv.xMax,uv.yMax); GL.Vertex3(rect.xMax,rect.y,0);
            GL.TexCoord2(uv.xMax,uv.y); GL.Vertex3(rect.xMax,rect.yMax,0);
            GL.TexCoord2(uv.x,uv.y); GL.Vertex3(rect.x,rect.yMax,0); GL.End();
        };
        var animation = new SpiritAttackAnimation(draw); animation.Prepare();
        var scene = new SpiritBossPreviewScene(draw);
        try
        {
            target.Create(); RenderTexture.active = target;
            for (int frame = 0; frame < 160; frame++)
            {
                float time = frame / 20f;
                GL.Clear(true,true,new Color(.035f,.025f,.065f,1));
                GL.PushMatrix(); GL.LoadPixelMatrix(0,1280,416,0);
                scene.Draw(animation, SpiritAttackValidation.Skills[3], time, 5.6f);
                GL.PopMatrix();
                pixels.ReadPixels(new Rect(0,0,1280,416),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(output,"frame"+frame.ToString("D4")+".png"),pixels.EncodeToPNG());
            }
            Debug.Log("CHAIN_PRISON_PREVIEW_OK: 160 frames, cast, persistent hold and impact break. " + output);
        }
        finally
        {
            RenderTexture.active = previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
