using System;
using DragonQuest.Prototype;

public static class KingFreedChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .005f;
    private static KingFreedSequence At(float seconds, int speech = 25, int epilogue = 21)
    { var sequence = new KingFreedSequence(speech, epilogue); sequence.Advance(seconds); return sequence; }

    public static int Main()
    {
        var start = At(0);
        Check(start.BlackOpacity == 1 && start.ProsperityOpacity == 0, "The ending starts at black on the speech image.");
        Check(Near(At(KingFreedSequence.FadeInDuration / 2).BlackOpacity, .5f), "The opening fade is gradual.");
        var speechEnd = At(start.SpeechTextEndAt);
        Check(speechEnd.SpeechTextOffset + speechEnd.SpeechLineCount * KingFreedSequence.LineHeight <= .005f,
            "The last speech line leaves its window before the image changes.");
        Check(speechEnd.BlackOpacity == 0 && speechEnd.ProsperityOpacity == 0, "There is time to contemplate the speech image.");
        Check(Near(At(start.DissolveAt + KingFreedSequence.DissolveDuration / 2).ProsperityOpacity, .5f), "The two paintings dissolve smoothly.");
        var epilogueStart = At(start.EpilogueTextStart);
        Check(epilogueStart.ProsperityOpacity == 1 && epilogueStart.SpeechTextOpacity == 0,
            "The epilogue has its own uninterrupted time on the prosperity image.");
        var textEnd = At(start.TextEndAt);
        Check(textEnd.EpilogueTextOffset + textEnd.EpilogueLineCount * KingFreedSequence.LineHeight <= .005f
            && textEnd.BlackOpacity == 0, "All epilogue lines clear the window before the final fade.");
        Check(Near(At(start.FadeOutAt + KingFreedSequence.FadeOutDuration / 2).BlackOpacity, .5f), "The final fade is gradual.");
        Check(At(start.BlackAt).BlackOpacity == 1 && !At(start.BlackAt).IsComplete, "Black holds before the menu.");
        Check(At(start.CompleteAt).IsComplete, "The menu may return after the black hold.");
        Check(At(0, 50).DissolveAt > start.DissolveAt && At(0, 50).TextEndAt > start.TextEndAt,
            "A longer speech delays prosperity and the final fade together.");
        Check(Near(At(0, 25, 42).DissolveAt, start.DissolveAt) && At(0, 25, 42).TextEndAt > start.TextEndAt,
            "A longer epilogue extends the second shot without changing the speech.");
        Check(At(1000, 1, 1).IsComplete, "Very short copy also reaches the menu.");
        float previousProsperity = 0, previousSpeechCamera = 0, previousProsperityCamera = 0;
        for (float t = 0; t < start.CompleteAt; t += .1f)
        {
            var frame = At(t);
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1 && frame.ProsperityOpacity >= previousProsperity,
                "The fade is bounded and prosperity never reverses to the speech.");
            Check(frame.SpeechProgress >= previousSpeechCamera && frame.SpeechProgress <= 1
                && frame.ProsperityProgress >= previousProsperityCamera && frame.ProsperityProgress <= 1,
                "Both slow camera movements stay continuous and bounded.");
            Check(frame.SpeechTextOpacity == 0 || frame.EpilogueTextOpacity == 0, "The two narrative blocks never overlap.");
            previousProsperity = frame.ProsperityOpacity;
            previousSpeechCamera = frame.SpeechProgress; previousProsperityCamera = frame.ProsperityProgress;
        }
        var fixedStep = At(0);
        for (int i = 0; i < 1800; i++) fixedStep.Advance(1f / 60);
        Check(Near(fixedStep.Elapsed, At(30).Elapsed) && Math.Abs(fixedStep.SpeechTextOffset - At(30).SpeechTextOffset) < .03f,
            "A long frame and sixty frames per second produce the same reading time.");
        var complete = At(float.MaxValue);
        complete.Advance(10);
        Check(complete.IsComplete && complete.BlackOpacity == 1 && complete.Elapsed == complete.CompleteAt, "Completion is stable at black.");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { At(0).Advance(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid time steps cannot corrupt the sequence.");
        }
        foreach (var counts in new[] { new[] { 0, 1 }, new[] { 1, 0 }, new[] { -1, 2 } })
        {
            bool rejected = false;
            try { new KingFreedSequence(counts[0], counts[1]); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Each shot requires narrative lines.");
        }
        Console.WriteLine("GOOD_ENDING_OK: " + checks + " complete speech, prosperity transition, reading time and fade checks.");
        return 0;
    }
}
