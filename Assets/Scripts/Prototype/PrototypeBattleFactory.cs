using DragonQuest.Combat;

namespace DragonQuest.Prototype
{
    // Dados iniciais em código; poderão vir de ScriptableObjects nas próximas etapas.
    public static class PrototypeBattleFactory
    {
        public static readonly AbilityDefinition Attack = new AbilityDefinition("attack", "Ataque",
            "Ataque fisico contra um inimigo. Nao consome MP.", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Defend = new AbilityDefinition("defend", "Defender",
            "Reduz o dano recebido pela metade ate o inicio do proximo turno.", 0, 0, AbilityEffect.Defend, TargetRule.Self);
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
            "Recebe o proximo golpe individual dirigido ao aliado. Expira no proximo turno de Bram; area e veneno ja aplicado nao sao interceptados.", 4, 0, AbilityEffect.Protect, TargetRule.Ally);
        public static readonly AbilityDefinition PoisonStrike = new AbilityDefinition("poison-strike", "Golpe venenoso",
            "Ataque individual que envenena por 3 turnos do afetado.", 4, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy, 3);
        public static readonly AbilityDefinition EnemySweep = new AbilityDefinition("enemy-sweep", "Varredura",
            "Ataque contra todos os aliados vivos.", 5, 0, AbilityEffect.PhysicalDamage, TargetRule.AllEnemies);

        public static BattleSession CreateEncounter()
        {
            var attacker = new CombatantDefinition("Aren", "Atacante", 100, 28, 22, 7, 4, 14, Attack, HeavyStrike, Cleave, Defend);
            var healer = new CombatantDefinition("Lia", "Healer", 80, 60, 9, 5, 20, 12, Attack, Heal, Cleanse, Revive, Defend);
            var tank = new CombatantDefinition("Bram", "Tank", 150, 30, 14, 13, 3, 8, Attack, Taunt, Protect, Defend);
            var guard = new CombatantDefinition("Guarda do tirano", "Inimigo", 180, 24, 18, 8, 0, 9, PoisonStrike, Attack, EnemySweep, Defend);
            var soldier = new CombatantDefinition("Soldado", "Inimigo", 90, 0, 15, 6, 0, 7, Attack, Defend);
            return new BattleSession(
                new CombatantState("aren", attacker, CombatTeam.Party),
                new CombatantState("lia", healer, CombatTeam.Party),
                new CombatantState("bram", tank, CombatTeam.Party),
                new CombatantState("guard", guard, CombatTeam.Enemy),
                new CombatantState("soldier", soldier, CombatTeam.Enemy));
        }
    }
}
