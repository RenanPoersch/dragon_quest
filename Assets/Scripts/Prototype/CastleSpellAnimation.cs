using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class CastleSpellAnimation
    {
        private BattleAnimationSheet protect, shieldHit, absorption, meteor;
        public void Prepare() => Load();
        private void Load()
        {
            if (meteor != null) return;
            meteor = new BattleAnimationSheet("PixelArt/TyrantFlamingMeteor", 4, 1, false);
            protect = new BattleAnimationSheet("PixelArt/TyrantProtect", 4, 1, false);
            shieldHit = new BattleAnimationSheet("PixelArt/TyrantShieldHit", 4, 1, false);
            absorption = new BattleAnimationSheet("PixelArt/MeteorAbsorption", 4, 1, false);
        }
        public void DrawCharge(CastleBattleRules rules, float time)
        {
            Load();
            float height = CastleMeteorPresentation.ChargeHeight(CastleMeteorPresentation.ChargeLevel(rules));
            meteor.Draw(new Vector2(510, 134 + Mathf.Sin(time * 2) * 3), height, 0, Color.white);
        }
        public void DrawProtect(Rect king, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.ProtectDuration) return;
            Load();
            protect.Draw(new Vector2(king.center.x, king.yMax + 8), king.height * 1.65f,
                CastleMeteorPresentation.EffectFrame(elapsed, CastleMeteorPresentation.ProtectDuration), Color.white);
        }
        public void DrawShieldHit(Rect target, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.ShieldHitDuration) return;
            Load();
            shieldHit.Draw(new Vector2(target.center.x, target.yMax + 10), target.height * 1.6f,
                CastleMeteorPresentation.EffectFrame(elapsed, CastleMeteorPresentation.ShieldHitDuration), Color.white);
        }
        public void DrawMeteor(Rect target, bool absorbed, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return;
            Load();
            if (absorbed)
            {
                float travel = Mathf.Clamp01(elapsed / CastleMeteorPresentation.ImpactDelay);
                Vector2 end = new Vector2(target.center.x, target.center.y + 25);
                Vector2 foot = Vector2.Lerp(new Vector2(510, 134), end, travel);
                float shrink = 1 - Mathf.Clamp01((elapsed - CastleMeteorPresentation.AbsorptionStart)
                    / (CastleMeteorPresentation.ImpactDelay - CastleMeteorPresentation.AbsorptionStart));
                if (elapsed < CastleMeteorPresentation.ImpactDelay)
                    meteor.Draw(foot, Mathf.Lerp(180, 250, travel) * shrink, 0, Color.white);
                if (elapsed >= CastleMeteorPresentation.AbsorptionStart)
                {
                    float sinceAbsorb = elapsed - CastleMeteorPresentation.AbsorptionStart;
                    absorption.Draw(new Vector2(target.center.x, target.yMax + 18), target.height * 2.2f,
                        CastleMeteorPresentation.EffectFrame(sinceAbsorb,
                            CastleMeteorPresentation.Duration - CastleMeteorPresentation.AbsorptionStart), Color.white);
                }
                return;
            }
            if (elapsed < CastleMeteorPresentation.ImpactDelay)
            {
                float travel = Mathf.Clamp01(elapsed / CastleMeteorPresentation.ImpactDelay);
                Vector2 foot = Vector2.Lerp(new Vector2(510, 134), new Vector2(820, 375), travel * travel);
                meteor.Draw(foot, Mathf.Lerp(180, 330, travel), CastleMeteorPresentation.MeteorFrame(elapsed), Color.white);
            }
            else
            {
                float fade = 1 - Mathf.Clamp01((elapsed - CastleMeteorPresentation.ImpactDelay - .8f) / .7f);
                meteor.Draw(new Vector2(820, 390), 340, CastleMeteorPresentation.MeteorFrame(elapsed),
                    new Color(1, 1, 1, fade));
            }
        }
    }
}
