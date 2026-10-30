using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Shared by the editor's scrub/replay window and automated captures of the real effects.
    public sealed class UltimatePreviewRenderer
    {
        private readonly UltimateAnimation animation = new UltimateAnimation();
        private readonly HeroCastAnimation cast = new HeroCastAnimation();
        private PrototypeBattleSprites sprites;
        private Texture2D background;
        private GUIStyle text, number, centered;
        private AbilityDefinition observed;
        private CombatantState[] party, enemies;
        private CombatantState actor;
        private readonly Dictionary<CombatantState,int> initialChanges = new Dictionary<CombatantState,int>();
        private static readonly Rect[] Homes = {new Rect(925,125,64,90),new Rect(985,203,64,90),new Rect(925,281,64,90)};
        private static readonly Rect[] EnemyHomes = {new Rect(260,155,80,110),new Rect(405,227,80,110)};
        private readonly List<Rect> allies = new List<Rect>();
        private readonly List<bool> revived = new List<bool>();

        private void Prepare(AbilityDefinition ability)
        {
            if(sprites==null) { sprites=new PrototypeBattleSprites(); animation.Prepare(); }
            if(background==null) background=Resources.Load<Texture2D>("BattleBackgrounds/city");
            if(text==null)
            {
                text=PrototypeTypography.Style(GUI.skin.label,22,true);
                centered=new GUIStyle(text) { alignment=TextAnchor.MiddleCenter };
                number=PrototypeTypography.Style(GUI.skin.label,28,true); number.alignment=TextAnchor.MiddleCenter;
            }
            if(observed==ability) return;
            observed=ability; allies.Clear(); revived.Clear(); initialChanges.Clear();
            var previewParty=PrototypePartyFactory.Create();
            previewParty.SetInfiniteLimit(true);
            var members=previewParty.Members;
            party=new CombatantState[members.Count];
            for(int i=0;i<party.Length;i++) { party[i]=members[i].CreateCombatant(); if(HeroUltimates.ForHero(party[i].Id)==ability) actor=party[i]; }
            enemies=new[] {new CombatantState("golem",new CombatantDefinition("Golem","Preview",5000,0,1,5,0,1,DragonQuest.Equipment.MateriaResolver.Defend),CombatTeam.Enemy),
                new CombatantState("wolf",new CombatantDefinition("Lobo","Preview",5000,0,1,5,0,1,DragonQuest.Equipment.MateriaResolver.Defend),CombatTeam.Enemy)};
            if(ability==HeroUltimates.Salvation)
            {
                SetPreviewVitals(party[0],0);
                SetPreviewVitals(party[1],party[1].Hp-60,poisoned:true);
                SetPreviewVitals(party[2],party[2].Hp-100,boundActions:1);
            }
            var participants=new List<CombatantState>(party); participants.AddRange(enemies);
            var battle=new BattleSession(()=>1,null,new DragonQuest.Inventory.ItemInventory(),participants.ToArray());
            while(battle.CurrentActor!=actor) battle.TryAct(battle.CurrentActor,DragonQuest.Equipment.MateriaResolver.Defend,battle.CurrentActor,out _);
            for(int i=0;i<party.Length;i++) { allies.Add(Homes[i]); revived.Add(!party[i].IsAlive); }
            foreach(var participant in participants) initialChanges[participant]=participant.VitalChanges.Count;
            if(!battle.TryAct(actor,ability,ability==HeroUltimates.Salvation?party[1]:enemies[0],out string error))
                throw new System.InvalidOperationException(error);
        }
        // Authoring-only fixture state; runtime setters remain internal to the combat model.
        private static void SetPreviewVitals(CombatantState member,int hp,bool poisoned=false,int boundActions=0)
            => typeof(CombatantState).GetMethod("RestoreVitals",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(member,new object[] {hp,member.Mp,poisoned,boundActions});

        private static void Box(Rect rect, Color color)
        { Color old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=old; }
        public void Draw(AbilityDefinition ability, float elapsed)
        {
            Prepare(ability);
            Box(new Rect(0,0,1280,720),new Color(.025f,.04f,.08f));
            GUI.BeginGroup(new Rect(0,0,1280,416));
            Rect arena=new Rect(20,20,1240,396);
            if(background!=null) GUI.DrawTexture(arena,background,ScaleMode.ScaleAndCrop);
            else Box(arena,new Color(.12f,.22f,.18f));
            int actorIndex=System.Array.IndexOf(party,actor);
            Rect home=Homes[actorIndex],focus=ability==HeroUltimates.Salvation?home:EnemyHomes[0];
            animation.DrawBackdrop(ability,elapsed,home,allies);
            for(int i=0;i<enemies.Length;i++)
            {
                float age=UltimateTimeline.HitAge(ability,elapsed);
                Rect rect=EnemyHomes[i];
                bool hit=ability==HeroUltimates.Aegis || ability==HeroUltimates.Omnislash && i==0;
                if(hit && age>=0 && age<.18f) rect.x+=Mathf.Sin(age*75)*9;
                sprites.Draw(rect,enemies[i].Id,0,Color.white);
            }
            for(int i=0;i<party.Length;i++)
            {
                Color tint=revived[i] && elapsed<UltimateTimeline.ImpactDelay(ability)?new Color(.4f,.4f,.4f):Color.white;
                if(party[i]==actor) animation.DrawActor(home,focus,actor.Id,ability,elapsed,tint,sprites,cast);
                else sprites.Draw(Homes[i],party[i].Id,0,tint);
            }
            animation.DrawEffects(ability,elapsed,home,focus,EnemyHomes,allies,revived);
            foreach(var target in enemies) DrawNumbers(target,EnemyHomes[System.Array.IndexOf(enemies,target)],ability,elapsed);
            foreach(var target in party) DrawNumbers(target,Homes[System.Array.IndexOf(party,target)],ability,elapsed);
            GUI.EndGroup();
            Box(new Rect(20,426,1240,60),new Color(.025f,.06f,.17f));
            GUI.Label(new Rect(40,426,1200,60),ability.Name,centered);
            Box(new Rect(20,496,1240,204),new Color(.025f,.06f,.17f));
            GUI.Label(new Rect(45,515,540,36),"ULTIMATE  /  "+ability.Name.ToUpperInvariant(),text);
            GUI.Label(new Rect(45,560,540,85),ability.Description,text);
            for(int i=0;i<party.Length;i++)
            {
                var member=party[i]; float y=516+i*52;
                GUI.Label(new Rect(700,y,120,32),member.Definition.Name,text);
                GUI.Label(new Rect(840,y,200,32),"HP  "+member.Hp+"/"+member.Definition.MaxHp,text);
                GUI.Label(new Rect(1060,y,160,32),"MP  "+member.Mp,text);
            }
        }
        private void DrawNumbers(CombatantState target, Rect rect, AbilityDefinition ability, float elapsed)
        {
            int lane=0;
            for(int i=initialChanges[target];i<target.VitalChanges.Count;i++)
            {
                CombatVitalChange change=target.VitalChanges[i];
                float started=change.ActionHitIndex>0?UltimateTimeline.HitTime(change.ActionHitIndex):UltimateTimeline.ImpactDelay(ability);
                float age=elapsed-started;
                float duration=change.ActionHitIndex>0?.72f:1.4f;
                if(age<0 || age>duration) continue;
                float alpha=Mathf.Clamp01((duration-age)/.25f);
                Rect label=new Rect(rect.center.x-90+(lane%3)*40,rect.y-10-age*95-(lane%3)*22,180,45);
                string value=(change.Amount>0?"+":"")+System.Math.Abs(change.Amount);
                number.normal.textColor=new Color(0,0,0,alpha); GUI.Label(new Rect(label.x+2,label.y+2,label.width,label.height),value,number);
                number.normal.textColor=change.Amount>0?new Color(.4f,1,.7f,alpha):new Color(1,1,1,alpha); GUI.Label(label,value,number);
                lane++;
            }
        }
    }
}
