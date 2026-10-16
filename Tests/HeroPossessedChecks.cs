using System;
using DragonQuest.Prototype;

public static class HeroPossessedChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a-b)<.003f;
    private static HeroPossessedSequence At(float time, int lines = 31)
    { var sequence = new HeroPossessedSequence(lines); sequence.Advance(time); return sequence; }
    public static int Main()
    {
        Check(Near(At(0).BlackOpacity,1), "The carpet starts under the standard black fade.");
        Check(Near(At(HeroPossessedSequence.FadeInDuration).BlackOpacity,0), "The opening fade finishes.");
        Check(HeroPossessedSequence.TextStart < HeroPossessedSequence.CameraStopAt,
            "Narration begins while the camera is still travelling.");
        float previous = .84f;
        for (float t = 0; t < At(0).CompleteAt; t += .025f)
        {
            var frame = At(t);
            Check(frame.CameraFocusY <= previous+.0001f && frame.CameraFocusY >= .322f-.0001f,
                "The camera travels upward from carpet to steps to the king, without reversing.");
            if (frame.CameraStopped) Check(Near(frame.CameraFocusY,.322f) && Near(frame.CameraImageWidth,1040),
                "The throne remains centered and the camera locks for all lightning.");
            else Check(frame.Lightning == 0, "The apparition waits for the stopped camera.");
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1 && frame.Lightning >= 0 && frame.Lightning <= 1,
                "Fade and lightning remain bounded.");
            previous = frame.CameraFocusY;
        }
        float strike = HeroPossessedSequence.FirstLightningAt;
        Check(At(strike+.08f).Lightning>.99f, "Lightning reveals the spirit after the camera stops.");
        Check(At(strike+1).Lightning==0 && At(strike+5.58f).Lightning>.99f,
            "The apparition disappears between intermittent strikes.");
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
        Check(done.IsComplete && done.BlackOpacity==1 && done.Lightning==0, "Large frames finish safely at black.");
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
        Console.WriteLine("HERO_POSSESSED_OK: "+checks+" camera, narration, apparition, fade and menu-return checks.");
        return 0;
    }
}
