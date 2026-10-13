using System;

namespace DragonQuest.Prototype
{
    public static class DragonAttackAnimation
    {
        public static string Resource(string abilityId) => abilityId == "dragon-claw" ? "DragonClaw"
            : abilityId == "dragon-breath" || abilityId == "charge-breath" ? "DragonBreath" : null;
        public static float FrameDuration(string abilityId) => abilityId == "dragon-breath" ? 0.18f : 0.12f;
        public static float Duration(string abilityId) => 6 * FrameDuration(abilityId);
        public static float ImpactDelay(string abilityId) => 3 * FrameDuration(abilityId);
        public static int Frame(string abilityId, float elapsed)
        {
            int frame = Math.Max(0, Math.Min(5, (int)(elapsed / FrameDuration(abilityId))));
            // A preparacao mostra apenas a garganta carregando, sem disparar fogo.
            return abilityId == "charge-breath" ? frame == 0 || frame == 5 ? 0 : 1 : frame;
        }
    }
}
