using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // O circulo marca o chao sem bloquear a passagem. A arte anima em oito quadros, com a base registrada.
    public sealed class InnSavePoint : Interactable
    {
        public const int Columns=4, Rows=2, FrameCount=8;
        public const float FramesPerSecond=6f, Height=.95f;
        private readonly Sprite[] frames=new Sprite[FrameCount];
        private SpriteRenderer view;
        private Material material;
        private Action save;
        private float elapsed;
        public Texture2D Source { get; private set; }
        public int CurrentFrame { get; private set; }
        public IReadOnlyList<Sprite> Frames => frames;

        public void Initialize(Action manualSave)
        {
            if(view!=null) throw new InvalidOperationException("Ponto de salvamento ja inicializado.");
            save=manualSave;
            Source=Resources.Load<Texture2D>("PixelArt/InnSavePoint");
            var shader=Resources.Load<Shader>("PixelCharacter");
            if(Source==null||shader==null||!Source.isReadable||Source.width%Columns!=0||Source.height%Rows!=0)
                throw new InvalidOperationException("Atlas do save deve ser legivel, com quatro colunas e duas linhas.");
            int width=Source.width/Columns,height=Source.height/Rows;
            Color32[] pixels=Source.GetPixels32();
            var anchors=new Vector2[FrameCount];int tallest=1;
            for(int i=0;i<FrameCount;i++)
            {
                int left=i%Columns*width,bottom=(Rows-1-i/Columns)*height;
                anchors[i]=FloorAnchor(pixels,Source.width,left,bottom,width,height,out int top);
                tallest=Math.Max(tallest,top-(int)anchors[i].y+1);
            }
            // Todos os quadros compartilham a escala; faiscas e brilho nao redimensionam o cristal.
            float pixelsPerUnit=tallest/(Height*ObliqueProjection.HeightScale);
            for(int i=0;i<FrameCount;i++)
            {
                int left=i%Columns*width,bottom=(Rows-1-i/Columns)*height;
                var pivot=new Vector2((anchors[i].x-left)/width,(anchors[i].y-bottom)/height);
                frames[i]=Sprite.Create(Source,new Rect(left,bottom,width,height),pivot,pixelsPerUnit,0,SpriteMeshType.FullRect);
                frames[i].name="Cristal de save / "+i;
            }
            var visual=new GameObject("Save / cristal animado");visual.transform.SetParent(transform,false);
            visual.transform.localPosition=Vector3.back*.035f;
            visual.transform.localRotation=Quaternion.LookRotation(new Vector3(-ObliqueProjection.YawSin,ObliqueProjection.YawCos,0),PixelSpriteLayout.UprightUp);
            visual.transform.localScale=new Vector3(1,PixelSpriteLayout.UprightScaleY,1);
            material=new Material(shader){name="Pousada / cristal de salvamento"};
            view=visual.AddComponent<SpriteRenderer>();view.sharedMaterial=material;
            elapsed=0;ShowAt(0);
        }

        private static Vector2 FloorAnchor(Color32[] pixels,int stride,int left,int bottom,int width,int height,out int top)
        {
            int lowest=-1;top=-1;int threshold=Math.Max(6,width/40);
            for(int y=0;y<height;y++)
            {
                int count=0;
                for(int x=0;x<width;x++) if(pixels[(bottom+y)*stride+left+x].a>=128) count++;
                if(count>0) top=bottom+y;
                if(lowest<0&&count>=threshold) lowest=y;
            }
            if(lowest<0||top<0) throw new InvalidOperationException("Quadro vazio do cristal de save.");
            int bestCount=0;float center=left+width*.5f;
            // O trecho inferior contem o circulo luminoso; registra-lo evita que as faiscas movam a base.
            for(int y=lowest;y<Math.Min(height,lowest+height/5);y++)
            {
                int min=width,max=-1,count=0;
                for(int x=0;x<width;x++) if(pixels[(bottom+y)*stride+left+x].a>=128)
                {min=Math.Min(min,x);max=Math.Max(max,x);count++;}
                if(count>bestCount){bestCount=count;center=left+(min+max+1)*.5f;}
            }
            return new Vector2(center,bottom+lowest-1);
        }
        public static int FrameAt(float seconds) => float.IsNaN(seconds)||float.IsInfinity(seconds)?0
            :(int)(Math.Floor(Math.Max(0,(double)seconds)*FramesPerSecond)%FrameCount);
        public void ShowAt(float seconds)
        {
            if(view==null) return;
            CurrentFrame=FrameAt(seconds);
            if(view.sprite!=frames[CurrentFrame]) view.sprite=frames[CurrentFrame];
        }
        private void OnEnable(){elapsed=0;ShowAt(0);}
        private void Update(){elapsed=Mathf.Repeat(elapsed+Time.deltaTime,FrameCount/FramesPerSecond);ShowAt(elapsed);}
        public override void Interact(InteractionContext context) => save?.Invoke();
        private void OnDestroy()
        {
            foreach(Sprite frame in frames) if(frame!=null){if(Application.isPlaying)Destroy(frame);else DestroyImmediate(frame);}
            if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
}
