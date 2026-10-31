using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Progression;

namespace DragonQuest.Content
{
    // Monta encontros a partir das definicoes compartilhadas de conteudo.
    public static class EncounterCatalog
    {
        public static readonly AbilityDefinition Attack = AbilityCatalog.Attack;
        public static readonly AbilityDefinition Defend = AbilityCatalog.Defend;
        public static readonly AbilityDefinition HeavyStrike = AbilityCatalog.HeavyStrike;
        public static readonly AbilityDefinition Heal = AbilityCatalog.Heal;
        public static readonly AbilityDefinition Cleave = AbilityCatalog.Cleave;
        public static readonly AbilityDefinition Cleanse = AbilityCatalog.Cleanse;
        public static readonly AbilityDefinition Revive = AbilityCatalog.Revive;
        public static readonly AbilityDefinition Taunt = AbilityCatalog.Taunt;
        public static readonly AbilityDefinition Protect = AbilityCatalog.Protect;
        public static readonly AbilityDefinition PoisonStrike = AbilityCatalog.PoisonStrike;
        public static readonly AbilityDefinition EnemySweep = AbilityCatalog.EnemySweep;
        public static readonly AbilityDefinition DragonClaw = AbilityCatalog.DragonClaw;
        public static readonly AbilityDefinition ChargeBreath = AbilityCatalog.ChargeBreath;
        public static readonly AbilityDefinition DragonBreath = AbilityCatalog.DragonBreath;

        public static BattleSession CreateDragonEncounter(PartyProgress party)
        {
            var participants = new System.Collections.Generic.List<CombatantState>();
            foreach (PartyMember member in party.Members) participants.Add(member.CreateCombatant());
            var dragon = new CombatantDefinition("Dragao corrompido","Boss secreto",2000,0,25,10,22,6,
                new ElementalAffinities(new[]{DamageElement.Ice},new[]{DamageElement.Fire}),100,300,0,
                DragonClaw,ChargeBreath,DragonBreath);
            participants.Add(new CombatantState(DragonProgress.EnemyId,dragon,CombatTeam.Enemy));
            return new BattleSession(false, null, party.Consumables, participants.ToArray());
        }

        public static BattleSession CreateRegionEncounter(PartyProgress party, RandomEncounterType type, int enemyCount)
        {
            if (party == null) throw new System.ArgumentNullException(nameof(party));
            if (type == RandomEncounterType.None || !System.Enum.IsDefined(typeof(RandomEncounterType), type))
                throw new System.ArgumentOutOfRangeException(nameof(type));
            if (enemyCount < 1 || enemyCount > 3) throw new System.ArgumentOutOfRangeException(nameof(enemyCount));
            var participants = new System.Collections.Generic.List<CombatantState>();
            foreach (PartyMember member in party.Members) participants.Add(member.CreateCombatant());
            CombatantDefinition enemy;
            string id;
            switch (type)
            {
                case RandomEncounterType.Pirate:
                    id = "pirate";
                    enemy = new CombatantDefinition("Bandido pirata", "Pirata", 85, 0, 13, 5, 0, 8,
                        new ElementalAffinities(new[] { DamageElement.Lightning }, new DamageElement[0]), 12, 30, 0, Attack, Defend);
                    break;
                case RandomEncounterType.Golem:
                    id = "golem";
                    enemy = new CombatantDefinition("Golem da pedreira", "Golem", 145, 0, 17, 11, 0, 4,
                        new ElementalAffinities(new[] { DamageElement.Ice }, new[] { DamageElement.Earth }), 20, 50, 0, Attack, Defend);
                    break;
                default:
                    id = "wolf";
                    enemy = new CombatantDefinition("Lobo da floresta", "Lobo", 65, 0, 12, 3, 0, 11,
                        new ElementalAffinities(new[] { DamageElement.Fire }, new DamageElement[0]), 8, 20, 0, Attack, Defend);
                    break;
            }
            for (int i = 0; i < enemyCount; i++) participants.Add(new CombatantState(id + "-" + (i + 1), enemy, CombatTeam.Enemy));
            return new BattleSession(party.Consumables, participants.ToArray());
        }

        public static BattleSession CreateEncounter(PartyProgress party = null)
        {
            if (party == null) party = PartySetup.Create();
            var participants = new System.Collections.Generic.List<CombatantState>();
            foreach (PartyMember member in party.Members) participants.Add(member.CreateCombatant());
            var guard = new CombatantDefinition("Guarda do Rei Rubro", "Inimigo", 180, 24, 18, 8, 0, 9,
                new ElementalAffinities(new[] { DamageElement.Lightning }, new[] { DamageElement.Fire }), goldReward: 25, experienceReward: 60, criticalChance: 0,
                PoisonStrike, Attack, EnemySweep, Defend);
            var soldier = new CombatantDefinition("Soldado", "Inimigo", 90, 0, 15, 6, 0, 7,
                new ElementalAffinities(new[] { DamageElement.Fire }, new[] { DamageElement.Ice }), goldReward: 10, experienceReward: 25, criticalChance: 0, Attack, Defend);
            participants.Add(new CombatantState("guard", guard, CombatTeam.Enemy));
            participants.Add(new CombatantState("soldier", soldier, CombatTeam.Enemy));
            return new BattleSession(party.Consumables, participants.ToArray());
        }
        public static readonly AbilityDefinition DrainLife = AbilityCatalog.DrainLife;
        public static readonly AbilityDefinition MindControl = AbilityCatalog.MindControl;
        public static readonly AbilityDefinition Silence = AbilityCatalog.Silence;
        public static readonly AbilityDefinition Chains = AbilityCatalog.Chains;
        public static readonly AbilityDefinition ChaosMagic = AbilityCatalog.ChaosMagic;

        public static BattleSession CreateCastle(PartyProgress party)
        {
            var rules=new CastleBattleRules(party);
            var participants=new List<CombatantState>();
            foreach(PartyMember member in party.Members) participants.Add(member.CreateCombatant());
            var tyrant=new CombatantDefinition("Rei Rubro","Rei possuido",1200,0,28,10,0,7,ElementalAffinities.Neutral,0,250,0,
                CastleBattleRules.RoyalStrike,CastleBattleRules.RoyalHeavy,CastleBattleRules.Protect,CastleBattleRules.PrepareMeteor,CastleBattleRules.Meteor);
            participants.Add(new CombatantState(CastleBattleRules.TyrantId,tyrant,CombatTeam.Enemy));
            if(rules.IsSpiritRevealed)
            {
                var spirit=new CombatantDefinition("Espirito maligno","Possessor",440,0,0,8,26,10,ElementalAffinities.Neutral,0,500,0,DrainLife,MindControl,Silence,Chains,ChaosMagic);
                participants.Add(new CombatantState(CastleBattleRules.SpiritId,spirit,CombatTeam.Enemy));
            }
            return new BattleSession(false, rules, party.Consumables, participants.ToArray());
        }
    }
}
