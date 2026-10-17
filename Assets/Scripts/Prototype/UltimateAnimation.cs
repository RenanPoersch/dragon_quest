using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Effects stay inside the battle viewport; the world remains visible throughout Omnislash.
    public sealed class UltimateAnimation
    {
        private static readonly Color Gold = new Color(1, .75f, .28f);
        private static readonly Color Mint = new Color(.38f, 1, .76f);
        private static readonly Color Cyan = new Color(.25f, .8f, 1);
        private GUIStyle banner, caption, combo;

        private void Styles()
        {
            if (banner != null) return;
            banner = PrototypeTypography.Style(GUI.skin.label, 30, true); banner.alignment = TextAnchor.MiddleCenter;
            caption = PrototypeTypography.Style(GUI.skin.label, 17, true); caption.alignment = TextAnchor.MiddleCenter;
            combo = PrototypeTypography.Style(GUI.skin.label, 32, true); combo.alignment = TextAnchor.MiddleCenter;
        }
        private static Color Fade(Color color, float alpha) { color.a *= Mathf.Clamp01(alpha); return color; }
        private static void Box(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
        private static void Line(Vector2 from, Vector2 to, float width, Color color)
        {
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y, to.x-from.x)*Mathf.Rad2Deg, from);
            Box(new Rect(from.x, from.y-width*.5f, Vector2.Distance(from,to), width), color);
            GUI.matrix = old;
        }
        private static void Ring(Vector2 center, float radius, float flatten, float rotation, Color color, float width = 2)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = rotation + i*Mathf.PI/16, b = rotation + (i+1)*Mathf.PI/16;
                Line(center + new Vector2(Mathf.Cos(a)*radius, Mathf.Sin(a)*radius*flatten),
                    center + new Vector2(Mathf.Cos(b)*radius, Mathf.Sin(b)*radius*flatten), width, color);
            }
        }
        private static void Star(Vector2 center, float size, Color color)
        {
            Line(center-Vector2.right*size, center+Vector2.right*size, 2, color);
            Line(center-Vector2.up*size, center+Vector2.up*size, 2, color);
            Box(new Rect(center.x-2,center.y-2,4,4),color);
        }
        private static void Shield(Vector2 center, float size, Color color)
        {
            Vector2[] points = {new Vector2(-.55f,-.65f), new Vector2(0,-.84f),new Vector2(.55f,-.65f),
                new Vector2(.48f,.25f),new Vector2(0,.75f),new Vector2(-.48f,.25f)};
            for(int i=0;i<points.Length;i++) Line(center+points[i]*size,center+points[(i+1)%points.Length]*size,3,color);
            Line(center-new Vector2(0,.55f)*size, center+new Vector2(0,.42f)*size,3,color);
            Line(center+new Vector2(-.32f,-.15f)*size,center+new Vector2(.32f,-.15f)*size,3,color);
        }
        private void Label(Rect rect, string value, GUIStyle style, Color color)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = new Color(0,0,0,color.a);
            GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),value,style);
            style.normal.textColor = color; GUI.Label(rect,value,style); style.normal.textColor = old;
        }

        public void DrawBackdrop(AbilityDefinition ability, float elapsed, Rect caster, IReadOnlyList<Rect> allies)
        {
            if (elapsed < 0 || elapsed >= UltimateTimeline.Duration(ability)) return;
            bool healing = ability.SourceAbilityId == "lia-salvation";
            Color color = healing ? Mint : ability.SourceAbilityId == "bram-aegis" ? Cyan : Gold;
            float charge = Mathf.Clamp01(elapsed/.65f)*Mathf.Clamp01((1.15f-elapsed)/.4f);
            Ring(new Vector2(caster.center.x,caster.yMax), 32+charge*30,.3f,elapsed*2,Fade(color,charge),3);
            for(int i=0;i<10;i++)
            {
                float a = i*Mathf.PI/5+elapsed*2;
                Star(caster.center+new Vector2(Mathf.Cos(a)*44,Mathf.Sin(a)*52),3,Fade(color,charge*.8f));
            }
            if (!healing) return;
            float bloom = Mathf.Clamp01((elapsed-.85f)/.6f)*Mathf.Clamp01((3.8f-elapsed)/.7f);
            foreach(Rect ally in allies)
            {
                for(int i=0;i<9;i++)
                    Box(new Rect(ally.center.x-54+i*12,48,10,ally.yMax-48),Fade(Mint,bloom*.035f*(1+Mathf.Sin(i*2+elapsed))));
                Ring(new Vector2(ally.center.x,ally.yMax),44+Mathf.Sin(elapsed*3)*5,.28f,elapsed,Fade(Mint,bloom),3);
                Ring(new Vector2(ally.center.x,ally.yMax),59,.28f,-elapsed,Fade(Gold,bloom*.65f));
            }
        }

        public static Rect ActorRect(Rect home, Rect target, float elapsed)
        {
            if (elapsed < .85f) return home;
            Rect rect = home;
            if (elapsed < UltimateTimeline.JumpStart)
            {
                int cut = Mathf.Clamp(UltimateTimeline.CutsLanded(elapsed)-1,0,11);
                rect.center = target.center + new Vector2(cut%2==0 ? 94 : -94, -8+(cut%3-1)*22);
            }
            else
            {
                float top = Mathf.Max(28,target.y-155);
                rect.x = target.center.x+30;
                float takeoff = Mathf.Clamp01((elapsed-UltimateTimeline.JumpStart)/(UltimateTimeline.HangStart-UltimateTimeline.JumpStart));
                rect.y = Mathf.Lerp(target.center.y-rect.height*.5f,top,1-(1-takeoff)*(1-takeoff));
                if (elapsed >= UltimateTimeline.DiveStart)
                {
                    float dive = Mathf.Clamp01((elapsed-UltimateTimeline.DiveStart)/(UltimateTimeline.FinalImpact-UltimateTimeline.DiveStart));
                    rect.y = Mathf.Lerp(top,target.yMax-home.height,dive*dive);
                }
                if (elapsed > 4.95f)
                {
                    float back = Mathf.SmoothStep(0,1,(elapsed-4.95f)/.5f);
                    rect.position = Vector2.Lerp(rect.position,home.position,back);
                }
            }
            return rect;
        }
        public static bool ActorVisible(float elapsed)
        {
            if (elapsed < .85f || elapsed >= UltimateTimeline.JumpStart) return true;
            if (elapsed < UltimateTimeline.CutStart) return false;
            int cut = UltimateTimeline.CutsLanded(elapsed);
            return elapsed-UltimateTimeline.HitTime(cut) < .115f;
        }
        public static int SwordPose(float elapsed) => elapsed < .85f ? 1
            : elapsed >= UltimateTimeline.JumpStart && elapsed < UltimateTimeline.DiveStart ? 1
            : elapsed >= 4.85f ? 3 : 2;

        public bool DrawActor(Rect home, Rect focus, string hero, AbilityDefinition ability, float elapsed,
            Color tint, PrototypeBattleSprites sprites, HeroCastAnimation cast)
        {
            if (!UltimateTimeline.IsUltimate(ability)) return false;
            if (UltimateTimeline.IsOmnislash(ability))
            {
                if (!ActorVisible(elapsed)) return true;
                Rect rect = ActorRect(home,focus,elapsed);
                if (elapsed >= UltimateTimeline.CutStart && elapsed < UltimateTimeline.JumpStart)
                {
                    int cut = UltimateTimeline.CutsLanded(elapsed);
                    for(int i=3;i>=1;i--)
                    {
                        Rect ghost=rect; ghost.x += (cut%2==0 ? -1 : 1)*i*17;
                        sprites.Draw(ghost,hero,2,Fade(Cyan,.2f/i),DragonQuest.Equipment.MateriaResolver.Attack);
                    }
                }
                Matrix4x4 previous=GUI.matrix;
                if (rect.center.x < focus.center.x && elapsed < UltimateTimeline.JumpStart)
                    GUIUtility.ScaleAroundPivot(new Vector2(-1,1),rect.center);
                sprites.Draw(rect,hero,SwordPose(elapsed),tint,DragonQuest.Equipment.MateriaResolver.Attack);
                GUI.matrix=previous;
                return true;
            }
            if (ability.SourceAbilityId == "lia-salvation")
            {
                Rect raised=home; raised.y-=Mathf.Sin(Mathf.Clamp01(elapsed/4.1f)*Mathf.PI)*22;
                cast.Draw(raised,hero,PrototypeBattleFactory.Heal,elapsed,4.1f,tint);
                return true;
            }
            Rect bram=home;
            float rush=Mathf.SmoothStep(0,1,(elapsed-1.55f)/.7f);
            float back=Mathf.SmoothStep(0,1,(elapsed-2.75f)/.65f);
            bram.x-=(1-back)*rush*510;
            sprites.Draw(bram,hero,elapsed<1.55f?1:elapsed<2.75f?2:3,tint,DragonQuest.Equipment.MateriaResolver.Attack);
            if(elapsed>1.05f && elapsed<2.8f) Shield(new Vector2(bram.x-20,bram.center.y),55,Fade(Cyan,.8f));
            return true;
        }

        public void DrawEffects(AbilityDefinition ability, float elapsed, Rect caster, Rect focus,
            IReadOnlyList<Rect> targets, IReadOnlyList<Rect> allies, IReadOnlyList<bool> revived)
        {
            if (elapsed < 0 || elapsed >= UltimateTimeline.Duration(ability)) return;
            Styles();
            Color color=UltimateTimeline.IsOmnislash(ability)?Gold:ability.SourceAbilityId=="lia-salvation"?Mint:Cyan;
            float bannerAlpha=Mathf.Clamp01(elapsed/.12f)*Mathf.Clamp01((.95f-elapsed)/.2f);
            if(bannerAlpha>0)
            {
                Box(new Rect(210,27,860,62),new Color(.02f,.04f,.09f,bannerAlpha*.92f));
                Box(new Rect(210,27,860,2),Fade(color,bannerAlpha)); Box(new Rect(210,87,860,2),Fade(color,bannerAlpha));
                Label(new Rect(220,28,840,42),ability.Name.ToUpperInvariant(),banner,Fade(color,bannerAlpha));
                Label(new Rect(220,66,840,22),"L I M I T   B R E A K",caption,Fade(Color.white,bannerAlpha));
            }
            if(UltimateTimeline.IsOmnislash(ability)) DrawOmnislash(elapsed,caster,focus);
            else if(ability.SourceAbilityId=="lia-salvation") DrawSalvation(elapsed,allies,revived);
            else DrawAegis(elapsed,targets,allies);
        }

        private void DrawOmnislash(float elapsed, Rect caster, Rect target)
        {
            int count=UltimateTimeline.CutsLanded(elapsed);
            for(int hit=1;hit<=12;hit++)
            {
                float age=elapsed-UltimateTimeline.HitTime(hit);
                if(age<0 || age>.24f) continue;
                float alpha=1-age/.24f;
                Vector2 direction=new Vector2(hit%2==0?1:-1,hit%3==0?.8f:-.65f).normalized;
                Vector2 center=target.center+new Vector2((hit%3-1)*10,(hit%4-2)*8);
                float reach=Mathf.Clamp(target.height*.6f,55,105);
                Vector2 start=center-direction*reach, end=center+direction*reach;
                Line(start,end,12,Fade(Gold,alpha*.2f)); Line(start,end,4,Fade(Gold,alpha));
                Line(start,end,2,Fade(Color.white,alpha));
                for(int i=0;i<6;i++)
                {
                    float a=hit*1.7f+i*Mathf.PI/3;
                    Star(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(12+age*160),3,Fade(Gold,alpha));
                }
            }
            if(count>0) Label(new Rect(530,110,250,46),count.ToString("00")+"  HITS",combo,Gold);
            if(elapsed>=UltimateTimeline.HangStart && elapsed<UltimateTimeline.DiveStart)
            {
                Rect airborne=ActorRect(caster,target,elapsed);
                Ring(airborne.center,44,.8f,elapsed*5,Fade(Gold,.65f));
                Label(new Rect(520,160,300,25),"CORTE FINAL",caption,Gold);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4+elapsed*2;
                    Line(airborne.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*65,
                        airborne.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*46,2,Gold);
                }
            }
            if(elapsed>=UltimateTimeline.DiveStart && elapsed<UltimateTimeline.FinalImpact)
            {
                Rect falling=ActorRect(caster,target,elapsed);
                for(int i=0;i<7;i++) Line(new Vector2(falling.center.x-18+i*7,36),
                    new Vector2(falling.center.x-18+i*7,falling.yMax),i%2==0?2:1,Fade(Gold,.4f));
            }
            float impact=elapsed-UltimateTimeline.FinalImpact;
            if(impact>=0 && impact<.7f)
            {
                float alpha=1-impact/.7f;
                Line(target.center-new Vector2(90,150),target.center+new Vector2(90,80),18,Fade(Gold,alpha*.25f));
                Line(target.center-new Vector2(90,150),target.center+new Vector2(90,80),5,Fade(Color.white,alpha));
                Ring(new Vector2(target.center.x,target.yMax-6),30+impact*240,.3f,0,Fade(Gold,alpha),4);
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI/8;
                    Vector2 p=target.center+new Vector2(Mathf.Cos(a)*impact*220,Mathf.Sin(a)*impact*140+impact*impact*140);
                    Star(p,4*alpha,Fade(i%2==0?Gold:Color.white,alpha));
                }
            }
        }
        private void DrawSalvation(float elapsed, IReadOnlyList<Rect> allies, IReadOnlyList<bool> revived)
        {
            float active=Mathf.Clamp01((elapsed-.9f)/.4f)*Mathf.Clamp01((4.05f-elapsed)/.7f);
            for(int index=0;index<allies.Count;index++)
            {
                Rect ally=allies[index];
                Ring(new Vector2(ally.center.x,ally.y-10),26,.3f,elapsed,Fade(Gold,active),3);
                for(int i=0;i<12;i++)
                {
                    float phase=Mathf.Repeat(elapsed*.45f+i/12f+index*.17f,1);
                    float x=ally.center.x+Mathf.Sin(i*2.4f+elapsed*2)*42;
                    float y=ally.yMax-phase*(ally.height+60);
                    Star(new Vector2(x,y),3+phase*3,Fade(i%3==0?Gold:Mint,active*Mathf.Sin(phase*Mathf.PI)));
                }
                if(elapsed>=2.35f)
                {
                    float alpha=Mathf.Clamp01((4-elapsed)/.5f);
                    Label(new Rect(ally.center.x-95,ally.y-48,190,24),revived[index]?"REVIVEU":"PURIFICADO",caption,Fade(Mint,alpha));
                    float wave=elapsed-2.35f;
                    if(wave<.6f) Ring(ally.center,20+wave*130,.85f,0,Fade(Mint,1-wave/.6f),3);
                }
            }
        }
        private void DrawAegis(float elapsed, IReadOnlyList<Rect> targets, IReadOnlyList<Rect> allies)
        {
            float guard=Mathf.Clamp01((elapsed-.85f)/.4f)*Mathf.Clamp01((3.8f-elapsed)/.5f);
            foreach(Rect ally in allies)
            {
                Shield(ally.center,ally.height*.65f,Fade(Cyan,guard));
                if(elapsed>1.35f && elapsed<2.1f) Star(ally.center,12,Fade(Color.white,(2.1f-elapsed)/.75f));
            }
            float impact=elapsed-2.25f;
            if(impact>=0 && impact<.8f)
            {
                float alpha=1-impact/.8f;
                foreach(Rect target in targets)
                {
                    Shield(target.center,35+impact*100,Fade(Color.white,alpha));
                    Ring(new Vector2(target.center.x,target.yMax),22+impact*190,.35f,0,Fade(Cyan,alpha),4);
                    for(int i=0;i<8;i++)
                    {
                        float a=i*Mathf.PI/4;
                        Line(target.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*18,
                            target.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(30+impact*140),3,Fade(Gold,alpha));
                    }
                }
            }
            if(elapsed>=2.3f) Label(new Rect(790,368,420,30),"SHIELD  -50% DANO  /  3 TURNOS",caption,Fade(Cyan,(3.85f-elapsed)/.5f));
        }
    }
}
