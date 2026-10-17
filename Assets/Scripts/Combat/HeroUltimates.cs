namespace DragonQuest.Combat
{
    public static class HeroUltimates
    {
        // Keep the command ID stable for saves/menus; each signature drives its own presentation.
        public static readonly AbilityDefinition Omnislash = new AbilityDefinition("limit-break", "Omnislash",
            "13 cortes de espada em UM inimigo; o corte final causa dano triplo. Consome 100% do LIMIT, sem MP.",
            0, 10, AbilityEffect.PhysicalDamage, TargetRule.Enemy, usesLimit: true,
            sourceAbilityId: "omnislash", hitCount: 13, finalHitMultiplier: 3);
        public static readonly AbilityDefinition Salvation = new AbilityDefinition("limit-break", "Aurora da Vida",
            "Cura 80% do HP maximo de TODOS os herois, remove efeitos negativos e revive os derrotados com 80% do HP. Consome 100% do LIMIT, sem MP.",
            0, 0, AbilityEffect.Heal, TargetRule.AllAllies, usesLimit: true, sourceAbilityId: "lia-salvation",
            partyEffect: PartyEffect.RestoreCleanseAndRevive, recoveryPercent: 80);
        public static readonly AbilityDefinition Aegis = new AbilityDefinition("limit-break", "Bastiao Inabalavel",
            "Primeiro um grito concede Shield; depois um golpe de escudo no chao atinge TODOS os inimigos. Shield reduz todo dano recebido pelos herois vivos em 50% por 3 turnos de cada aliado. Consome 100% do LIMIT, sem MP.",
            0, 65, AbilityEffect.PhysicalDamage, TargetRule.AllEnemies, usesLimit: true, sourceAbilityId: "bram-aegis",
            partyEffect: PartyEffect.DamageShield, shieldDuration: 3);

        public static AbilityDefinition ForHero(string id) => id == "lia" ? Salvation : id == "bram" ? Aegis : Omnislash;
    }
}
