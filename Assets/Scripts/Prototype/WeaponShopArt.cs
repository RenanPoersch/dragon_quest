using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class WeaponShopArt
    {
        private static readonly Color Steel=ObliqueMeshData.Hex("9BB5C8"), Edge=ObliqueMeshData.Hex("E3EEF4"),
            Iron=ObliqueMeshData.Hex("394553"), Brass=ObliqueMeshData.Hex("C59B52"), Leather=ObliqueMeshData.Hex("6B3C2B"),
            Red=ObliqueMeshData.Hex("7E2934"), Wood=ObliqueMeshData.Hex("493224");
        public static readonly Vector4[] Fixtures={
            new Vector4(0,4.7f,14,.6f),new Vector4(0,-4.7f,14,.6f),
            new Vector4(-6.7f,0,.6f,10),new Vector4(6.7f,0,.6f,10),
            new Vector4(0,.15f,4,.6f),new Vector4(-4.8f,1.5f,1.5f,3),new Vector4(4.8f,1.5f,1.5f,3),
            new Vector4(-4.9f,-2.5f,1.4f,1.1f),new Vector4(4.9f,-2.5f,1.4f,1.1f),
            new Vector4(-2.8f,3.5f,1.5f,1.2f),new Vector4(2.8f,3.5f,1.5f,1.2f)
        };
        private static void Plain(ObliqueMeshData m)=>m.DefaultSurface=PixelSurface.None;
        private static void Timber(ObliqueMeshData m)=>m.DefaultSurface=(PixelSurface)0;
        public static ObliqueMeshData Build()
        {
            var m=new ObliqueMeshData();
            m.DefaultSurface=(PixelSurface)7;m.Plane(0,0,14,10,.02f,Color.white);
            Plain(m);
            m.Box(0,4.7f,14,.6f,0,3.2f,ObliqueMeshData.Hex("50535E"));
            // Juntas de alvenaria e pilares de madeira no fundo.
            for(int row=0;row<7;row++) {
                float h=.16f+row*.44f;
                m.Box(0,4.385f,13.5f,.02f,h,h+.025f,Iron);
                for(int col=0;col<10;col++) m.Box(-6.1f+col*1.3f+(row%2)*.5f,4.37f,.025f,.02f,h,h+.42f,Iron);
            }
            Timber(m);
            foreach(float x in new[]{-6.7f,6.7f})m.Box(x,0,.6f,10,0,.65f,Color.white);
            m.Box(0,-4.7f,14,.6f,0,.18f,Color.white);
            foreach(float x in new[]{-6.2f,-3.7f,3.7f,6.2f})m.Box(x,4.3f,.19f,.2f,0,3.24f,Color.white);
            m.Box(0,4.3f,13.3f,.22f,3.05f,3.22f,Color.white);
            Plain(m);m.Plane(0,-2.3f,3.35f,3.65f,.035f,Brass);m.Plane(0,-2.3f,3.12f,3.42f,.04f,Red);
            // Dois estandartes flanqueiam a exposicao de armas da parede.
            foreach(float x in new[]{-5.75f,5.75f}) {
                m.Box(x,4.23f,.93f,.08f,.74f,2.97f,Red);
                m.Box(x,4.17f,1.06f,.12f,2.92f,3.03f,Brass);
                Shield(m,x,4.1f,1.57f,.33f,Brass);
            }
            // Painel central carregado, com alturas alternadas.
            Timber(m);m.Box(0,4.19f,7,.18f,.58f,2.89f,Color.white);
            Plain(m);m.Box(0,4.04f,7.1f,.08f,.62f,.71f,Brass);
            for(int i=0;i<9;i++) {
                float x=-3.05f+i*.76f;
                if(i%3==1)Axe(m,x,3.97f,.87f,1.42f);
                else Sword(m,x,3.97f,.9f,1.15f+(i%2)*.22f);
            }
            // Racks laterais: espadas e escudos a esquerda, lancas a direita.
            foreach(float x in new[]{-4.8f,4.8f}) {
                Timber(m);m.Box(x,2.92f,1.5f,.12f,0,2.65f,Color.white);
                foreach(float dx in new[]{-.7f,.7f})m.Box(x+dx,1.5f,.09f,3,0,2.7f,Color.white);
                foreach(float h in new[]{.14f,1.06f,2.12f})m.Box(x,1.5f,1.5f,3,h,h+.09f,Color.white);
            }
            Plain(m);
            for(int i=0;i<4;i++) {
                float x=-5.31f+i*.34f;
                Sword(m,x,.12f,.28f,.87f+i*.05f);
                Shield(m,x,1.12f,1.26f,.19f,i%2==0?Red:Steel);
                Spear(m,4.28f+i*.34f,.25f,.23f,1.97f+i*.12f);
                Helmet(m,4.29f+i*.34f,1.25f,1.19f,.14f);
            }
            // Balcao de couro com ferragens, laminas, elmos e escudos pequenos.
            Timber(m);m.Box(0,.15f,4,.6f,0,.8f,Color.white);
            Plain(m);m.Box(0,-.165f,3.83f,.045f,.15f,.73f,Leather);
            m.Box(0,.15f,4.15f,.8f,.8f,.91f,Iron);
            m.Box(0,-.27f,4.18f,.04f,.84f,.94f,Brass);
            for(int i=0;i<9;i++)m.Box(-1.8f+i*.45f,-.196f,.045f,.02f,.2f,.25f,Brass);
            for(int i=0;i<3;i++) {
                float x=-1.56f+i*.58f;
                m.Box(x,.14f,.12f,.5f,.93f,.965f,Steel);
                m.Box(x-.035f,.14f,.025f,.5f,.966f,.977f,Edge);
                m.Box(x,-.13f,.36f,.055f,.93f,.99f,Brass);
                m.Box(x,-.27f,.075f,.22f,.94f,1,Leather);
            }
            Helmet(m,.45f,.15f,.93f,.23f);Helmet(m,1.22f,.15f,.93f,.27f);
            Shield(m,1.77f,.15f,.94f,.18f,Red);
            // Manequins: armadura clara e armadura de ferro, longe da passagem.
            Armor(m,-4.9f,-2.5f,Steel);Armor(m,4.9f,-2.5f,Iron);
            // Bancada de ferreiro e estoque de barras, ao fundo.
            Timber(m);m.Box(-2.8f,3.5f,1.4f,1.1f,0,.6f,Color.white);
            Plain(m);m.Box(-2.8f,3.5f,.63f,.5f,.6f,.81f,Iron);
            m.Box(-2.8f,3.5f,1.15f,.48f,.8f,.99f,Steel);
            m.Cone(-2.19f,3.5f,.23f,.84f,.97f,Steel,5,.04f);
            m.Box(-3.1f,3.25f,.055f,.41f,1,1.06f,Leather);m.Box(-3.1f,3.1f,.27f,.13f,1,1.13f,Iron);
            Timber(m);m.Box(2.8f,3.5f,1.4f,1.1f,0,.58f,Color.white);
            Plain(m);
            for(int row=0;row<3;row++)for(int i=0;i<4;i++)m.Box(2.32f+i*.3f,3.5f,.25f,.65f,.6f+row*.1f,.69f+row*.1f,row%2==0?Steel:Iron);
            foreach(float x in new[]{-3.75f,3.75f})Lantern(m,x,4.15f,2.34f);
            return m;
        }
        private static void Sword(ObliqueMeshData m,float x,float y,float h,float length)
        {
            Plain(m);m.Box(x,y,.075f,.075f,h,h+.24f,Leather);
            m.Cone(x,y,.085f,h-.05f,h+.025f,Brass,6,.065f);
            m.Box(x,y,.38f,.09f,h+.24f,h+.31f,Brass);
            m.Box(x,y,.15f,.045f,h+.31f,h+length,Steel);
            m.Box(x-.052f,y-.028f,.03f,.015f,h+.32f,h+length,Edge);
            m.Triangle(ObliqueMeshData.V(x-.075f,y-.027f,h+length),ObliqueMeshData.V(x,y-.027f,h+length+.2f),ObliqueMeshData.V(x+.075f,y-.027f,h+length),Edge);
        }
        private static void Axe(ObliqueMeshData m,float x,float y,float h,float length)
        {
            Plain(m);m.Box(x,y,.07f,.075f,h,h+length,Wood);
            m.Box(x,y,.11f,.09f,h+length-.3f,h+length+.03f,Brass);
            foreach(float side in new[]{-1f,1f})m.Quad(
                ObliqueMeshData.V(x+side*.07f,y-.06f,h+length-.26f),ObliqueMeshData.V(x+side*.33f,y-.06f,h+length-.43f),
                ObliqueMeshData.V(x+side*.36f,y-.06f,h+length+.15f),ObliqueMeshData.V(x+side*.07f,y-.06f,h+length-.02f),Steel,false);
        }
        private static void Spear(ObliqueMeshData m,float x,float y,float h,float length)
        {
            Plain(m);m.Cone(x,y,.038f,h,h+length,Wood,6,.038f);
            m.Cone(x,y,.13f,h+length,h+length+.38f,Edge,5);
            m.Box(x,y,.15f,.1f,h+length-.07f,h+length,Brass);
        }
        private static void Shield(ObliqueMeshData m,float x,float y,float h,float r,Color face)
        {
            Plain(m);
            m.Quad(ObliqueMeshData.V(x-r,y,h+r*1.4f),ObliqueMeshData.V(x+r,y,h+r*1.4f),ObliqueMeshData.V(x+r*.83f,y,h+r*.4f),ObliqueMeshData.V(x,y,h),Brass,false);
            m.Triangle(ObliqueMeshData.V(x-r,y,h+r*1.4f),ObliqueMeshData.V(x,y,h),ObliqueMeshData.V(x-r*.83f,y,h+r*.4f),Brass,false);
            float yy=y-.018f;
            m.Quad(ObliqueMeshData.V(x-r*.82f,yy,h+r*1.25f),ObliqueMeshData.V(x+r*.82f,yy,h+r*1.25f),ObliqueMeshData.V(x+r*.67f,yy,h+r*.45f),ObliqueMeshData.V(x,yy,h+r*.14f),face,false);
            m.Triangle(ObliqueMeshData.V(x-r*.82f,yy,h+r*1.25f),ObliqueMeshData.V(x,yy,h+r*.14f),ObliqueMeshData.V(x-r*.67f,yy,h+r*.45f),face,false);
            m.Box(x,yy-.035f,r*.17f,.04f,h+r*.47f,h+r*1.05f,Brass);
            m.Box(x,yy-.036f,r*.72f,.04f,h+r*.73f,h+r*.87f,Brass);
        }
        private static void Helmet(ObliqueMeshData m,float x,float y,float h,float r)
        {
            Plain(m);m.Cone(x,y,r,h,h+r*.64f,Steel,8,r*.9f);m.Cone(x,y,r*.9f,h+r*.64f,h+r*1.16f,Steel,8,.025f);
            m.Box(x,y-r*.92f,r*1.35f,.025f,h+r*.28f,h+r*.42f,Iron);
            m.Box(x,y-r*.96f,r*.16f,.03f,h,h+r*.77f,Brass);
        }
        private static void Armor(ObliqueMeshData m,float x,float y,Color metal)
        {
            Timber(m);m.Box(x,y,1.3f,1,0,.15f,Color.white);
            Plain(m);
            foreach(float side in new[]{-1f,1f}) {
                m.Box(x+side*.2f,y,.23f,.27f,.15f,.9f,metal);
                m.Box(x+side*.2f,y-.1f,.29f,.44f,.15f,.29f,Iron);
                m.Cone(x+side*.49f,y,.19f,1.09f,1.43f,metal,7,.13f);
                m.Box(x+side*.49f,y,.17f,.2f,.79f,1.12f,metal);
            }
            m.Cone(x,y,.34f,.87f,1.44f,metal,8,.43f);
            m.Box(x,y-.35f,.07f,.05f,.98f,1.43f,Brass);
            m.Box(x,y-.3f,.57f,.06f,.91f,.99f,Leather);
            Helmet(m,x,y,1.48f,.27f);
            Shield(m,x+.56f,y-.17f,.37f,.33f,Red);
        }
        private static void Lantern(ObliqueMeshData m,float x,float y,float h)
        {
            Plain(m);m.Box(x,y,.14f,.14f,h,h+.41f,ObliqueMeshData.Hex("FFB64C"));
            foreach(float dx in new[]{-.13f,.13f})m.Box(x+dx,y,.035f,.23f,h-.05f,h+.48f,Iron);
            foreach(float z in new[]{h-.05f,h+.45f})m.Box(x,y,.32f,.29f,z,z+.06f,Iron);
        }
    }
}
