using DragonQuest.Presentation.Cinematics.Endings;
using System;

public static class HeroPossessedChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a-b)<.003f;
    private static HeroPossessedSequence At(float time, int lines = 31, int seed = HeroPossessedSequence.DefaultStormSeed)
    { var sequence = new HeroPossessedSequence(lines,seed); sequence.Advance(time); return sequence; }
    public static int Main()
    {
        Check(Near(At(0).BlackOpacity,1), "The carpet starts under the standard black fade.");
        Check(Near(At(HeroPossessedSequence.FadeInDuration).BlackOpacity,0), "The opening fade finishes.");
        Check(HeroPossessedSequence.TextStart < HeroPossessedSequence.CameraStopAt,
            "Narration begins while the camera is still travelling.");
        foreach (float join in new[] { HeroPossessedSequence.CameraStart + HeroPossessedSequence.CarpetDuration,
            HeroPossessedSequence.CameraStart + HeroPossessedSequence.CarpetDuration + HeroPossessedSequence.StairDuration })
        {
            const float step = 1f/60;
            float before = (At(join).CameraFocusY - At(join-step).CameraFocusY)/step;
            float after = (At(join+step).CameraFocusY - At(join).CameraFocusY)/step;
            Check(before < -.015f && after < -.015f && Math.Abs(before-after) < .001f,
                "Camera velocity stays continuous and nonzero between carpet, stairs and reveal.");
        }
        float previous = .84f;
        for (float t = 0; t < At(0).CompleteAt; t += .025f)
        {
            var frame = At(t);
            Check(frame.CameraFocusY <= previous+.0001f && frame.CameraFocusY >= HeroPossessedSequence.FinalCameraFocusY-.0001f,
                "The camera travels upward from carpet to steps to the king, without reversing.");
            if (frame.CameraStopped) Check(Near(frame.CameraFocusY,HeroPossessedSequence.FinalCameraFocusY) && Near(frame.CameraImageWidth,1040),
                "The throne remains centered and the camera locks for all lightning.");
            else Check(frame.Lightning == 0, "The apparition waits for the stopped camera.");
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1 && frame.Lightning >= 0 && frame.Lightning <= 1,
                "Fade and lightning remain bounded.");
            Check(frame.Lightning > 0 ? frame.LightningFrame >= 0 && frame.LightningFrame < HeroPossessedSequence.LightningFrameCount : frame.LightningFrame == -1,
                "Only a visible flash selects one of the four sprite frames.");
            previous = frame.CameraFocusY;
        }
        float strike = At(0).StormFlashPeakAt(0);
        Check(At(strike).Lightning>.99f, "Lightning reveals the naturally colored spirit after the camera stops.");
        VerifyStorm();
        Check(HeroPossessedSequence.FinalCameraFocusY < .322f-.1f,
            "The final camera rises well above the previous throne framing to show the ghost.");
        var finalText = At(At(0).TextEndAt-.2f);
        Check(finalText.TextOffset+finalText.LineCount*HeroPossessedSequence.LineHeight>0 && finalText.BlackOpacity==0,
            "The last line is visible before the final fade.");
        var finishedText = At(At(0).TextEndAt);
        Check(finishedText.TextOffset+finishedText.LineCount*HeroPossessedSequence.LineHeight<=.003f
            && finishedText.FadeOutAt >= finishedText.TextEndAt+HeroPossessedSequence.EndHold,
            "All narration clears the column before the closing hold and fade.");
        var shortText = At(0,1);
        Check(shortText.FadeOutAt>HeroPossessedSequence.CameraStopAt+8.5f, "Short copy cannot skip the throne or its second strike.");
        Check(At(0,60).FadeOutAt>At(0,31).FadeOutAt, "Longer narration extends the ending.");
        Check(Near(At(finishedText.FadeOutAt+HeroPossessedSequence.FadeOutDuration/2).BlackOpacity,.5f),
            "Closing fade matches the first ending.");
        Check(Near(At(finishedText.BlackAt).BlackOpacity,1) && !At(finishedText.BlackAt).IsComplete,
            "Black holds before the automatic return to menu.");
        var done = At(10000);
        Check(done.IsComplete && done.BlackOpacity==1 && done.Lightning==0 && done.LightningFrame==-1, "Large frames finish safely at black.");
        done.Advance(10); Check(Near(done.Elapsed,done.CompleteAt), "Completion is stable.");
        var fine = At(0);
        for (int i=0;i<1200;i++) fine.Advance(1f/60);
        Check(Near(fine.CameraFocusY,At(20).CameraFocusY), "Camera timing is independent of frame rate.");
        foreach(float invalid in new[]{-1f,float.NaN,float.PositiveInfinity})
        {
            bool rejected=false;
            try { At(0).Advance(invalid); } catch(ArgumentOutOfRangeException) { rejected=true; }
            Check(rejected,"Invalid time steps are rejected.");
        }
        Console.WriteLine("HERO_POSSESSED_OK: "+checks+" storm timing, seeded positions, four frames, camera, narrative and fade checks.");
        return 0;
    }

    private static void VerifyStorm()
    {
        int frames=0,burstFlashes=0;
        bool left=false,right=false,shortFlash=false,longFlash=false,mirrored=false,normal=false;
        var schedule=At(0,80);
        HeroPossessedSequence.StormLight previous=HeroPossessedSequence.StormLight.Dark;
        Check(schedule.StormFlashCount>12,"The storm contains varied bursts through a longer narrative.");
        for(int i=0;i<schedule.StormFlashCount;i++)
        {
            float peak=schedule.StormFlashPeakAt(i);
            var sample=At(peak,80).LightningState;
            Check(sample.Intensity>=.54f && sample.Frame>=0 && sample.Frame<4,"Every scheduled flash is visible at its peak.");
            float duration=schedule.StormFlashEndAt(i)-schedule.StormFlashStartAt(i);
            Check(duration>=0 && duration<=1.0001f,"Each flash has a random duration from zero to one second.");
            shortFlash|=duration<.2f;longFlash|=duration>.7f;
            float radians=sample.Angle*(float)Math.PI/180;
            float extentX=(Math.Abs((float)Math.Cos(radians))*sample.Width+Math.Abs((float)Math.Sin(radians))*sample.Height/(2f/3))/2;
            float extentY=(Math.Abs((float)Math.Sin(radians))*sample.Width*(2f/3)+Math.Abs((float)Math.Cos(radians))*sample.Height)/2;
            Check(sample.X-extentX>=.1799f && sample.X+extentX<=.8401f
                && Near(sample.Y+extentY,HeroPossessedSequence.LightningTopY)
                && Near(sample.Width,HeroPossessedSequence.LightningWidth) && sample.Angle==0,
                "The bolt's root, size and angle remain fixed at the top; only horizontal position changes.");
            if(sample.Burst==previous.Burst)
            {
                Check(peak-schedule.StormFlashPeakAt(i-1)<1.1f,"Successive flashes honor their randomized duration and dark gaps.");
                Check(sample.Frame==(previous.Frame+1)%4,"The four shapes alternate within a burst.");
                burstFlashes++;
            }
            else
            {
                if(i>0)
                {
                    Check(burstFlashes>=3 && burstFlashes<=5,"Bursts have three to five irregular flashes.");
                }
                burstFlashes=1;
            }
            if(i>0)
            {
                float dx=sample.X-previous.X;
                Check(Math.Abs(dx)>.18f && sample.Y==previous.Y && sample.Height==previous.Height,
                    "Every flash jumps horizontally without moving its top or its vertical extent.");
            }
            if(i+1<schedule.StormFlashCount)
            {
                float gap=(schedule.StormFlashEndAt(i)+schedule.StormFlashStartAt(i+1))/2;
                Check(At(gap,80).Lightning==0 && At(gap,80).LightningFrame==-1,
                    "Dark gaps hide both the spirit and the ray, including inside rapid bursts.");
            }
            frames|=1<<sample.Frame; left|=sample.X<.45f;right|=sample.X>.55f;
            mirrored|=sample.Mirrored; normal|=!sample.Mirrored; previous=sample;
        }
        Check(frames==15 && left && right && shortFlash && longFlash && mirrored && normal,
            "The storm uses both sides, all four forms, mirrored variants and distinctly short and long flashes.");
        var same=At(schedule.StormFlashPeakAt(0),80).LightningState;
        var different=At(schedule.StormFlashPeakAt(0),80,27).LightningState;
        Check(same.X!=different.X || same.Y!=different.Y,"Different seeds change each presentation's storm.");
        float target=schedule.StormFlashPeakAt(0);
        var stepped=At(0,80);
        while(stepped.Elapsed<target-.01f) stepped.Advance(.01f);
        stepped.Advance(target-stepped.Elapsed);
        Check(stepped.LightningFrame==same.Frame && Near(stepped.LightningState.X,same.X)
            && Near(stepped.Lightning,same.Intensity),"Positions, forms and timing depend on elapsed time, not FPS.");
        foreach(int seed in new[]{0,-1,int.MinValue})
            Check(At(At(0,31,seed).StormFlashPeakAt(0),31,seed).Lightning>.99f,"All signed seeds produce a working storm.");
    }
}
