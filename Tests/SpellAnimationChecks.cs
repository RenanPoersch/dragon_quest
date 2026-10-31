using DragonQuest.Presentation.Battle.Animations;
using DragonQuest.Content;
using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;

public static class SpellAnimationChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Main()
    {
        var cases = new[] {
            AbilityCatalog.Fire, AbilityCatalog.Ice, AbilityCatalog.Thunder,
            AbilityCatalog.Earth, AbilityCatalog.Chaos, AbilityCatalog.Heal,
            AbilityCatalog.Cleanse, AbilityCatalog.Revive };
        foreach (var original in cases)
        {
            Check(SpellAnimation.Resource(original) != null, "Missing spell: " + original.Id);
            var clone = new AbilityDefinition("instance/" + original.Id, original.Name + " + All", original.Description,
                original.MpCost, original.Power, original.Effect,
                original.Effect == AbilityEffect.MagicDamage ? TargetRule.AllEnemies : TargetRule.AllAllies,
                sourceAbilityId: original.SourceAbilityId, element: original.Element, menuGroup: AbilityMenuGroup.Magic);
            Check(SpellAnimation.Resource(clone) == SpellAnimation.Resource(original), "All must preserve animation: " + original.Id);
        }
        foreach (DamageElement element in new[] { DamageElement.Fire, DamageElement.Ice, DamageElement.Lightning, DamageElement.Earth, DamageElement.Chaos })
        {
            var attack = new AbilityDefinition("attack", "Ataque", "", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy, element: element);
            Check(SpellAnimation.Resource(attack) != null, "Infused attack animation.");
        }
        Check(SpellAnimation.Resource(MateriaResolver.Attack) == null, "Plain attacks retain existing sprites.");
        var summon = new AbilityDefinition("phoenix/summon", "Fenix", "", 12, 35, AbilityEffect.MagicDamage,
            TargetRule.Enemy, sourceAbilityId: "fire", element: DamageElement.Fire, menuGroup: AbilityMenuGroup.Summon);
        Check(SpellAnimation.Resource(summon) == null, "Summons retain their own presentation.");
        Check(SpellAnimation.Resource(null) == null, "Null is safe.");
        Check(SpellAnimation.Frame(-1) == 0 && SpellAnimation.Frame(.5f) == 4 && SpellAnimation.Frame(2) == 7, "Animation timing.");
        Console.WriteLine("Spell animation mapping, All, Infusion and timing checks passed.");
    }
}
