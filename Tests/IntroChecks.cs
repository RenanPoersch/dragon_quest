using System;
using DragonQuest.Prototype;

public static class IntroChecks
{
    private static int checks;
    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .001f;

    public static int Main()
    {
        var intro = new IntroSequence();
        Check(intro.IsPlaying && Near(intro.Opacity, 0) && intro.VisibleLetters == 0, "Opening starts black with no title.");
        Check(!intro.TrySkip(), "The input that launched the scene cannot dismiss the intro.");
        intro.Advance(.9f);
        Check(Near(intro.Opacity, .5f), "The landscape fades in gradually.");
        Check(intro.VisibleLetters == 0, "The landscape is revealed before lettering.");
        intro.Advance(.9f);
        Check(Near(intro.Opacity, 1), "The landscape reaches full opacity.");
        Check(intro.VisibleLetters > 0 && intro.VisibleLetters < IntroSequence.Title.Length, "Title writes letter by letter.");
        intro.Advance(1.5f);
        Check(intro.VisibleLetters == IntroSequence.Title.Length && Near(intro.TitleOpacity, 1), "The complete game name stays visible.");
        intro.Advance(4.5f);
        Check(intro.IsPlaying && intro.IsExiting && Near(intro.Opacity, 1), "Automatic transition starts after the title hold.");
        intro.Advance(IntroSequence.FadeOutDuration / 2);
        Check(Near(intro.Opacity, .5f), "Automatic transition fades out before exposing the menu.");
        intro.Advance(IntroSequence.FadeOutDuration / 2 + .001f);
        Check(!intro.IsPlaying && Near(intro.Opacity, 0), "Menu is released only after fade-out completes.");

        foreach (float skipAt in new[] { .36f, 1.1f, 4f, 7.7f })
        {
            var skip = new IntroSequence();
            skip.Advance(skipAt);
            float opacity = skip.Opacity;
            Check(skip.TrySkip(), "Skip works during fade-in, lettering and hold.");
            Check(Near(skip.Opacity, opacity), "Skipping never flashes the background to full brightness.");
            Check(!skip.TrySkip(), "Repeated input does not restart fade-out.");
            skip.Advance(IntroSequence.FadeOutDuration + .001f);
            Check(!skip.IsPlaying && Near(skip.Opacity, 0), "Skip reliably releases the menu.");
        }
        var paused = new IntroSequence();
        paused.Advance(0);
        Check(Near(paused.Elapsed, 0), "A zero delta is stable.");
        paused.Advance(100);
        Check(!paused.IsPlaying && paused.VisibleLetters == IntroSequence.Title.Length, "A slow frame cannot leave the menu blocked.");
        Console.WriteLine("INTRO_CHECKS_OK: " + checks + " timing, title reveal, automatic transition and skip checks.");
        return 0;
    }
}
