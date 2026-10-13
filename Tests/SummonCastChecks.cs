using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Prototype;

public static class SummonCastChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Main()
    {
        var originals = new[] { PrototypePartyFactory.Summon, PrototypePartyFactory.Bahamut, PrototypePartyFactory.Titan };
        var resources = new[] { "PhoenixSummon", "BahamutSummon", "TitanSummon" };
        for (int i = 0; i < originals.Length; i++)
        {
            var original = originals[i];
            Check(SummonAnimation.Resource(original) == resources[i], "Summon resource.");
            var command = new AbilityDefinition("instance/" + original.Id, original.Name + " + All", original.Description,
                original.MpCost, original.Power, original.Effect, TargetRule.AllEnemies,
                sourceAbilityId: original.SourceAbilityId, element: original.Element, menuGroup: AbilityMenuGroup.Summon);
            Check(SummonAnimation.Resource(command) == resources[i], "Resolved summon + All keeps its animation.");
            Check(HeroCastAnimation.IsCasting(command), "Summons trigger cast.");
            Check(SpellAnimation.Resource(SummonAnimation.ImpactEffect(command)) != null, "Summon target impact has art.");
        }
        foreach (var spell in new[] { PrototypePartyFactory.Fire, PrototypeBattleFactory.Heal,
            PrototypeBattleFactory.Cleanse, PrototypeBattleFactory.Revive })
            Check(HeroCastAnimation.IsCasting(spell), "Offensive and restorative spells trigger cast.");
        foreach (string hero in new[] { "aren", "lia", "bram" })
            Check(HeroCastAnimation.Resource(hero) != null, "Every hero has a cast sheet.");
        Check(HeroCastAnimation.Resource("guard") == null, "Enemy does not use a hero sheet.");
        Check(!HeroCastAnimation.IsCasting(MateriaResolver.Attack), "Normal attack preserves weapon animation.");
        var infused = new AbilityDefinition("attack", "Ataque", "", 0, 0, AbilityEffect.PhysicalDamage,
            TargetRule.Enemy, element: DamageElement.Fire);
        Check(!HeroCastAnimation.IsCasting(infused) && SummonAnimation.Resource(infused) == null, "Infusion remains a weapon attack.");
        Check(SummonAnimation.Resource(PrototypePartyFactory.Fire) == null, "Magic is not a summon.");
        Check(HeroCastAnimation.Pose(0, SummonAnimation.Duration) == 0 && HeroCastAnimation.Pose(.25f, SummonAnimation.Duration) == 1,
            "Idle and step poses.");
        Check(HeroCastAnimation.Pose(SummonAnimation.ImpactDelay, SummonAnimation.Duration) == 2,
            "Both hands stay raised until summon impact.");
        Check(HeroCastAnimation.Pose(SummonAnimation.Duration, SummonAnimation.Duration) == 3
            && HeroCastAnimation.Step(SummonAnimation.Duration, SummonAnimation.Duration) == 0, "Hero returns to normal position.");
        Check(HeroCastAnimation.Step(.25f, SummonAnimation.Duration) < 0, "Hero advances toward enemies.");
        Check(SummonAnimation.Frame(SummonAnimation.StartDelay) == 0 && SummonAnimation.Frame(SummonAnimation.ImpactDelay) == 4,
            "Summon emerges after cast and impact matches attack frame.");
        Check(SummonAnimation.Frame(SummonAnimation.Duration) == 7 && SummonAnimation.Duration > SummonAnimation.ImpactDelay,
            "Summon completes attack and recovery.");
        Check(PrototypePartyFactory.Bahamut.Element == DamageElement.None, "Visual impact must not change Bahamut damage element.");
        Check(SummonAnimation.AttackFrameResource(PrototypePartyFactory.Bahamut) == "BahamutAttack"
            && SummonAnimation.AttackFrameResource(PrototypePartyFactory.Summon) == "PhoenixAttack", "Complete attack images replace the clipped impact frames.");
        Check(SummonAnimation.AttackFrameResource(PrototypePartyFactory.Titan) == null, "Titan retains its original attack sheet.");
        Console.WriteLine("Summon, All, hero cast, Infusion and timing checks passed.");
    }
}
