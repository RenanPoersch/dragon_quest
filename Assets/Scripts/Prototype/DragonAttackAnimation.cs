using System;

namespace DragonQuest.Prototype
{
    public static class DragonAttackAnimation
    {
        public static string Resource(string abilityId) => abilityId == "dragon-claw" ? "DragonClaw"
            : abilityId == "dragon-breath" ? "DragonBreath"
            : abilityId == "charge-breath" ? "DragonCharge" : null;
        public static float FrameDuration(string abilityId) => abilityId == "dragon-breath" || abilityId == "charge-breath" ? 0.18f : 0.12f;
        public static float Duration(string abilityId) => 6 * FrameDuration(abilityId);
        public static float ImpactDelay(string abilityId) => 3 * FrameDuration(abilityId);
        public static int Frame(string abilityId, float elapsed)
        {
            int frame = Math.Max(0, Math.Min(5, (int)(elapsed / FrameDuration(abilityId))));
            return frame;
        }
    }
}
