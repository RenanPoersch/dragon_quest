using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class SpiritAttackAnimation
    {
        private readonly Dictionary<string, SpiritBossSpriteSheet> sheets = new Dictionary<string, SpiritBossSpriteSheet>();
        private readonly Action<Texture2D, Rect, Rect, Color> render;
        public SpiritAttackAnimation(Action<Texture2D, Rect, Rect, Color> draw = null) { render = draw ?? DrawGui; }
        private static void DrawGui(Texture2D texture, Rect rect, Rect uv, Color tint)
        {
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true); GUI.color = previous;
        }
        public void Prepare()
        {
            foreach (string resource in SpiritAttackTimeline.Resources)
                if (!sheets.ContainsKey(resource)) sheets[resource] = new SpiritBossSpriteSheet(resource, render);
        }
        private SpiritBossSpriteSheet Sheet(AbilityDefinition ability)
        {
            string resource = SpiritAttackTimeline.Resource(ability);
            if (resource == null) return null;
            if (!sheets.TryGetValue(resource, out var sheet)) sheets[resource] = sheet = new SpiritBossSpriteSheet(resource, render);
            return sheet;
        }
        private void Fill(Rect area, Color color) => render(Texture2D.whiteTexture, area, new Rect(0, 0, 1, 1), color);
        public void DrawBackdrop(Rect arena, AbilityDefinition ability, float time)
        {
            if (SpiritAttackTimeline.Skill(ability) < 0) return;
            float opacity = SpiritAttackTimeline.Opacity(ability, time);
            Fill(arena, new Color(.015f, .005f, .045f, opacity * .56f));
            float impactAge = time - SpiritAttackTimeline.Impact(ability);
            if (impactAge >= 0 && impactAge < .1f)
                Fill(arena, new Color(.65f, .72f, 1f, (1 - impactAge / .1f) * .22f));
        }
        public bool Draw(Rect home, AbilityDefinition ability, float time, Color tint)
        {
            var sheet = Sheet(ability);
            if (sheet == null) return false;
            float charge = Mathf.Clamp01(time / SpiritAttackTimeline.Impact(ability));
            float release = Mathf.Clamp01((time - SpiritAttackTimeline.Impact(ability)) / .45f);
            float strength = Mathf.Sin(charge * Mathf.PI * .5f) * (1 - release);
            float scale = 1;
            switch (SpiritAttackTimeline.Skill(ability))
            {
                case 0: home.y -= strength * 25; scale += strength * .15f; break;
                case 1: home.y -= Mathf.Sin(charge * Mathf.PI) * 28; home.y += Mathf.Sin(release * Mathf.PI) * 12; break;
                case 2: home.x -= strength * 28; scale += strength * .22f; break;
                case 3: home.x += Mathf.Sin(charge * Mathf.PI) * 34 - Mathf.Sin(release * Mathf.PI) * 15; break;
                case 4: home.y -= strength * 35; scale += strength * .42f; break;
            }
            float height = Mathf.Min(home.height * scale, home.yMax - 24);
            home = new Rect(home.center.x - home.width * scale / 2, home.yMax - height,
                home.width * scale, height);
            sheet.DrawActor(home, SpiritAttackTimeline.ActorFrame(ability, time), tint);
            return true;
        }
        public static Vector2 ScreenOffset(AbilityDefinition ability, float time)
        {
            float amount = SpiritAttackTimeline.Shake(ability, time);
            return new Vector2(Mathf.Sin(time * 89) * amount, Mathf.Sin(time * 67) * amount * .45f);
        }
        public static Vector2 TargetOffset(AbilityDefinition ability, float time)
        {
            float age = time - SpiritAttackTimeline.Impact(ability);
            if (age < 0 || age > .4f) return Vector2.zero;
            float amount = 1 - age / .4f;
            int skill = SpiritAttackTimeline.Skill(ability);
            return new Vector2(Mathf.Sin(age * 75) * amount * (skill == 3 ? 12 : 4),
                skill == 0 ? -Mathf.Sin(age * Mathf.PI / .4f) * 12 : skill == 1 ? amount * 6 : 0);
        }
        public void DrawEffects(Rect caster, IReadOnlyList<Rect> targets, AbilityDefinition ability, float time)
        {
            var sheet = Sheet(ability);
            float alpha = SpiritAttackTimeline.Opacity(ability, time);
            if (sheet == null || alpha <= 0 || targets.Count == 0) return;
            int skill = SpiritAttackTimeline.Skill(ability), frame = SpiritAttackTimeline.EffectFrame(ability, time);
            Color tint = new Color(1, 1, 1, alpha);
            if (skill == 4)
            {
                // One catastrophe covers the whole party, including when the action has three targets.
                Vector2 center = Vector2.zero;
                foreach (Rect target in targets) center += target.center;
                center /= targets.Count;
                float growth = Mathf.SmoothStep(.48f, 1f, Mathf.Clamp01((time - .65f) / 1.9f));
                sheet.DrawEffect(center, 330 * growth, frame, tint);
                return;
            }
            foreach (Rect target in targets)
            {
                if (skill == 0)
                {
                    bool fading = frame >= 6;
                    sheet.DrawEffect(new Vector2(target.center.x, fading ? target.y - 20 : Mathf.Clamp(target.y + 8, 152, 288)),
                        fading ? 130 : 245, frame, tint);
                }
                else if (skill == 1)
                    sheet.DrawEffect(frame >= 6 ? new Vector2(target.center.x, target.y - 20)
                        : new Vector2(target.center.x, Mathf.Min(target.center.y, 300)),
                        frame >= 6 ? 300 : 220, frame, tint);
                else if (skill == 2)
                {
                    // Torn souls arc from the victim into the open chest. HP returns on the first arrival.
                    for (int soul = 0; soul < 3; soul++)
                    {
                        float progress = Mathf.Clamp01((time - .65f - soul * .18f)
                            / (SpiritAttackTimeline.Impact(ability) - .65f));
                        if (progress <= 0 || time > SpiritAttackTimeline.Impact(ability) + .6f + soul * .18f) continue;
                        Vector2 point = Vector2.Lerp(target.center, new Vector2(caster.center.x, caster.y + caster.height * .4f),
                            Mathf.SmoothStep(0, 1, progress));
                        point.y -= Mathf.Sin(progress * Mathf.PI) * (80 + soul * 25);
                        Color soulTint = tint; soulTint.a *= 1 - .6f * soul / 3;
                        sheet.DrawEffect(point, Mathf.Lerp(165, 70, progress), frame, soulTint);
                    }
                }
                else if (skill == 3) sheet.DrawEffect(new Vector2(target.center.x, Mathf.Min(target.center.y, 310)), 210, frame, tint);
            }
        }
    }
}
