using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Interior nativo: a referencia orienta os volumes, sem virar um fundo plano.
    public static class ArcaneMateriaShopArt
    {
        private static readonly Color Wood = ObliqueMeshData.Hex("342638");
        private static readonly Color Brass = ObliqueMeshData.Hex("C4A06A");
        private static readonly string[] Gems = { "36EDAE", "40CDFF", "C478FF", "FF6897", "FFC85B", "EEF6FF" };
        public static readonly Vector4[] Fixtures = {
            new Vector4(0,4.7f,14,.6f), new Vector4(0,-4.7f,14,.6f),
            new Vector4(-6.7f,0,.6f,10), new Vector4(6.7f,0,.6f,10),
            new Vector4(0,.15f,4,.6f), new Vector4(-4.8f,1.5f,1.5f,3),
            new Vector4(4.8f,1.5f,1.5f,3), new Vector4(-4.8f,3.65f,1.5f,1.3f),
            new Vector4(4.9f,-2.5f,1.4f,1.1f), new Vector4(-4.9f,-2.5f,1.4f,1.1f)
        };
        private static void Solid(ObliqueMeshData m) => m.DefaultSurface = PixelSurface.None;
        private static void Tile(ObliqueMeshData m, int tile) => m.DefaultSurface = (PixelSurface)tile;
        public static ObliqueMeshData Build()
        {
            var m = new ObliqueMeshData();
            Tile(m,7); m.Plane(0,0,14,10,.02f,Color.white);
            Tile(m,0);
            m.Box(0,4.7f,14,.6f,0,3.15f,Color.white);
            m.Box(-6.7f,0,.6f,10,0,.65f,Color.white);
            m.Box(6.7f,0,.6f,10,0,.65f,Color.white);
            m.Box(0,-4.7f,14,.6f,0,.18f,Color.white);
            Solid(m);
            // Tapete violeta bordado: passagem central livre.
            m.Plane(0,-2.25f,3.35f,3.7f,.035f,Brass);
            m.Plane(0,-2.25f,3.1f,3.45f,.04f,ObliqueMeshData.Hex("522746"));
            Ring(m,0,-2.15f,.052f,1.05f,.045f,Brass,false);
            foreach(float x in new[]{-5.8f,5.8f}) Curtain(m,x);
            // Escada lateral leva ao mezanino decorativo atras do vendedor.
            Tile(m,0);
            for(int i=0;i<9;i++) m.Box(-4.8f,.35f+i*.45f,1.48f,.46f,0,.16f+i*.21f,Color.white);
            m.Box(0,3.7f,11.4f,1.15f,2.03f,2.17f,Color.white);
            Solid(m);
            m.Box(0,3.13f,11.4f,.09f,2.78f,2.89f,Brass);
            for(int i=0;i<20;i++) m.Cone(-5.5f+i*.58f,3.13f,.045f,2.17f,2.82f,Brass,6,.045f);
            for(int i=0;i<9;i++) m.Cone(-5.46f,.35f+i*.45f,.045f,.16f+i*.21f,.83f+i*.21f,Brass,6,.045f);
            // Estantes altas, densas, com livros, frascos, cristais e materias.
            Shelf(m,4.8f,1.5f,1.5f,3,3,false);
            foreach(float x in new[]{-2.8f,0,2.8f}) Shelf(m,x,4.23f,2.45f,.32f,4,true);
            // Vitrine aberta: vidro sugerido por bordas e reflexos, sem esconder as esferas.
            Tile(m,0); m.Box(0,.15f,4,.6f,0,.74f,Color.white);
            Solid(m); m.Box(0,.15f,4.15f,.8f,.74f,.83f,Brass);
            m.Box(0,.15f,3.96f,.65f,.83f,.85f,ObliqueMeshData.Hex("151A32"));
            for(int row=0;row<2;row++) for(int i=0;i<9;i++)
                Orb(m,-1.68f+i*.42f,-.04f+row*.34f,.87f,.14f,Gems[(i+row*3)%Gems.Length]);
            foreach(float x in new[]{-2.02f,2.02f}) m.Box(x,.15f,.045f,.76f,.83f,1.27f,Brass);
            foreach(float y in new[]{-.23f,.53f}) m.Box(0,y,4.08f,.04f,1.24f,1.28f,Brass);
            m.Box(-1.45f,-.235f,.06f,.018f,.89f,1.18f,ObliqueMeshData.Hex("B9F2FA"));
            m.Box(1.4f,-.235f,.035f,.018f,.97f,1.17f,ObliqueMeshData.Hex("B9F2FA"));
            // Livro aberto, barril de cajados e pedestal com materia azul grande.
            Tile(m,0); m.Box(-4.9f,-2.5f,1.25f,.85f,0,.74f,Color.white);
            Solid(m); m.Box(-4.9f,-2.5f,1.38f,.94f,.74f,.83f,Brass);
            m.Box(-5.2f,-2.5f,.58f,.72f,.84f,.9f,ObliqueMeshData.Hex("E7CBA5"));
            m.Box(-4.58f,-2.5f,.58f,.72f,.84f,.9f,ObliqueMeshData.Hex("E7CBA5"));
            for(int i=0;i<5;i++) for(int page=0;page<2;page++)
                m.Plane(-5.2f+page*.62f,-2.75f+i*.12f,.4f,.025f,.91f,ObliqueMeshData.Hex("7C558B"));
            Tile(m,0); m.Cone(4.9f,-2.5f,.59f,0,.79f,Color.white,10,.52f);
            Solid(m); Ring(m,4.9f,-2.5f,.25f,.6f,.055f,Brass,false);
            Ring(m,4.9f,-2.5f,.67f,.56f,.055f,Brass,false);
            Orb(m,4.9f,-2.5f,.82f,.46f,Gems[1]);
            Tile(m,0); m.Cone(5.1f,3.68f,.43f,0,.9f,Color.white,8,.43f);
            Solid(m);
            for(int i=0;i<5;i++) {
                float x=4.83f+i*.13f;
                m.Cone(x,3.68f,.034f,.5f,1.65f+i*.09f,Brass,6,.034f);
                Orb(m,x,3.68f,1.65f+i*.09f,.085f,Gems[i]);
            }
            // Orrerio suspenso ao fundo, inspirado na referencia, com o centro livre.
            Ring(m,2.65f,3.88f,3.75f,.83f,.035f,Brass,true);
            Ring(m,2.65f,3.88f,3.75f,.61f,.026f,ObliqueMeshData.Hex("D3BBEF"),false);
            Orb(m,2.65f,3.88f,3.4f,.33f,Gems[2]);
            Orb(m,1.9f,3.88f,3.64f,.15f,Gems[1]);
            Orb(m,3.42f,3.88f,3.61f,.14f,Gems[4]);
            return m;
        }
        private static void Curtain(ObliqueMeshData m,float x)
        {
            Solid(m);
            for(int i=0;i<8;i++) m.Box(x-.62f+i*.18f,4.29f,.2f,.1f,.35f,3.08f,
                ObliqueMeshData.Hex(i%2==0?"463076":"684692"));
            m.Box(x,4.18f,1.55f,.12f,2.98f,3.12f,Brass);
            m.Box(x,4.16f,1.3f,.11f,1.24f,1.32f,Brass);
        }
        private static void Shelf(ObliqueMeshData m,float x,float y,float w,float d,int rows,bool rear)
        {
            Tile(m,0); m.Box(x,y+d*.45f,w,.09f,0,3.04f,Color.white);
            foreach(float side in new[]{-1f,1f}) m.Box(x+side*w*.5f,y,.08f,d,0,3.04f,Color.white);
            for(int row=0;row<rows;row++) {
                float h=.24f+row*.69f;
                Tile(m,0); m.Box(x,y,w,d,h,h+.09f,Color.white);
                Solid(m); m.Box(x,y-d*.5f,w,.035f,h+.07f,h+.12f,Brass);
                int count=rear?9:5;
                for(int i=0;i<count;i++) {
                    float gx=x-w*.41f+i*w*.82f/(count-1), gy=y-d*.36f;
                    if((i+row)%3==0) {
                        m.Box(gx,gy,.12f,.17f,h+.1f,h+.41f,ObliqueMeshData.Hex(Gems[(i+row)%6]));
                        m.Box(gx,gy-.095f,.12f,.018f,h+.16f,h+.19f,Brass);
                    } else if((i+row)%4==0) {
                        m.Cone(gx,gy,.1f,h+.1f,h+.35f,ObliqueMeshData.Hex("947AB8"),8,.065f);
                        m.Cone(gx,gy,.035f,h+.35f,h+.45f,Brass,6,.035f);
                    } else Orb(m,gx,gy,h+.12f,.13f,Gems[(row*2+i)%6]);
                }
            }
        }
        private static void Orb(ObliqueMeshData m,float x,float y,float bottom,float radius,string hex)
        {
            Solid(m); Color color=ObliqueMeshData.Hex(hex);
            float[] heights={-.98f,-.7f,0,.7f,.98f};
            float[] radii={.05f,.71f,1,.71f,.05f};
            for(int band=0;band<4;band++)
                m.Cone(x,y,radius*radii[band],bottom+radius*(1+heights[band]),
                    bottom+radius*(1+heights[band+1]),color,10,radius*radii[band+1]);
            m.Box(x-radius*.32f,y-radius*.86f,radius*.24f,.014f,
                bottom+radius*.96f,bottom+radius*1.47f,ObliqueMeshData.Hex("E6FFFF"));
        }
        private static void Ring(ObliqueMeshData m,float x,float y,float h,float r,float thickness,Color color,bool upright)
        {
            Solid(m);
            for(int i=0;i<48;i++) {
                double a=i*Math.PI/24,b=(i+1)*Math.PI/24;
                Vector3 P(double angle,float radius) => upright
                    ? ObliqueMeshData.V(x+(float)Math.Cos(angle)*radius,y,h+(float)Math.Sin(angle)*radius)
                    : ObliqueMeshData.V(x+(float)Math.Cos(angle)*radius,y+(float)Math.Sin(angle)*radius,h);
                m.Quad(P(a,r),P(b,r),P(b,r+thickness),P(a,r+thickness),color,false);
            }
        }
    }
}
