using DragonQuest.Combat;

namespace DragonQuest.Content
{
    public static class AbilityCatalog
    {
        public static readonly AbilityDefinition Attack = new AbilityDefinition("attack", "Ataque", "Ataque basico, sem materia.", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Defend = new AbilityDefinition("defend", "Defender", "Reduz o dano recebido ate o proximo turno.", 0, 0, AbilityEffect.Defend, TargetRule.Self);
        public static readonly AbilityDefinition HeavyStrike = new AbilityDefinition("heavy-strike", "Golpe forte",
            "Ataque fisico mais poderoso. Custa 4 MP.", 4, 14, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Heal = new AbilityDefinition("heal", "Curar",
            "Recupera HP de um aliado vivo e ferido. Custa 6 MP.", 6, 18, AbilityEffect.Heal, TargetRule.Ally);
        public static readonly AbilityDefinition Cleave = new AbilityDefinition("cleave", "Corte amplo",
            "Atinge todos os inimigos vivos. Custa 7 MP uma unica vez.", 7, 5, AbilityEffect.PhysicalDamage, TargetRule.AllEnemies);
        public static readonly AbilityDefinition Cleanse = new AbilityDefinition("cleanse", "Purificar",
            "Remove o veneno de um aliado vivo. Custa 4 MP.", 4, 0, AbilityEffect.Cleanse, TargetRule.Ally);
        public static readonly AbilityDefinition Revive = new AbilityDefinition("revive", "Reviver",
            "Revive um aliado derrotado com 30% do HP maximo. Custa 12 MP.", 12, 30, AbilityEffect.Revive, TargetRule.Ally);
        public static readonly AbilityDefinition Taunt = new AbilityDefinition("taunt", "Provocar",
            "Atrai os golpes individuais de um inimigo por 2 turnos dele. Ataques em area atingem a equipe.", 3, 2, AbilityEffect.Taunt, TargetRule.Enemy);
        public static readonly AbilityDefinition Protect = new AbilityDefinition("protect", "Proteger",
            "Recebe o proximo golpe fisico individual dirigido ao aliado. Expira no proximo turno do usuario; area, magia e veneno ja aplicado nao sao interceptados.", 4, 0, AbilityEffect.Protect, TargetRule.Ally);
        public static readonly AbilityDefinition PoisonStrike = new AbilityDefinition("poison-strike", "Golpe venenoso",
            "Ataque individual que envenena ate ser curado ou o alvo chegar a 0 HP. Veneno: 5% do HP maximo por turno e a cada 3s no mapa.", 4, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy, 3);
        public static readonly AbilityDefinition EnemySweep = new AbilityDefinition("enemy-sweep", "Varredura",
            "Ataque contra todos os aliados vivos.", 5, 0, AbilityEffect.PhysicalDamage, TargetRule.AllEnemies);
        public static readonly AbilityDefinition DragonClaw = new AbilityDefinition("dragon-claw", "Garra draconica",
            "Golpe fisico individual.",0,5,AbilityEffect.PhysicalDamage,TargetRule.Enemy);
        public static readonly AbilityDefinition ChargeBreath = new AbilityDefinition("charge-breath", "Preparar sopro",
            "concentra chamas! No proximo turno dele, o sopro atingira toda a equipe. Defenda-se!",0,0,AbilityEffect.Prepare,TargetRule.Self);
        public static readonly AbilityDefinition DragonBreath = new AbilityDefinition("dragon-breath", "Sopro de fogo",
            "Fogo em toda a equipe depois de um turno de preparacao.",0,36,AbilityEffect.MagicDamage,TargetRule.AllEnemies,element:DamageElement.Fire);
        public static readonly AbilityDefinition Thunder = new AbilityDefinition("thunder", "Thunder",
            "Magia de Raio contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Lightning);
        public static readonly AbilityDefinition Fire = new AbilityDefinition("fire", "Fogo",
            "Magia de Fogo contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Fire);
        public static readonly AbilityDefinition Ice = new AbilityDefinition("ice", "Gelo",
            "Magia de Gelo contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Ice);
        public static readonly AbilityDefinition Summon = new AbilityDefinition("summon", "Invocar Fenix",
            "Revive e recupera 80% do HP maximo de cada aliado; dano baixo de Fogo em todos os inimigos. Custa 12 MP.", 12, 10, AbilityEffect.MagicDamage, TargetRule.AllEnemies,
            element: DamageElement.Fire, partyEffect: PartyEffect.RestoreAndRevive, recoveryPercent: 80);
        public static readonly AbilityDefinition Earth = new AbilityDefinition("earth", "Terra", "Magia de Terra, poder 22, 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Earth);
        public static readonly AbilityDefinition Chaos = new AbilityDefinition("chaos", "Caos", "Magia de Caos, poder 28, 8 MP.", 8, 28, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Chaos);
        public static readonly AbilityDefinition Bahamut = new AbilityDefinition("bahamut", "Invocar Bahamut", "Dano alto de Caos em todos os inimigos, poder 70, 20 MP.", 20, 70, AbilityEffect.MagicDamage, TargetRule.AllEnemies, element: DamageElement.Chaos);
        public static readonly AbilityDefinition Titan = new AbilityDefinition("titan", "Invocar Titan", "Dano baixo de Terra e Shield: reduz todo dano recebido pela equipe em 50% por 3 turnos de cada aliado. 16 MP.", 16, 10, AbilityEffect.MagicDamage, TargetRule.AllEnemies, element: DamageElement.Earth, partyEffect: PartyEffect.DamageShield, shieldDuration: 3);
    }
}
