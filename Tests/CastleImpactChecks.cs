using System;
using DragonQuest.Prototype;

public static class CastleImpactChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static bool Near(float a, float b) => Math.Abs(a-b)<.003f;
    private static CastleImpactSequence At(float t)
    { var s = new CastleImpactSequence(); s.Advance(t); return s; }
    public static int Main()
    {
        Check(At(0).BlackOpacity == 1 && At(0).MeteorOpacity == 0, "Establish storm from black before meteor.");
        Check(At(CastleImpactSequence.MeteorStart).MeteorOpacity == 0, "Meteor waits for the storm establishing shot.");
        Check(CastleImpactSequence.FallDuration >= 12, "Descent remains deliberately slow.");
        var emerging = At(CastleImpactSequence.MeteorStart+2.1f);
        Check(emerging.MeteorY + emerging.MeteorSize*.24f > 24,
            "Visible rock enters the picture as its cloud emergence finishes, well before the last part of the fall.");
        float lastX = At(0).MeteorX, lastY = At(0).MeteorY, lastSize = At(0).MeteorSize;
        for (float t=0; t<CastleImpactSequence.ImpactAt; t+=.05f)
        {
            var s = At(t);
            Check(s.MeteorX>=lastX && s.MeteorY>=lastY && s.MeteorSize>=lastSize,
                "Meteor continuously descends toward the castle and grows in scale.");
            Check(s.WhiteOpacity==0, "White starts only on collision.");
            Check(s.MeteorOpacity>=0 && s.MeteorOpacity<=1, "Cloud emergence opacity remains bounded.");
            lastX=s.MeteorX; lastY=s.MeteorY; lastSize=s.MeteorSize;
        }
        Check(Near(At(CastleImpactSequence.ImpactAt+.225f).WhiteOpacity,.5f), "Impact blooms smoothly to white.");
        Check(At(CastleImpactSequence.ImpactAt+.45f).WhiteOpacity==1, "Impact reaches full white.");
        Check(At(CastleImpactSequence.CompleteAt-.01f).WhiteOpacity==1
            && !At(CastleImpactSequence.CompleteAt-.01f).IsComplete, "White holds before the hillside begins.");
        var huge = At(10000);
        Check(huge.IsComplete && huge.WhiteOpacity==1, "A long frame ends the impact under white.");
        huge.Advance(10);
        Check(Near(huge.Elapsed,CastleImpactSequence.CompleteAt), "Completion is stable.");
        var sixty = At(0); for (int i=0;i<600;i++) sixty.Advance(1f/60);
        Check(Near(sixty.MeteorX,At(10).MeteorX) && Near(sixty.MeteorY,At(10).MeteorY), "Frame rate does not change the path.");
        foreach (float bad in new[] {-1f,float.NaN,float.PositiveInfinity})
        {
            bool rejected=false; try { At(0).Advance(bad); } catch (ArgumentOutOfRangeException) { rejected=true; }
            Check(rejected,"Invalid clocks are rejected.");
        }
        Console.WriteLine("CASTLE_IMPACT_OK: "+checks+" slow descent, storm, collision, white hold and frame-rate checks.");
        return 0;
    }
}
