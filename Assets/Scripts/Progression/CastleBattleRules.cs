using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Progression
{
    public enum GameEnding { None, KingFreed, CastleDestroyed, HeroPossessed }

    public sealed class CastleBattleRules : IBattleRules, IBattleDamageRules
    {
        public const string TyrantId="tyrant", SpiritId="evil-spirit";
        // Protect inicia a contagem 5 a 1; o lancamento ocupa o turno seguinte do Rei Rubro.
        public const int MeteorCastTurns=5;
        public const int MeteorHpThreshold=450;
        public const int ProtectDamageReductionPercent=60;
        public static readonly AbilityDefinition RoyalStrike=new AbilityDefinition("royal-strike","Golpe do Rei Rubro",
            "Golpe fisico forte, sem elemento.",0,8,AbilityEffect.PhysicalDamage,TargetRule.Enemy);
        public static readonly AbilityDefinition RoyalHeavy=new AbilityDefinition("royal-heavy","Golpe esmagador",
            "Golpe fisico ainda mais forte, sem elemento.",0,14,AbilityEffect.PhysicalDamage,TargetRule.Enemy);
        public static readonly AbilityDefinition Protect=new AbilityDefinition("tyrant-protect","Protect",
            "Uma barreira reduz em 60% o dano recebido durante a conjuracao.",0,0,AbilityEffect.Prepare,TargetRule.Self);
        public static readonly AbilityDefinition PrepareMeteor=new AbilityDefinition("prepare-meteor","Carregar meteoro",
            "Concentra o meteoro flamejante.",0,0,AbilityEffect.Prepare,TargetRule.Self);
        public static readonly AbilityDefinition Meteor=new AbilityDefinition("flaming-meteor","Meteoro flamejante",
            "Aniquila os presentes e destroi o castelo. Apenas um portador vivo da Armadura draconica pode absorve-lo.",
            0,0,AbilityEffect.MagicDamage,TargetRule.Self,element:DamageElement.Fire);

        private readonly HashSet<string> armorBearers=new HashSet<string>();
        public bool IsSpiritRevealed => armorBearers.Count>0;
        public bool MeteorStarted {get;private set;}
        public bool MeteorAbsorbed {get;private set;}
        public bool ProtectCast {get;private set;}
        public string MeteorAbsorberId {get;private set;}
        public int PreparationsRemaining {get;private set;}
        public int MeteorTurnsElapsed {get;private set;}
        public GameEnding Ending {get;private set;}

        public CastleBattleRules(PartyProgress party)
        {
            foreach(PartyMember member in party.Members)
                if(member.Armor?.Definition==DragonProgress.Armor) armorBearers.Add(member.Id);
        }
        private static CombatantState Find(BattleSession battle,string id)
        {
            foreach(CombatantState participant in battle.Combatants) if(participant.Id==id) return participant;
            return null;
        }
        public CombatantState LivingArmorBearer(BattleSession battle)
        {
            foreach(CombatantState participant in battle.Combatants)
                if(participant.Team==CombatTeam.Party&&participant.IsAlive&&armorBearers.Contains(participant.Id)) return participant;
            return null;
        }
        public AbilityDefinition ChooseEnemyAbility(BattleSession battle)
        {
            if(battle.CurrentActor?.Id!=TyrantId) return null;
            if(MeteorStarted&&!MeteorAbsorbed)
                return !ProtectCast?Protect:PreparationsRemaining>0?PrepareMeteor:Meteor;
            return battle.CurrentActor.TurnsTaken%2==0?RoyalStrike:RoyalHeavy;
        }
        public int LimitDamage(CombatantState target,int damage)
        {
            if(target.Id!=TyrantId||ProtectCast||Ending!=GameEnding.None) return damage;
            // Nem golpes fatais nem os cortes restantes de um LIMIT podem pular Protect.
            return System.Math.Min(damage,System.Math.Max(0,target.Hp-MeteorHpThreshold));
        }
        public bool ValidateAction(CombatantState actor,AbilityDefinition ability,out string message)
        {
            message=string.Empty;
            if(actor.Id!=TyrantId) return true;
            bool casting=MeteorStarted&&!MeteorAbsorbed;
            if(ability==Protect&&(!casting||ProtectCast))
            {message="Protect so pode iniciar a conjuracao uma vez.";return false;}
            if(ability==Meteor&&(!casting||!ProtectCast||PreparationsRemaining!=0))
            {message="O meteoro so pode ser lancado no turno seguinte ao fim da contagem.";return false;}
            if(ability==PrepareMeteor&&(!casting||!ProtectCast||PreparationsRemaining<=0))
            {message="Nao ha preparacao de meteoro pendente.";return false;}
            if(casting&&ability!=Protect&&ability!=PrepareMeteor&&ability!=Meteor)
            {message="O Rei Rubro esta concentrado no meteoro.";return false;}
            return true;
        }
        public bool TryResolveAction(BattleSession battle,CombatantState actor,AbilityDefinition ability,out string message)
        {
            message=string.Empty;
            if(actor.Id!=TyrantId) return false;
            if(ability==Protect||ability==PrepareMeteor)
            {
                MeteorTurnsElapsed++;
                actor.IsCharging=true;
                if(ability==Protect)
                {
                    ProtectCast=true;
                    actor.GrantDamageShield(MeteorCastTurns,true,ProtectDamageReductionPercent);
                }
                if(PreparationsRemaining>0) PreparationsRemaining--;
                message=ability==Protect?"O Rei Rubro conjura Protect.":"O Rei Rubro continua a conjuracao.";
                return true;
            }
            if(ability!=Meteor) return false;
            MeteorTurnsElapsed++;
            CombatantState bearer=LivingArmorBearer(battle);
            actor.IsCharging=false;
            if(bearer!=null)
            {
                MeteorAbsorbed=true;
                MeteorAbsorberId=bearer.Id;
                message=bearer.Definition.Name+" ergue a Armadura draconica. O meteoro e absorvido por completo! O castelo permanece intacto.";
            }
            else
            {
                Ending=GameEnding.CastleDestroyed;
                foreach(CombatantState participant in battle.Combatants)
                {
                    // O impacto final aniquila inclusive quem estava protegido por Shield ou Protect.
                    participant.ClearConditions();
                    participant.TakeDamage(participant.Hp);
                }
                message="O meteoro flamejante aniquila a equipe e o Rei Rubro. O castelo desaba em chamas.";
            }
            return true;
        }
        public void AfterAction(BattleSession battle)
        {
            CombatantState tyrant=Find(battle,TyrantId);
            if(Ending==GameEnding.None&&!MeteorStarted&&tyrant!=null&&tyrant.IsAlive
                &&tyrant.Hp<=MeteorHpThreshold)
            {MeteorStarted=true;PreparationsRemaining=MeteorCastTurns;tyrant.IsCharging=true;}
        }
        public BattleOutcome EvaluateOutcome(BattleSession battle)
        {
            if(Ending!=GameEnding.None) return BattleOutcome.Completed;
            CombatantState tyrant=Find(battle,TyrantId),spirit=Find(battle,SpiritId);
            if(tyrant!=null&&!tyrant.IsAlive) Ending=GameEnding.HeroPossessed;
            else if(IsSpiritRevealed&&spirit!=null&&!spirit.IsAlive) Ending=GameEnding.KingFreed;
            if(Ending==GameEnding.None) return BattleOutcome.None;
            PreparationsRemaining=0;
            if(tyrant!=null) tyrant.IsCharging=false;
            return BattleOutcome.Completed;
        }
    }
}
