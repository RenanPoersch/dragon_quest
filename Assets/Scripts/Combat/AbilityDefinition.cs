using System;

namespace DragonQuest.Combat
{
    public enum AbilityEffect { PhysicalDamage, Heal, Defend, Cleanse, Revive, Taunt, Protect, MagicDamage, Prepare, Drain, Bind, MindControl, Silence }
    public enum TargetRule { Enemy, Ally, Self, AllEnemies, AllAllies }
    public enum AbilityMenuGroup { Direct, Magic, Summon }
    public enum PartyEffect { None, RestoreAndRevive, DamageShield, RestoreCleanseAndRevive }

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
        public DamageElement Element { get; }
        public AbilityMenuGroup MenuGroup { get; }
        public PartyEffect PartyEffect { get; }
        public int RecoveryPercent { get; }
        public int ShieldDuration { get; }
        public int HitCount { get; }
        public int FinalHitMultiplier { get; }
        public bool IsMagic => MenuGroup == AbilityMenuGroup.Magic || MenuGroup == AbilityMenuGroup.Summon
            || Effect == AbilityEffect.MagicDamage || Effect == AbilityEffect.Drain
            || Effect == AbilityEffect.Heal || Effect == AbilityEffect.Cleanse || Effect == AbilityEffect.Revive
            || Effect == AbilityEffect.MindControl || Effect == AbilityEffect.Silence;

        public AbilityDefinition(string id, string name, string description, int mpCost, int power,
            AbilityEffect effect, TargetRule targets, int poisonTurns = 0, bool usesLimit = false, string sourceAbilityId = null,
            DamageElement element = DamageElement.None, AbilityMenuGroup menuGroup = AbilityMenuGroup.Direct,
            PartyEffect partyEffect = PartyEffect.None, int recoveryPercent = 0, int shieldDuration = 0,
            int hitCount = 1, int finalHitMultiplier = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Habilidade precisa de ID.", nameof(id));
            if (mpCost < 0 || power < 0) throw new ArgumentOutOfRangeException(nameof(mpCost));
            if (effect == AbilityEffect.Defend && targets != TargetRule.Self)
                throw new ArgumentException("Defesa deve ter o proprio usuario como alvo.");
            if (effect == AbilityEffect.Prepare && targets != TargetRule.Self)
                throw new ArgumentException("Preparacao exige o proprio usuario como alvo.");
            if (!Enum.IsDefined(typeof(AbilityEffect), effect) || !Enum.IsDefined(typeof(TargetRule), targets))
                throw new ArgumentException("Efeito ou alvo desconhecido.");
            if (!Enum.IsDefined(typeof(DamageElement), element)
                || (element != DamageElement.None && effect != AbilityEffect.PhysicalDamage && effect != AbilityEffect.MagicDamage && effect != AbilityEffect.Drain))
                throw new ArgumentException("Elemento valido so pode ser usado em habilidades de dano.");
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
            if (effect == AbilityEffect.Drain && targets != TargetRule.Enemy)
                throw new ArgumentException("Drenar vida exige um inimigo individual.");
            if (effect == AbilityEffect.Bind && (targets != TargetRule.Enemy || power != 1))
                throw new ArgumentException("Correntes exige um inimigo e uma prisao quebravel por acerto.");
            if ((effect == AbilityEffect.MindControl || effect == AbilityEffect.Silence)
                && (targets != TargetRule.Enemy || power != 1))
                throw new ArgumentException("Controle mental e silencio exigem um inimigo e duram exatamente uma acao.");
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
            Element = element;
            if (!Enum.IsDefined(typeof(AbilityMenuGroup), menuGroup)) throw new ArgumentException("Grupo de menu desconhecido.");
            MenuGroup = menuGroup;
            if (!Enum.IsDefined(typeof(PartyEffect), partyEffect)
                || (partyEffect == PartyEffect.RestoreAndRevive && effect != AbilityEffect.MagicDamage)
                || (partyEffect == PartyEffect.DamageShield && effect != AbilityEffect.MagicDamage && effect != AbilityEffect.PhysicalDamage)
                || (partyEffect == PartyEffect.RestoreCleanseAndRevive && (effect != AbilityEffect.Heal || targets != TargetRule.AllAllies || power != 0))
                || (partyEffect == PartyEffect.None && (recoveryPercent != 0 || shieldDuration != 0))
                || ((partyEffect == PartyEffect.RestoreAndRevive || partyEffect == PartyEffect.RestoreCleanseAndRevive)
                    && (recoveryPercent < 1 || recoveryPercent > 100 || shieldDuration != 0))
                || (partyEffect == PartyEffect.DamageShield && (shieldDuration < 1 || shieldDuration > 99 || recoveryPercent != 0)))
                throw new ArgumentException("Efeito de equipe invalido.");
            PartyEffect = partyEffect; RecoveryPercent = recoveryPercent; ShieldDuration = shieldDuration;
            if (hitCount < 1 || hitCount > 32 || finalHitMultiplier < 1 || finalHitMultiplier > 10
                || (hitCount == 1 && finalHitMultiplier != 1)
                || (hitCount > 1 && (effect != AbilityEffect.PhysicalDamage || targets != TargetRule.Enemy)))
                throw new ArgumentException("Sequencia de golpes exige dano fisico em um unico inimigo.");
            HitCount = hitCount; FinalHitMultiplier = finalHitMultiplier;
        }
    }
}
