using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public enum BattleCommandKind { Ability, Magic, Summon, Items }

    public sealed class BattleMenuCommand
    {
        public BattleCommandKind Kind { get; }
        public AbilityDefinition Ability { get; }
        public string Label { get; }
        public BattleMenuCommand(BattleCommandKind kind, string label, AbilityDefinition ability = null)
        { Kind = kind; Label = label; Ability = ability; }
    }

    // Groups commands by materia provenance; defense replaces the attack row on demand.
    public static class BattleCommandMenu
    {
        public static float KeepRowVisible(float scroll, int index, int count, float viewportHeight)
        {
            const float rowSpacing = 36, rowHeight = 34;
            if (count <= 0) return 0;
            index = System.Math.Max(0, System.Math.Min(index, count - 1));
            float top = index * rowSpacing;
            float bottom = top + rowHeight;
            if (top < scroll) scroll = top;
            else if (bottom > scroll + viewportHeight) scroll = bottom - viewportHeight;
            return System.Math.Max(0, System.Math.Min(scroll, System.Math.Max(0, count * rowSpacing - viewportHeight)));
        }

        public static List<AbilityDefinition> Group(CombatantState actor, AbilityMenuGroup group)
        {
            var result = new List<AbilityDefinition>();
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (!ability.UsesLimit && ability.MenuGroup == group) result.Add(ability);
            return result;
        }

        public static AbilityDefinition Defend(CombatantState actor)
        {
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (!ability.UsesLimit && ability.Effect == AbilityEffect.Defend) return ability;
            return null;
        }

        public static List<BattleMenuCommand> Root(CombatantState actor)
        {
            var result = new List<BattleMenuCommand>();
            AbilityDefinition attack = null, limit = null;
            bool magic = false, summon = false;
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
            {
                if (ability.SourceAbilityId == "attack" && !ability.UsesLimit) attack = ability;
                if (ability.UsesLimit && limit == null) limit = ability;
                if (!ability.UsesLimit) { magic |= ability.MenuGroup == AbilityMenuGroup.Magic; summon |= ability.MenuGroup == AbilityMenuGroup.Summon; }
            }
            if (actor.Limit.IsReady && limit != null) result.Add(new BattleMenuCommand(BattleCommandKind.Ability, "LIMIT", limit));
            else if (attack != null) result.Add(new BattleMenuCommand(BattleCommandKind.Ability, attack.Name, attack));
            if (magic) result.Add(new BattleMenuCommand(BattleCommandKind.Magic, "Magic"));
            if (summon) result.Add(new BattleMenuCommand(BattleCommandKind.Summon, "Summon"));
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (ability != attack && !ability.UsesLimit && ability.Effect != AbilityEffect.Defend
                    && ability.MenuGroup == AbilityMenuGroup.Direct)
                    result.Add(new BattleMenuCommand(BattleCommandKind.Ability, ability.Name, ability));
            result.Add(new BattleMenuCommand(BattleCommandKind.Items, "Itens"));
            return result;
        }
    }
}
