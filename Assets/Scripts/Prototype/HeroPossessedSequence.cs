using System;
using System.Collections.Generic;

namespace DragonQuest.Prototype
{
    // One unscaled clock drives the upward camera, narration, lightning and fades.
    public sealed class HeroPossessedSequence
    {
        public const float FadeInDuration = CastleDestroyedSequence.FadeInDuration;
        public const float CameraStart = 1.4f, CarpetDuration = 9, StairDuration = 10, RevealDuration = 8;
        public const float CameraStopAt = CameraStart + CarpetDuration + StairDuration + RevealDuration;
        public const float FinalCameraFocusY = .16f;
        public const float TextStart = 2.2f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = CastleDestroyedSequence.EndHold;
        public const float FadeOutDuration = CastleDestroyedSequence.FadeOutDuration;
        public const float BlackHold = CastleDestroyedSequence.BlackHold;
        public const float FirstLightningAt = CameraStopAt + .7f;
        public const int LightningFrameCount = 4, DefaultStormSeed = 3157;
        private const float FlashRise = .006f;
        private readonly List<StormFlash> flashes = new List<StormFlash>();
        public int StormSeed { get; }
        public int StormFlashCount => flashes.Count;
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        // Even short/localized copy leaves time to see two strikes after the camera locks.
        public float FadeOutAt => Math.Max(TextEndAt + EndHold, CameraStopAt + 11);
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public bool CameraStopped => Elapsed >= CameraStopAt;
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        public float BlackOpacity => Math.Max(1 - Smooth(Elapsed / FadeInDuration), Smooth((Elapsed - FadeOutAt) / FadeOutDuration));

        // Normalized image coordinates, measured from the top of the continuous painting.
        public float CameraFocusY
        {
            get
            {
                float t = Elapsed - CameraStart;
                if (t <= CarpetDuration) return Lerp(.84f, .64f, Smooth(t / CarpetDuration));
                t -= CarpetDuration;
                if (t <= StairDuration) return Lerp(.64f, .44f, Smooth(t / StairDuration));
                return Lerp(.44f, FinalCameraFocusY, Smooth((t - StairDuration) / RevealDuration));
            }
        }
        public float CameraImageWidth => Lerp(1500, 1040, Smooth((Elapsed - CameraStart) / (CameraStopAt - CameraStart)));
        public float Lightning => LightningState.Intensity;
        public int LightningFrame => LightningState.Frame;
        public StormLight LightningState
        {
            get
            {
                if (!CameraStopped || Elapsed >= FadeOutAt) return StormLight.Dark;
                for (int i=0; i<flashes.Count; i++)
                {
                    StormFlash flash=flashes[i];
                    if (Elapsed<flash.Start) break;
                    float strength=flash.Strength*Pulse(Elapsed-flash.Start,FlashRise,flash.Duration-FlashRise);
                    if (strength>0) return new StormLight(i,flash.Burst,flash.Frame,strength,flash.X,flash.Y,
                        flash.Width,flash.Height,flash.Angle,flash.Mirrored);
                }
                return StormLight.Dark;
            }
        }
        // The Editor and render checks can inspect actual generated peaks without
        // relying on frame counters or hard-coded timing for one particular seed.
        public float StormFlashPeakAt(int index) => flashes[index].Start+FlashRise;
        public float StormFlashEndAt(int index) => flashes[index].Start+flashes[index].Duration;

        private void BuildStorm(int seed)
        {
            uint random=unchecked((uint)seed)^0x9e3779b9u;
            if (random==0) random=1;
            float time=FirstLightningAt;
            for (int burst=0; time<FadeOutAt-.8f; burst++)
            {
                int count=3+(int)(Next(ref random)*3);
                int firstFrame=(int)(Next(ref random)*LightningFrameCount);
                float width=Lerp(.11f,.18f,Next(ref random)), height=Lerp(.14f,.24f,Next(ref random));
                float angle=Lerp(-12,12,Next(ref random));
                bool mirror=Next(ref random)<.5f;
                float radians=angle*(float)Math.PI/180;
                float extentX=(Math.Abs((float)Math.Cos(radians))*width+Math.Abs((float)Math.Sin(radians))*height/(2f/3))/2;
                float extentY=(Math.Abs((float)Math.Sin(radians))*width*(2f/3)+Math.Abs((float)Math.Cos(radians))*height)/2;
                float x=Next(ref random)<.5f?Lerp(.27f,.40f,Next(ref random)):Lerp(.60f,.73f,Next(ref random));
                x=Clamp(x,.18f+extentX,.84f-extentX);
                float y=Clamp(Lerp(.87f,.97f,Next(ref random)),.78f+extentY,1.045f-extentY);
                for (int pulse=0; pulse<count; pulse++)
                {
                    float duration=Lerp(.034f,.064f,Next(ref random));
                    float strength=pulse==0?1:Lerp(.55f,.95f,Next(ref random));
                    flashes.Add(new StormFlash(time,duration,strength,burst,(firstFrame+pulse)%LightningFrameCount,
                        x,y,width,height,angle,mirror));
                    time+=duration+Lerp(.024f,.067f,Next(ref random));
                }
                time+=Lerp(1.2f,2.8f,Next(ref random));
            }
        }

        public HeroPossessedSequence(int lineCount, int stormSeed = DefaultStormSeed)
        {
            if (lineCount <= 0) throw new ArgumentOutOfRangeException(nameof(lineCount));
            LineCount = lineCount; StormSeed=stormSeed; BuildStorm(stormSeed);
        }
        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            Elapsed = Math.Min(CompleteAt, Elapsed + unscaledDeltaTime);
        }
        private static float Pulse(float t, float rise, float decay)
        {
            if (t < 0 || t > rise + decay) return 0;
            return t < rise ? t / rise : 1 - Smooth((t - rise) / decay);
        }
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp(float value,float minimum,float maximum) => Math.Max(minimum,Math.Min(maximum,value));
        private static float Next(ref uint state)
        { state^=state<<13; state^=state>>17; state^=state<<5; return (state>>8)*(1f/16777216); }
        private static float Smooth(float value)
        { float t = Math.Max(0, Math.Min(1, value)); return t * t * (3 - 2 * t); }

        private readonly struct StormFlash
        {
            public readonly float Start,Duration,Strength,X,Y,Width,Height,Angle;
            public readonly int Burst,Frame;
            public readonly bool Mirrored;
            public StormFlash(float start,float duration,float strength,int burst,int frame,float x,float y,float width,float height,float angle,bool mirrored)
            { Start=start;Duration=duration;Strength=strength;Burst=burst;Frame=frame;X=x;Y=y;Width=width;Height=height;Angle=angle;Mirrored=mirrored; }
        }
        public readonly struct StormLight
        {
            public static StormLight Dark => new StormLight(-1,-1,-1,0,0,0,0,0,0,false);
            public readonly int FlashIndex,Burst,Frame;
            public readonly float Intensity,X,Y,Width,Height,Angle;
            public readonly bool Mirrored;
            public StormLight(int flashIndex,int burst,int frame,float intensity,float x,float y,float width,float height,float angle,bool mirrored)
            { FlashIndex=flashIndex;Burst=burst;Frame=frame;Intensity=intensity;X=x;Y=y;Width=width;Height=height;Angle=angle;Mirrored=mirrored; }
        }
    }
}
