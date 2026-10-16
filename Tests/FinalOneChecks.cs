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
            "The cliff begins entirely outside the picture.");
        float arrivalEnd = CastleDestroyedSequence.ArrivalEnd;
        Check(Near(At(arrivalEnd).LandX, CastleDestroyedSequence.LandStopX), "Foreground reaches its mark.");
        Check(!At(arrivalEnd).ElderVisible && CastleDestroyedSequence.ElderEntranceStart > arrivalEnd,
            "The old man waits off screen until the cliff has arrived.");
        var entrance = At(CastleDestroyedSequence.ElderEntranceStart);
        Check(entrance.ElderWalking && entrance.ElderFrame < 4
            && entrance.ElderX + CastleDestroyedSequence.ElderWidth / 2 < 0,
            "The old man begins walking right from outside the frame.");
        var entering = At(CastleDestroyedSequence.ElderEntranceStart + CastleDestroyedSequence.ElderWalkDuration / 2);
        Check(entering.ElderX > 0 && entering.ElderX < At(CastleDestroyedSequence.ElderAtCastleAt).ElderX,
            "The old man walks into the frame on the cliff after its arrival.");
        var looking = At(CastleDestroyedSequence.ElderAtCastleAt);
        Check(looking.ElderFacingCastle && !looking.ElderWalking && looking.ElderFrame >= 4 && looking.ElderFrame < 8,
            "He stops in a rear-facing pose toward the distant castle.");
        Check(looking.ElderLocalX > 0 && looking.ElderLocalX + CastleDestroyedSequence.ElderWidth / 2 < CastleDestroyedSequence.LandWidth,
            "The watching character stays well inside the cliff edge.");
        Check(CastleDestroyedSequence.TextStart > CastleDestroyedSequence.ElderAtCastleAt,
            "The story starts after the old man has reached his place.");
        Check(At(CastleDestroyedSequence.ElderEntranceStart + .2f).ElderFrame
            != At(CastleDestroyedSequence.ElderEntranceStart + .4f).ElderFrame, "Walking changes the leg and arm pose.");
        var lastLine = At(At(0).TextEndAt - .5f);
        Check(lastLine.TextOffset + lastLine.LineCount * CastleDestroyedSequence.LineHeight > 0
            && Near(lastLine.BlackOpacity, 0), "No fade while the last line is still crossing the window.");
        var textEnd = At(At(0).TextEndAt);
        Check(textEnd.TextOffset + textEnd.LineCount * CastleDestroyedSequence.LineHeight <= .003f,
            "All text clears the window before fading.");
        Check(Near(textEnd.BlackOpacity, 0), "The ruins remain visible for a pause after the text.");
        var leaving = At(textEnd.ElderExitAt + CastleDestroyedSequence.ElderWalkDuration / 2);
        Check(leaving.ElderWalking && leaving.ElderFrame >= 8 && leaving.ElderX < looking.ElderX,
            "After the narrative he walks back toward the left edge.");
        var gone = At(textEnd.ElderExitEnd);
        Check(!gone.ElderVisible && gone.ElderX + CastleDestroyedSequence.ElderWidth / 2 < 0
            && Near(gone.BlackOpacity, 0), "He leaves the frame completely before the fade starts.");
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
                "The cliff keeps moving slowly right after its approach.");
            Check(frame.BackgroundX <= previousBackground && frame.BackgroundX >= -CastleDestroyedSequence.BackgroundDrift,
                "The background moves left, opposite the cliff, within its overscan margin.");
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
        Console.WriteLine("FINAL_ONE_OK: " + checks + " opposing drift, delayed walk, rear pose, departure, narrative and fade checks.");
        return 0;
    }
}
