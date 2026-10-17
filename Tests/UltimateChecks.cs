using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Prototype;

public static class UltimateChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if(!value) throw new Exception(message); }
    private static CombatantState Fighter(string id, CombatTeam team, int speed, AbilityDefinition ultimate = null,
        int hp = 1000, int defense = 5, double critical = 0) => new CombatantState(id,
            new CombatantDefinition(id,"Teste",hp,30,25,defense,19,speed,ElementalAffinities.Neutral,0,0,critical,
                ultimate == null ? new[] { MateriaResolver.Attack, MateriaResolver.Defend }
                    : new[] { MateriaResolver.Attack, MateriaResolver.Defend, ultimate }),team);
    private static BattleSession Battle(params CombatantState[] actors) => new BattleSession(()=>1,null,new ItemInventory(),actors);
    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
        => Check(battle.TryAct(battle.CurrentActor,ability,target,out string error),"Action failed: "+error);
    private static void Ready(CombatantState actor) => actor.Limit.Restore(100);

    private static void Signatures()
    {
        foreach(var member in PrototypePartyFactory.Create().Members)
        {
            var actor=member.CreateCombatant();
            var ultimate=actor.Definition.Abilities.Single(a=>a.UsesLimit);
            Check(ultimate==HeroUltimates.ForHero(member.Id),"Character keeps signature independently of materia.");
            Check(ultimate.MpCost==0 && ultimate.Id=="limit-break","Stable menu ID and zero MP.");
            Ready(actor);
            Check(BattleCommandMenu.Root(actor)[0].Ability==ultimate,"LIMIT opens the character's own ultimate.");
        }
        Check(HeroUltimates.Omnislash.Targets==TargetRule.Enemy,"Omnislash is strictly single target.");
        Check(HeroUltimates.Aegis.Targets==TargetRule.AllEnemies,"Shield bash hits the enemy formation.");
    }
    private static void Salvation()
    {
        var lia=Fighter("lia",CombatTeam.Party,30,HeroUltimates.Salvation,hp:101);
        var ally=Fighter("aren",CombatTeam.Party,20,hp:103);
        var dead=Fighter("bram",CombatTeam.Party,10,hp:107);
        var enemy=Fighter("enemy",CombatTeam.Enemy,1);
        lia.TakeDamage(90); ally.TakeDamage(80); dead.TakeDamage(107);
        lia.PoisonTurns=1; ally.PoisonTurns=1; ally.BoundActions=1; ally.TauntTurns=3; ally.TauntedBy=enemy;
        ally.IsDefending=true; ally.GrantDamageShield(3,false); ally.ProtectedBy=lia;
        var battle=Battle(lia,ally,dead,enemy); Ready(lia);
        ally.ProtectedBy=lia;
        Check(battle.GetValidTargets(HeroUltimates.Salvation).Count==3,"Recovery includes dead, injured and afflicted allies.");
        int mp=lia.Mp;
        Act(battle,HeroUltimates.Salvation,dead);
        Check(lia.Hp==92 && ally.Hp==103 && dead.Hp==86,"Exactly 80% max HP rounded up, capped; resurrection at 80%.");
        Check(!lia.IsPoisoned && !ally.IsPoisoned && ally.BoundActions==0 && ally.TauntTurns==0 && ally.TauntedBy==null,"All harmful statuses removed, including caster before poison tick.");
        Check(ally.ShieldTurns==3 && ally.ProtectedBy==lia,"Purification preserves beneficial shields and protection.");
        Check(lia.Mp==mp && lia.Limit.Charge==0 && lia.TurnsTaken==1,"Recovery uses one turn, no MP, full Limit.");
        Check(enemy.Hp==1000,"Lia ultimate causes no enemy damage.");
        var presentation=new DefeatedEnemyPresentation();
        Check(presentation.IsVisible(dead,10),"Revived ally remains in the formation.");

        lia=Fighter("lia",CombatTeam.Party,30,HeroUltimates.Salvation);
        ally=Fighter("ally",CombatTeam.Party,20); enemy=Fighter("enemy",CombatTeam.Enemy,1);
        battle=Battle(lia,ally,enemy); Ready(lia); ally.PoisonTurns=1; ally.BoundActions=1;
        Check(battle.GetValidTargets(HeroUltimates.Salvation).Count==2,"Full HP allies are still valid for cleansing.");
        Act(battle,HeroUltimates.Salvation,lia);
        Check(ally.Hp==1000 && !ally.IsPoisoned && ally.BoundActions==0,"Cleanse works at full health.");
    }
    private static void Aegis()
    {
        var bram=Fighter("bram",CombatTeam.Party,30,HeroUltimates.Aegis);
        var ally=Fighter("ally",CombatTeam.Party,20);
        var dead=Fighter("dead",CombatTeam.Party,10); dead.TakeDamage(1000);
        var first=Fighter("first",CombatTeam.Enemy,2); var second=Fighter("second",CombatTeam.Enemy,1);
        first.ProtectedBy=second;
        var battle=Battle(bram,ally,dead,first,second); Ready(bram);
        Act(battle,HeroUltimates.Aegis,first);
        Check(first.Hp==915 && second.Hp==915,"AoE physical shield bash hits each living enemy exactly once without interception.");
        Check(bram.ShieldTurns==3 && ally.ShieldTurns==3 && dead.ShieldTurns==0,"Three future turns for each living ally, including Bram.");
        int before=bram.Hp; bram.TakeDamage(101); Check(bram.Hp==before-51,"Shield reduces actual incoming damage by 50%, rounding up.");
        Act(battle,MateriaResolver.Defend,ally);
        Check(ally.ShieldTurns==2 && bram.ShieldTurns==3,"Duration belongs to each recipient's own actions.");
        bram.GrantDamageShield(3,false); before=bram.Hp; bram.TakeDamage(100);
        Check(bram.Hp==before-50,"Shield refreshes without stacking damage reduction.");
        bram.AdvanceShieldDuration(); bram.AdvanceShieldDuration(); bram.AdvanceShieldDuration();
        Check(bram.ShieldTurns==0,"Shield expires after exactly three future actions.");
    }
    private static void Omnislash()
    {
        var aren=Fighter("aren",CombatTeam.Party,30,HeroUltimates.Omnislash);
        var first=Fighter("first",CombatTeam.Enemy,2); var second=Fighter("second",CombatTeam.Enemy,1);
        var battle=Battle(aren,first,second);
        Check(!battle.TryAct(aren,HeroUltimates.Omnislash,first,out _) && aren.TurnsTaken==0,"Incomplete gauge cannot spend a turn.");
        Ready(aren); int mp=aren.Mp;
        Check(!battle.TryAct(aren,HeroUltimates.Omnislash,aren,out _) && aren.Limit.Charge==100,"Invalid target preserves gauge.");
        Act(battle,HeroUltimates.Omnislash,first);
        Check(first.Hp==550 && second.Hp==1000,"12 cuts of 30 plus a finisher of 90, all on the selected enemy.");
        Check(first.VitalChanges.Count==13 && first.VitalChanges.Take(12).All(c=>c.Amount==-30) && first.VitalChanges.Last().Amount==-90,"Every cut records its own real damage.");
        Check(first.VitalChanges.Select(c=>c.ActionHitIndex).SequenceEqual(Enumerable.Range(1,13)),"Damage events carry individual cinematic hit indices.");
        Check(aren.Limit.Charge==0 && aren.Mp==mp && aren.TurnsTaken==1,"All 13 hits spend one action and one gauge.");

        aren=Fighter("aren",CombatTeam.Party,30,HeroUltimates.Omnislash);
        first=Fighter("first",CombatTeam.Enemy,2,hp:10); second=Fighter("second",CombatTeam.Enemy,1);
        battle=Battle(aren,first,second); Ready(aren); Act(battle,HeroUltimates.Omnislash,first);
        Check(!first.IsAlive && first.VitalChanges.Count==1 && second.Hp==1000,"A lethal cut stops mechanical overkill and never changes target.");
        var death=new DefeatedEnemyPresentation(); death.Observe(first,UltimateTimeline.FinalImpact);
        Check(death.IsVisible(first,UltimateTimeline.HangStart) && !death.IsRed(first,UltimateTimeline.HangStart),"Defeated target stays for the aerial finisher.");
        Check(death.IsRed(first,UltimateTimeline.FinalImpact+.01f),"Death flash starts at final impact.");

        aren=Fighter("aren",CombatTeam.Party,30,HeroUltimates.Omnislash);
        first=Fighter("first",CombatTeam.Enemy,2); second=Fighter("second",CombatTeam.Enemy,1,hp:10);
        first.ProtectedBy=second; battle=Battle(aren,first,second); Ready(aren);
        Act(battle,HeroUltimates.Omnislash,first);
        Check(first.Hp==1000 && !second.IsAlive,"An interception locks the entire sequence to one recipient.");

        aren=Fighter("aren",CombatTeam.Party,30,HeroUltimates.Omnislash,critical:.5);
        first=Fighter("first",CombatTeam.Enemy,1); int rolls=0;
        battle=new BattleSession(()=> ++rolls%2==1 ? 0 : 1,null,new ItemInventory(),aren,first);
        Ready(aren); first.IsDefending=true; first.GrantDamageShield(3,false);
        Act(battle,HeroUltimates.Omnislash,first);
        Check(rolls==13 && first.VitalChanges.Count(c=>c.IsCritical)==7,"Every cut rolls its own critical.");
        Check(first.VitalChanges[0].Amount==-10 && first.VitalChanges[1].Amount==-8 && first.VitalChanges[12].Amount==-28,"Each hit respects critical rounding, Defend and Shield; final remains triple.");
    }
    private static void TimingAndValidation()
    {
        for(int hit=1;hit<13;hit++) Check(UltimateTimeline.HitTime(hit)<UltimateTimeline.HitTime(hit+1),"Individual damage times strictly increase.");
        Check(UltimateTimeline.CutsLanded(UltimateTimeline.CutStart-.01f)==0
            && UltimateTimeline.CutsLanded(UltimateTimeline.CutStart+.01f)==1
            && UltimateTimeline.CutsLanded(UltimateTimeline.HitTime(12)+.01f)==12,"The teleport combo has twelve distinct cuts.");
        Check(UltimateTimeline.JumpStart>UltimateTimeline.HitTime(12) && UltimateTimeline.HangStart<UltimateTimeline.DiveStart
            && UltimateTimeline.DiveStart<UltimateTimeline.FinalImpact,"Jump, airborne hold, dive and final impact occur in order.");
        Check(UltimateTimeline.DiveStart-UltimateTimeline.HangStart>.5f,"Sword wind-up hangs visibly in the air.");
        Check(UltimateTimeline.CutsLanded(UltimateTimeline.FinalImpact)==13 && UltimateTimeline.Duration(HeroUltimates.Omnislash)>UltimateTimeline.FinalImpact+DefeatedEnemyPresentation.Duration,"Finisher and defeat animation complete before controls return.");
        Check(!UltimateTimeline.IsUltimate(MateriaResolver.Attack) && !UltimateTimeline.IsUltimate(null),"Normal attacks never use ultimate choreography.");
        bool rejected=false;
        try { new AbilityDefinition("bad","bad","",0,10,AbilityEffect.PhysicalDamage,TargetRule.AllEnemies,hitCount:13); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"Multi-hit must stay single target.");
    }
    public static void Main()
    {
        Signatures(); Salvation(); Aegis(); Omnislash(); TimingAndValidation(); SpriteSequences();
        Console.WriteLine(checks+" ultimate checks passed: signature commands, 80% recovery/revive/cleanse, shield/AoE, single-target cuts, individual damage and aerial finish.");
    }

    private static void SpriteSequences()
    {
        Check(UltimateSpriteTimeline.Atlases.Length==8 && UltimateSpriteTimeline.Atlases.Distinct().Count()==8,"Eight original sprite atlases have distinct resources.");
        foreach(var ability in new[]{HeroUltimates.Omnislash,HeroUltimates.Salvation,HeroUltimates.Aegis})
        {
            var used=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.HashSet<int>>();
            bool inRange=true;
            for(float time=0;time<UltimateTimeline.Duration(ability);time+=1f/120)
            {
                string atlas=UltimateSpriteTimeline.PoseAtlas(ability,time);
                int frame=UltimateSpriteTimeline.PoseFrame(ability,time);
                inRange &= frame>=0 && frame<16 && UltimateSpriteTimeline.Atlases.Contains(atlas);
                if(!used.ContainsKey(atlas)) used[atlas]=new System.Collections.Generic.HashSet<int>();
                used[atlas].Add(frame);
            }
            Check(inRange,"Every frame of the complete cinematic refers to a valid actual sprite.");
            foreach(var sequence in used) Check(sequence.Value.Count==16,"All sixteen authored character poses play: "+sequence.Key);
        }
        for(int hit=1;hit<=12;hit++)
        {
            float time=UltimateTimeline.HitTime(hit);
            Check(UltimateSpriteTimeline.ActorVisible(time),"The sword wielder is visible at each actual hit.");
            Check(!UltimateSpriteTimeline.ActorVisible(time+.14f),"The sword wielder vanishes between cuts.");
        }
        Check(UltimateSpriteTimeline.PoseAtlas(HeroUltimates.Omnislash,UltimateTimeline.HangStart)=="ArenFinisher"
            && UltimateSpriteTimeline.PoseFrame(HeroUltimates.Omnislash,UltimateTimeline.HangStart+.1f)>=4
            && UltimateSpriteTimeline.PoseFrame(HeroUltimates.Omnislash,UltimateTimeline.HangStart+.1f)<=7,"Airborne hold uses its dedicated drawn wind-up poses.");
        Check(UltimateSpriteTimeline.BeamFrame(UltimateTimeline.AegisImpact)>=8
            && UltimateSpriteTimeline.BeamFrame(UltimateTimeline.AegisImpact)<=11,"AoE damage lands when the drawn bombardment reaches its explosive peak.");
        int firingPose=UltimateSpriteTimeline.PoseFrame(HeroUltimates.Aegis,2f);
        bool steady=true;
        for(float time=2;time<5.65f;time+=.01f) steady &= UltimateSpriteTimeline.PoseFrame(HeroUltimates.Aegis,time)==firingPose;
        Check(steady,"Bram holds one braced pose through the beam instead of cycling moving body drawings.");
        foreach(var pick in new Func<float,int>[] { UltimateSpriteTimeline.CelestialFrame,UltimateSpriteTimeline.BeamFrame,UltimateSpriteTimeline.BarrierFrame })
        {
            var used=new System.Collections.Generic.HashSet<int>();
            for(float time=0;time<6;time+=1f/120) used.Add(pick(time));
            Check(used.Count==16 && used.Min()==0 && used.Max()==15,"Effect animation plays all sixteen frames without escaping its atlas.");
        }
    }
}
