using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Todas as mercadorias e a mobilia sao sprites; a planta continua fisica em XY.
    public sealed class ShopPixelArt : IDisposable
    {
        public const int Columns=5, Rows=4, Count=20, CellPixels=96;
        private Texture2D pixels;
        private readonly Sprite[] sprites=new Sprite[Count];
        private readonly Material material;
        // Janelas medidas no PNG original (origem superior). Os desenhos nao seguem
        // exatamente as celulas pedidas ao gerador; recortes permanecem em memoria.
        private static readonly RectInt[] Regions={
            new RectInt(19,5,200,298),new RectInt(236,114,297,204),new RectInt(541,10,201,304),new RectInt(754,102,266,229),new RectInt(1029,10,219,320),
            new RectInt(12,306,209,338),new RectInt(226,399,314,246),new RectInt(552,313,165,341),new RectInt(784,323,154,329),new RectInt(998,342,243,304),
            new RectInt(34,641,206,322),new RectInt(296,694,167,258),new RectInt(542,667,168,297),new RectInt(713,656,268,307),new RectInt(997,710,253,256),
            new RectInt(16,1009,279,229),new RectInt(332,971,116,270),new RectInt(530,962,193,282),new RectInt(771,968,205,276),new RectInt(1014,971,222,272)
        };
        public ShopPixelArt(Material sharedMaterial)
        {
            material=sharedMaterial;
            var source=Resources.Load<Texture2D>("PixelArt/ShopPropsAtlas");
            if(source==null)throw new InvalidOperationException("Atlas pixel art das lojas ausente.");
            var original=source.GetPixels32();
            // Grade logica de pixels consistente com os personagens, somente em memoria.
            pixels=new Texture2D(Columns*CellPixels,Rows*CellPixels,TextureFormat.RGBA32,false);
            pixels.name="Lojas / sprites pixel art 96px";
            pixels.filterMode=FilterMode.Point;pixels.wrapMode=TextureWrapMode.Clamp;
            var sampled=SampleAtlas(original,source.width,source.height);
            pixels.SetPixels32(sampled);pixels.Apply(false,false);
            for(int index=0;index<Count;index++) {
                RectInt bounds=CellBounds(index,sampled);
                sprites[index]=Sprite.Create(pixels,new Rect(bounds.x,bounds.y,bounds.width,bounds.height),new Vector2(.5f,0),CellPixels,0,SpriteMeshType.FullRect);
                sprites[index].name="Decoracao pixel art / "+index;
            }
        }
        public static Color32[] SampleAtlas(Color32[] original,int width,int height)
        {
            if(original==null||original.Length!=width*height||width!=1254||height!=1254)throw new ArgumentException("Dimensoes do atlas das lojas invalidas.");
            var result=new Color32[Columns*Rows*CellPixels*CellPixels];
            for(int index=0;index<Count;index++) {
                RectInt r=Regions[index];int rw=r.width,rh=r.height;
                var labels=new int[rw*rh];int label=0,best=0,bestCount=0;
                var queue=new Queue<int>();
                // Seleciona o objeto inteiro, excluindo pixels de vizinhos nas janelas.
                for(int i=0;i<labels.Length;i++) {
                    int sourceIndex=(height-1-r.y-i/rw)*width+r.x+i%rw;
                    if(labels[i]!=0||original[sourceIndex].a<128)continue;
                    label++;labels[i]=label;queue.Enqueue(i);int count=0;
                    while(queue.Count>0) {
                        int current=queue.Dequeue();count++;int x=current%rw,y=current/rw;
                        foreach(int next in new[]{x>0?current-1:-1,x<rw-1?current+1:-1,y>0?current-rw:-1,y<rh-1?current+rw:-1}) {
                            if(next<0||labels[next]!=0)continue;
                            int si=(height-1-r.y-next/rw)*width+r.x+next%rw;
                            if(original[si].a<128)continue;
                            labels[next]=label;queue.Enqueue(next);
                        }
                    }
                    if(count>bestCount){bestCount=count;best=label;}
                }
                if(bestCount<1000)throw new InvalidOperationException("Objeto ausente no atlas: "+index);
                float scale=Math.Min((CellPixels-6f)/rw,(CellPixels-6f)/rh);
                int dw=Math.Max(1,(int)Math.Round(rw*scale)),dh=Math.Max(1,(int)Math.Round(rh*scale));
                int ox=index%Columns*CellPixels+(CellPixels-dw)/2,oy=(Rows-1-index/Columns)*CellPixels+3;
                for(int y=0;y<dh;y++)for(int x=0;x<dw;x++) {
                    int sx=Math.Min(rw-1,(int)((x+.5f)*rw/dw)),sy=Math.Min(rh-1,(int)((y+.5f)*rh/dh));
                    if(labels[sy*rw+sx]!=best)continue;
                    Color32 c=original[(height-1-r.y-sy)*width+r.x+sx];c.a=255;
                    result[(oy+dh-1-y)*Columns*CellPixels+ox+x]=c;
                }
            }
            return result;
        }
        public static RectInt CellBounds(int index,Color32[] alpha)
        {
            if(index<0||index>=Count||alpha==null||alpha.Length!=Columns*Rows*CellPixels*CellPixels)throw new ArgumentException("Grade de sprites invalida.");
            int left=index%Columns*CellPixels,bottom=(Rows-1-index/Columns)*CellPixels;
            int minX=left+CellPixels,minY=bottom+CellPixels,maxX=left-1,maxY=bottom-1;
            for(int y=bottom;y<bottom+CellPixels;y++)for(int x=left;x<left+CellPixels;x++)if(alpha[y*Columns*CellPixels+x].a>=128) {
                minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
            }
            if(maxX<minX)throw new InvalidOperationException("Sprite vazio na loja: "+index);
            return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        private void Prop(Transform interior,int index,float x,float y,float width,float height,float altitude=0)
        {
            // Ancora fora da escala nao uniforme da planta: pixels e proporcoes ficam intactos.
            var anchor=new GameObject("Loja / prop pixel art "+index);
            anchor.transform.SetParent(interior.parent,false);
            anchor.transform.localPosition=new Vector3(x*PrototypeShopLayout.WidthScale,y*PrototypeShopLayout.DepthScale,-altitude);
            var visual=new GameObject(sprites[index].name);visual.transform.SetParent(anchor.transform,false);
            var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=sprites[index];renderer.sharedMaterial=material;
            visual.AddComponent<ShopPixelBillboard>().Initialize(width,height);
        }
        public readonly struct ShopProp
        {
            public readonly int Index;
            public readonly float X,Y,Width,Height,Altitude;
            public ShopProp(int index,float x,float y,float width,float height,float altitude)
            { Index=index;X=x;Y=y;Width=width;Height=height;Altitude=altitude; }
        }
        public void Populate(Transform interior,string kind)
        {
            foreach(var p in Layout(kind))Prop(interior,p.Index,p.X,p.Y,p.Width,p.Height,p.Altitude);
        }
        public static ShopProp[] Layout(string kind)
        {
            var props=new List<ShopProp>();
            void Prop(int index,float x,float y,float width,float height,float altitude=0) => props.Add(new ShopProp(index,x,y,width,height,altitude));
            bool items=kind=="item-shop",materia=kind=="materia-shop";
            if(items) {
                Prop(0,-4.8f,1.15f,1.62f,2.16f);Prop(0,4.8f,1.15f,1.62f,2.16f);
                Prop(1,0,-.12f,3.15f,1.12f);
                foreach(float x in new[]{-3.2f,3.2f})Prop(17,x,4.2f,1.03f,2.24f);
                Prop(19,5.35f,-2.65f,1.08f,1.5f);
                Prop(0,-2.9f,3.72f,1.13f,1.7f);Prop(0,2.9f,3.72f,1.13f,1.7f);
                foreach(float x in new[]{-2.75f,2.75f})Prop(16,x,4.08f,.35f,.61f,1.56f);
            }
            else if(materia) {
                Prop(2,4.8f,1.15f,1.62f,2.28f);
                Prop(3,0,-.12f,3.15f,1.15f);
                Prop(18,-4.8f,1.6f,1.8f,2.3f);
                foreach(float x in new[]{-2.8f,0,2.8f})Prop(2,x,4.12f,1.67f,2.15f);
                foreach(float x in new[]{-5.8f,5.8f})Prop(17,x,4.25f,1.04f,2.45f);
                Prop(9,-4.9f,-2.5f,1.1f,1.08f);Prop(11,4.9f,-2.5f,1.02f,1.39f);
                Prop(10,5.1f,3.55f,.83f,1.52f);Prop(13,2.65f,3.88f,1.36f,1.26f,2.73f);
            }
            else {
                Prop(4,-4.8f,1.15f,1.62f,2.26f);Prop(5,4.8f,1.15f,1.62f,2.26f);
                Prop(6,0,-.12f,3.15f,1.14f);
                Prop(7,-4.9f,-2.5f,1.04f,1.53f);Prop(8,4.9f,-2.5f,1.04f,1.53f);
                foreach(float x in new[]{-2.4f,0,2.4f})Prop(4,x,4.16f,1.48f,2.04f);
                foreach(float x in new[]{-5.75f,5.75f})Prop(12,x,4.21f,.7f,1.85f,.54f);
                Prop(14,-2.8f,3.1f,1.15f,.97f);Prop(15,2.8f,3.1f,1.15f,.85f);
                foreach(float x in new[]{-3.75f,3.75f})Prop(16,x,4.1f,.36f,.63f,1.8f);
            }
            return props.ToArray();
        }
        public static ObliqueMeshData Shell(string kind)
        {
            var m=new ObliqueMeshData();m.DefaultSurface=(PixelSurface)7;
            m.Plane(0,0,14,10,.02f,Color.white);m.DefaultSurface=(PixelSurface)0;
            m.Box(0,4.7f,14,.6f,0,2.8f,Color.white);
            foreach(float x in new[]{-6.7f,6.7f})m.Box(x,0,.6f,10,0,.5f,Color.white);
            m.Box(0,-4.7f,14,.6f,0,.18f,Color.white);
            m.DefaultSurface=(PixelSurface)2;m.Plane(0,-2.2f,3.35f,3.65f,.035f,Color.white);
            m.DefaultSurface=(PixelSurface)1;m.Plane(0,-2.2f,3.1f,3.4f,.04f,Color.white);
            return m;
        }
        public void Dispose() {
            foreach(var sprite in sprites)if(sprite!=null)UnityEngine.Object.Destroy(sprite);
            if(pixels!=null)UnityEngine.Object.Destroy(pixels);
        }
    }
    public sealed class ShopPixelBillboard : MonoBehaviour
    {
        private float width,height;
        private Camera view;
        public void Initialize(float w,float h) {width=w;height=h;view=Camera.main;Face();}
        private void LateUpdate()=>Face();
        private void Face() {
            if(view==null)view=Camera.main;
            Vector3 forward=view!=null?view.transform.forward:new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0);
            transform.rotation=Quaternion.LookRotation(PixelSpriteLayout.HorizontalCameraForward(forward),PixelSpriteLayout.UprightUp);
            var sprite=GetComponent<SpriteRenderer>().sprite;
            // Uniforme por sprite, com limites de enquadramento e sem distorcer a arte.
            float scale=Math.Min(width/sprite.bounds.size.x,height/sprite.bounds.size.y);
            transform.localScale=new Vector3(scale,scale*PixelSpriteLayout.UprightScaleY,1);
        }
    }
}
