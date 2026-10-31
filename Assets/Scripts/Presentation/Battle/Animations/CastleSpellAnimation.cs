using DragonQuest.Presentation.Battle;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    public sealed class CastleSpellAnimation
    {
        private BattleAnimationSheet protect, shieldHit, fireball, throwingKing;
        private readonly MeteorAbsorptionAnimation absorption;
        private readonly System.Action<Texture2D, Rect, Rect, Color> drawTexture;
        public CastleSpellAnimation(System.Action<Texture2D, Rect, Rect, Color> drawTexture = null)
        { this.drawTexture = drawTexture; absorption = new MeteorAbsorptionAnimation(drawTexture); }
        public void Prepare() => Load();
        private void Load()
        {
            if (fireball != null) return;
            fireball = new BattleAnimationSheet("PixelArt/TyrantFireballRelease", 2, 1, false, drawTexture);
            throwingKing = new BattleAnimationSheet("PixelArt/TyrantMeteorThrow", 4, 1, true, drawTexture);
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
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return false;
            Load();
            return throwingKing.Draw(new Vector2(king.center.x, king.yMax), king.height,
                CastleMeteorPresentation.ThrowPose(elapsed), tint);
        }
        public static Vector2 ThrowImpactPoint => new Vector2(640, 240);
        public static Vector2 ThrowFireballCenter(Rect king, float elapsed)
        {
            Vector2 start = new Vector2(king.center.x, king.y - king.height * .45f);
            Vector2 overhead = new Vector2(king.center.x, king.y - king.height * .76f);
            Vector2 back = overhead + new Vector2(-king.height * .48f, king.height * .24f);
            if (elapsed < CastleMeteorPresentation.ThrowRaiseAt)
                return Vector2.Lerp(start, overhead, CastleMeteorPresentation.ThrowRaiseProgress(elapsed));
            if (elapsed < CastleMeteorPresentation.ThrowReleaseAt)
                return Vector2.Lerp(overhead, back, CastleMeteorPresentation.ThrowWindupProgress(elapsed));
            float t = CastleMeteorPresentation.ThrowFlightProgress(elapsed), inverse = 1 - t;
            Vector2 arc = new Vector2(555, 48);
            return inverse * inverse * back + 2 * inverse * t * arc + t * t * ThrowImpactPoint;
        }
        public void DrawMeteor(Rect king, Rect target, bool absorbed, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return;
            Load();
            if (absorbed)
            {
                absorption.DrawTransfer(king, target, elapsed);
                return;
            }
            // A esfera acompanha as duas maos, recua e depois e arremessada ao centro da arena.
            if (elapsed < CastleMeteorPresentation.ThrowImpactAt)
                fireball.DrawCentered(ThrowFireballCenter(king, elapsed),
                    king.height * CastleMeteorPresentation.FireballScale(elapsed), 1, Color.white);
        }
    }
}
