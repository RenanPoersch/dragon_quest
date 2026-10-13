using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Exploration;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    // Modelos originais de poucos poligonos. Nao adiciona colliders: a planta continua no layout.
    public sealed class ObliqueScenery : IDisposable
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Material material, materiaShopMaterial;
        private readonly Material shadowMaterial;
        private readonly Material cityMaterial, cityDetailsMaterial, castleMaterial, glowMaterial, cityGroundMaterial, worldGroundMaterial;
        public PixelSpriteLibrary PixelSprites { get; }
        private readonly ShopPixelArt shopPixels;
        public ObliqueScenery()
        {
            Shader shader = Resources.Load<Shader>("ObliqueWorld");
            if (shader == null) throw new InvalidOperationException("Shader Resources/ObliqueWorld ausente.");
            material = new Material(shader) { name = "Cenario 2.5D - cores por face" };
            var atlas = Resources.Load<Texture2D>("PixelArt/TerrainAtlas");
            if (atlas == null) throw new InvalidOperationException("Atlas de materiais ausente.");
            material.SetTexture("_PixelAtlas", atlas);
            cityMaterial = AtlasMaterial(shader, "CityMaterialsAtlas", "Periferia / materiais gastos");
            cityMaterial.SetFloat("_Brightness", 0.8f);
            cityDetailsMaterial = AtlasMaterial(shader, "CityDetailsAtlas", "Periferia / fachadas e letreiros");
            castleMaterial = AtlasMaterial(shader, "CastleMaterialsAtlas", "Castelo / alvenaria de pedra");
            castleMaterial.SetFloat("_Brightness", 0.95f);
            var groundShader = Resources.Load<Shader>("CityGround");
            var groundPainting = Resources.Load<Texture2D>("PixelArt/CityGroundMap");
            if (groundShader == null || groundPainting == null) throw new InvalidOperationException("Pintura ou shader do chao da periferia ausente.");
            cityGroundMaterial = new Material(groundShader) {name = "Periferia / chao pintado uma vez"};
            cityGroundMaterial.SetTexture("_MainTex", groundPainting);
            var worldPainting = Resources.Load<Texture2D>("PixelArt/WorldGroundMap");
            if (worldPainting == null) throw new InvalidOperationException("Pintura do chao da ilha ausente.");
            worldGroundMaterial = new Material(groundShader) {name = "Ilha / chao continuo em pixel art"};
            worldGroundMaterial.SetTexture("_MainTex", worldPainting);
            var glowShader = Resources.Load<Shader>("PixelGlow");
            if (glowShader == null) throw new InvalidOperationException("Shader PixelGlow ausente.");
            glowMaterial = new Material(glowShader) {name = "Periferia / vapor"};
            materiaShopMaterial = AtlasMaterial(Resources.Load<Shader>("MateriaShop"), "MateriaShopAtlas", "Lojas / madeira e tecido pixel art");
            PixelSprites = new PixelSpriteLibrary();
            shopPixels = new ShopPixelArt(PixelSprites.Material);
            var shadowShader = Resources.Load<Shader>("PixelShadow");
            if (shadowShader == null) throw new InvalidOperationException("Shader PixelShadow ausente.");
            shadowMaterial = new Material(shadowShader) {name = "Sombra transparente no chao"};
        }

        private static Material AtlasMaterial(Shader shader, string resource, string name)
        {
            var texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            if (texture == null) throw new InvalidOperationException("Atlas ausente: " + resource);
            var appearance = new Material(shader) {name = name}; appearance.SetTexture("_PixelAtlas", texture); return appearance;
        }

        public Transform Show(string name, ObliqueMeshData data, Transform parent)
            => ShowWithMaterial(name, data, parent, material);

        private Transform ShowWithMaterial(string name, ObliqueMeshData data, Transform parent, Material appearance)
        {
            GameObject model = new GameObject(name);
            model.transform.SetParent(parent, false);
            if (data.Vertices.Count == 0) return model.transform;
            var solid = new List<int>(); var shadows = new List<int>();
            for (int i = 0; i < data.Triangles.Count; i += 3)
            {
                var target = data.Colors[data.Triangles[i]].a < 1 ? shadows : solid;
                target.Add(data.Triangles[i]); target.Add(data.Triangles[i + 1]); target.Add(data.Triangles[i + 2]);
            }
            if (solid.Count > 0) RenderMesh(name, data, solid, appearance, model);
            if (shadows.Count > 0)
            {
                var shadow = new GameObject("Sombra transparente"); shadow.transform.SetParent(model.transform, false);
                RenderMesh(name + " / sombra", data, shadows, shadowMaterial, shadow);
            }
            return model.transform;
        }

        public void MateriaShop(Transform parent) { ShowWithMaterial("Loja de materias / estrutura pixel art", ShopPixelArt.Shell("materia-shop"), parent, materiaShopMaterial); shopPixels.Populate(parent,"materia-shop"); }

        public void WeaponShop(Transform parent) { ShowWithMaterial("Loja de armas / estrutura pixel art", ShopPixelArt.Shell("equipment-shop"), parent, materiaShopMaterial); shopPixels.Populate(parent,"equipment-shop"); }

        public void ItemShop(Transform parent) { ShowWithMaterial("Loja de itens / estrutura pixel art", ShopPixelArt.Shell("item-shop"), parent, materiaShopMaterial); shopPixels.Populate(parent,"item-shop"); }

        public void CityBlock(CityBlock block, Transform parent)
            => ShowWithMaterial("Volume da periferia", CityPeripheryArt.Block(block), parent,
                block.Name.StartsWith("Ponte de pedra") || block.Name == "Patio da entrada do castelo" ? castleMaterial : cityMaterial);

        public void CityGround(Transform parent)
        {
            ShowWithMaterial("Chao continuo da periferia", CityGroundArt.Build(), parent, cityGroundMaterial);
        }

        public void CityBuilding(string name, Vector2 size, string hex, Transform parent)
            => ShowCityArt(name, CityPeripheryArt.Building(name, size, hex), parent);

        public void CityStreets(Transform parent) => ShowCityArt("Tubulacoes sobre as vielas", CityPeripheryArt.Streets(), parent);

        private void ShowCityArt(string name, PeripheryBuildingArt art, Transform parent)
        {
            ShowWithMaterial(name + " / volumes", art.Body, parent, CityPeripheryArt.IsRoyalCastle(name) ? castleMaterial : cityMaterial);
            if (art.Details.Vertices.Count > 0) ShowWithMaterial(name + " / fachadas", art.Details, parent, cityDetailsMaterial);
            if (art.Glow.Vertices.Count > 0)
            {
                var vapor = new GameObject(name + " / vapor"); vapor.transform.SetParent(parent, false);
                RenderMesh(name + " / vapor", art.Glow, art.Glow.Triangles, glowMaterial, vapor);
            }
        }

        private void RenderMesh(string name, ObliqueMeshData data, List<int> triangles, Material appearance, GameObject model)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(data.Vertices); mesh.SetColors(data.Colors); mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, data.UVs); mesh.SetUVs(1, data.Surfaces);
            mesh.RecalculateBounds(); meshes.Add(mesh);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = appearance;
        }

        public void Block(string name, Vector2 size, string hex, bool solid, int order, Transform parent)
            => Show("Volume", BlockData(name, size, hex, solid, order), parent);

        public static ObliqueMeshData BlockData(string name, Vector2 size, string hex, bool solid, int order)
        {
            var mesh = new ObliqueMeshData { DefaultSurface = PixelSurfaces.ForBlock(name, solid, order) };
            Color color = ObliqueMeshData.Hex(hex);
            float w=size.x, d=size.y;
            if(name.StartsWith("Lava"))
            {
                mesh.Plane(0,0,w,d,0.035f,color);
                for(float y=-d/2+0.5f;y<d/2;y+=1.2f)
                    mesh.Plane(0,y,w*0.7f,0.1f,0.045f,ObliqueMeshData.Hex("E58E42"));
                return mesh;
            }
            if(name.StartsWith("Cama"))
            {
                mesh.Box(0,0,w,d,0,0.35f,ObliqueMeshData.Hex("5C493B"));
                mesh.Box(0,0,w*0.94f,d*0.95f,0.35f,0.52f,color);
                mesh.Box(0,d*0.3f,w*0.85f,d*0.2f,0.52f,0.62f,ObliqueMeshData.Hex("E5D9BD"));return mesh;
            }
            if(name.StartsWith("Monolito"))
            { mesh.Box(0,0,w,d,0,1.1f,color);mesh.Roof(0,0,w,d,1.1f,1.25f,color);return mesh; }
            if(name=="Altar antigo")
            {mesh.Box(0,0,w,d,0,0.6f,color);mesh.Box(0,0,w+0.08f,d+0.08f,0.6f,0.72f,color);return mesh;}
            if (name.Contains("Arvore - detalhe") || name.Contains("Arvore - folhagem") || name.Contains("Coluna - detalhe")
                || name == "Fonte - agua" || name == "Fonte - centro" || name == "Trono - ouro") return mesh;
            if (name.Contains("Arvore")) { Tree(mesh,0,0,w/2, d > 1 ? 2.8f : 1.1f,color); return mesh; }
            if(name.StartsWith("Coluna quebrada"))
            {
                float top=name.EndsWith("baixa")?0.85f:1.65f;
                mesh.Box(0,0,w,d,0,0.15f,color);
                mesh.Cone(0,0,w*0.3f,0.15f,top,color,7,w*0.25f);
                mesh.Roof(0,0,w*0.5f,d*0.5f,top,top+0.2f,color);return mesh;
            }
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
                if(name.StartsWith("Trilha")) height=0.02f;
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
            var mesh=new ObliqueMeshData {DefaultSurface=PixelSurface.Wall}; float w=size.x,d=size.y;
            Color wall=ObliqueMeshData.Hex(fortified?"9BA1A0":"B8AF98"), roof=ObliqueMeshData.Hex(hex);
            float height=fortified?3.1f:2.2f;
            mesh.Shadow(0.35f,-0.2f,w*0.65f,d*0.6f);
            mesh.Box(0,0,w,d,0,height,wall);
            mesh.DefaultSurface = fortified ? PixelSurface.Basalt : PixelSurface.Roof;
            mesh.Roof(0,0,w+0.22f,d+0.2f,height,height+1.1f,roof);
            mesh.DefaultSurface = PixelSurface.Wall;
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
                ObliqueMeshData.V(x+width/2,y,top),ObliqueMeshData.V(x+width/2,y,bottom),color,false,PixelSurface.None);

        public static void Tree(ObliqueMeshData mesh,float x,float y,float radius,float height,Color color)
        {
            mesh.Shadow(x+0.2f,y-0.16f,radius*1.15f,radius*0.8f);
            PixelSurface previous = mesh.DefaultSurface; mesh.DefaultSurface = PixelSurface.Wood;
            mesh.Cone(x,y,radius*0.16f,0,height*0.55f,ObliqueMeshData.Hex("7C6650"),6,radius*0.12f);
            mesh.DefaultSurface = PixelSurface.Leaves;
            mesh.Cone(x,y,radius,height*0.25f,height*0.88f,color);
            mesh.Cone(x,y,radius*0.7f,height*0.53f,height,color);
            mesh.DefaultSurface = previous;
        }

        private static void Mountain(ObliqueMeshData mesh,float x,float y,float width,float depth,float height,Color color)
        {
            Vector3 a=ObliqueMeshData.V(x-width/2,y-depth/2,0), b=ObliqueMeshData.V(x-width/2,y+depth/2,0);
            Vector3 c=ObliqueMeshData.V(x+width/2,y+depth/2,0), d=ObliqueMeshData.V(x+width/2,y-depth/2,0);
            Vector3 peak=ObliqueMeshData.V(x-width*0.1f,y+depth*0.1f,height);
            mesh.Triangle(a,b,peak,color); mesh.Triangle(b,c,peak,color);
            mesh.Triangle(c,d,peak,color); mesh.Triangle(d,a,peak,color);
        }

        public static ObliqueMeshData IslandData(bool includeGround = true)
        {
            var mesh=new ObliqueMeshData {DefaultSurface=PixelSurface.Water};
            mesh.Plane(0,0,80,65,-0.95f,ObliqueMeshData.Hex("365E70"));
            var coast=PrototypeWorldLayout.Coast;
            var inner=new List<Vector2>();
            foreach(Vector2 p in coast) inner.Add(new Vector2(p.x*0.955f,p.y*0.955f));
            if (includeGround) mesh.Append(WorldIslandArt.Ground(), Vector3.zero);
            for(int i=0;i<coast.Count;i++)
            {
                Vector2 a=coast[i], b=coast[(i+1)%coast.Count], c=inner[(i+1)%coast.Count], d=inner[i];
                mesh.Quad(ObliqueMeshData.V(a.x,a.y,-0.08f),ObliqueMeshData.V(d.x,d.y,0),ObliqueMeshData.V(c.x,c.y,0),ObliqueMeshData.V(b.x,b.y,-0.08f),ObliqueMeshData.Hex("C2B494"), true, PixelSurface.Sand);
                mesh.Quad(ObliqueMeshData.V(a.x,a.y,-0.8f),ObliqueMeshData.V(a.x,a.y,-0.08f),ObliqueMeshData.V(b.x,b.y,-0.08f),ObliqueMeshData.V(b.x,b.y,-0.8f),ObliqueMeshData.Hex("7D8073"), true, PixelSurface.Basalt);
                // Faixa de espuma junto a costa, sem textura quadriculada.
                mesh.Quad(ObliqueMeshData.V(a.x*1.01f,a.y*1.01f,-0.94f),ObliqueMeshData.V(a.x*1.026f,a.y*1.026f,-0.94f),
                    ObliqueMeshData.V(b.x*1.026f,b.y*1.026f,-0.94f),ObliqueMeshData.V(b.x*1.01f,b.y*1.01f,-0.94f),ObliqueMeshData.Hex("789997"),false,PixelSurface.None);
            }
            for(int i=0;i<35;i++)
            {
                float x=-25+(i*17%51),y=-20+(i*7%40);
                if(PrototypeWorldLayout.IsLand(new Vector2(x,y))) continue;
                mesh.DefaultSurface = PixelSurface.None;
                mesh.Plane(x,y,0.5f+(i%3)*0.25f,0.035f,-0.93f,ObliqueMeshData.Hex("537C89"));
            }
            return mesh;
        }

        public GameObject World(Transform parent)
        {
            Show("Costa arredondada e mar",IslandData(false),parent);
            ShowWithMaterial("Chao continuo da ilha",WorldIslandArt.Ground(),parent,worldGroundMaterial);
            var seal = new GameObject(PrototypeSecretLayout.SealedRocks);
            seal.transform.SetParent(parent, false); seal.transform.localPosition = PrototypeWorldLayout.Volcano;
            var data = WorldIslandArt.Ward();
            RenderMesh("Tres selos ao redor do vulcao",data,data.Triangles,glowMaterial,seal);
            seal.AddComponent<CircleCollider2D>().radius = PrototypeWorldLayout.WardRadius;
            return seal;
        }

        public GameObject AltarFlame(Transform parent)
        {
            var mesh=new ObliqueMeshData();mesh.Cone(0,0,0.2f,0.72f,1.32f,ObliqueMeshData.Hex("E1AE61"),5);
            GameObject flame=Show("Chama antiga",mesh,parent).gameObject;flame.SetActive(false);return flame;
        }

        public GameObject MonolithRune(MonolithSymbol symbol,Transform parent)
        {
            var mesh=new ObliqueMeshData();float y=-0.41f, h=0.75f;
            Color rune=ObliqueMeshData.Hex("C4C0A5");
            int points=symbol==MonolithSymbol.Star?10:12;
            for(int i=0;i<points;i++)
            {
                double a=i*Math.PI*2/points,b=(i+1)*Math.PI*2/points;
                float radiusA=symbol==MonolithSymbol.Star&&i%2==1?0.1f:0.22f;
                float radiusB=symbol==MonolithSymbol.Star&&(i+1)%2==1?0.1f:0.22f;
                mesh.Triangle(ObliqueMeshData.V(0,y,h),ObliqueMeshData.V((float)Math.Sin(a)*radiusA,y,h+(float)Math.Cos(a)*radiusA),
                    ObliqueMeshData.V((float)Math.Sin(b)*radiusB,y,h+(float)Math.Cos(b)*radiusB),rune,false);
            }
            Show("Simbolo gravado / "+symbol,mesh,parent);
            if(symbol==MonolithSymbol.Moon)
            {
                var cut=new ObliqueMeshData();
                cut.Quad(ObliqueMeshData.V(0.05f,y-0.005f,h-0.18f),ObliqueMeshData.V(-0.08f,y-0.005f,h),
                    ObliqueMeshData.V(0.05f,y-0.005f,h+0.18f),ObliqueMeshData.V(0.22f,y-0.005f,h),ObliqueMeshData.Hex("727264"),false);
                Show("Lua - recorte",cut,parent);
            }
            var glow=new ObliqueMeshData();glow.Cone(0,0,0.18f,1.28f,1.65f,ObliqueMeshData.Hex("DDD098"),6,0.09f);
            GameObject light=Show("Luz do monolito",glow,parent).gameObject;light.SetActive(false);return light;
        }

        public void ClawMarks(Transform parent)
        {
            var mesh=new ObliqueMeshData();
            foreach(float x in new[]{-0.2f,-0.07f,0.06f})
                mesh.Quad(ObliqueMeshData.V(x-0.015f,-0.5f+0.18f/0.85f*0.6f-0.01f,0.18f),
                    ObliqueMeshData.V(x+0.005f,-0.5f+0.5f/0.85f*0.6f-0.01f,0.5f),
                    ObliqueMeshData.V(x+0.035f,-0.5f+0.5f/0.85f*0.6f-0.01f,0.5f),
                    ObliqueMeshData.V(x+0.015f,-0.5f+0.18f/0.85f*0.6f-0.01f,0.18f),ObliqueMeshData.Hex("3F4B45"),false);
            Show("Sulcos de garras",mesh,parent);
        }

        public void Hollow(Transform parent)
        {
            var mesh=new ObliqueMeshData();
            mesh.Quad(ObliqueMeshData.V(-0.1f,-0.15f,0.1f),ObliqueMeshData.V(-0.09f,-0.15f,0.65f),
                ObliqueMeshData.V(0.09f,-0.15f,0.65f),ObliqueMeshData.V(0.1f,-0.15f,0.1f),ObliqueMeshData.Hex("303F36"),false);
            Show("Abertura entre raizes",mesh,parent);
        }

        public void CastleIcon(Transform parent)
        {
            WorldPoi(parent, 0);
            // Mantem exatamente a area de colisao do icone declarada no layout.
            CityBlock footprint=PrototypeWorldLayout.CastleIcon[0];
            BoxCollider2D collider=parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size=footprint.Size; collider.offset=footprint.Position;
        }

        public static ObliqueMeshData CastleIconData() => CastleFortressArt.Icon();

        public void ForestIcon(Transform parent)
            => WorldPoi(parent, 2);

        public void QuarryIcon(Transform parent)
        {
            WorldPoi(parent, 1);
            CityBlock footprint = PrototypeWorldLayout.QuarryIcon[0];
            var collider = parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size = footprint.Size; collider.offset = footprint.Position;
        }

        private void WorldPoi(Transform parent, int frame)
        {
            Transform visual = PixelSprites.Create(parent, "WorldPoiSprites", frame);
            visual.localPosition = new Vector3(ObliqueProjection.YawSin * 0.3f, -ObliqueProjection.YawCos * 0.3f, -0.015f);
        }

        public static ObliqueMeshData ForestIconData()
        {
            var mesh=new ObliqueMeshData();
            Tree(mesh,-0.45f,0,0.48f,1.5f,ObliqueMeshData.Hex("405B4D"));
            Tree(mesh,0.45f,0,0.48f,1.4f,ObliqueMeshData.Hex("4A6451"));
            Tree(mesh,0,0.45f,0.46f,1.8f,ObliqueMeshData.Hex("526D57"));
            return mesh;
        }

        public void RuinsIcon(Transform parent)
        {
            WorldPoi(parent, 3);
            CityBlock footprint=PrototypeRuinsLayout.Icon[0];
            BoxCollider2D collider=parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size=footprint.Size;collider.offset=footprint.Position;
        }

        public static ObliqueMeshData RuinsIconData()
        {
            var mesh=new ObliqueMeshData {DefaultSurface=PixelSurface.Wall};Color stone=ObliqueMeshData.Hex("A4A491");
            mesh.Shadow(0.1f,-0.1f,1.1f,0.6f);
            mesh.Box(0,0.1f,1.8f,0.8f,0,0.12f,ObliqueMeshData.Hex("818576"));
            mesh.Cone(-0.55f,0.2f,0.2f,0.12f,1.5f,stone,7,0.17f);
            mesh.Roof(-0.55f,0.2f,0.34f,0.34f,1.5f,1.7f,stone);
            mesh.Cone(0.55f,0.2f,0.2f,0.12f,0.95f,stone,7,0.17f);
            mesh.Box(-0.25f,0.2f,0.6f,0.26f,1.3f,1.43f,stone);
            mesh.Roof(0.4f,-0.15f,0.45f,0.3f,0.12f,0.36f,stone);
            return mesh;
        }

        public void VolcanoIcon(Transform parent)
            => WorldPoi(parent, 4);

        public Transform Dragon(Transform parent) => PixelSprites.Create(parent, "Dragon");
        public Transform DragonEssence(Transform parent)
        {
            var mesh=new ObliqueMeshData();
            mesh.Cone(0,0,0.3f,0.8f,1.4f,ObliqueMeshData.Hex("F2C879"),8,0);
            mesh.Cone(0,0,0.16f,0.85f,1.25f,ObliqueMeshData.Hex("FFF0BE"),8,0);
            return Show("Essencia do dragao",mesh,parent);
        }

        public static ObliqueMeshData DragonData()
        {
            var mesh=new ObliqueMeshData();
            Color scales=ObliqueMeshData.Hex("863E3D"),bone=ObliqueMeshData.Hex("B99A78"),wing=ObliqueMeshData.Hex("543454");
            mesh.Shadow(0,0,1.5f,1.4f);
            mesh.Box(0,0.1f,1.3f,1.8f,0.6f,1.7f,scales);
            mesh.Roof(0,0.1f,1.3f,1.8f,1.7f,2,scales);
            mesh.Box(0,-0.7f,0.75f,0.15f,0.7f,1.5f,bone);
            foreach(float side in new[]{-1f,1f})
            {
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.65f),ObliqueMeshData.V(side*2.8f,1,2.4f),ObliqueMeshData.V(side*1.6f,-0.5f,1.35f),wing);
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.65f),ObliqueMeshData.V(side*1.6f,-0.5f,1.35f),ObliqueMeshData.V(side*0.7f,-0.9f,1.1f),wing);
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.68f),ObliqueMeshData.V(side*2.8f,1,2.42f),ObliqueMeshData.V(side*1.6f,0.85f,2.25f),bone);
                foreach(float y in new[]{-0.55f,0.7f})
                {
                    mesh.Box(side*0.65f,y,0.35f,0.42f,0.12f,0.85f,scales);
                    mesh.Box(side*0.65f,y-0.15f,0.42f,0.65f,0.03f,0.25f,bone);
                }
                mesh.Cone(side*0.3f,-1.08f,0.12f,1.92f,2.42f,bone,6,0);
                mesh.Box(side*0.43f,-1.38f,0.08f,0.18f,1.77f,1.88f,ObliqueMeshData.Hex("EBC05E"));
            }
            mesh.Box(0,-1.25f,0.85f,0.75f,1.4f,1.98f,scales);
            mesh.Box(0,-1.7f,0.72f,0.45f,1.4f,1.65f,scales);
            mesh.Box(0,-1.94f,0.62f,0.04f,1.45f,1.52f,ObliqueMeshData.Hex("30282E"));
            mesh.Triangle(ObliqueMeshData.V(-0.36f,1,0.8f),ObliqueMeshData.V(0.36f,1,0.8f),ObliqueMeshData.V(0,3.1f,0.18f),scales);
            for(int i=0;i<4;i++) mesh.Cone(0,0.3f+i*0.32f,0.12f,1.8f,2.25f,bone,5,0);
            return mesh;
        }

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

        public static ObliqueMeshData PersonBodyData(string hex, bool armed, bool spikyBlond = false)
        {
            var body=new ObliqueMeshData(); Color cloth=ObliqueMeshData.Hex(hex), skin=ObliqueMeshData.Hex("D4B294");
            body.Box(0,0,0.38f,0.26f,0.5f,1.02f,cloth);
            body.Box(0,0,0.2f,0.2f,1.02f,1.12f,skin);
            body.Box(0,-0.015f,0.29f,0.27f,1.1f,1.4f,skin);
            Color hair = ObliqueMeshData.Hex(spikyBlond ? "D9B35D" : "4C4544");
            body.Box(0,0.015f,0.32f,0.29f,1.34f,1.45f,hair);
            if (spikyBlond)
                foreach (float x in new[] {-0.12f, -0.04f, 0.04f, 0.12f})
                    body.Cone(x, 0.015f, 0.075f, 1.41f, 1.58f + (x < 0 ? 0.04f : 0), hair, 4);
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

        public Action<bool> Chest(Transform parent)
        {
            var shadow = new ObliqueMeshData(); shadow.Shadow(0, 0, 0.4f, 0.27f);
            Show("Sombra do bau", shadow, parent).localPosition = Vector3.back * 0.035f;
            var visual = PixelSprites.Create(parent, "ChestStates");
            // Base apoiada na frente do collider, inteiramente acima do piso.
            visual.localPosition = new Vector3(ObliqueProjection.YawSin * 0.35f, -ObliqueProjection.YawCos * 0.35f, -0.075f);
            var renderer = visual.GetComponent<SpriteRenderer>();
            var states = PixelSprites.Atlas("ChestStates").Sprites;
            return opened => { var next = states[opened ? 1 : 0]; if (renderer.sprite != next) renderer.sprite = next; };
        }

        public Transform King(Transform parent) => Show("Rei e coroa",KingData(),parent);
        public Transform PixelPerson(Transform parent, string atlas, int frame = 0, bool animated = false)
        {
            float height = atlas == "NpcRoster" && frame == 11 ? 0.15f : 0.075f;
            var shadow = new ObliqueMeshData(); shadow.Shadow(0, 0, 0.34f, 0.22f);
            var shade = Show("Sombra no chao", shadow, parent); shade.localPosition = Vector3.back * (height - 0.035f);
            var visual = PixelSprites.Create(parent, atlas, frame, animated); visual.localPosition = Vector3.back * height;
            return visual;
        }
        public Transform Spirit(Transform parent) => Show("Espirito maligno",SpiritData(),parent);
        private static ObliqueMeshData StandingPerson(string hex, bool spikyBlond = false)
        {
            var mesh=PersonBodyData(hex,true,spikyBlond);Color cloth=ObliqueMeshData.Hex(hex);
            mesh.Append(LegData(-0.12f,cloth),new Vector3(0,0,-0.5f));mesh.Append(LegData(0.12f,cloth),new Vector3(0,0,-0.5f));
            return mesh;
        }
        public static ObliqueMeshData KingData(bool possessed=true)
        {
            var mesh=StandingPerson("9A5979");Color gold=ObliqueMeshData.Hex("D8B968");
            mesh.Box(0,0,0.34f,0.3f,1.4f,1.49f,gold);
            foreach(float x in new[]{-0.12f,0.12f}) foreach(float y in new[]{-0.09f,0.09f}) mesh.Cone(x,y,0.06f,1.49f,1.72f,gold,5,0);
            if(possessed) mesh.Box(0,-0.158f,0.2f,0.025f,1.24f,1.29f,ObliqueMeshData.Hex("BBA0ED"));
            return mesh;
        }
        public static ObliqueMeshData SpiritData()
        {
            var mesh=new ObliqueMeshData();Color shadow=ObliqueMeshData.Hex("483858"),bone=ObliqueMeshData.Hex("ADA6BC");
            mesh.Cone(0,0,0.48f,0.25f,1.35f,shadow,7,0.22f);
            mesh.Cone(0,0.12f,0.34f,1.35f,2.1f,shadow,7,0);
            mesh.Box(0,-0.14f,0.39f,0.29f,1.32f,1.65f,bone);
            foreach(float side in new[]{-1f,1f})
            {
                mesh.Triangle(ObliqueMeshData.V(side*0.18f,0,1.25f),ObliqueMeshData.V(side*0.8f,-0.12f,1.1f),ObliqueMeshData.V(side*0.45f,-0.22f,0.72f),shadow);
                mesh.Box(side*0.09f,-0.292f,0.08f,0.025f,1.47f,1.53f,ObliqueMeshData.Hex("D39DFF"));
            }
            mesh.Box(0,-0.292f,0.12f,0.025f,1.37f,1.4f,ObliqueMeshData.Hex("382B44"));
            return mesh;
        }
        public static ObliqueMeshData RuinedCastleData()
        {
            var mesh=new ObliqueMeshData();Color stone=ObliqueMeshData.Hex("666169"),charcoal=ObliqueMeshData.Hex("45404B");
            mesh.Box(0,0,8,4.5f,0,0.3f,stone);
            mesh.Box(-3,1,1.6f,1.6f,0.3f,1.65f,charcoal);mesh.Box(3,1,1.6f,1.6f,0.3f,0.95f,charcoal);
            mesh.Box(0,1.8f,4.3f,0.4f,0.3f,1.1f,stone);
            mesh.Roof(-1,-0.4f,2.8f,1.5f,0.3f,0.75f,charcoal);
            foreach(float x in new[]{-2.5f,0,2.3f})
            {
                mesh.Box(x,-0.8f,0.9f,0.7f,0.3f,0.58f,stone);
                mesh.Cone(x,-0.6f,0.35f,0.58f,1.3f,ObliqueMeshData.Hex("C87740"),6,0);
                mesh.Cone(x,-0.6f,0.16f,0.6f,1,ObliqueMeshData.Hex("EBC16A"),6,0);
            }
            return mesh;
        }
        public static ObliqueMeshData EndingSceneData(GameEnding ending)
        {
            var mesh=new ObliqueMeshData();
            mesh.Plane(0,0,14,10,0,ObliqueMeshData.Hex(ending==GameEnding.CastleDestroyed?"433E47":"89818A"));
            if(ending==GameEnding.CastleDestroyed) {mesh.Append(RuinedCastleData(),Vector3.zero);return mesh;}
            var throne=BlockData("Trono - encosto",new Vector2(1.8f,1.4f),"77576B",true,3);
            mesh.Append(throne,new Vector3(0,3.4f,0));
            if(ending==GameEnding.KingFreed) mesh.Append(KingData(false),new Vector3(0,2.3f,0));
            else
            {
                mesh.Box(0,2.3f,0.5f,1.2f,0,0.24f,ObliqueMeshData.Hex("9A5979"));
                mesh.Cone(0.5f,2.2f,0.2f,0,0.12f,ObliqueMeshData.Hex("D8B968"),7,0.2f);
            }
            foreach(var pair in new[]{("B4A078",0f),("DADECB",-2.2f),("608DA1",2.2f)})
                mesh.Append(StandingPerson(pair.Item1, pair.Item2 == 0),new Vector3(pair.Item2,-1.5f,0));
            if(ending==GameEnding.HeroPossessed)
            {
                mesh.Append(SpiritData(),new Vector3(0,-1.1f,-0.65f));
                mesh.Box(0,-1.658f,0.2f,0.025f,1.24f,1.3f,ObliqueMeshData.Hex("CB9BEF"));
            }
            return mesh;
        }

        public void Dispose()
        {
            foreach(Mesh mesh in meshes) if(mesh!=null) UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(material); UnityEngine.Object.Destroy(shadowMaterial);
            UnityEngine.Object.Destroy(cityMaterial); UnityEngine.Object.Destroy(cityDetailsMaterial); UnityEngine.Object.Destroy(glowMaterial);
            UnityEngine.Object.Destroy(castleMaterial);
            UnityEngine.Object.Destroy(cityGroundMaterial);
            UnityEngine.Object.Destroy(worldGroundMaterial);
            shopPixels.Dispose();
            UnityEngine.Object.Destroy(materiaShopMaterial);
            PixelSprites.Dispose();
        }
    }
}
