using DragonQuest.Presentation.Common;
using DragonQuest.Presentation.Battle.Sprites;
using DragonQuest.Presentation.Battle;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    public sealed class UltimateAnimation
    {
        private readonly System.Action<Texture2D,Rect,Rect,Color> drawFrame;
        private readonly bool drawText;
        public UltimateAnimation(System.Action<Texture2D,Rect,Rect,Color> drawFrame=null,bool drawText=true)
        { this.drawFrame=drawFrame; this.drawText=drawText; }
        private readonly Dictionary<string,UltimateSpriteSheet> sheets=new Dictionary<string,UltimateSpriteSheet>();
        private readonly Dictionary<string,BattleAnimationSheet> blessings=new Dictionary<string,BattleAnimationSheet>();
        private GUIStyle caption;
        private static readonly Color Mint=new Color(.6f,1,.85f);
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
        private void Styles()
        {
            if(caption!=null) return;
            caption=GameTypography.Style(GUI.skin.label,17,true); caption.alignment=TextAnchor.MiddleCenter;
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
            // The shout stays at home; the shield bash is a short planted lunge, never a jump.
            float drive=Mathf.SmoothStep(0,1,(elapsed-UltimateTimeline.AegisBashStart)
                /(UltimateTimeline.AegisImpact-UltimateTimeline.AegisBashStart));
            float recover=Mathf.SmoothStep(0,1,(elapsed-UltimateTimeline.AegisImpact-.2f)
                /(UltimateTimeline.AegisRecoveryEnd-UltimateTimeline.AegisImpact-.2f));
            rect.x-=30*drive*(1-recover);
            return rect;
        }
        public void DrawBackdrop(AbilityDefinition ability,float elapsed,Rect caster,IReadOnlyList<Rect> allies)
        {
            if(!UltimateTimeline.IsUltimate(ability) || elapsed<0 || elapsed>=UltimateTimeline.Duration(ability)) return;
            if(ability.SourceAbilityId=="bram-aegis" && UltimateSpriteTimeline.BarrierVisible(elapsed))
                foreach(Rect home in allies)
                {
                    Rect ally=home==caster?BramRect(home,elapsed):home;
                    Sheet("BramBarrier").DrawCanvas(new Rect(ally.center.x-65,ally.center.y-82,130,164),
                        UltimateSpriteTimeline.BarrierFrame(elapsed),Fade(Color.white,.8f*Mathf.Clamp01((UltimateTimeline.AegisShieldEnd-elapsed)/.2f)));
                }
        }
        public bool DrawActor(Rect home,Rect focus,string hero,AbilityDefinition ability,float elapsed,
            Color tint,BattleSprites sprites,HeroCastAnimation cast)
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
                float rise=Mathf.SmoothStep(0,1,(elapsed-.9f)/1.4f);
                float settle=Mathf.SmoothStep(0,1,(elapsed-4.85f)/1.1f);
                rect.y-=26*rise*(1-settle);
            }
            else
            {
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
                DrawShieldImpact(elapsed,targets);
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
        private void DrawShieldImpact(float elapsed,IReadOnlyList<Rect> targets)
        {
            if(!UltimateSpriteTimeline.ShieldImpactVisible(elapsed)) return;
            foreach(Rect target in targets)
            {
                float size=Mathf.Clamp(target.height*1.9f,170,235);
                // The authored impact sits at 78% of its canvas, on each enemy's ground line.
                Rect area=new Rect(target.center.x-size*.5f,target.yMax-size*.78f,size,size);
                Sheet("BramShieldImpact").DrawCanvas(area,UltimateSpriteTimeline.ShieldImpactFrame(elapsed),
                    Fade(Color.white,(UltimateTimeline.AegisEffectEnd-elapsed)/.2f));
            }
        }
    }
}
