using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleSpellAnimation
    {
        private BattleAnimationSheet protect, shieldHit, fireball;
        private readonly MeteorAbsorptionAnimation absorption;
        private readonly System.Action<Texture2D, Rect, Rect, Color> drawTexture;
        public CastleSpellAnimation(System.Action<Texture2D, Rect, Rect, Color> drawTexture = null)
        { this.drawTexture = drawTexture; absorption = new MeteorAbsorptionAnimation(drawTexture); }
        public void Prepare() => Load();
        private void Load()
        {
            if (fireball != null) return;
            fireball = new BattleAnimationSheet("PixelArt/TyrantFireballRelease", 2, 1, false, drawTexture);
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
        public bool DrawMeteorCaster(Rect king, float elapsed, Color tint)
        { Load(); return fireball.Draw(new Vector2(king.center.x, king.yMax), king.height, 0, tint); }
        public void DrawMeteor(Rect king, Rect target, bool absorbed, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return;
            Load();
            if (absorbed)
            {
                absorption.DrawTransfer(king, target, elapsed);
                return;
            }
            // A bola permanece na mao e cresce; a queda aparece apenas na cena externa.
            if (elapsed < CastleMeteorPresentation.ImpactDelay)
                fireball.Draw(new Vector2(king.center.x + king.height * .43f, king.y + king.height * .26f),
                    king.height * CastleMeteorPresentation.FireballScale(elapsed), 1, Color.white);
        }
    }
}
