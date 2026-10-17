using System;
using System.Collections.Generic;
using DragonQuest.Combat;

namespace DragonQuest.Equipment
{
    public static class MateriaResolver
    {
        public static readonly AbilityDefinition Attack = new AbilityDefinition("attack", "Ataque", "Ataque basico, sem materia.", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Defend = new AbilityDefinition("defend", "Defender", "Reduz o dano recebido ate o proximo turno.", 0, 0, AbilityEffect.Defend, TargetRule.Self);
        public static readonly AbilityDefinition LimitBreak = HeroUltimates.Omnislash;

        // A weapon has one basic-attack element; the first valid linked pair wins.
        public static DamageElement InfusedElement(EquipmentLoadout equipment)
        {
            foreach (DamageElement element in InfusedElements(equipment)) return element;
            return DamageElement.None;
        }

        public static IEnumerable<DamageElement> InfusedElements(EquipmentLoadout equipment)
        {
            if (equipment == null) yield break;
            for (int i = 0; i < equipment.Sockets.Count; i++)
            {
                var source = equipment.Sockets[i];
                var linked = equipment.Sockets[equipment.LinkedSocket(i)];
                if (source == null || linked == null || linked.Definition.Modifier != MateriaModifier.Infuse) continue;
                if (source.Definition.Type != MateriaType.Magic && source.Definition.Type != MateriaType.Summon) continue;
                foreach (var ability in source.Definition.Abilities)
                    if (ability.Effect == AbilityEffect.MagicDamage && ability.Element != DamageElement.None)
                        yield return ability.Element;
            }
        }

        public static List<AbilityDefinition> Resolve(PartyMember member)
        {
                        DamageElement element = InfusedElement(member.Weapon);
            var basicAttack = element == DamageElement.None ? Attack : new AbilityDefinition("attack", "Ataque",
                "Ataque basico com Infusao de " + ElementalAffinities.NameOf(element) + ". Sem custo de MP.",
                0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy, element: element);
            var commands = new List<AbilityDefinition> { basicAttack, Defend, HeroUltimates.ForHero(member.Id) };
            foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
            {
                if (equipment == null) continue;
                for (int i = 0; i < equipment.Sockets.Count; i++)
                {
                    MateriaInstance source = equipment.Sockets[i];
                    if (source == null) continue;
                    MateriaInstance linked = equipment.Sockets[equipment.LinkedSocket(i)];
                    foreach (AbilityDefinition original in source.Definition.Abilities)
                    {
                        AbilityEffect effect = original.Effect;
                        TargetRule targets = original.Targets;
                        int cost = original.MpCost, power = original.Power;
                        string name = original.Name;
                        string details = original.Description + " Origem: " + source.Definition.Name + ".";
                        bool comboSource = source.Definition.Type == MateriaType.Magic || source.Definition.Type == MateriaType.Summon;
                        bool supportsAll = effect == AbilityEffect.PhysicalDamage || effect == AbilityEffect.MagicDamage
                            || effect == AbilityEffect.Heal || effect == AbilityEffect.Cleanse || effect == AbilityEffect.Revive;
                        if (comboSource && supportsAll && linked != null && linked.Definition.Modifier == MateriaModifier.All)
                        {
                            if (targets == TargetRule.Enemy || targets == TargetRule.Ally)
                            {
                                targets = targets == TargetRule.Enemy ? TargetRule.AllEnemies : TargetRule.AllAllies;
                                cost = (int)Math.Min(int.MaxValue, ((long)cost * 3 + 1) / 2);
                                name += " + All";
                                details += " Combo: todos os alvos validos, custo de MP x1,5.";
                            }
                        }

                        if (member.Class == CharacterClass.Healer && (effect == AbilityEffect.MagicDamage || effect == AbilityEffect.Heal))
                        {
                            power = (int)Math.Min(int.MaxValue, (long)power * 125 / 100);
                            details += " Afinidade: +25% no poder base.";
                        }
                        commands.Add(new AbilityDefinition(source.Id + "/" + original.Id, name, details, cost, power,
                            effect, targets, original.PoisonTurns, original.UsesLimit, original.SourceAbilityId, original.Element,
                            source.Definition.Type == MateriaType.Magic ? AbilityMenuGroup.Magic
                                : source.Definition.Type == MateriaType.Summon ? AbilityMenuGroup.Summon : AbilityMenuGroup.Direct,
                            original.PartyEffect, original.RecoveryPercent, original.ShieldDuration, original.HitCount, original.FinalHitMultiplier));
                    }
                }
            }
            return commands;
        }
    }
}
