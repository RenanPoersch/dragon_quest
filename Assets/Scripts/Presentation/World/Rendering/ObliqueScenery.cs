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
        private readonly Material shadowMaterial;
        private readonly Material cityMaterial, cityDetailsMaterial, castleMaterial, glowMaterial, cityGroundMaterial, worldGroundMaterial;
        public PixelSpriteLibrary PixelSprites { get; }
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
            var worldShader = Resources.Load<Shader>("WorldGround");
            var worldPainting = Resources.Load<Texture2D>("PixelArt/WorldGroundMap");
            if (worldShader == null || worldPainting == null) throw new InvalidOperationException("Mapa continuo da ilha ausente.");
            worldGroundMaterial = new Material(worldShader) { name = "Ilha / uma pintura continua de terreno, costa e mar" };
            worldGroundMaterial.SetTexture("_MainTex", worldPainting);
            var glowShader = Resources.Load<Shader>("PixelGlow");
            if (glowShader == null) throw new InvalidOperationException("Shader PixelGlow ausente.");
            glowMaterial = new Material(glowShader) {name = "Periferia / vapor"};
            PixelSprites = new PixelSpriteLibrary();
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

        public void CityBlock(CityBlock block, Transform parent)
            => ShowWithMaterial("Volume da periferia", CityPeripheryArt.Block(block), parent,
                block.Name.StartsWith("Ponte de pedra") || block.Name == "Patio da entrada do castelo" ? castleMaterial : cityMaterial);

        public void CityGround(Transform parent)
        {
            ShowWithMaterial("Chao continuo da periferia", CityGroundArt.Build(), parent, cityGroundMaterial);
        }

        public void CityBuilding(string name, Vector2 size, string hex, Transform parent)
        {
            ShowCityArt(name, CityPeripheryArt.Building(name, size, hex), parent);
            if (name == "Castelo do Rei Rubro") CastleTorches(size, parent, PixelSprites.Material);
        }

        public static void CastleTorches(Vector2 size, Transform parent, Material spriteMaterial)
        {
            foreach (float x in new[] {-CastleFortressArt.TorchOffsetX, CastleFortressArt.TorchOffsetX})
            {
                var flame = new GameObject(x < 0 ? "Tocha oeste / chama animada" : "Tocha leste / chama animada");
                flame.transform.SetParent(parent, false);
                flame.transform.localPosition = CastleFortressArt.TorchFlamePosition(x, size);
                flame.transform.localRotation = Quaternion.LookRotation(
                    new Vector3(-ObliqueProjection.YawSin, ObliqueProjection.YawCos, 0), PixelSpriteLayout.UprightUp);
                flame.transform.localScale = new Vector3(0.7f, 0.7f * PixelSpriteLayout.UprightScaleY, 1);
                flame.AddComponent<RuinsFlameAnimation>().Initialize(spriteMaterial, x < 0 ? 0 : 0.3f);
            }
        }

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

        public GameObject World(Transform parent)
        {
            ShowWithMaterial("Mapa completo em pixel art / terreno, costa e mar",WorldIslandArt.Ground(),parent,worldGroundMaterial);
            var seal = new GameObject(PrototypeSecretLayout.SealedRocks);
            seal.transform.SetParent(parent, false); seal.transform.localPosition = PrototypeWorldLayout.Volcano;
            var data = WorldIslandArt.Ward();
            RenderMesh("Tres selos ao redor do vulcao",data,data.Triangles,glowMaterial,seal);
            seal.AddComponent<CircleCollider2D>().radius = PrototypeWorldLayout.WardRadius;
            return seal;
        }

        public void CastleIcon(Transform parent)
        {
            WorldPoi(parent, 0);
            // Mantem exatamente a area de colisao do icone declarada no layout.
            CityBlock footprint=PrototypeWorldLayout.CastleIcon[0];
            BoxCollider2D collider=parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size=footprint.Size; collider.offset=footprint.Position;
        }

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
            Show("Sombra projetada atras do POI", WorldPoiShadowData(frame), parent);
            Transform visual = PixelSprites.Create(parent, "WorldPoiSprites", frame);
            visual.localPosition = new Vector3(ObliqueProjection.YawSin * 0.3f, -ObliqueProjection.YawCos * 0.3f, -0.015f);
        }

        public static ObliqueMeshData WorldPoiShadowData(int frame)
        {
            var mesh = new ObliqueMeshData();
            Vector2 rear = ObliqueProjection.GroundDirection(new Vector2(0.22f, 0.9f)).normalized;
            Vector2 side = new Vector2(rear.y, -rear.x);
            Vector2 foot = new Vector2(ObliqueProjection.YawSin * 0.3f, -ObliqueProjection.YawCos * 0.3f);
            const float scale = 1.10f * 1.15f * 1.25f;
            float halfWidth = (frame == 4 ? 0.85f : frame == 2 ? 0.8f : 0.7f) * scale;
            // Caixa projetada atras e ligeiramente a direita, sem elipse sob os pes.
            for (int i = 0; i < 4; i++)
            {
                float near = 0.16f + i * 0.22f * scale, far = near + 0.22f * scale;
                Vector2 a = foot + rear * near - side * halfWidth, b = foot + rear * near + side * halfWidth;
                Vector2 c = foot + rear * far + side * halfWidth, d = foot + rear * far - side * halfWidth;
                mesh.Quad(ObliqueMeshData.V(a.x,a.y,0.035f), ObliqueMeshData.V(b.x,b.y,0.035f),
                    ObliqueMeshData.V(c.x,c.y,0.035f), ObliqueMeshData.V(d.x,d.y,0.035f),
                    new Color(0,0,0,0.32f-i*0.04f), false, PixelSurface.None);
            }
            return mesh;
        }
        public void RuinsIcon(Transform parent)
        {
            WorldPoi(parent, 3);
            CityBlock footprint=PrototypeRuinsLayout.Icon[0];
            BoxCollider2D collider=parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size=footprint.Size;collider.offset=footprint.Position;
        }

        public void VolcanoIcon(Transform parent)
            => WorldPoi(parent, 4);

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

        public Transform PixelPerson(Transform parent, string atlas, int frame = 0, bool animated = false)
        {
            float height = atlas == "TyrantThrone" || atlas == "NpcRoster" && frame == 11 ? 0.15f : 0.075f;
            var shadow = new ObliqueMeshData(); shadow.Shadow(0, 0, 0.34f, 0.22f);
            var shade = Show("Sombra no chao", shadow, parent); shade.localPosition = Vector3.back * (height - 0.035f);
            var visual = PixelSprites.Create(parent, atlas, frame, animated); visual.localPosition = Vector3.back * height;
            if (atlas == "SpiritBattle") SpiritFloatAnimation.Attach(visual);
            return visual;
        }

        public void Dispose()
        {
            foreach(Mesh mesh in meshes) if(mesh!=null) UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(material); UnityEngine.Object.Destroy(shadowMaterial);
            UnityEngine.Object.Destroy(cityMaterial); UnityEngine.Object.Destroy(cityDetailsMaterial); UnityEngine.Object.Destroy(glowMaterial);
            UnityEngine.Object.Destroy(castleMaterial);
            UnityEngine.Object.Destroy(cityGroundMaterial);
            UnityEngine.Object.Destroy(worldGroundMaterial);
            PixelSprites.Dispose();
        }
    }
}
