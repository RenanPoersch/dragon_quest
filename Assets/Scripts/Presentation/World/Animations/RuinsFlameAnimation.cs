using System;
using UnityEngine;

namespace DragonQuest.Presentation.World.Animations
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RuinsFlameAnimation : MonoBehaviour
    {
        public const int FrameCount = 8, FramesPerSecond = 10;
        private const int Columns = 4, Rows = 2, PixelWidth = 32, PixelHeight = 48;
        private readonly Sprite[] frames = new Sprite[FrameCount];
        private readonly Texture2D[] textures = new Texture2D[FrameCount];
        private SpriteRenderer renderer;
        private float elapsed, phaseOffset;
        public int CurrentFrame { get; private set; }

        public void Initialize(Material material, float phaseOffset = 0)
        {
            var source=Resources.Load<Texture2D>("PixelArt/RuinsAltarFlame");
            if(source==null || !source.isReadable || source.width%Columns!=0 || source.height%Rows!=0)
                throw new InvalidOperationException("Atlas da chama deve ser legivel, com quatro colunas e duas linhas.");
            renderer=GetComponent<SpriteRenderer>();renderer.sharedMaterial=material;
            Color32[] original=source.GetPixels32();
            int width=source.width/Columns,height=source.height/Rows;
            for(int frame=0;frame<FrameCount;frame++)
            {
                int left=frame%Columns*width,bottom=(Rows-1-frame/Columns)*height;
                var pixels=new Color32[PixelWidth*PixelHeight];
                for(int y=0;y<PixelHeight;y++) for(int x=0;x<PixelWidth;x++)
                {
                    int sx=left+(int)((x+.5f)*width/PixelWidth);
                    int sy=bottom+(int)((y+.5f)*height/PixelHeight);
                    pixels[y*PixelWidth+x]=original[sy*source.width+sx];
                }
                var texture=new Texture2D(PixelWidth,PixelHeight,TextureFormat.RGBA32,false)
                    {name="Chama do altar / "+frame,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                texture.SetPixels32(pixels);texture.Apply(false,true);textures[frame]=texture;
                // Register every flame's foot, keeping its base fixed as the drawn tongues flicker.
                Vector2 pivot=FindFoot(original,source.width,left,bottom,width,height);
                frames[frame]=Sprite.Create(texture,new Rect(0,0,PixelWidth,PixelHeight),pivot,48,0,SpriteMeshType.FullRect);
            }
            this.phaseOffset = float.IsNaN(phaseOffset) || float.IsInfinity(phaseOffset) ? 0
                : Mathf.Repeat(phaseOffset, (float)FrameCount / FramesPerSecond);
            elapsed = this.phaseOffset; ShowAt(elapsed);
        }

        public static int FrameAt(float seconds)
            => float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0
                : (int)(Math.Floor(Math.Max(0,seconds)*FramesPerSecond)%FrameCount);

        public void ShowAt(float seconds)
        {
            if(renderer==null || frames[0]==null) return;
            CurrentFrame=FrameAt(seconds);
            if(renderer.sprite!=frames[CurrentFrame]) renderer.sprite=frames[CurrentFrame];
        }

        private void Update()
        {
            elapsed=Mathf.Repeat(elapsed+Time.deltaTime,(float)FrameCount/FramesPerSecond);
            ShowAt(elapsed);
        }
        private void OnEnable() { elapsed=phaseOffset;ShowAt(elapsed); }
        private void OnDisable() { elapsed=0; }

        private static Vector2 FindFoot(Color32[] pixels,int stride,int left,int bottom,int width,int height)
        {
            int foot=0;
            for(int y=0;y<height;y++)
            {
                int count=0;
                for(int x=width/4;x<width*3/4;x++) if(pixels[(bottom+y)*stride+left+x].a>=128) count++;
                if(count>=Math.Max(2,width/24)) { foot=y;break; }
            }
            float totalX=0;int total=0;
            for(int y=foot;y<Math.Min(height,foot+height/16);y++)
                for(int x=width/4;x<width*3/4;x++)
                    if(pixels[(bottom+y)*stride+left+x].a>=128) { totalX+=x+.5f;total++; }
            return new Vector2(total>0?totalX/total/width:.5f,(float)foot/height);
        }

        private void OnDestroy()
        {
            foreach(var frame in frames) Release(frame);
            foreach(var texture in textures) Release(texture);
        }
        private static void Release(UnityEngine.Object asset)
        {
            if(asset==null) return;
            if(Application.isPlaying) Destroy(asset);else DestroyImmediate(asset);
        }
    }
}
