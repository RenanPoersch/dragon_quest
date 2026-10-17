using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class UltimateAnimation
    {
        private readonly System.Action<Texture2D,Rect,Rect,Color> drawFrame;
        private readonly bool drawText;
        public UltimateAnimation(System.Action<Texture2D,Rect,Rect,Color> drawFrame=null,bool drawText=true)
        { this.drawFrame=drawFrame; this.drawText=drawText; }
        private readonly Dictionary<string,UltimateSpriteSheet> sheets=new Dictionary<string,UltimateSpriteSheet>();
        private readonly Dictionary<string,BattleAnimationSheet> blessings=new Dictionary<string,BattleAnimationSheet>();
        private GUIStyle banner,caption,combo;
        private Color bramTint=Color.white;
        private static readonly Color Gold=new Color(1,.8f,.4f), Mint=new Color(.6f,1,.85f), Blue=new Color(.5f,.85f,1);
        private UltimateSpriteSheet Sheet(string name)
        {
            if(!sheets.TryGetValue(name,out var sheet)) { sheet=new UltimateSpriteSheet(name,drawFrame); sheets.Add(name,sheet); }
            return sheet;
        }
        public void Prepare()
        {
            foreach(string name in UltimateSpriteTimeline.Atlases) Sheet(name);
        }
        private static Color Fade(Color color,float alpha) { color.a*=Mathf.Clamp01(alpha); return color; }
        private static void Box(Rect rect,Color color)
        { Color previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }
        private void Styles()
        {
            if(banner!=null) return;
            banner=PrototypeTypography.Style(GUI.skin.label,30,true); banner.alignment=TextAnchor.MiddleCenter;
            caption=PrototypeTypography.Style(GUI.skin.label,17,true); caption.alignment=TextAnchor.MiddleCenter;
            combo=PrototypeTypography.Style(GUI.skin.label,30,true); combo.alignment=TextAnchor.MiddleCenter;
        }
        private void Label(Rect rect,string value,GUIStyle style,Color color)
        {
            Color previous=style.normal.textColor;
            style.normal.textColor=new Color(0,0,0,color.a);
            GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),value,style);
            style.normal.textColor=color; GUI.Label(rect,value,style); style.normal.textColor=previous;
        }
        public static Rect ActorRect(Rect home,Rect target,float elapsed)
        {
            if(elapsed<.94f) return home;
            Rect rect=home;
            if(elapsed<UltimateTimeline.JumpStart)
            {
                int cut=UltimateSpriteTimeline.CutIndex(elapsed);
                float direction=cut%2==0?1:-1;
                float sweep=Mathf.Clamp01((UltimateSpriteTimeline.CutAge(elapsed)+.065f)/.195f);
                rect.center=target.center+new Vector2(direction*(83-45*sweep),-10+(cut%3-1)*18);
            }
            else
            {
                float top=Mathf.Max(60,target.y-150);
                rect.x=target.center.x+24;
                float rise=Mathf.Clamp01((elapsed-UltimateTimeline.JumpStart)/(UltimateTimeline.HangStart-UltimateTimeline.JumpStart));
                rect.y=Mathf.Lerp(target.center.y-rect.height*.5f,top,1-(1-rise)*(1-rise));
                if(elapsed>=UltimateTimeline.DiveStart)
                {
                    float dive=Mathf.Clamp01((elapsed-UltimateTimeline.DiveStart)/(UltimateTimeline.FinalImpact-UltimateTimeline.DiveStart));
                    rect.y=Mathf.Lerp(top,target.yMax-home.height,dive*dive);
                }
                float back=Mathf.SmoothStep(0,1,(elapsed-UltimateTimeline.FinalImpact-.72f)/.45f);
                rect.position=Vector2.Lerp(rect.position,home.position,back);
            }
            return rect;
        }
        public static bool ActorVisible(float elapsed) => UltimateSpriteTimeline.ActorVisible(elapsed);
        public static Rect BramRect(Rect home,float elapsed)
        {
            Rect rect=home;
            float rise=Mathf.SmoothStep(0,1,(elapsed-.85f)/.85f);
            float land=Mathf.SmoothStep(0,1,(elapsed-5.65f)/.7f);
            rect.position=Vector2.Lerp(home.position,new Vector2(735,75),rise*(1-land));
            if(elapsed>=1.75f && elapsed<5.65f)
                rect.position+=new Vector2(Mathf.Sin(elapsed*37)*.7f,Mathf.Sin(elapsed*29)*.4f);
            return rect;
        }
        public void DrawBackdrop(AbilityDefinition ability,float elapsed,Rect caster,IReadOnlyList<Rect> allies)
        {
            if(!UltimateTimeline.IsUltimate(ability) || elapsed<0 || elapsed>=UltimateTimeline.Duration(ability)) return;
            if(ability.SourceAbilityId=="bram-aegis" && elapsed>=.55f)
                foreach(Rect home in allies)
                {
                    Rect ally=home==caster?BramRect(home,elapsed):home;
                    Sheet("BramBarrier").DrawCanvas(new Rect(ally.center.x-65,ally.center.y-82,130,164),
                        UltimateSpriteTimeline.BarrierFrame(elapsed),Fade(Color.white,.8f*Mathf.Clamp01((5.8f-elapsed)/.6f)));
                }
        }
        public bool DrawActor(Rect home,Rect focus,string hero,AbilityDefinition ability,float elapsed,
            Color tint,PrototypeBattleSprites sprites,HeroCastAnimation cast)
        {
            if(!UltimateTimeline.IsUltimate(ability)) return false;
            Rect rect=home; bool mirror=false;
            if(UltimateTimeline.IsOmnislash(ability))
            {
                if(!ActorVisible(elapsed)) return true;
                rect=ActorRect(home,focus,elapsed);
                mirror=elapsed>=UltimateTimeline.CutStart-.065f && elapsed<UltimateTimeline.JumpStart && rect.center.x<focus.center.x;
            }
            else if(ability.SourceAbilityId=="lia-salvation")
            {
                float rise=Mathf.SmoothStep(0,1,(elapsed-.6f)/.7f);
                float settle=Mathf.SmoothStep(0,1,(elapsed-5.1f)/.7f);
                rect.y-=38*rise*(1-settle);
            }
            else
            {
                bramTint=tint;
                if(elapsed>=1.75f && elapsed<5.65f) return true;
                rect=BramRect(home,elapsed);
            }
            Sheet(UltimateSpriteTimeline.PoseAtlas(ability,elapsed)).DrawActor(rect,UltimateSpriteTimeline.PoseFrame(ability,elapsed),tint,mirror);
            return true;
        }
        public void DrawEffects(AbilityDefinition ability,float elapsed,Rect caster,Rect focus,
            IReadOnlyList<Rect> targets,IReadOnlyList<Rect> allies,IReadOnlyList<bool> revived)
        {
            if(elapsed<0 || elapsed>=UltimateTimeline.Duration(ability)) return;
            if(drawText) Styles();
            Color color=UltimateTimeline.IsOmnislash(ability)?Gold:ability.SourceAbilityId=="lia-salvation"?Mint:Blue;
            float bannerAlpha=Mathf.Clamp01(elapsed/.12f)*Mathf.Clamp01((.94f-elapsed)/.2f);
            if(drawText && bannerAlpha>0)
            {
                Box(new Rect(210,27,860,62),new Color(.02f,.04f,.09f,bannerAlpha*.9f));
                Label(new Rect(220,28,840,42),ability.Name.ToUpperInvariant(),banner,Fade(color,bannerAlpha));
                Label(new Rect(220,66,840,22),"L I M I T   B R E A K",caption,Fade(Color.white,bannerAlpha));
            }
            if(UltimateTimeline.IsOmnislash(ability)) DrawOmnislash(elapsed,focus);
            else if(ability.SourceAbilityId=="lia-salvation")
            {
                if(elapsed>=.7f)
                {
                    float alpha=Mathf.Clamp01((elapsed-.7f)/.25f)*Mathf.Clamp01((6-elapsed)/.5f);
                    Sheet("LiaCelestial").DrawCanvas(new Rect(caster.center.x-575,24,510,380),
                        UltimateSpriteTimeline.CelestialFrame(elapsed),Fade(Color.white,alpha));
                }
                DrawGospel(elapsed,allies,revived);
            }
            else
            {
                if(elapsed>=1.75f && elapsed<5.65f)
                    Sheet("BramCatastrophe").DrawActor(BramRect(caster,elapsed),
                        UltimateSpriteTimeline.PoseFrame(ability,elapsed),bramTint);
                DrawCataclysm(elapsed,caster,targets);
            }
        }
        private void DrawOmnislash(float elapsed,Rect target)
        {
            var effect=Sheet("ArenImpact");
            for(int hit=1;hit<=12;hit++)
            {
                float age=elapsed-UltimateTimeline.HitTime(hit);
                if(age<0 || age>=.2f) continue;
                float size=Mathf.Clamp(target.height*1.45f,145,225);
                effect.DrawCanvas(new Rect(target.center.x-size*.5f,target.center.y-size*.5f,size,size),
                    UltimateSpriteTimeline.SlashFrame(age,hit),Color.white);
            }
            int count=UltimateTimeline.CutsLanded(elapsed);
            if(drawText && count>0) Label(new Rect(535,95,230,45),count.ToString("00")+" HITS",combo,Gold);
            if(drawText && elapsed>=UltimateTimeline.HangStart && elapsed<UltimateTimeline.DiveStart)
                Label(new Rect(535,145,230,28),"CORTE FINAL",caption,Gold);
            if(elapsed>=UltimateTimeline.FinalImpact-.1f && elapsed<UltimateTimeline.FinalImpact+1.02f)
                effect.DrawCanvas(new Rect(target.center.x-155,target.yMax-240,310,270),
                    UltimateSpriteTimeline.FinisherFrame(elapsed),Fade(Color.white,(UltimateTimeline.FinalImpact+1.02f-elapsed)/.2f));
        }
        private void DrawGospel(float elapsed,IReadOnlyList<Rect> allies,IReadOnlyList<bool> revived)
        {
            if(!drawText) return;
            float started=UltimateTimeline.SalvationImpact-.4f;
            if(elapsed<started || elapsed>=5.4f) return;
            for(int i=0;i<allies.Count;i++)
            {
                Rect ally=allies[i];
                string resource=revived[i]?"ReviveSpell":"HealSpell";
                if(!blessings.TryGetValue(resource,out var sheet))
                { sheet=new BattleAnimationSheet("PixelArt/Spells/"+resource,4,2,false); blessings.Add(resource,sheet); }
                float age=elapsed-started;
                int frame=Mathf.Clamp((int)(age*8),0,7);
                sheet.Draw(new Vector2(ally.center.x,ally.yMax+8),ally.height*1.65f,frame,
                    Fade(Color.white,(5.4f-elapsed)/.4f));
                if(elapsed>=UltimateTimeline.SalvationImpact)
                    Label(new Rect(ally.center.x-95,ally.y-48,190,24),revived[i]?"REVIVEU":"PURIFICADO",caption,
                        Fade(Mint,(5.4f-elapsed)/.4f));
            }
        }
        private void DrawCataclysm(float elapsed,Rect caster,IReadOnlyList<Rect> targets)
        {
            if(elapsed<1.75f || elapsed>=5.65f) return;
            Rect bram=BramRect(caster,elapsed);
            Vector2 source=new Vector2(bram.x-52,bram.center.y+5);
            foreach(Rect target in targets)
            {
                Vector2 impact=new Vector2(target.center.x,target.yMax-5);
                float width=Mathf.Max(200,(source.x-impact.x)/.63f),height=Mathf.Max(170,(impact.y-source.y)/.6f);
                Rect area=new Rect(source.x-width*.85f,source.y-height*.18f,width,height);
                Sheet("BramCataclysm").DrawCanvas(area,UltimateSpriteTimeline.BeamFrame(elapsed),
                    Fade(Color.white,(5.65f-elapsed)/.45f));
            }
            if(drawText && elapsed>=UltimateTimeline.AegisImpact)
                Label(new Rect(780,376,430,28),"SHIELD  -50% DANO  /  3 TURNOS",caption,Fade(Blue,(5.8f-elapsed)/.5f));
        }
    }
}
