using DragonQuest.Presentation.Cinematics.Endings;
using System;
using System.IO;
using DragonQuest.Progression;
using UnityEditor;
using UnityEngine;

public static class EndingMotionValidation
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new InvalidOperationException("Ending motion: " + message); }

    [MenuItem("Tools/Dragon Quest/Validate Ending Motion")]
    public static void Run()
    {
        Shader shader = Resources.Load<Shader>(EndingArtworkRenderer.ShaderResource);
        Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "motion shader compiles on the GPU");
        CheckSampling();
        foreach (int fps in new[] { 30, 60, 144 })
        {
            CheckMotion("castle", CastleDestroyedPresentation.BackgroundResource, seconds =>
            {
                var sequence = new CastleDestroyedSequence(EndingNarrative.CastleDestroyedEpilogue.Split('\n').Length);
                sequence.Advance(seconds);
                var texture = Resources.Load<Texture2D>(CastleDestroyedPresentation.BackgroundResource);
                float width = 1280 + CastleDestroyedSequence.BackgroundDrift + 24;
                float height = width * texture.height / texture.width;
                return new Rect(-12 + sequence.BackgroundX, (660-height)/2, width, height);
            }, 24, fps);
            var king = new KingFreedSequence(EndingNarrative.KingFreedSpeech.Split('\n').Length,
                EndingNarrative.KingFreedEpilogue.Split('\n').Length);
            foreach (bool prosperity in new[] { false, true })
            {
                string resource = prosperity ? KingFreedPresentation.ProsperityResource : KingFreedPresentation.SpeechResource;
                var texture = Resources.Load<Texture2D>(resource);
                CheckMotion(prosperity ? "prosperity" : "speech", resource, seconds =>
                {
                    var sequence = new KingFreedSequence(king.SpeechLineCount, king.EpilogueLineCount);
                    sequence.Advance(seconds);
                    return KingFreedPresentation.ArtBounds(prosperity ? sequence.ProsperityProgress : sequence.SpeechProgress,
                        texture.width/(float)texture.height, prosperity);
                }, prosperity ? king.EpilogueTextStart + 8 : 10, fps);
            }
            CheckMotion("possession", HeroPossessedPresentation.ThroneResource, seconds =>
            {
                var sequence = new HeroPossessedSequence(31);
                sequence.Advance(seconds);
                var texture = Resources.Load<Texture2D>(HeroPossessedPresentation.ThroneResource);
                return HeroPossessedPresentation.CameraBounds(sequence, texture.width/(float)texture.height);
            }, HeroPossessedSequence.CameraStart + HeroPossessedSequence.CarpetDuration - 3f/fps, fps);
        }
        // All original composition, narrative, weather and transition checks remain applicable.
        FinalOneValidation.Run(); KingFreedValidation.Run(); HeroPossessedValidation.Run(); CastleImpactValidation.Run();
        Debug.Log("ENDING_MOTION_OK: " + checks + " GPU checks; every frame changes at 30/60/144 fps, fractional sampling, framing, alpha and original endings.");
    }

    public static void RunBatch()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void CheckSampling()
    {
        var artwork = new Texture2D(8,8,TextureFormat.RGBA32,false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var readback = new Texture2D(8,8,TextureFormat.RGBA32,false);
        try
        {
            for (int y=0;y<8;y++) for (int x=0;x<8;x++)
                artwork.SetPixel(x,y,y<4 ? Color.blue : Color.red);
            artwork.Apply(false,true); // Match the game's non-readable Resources.
            using (var renderer = new EndingArtworkRenderer())
            {
                var viewport = new Rect(0,0,8,8);
                RenderTexture previous = RenderTexture.active;
                var frame = renderer.RenderFrame(artwork,viewport,viewport);
                Check(RenderTexture.active == previous, "render target is restored");
                Read(frame,readback);
                Check(readback.GetPixel(3,0).b>.9f && readback.GetPixel(3,7).r>.9f, "top and bottom keep the source orientation");
                Color before = readback.GetPixel(3,3);
                var next = renderer.RenderFrame(artwork,new Rect(0,.25f,8,8),viewport);
                Check(ReferenceEquals(frame,next), "consecutive frames reuse their GPU surface");
                Read(next,readback);
                Color after = readback.GetPixel(3,3);
                Check(after.r > before.r+.1f && after.b < before.b-.1f, "a quarter pixel pan blends adjacent texels without a whole pixel jump");
                Read(renderer.RenderFrame(artwork,new Rect(2,0,4,8),viewport),readback);
                Check(readback.GetPixel(0,4).a==0 && readback.GetPixel(3,4).a>.9f,
                    "narrow throne framing leaves transparent margins instead of stretching edge texels");
                Check(artwork.filterMode==FilterMode.Point && !artwork.isReadable, "shared artwork stays Point filtered and non-readable");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(artwork); UnityEngine.Object.DestroyImmediate(readback); }
    }

    private static void CheckMotion(string name, string resource, Func<float,Rect> boundsAt, float start, int fps)
    {
        var artwork = Resources.Load<Texture2D>(resource);
        var viewport = new Rect(0,0,1280,660);
        var readback = new Texture2D(640,330,TextureFormat.RGBA32,false);
        Check(artwork!=null && !artwork.isReadable && artwork.filterMode==FilterMode.Point, "native source: " + name);
        try
        {
            using (var renderer = new EndingArtworkRenderer())
            {
                Color32[] previous = null;
                for (int i=0;i<10;i++)
                {
                    Read(renderer.RenderFrame(artwork,boundsAt(start+i/(float)fps),viewport,.5f),readback);
                    Color32[] pixels = readback.GetPixels32();
                    if (previous!=null)
                    {
                        int changed=0;
                        for (int p=0;p<pixels.Length;p++) if (!pixels[p].Equals(previous[p])) changed++;
                        Check(changed>10, name+" moves on every frame at "+fps+" fps: "+i+" (changed "+changed+")");
                    }
                    previous=pixels;
                }
                if (fps==60)
                {
                    string[] args = Environment.GetCommandLineArgs();
                    int index = Array.IndexOf(args,"-endingMotionCaptureDir");
                    if (index>=0 && index+1<args.Length)
                    {
                        string directory = Path.GetFullPath(args[index+1]); Directory.CreateDirectory(directory);
                        File.WriteAllBytes(Path.Combine(directory,name+".png"),readback.EncodeToPNG());
                    }
                }
                Check(artwork.filterMode==FilterMode.Point, "source settings unchanged: "+name);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(readback); }
    }

    private static void Read(RenderTexture frame, Texture2D readback)
    {
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active=frame;
            readback.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0); readback.Apply();
        }
        finally { RenderTexture.active=previous; }
    }
}
