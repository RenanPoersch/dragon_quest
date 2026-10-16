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
        float visibleShip = (1280 - NewGameVoyageSequence.ShipStopX) / NewGameVoyageSequence.ShipWidth;
        Check(NewGameVoyageSequence.ShipStopX < 640 && visibleShip > .45f && visibleShip < .55f,
            "Close framing shows only the forward half, with the stern beyond the right edge.");
        Check(NewGameVoyageSequence.HeroSize >= 90, "Heroes grow with the close view.");
        Check(NewGameVoyageSequence.TextStart > arrivalAt && Near(At(arrivalAt).NarrativeOpacity, 0),
            "The entire narrative area remains absent until arrival.");
        Check(Near(At(NewGameVoyageSequence.TextStart + .7f).NarrativeOpacity, .5f)
            && Near(At(NewGameVoyageSequence.TextStart + 1.4f).NarrativeOpacity, 1), "Narrative fades in like the ending.");
        Check(NewGameVoyageSequence.FadeInDuration == CastleDestroyedSequence.FadeInDuration
            && NewGameVoyageSequence.FadeOutDuration == CastleDestroyedSequence.FadeOutDuration
            && NewGameVoyageSequence.BlackHold == CastleDestroyedSequence.BlackHold, "Black fades use the ending's timings.");
        Check(Near(At(NewGameVoyageSequence.TextEndAt - 1).ShipX, NewGameVoyageSequence.ShipStopX), "Ship waits for the entire story.");
        Check(At(NewGameVoyageSequence.WalkStart + .2f).HeroWalkProgress(0) > 0
            && Near(At(NewGameVoyageSequence.WalkStart + .2f).HeroWalkProgress(1), 0), "Party walks forward in a staggered line.");
        for (int i = 0; i < 3; i++)
        {
            Check(Near(start.HeroWalkProgress(i), 0), "Heroes start on deck.");
            Check(Near(At(9).HeroWalkProgress(i), 1), "All heroes reach the bow and stop walking.");
            float x = NewGameVoyageSequence.ShipStopX + At(9).HeroLocalX(i) * NewGameVoyageSequence.ShipWidth;
            Check(x - NewGameVoyageSequence.HeroSize / 2 > 600 && x + NewGameVoyageSequence.HeroSize / 2 < 1280,
                "All three heroes fit on the visible foredeck beside the narrative.");
        }
        Check(start.TextOffset >= NewGameVoyageSequence.TextWindowHeight, "Story enters at the bottom of the left column.");
        var textEnd = At(NewGameVoyageSequence.TextEndAt);
        Check(textEnd.TextOffset + NewGameVoyageSequence.LineCount * NewGameVoyageSequence.LineHeight <= .002f,
            "The last line clears the column before departure.");
        Check(!textEnd.IsDeparting, "There is a pause after the last line.");
        float previousX = start.ShipX;
        float previousBackground = start.BackgroundOffset;
        for (float t = .1f; t < NewGameVoyageSequence.RevealAt; t += .1f)
        {
            var frame = At(t);
            Check(frame.ShipX <= previousX + .002f, "The vessel only travels left.");
            Check(frame.BlackOpacity >= 0 && frame.BlackOpacity <= 1, "Fade stays in range.");
            Check(frame.BackgroundOffset > previousBackground, "Background travels right throughout leftward navigation.");
            if (t <= arrivalAt) Check(Near(frame.NarrativeOpacity, 0), "No text, heading or shade during arrival.");
            previousX = frame.ShipX;
            previousBackground = frame.BackgroundOffset;
        }
        Check(Near(At(NewGameVoyageSequence.DepartureAt).NarrativeOpacity, 0), "Narrative shade clears before departure.");
        var halfway = At(NewGameVoyageSequence.FadeOutAt + NewGameVoyageSequence.FadeOutDuration / 2);
        Check(halfway.IsDeparting && halfway.ShipX < NewGameVoyageSequence.ShipStopX && halfway.BlackOpacity > 0,
            "Ship sails left while fade-out is in progress.");
        Check(Near(halfway.BlackOpacity, .5f), "Three-second black fade has the ending's smooth midpoint.");
        var black = At(NewGameVoyageSequence.BlackAt);
        Check(black.ShipX + NewGameVoyageSequence.ShipWidth < 0 && Near(black.BlackOpacity, 1), "Ship fully leaves frame at black.");
        Check(Near(At(NewGameVoyageSequence.BlackAt + .8f).BlackOpacity, 1), "Black is held before revealing the map.");
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
