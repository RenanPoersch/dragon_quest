using System;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Sprites
{
    // Coordinates are inspected from the source PNG; no pixel rewriting or per-pose resizing.
    public sealed class UltimateSpriteSheet
    {
        [Serializable] public sealed class Frame
        {
            public int x,y,width,height;
            public float pivotX,pivotY;
        }
        [Serializable] public sealed class Atlas
        {
            public int width,height;
            public float referenceHeight;
            public Frame[] frames;
        }
        private readonly Texture2D texture;
        private readonly Atlas atlas;
        private readonly Action<Texture2D,Rect,Rect,Color> drawFrame;
        public int FrameCount => atlas.frames.Length;
        public UltimateSpriteSheet(string name,Action<Texture2D,Rect,Rect,Color> drawFrame=null)
        {
            this.drawFrame=drawFrame;
            string path="PixelArt/Ultimates/"+name;
            texture=Resources.Load<Texture2D>(path);
            var coordinates=Resources.Load<TextAsset>(path+"Frames");
            if(texture==null || coordinates==null) throw new InvalidOperationException("Ultimate atlas missing: "+path);
            atlas=JsonUtility.FromJson<Atlas>(coordinates.text);
            if(atlas==null || atlas.frames==null || atlas.frames.Length!=16 || atlas.referenceHeight<=0
                || atlas.width!=texture.width || atlas.height!=texture.height)
                throw new InvalidOperationException("Invalid ultimate atlas coordinates: "+path);
            texture.filterMode=FilterMode.Point;
        }
        private void Draw(Rect destination, int frame, Color tint,bool mirror=false)
        {
            Frame cell=atlas.frames[Mathf.Clamp(frame,0,atlas.frames.Length-1)];
            Rect uv=new Rect((float)cell.x/atlas.width,1f-(float)(cell.y+cell.height)/atlas.height,
                (float)cell.width/atlas.width,(float)cell.height/atlas.height);
            if(mirror) { uv.x+=uv.width; uv.width=-uv.width; }
            if(drawFrame!=null) { drawFrame(texture,destination,uv,tint); return; }
            Color previous=GUI.color; GUI.color=tint;
            GUI.DrawTextureWithTexCoords(destination,texture,uv,true);
            GUI.color=previous;
        }
        public void DrawActor(Rect home, int frame, Color tint, bool mirror=false)
        {
            Frame cell=atlas.frames[Mathf.Clamp(frame,0,atlas.frames.Length-1)];
            float scale=home.height/atlas.referenceHeight;
            Rect destination=new Rect(home.center.x-(mirror?cell.width-cell.pivotX:cell.pivotX)*scale,home.yMax-cell.pivotY*scale,
                cell.width*scale,cell.height*scale);
            Draw(destination,frame,tint,mirror);
        }
        public void DrawCanvas(Rect area, int frame, Color tint) => Draw(area,frame,tint);
    }
}
