using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class SummonAnimation
    {
        public const float StartDelay = .35f;
        public const float AttackDuration = 2.4f;
        public const float Duration = StartDelay + AttackDuration + .3f;
        public const float ImpactDelay = StartDelay + AttackDuration * .5f;
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();
        public static string Resource(AbilityDefinition ability)
        {
            if (ability == null || ability.Effect != AbilityEffect.MagicDamage) return null;
            switch (ability.SourceAbilityId)
            {
                case "summon": return "PhoenixSummon";
                case "bahamut": return "BahamutSummon";
                case "titan": return "TitanSummon";
                default: return null;
            }
        }
        public static int Frame(float elapsed) => Math.Max(0, Math.Min((int)((elapsed - StartDelay) / AttackDuration * 8), 7));
        public static string AttackFrameResource(AbilityDefinition ability) => Resource(ability) == "BahamutSummon"
            ? "BahamutAttack" : Resource(ability) == "PhoenixSummon" ? "PhoenixAttack" : null;
        // This selects visual impact art only; the battle's damage element is untouched.
        public static AbilityDefinition ImpactEffect(AbilityDefinition ability) => Resource(ability) == "PhoenixSummon"
            ? PrototypePartyFactory.Fire : Resource(ability) == "TitanSummon" ? PrototypePartyFactory.Earth
            : Resource(ability) == "BahamutSummon" ? PrototypePartyFactory.Chaos : null;
        public bool Draw(AbilityDefinition ability, float elapsed)
        {
            string resource = Resource(ability);
            if (resource == null || elapsed < StartDelay || elapsed >= StartDelay + AttackDuration) return false;
            if (!sheets.TryGetValue(resource, out BattleAnimationSheet sheet))
            {
                sheet = new BattleAnimationSheet("PixelArt/Summons/" + resource, 4, 2, false);
                string attack = AttackFrameResource(ability);
                if (attack != null && !sheet.ReplaceFrame(4, "PixelArt/Summons/" + attack))
                    Debug.LogWarning("Quadro de ataque completo ausente: " + attack);
                sheets[resource] = sheet;
            }
            float fade = Math.Min(1, Math.Min((elapsed - StartDelay) / .15f,
                (StartDelay + AttackDuration - elapsed) / .2f));
            return sheet.Draw(new Vector2(720, 390), 300, Frame(elapsed), new Color(1, 1, 1, Math.Max(0, fade)));
        }
    }
}
