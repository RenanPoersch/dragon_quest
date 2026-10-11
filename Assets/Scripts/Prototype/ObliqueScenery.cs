using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Modelos originais de poucos poligonos. Nao adiciona colliders: a planta continua no layout.
    public sealed class ObliqueScenery : IDisposable
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Material material;
        public ObliqueScenery()
        {
            Shader shader = Resources.Load<Shader>("ObliqueWorld");
            if (shader == null) throw new InvalidOperationException("Shader Resources/ObliqueWorld ausente.");
            material = new Material(shader) { name = "Cenario 2.5D - cores por face" };
        }

        public Transform Show(string name, ObliqueMeshData data, Transform parent)
        {
            GameObject model = new GameObject(name);
            model.transform.SetParent(parent, false);
            if (data.Vertices.Count == 0) return model.transform;
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(data.Vertices); mesh.SetColors(data.Colors); mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds(); meshes.Add(mesh);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = material;
            return model.transform;
        }

        public void Block(string name, Vector2 size, string hex, bool solid, int order, Transform parent)
            => Show("Volume", BlockData(name, size, hex, solid, order), parent);

        public static ObliqueMeshData BlockData(string name, Vector2 size, string hex, bool solid, int order)
        {
            var mesh = new ObliqueMeshData();
            Color color = ObliqueMeshData.Hex(hex);
            float w=size.x, d=size.y;
            if (name.Contains("Arvore - detalhe") || name.Contains("Arvore - folhagem") || name.Contains("Coluna - detalhe")
                || name == "Fonte - agua" || name == "Fonte - centro" || name == "Trono - ouro") return mesh;
            if (name.Contains("Arvore")) { Tree(mesh,0,0,w/2, d > 1 ? 2.8f : 1.1f,color); return mesh; }
            if (name.Contains("Coluna"))
            {
                mesh.Box(0,0,w,d,0,0.2f,color);
                mesh.Cone(0,0,w*0.32f,0.2f,2.8f,color,8,w*0.32f);
                mesh.Box(0,0,w*1.15f,d*0.85f,2.8f,3.05f,color); return mesh;
            }
            if (name.StartsWith("Fonte"))
            {
                mesh.Cone(0,0,w/2,0,0.4f,color,12,w/2);
                mesh.Cone(0,0,w*0.4f,0.4f,0.41f,ObliqueMeshData.Hex("5F949E"),12,w*0.4f);
                mesh.Cone(0,0,0.2f,0.41f,1.25f,ObliqueMeshData.Hex("BDB9A5"),8,0.16f);
                mesh.Cone(0,0,0.48f,1.2f,1.35f,color,12,0.5f); return mesh;
            }
            if (name.Contains("Trono - encosto"))
            {
                mesh.Box(0,0,w,d,0,0.5f,color);
                mesh.Box(0,d*0.36f,w,0.2f,0.5f,2.2f,color);
                foreach(float x in new[]{-w*0.4f,w*0.4f}) mesh.Box(x,0,0.17f,d,0.5f,1.0f,ObliqueMeshData.Hex("C6A764"));
                mesh.Box(0,d*0.36f,w*1.1f,0.24f,2.05f,2.2f,ObliqueMeshData.Hex("C6A764")); return mesh;
            }
            if (name.Contains("Cordilheira") || name.Contains("Rochas"))
            {
                int peaks = Math.Max(2,(int)Math.Ceiling(w/1.5f));
                for(int i=0;i<peaks;i++)
                {
                    float x=-w/2+w*(i+0.5f)/peaks;
                    Mountain(mesh,x,0,w/peaks,d,1.2f+(i%3)*0.4f,color);
                }
                return mesh;
            }
            if (name.StartsWith("Pedra")) { Mountain(mesh,0,0,w,d,0.85f,color); return mesh; }
            if (name.Contains("Balcao") || name == "Estante")
            {
                float height=name=="Estante"?1.5f:0.85f;
                mesh.Box(0,0,w,d,0,height,color);
                mesh.Box(0,0,w+0.1f,d+0.12f,height,height+0.08f,ObliqueMeshData.Hex("AD875D"));
                if(name=="Estante")
                    for(int i=0;i<5;i++) mesh.Box(0,-d*0.35f+i*d*0.17f,w*0.4f,0.12f,height+0.08f,height+0.45f,ObliqueMeshData.Hex(i%2==0?"7F9690":"A67866"));
                return mesh;
            }
            if(name=="Mercadoria provisoria") { mesh.Cone(0,0,0.16f,1.58f,1.82f,color,6,0.09f); return mesh; }
            if (name.StartsWith("Banco"))
            {
                mesh.Box(0,0,w,d,0.4f,0.52f,color);
                mesh.Box(0,d*0.35f,w,0.12f,0.5f,0.95f,color); return mesh;
            }
            if (name.StartsWith("Flores")) { mesh.Cone(0,0,w*0.7f,0.03f,0.25f,color,5,0.08f); return mesh; }
            if (name.Contains("corrimao")) { mesh.Box(0,0,w,d,0,0.7f,color); return mesh; }
            if (name.StartsWith("Limite") && hex == "2A4533")
            {
                mesh.Box(0,0,w,d,0,0.15f,color);
                if(name.EndsWith("sul")) return mesh;
                float length=Math.Max(w,d);
                for(float step=-length/2+0.8f;step<length/2;step+=1.7f)
                    Tree(mesh,w>d?step:0,w>d?0:step,0.8f,2.8f,color);
                return mesh;
            }
            if (name.Contains("Limite") || name.Contains("Parede") || name.Contains("Muralha"))
            {
                bool indoors=name.StartsWith("Limite")||name.StartsWith("Parede");
                float height = name.EndsWith("sul") ? 0.18f : indoors ? 2.4f : 1.2f;
                mesh.Box(0,0,w,d,0,height,color);
                mesh.Box(0,0,w,d,height,height+0.08f,ObliqueMeshData.Hex("A49B8C")); return mesh;
            }
            if (order < 0 || name=="Rio" || name.StartsWith("Lago"))
            {
                float height=Math.Max(0,(order+10)*0.012f);
                if(name.StartsWith("Estrado")) height=0.12f;
                mesh.Plane(0,0,w,d,height,color);
                if(name.StartsWith("Piso") || name=="Praca" || name.StartsWith("Patio"))
                {
                    Color joint=new Color(color.r*0.92f,color.g*0.92f,color.b*0.92f,1);
                    for(float x=-w/2+1;x<w/2;x+=1) mesh.Plane(x,0,0.018f,d,height+0.002f,joint);
                    for(float y=-d/2+1;y<d/2;y+=1) mesh.Plane(0,y,w,0.018f,height+0.002f,joint);
                }
                return mesh;
            }
            mesh.Box(0,0,w,d,0,solid?0.7f:0.08f,color);
            return mesh;
        }

        public void Building(string name, Vector2 size, string hex, Transform parent)
            => Show(name+" - modelo", BuildingData(size,hex,name.Contains("Castelo")||name.Contains("Torre")),parent);

        public static ObliqueMeshData BuildingData(Vector2 size,string hex,bool fortified=false)
        {
            var mesh=new ObliqueMeshData(); float w=size.x,d=size.y;
            Color wall=ObliqueMeshData.Hex(fortified?"9BA1A0":"B8AF98"), roof=ObliqueMeshData.Hex(hex);
            float height=fortified?3.1f:2.2f;
            mesh.Shadow(0.35f,-0.2f,w*0.65f,d*0.6f);
            mesh.Box(0,0,w,d,0,height,wall);
            mesh.Roof(0,0,w+0.22f,d+0.2f,height,height+1.1f,roof);
            float front=-d/2-0.008f;
            Window(mesh,0,front,0.85f,0.0f,1.5f,ObliqueMeshData.Hex("3B3D40"));
            foreach(float x in new[]{-w*0.3f,w*0.3f})
            {
                Window(mesh,x,front,0.5f,0.95f,1.65f,ObliqueMeshData.Hex("DFC187"));
                mesh.Box(x,front-0.015f,0.64f,0.12f,0.85f,0.95f,roof);
            }
            if(fortified)
            {
                foreach(float x in new[]{-w*0.4f,w*0.4f})
                {
                    mesh.Box(x,0,w*0.18f,d*0.8f,0,height+0.6f,wall);
                    mesh.Cone(x,0,w*0.13f,height+0.6f,height+1.5f,roof,4);
                }
            }
            else mesh.Box(-w*0.25f,d*0.2f,0.35f,0.4f,height+0.1f,height+1.2f,ObliqueMeshData.Hex("8A8175"));
            return mesh;
        }

        private static void Window(ObliqueMeshData mesh,float x,float y,float width,float bottom,float top,Color color)
            => mesh.Quad(ObliqueMeshData.V(x-width/2,y,bottom),ObliqueMeshData.V(x-width/2,y,top),
                ObliqueMeshData.V(x+width/2,y,top),ObliqueMeshData.V(x+width/2,y,bottom),color,false);

        public static void Tree(ObliqueMeshData mesh,float x,float y,float radius,float height,Color color)
        {
            mesh.Shadow(x+0.2f,y-0.16f,radius*1.15f,radius*0.8f);
            mesh.Cone(x,y,radius*0.16f,0,height*0.55f,ObliqueMeshData.Hex("7C6650"),6,radius*0.12f);
            mesh.Cone(x,y,radius,height*0.25f,height*0.88f,color);
            mesh.Cone(x,y,radius*0.7f,height*0.53f,height,color);
        }

        private static void Mountain(ObliqueMeshData mesh,float x,float y,float width,float depth,float height,Color color)
        {
            Vector3 a=ObliqueMeshData.V(x-width/2,y-depth/2,0), b=ObliqueMeshData.V(x-width/2,y+depth/2,0);
            Vector3 c=ObliqueMeshData.V(x+width/2,y+depth/2,0), d=ObliqueMeshData.V(x+width/2,y-depth/2,0);
            Vector3 peak=ObliqueMeshData.V(x-width*0.1f,y+depth*0.1f,height);
            mesh.Triangle(a,b,peak,color); mesh.Triangle(b,c,peak,color);
            mesh.Triangle(c,d,peak,color); mesh.Triangle(d,a,peak,color);
        }

        public static ObliqueMeshData IslandData()
        {
            var mesh=new ObliqueMeshData();
            mesh.Plane(0,0,80,65,-0.95f,ObliqueMeshData.Hex("365E70"));
            var coast=PrototypeWorldLayout.Coast;
            var inner=new List<Vector2>();
            foreach(Vector2 p in coast) inner.Add(new Vector2(p.x*0.955f,p.y*0.955f));
            mesh.Polygon(inner,0,ObliqueMeshData.Hex("71806A"));
            for(int i=0;i<coast.Count;i++)
            {
                Vector2 a=coast[i], b=coast[(i+1)%coast.Count], c=inner[(i+1)%coast.Count], d=inner[i];
                mesh.Quad(ObliqueMeshData.V(a.x,a.y,-0.08f),ObliqueMeshData.V(d.x,d.y,0),ObliqueMeshData.V(c.x,c.y,0),ObliqueMeshData.V(b.x,b.y,-0.08f),ObliqueMeshData.Hex("C2B494"));
                mesh.Quad(ObliqueMeshData.V(a.x,a.y,-0.8f),ObliqueMeshData.V(a.x,a.y,-0.08f),ObliqueMeshData.V(b.x,b.y,-0.08f),ObliqueMeshData.V(b.x,b.y,-0.8f),ObliqueMeshData.Hex("7D8073"));
                // Faixa de espuma junto a costa, sem textura quadriculada.
                mesh.Quad(ObliqueMeshData.V(a.x*1.01f,a.y*1.01f,-0.94f),ObliqueMeshData.V(a.x*1.026f,a.y*1.026f,-0.94f),
                    ObliqueMeshData.V(b.x*1.026f,b.y*1.026f,-0.94f),ObliqueMeshData.V(b.x*1.01f,b.y*1.01f,-0.94f),ObliqueMeshData.Hex("789997"),false);
            }
            for(int i=0;i<35;i++)
            {
                float x=-25+(i*17%51),y=-20+(i*7%40);
                if(PrototypeWorldLayout.IsLand(new Vector2(x,y))) continue;
                mesh.Plane(x,y,0.5f+(i%3)*0.25f,0.035f,-0.93f,ObliqueMeshData.Hex("537C89"));
            }
            return mesh;
        }

        public void World(Transform parent)
        {
            Show("Ilha em relevo e mar",IslandData(),parent);
            foreach(CityBlock block in PrototypeWorldLayout.Blocks)
            {
                if(block.Name=="Mar"||block.Name.StartsWith("Costa")||block.Name.StartsWith("Ilha -")) continue;
                Transform model=Show(block.Name,BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),parent);
                model.localPosition=block.Position;
                if(block.Solid) model.gameObject.AddComponent<BoxCollider2D>().size=block.Size;
            }
        }

        public void CastleIcon(Transform parent)
        {
            Show("Castelo em miniatura",CastleIconData(),parent);
            // Mantem exatamente a area de colisao do icone declarada no layout.
            CityBlock footprint=PrototypeWorldLayout.CastleIcon[0];
            BoxCollider2D collider=parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size=footprint.Size; collider.offset=footprint.Position;
        }

        public static ObliqueMeshData CastleIconData()
        {
            var mesh=new ObliqueMeshData(); Color wall=ObliqueMeshData.Hex("B1AEA2"),roof=ObliqueMeshData.Hex("727681");
            mesh.Shadow(0.15f,-0.1f,1,0.6f);
            mesh.Box(0,0.1f,1.2f,0.8f,0,1.0f,wall);
            mesh.Roof(0,0.1f,1.3f,0.9f,1,1.35f,roof);
            foreach(float x in new[]{-0.65f,0.65f})
            { mesh.Box(x,0.4f,0.45f,0.5f,0,1.4f,wall); mesh.Cone(x,0.4f,0.32f,1.4f,1.95f,roof,4); }
            Window(mesh,0,-0.31f,0.28f,0,0.65f,ObliqueMeshData.Hex("3C3E43"));
            return mesh;
        }

        public void ForestIcon(Transform parent)
            => Show("Bosque em miniatura",ForestIconData(),parent);

        public static ObliqueMeshData ForestIconData()
        {
            var mesh=new ObliqueMeshData();
            Tree(mesh,-0.45f,0,0.48f,1.5f,ObliqueMeshData.Hex("405B4D"));
            Tree(mesh,0.45f,0,0.48f,1.4f,ObliqueMeshData.Hex("4A6451"));
            Tree(mesh,0,0.45f,0.46f,1.8f,ObliqueMeshData.Hex("526D57"));
            return mesh;
        }

        public void VolcanoIcon(Transform parent)
            => Show("Vulcao com cratera",VolcanoIconData(),parent);

        public static ObliqueMeshData VolcanoIconData()
        {
            var mesh=new ObliqueMeshData();
            mesh.Cone(0,0,0.9f,0,1.55f,ObliqueMeshData.Hex("777979"),9,0.35f);
            mesh.Cone(0,0,0.34f,1.55f,1.56f,ObliqueMeshData.Hex("BF805D"),9,0.34f);
            mesh.Cone(0,0,0.21f,1.56f,1.565f,ObliqueMeshData.Hex("553F3F"),9,0.21f);
            return mesh;
        }

        public Transform Person(string hex,Transform parent,bool armed=false,bool animated=false)
        {
            GameObject figure=new GameObject("Personagem - modelo 2.5D"); figure.transform.SetParent(parent,false);
            Show("Corpo e cabeca",PersonBodyData(hex,armed),figure.transform);
            Color cloth=ObliqueMeshData.Hex(hex);
            Transform left=Leg(-0.12f,cloth,figure.transform),right=Leg(0.12f,cloth,figure.transform);
            var shadow=new ObliqueMeshData(); shadow.Shadow(0.08f,-0.03f,0.36f,0.25f); Show("Sombra no chao",shadow,parent);
            if(animated) figure.AddComponent<ObliqueCharacter>().Initialize(parent,left,right);
            return figure.transform;
        }

        public static ObliqueMeshData PersonBodyData(string hex, bool armed)
        {
            var body=new ObliqueMeshData(); Color cloth=ObliqueMeshData.Hex(hex), skin=ObliqueMeshData.Hex("D4B294");
            body.Box(0,0,0.38f,0.26f,0.5f,1.02f,cloth);
            body.Box(0,0,0.2f,0.2f,1.02f,1.12f,skin);
            body.Box(0,-0.015f,0.29f,0.27f,1.1f,1.4f,skin);
            body.Box(0,0.015f,0.32f,0.29f,1.34f,1.45f,ObliqueMeshData.Hex("4C4544"));
            foreach(float x in new[]{-0.25f,0.25f}) body.Box(x,0,0.12f,0.19f,0.56f,0.98f,cloth);
            body.Box(0,0.16f,0.4f,0.05f,0.38f,1.02f,ObliqueMeshData.Hex(armed?"835A4A":"556577"));
            body.Box(0,-0.145f,0.39f,0.025f,0.54f,0.61f,ObliqueMeshData.Hex("B7A374"));
            if(armed)
            { body.Box(0.38f,0.1f,0.06f,0.08f,0.23f,1.02f,ObliqueMeshData.Hex("BCC6C7")); body.Box(0.38f,0.1f,0.23f,0.12f,0.45f,0.5f,ObliqueMeshData.Hex("87775D")); }
            return body;
        }

        private Transform Leg(float x,Color color,Transform parent)
        {
            Transform leg=Show("Perna",LegData(x,color),parent); leg.localPosition=new Vector3(0,0,-0.5f); return leg;
        }

        public static ObliqueMeshData LegData(float x,Color color)
        {
            var mesh=new ObliqueMeshData();
            mesh.Box(x,0,0.15f,0.17f,-0.35f,0,color);
            mesh.Box(x,-0.06f,0.18f,0.3f,-0.5f,-0.35f,ObliqueMeshData.Hex("49494A"));
            return mesh;
        }

        public Transform Chest(Transform parent)
        {
            var mesh=new ObliqueMeshData();
            mesh.Box(0,0,0.9f,0.7f,0,0.45f,ObliqueMeshData.Hex("8C6B4C"));
            mesh.Box(0,-0.36f,0.12f,0.03f,0.2f,0.55f,ObliqueMeshData.Hex("D6BB7D")); Show("Bau - caixa",mesh,parent);
            var lid=new ObliqueMeshData(); lid.Box(0,-0.35f,0.94f,0.74f,0,0.16f,ObliqueMeshData.Hex("B1956B"));
            Transform hinge=Show("Bau - tampa articulada",lid,parent); hinge.localPosition=new Vector3(0,0.35f,-0.45f); return hinge;
        }

        public void Dispose()
        { foreach(Mesh mesh in meshes) if(mesh!=null) UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(material); }
    }
}
