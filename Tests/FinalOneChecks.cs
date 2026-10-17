using System;
using DragonQuest.Prototype;

public static class FinalOneChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .003f;
    private static CastleDestroyedSequence At(float time, int lines = 22)
    { var sequence = new CastleDestroyedSequence(lines); sequence.Advance(time); return sequence; }

    public static int Main()
    {
        Check(Near(At(0).BlackOpacity, 1), "The aftermath starts under black.");
        Check(CastleDestroyedSequence.LandStartX + CastleDestroyedSequence.LandWidth < 0,
            "The hillside and its seated figure begin outside the picture.");
        float arrivalEnd = CastleDestroyedSequence.ArrivalEnd;
        Check(Near(At(arrivalEnd).LandX, CastleDestroyedSequence.LandStopX), "Foreground reaches its mark.");
        Check(CastleDestroyedSequence.TextStart > arrivalEnd && CastleDestroyedSequence.TextStart - arrivalEnd < 1,
            "The story follows the seated composition's arrival without waiting for a walk.");
        Check(At(arrivalEnd).TextOffset >= CastleDestroyedSequence.TextWindowHeight,
            "The text stays below its window during the hill's entrance.");
        var lastLine = At(At(0).TextEndAt - .5f);
        Check(lastLine.TextOffset + lastLine.LineCount * CastleDestroyedSequence.LineHeight > 0
            && Near(lastLine.BlackOpacity, 0), "No fade while the last line is still crossing the window.");
        var textEnd = At(At(0).TextEndAt);
        Check(textEnd.TextOffset + textEnd.LineCount * CastleDestroyedSequence.LineHeight <= .003f,
            "All text clears the window before fading.");
        Check(Near(textEnd.BlackOpacity, 0), "The ruins remain visible for a pause after the text.");
        Check(Near(textEnd.CompleteAt - textEnd.TextEndAt,
            CastleDestroyedSequence.EndHold + CastleDestroyedSequence.FadeOutDuration + CastleDestroyedSequence.BlackHold),
            "After the final line, the seated scene holds and fades without a walking departure.");
        Check(Near(At(textEnd.FadeOutAt + CastleDestroyedSequence.FadeOutDuration / 2).BlackOpacity, .5f),
            "The final fade is gradual.");
        var black = At(textEnd.BlackAt);
        Check(Near(black.BlackOpacity, 1) && !black.IsComplete, "Black holds before returning to the menu.");
        float previousX = CastleDestroyedSequence.LandStartX;
        float previousBackground = 0;
        for (float t = 0; t < textEnd.CompleteAt; t += .1f)
        {
            var frame = At(t);
            Check(frame.LandX >= previousX - .003f && frame.LandX <= CastleDestroyedSequence.LandStopX + CastleDestroyedSequence.LandDrift,
                "The entire painted hillside keeps moving slowly right without separating the figure from the ground.");
            Check(frame.BackgroundX <= previousBackground && frame.BackgroundX >= -CastleDestroyedSequence.BackgroundDrift,
                "The background moves left, opposite the hill, within its overscan margin.");
            if (t > arrivalEnd + .1f)
            {
                Check(frame.LandX > previousX && frame.LandX - previousX < .1f,
                    "Steady foreground drift stays below one reference pixel per second.");
                Check(previousBackground - frame.BackgroundX < .13f,
                    "Background drift stays below 1.3 reference pixels per second.");
            }
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1, "Fade opacity remains bounded.");
            previousX = frame.LandX;
            previousBackground = frame.BackgroundX;
        }
        Check(At(0, 44).TextEndAt > At(0, 22).TextEndAt, "Longer narrative extends the scene before fading.");
        var complete = At(10000);
        Check(complete.IsComplete && Near(complete.BlackOpacity, 1), "A long frame finishes safely at black.");
        complete.Advance(10);
        Check(Near(complete.Elapsed, complete.CompleteAt), "Completion is stable.");
        var fixedStep = At(0);
        for (int i = 0; i < 1200; i++) fixedStep.Advance(1f / 60);
        Check(Near(fixedStep.Elapsed, At(20).Elapsed) && Near(fixedStep.LandX, At(20).LandX),
            "Timing is independent of frame rate and uses the supplied unscaled clock.");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { At(0).Advance(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid time steps are rejected.");
        }
        Console.WriteLine("FINAL_ONE_OK: " + checks + " seated composition, opposing drift, complete narrative and fade checks.");
        return 0;
    }
}
