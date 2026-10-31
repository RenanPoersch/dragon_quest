using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    public sealed class HeroCastAnimation
    {
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();
        public static bool IsCasting(AbilityDefinition ability) => ability != null && !ability.UsesLimit
            && (ability.MenuGroup == AbilityMenuGroup.Magic || ability.MenuGroup == AbilityMenuGroup.Summon
                || ability.Effect == AbilityEffect.MagicDamage || ability.Effect == AbilityEffect.Heal
                || ability.Effect == AbilityEffect.Cleanse || ability.Effect == AbilityEffect.Revive);
        public static string Resource(string hero) => hero == "aren" ? "ArenCast"
            : hero == "lia" ? "LiaCast" : hero == "bram" ? "BramCast" : null;
        public static int Pose(float elapsed, float duration) => elapsed < .16f ? 0
            : elapsed < .34f ? 1 : elapsed < duration - .28f ? 2 : 3;
        public static float Step(float elapsed, float duration)
        {
            float progress = elapsed < .2f ? elapsed / .2f
                : elapsed < duration - .28f ? 1 : (duration - elapsed) / .28f;
            return -28 * Math.Max(0, Math.Min(progress, 1));
        }
        public bool Draw(Rect target, string hero, AbilityDefinition ability, float elapsed, float duration, Color tint)
        {
            string resource = Resource(hero);
            if (resource == null || !IsCasting(ability)) return false;
            if (!sheets.TryGetValue(hero, out BattleAnimationSheet sheet))
            { sheet = new BattleAnimationSheet("PixelArt/Cast/" + resource, 4, 1, true); sheets[hero] = sheet; }
            return sheet.Draw(new Vector2(target.center.x + Step(elapsed, duration), target.yMax),
                target.height, Pose(elapsed, duration), tint);
        }
    }
}
