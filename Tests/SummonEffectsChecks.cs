using DragonQuest.Content;
using DragonQuest.Authoring;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;

public static class SummonEffectsChecks
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static CombatantState Member(string id,int hp,int speed,CombatTeam team,params AbilityDefinition[] commands)
        => new CombatantState(id,new CombatantDefinition(id,"",hp,100,10,0,10,speed,commands),team);
    static void Act(BattleSession battle,AbilityDefinition ability,CombatantState target)
    { Check(battle.TryAct(battle.CurrentActor,ability,target,out string message),message); }
    static void WaitFor(BattleSession battle,CombatantState target)
    {
        int safety=30;
        while(battle.CurrentActor!=target && --safety>0) Act(battle,MateriaResolver.Defend,battle.CurrentActor);
        Check(safety>0,"Turn progression.");
    }
    public static void Main()
    {
        Phoenix(); Shield(); IncomingDamage(); Chaos(); MateriaAndStatuses();
        Console.WriteLine("Phoenix recovery/revive, Titan shield duration/damage, Chaos and concurrent statuses passed.");
    }
    static void Phoenix()
    {
        var actor=Member("caster",100,100,CombatTeam.Party,AbilityCatalog.Summon,MateriaResolver.Defend);
        var injured=Member("injured",100,80,CombatTeam.Party,MateriaResolver.Defend);
        var fallen=Member("fallen",200,70,CombatTeam.Party,MateriaResolver.Defend);
        var enemy=Member("enemy",2000,1,CombatTeam.Enemy,MateriaResolver.Defend);
        var enemy2=Member("enemy2",2000,2,CombatTeam.Enemy,MateriaResolver.Defend);
        var battle=new BattleSession(actor,injured,fallen,enemy,enemy2);
        actor.TakeDamage(50); injured.TakeDamage(90); fallen.TakeDamage(200);
        Act(battle,AbilityCatalog.Summon,enemy);
        Check(actor.Hp==100 && injured.Hp==90 && fallen.Hp==160,"Phoenix heals 80% of maximum HP, caps living HP and revives at 80%.");
        Check(enemy.Hp==1980 && enemy2.Hp==1980,"Phoenix hits every enemy once with low Fire damage.");
        Check(actor.Mp==88 && actor.TurnsTaken==1,"All effects share one MP cost and one action.");
        Check(fallen.IsAlive && CombatStatuses.Active(fallen).Count==0,"Revived character has no defeated status.");
        var finisher=Member("finisher",100,100,CombatTeam.Party,AbilityCatalog.Summon);
        var down=Member("down",100,50,CombatTeam.Party,MateriaResolver.Defend);
        var weak=Member("weak",5,1,CombatTeam.Enemy,MateriaResolver.Defend);
        var lastBattle=new BattleSession(finisher,down,weak); down.TakeDamage(100);
        Act(lastBattle,AbilityCatalog.Summon,weak);
        Check(lastBattle.Outcome==BattleOutcome.Victory && down.Hp==80,"Recovery happens even when Phoenix kills the final enemy.");
    }
    static void Shield()
    {
        var actor=Member("caster",500,100,CombatTeam.Party,AbilityCatalog.Titan,MateriaResolver.Defend);
        var ally=Member("ally",500,80,CombatTeam.Party,MateriaResolver.Defend);
        var fallen=Member("fallen",100,70,CombatTeam.Party,MateriaResolver.Defend);
        var enemy=Member("enemy",5000,1,CombatTeam.Enemy,MateriaResolver.Defend);
        var battle=new BattleSession(actor,ally,fallen,enemy); fallen.TakeDamage(100);
        Act(battle,AbilityCatalog.Titan,enemy);
        Check(actor.ShieldTurns==3 && ally.ShieldTurns==3 && fallen.ShieldTurns==0,"Titan gives each living ally three future turns, including caster.");
        Check(enemy.Hp==4980 && actor.Mp==84,"Titan deals low Earth damage and spends MP once.");
        actor.TakeDamage(101);Check(actor.Hp==449,"Shield reduces odd incoming damage by 50%, rounding up.");
        Act(battle,MateriaResolver.Defend,ally);
        Check(ally.ShieldTurns==2 && actor.ShieldTurns==3,"Shield expires by each recipient's own actions.");
        WaitFor(battle,actor);
        Act(battle,AbilityCatalog.Titan,enemy);
        Check(actor.ShieldTurns==3 && ally.ShieldTurns==3,"Recast refreshes rather than stacks the shield.");
        int before=actor.Hp; actor.TakeDamage(100); Check(actor.Hp==before-50,"Repeated shield does not stack to 75%.");
        for(int turn=0;turn<3;turn++) { WaitFor(battle,actor); Act(battle,MateriaResolver.Defend,actor); }
        Check(actor.ShieldTurns==0,"Shield expires after three subsequent caster turns.");
        before=actor.Hp; actor.TakeDamage(100);Check(actor.Hp==before-100,"Full damage resumes after expiration.");
        ally.GrantDamageShield(3,false); ally.PoisonTurns=3;
        WaitFor(battle,ally);before=ally.Hp;Act(battle,MateriaResolver.Defend,ally);
        Check(ally.Hp==before-13,"Shield also halves poison damage (5% of 500 =25, halved to13).");
        ally.TakeDamage(10000);Check(ally.ShieldTurns==0 && CombatStatuses.Active(ally).Single().Status==CombatStatus.Defeated,"Defeat clears shield and harmful statuses.");
    }
    static void Chaos()
    {
        var actor=Member("caster",100,100,CombatTeam.Party,AbilityCatalog.Bahamut);
        var weak=new CombatantState("weak",new CombatantDefinition("weak","",2000,0,0,0,0,1,
            new ElementalAffinities(weaknesses:new[]{DamageElement.Chaos}),MateriaResolver.Defend),CombatTeam.Enemy);
        var resist=new CombatantState("resist",new CombatantDefinition("resist","",2000,0,0,0,0,2,
            new ElementalAffinities(resistances:new[]{DamageElement.Chaos}),MateriaResolver.Defend),CombatTeam.Enemy);
        var battle=new BattleSession(actor,weak,resist);Act(battle,AbilityCatalog.Bahamut,weak);
        Check(weak.Hp==1880 && resist.Hp==1960,"High Chaos damage honors weakness and resistance for all enemies.");
        Check(actor.Mp==80,"Bahamut retains its MP cost.");
    }
    static void IncomingDamage()
    {
        foreach(var effect in new[]{AbilityEffect.PhysicalDamage,AbilityEffect.MagicDamage})
            foreach(bool defending in new[]{false,true})
            {
                var attack=new AbilityDefinition("hit","Hit","",0,100,effect,TargetRule.Enemy);
                var foe=Member("foe",1000,100,CombatTeam.Enemy,attack);
                var target=Member("target",500,80,CombatTeam.Party,MateriaResolver.Defend);
                var battle=new BattleSession(foe,target);
                target.GrantDamageShield(3,false);target.IsDefending=defending;
                Act(battle,attack,target);
                Check(target.Hp==(defending?472:445),"Shield halves physical/magic damage and combines with Defend.");
                Check(target.ShieldTurns==3,"Enemy actions do not reduce an ally's shield duration.");
                Check(target.VitalChanges.Last().Amount==(defending?-28:-55),"HP numbers reflect the reduced damage.");
            }
    }
    static void MateriaAndStatuses()
    {
        var party=PartyFixture.Create();var hero=party.Members[1];
        hero.Weapon.ClearMaterias(); hero.Armor.ClearMaterias();
        hero.Weapon.Set(0,new MateriaInstance("phoenix",MateriaCatalog.SummonMateria));
        hero.Weapon.Set(1,new MateriaInstance("all",MateriaCatalog.AllMateria));
        var command=MateriaResolver.Resolve(hero).Single(a=>a.SourceAbilityId=="summon");
        Check(command.PartyEffect==PartyEffect.RestoreAndRevive && command.RecoveryPercent==80 && command.MpCost==12,
            "Materia resolution preserves recovery without class or All scaling its percentage/cost.");
        hero.Weapon.Set(0,new MateriaInstance("titan",MateriaCatalog.TitanMateria));
        command=MateriaResolver.Resolve(hero).Single(a=>a.SourceAbilityId=="titan");
        Check(command.ShieldDuration==3 && command.PartyEffect==PartyEffect.DamageShield,"Materia preserves shield data.");
        var member=Member("statuses",100,1,CombatTeam.Party,MateriaResolver.Defend);
        member.PoisonTurns=3;member.GrantDamageShield(3,false);member.IsDefending=true;member.BoundActions=1;
        var statuses=CombatStatuses.Active(member);
        Check(statuses.Count==4 && statuses.Any(s=>s.Status==CombatStatus.Poison) && statuses.Any(s=>s.Status==CombatStatus.Shield),
            "Poison, Shield, Defend and Bound are all shown concurrently.");
        member.ClearConditions();Check(CombatStatuses.Active(member).Count==0,"Removed statuses disappear.");
    }
}
