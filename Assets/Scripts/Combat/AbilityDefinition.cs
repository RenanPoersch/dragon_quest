using System;

namespace DragonQuest.Combat
{
    public enum AbilityEffect { PhysicalDamage, Heal, Defend, Cleanse, Revive, Taunt, Protect, MagicDamage }
    public enum TargetRule { Enemy, Ally, Self, AllEnemies, AllAllies }

    public sealed class AbilityDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int MpCost { get; }
        public int Power { get; }
        public AbilityEffect Effect { get; }
        public TargetRule Targets { get; }
        public int PoisonTurns { get; }
        public bool UsesLimit { get; }
        public string SourceAbilityId { get; }

        public AbilityDefinition(string id, string name, string description, int mpCost, int power,
            AbilityEffect effect, TargetRule targets, int poisonTurns = 0, bool usesLimit = false, string sourceAbilityId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Habilidade precisa de ID.", nameof(id));
            if (mpCost < 0 || power < 0) throw new ArgumentOutOfRangeException(nameof(mpCost));
            if (effect == AbilityEffect.Defend && targets != TargetRule.Self)
                throw new ArgumentException("Defesa deve ter o proprio usuario como alvo.");
            if (!Enum.IsDefined(typeof(AbilityEffect), effect) || !Enum.IsDefined(typeof(TargetRule), targets))
                throw new ArgumentException("Efeito ou alvo desconhecido.");
            if (poisonTurns < 0 || (poisonTurns > 0 && effect != AbilityEffect.PhysicalDamage))
                throw new ArgumentException("Veneno exige um ataque fisico e duracao positiva.");
            if (targets == TargetRule.AllEnemies && effect != AbilityEffect.PhysicalDamage && effect != AbilityEffect.MagicDamage)
                throw new ArgumentException("Ataque em area exige dano.");
            if ((effect == AbilityEffect.Heal || effect == AbilityEffect.Cleanse || effect == AbilityEffect.Revive)
                && targets != TargetRule.Ally && targets != TargetRule.AllAllies)
                throw new ArgumentException("Esta habilidade exige um aliado como alvo.");
            if (effect == AbilityEffect.Protect && targets != TargetRule.Ally)
                throw new ArgumentException("Proteger exige um aliado individual.");
            if (targets == TargetRule.AllAllies && effect != AbilityEffect.Heal && effect != AbilityEffect.Cleanse && effect != AbilityEffect.Revive)
                throw new ArgumentException("Area aliada exige cura, purificacao ou ressuscitar.");
            if (effect == AbilityEffect.Taunt && (targets != TargetRule.Enemy || power < 1))
                throw new ArgumentException("Provocar exige um inimigo e duracao positiva.");
            if (effect == AbilityEffect.Revive && (power < 1 || power > 100))
                throw new ArgumentException("Reviver exige uma porcentagem de HP entre 1 e 100.");
            Id = id;
            Name = name;
            Description = description;
            MpCost = mpCost;
            Power = power;
            Effect = effect;
            Targets = targets;
            PoisonTurns = poisonTurns;
            UsesLimit = usesLimit;
            SourceAbilityId = sourceAbilityId ?? id;
        }
    }
}
