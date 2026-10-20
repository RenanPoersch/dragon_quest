using System;
using DragonQuest.Prototype;

public static class EndingTransitionChecks
{
    private static int checks;
    private static void Check(bool value, string reason)
    { checks++; if (!value) throw new Exception(reason); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    public static int Main()
    {
        var exit = new BattleExitSequence();
        Check(!exit.IsPlaying && !exit.Advance(100), "An unused fade cannot start a cinematic.");
        exit.Begin(); exit.MarkDrawn(); exit.Advance(.3f); exit.Begin();
        Check(exit.IsPlaying && exit.Elapsed == 0 && exit.MusicGain == 1, "Each battle starts a fresh full-volume fade.");
        Check(!exit.Advance(100) && exit.Elapsed == 0, "Loading cannot consume the first battle frame.");
        exit.MarkDrawn(); exit.Advance(BattleExitSequence.FadeOutDuration / 2);
        Check(Near(exit.BlackOpacity, .5f) && Near(exit.MusicGain, .5f), "Battle picture and music fade together.");
        Check(!exit.Advance(100) && exit.IsPlaying && exit.BlackOpacity == 1 && exit.MusicGain == 0,
            "Long frames stop at black without loading the ending early.");
        Check(!exit.Advance(100), "The covering black frame must actually be painted.");
        exit.MarkDrawn(); Check(exit.Advance(0) && !exit.IsPlaying, "A black repaint releases the cinematic.");
        Check(!exit.Advance(100), "Repeated updates cannot start the cinematic again.");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { exit.Advance(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid frame durations are rejected.");
        }
        foreach (int fps in new[] { 30, 60, 144 })
        {
            exit.Begin(); exit.MarkDrawn();
            float previousOpacity = 0;
            for (int frame = 0; frame < fps * 2; frame++)
            {
                exit.Advance(1f / fps);
                Check(exit.BlackOpacity >= previousOpacity && exit.BlackOpacity <= 1
                    && Near(exit.BlackOpacity + exit.MusicGain, 1), "Battle fade stays monotonic and synchronized at " + fps + " fps.");
                previousOpacity = exit.BlackOpacity;
            }
            Check(exit.IsPlaying && exit.MusicGain == 0, "All frame rates wait for a black repaint.");
            exit.MarkDrawn(); Check(exit.Advance(0), "All frame rates release after black.");
        }
        var king = new KingFreedSequence(25, 21);
        var possessed = new HeroPossessedSequence(30);
        var impact = new CastleImpactSequence();
        Check(king.MusicGain == 0 && possessed.MusicGain == 0 && impact.MusicGain == 0,
            "All three ending tracks start silent on black.");
        for (int frame = 0; frame < 84; frame++)
        {
            king.Advance(1f / 60); possessed.Advance(1f / 60); impact.Advance(1f / 60);
            Check(Near(king.MusicGain + king.BlackOpacity, 1) && Near(possessed.MusicGain + possessed.BlackOpacity, 1)
                && Near(impact.MusicGain + impact.BlackOpacity, 1), "Each ending clock keeps artwork and music in sync.");
            if (frame == 41) Check(Near(king.MusicGain, .5f) && Near(possessed.MusicGain, .5f)
                && Near(impact.MusicGain, .5f), "All three openings reach half strength together.");
        }
        Check(Near(king.MusicGain, 1) && Near(possessed.MusicGain, 1) && Near(impact.MusicGain, 1),
            "The opening reaches full volume in every ending.");
        king.Advance(king.DissolveAt + KingFreedSequence.DissolveDuration / 2 - king.Elapsed);
        impact.Advance(CastleImpactSequence.ImpactAt + CastleImpactSequence.FlashDuration / 2 - impact.Elapsed);
        Check(king.ProsperityOpacity > 0 && king.ProsperityOpacity < 1 && king.MusicGain == 1,
            "The good ending's internal dissolve keeps full music.");
        Check(impact.WhiteOpacity > 0 && impact.MusicGain == 1, "The meteor flash keeps full music.");
        var aftermath = new CastleDestroyedSequence(30); aftermath.Advance(.7f);
        Check(impact.MusicGain * aftermath.MusicGain == 1, "The white hillside transition never repeats the opening audio fade.");
        king.Advance(king.FadeOutAt - king.Elapsed + 1.5f);
        possessed.Advance(possessed.FadeOutAt - possessed.Elapsed + 1.5f);
        aftermath.Advance(aftermath.FadeOutAt - aftermath.Elapsed + 1.5f);
        Check(Near(king.MusicGain, .5f) && Near(possessed.MusicGain, .5f) && Near(aftermath.MusicGain, .5f),
            "The three existing final music fades still reach half volume.");
        king.Advance(1.5f); possessed.Advance(1.5f); aftermath.Advance(1.5f);
        Check(king.MusicGain == 0 && possessed.MusicGain == 0 && aftermath.MusicGain == 0,
            "Final black holds remain silent.");
        Console.WriteLine("ENDING_TRANSITION_CHECKS_OK: " + checks + " checks.");
        return 0;
    }
}
