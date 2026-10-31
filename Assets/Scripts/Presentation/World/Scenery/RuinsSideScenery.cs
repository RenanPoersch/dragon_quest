using DragonQuest.Infrastructure;
using DragonQuest.Presentation.World.Animations;
using DragonQuest.Exploration.Layouts;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.World.Scenery
{
    // Pixel art layers have their own depth so foreground pillars really occlude the hero.
    [DefaultExecutionOrder(120)]
    public sealed class RuinsSideScenery : MonoBehaviour
    {
        private readonly List<Transform> planes = new List<Transform>();
        private readonly List<float> compensation = new List<float>();
        private readonly List<Object> pixelAssets = new List<Object>();
        private Material material;
        private Camera view;
        private Sprite fullColumn, brokenColumn, rearColumn, rearBrokenColumn, frontColumn;

        public Transform Build(out GameObject flame)
        {
            material = new Material(GameAssets.Required<Shader>("RuinsSide"));
            fullColumn=PixelSprite("PixelArt/RuinsPaintedColumns",new Rect(88,101,420,1332),160,new Vector2(.5f,0));
            brokenColumn=PixelSprite("PixelArt/RuinsPaintedColumns",new Rect(576,101,394,713),86,new Vector2(.5f,0));
            rearColumn=PixelSprite("PixelArt/RuinsPaintedColumns",new Rect(88,101,420,1332),160,new Vector2(.5f,0),new Color(.42f,.49f,.56f));
            rearBrokenColumn=PixelSprite("PixelArt/RuinsPaintedColumns",new Rect(576,101,394,713),86,new Vector2(.5f,0),new Color(.42f,.49f,.56f));
            frontColumn=PixelSprite("PixelArt/RuinsPaintedColumns",new Rect(88,101,420,1332),160,new Vector2(.5f,0),new Color(.78f,.76f,.72f));
            var panorama=Plane("Panorama desenhado / mar e montanhas",14,.90f);
            Sprite background=PixelSprite("PixelArt/RuinsPaintedPanorama",default,193,new Vector2(.5f,0));
            for(int tile=-1;tile<=1;tile++)
            {
                var go=new GameObject("Panorama / sprite de pixel art");go.transform.SetParent(panorama.Root,false);
                go.transform.localPosition=new Vector3(tile*26,-1.4f,14);
                go.transform.localScale=new Vector3(26/background.bounds.size.x,9.8f/background.bounds.size.y,1);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=background;renderer.sharedMaterial=material;
            }
            var rear=Plane("Colunata escura ao fundo",7,.48f);
            rear.Rect(-45,-.5f,125,1,"4A4941");
            for(int i=0;i<18;i++)
            {
                float x=-28+i*5.6f;
                Column(rear,x,.5f,i%4==1?3.4f:6,i%4==1);
            }

            var main = Plane("Templo / piso e pedras", 2, 0);
            main.Rect(-30, -4, 100, 4, "4C4840");
            main.Rect(-30, -.17f, 100, .17f, "AAA087");
            main.Rect(-30, -.48f, 100, .3f, "756C59");
            for (int i = -12; i < 30; i++)
            {
                float x = i * 1.65f;
                main.Rect(x, -.46f, .035f, .29f, "514B40");
                main.Rect(x + .35f, -.8f, .7f, .045f, "5F594D");
            }
            // Broken Doric columns and scattered blocks flank the corridor, behind the walking plane.
            foreach (float x in new[] {-1f, 8f, 17f, 35f, 40f})
                Column(main, x, 0, x == 17 ? 2.4f : 6.6f, x == 17);
            main.Polygon(new[] {new Vector2(3.6f,0),new Vector2(3.8f,.45f),new Vector2(5,.38f),new Vector2(5.4f,0)},"817965");
            main.Rect(12.4f, .06f, 2.4f, .5f, "8C826C");
            main.Rect(12.7f, .56f, 1.7f, .18f, "B0A183");
            main.Rect(21.4f, 0, .9f, .35f, "93866F");
            // Three actual silhouettes, with a broad landing beneath the altar.
            for (int i = 0; i < 3; i++)
            {
                float x = 26 + i * .8f, y = i * .35f;
                main.Rect(x, y, 14 - i * .8f, .35f, "8D8068");
                main.Rect(x, y + .29f, 14 - i * .8f, .06f, "C3B18D");
                main.Rect(x, y, .06f, .29f, "605B4D");
            }

            var altar = new GameObject("Altar antigo");
            altar.transform.SetParent(transform, false);
            altar.transform.localPosition = RuinsLayout.Altar;
            altar.transform.localScale = new Vector3(.65f,.65f,.65f);
            var shrine = new Painter(altar.transform, 1.5f, material, pixelAssets);
            shrine.Rect(-.9f, 0, 1.8f, .18f, "C2AE84");
            shrine.Rect(-.63f, .18f, 1.26f, .92f, "9F8C6C");
            shrine.Rect(-.8f, 1.1f, 1.6f, .23f, "D2BD93");
            shrine.Rect(-.46f, .33f, .92f, .59f, "70634F");
            shrine.Polygon(new[] {new Vector2(0,.41f),new Vector2(-.22f,.65f),new Vector2(-.07f,.73f),new Vector2(.02f,.87f),new Vector2(.23f,.61f)}, "C6AA70");
            shrine.Commit("Altar / chama gravada na pedra");
            var bookObject=new GameObject("Livro de pedra / inscricoes antigas");
            bookObject.transform.SetParent(altar.transform,false);
            var book=new Painter(bookObject.transform,1.35f,material,pixelAssets);
            // Two carved stone leaves, raised at the back so the engraved writing is visible in side view.
            book.Polygon(new[] {new Vector2(-.8f,1.38f),new Vector2(-.8f,1.54f),new Vector2(0,1.48f),new Vector2(.8f,1.54f),new Vector2(.8f,1.38f),new Vector2(0,1.33f)},"7F7D6C");
            book.Polygon(new[] {new Vector2(-.8f,1.54f),new Vector2(-.65f,1.98f),new Vector2(-.05f,1.9f),new Vector2(0,1.48f)},"DBD4B5");
            book.Polygon(new[] {new Vector2(0,1.48f),new Vector2(.05f,1.9f),new Vector2(.65f,1.98f),new Vector2(.8f,1.54f)},"BBB89F");
            book.Rect(-.035f,1.47f,.07f,.44f,"696E61");
            book.Rect(-.69f,1.9f,.48f,1f/24,"EEE4C5");
            book.Rect(.2f,1.9f,.48f,1f/24,"D9D4B7");
            int[] runes={0x16D,0x1AF,0x0BA,0x1E5,0x157,0x0ED};
            for(int page=0;page<2;page++) for(int row=0;row<3;row++) for(int col=0;col<2;col++)
            {
                float x=page==0?-.59f+col*.24f:.16f+col*.24f;
                float y=1.55f+row*.115f;
                int rune=runes[(page*3+row+col)%runes.Length];
                for(int py=0;py<3;py++) for(int px=0;px<3;px++)
                    if((rune&(1<<(py*3+px)))!=0) book.Rect(x+px/24f,y+py/24f,1f/24,1f/24,"676E62");
            }
            book.Commit("Livro aberto / paginas e runas entalhadas");
            flame = new GameObject("Chama do altar resolvido");
            flame.transform.SetParent(altar.transform, false);
            flame.transform.localPosition=new Vector3(0,2.02f,.85f);
            flame.AddComponent<RuinsFlameAnimation>().Initialize(material);
            flame.SetActive(false);

            var front = Plane("Pilares em primeiro plano", -2, -.18f);
            foreach (float x in new[] {-4f, 5.5f, 14f, 22f, 39f, 47f})
                Column(front, x, -.65f, 9.8f, false);
            front.Rect(-40, -3, 120, 2.2f, "393D35");
            foreach (float x in new[] {2f, 11f, 19f, 34f, 43f})
                front.Polygon(new[] {new Vector2(x,-.8f),new Vector2(x+.2f,-.32f),new Vector2(x+.7f,-.4f),new Vector2(x+1,-.8f)},"535446");
            foreach (var plane in painters) plane.Commit(plane.Root.name + " / geometria");
            painters.Clear();
            return altar.transform;
        }

        private readonly List<Painter> painters = new List<Painter>();
        private Painter Plane(string name, float z, float factor)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            planes.Add(root); compensation.Add(factor);
            var painter = new Painter(root, z, material, pixelAssets); painters.Add(painter); return painter;
        }

        private void Column(Painter p, float x, float y, float h, bool broken)
        {
            Sprite source = broken ? brokenColumn : fullColumn;
            if(p.Root.name.Contains("Colunata")) source=broken ? rearBrokenColumn : rearColumn;
            else if(p.Root.name.Contains("primeiro")) source=frontColumn;
            var go=new GameObject(broken ? "Pilar quebrado / sprite desenhado" : "Pilar / sprite desenhado");
            go.transform.SetParent(p.Root,false);go.transform.localPosition=new Vector3(-x,y,p.Depth-.08f);
            float scale=h/source.bounds.size.y;
            go.transform.localScale=Vector3.one*scale;
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=source;renderer.sharedMaterial=material;
        }

        // Sample imported artwork onto a fixed pixel grid, preserving the drawing and its alpha.
        private Sprite PixelSprite(string resource, Rect crop, int pixelHeight, Vector2 pivot, Color? tint=null)
        {
            Texture2D source=GameAssets.Required<Texture2D>(resource);
            if(crop.width==0) crop=new Rect(0,0,source.width,source.height);
            int width=Mathf.RoundToInt(pixelHeight*crop.width/crop.height);
            Color32[] original=source.GetPixels32(),pixels=new Color32[width*pixelHeight];
            for(int y=0;y<pixelHeight;y++) for(int x=0;x<width;x++)
            {
                int sx=Mathf.Clamp((int)(crop.x+(x+.5f)*crop.width/width),0,source.width-1);
                int sy=Mathf.Clamp((int)(crop.y+(y+.5f)*crop.height/pixelHeight),0,source.height-1);
                Color32 color=original[sy*source.width+sx];
                if(tint.HasValue) color=new Color32((byte)(color.r*tint.Value.r),(byte)(color.g*tint.Value.g),(byte)(color.b*tint.Value.b),color.a);
                pixels[y*width+x]=color;
            }
            var texture=new Texture2D(width,pixelHeight,TextureFormat.RGBA32,false)
                {name=resource+" / grade de pixels",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);pixelAssets.Add(texture);
            var sprite=Sprite.Create(texture,new Rect(0,0,width,pixelHeight),pivot,24,0,SpriteMeshType.FullRect);
            pixelAssets.Add(sprite);return sprite;
        }

        public void UpdateParallax(float cameraX)
        {
            for (int i=0;i<planes.Count;i++) planes[i].localPosition = new Vector3(cameraX*compensation[i],0,0);
        }
        private void LateUpdate()
        {
            if (view == null) view = Camera.main;
            if (view != null) UpdateParallax(view.transform.position.x);
        }
        private void OnDestroy()
        {
            foreach (Object asset in pixelAssets) Destroy(asset);
            if (material != null) Destroy(material);
        }

        private sealed class Painter
        {
            public readonly Transform Root;
            public float Depth => z;
            private readonly float z;
            private readonly Material material;
            private readonly List<Object> owned;
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int> indices = new List<int>();
            public Painter(Transform root, float depth, Material mat, List<Object> assets)
            { Root=root; z=depth; material=mat; owned=assets; }
            public void Rect(float x,float y,float width,float height,string hex)
                => Polygon(new[] {new Vector2(x,y),new Vector2(x,y+height),new Vector2(x+width,y+height),new Vector2(x+width,y)},hex);
            public void Polygon(Vector2[] points,string hex)
            {
                ColorUtility.TryParseHtmlString("#"+hex,out Color color);
                int start=vertices.Count;
                foreach(var point in points) { vertices.Add(new Vector3(-point.x,point.y,z)); colors.Add(color); }
                for(int i=1;i<points.Length-1;i++) { indices.Add(start); indices.Add(start+i); indices.Add(start+i+1); }
            }
            public void Commit(string name)
            {
                if(vertices.Count==0) return;
                // Bake each layer onto an actual low-resolution bitmap, with nearest-neighbour sampling.
                const int ppu = 24;
                float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
                foreach(var v in vertices) { minX=Mathf.Min(minX,v.x); minY=Mathf.Min(minY,v.y); maxX=Mathf.Max(maxX,v.x); maxY=Mathf.Max(maxY,v.y); }
                minX=Mathf.Floor(minX*ppu)/ppu; minY=Mathf.Floor(minY*ppu)/ppu;
                int width=Mathf.CeilToInt((maxX-minX)*ppu),height=Mathf.CeilToInt((maxY-minY)*ppu);
                var pixels=new Color32[width*height];
                bool grain = !Root.name.Contains("Horizonte") && !Root.name.Contains("Chama")
                    && !Root.name.Contains("Mar") && !Root.name.Contains("Ilhas");
                for(int i=0;i<indices.Count;i+=3)
                {
                    Vector2 a=((Vector2)vertices[indices[i]]-new Vector2(minX,minY))*ppu;
                    Vector2 b=((Vector2)vertices[indices[i+1]]-new Vector2(minX,minY))*ppu;
                    Vector2 c=((Vector2)vertices[indices[i+2]]-new Vector2(minX,minY))*ppu;
                    int x0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))));
                    int x1=Mathf.Min(width-1,Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))));
                    int y0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))));
                    int y1=Mathf.Min(height-1,Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))));
                    Color32 color=colors[indices[i]];
                    float orientation=Edge(a,b,c);
                    for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
                    {
                        Vector2 point=new Vector2(x+.5f,y+.5f);
                        if(Edge(a,b,point)*orientation < 0 || Edge(b,c,point)*orientation < 0 || Edge(c,a,point)*orientation < 0) continue;
                        // Sparse one-pixel chips and short, staggered stone grain, using a restrained palette.
                        int hash=unchecked(x*73856093 ^ y*19349663) & 63;
                        int tone=grain ? hash==0 ? 10 : hash<4 ? -9 : (y%17==0 && (x+y/17)%11<3) ? -6 : 0 : 0;
                        pixels[y*width+x]=new Color32((byte)Mathf.Clamp(color.r+tone,0,255),(byte)Mathf.Clamp(color.g+tone,0,255),(byte)Mathf.Clamp(color.b+tone,0,255),255);
                    }
                }
                var texture=new Texture2D(width,height,TextureFormat.RGBA32,false) { name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
                texture.SetPixels32(pixels);texture.Apply(false,true);owned.Add(texture);
                var sprite=Sprite.Create(texture,new Rect(0,0,width,height),Vector2.zero,ppu,0,SpriteMeshType.FullRect);owned.Add(sprite);
                var go=new GameObject(name);go.transform.SetParent(Root,false);go.transform.localPosition=new Vector3(minX,minY,z);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
            }
            private static float Edge(Vector2 a,Vector2 b,Vector2 p) => (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
        }
    }
}
