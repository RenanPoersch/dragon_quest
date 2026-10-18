using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // All layers are transparent, point-filtered sprite atlases drawn in the battle's 2D plane.
    public sealed class MeteorAbsorptionAnimation
    {
        private BattleAnimationSheet hero, release, ribbons, aura;
        private readonly Action<Texture2D, Rect, Rect, Color> drawTexture;

        public MeteorAbsorptionAnimation(Action<Texture2D, Rect, Rect, Color> drawTexture = null)
        { this.drawTexture = drawTexture; }

        public void Prepare()
        {
            if (hero != null) return;
            hero = new BattleAnimationSheet("PixelArt/ArenMeteorDraconic", 4, 1, false, drawTexture);
            release = new BattleAnimationSheet("PixelArt/TyrantFireballRelease", 2, 1, false, drawTexture);
            ribbons = new BattleAnimationSheet("PixelArt/MeteorFireRibbons", 4, 1, false, drawTexture);
            aura = new BattleAnimationSheet("PixelArt/DraconicAbsorptionAura", 4, 1, false, drawTexture);
        }

        public static Rect BearerRect(Rect ground, float elapsed)
        {
            float advance = CastleMeteorPresentation.AbsorberAdvance(elapsed);
            ground.x -= 215 * advance;
            ground.y -= 100 * advance + (elapsed < CastleMeteorPresentation.AbsorberReturn
                ? Mathf.Sin(elapsed * 5) * 2 * advance : 0);
            return ground;
        }

        public static Vector2 FireballCenter(Rect king)
            => new Vector2(king.center.x + king.height * .43f, king.y - king.height * .1f);

        public bool DrawBearer(Rect airborne, string id, float elapsed, Color tint)
        {
            if (id != "aren" || elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return false;
            Prepare();
            DrawAura(airborne, elapsed, false);
            return hero.Draw(new Vector2(airborne.center.x, airborne.yMax), airborne.height,
                CastleMeteorPresentation.AbsorberPose(elapsed), tint);
        }

        public bool DrawCaster(Rect king, float elapsed, Color tint)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return false;
            Prepare();
            return release.Draw(new Vector2(king.center.x, king.yMax), king.height, 0, tint);
        }

        public void DrawTransfer(Rect king, Rect airborne, float elapsed)
        {
            if (elapsed < 0 || elapsed >= CastleMeteorPresentation.Duration) return;
            Prepare();
            if (elapsed >= CastleMeteorPresentation.AbsorptionEnd)
            {
                DrawAura(airborne, elapsed, true);
                return;
            }
            Vector2 source = FireballCenter(king);
            Vector2 chest = new Vector2(airborne.center.x - airborne.width * .45f,
                airborne.y + airborne.height * .43f);
            float progress = CastleMeteorPresentation.AbsorptionProgress(elapsed);
            if (elapsed >= CastleMeteorPresentation.AbsorptionStart)
            {
                float fade = Mathf.Min(1, (elapsed - CastleMeteorPresentation.AbsorptionStart) / .15f)
                    * Mathf.Clamp01((CastleMeteorPresentation.AbsorptionEnd - elapsed) / .25f);
                // Each atlas frame contains separate curling strips; no solid beam or 3D particles.
                int frame = (int)((elapsed - CastleMeteorPresentation.AbsorptionStart) * 12) % 4;
                float height = Mathf.Lerp(82, 34, progress);
                ribbons.DrawBetween(source, chest, height, frame, new Color(1, 1, 1, fade));
            }
            float size = 1 - progress;
            if (size > .01f)
                release.DrawCentered(source, king.height * size, 1, Color.white);
            // The front arcs receive the stream after it has reached the armor's chest.
            DrawAura(airborne, elapsed, true);
        }

        private void DrawAura(Rect airborne, float elapsed, bool foreground)
        {
            float opacity = Mathf.Clamp01((elapsed - .12f) / .38f)
                * Mathf.Clamp01((3.25f - elapsed) / .55f);
            if (opacity <= 0) return;
            bool absorbing = elapsed >= CastleMeteorPresentation.AbsorptionStart
                && elapsed < CastleMeteorPresentation.AbsorptionEnd;
            float pulse = absorbing ? 1 + Mathf.Sin(elapsed * 13) * .025f : 1;
            int frame = (int)(Mathf.Max(0, elapsed - .12f) * 10) % 4;
            aura.DrawCentered(new Vector2(airborne.center.x - 8, airborne.center.y),
                airborne.height * 1.45f * pulse, frame,
                new Color(1, 1, 1, opacity * (foreground ? .38f : .82f)));
        }
    }
}
