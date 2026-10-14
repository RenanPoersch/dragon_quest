using System;
using DragonQuest.Progression;
using DragonQuest.Prototype;

class QuarryRuneChecks
{
    static int checks;
    static void Check(bool ok,string reason) { checks++; if (!ok) throw new Exception(reason); }
    static void Main()
    {
        foreach (MonolithSymbol symbol in Enum.GetValues(typeof(MonolithSymbol)))
        {
            int rate=QuarryRuneAnimation.Rate(symbol);
            Check(QuarryRuneAnimation.FrameAt(-1,symbol)==0,"Negative time rests on first frame");
            Check(QuarryRuneAnimation.FrameAt(float.NaN,symbol)==0,"Invalid time is safe");
            Check(QuarryRuneAnimation.FrameAt(float.PositiveInfinity,symbol)==0,"Infinite time is safe");
            for (int frame=0;frame<QuarryRuneAnimation.FrameCount;frame++)
            {
                Check(QuarryRuneAnimation.FrameAt((frame+.1f)/rate,symbol)==frame,"All eight drawn frames are used");
                Check(QuarryRuneAnimation.FrameAt((frame+8.1f)/rate,symbol)==frame,"Animation loops with stable frame order");
            }
            Check(QuarryRuneAnimation.FrameAt(8.01f/rate,symbol)==0,"Loop returns to first frame");
            Check(QuarryRuneAnimation.Resource(symbol).Contains(symbol.ToString()),"Each symbol has its own drawn atlas");
        }
        Check(QuarryRuneAnimation.Rate(MonolithSymbol.Moon)<QuarryRuneAnimation.Rate(MonolithSymbol.Star)
            && QuarryRuneAnimation.Rate(MonolithSymbol.Star)<QuarryRuneAnimation.Rate(MonolithSymbol.Sun),"Three symbols have distinct animation cadence");
        Console.WriteLine(checks+" rune frame, loop, cadence and resource checks passed.");
    }
}
