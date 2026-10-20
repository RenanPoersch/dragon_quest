using System;
using System.Collections.Generic;

namespace DragonQuest.Prototype
{
    // One unscaled clock drives the upward camera, narration, lightning and fades.
    public sealed class HeroPossessedSequence
    {
        public const float FadeInDuration = 3.2f;
        public const float CameraStart = FadeInDuration, CarpetDuration = 9, StairDuration = 10, RevealDuration = 8;
        public const float CameraStopAt = CameraStart + CarpetDuration + StairDuration + RevealDuration;
        public const float FinalCameraFocusY = .16f;
        public const float TextStart = FadeInDuration + .8f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = CastleDestroyedSequence.EndHold;
        public const float FadeOutDuration = CastleDestroyedSequence.FadeOutDuration;
        public const float BlackHold = CastleDestroyedSequence.BlackHold;
        public const float FirstLightningAt = CameraStopAt + .7f;
        public const int LightningFrameCount = 4, DefaultStormSeed = 3157;
        public const float LightningTopY=1.08f,LightningWidth=.16f,LightningHeight=.30f;
        private static readonly float[] LocationX={.27f,.36f,.45f,.55f,.64f,.73f};
        private readonly List<StormFlash> flashes = new List<StormFlash>();
        public int StormSeed { get; }
        public int StormFlashCount => flashes.Count;
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float MusicGain => 1 - BlackOpacity;
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
                // Share velocities at the carpet/stair/reveal joins instead of
                // easing to a complete stop at every waypoint.
                float stairsVelocity = (.44f - .84f) / (CarpetDuration + StairDuration);
                float revealVelocity = (FinalCameraFocusY - .64f) / (StairDuration + RevealDuration);
                if (t <= CarpetDuration) return CameraCurve(.84f, .64f, 0, stairsVelocity, t, CarpetDuration);
                t -= CarpetDuration;
                if (t <= StairDuration) return CameraCurve(.64f, .44f, stairsVelocity, revealVelocity, t, StairDuration);
                return CameraCurve(.44f, FinalCameraFocusY, revealVelocity, 0, t - StairDuration, RevealDuration);
            }
        }
        public float CameraImageWidth => Lerp(1500, 1040, Smooth((Elapsed - CameraStart) / (CameraStopAt - CameraStart)));
        private static float CameraCurve(float from, float to, float startVelocity, float endVelocity, float time, float duration)
        {
            float t = Math.Max(0, Math.Min(1, time / duration)), t2 = t * t, t3 = t2 * t;
            return (2*t3 - 3*t2 + 1)*from + (t3 - 2*t2 + t)*duration*startVelocity
                + (-2*t3 + 3*t2)*to + (t3 - t2)*duration*endVelocity;
        }
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
                    float rise=FlashRise(flash.Duration);
                    float strength=flash.Strength*Pulse(Elapsed-flash.Start,rise,flash.Duration-rise);
                    if (strength>0) return new StormLight(i,flash.Burst,flash.Frame,strength,flash.X,flash.Y,
                        flash.Width,flash.Height,flash.Angle,flash.Mirrored);
                }
                return StormLight.Dark;
            }
        }
        // The Editor and render checks can inspect actual generated peaks without
        // relying on frame counters or hard-coded timing for one particular seed.
        public float StormFlashPeakAt(int index) => flashes[index].Start+FlashRise(flashes[index].Duration);
        public float StormFlashStartAt(int index) => flashes[index].Start;
        public float StormFlashEndAt(int index) => flashes[index].Start+flashes[index].Duration;

        private void BuildStorm(int seed)
        {
            uint random=unchecked((uint)seed)^0x9e3779b9u;
            if (random==0) random=1;
            float time=FirstLightningAt;
            int usedLocations=0;
            float previousX=-10;
            for (int burst=0; time<FadeOutAt-.8f; burst++)
            {
                int count=3+(int)(Next(ref random)*3);
                int firstFrame=(int)(Next(ref random)*LightningFrameCount);
                for (int pulse=0; pulse<count; pulse++)
                {
                    // Each flash chooses a distant horizontal position. The root,
                    // height and angle stay fixed so the bolt enters from the top.
                    int location=ChooseLocation(ref random,ref usedLocations,previousX);
                    bool mirror=Next(ref random)<.5f;
                    float x=Clamp(LocationX[location]+Lerp(-.010f,.010f,Next(ref random)),.18f+LightningWidth/2,.84f-LightningWidth/2);
                    float remaining=FadeOutAt-.01f-time;
                    if(remaining<=0) break;
                    float duration=Math.Min(Next(ref random),remaining);
                    float strength=pulse==0?1:Lerp(.55f,.95f,Next(ref random));
                    flashes.Add(new StormFlash(time,duration,strength,burst,(firstFrame+pulse)%LightningFrameCount,
                        x,LightningTopY-LightningHeight/2,LightningWidth,LightningHeight,0,mirror));
                    previousX=x;
                    time+=duration+Lerp(.024f,.067f,Next(ref random));
                }
                time+=Lerp(1.2f,2.8f,Next(ref random));
            }
        }
        private static int ChooseLocation(ref uint random,ref int used,float previousX)
        {
            int eligible=EligibleLocations(used,previousX);
            if (eligible==0) { used=0;eligible=EligibleLocations(used,previousX); }
            int count=0;
            for(int i=0;i<LocationX.Length;i++) if((eligible&(1<<i))!=0) count++;
            int choice=(int)(Next(ref random)*count);
            for(int i=0;i<LocationX.Length;i++)
                if((eligible&(1<<i))!=0 && choice--==0) { used|=1<<i;return i; }
            throw new InvalidOperationException("No black area available for the next storm flash.");
        }
        private static int EligibleLocations(int used,float previousX)
        {
            int result=0;
            for(int i=0;i<LocationX.Length;i++)
            {
                float dx=LocationX[i]-previousX;
                // About 250 reference pixels between anchors leaves room for
                // jitter and bounds clamping while keeping flashes visibly apart.
                if((used&(1<<i))==0 && dx*dx>.24f*.24f) result|=1<<i;
            }
            return result;
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
            if (rise+decay<=0 || t < 0 || t > rise + decay) return 0;
            return t < rise ? t / rise : 1 - Smooth((t - rise) / decay);
        }
        private static float FlashRise(float duration) => Math.Min(.012f,duration*.2f);
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
