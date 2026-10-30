using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public static class PrototypeCastleBattleFactory
    {
        public static readonly AbilityDefinition DrainLife=new AbilityDefinition("drain-life","Drenar vida",
            "Dano magico individual; o espirito recupera o HP realmente drenado.",0,8,AbilityEffect.Drain,TargetRule.Enemy);
        public static readonly AbilityDefinition MindControl = new AbilityDefinition("mind-control", "Mind Control",
            "Confunde um heroi por uma acao: ele ataca automaticamente um aliado ou inimigo aleatorio.",
            0, 1, AbilityEffect.MindControl, TargetRule.Enemy);
        public static readonly AbilityDefinition Silence = new AbilityDefinition("silence", "Silence",
            "Impede um heroi de usar magias e invocacoes por uma acao. Ataque, defesa e itens continuam disponiveis.",
            0, 1, AbilityEffect.Silence, TargetRule.Enemy);
        public static readonly AbilityDefinition Chains=new AbilityDefinition("chains","Correntes",
            "Prende o alvo e impede suas acoes ate as correntes receberem qualquer acerto, independentemente do dano.",0,1,AbilityEffect.Bind,TargetRule.Enemy);
        public static readonly AbilityDefinition ChaosMagic=new AbilityDefinition("chaos-magic","Caos Magic",
            "Dano magico sem elemento em toda a equipe.",0,5,AbilityEffect.MagicDamage,TargetRule.AllEnemies);

        public static BattleSession Create(PartyProgress party)
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
            return new BattleSession(rules,party.Consumables,participants.ToArray());
        }
    }
}
