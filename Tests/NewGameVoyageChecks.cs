using System;
using DragonQuest.Prototype;

public static class NewGameVoyageChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    private static NewGameVoyageSequence At(float elapsed)
    { var sequence = new NewGameVoyageSequence(); sequence.Advance(elapsed); return sequence; }

    public static int Main()
    {
        var start = At(0);
        Check(start.IsPlaying && start.ShowsScene && Near(start.BlackOpacity, 1), "New game starts at black.");
        Check(start.ShipX > 1280, "Ship starts outside the right edge.");
        Check(Near(At(NewGameVoyageSequence.FadeInDuration / 2).BlackOpacity, .5f), "Arrival fades in smoothly.");
        float arrivalAt = NewGameVoyageSequence.ArrivalStart + NewGameVoyageSequence.ArrivalDuration;
        Check(Near(At(arrivalAt).ShipX, NewGameVoyageSequence.ShipStopX), "Ship stops slightly before the screen midpoint.");
        Check(NewGameVoyageSequence.ShipStopX < 640 && NewGameVoyageSequence.ShipStopX + NewGameVoyageSequence.ShipWidth < 1280,
            "The entire stationary ship fits next to the left narrative.");
        Check(Near(At(NewGameVoyageSequence.TextEndAt - 1).ShipX, NewGameVoyageSequence.ShipStopX), "Ship waits for the entire story.");
        Check(At(NewGameVoyageSequence.WalkStart + .2f).HeroWalkProgress(0) > 0
            && Near(At(NewGameVoyageSequence.WalkStart + .2f).HeroWalkProgress(1), 0), "Party walks forward in a staggered line.");
        for (int i = 0; i < 3; i++)
        {
            Check(Near(start.HeroWalkProgress(i), 0), "Heroes start on deck.");
            Check(Near(At(9).HeroWalkProgress(i), 1), "All heroes reach the bow and stop walking.");
        }
        Check(start.TextOffset >= NewGameVoyageSequence.TextWindowHeight, "Story enters at the bottom of the left column.");
        var textEnd = At(NewGameVoyageSequence.TextEndAt);
        Check(textEnd.TextOffset + NewGameVoyageSequence.LineCount * NewGameVoyageSequence.LineHeight <= .002f,
            "The last line clears the column before departure.");
        Check(!textEnd.IsDeparting, "There is a pause after the last line.");
        float previousX = start.ShipX;
        for (float t = .1f; t < NewGameVoyageSequence.RevealAt; t += .1f)
        {
            var frame = At(t);
            Check(frame.ShipX <= previousX + .002f, "The vessel only travels left.");
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1, "Fade stays in range.");
            previousX = frame.ShipX;
        }
        var halfway = At(NewGameVoyageSequence.DepartureAt + 4);
        Check(halfway.IsDeparting && halfway.ShipX < NewGameVoyageSequence.ShipStopX && halfway.BlackOpacity > 0,
            "Ship sails left while fade-out is in progress.");
        var black = At(NewGameVoyageSequence.BlackAt);
        Check(black.ShipX + NewGameVoyageSequence.ShipWidth < 0 && Near(black.BlackOpacity, 1), "Ship fully leaves frame at black.");
        Check(At(NewGameVoyageSequence.RevealAt).IsPlaying && !At(NewGameVoyageSequence.RevealAt).ShowsScene,
            "Gameplay is revealed under the final fade while inputs remain blocked.");
        Check(Near(At(NewGameVoyageSequence.RevealAt + NewGameVoyageSequence.RevealDuration / 2).BlackOpacity, .5f),
            "The map fades in from black.");
        var complete = At(10000);
        Check(!complete.IsPlaying && Near(complete.BlackOpacity, 0), "A long frame safely finishes the cinematic.");
        complete.Advance(1);
        Check(Near(complete.Elapsed, NewGameVoyageSequence.CompleteAt), "Completion stays stable.");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { start.Advance(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid delta is rejected.");
        }
        Console.WriteLine("NEW_GAME_VOYAGE_OK: " + checks + " arrival, walking, scrolling, departure and transition checks.");
        return 0;
    }
}
