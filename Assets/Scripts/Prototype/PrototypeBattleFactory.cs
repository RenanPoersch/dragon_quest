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

        public static BattleSession CreateEncounter()
        {
            var attacker = new CombatantDefinition("Aren", "Atacante", 100, 20, 22, 7, 4, 14, Attack, HeavyStrike, Defend);
            var healer = new CombatantDefinition("Lia", "Healer", 80, 36, 9, 5, 20, 12, Attack, Heal, Defend);
            var tank = new CombatantDefinition("Bram", "Tank", 150, 15, 14, 13, 3, 8, Attack, Defend);
            var guard = new CombatantDefinition("Guarda do tirano", "Inimigo", 150, 0, 22, 8, 0, 9, Attack, Defend);
            return new BattleSession(
                new CombatantState("aren", attacker, CombatTeam.Party),
                new CombatantState("lia", healer, CombatTeam.Party),
                new CombatantState("bram", tank, CombatTeam.Party),
                new CombatantState("guard", guard, CombatTeam.Enemy));
        }
    }
}
