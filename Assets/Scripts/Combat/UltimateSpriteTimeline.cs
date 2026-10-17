using System;

namespace DragonQuest.Combat
{
    // Pose and effect indices are independent of framerate, and share the actual hit clock.
    public static class UltimateSpriteTimeline
    {
        public static readonly string[] Atlases = { "ArenOmnislash", "ArenFinisher", "ArenImpact", "LiaGospel",
            "LiaCelestial", "BramBastion", "BramShieldImpact", "BramBarrier" };
        public const int FramesPerAtlas = 16;
        private static readonly int[][] Cuts={new[]{3,4,5,7},new[]{8,9,10,11},new[]{3,12,13,14},new[]{4,5,6,15}};
        private static readonly int[] LiaFloatPoses={8,9,10,11,10,9};
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(value, max));
        private static int Range(float elapsed, float start, float end, int first, int last)
            => Clamp(first+(int)((elapsed-start)/Math.Max(.001f,end-start)*(last-first+1)),first,last);
        private static int Loop(float elapsed, float start, int first, int count, float fps)
            => first+Math.Max(0,(int)((elapsed-start)*fps))%count;

        public static int CutIndex(float elapsed) => Clamp((int)Math.Floor(
            (elapsed-UltimateTimeline.CutStart+.065f)/UltimateTimeline.CutInterval),0,11);
        public static float CutAge(float elapsed) => elapsed-UltimateTimeline.HitTime(CutIndex(elapsed)+1);
        public static bool ActorVisible(float elapsed)
        {
            if(elapsed<.94f || elapsed>=UltimateTimeline.JumpStart) return true;
            return elapsed>=UltimateTimeline.CutStart-.065f && CutAge(elapsed)<.13f;
        }
        public static string PoseAtlas(AbilityDefinition ability, float elapsed) => UltimateTimeline.IsOmnislash(ability)
            ? elapsed>=UltimateTimeline.JumpStart?"ArenFinisher":"ArenOmnislash"
            : ability?.SourceAbilityId=="lia-salvation"?"LiaGospel":"BramBastion";
        public static int PoseFrame(AbilityDefinition ability, float elapsed)
        {
            if(UltimateTimeline.IsOmnislash(ability))
            {
                if(elapsed<.94f) return Range(elapsed,0,.75f,0,3);
                if(elapsed<UltimateTimeline.JumpStart)
                    return Cuts[CutIndex(elapsed)%Cuts.Length][Range(CutAge(elapsed),-.065f,.13f,0,3)];
                if(elapsed<UltimateTimeline.HangStart) return Range(elapsed,UltimateTimeline.JumpStart,UltimateTimeline.HangStart,0,3);
                if(elapsed<UltimateTimeline.DiveStart) return Loop(elapsed,UltimateTimeline.HangStart,4,4,10);
                if(elapsed<UltimateTimeline.FinalImpact) return Range(elapsed,UltimateTimeline.DiveStart,UltimateTimeline.FinalImpact,8,11);
                return Range(elapsed,UltimateTimeline.FinalImpact,UltimateTimeline.FinalImpact+.72f,12,15);
            }
            if(ability?.SourceAbilityId=="lia-salvation")
            {
                if(elapsed<.9f) return Range(elapsed,0,.9f,0,3);
                if(elapsed<1.9f) return Range(elapsed,.9f,1.9f,4,7);
                if(elapsed<5f) return LiaFloatPoses[Loop(elapsed,1.9f,0,LiaFloatPoses.Length,3)];
                return Range(elapsed,5f,5.95f,12,15);
            }
            if(elapsed<UltimateTimeline.AegisShieldImpact)
                return Range(elapsed,0,UltimateTimeline.AegisShieldImpact,0,3);
            if(elapsed<UltimateTimeline.AegisShieldFade)
                return Range(elapsed,UltimateTimeline.AegisShieldImpact,UltimateTimeline.AegisShieldFade,4,5);
            if(elapsed<UltimateTimeline.AegisShieldEnd)
                return Range(elapsed,UltimateTimeline.AegisShieldFade,UltimateTimeline.AegisShieldEnd,6,7);
            if(elapsed<UltimateTimeline.AegisBashStart) return 7;
            if(elapsed<UltimateTimeline.AegisImpact)
                return Range(elapsed,UltimateTimeline.AegisBashStart,UltimateTimeline.AegisImpact,8,9);
            if(elapsed<UltimateTimeline.AegisImpact+.1f) return 10;
            if(elapsed<UltimateTimeline.AegisImpact+.2f) return 11;
            return Range(elapsed,UltimateTimeline.AegisImpact+.2f,UltimateTimeline.AegisRecoveryEnd,12,15);
        }
        public static int CelestialFrame(float elapsed)
        {
            if(elapsed<1.45f) return Range(elapsed,.7f,1.45f,0,7);
            if(elapsed<5.1f) return Loop(elapsed,1.45f,8,4,4);
            return Range(elapsed,5.1f,5.9f,12,15);
        }
        public static bool BarrierVisible(float elapsed) => elapsed>=UltimateTimeline.AegisShieldStart
            && elapsed<UltimateTimeline.AegisShieldEnd;
        public static bool ShieldImpactVisible(float elapsed) => elapsed>=UltimateTimeline.AegisImpact
            && elapsed<UltimateTimeline.AegisEffectEnd;
        public static int ShieldImpactFrame(float elapsed)
            => Range(elapsed,UltimateTimeline.AegisImpact,UltimateTimeline.AegisEffectEnd,0,15);
        public static int BarrierFrame(float elapsed)
        {
            if(elapsed<UltimateTimeline.AegisShieldImpact)
                return Range(elapsed,UltimateTimeline.AegisShieldStart,UltimateTimeline.AegisShieldImpact,0,7);
            if(elapsed<UltimateTimeline.AegisShieldFade)
                return Range(elapsed,UltimateTimeline.AegisShieldImpact,UltimateTimeline.AegisShieldFade,8,11);
            return Range(elapsed,UltimateTimeline.AegisShieldFade,UltimateTimeline.AegisShieldEnd,12,15);
        }
        public static int SlashFrame(float age, int hit) => Range(age,0,.2f,hit%2==0?4:0,hit%2==0?7:3);
        public static int FinisherFrame(float elapsed) => Range(elapsed,UltimateTimeline.FinalImpact-.1f,UltimateTimeline.FinalImpact+.95f,8,15);
    }
}
