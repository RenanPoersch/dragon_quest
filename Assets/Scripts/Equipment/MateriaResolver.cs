using System;
using System.Collections.Generic;
using DragonQuest.Combat;

namespace DragonQuest.Equipment
{
    public static class MateriaResolver
    {
        public static readonly AbilityDefinition Attack = new AbilityDefinition("attack", "Ataque", "Ataque basico, sem materia.", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Defend = new AbilityDefinition("defend", "Defender", "Reduz o dano recebido ate o proximo turno.", 0, 0, AbilityEffect.Defend, TargetRule.Self);
        public static readonly AbilityDefinition LimitBreak = new AbilityDefinition("limit-break", "Limit Break", "Usa toda a barra de Limit para atingir todos os inimigos. Nao gasta MP.", 0, 35, AbilityEffect.PhysicalDamage, TargetRule.AllEnemies, usesLimit: true);

        public static List<AbilityDefinition> Resolve(PartyMember member)
        {
            var commands = new List<AbilityDefinition> { Attack, Defend, LimitBreak };
            foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
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
                        else if (comboSource && linked != null && linked.Definition.Modifier == MateriaModifier.Infuse && effect == AbilityEffect.MagicDamage)
                        {
                            effect = AbilityEffect.PhysicalDamage;
                            name += " + Infusao";
                            details += " Combo: golpe fisico com o poder da magia/invocacao; usa ataque e pode ser interceptado.";
                        }
                        if (member.Class == CharacterClass.Healer && (effect == AbilityEffect.MagicDamage || effect == AbilityEffect.Heal))
                        {
                            power = (int)Math.Min(int.MaxValue, (long)power * 125 / 100);
                            details += " Afinidade: +25% no poder base.";
                        }
                        commands.Add(new AbilityDefinition(source.Id + "/" + original.Id, name, details, cost, power,
                            effect, targets, original.PoisonTurns, original.UsesLimit, original.SourceAbilityId));
                    }
                }
            return commands;
        }
    }
}
