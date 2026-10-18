using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleSpellAnimation
    {
        private BattleAnimationSheet protect, shieldHit, meteor;
        private readonly MeteorAbsorptionAnimation absorption;
        private readonly System.Action<Texture2D, Rect, Rect, Color> drawTexture;
        public CastleSpellAnimation(System.Action<Texture2D, Rect, Rect, Color> drawTexture = null)
        { this.drawTexture = drawTexture; absorption = new MeteorAbsorptionAnimation(drawTexture); }
        public void Prepare() => Load();
        private void Load()
        {
            if (meteor != null) return;
            meteor = new BattleAnimationSheet("PixelArt/TyrantFlamingMeteor", 4, 1, false, drawTexture);
            protect = new BattleAnimationSheet("PixelArt/TyrantProtect", 4, 1, false, drawTexture);
            shieldHit = new BattleAnimationSheet("PixelArt/TyrantShieldHit", 4, 1, false, drawTexture);
            absorption.Prepare();
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
        public bool DrawAbsorber(Rect target, string id, float elapsed, Color tint)
        { Load(); return absorption.DrawBearer(target, id, elapsed, tint); }
        public bool DrawAbsorbingCaster(Rect king, float elapsed, Color tint)
        { Load(); return absorption.DrawCaster(king, elapsed, tint); }
        public void DrawMeteor(Rect king, Rect target, bool absorbed, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return;
            Load();
            if (absorbed)
            {
                absorption.DrawTransfer(king, target, elapsed);
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
