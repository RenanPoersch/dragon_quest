using DragonQuest.Progression;
using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Exploration.Layouts;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonQuest.Exploration;
using UnityEngine;

internal static class ObliqueChecks
{
    private static int checks;
    private static void Assert(bool condition,string text) { checks++; if(!condition) throw new Exception(text); }
    private static void Near(float a,float b,string text) => Assert(Math.Abs(a-b)<0.0001f,text);
    private static void NearPixel(float a,float b,string text) => Assert(Math.Abs(a-b)<0.001f,text);

    private static void Validate(ObliqueMeshData data,string name)
    {
        Assert(data.Vertices.Count==data.Colors.Count && data.Triangles.Count%3==0,name+": buffers incompatíveis.");
        Assert(data.UVs.Count == data.Vertices.Count && data.Surfaces.Count == data.Vertices.Count, name + ": buffers de textura incompletos.");
        Assert(data.UVs.All(uv => float.IsFinite(uv.x) && float.IsFinite(uv.y))
            && data.Surfaces.All(s => s.x >= -1 && s.x < 12 && s.y >= 0.62f && s.y <= 1), name + ": material ou UV invalido.");
        Assert(data.Vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),name+": coordenada inválida.");
        Assert(data.Colors.All(c=>float.IsFinite(c.r)&&float.IsFinite(c.g)&&float.IsFinite(c.b)&&c.r>=0&&c.r<=1&&c.g>=0&&c.g<=1&&c.b>=0&&c.b<=1),name+": cor inválida.");
        Assert(data.Triangles.All(i=>i>=0&&i<data.Vertices.Count),name+": índice fora da malha.");
        bool degenerate=false;
        for(int i=0;i<data.Triangles.Count;i+=3)
        {
            Vector3 a=data.Vertices[data.Triangles[i]],b=data.Vertices[data.Triangles[i+1]],c=data.Vertices[data.Triangles[i+2]];
            double ux=b.x-a.x,uy=b.y-a.y,uz=b.z-a.z,vx=c.x-a.x,vy=c.y-a.y,vz=c.z-a.z;
            double x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;
            if(x*x+y*y+z*z<1e-18) degenerate=true;
        }
        Assert(!degenerate,name+": triângulo sem área.");
    }

    private static void Export(string directory,string name,ObliqueMeshData data,Vector2 focus,float size,float focusHeight=0, object[] sprites=null,
        string atlas="TerrainAtlas", bool additive=false, bool singleTexture=false)
    {
        File.WriteAllText(Path.Combine(directory,name+".json"),JsonSerializer.Serialize(new {
            vertices=data.Vertices.Select(p=>new[]{p.x,p.y,p.z}),colors=data.Colors.Select(c=>new[]{c.r,c.g,c.b,c.a}),atlas,additive,singleTexture,
            triangles=data.Triangles,focus=new[]{focus.x,focus.y},size,focusHeight,
            uv=data.UVs.Select(uv=>new[]{uv.x,uv.y}),surfaces=data.Surfaces.Select(s=>new[]{s.x,s.y}),sprites=sprites??Array.Empty<object>(),
            groundScale=ObliqueProjection.GroundScale,heightScale=ObliqueProjection.HeightScale,
            yawCos=ObliqueProjection.YawCos,yawSin=ObliqueProjection.YawSin
        }));
    }
    private static void Person(ObliqueMeshData scene,Vector2 point,string color,bool armed)
    {
        scene.Append(LegacySceneryGeometry.PersonBodyData(color,armed),ObliqueProjection.Point(point));
        foreach(float x in new[]{-0.12f,0.12f})
            scene.Append(LegacySceneryGeometry.LegData(x,ObliqueMeshData.Hex(color)),ObliqueProjection.Point(point,0.5f));
    }
    private static void AddBlock(ObliqueMeshData scene,CityBlock block)
        => scene.Append(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),ObliqueProjection.Point(block.Position));

    public static void Main(string[] args)
    {
        string directory=args[0];
        PixelArtChecks.Run();
        CheckSpriteDepth(directory);
        CheckCameraTracking();
        CheckPaintedGround(directory);
        CheckCityViewportCoverage(directory);
        Near(ObliqueProjection.ScreenPlane(new Vector2(4,3)).x,4*ObliqueProjection.YawCos+3*ObliqueProjection.YawSin,"Eixos do chão devem acompanhar rotação da câmera.");
        Near(ObliqueProjection.ScreenPlane(new Vector2(4,3)).y,(-4*ObliqueProjection.YawSin+3*ObliqueProjection.YawCos)*ObliqueProjection.GroundScale,"Profundidade do chão precisa comprimir na tela.");
        Near(ObliqueProjection.ScreenPlane(Vector2.zero,2).y,2*ObliqueProjection.HeightScale,"Altura deve subir na tela.");
        Assert(ObliqueProjection.Point(Vector2.zero,2).z==-2,"Altura não pode mover a posição física XY.");
        foreach(var input in new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)})
        {
            var screen=ObliqueProjection.ScreenPlane(ObliqueProjection.GroundDirection(input));
            Near(screen.x,input.x,"Controles horizontais precisam corresponder à tela.");
            Near(screen.y,input.y*ObliqueProjection.GroundScale,"Controles verticais precisam corresponder à tela.");
        }
        var travel=new WorldNavigation();
        foreach(var map in travel.Maps)
        {
            Assert(map.FixedCamera==(map.Id!=WorldNavigation.City&&map.Id!=WorldNavigation.World&&map.Id!=WorldNavigation.Volcano),"Ilha, cidade e trilha do vulcao seguem o heroi; ambientes menores usam enquadramento fixo.");
            foreach(var hero in new[]{map.Spawn,new Vector2(map.Bounds.xMax,map.Bounds.yMax)})
            {
                var focus=ObliqueProjection.CameraFocus(hero,map.Bounds,map.CameraSize,16f/9,map.FixedCamera);
                Assert(float.IsFinite(focus.x)&&float.IsFinite(focus.y),"Foco da câmera precisa ser finito.");
                if(map.FixedCamera) Assert(focus==map.Bounds.center,"Câmera fixa não pode saltar com o personagem.");
            }
            foreach(float aspect in new[]{16f/9,4f/3,0.75f})
            {
                float size=ObliqueProjection.FrameSize(map.CameraSize,map.Bounds,aspect,map.FixedCamera);
                foreach(var point in new[]{new Vector2(map.Bounds.xMin,map.Bounds.yMin),new Vector2(map.Bounds.xMin,map.Bounds.yMax),
                    new Vector2(map.Bounds.xMax,map.Bounds.yMin),new Vector2(map.Bounds.xMax,map.Bounds.yMax)})
                {
                    var screen=ObliqueProjection.ScreenPlane(new Vector2(point.x-map.Bounds.center.x,point.y-map.Bounds.center.y));
                    if(map.FixedCamera) Assert(Math.Abs(screen.x)<=size*aspect&&Math.Abs(screen.y)<=size,"Câmera fixa deve enquadrar o mapa em telas de proporções diferentes.");
                }
            }
        }
        foreach(var point in new[]{WorldLayout.Spawn,WorldLayout.Castle,WorldLayout.WestForest,
            WorldLayout.EastForest,WorldLayout.Ruins,WorldLayout.Volcano,new Vector2(-14,0),new Vector2(12,-6)})
            foreach(float aspect in new[]{16f/9,4f/3,0.75f})
            {
                Vector2 focus=ObliqueProjection.CameraFocus(point,WorldLayout.Bounds,travel.Current.CameraSize,aspect,false);
                Vector2 body=ObliqueProjection.ScreenPlane(new Vector2(point.x-focus.x,point.y-focus.y),ObliqueProjection.HeroCenterHeight);
                Vector2 anchor=ObliqueProjection.ScreenPlane(Vector2.zero,ObliqueProjection.HeroCenterHeight);
                Near(body.x,0,"Herói precisa ficar no centro horizontal da ilha, inclusive ao nascer e junto à costa.");
                Near(body.y-anchor.y,0,"Foco elevado deve alinhar centro do corpo verticalmente.");
            }
        foreach(var point in new[]{new Vector2(-12,21),new Vector2(12,21),new Vector2(-12,-21),new Vector2(12,-21)})
            foreach(float aspect in new[]{16f/9,4f/3})
            {
                Vector2 focus=ObliqueProjection.CameraFocus(point,CityLayout.Bounds,7.5f,aspect,false);
                Vector2 relative=new Vector2(point.x-focus.x,point.y-focus.y);
                var foot=ObliqueProjection.ScreenPlane(relative);var head=ObliqueProjection.ScreenPlane(relative,1.45f);
                Assert(Math.Abs(foot.x)<7.5f*aspect&&Math.Abs(foot.y)<7.5f&&Math.Abs(head.y)<7.5f,"Câmera deve manter pés/cabeça visíveis nas extremidades.");
                Assert(CityLayout.Bounds.Contains(focus),"Acompanhamento nos cantos deve manter foco dentro da periferia.");
            }
        foreach(float aspect in new[]{16f/9,4f/3})
        {
            float size=RuinsLayout.CameraSize;
            float flameY=RuinsLayout.Altar.y+2.15f-RuinsLayout.CameraHeight;
            Assert(Math.Abs(flameY)<size,"Camera lateral deve enquadrar altar e chama sobre os tres degraus.");
        }

        var world=LegacySceneryGeometry.IslandData(); Validate(world,"Costa e mar");
        // Area triangulada da superficie precisa corresponder ao contorno concavo.
        var inner=WorldLayout.Coast.Select(p=>new Vector2(p.x*0.955f,p.y*0.955f)).ToArray();
        var surface=new ObliqueMeshData();surface.Polygon(inner,0,ObliqueMeshData.Hex("71806A"));
        double polygonArea=0,triangleArea=0;
        for(int i=0;i<inner.Length;i++) {var a=inner[i];var b=inner[(i+1)%inner.Length];polygonArea+=(double)a.x*b.y-(double)b.x*a.y;}
        for(int i=0;i<surface.Vertices.Count;i+=3)
        {var a=surface.Vertices[i];var b=surface.Vertices[i+1];var c=surface.Vertices[i+2];triangleArea+=Math.Abs((double)(b.x-a.x)*(c.y-a.y)-(double)(b.y-a.y)*(c.x-a.x));}
        Assert(Math.Abs(polygonArea-triangleArea)<0.001,"Triangulação deve preencher a ilha sem sobreposição nem buracos.");
        var islandGround = WorldIslandArt.Ground(); Validate(islandGround,"Pintura continua da ilha");
        for (int i = 0; i < islandGround.Vertices.Count; i++)
        {
            var p = islandGround.Vertices[i]; var bounds = WorldLayout.GroundPaintingBounds;
            Near(islandGround.UVs[i].x, (p.x - bounds.xMin) / bounds.width, "UV X absoluta da pintura da ilha.");
            Near(islandGround.UVs[i].y, (p.y - bounds.yMin) / bounds.height, "UV Y absoluta da pintura da ilha.");
            Assert(islandGround.UVs[i].x >= 0 && islandGround.UVs[i].x <= 1 && islandGround.UVs[i].y >= 0 && islandGround.UVs[i].y <= 1,
                "Ilha inteira deve ficar dentro da pintura, sem repeticao nas bordas.");
        }
        var ward = new ObliqueMeshData(); ward.Append(WorldIslandArt.Ward(), ObliqueProjection.Point(WorldLayout.Volcano));
        Validate(ward,"Barreira magica circular");
        Assert(ward.Colors.All(c=>c.a>0&&c.a<1),"Barreira deve permitir ver o vulcao atraves dela.");
        foreach (var p in WorldIslandArt.Ward().Vertices)
            Near((float)Math.Sqrt(p.x*p.x+p.y*p.y),WorldLayout.WardRadius,"Parede magica deve acompanhar o colisor circular.");
        MapDefinition worldMap=travel.Current;
        float worldSize=ObliqueProjection.FrameSize(worldMap.CameraSize,worldMap.Bounds,16f/9,worldMap.FixedCamera);
        Vector2 worldFocus=ObliqueProjection.CameraFocus(WorldLayout.Spawn,worldMap.Bounds,worldSize,16f/9,worldMap.FixedCamera);
        Export(directory,"WorldOblique",world,worldFocus,worldSize,ObliqueProjection.HeroCenterHeight,
            new[]{Sprite("ArenWalk",0,WorldLayout.Spawn),
                WorldIcon(0,WorldLayout.Castle),WorldIcon(1,WorldLayout.WestForest),
                WorldIcon(2,WorldLayout.EastForest),WorldIcon(3,WorldLayout.Ruins),WorldIcon(4,WorldLayout.Volcano)});
        Export(directory,"WorldGround",islandGround,worldFocus,worldSize,ObliqueProjection.HeroCenterHeight,atlas:"WorldGroundMap",singleTexture:true);
        Export(directory,"WorldWard",ward,worldFocus,worldSize,ObliqueProjection.HeroCenterHeight,additive:true);

        var city=new ObliqueMeshData(); var cityDetails=new ObliqueMeshData(); var cityGlow=new ObliqueMeshData(); var fortress=new ObliqueMeshData();
        foreach(var block in CityLayout.Blocks)
        {
            var data=CityPeripheryArt.Block(block); Validate(data,block.Name);
            var target = block.Name.StartsWith("Ponte de pedra") || block.Name == "Patio da entrada do castelo" ? fortress : city;
            target.Append(data,ObliqueProjection.Point(block.Position));
        }
        foreach(var building in CityLayout.Buildings)
        {
            var art=CityPeripheryArt.Building(building.Name,building.Size,building.Color);
            Validate(art.Body,building.Name); Validate(art.Details,building.Name+" / detalhes"); Validate(art.Glow,building.Name+" / vapor");
            Assert(art.Details.UVs.All(uv=>uv.x>0&&uv.x<1&&uv.y>0&&uv.y<1),"Painel deve usar uma celula inteira, sem repetir letras nem amostrar vizinhos.");
            Assert(art.Glow.Colors.All(c=>c.a>0&&c.a<1),"Vapor usa transparencia propria.");
            var offset=ObliqueProjection.Point(building.Position);
            if (CityPeripheryArt.IsRoyalCastle(building.Name))
            {
                fortress.Append(art.Body,offset);
                Assert(-art.Body.Vertices.Min(p=>p.z)>7.8f,"Castelo e torres devem ser muito mais altos que as casas do bairro.");
                Assert(art.Body.Surfaces.All(s=>s.x==-1||s.x==(int)PixelSurface.Sand||s.x==(int)PixelSurface.Wall
                    ||s.x==(int)PixelSurface.Basalt||s.x==(int)PixelSurface.Paving||s.x==(int)PixelSurface.Wood||s.x==(int)PixelSurface.Fabric),
                    "Fortaleza deve usar pedras, porta de carvalho e estandartes, sem telhado industrial.");
                var solidVertices = art.Body.Vertices.Where((p,i)=>art.Body.Colors[i].a==1).ToArray();
                Assert(solidVertices.Min(p=>p.x)>=-building.Size.x/2-0.051f && solidVertices.Max(p=>p.x)<=building.Size.x/2+0.051f,
                    "Volumes de pedra nao podem invadir os acessos laterais alem da cornija.");
            }
            else city.Append(art.Body,offset);
            cityDetails.Append(art.Details,offset);cityGlow.Append(art.Glow,offset);
        }
        var streets=CityPeripheryArt.Streets(); Validate(streets.Body,"Detalhes das ruas"); Validate(streets.Glow,"Transparencia das ruas");
        var ground = CityGroundArt.Build();
        Assert(ground.Vertices.Where(p=>p.z==0).All(p=>p.y<=10.1f||p.y>=14.9f),"Piso da cidade nao pode cobrir o fosso seco, inclusive alem da area caminhavel.");
        var moat = CityPeripheryArt.Block(CityLayout.Blocks.First(b=>b.Name.StartsWith("Fosso seco")));
        Assert(moat.Vertices.Any(p=>p.z>0.5f),"Fosso precisa ficar visualmente abaixo da rua, com bloqueio fisico definido no layout.");
        city.Append(streets.Body,Vector3.zero);cityGlow.Append(streets.Glow,Vector3.zero);
        CheckEquipmentApproach();
        CheckExplorerVisibility();
        var sign=new ObliqueMeshData();CityPeripheryArt.Panel(sign,0,-2.1f,2,3,0.8f,1);
        Near(sign.UVs[0].x,0.01f,"Placa inicia na borda esquerda da celula.");Near(sign.UVs[2].x,0.99f,"Placa termina na borda direita da celula.");
        Assert(sign.Surfaces.All(s=>s.x==1&&s.y==1),"Letreiro nao pode receber material de grama ou escurecimento por face.");
        Assert(sign.Vertices.All(p=>p.y< -2),"Letreiro precisa estar preso a fachada, nao flutuar sobre o telhado.");
        Validate(LegacySceneryGeometry.PersonBodyData("B4A078",true),"Herói");
        foreach(float x in new[]{-0.12f,0.12f})Validate(LegacySceneryGeometry.LegData(x,ObliqueMeshData.Hex("B4A078")),"Perna");
        Export(directory,"CityOblique",city,CityLayout.Spawn,6,ObliqueProjection.HeroCenterHeight,
            new[]{Sprite("ArenWalk",0,CityLayout.Spawn),Sprite("NpcRoster",0,CityLayout.Citizen),Sprite("NpcRoster",10,CityLayout.Guard)},"CityMaterialsAtlas");
        Export(directory,"CityDetails",cityDetails,CityLayout.Spawn,6,ObliqueProjection.HeroCenterHeight,atlas:"CityDetailsAtlas");
        Export(directory,"CityGlow",cityGlow,CityLayout.Spawn,6,ObliqueProjection.HeroCenterHeight,additive:true);
        Export(directory,"CityFortress",fortress,CityLayout.Spawn,6,ObliqueProjection.HeroCenterHeight,atlas:"CastleMaterialsAtlas");
        var hall=new ObliqueMeshData();
        foreach(var block in WorldLayout.CastleBlocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(hall,block);}
        var king=LegacySceneryGeometry.KingData();var spirit=LegacySceneryGeometry.SpiritData();
        Validate(king,"Rei Rubro e coroa");Validate(LegacySceneryGeometry.KingData(false),"Rei liberto");Validate(spirit,"Espirito maligno");
        Assert(king.Vertices.All(p=>p.z<=0),"Botas do rei nao podem ficar abaixo do piso.");
        hall.Append(spirit,ObliqueProjection.Point(WorldLayout.Throne+Vector2.right*1.8f));
        Export(directory,"KingPortrait",king,Vector2.zero,1.1f,0.85f);
        Export(directory,"SpiritPortrait",spirit,Vector2.zero,1.4f,1.05f);
        Export(directory,"CastleOblique",hall,Vector2.zero,9,0,
            new[]{Sprite("ArenWalk",0,WorldLayout.CastleSpawn),Sprite("NpcRoster",11,WorldLayout.Throne)});
        // Ruins now use bitmap layers; their previews are generated by RuinsSideValidation in Unity.
        var dragon=LegacySceneryGeometry.DragonData();Validate(dragon,"Dragao e asas");
        Export(directory,"DragonPortrait",dragon,new Vector2(0,0.5f),2.6f,1);
        var cavern=new ObliqueMeshData();
        foreach(var block in VolcanoLayout.Blocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(cavern,block);}
        cavern.Append(dragon,ObliqueProjection.Point(VolcanoLayout.Dragon));
        Export(directory,"VolcanoOblique",cavern,new Vector2(0,5),7.5f,ObliqueProjection.HeroCenterHeight,
            new[]{Sprite("ArenWalk",0,new Vector2(0,5))});
        // The complete painted inn is rendered and checked by InnValidation in Unity.
        foreach(var ending in new[]{DragonQuest.Progression.GameEnding.KingFreed,DragonQuest.Progression.GameEnding.CastleDestroyed,DragonQuest.Progression.GameEnding.HeroPossessed})
        {
            var data=LegacySceneryGeometry.EndingSceneData(ending);Validate(data,"Cena final / "+ending);
            Assert(data.Vertices.All(p=>p.z<=0),"Atores das cenas finais devem ficar sobre o piso.");
            Export(directory,"Ending"+ending,data,Vector2.zero,5.8f);
        }
        foreach(bool eastern in new[]{false,true})
            foreach(var block in WorldLayout.CreateForestBlocks(eastern))Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);
        Validate(ObliqueScenery.BlockData("Balcao",new Vector2(4,0.6f),"614533",true,4),"Balcão");
        Validate(ObliqueScenery.BlockData("Estante",new Vector2(1.5f,3),"705033",true,3),"Estante");
        Console.WriteLine(checks+" verificacoes de perspectiva, camera e malhas passaram.");
    }
    private static void CheckPaintedGround(string directory)
    {
        var ground=CityGroundArt.Build();Validate(ground,"Pintura unica do chao");
        var bounds=CityGroundArt.TerrainBounds;
        var coreMin=CityGroundArt.MapUv(new Vector2(CityLayout.Bounds.xMin,CityLayout.Bounds.yMin));
        var coreMax=CityGroundArt.MapUv(new Vector2(CityLayout.Bounds.xMax,CityLayout.Bounds.yMax));
        NearPixel(coreMin.x*CityLayout.GroundTextureWidth,670,"Piso original deve comecar no pixel horizontal nativo.");
        NearPixel((1-coreMax.y)*CityLayout.GroundTextureHeight,673,"Piso original deve comecar no pixel vertical nativo.");
        NearPixel((coreMax.x-coreMin.x)*CityLayout.GroundTextureWidth,965,"Largura original nao pode ser ampliada no mapa unico.");
        NearPixel((coreMax.y-coreMin.y)*CityLayout.GroundTextureHeight,1629,"Altura original nao pode ser ampliada no mapa unico.");
        Assert(ground.Vertices.Count==24&&ground.Triangles.Count==24,"Todo o terreno deve usar uma pintura inteira; o fosso nao pode ter tampas laterais na antiga borda.");
        Assert(ground.UVs.All(uv=>uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1),"UV deve permanecer dentro da imagem, sem repeticao.");
        for(int i=0;i<ground.Vertices.Count;i++)
        {
            var p=ground.Vertices[i];var uv=ground.UVs[i];
            Near(uv.x*bounds.width+bounds.xMin,p.x,"Cada pixel horizontal precisa corresponder a mesma posicao na planta.");
            Near(uv.y*bounds.height+bounds.yMin,p.y,"Norte e sul da imagem precisam corresponder a planta, sem inversao.");
        }
        foreach(var point in CityLayout.Street.Concat(new[]{CityLayout.Spawn,CityLayout.ItemDoor,
            CityLayout.EquipmentDoor,CityLayout.MateriaDoor,CityLayout.InnDoor,CityLayout.CastleDoor}))
        {
            float height=10-RayDistance(ObliqueProjection.Point(point,10),new Vector3(0,0,1),ground);
            Assert(height>=-0.0001f&&height<=0.06f,"Vielas, retornos e portas devem permanecer cobertos pelo chao unico sem desnivel maior que a ponte.");
        }
        Near(10-RayDistance(ObliqueProjection.Point(CityLayout.Spawn,10),new Vector3(0,0,1),ground),0,
            "Piso da chegada deve permanecer na altura do chao.");
        Near(10-RayDistance(ObliqueProjection.Point(CityLayout.Bridge,10),new Vector3(0,0,1),ground),0.058f,
            "Pintura da ponte deve ficar sobre o volume de pedra.");
        foreach(float x in new[]{bounds.xMin+1,-20f,-7f,7f,20f,bounds.xMax-1})
            Near(10-RayDistance(ObliqueProjection.Point(new Vector2(x,CityLayout.Bridge.y),10),new Vector3(0,0,1),ground),-0.7f,
                "Fosso deve conservar profundidade em toda a largura visivel.");
        foreach(var block in CityLayout.Blocks.Where(b=>b.Name.StartsWith("Fosso seco")))
        {
            var wall=CityPeripheryArt.Block(block);
            float edge=block.Position.x<0 ? wall.Vertices.Min(p=>p.x)+block.Position.x : wall.Vertices.Max(p=>p.x)+block.Position.x;
            Near(edge,block.Position.x<0 ? bounds.xMin : bounds.xMax,"Paredes do fosso devem continuar ate a borda da pintura.");
        }
        foreach(var block in CityLayout.Blocks.Where(b=>CityGroundArt.ReplacesBlock(b.Name)))
            Assert(CityPeripheryArt.Block(block).Vertices.Count==0,"Pisos antigos nao podem cobrir a textura unica com blocos repetidos.");
        Export(directory,"CityGround",ground,CityLayout.Spawn,6,ObliqueProjection.HeroCenterHeight,
            atlas:"CityGroundMap",singleTexture:true);
    }
    private static void CheckEquipmentApproach()
    {
        var towardCamera=new Vector3(ObliqueProjection.YawSin*ObliqueProjection.HeightScale,
            -ObliqueProjection.YawCos*ObliqueProjection.HeightScale,-ObliqueProjection.GroundScale);
        var oldHouse=new ObliqueMeshData();
        oldHouse.Append(CityPeripheryArt.Building("Casa do padeiro",new Vector2(3.6f,4),"80684F").Body,
            ObliqueProjection.Point(new Vector2(4.8f,-16)));
        Assert(float.IsFinite(RayDistance(ObliqueProjection.Point(new Vector2(4.2f,-13.225f),0.075f),towardCamera,oldHouse)),
            "Regressao deve reproduzir a casa ocultando o acesso antigo da loja de armas.");
        var others=new ObliqueMeshData();
        foreach(var building in CityLayout.Buildings.Where(b=>b.Name!="Loja de equipamentos"))
            others.Append(CityPeripheryArt.Building(building.Name,building.Size,building.Color).Body,ObliqueProjection.Point(building.Position));
        foreach(float x in new[]{-0.3f,0,0.3f}) foreach(float h in new[]{0.075f,0.7f,1.4f})
            Assert(!float.IsFinite(RayDistance(ObliqueProjection.Point(CityLayout.DoorReturn(CityLayout.EquipmentDoor)+Vector2.right*x,h),towardCamera,others)),
                "Casa vizinha nao pode esconder o personagem no patio de acesso da loja de armas.");
        foreach(float x in new[]{-1f,0,1f})
            Assert(!float.IsFinite(RayDistance(ObliqueProjection.Point(new Vector2(CityLayout.EquipmentDoor.x+x,-12.445f),2.2f),towardCamera,others)),
                "Casa vizinha nao pode esconder o letreiro ARMAS.");
    }
    private static void CheckExplorerVisibility()
    {
        var buildings=new ObliqueMeshData();
        foreach(var building in CityLayout.Buildings)
            buildings.Append(CityPeripheryArt.Building(building.Name,building.Size,building.Color).Body,ObliqueProjection.Point(building.Position));
        buildings.Append(CityPeripheryArt.Streets().Body,Vector3.zero);
        var towardCamera=new Vector3(ObliqueProjection.YawSin*ObliqueProjection.HeightScale,
            -ObliqueProjection.YawCos*ObliqueProjection.HeightScale,-ObliqueProjection.GroundScale);
        Assert(float.IsFinite(RayDistance(ObliqueProjection.Point(new Vector2(7.7f,-0.2f),0.5f),towardCamera,buildings)),
            "Posicao anterior da exploradora deve reproduzir a oclusao por uma casa.");
        foreach(float x in new[]{-0.2f,0,0.2f}) foreach(float height in new[]{0.075f,0.5f,1f,1.4f})
            Assert(!float.IsFinite(RayDistance(ObliqueProjection.Point(CityLayout.Explorer+Vector2.right*x,height),towardCamera,buildings)),
                "Exploradora deve aparecer inteira na rua central.");
    }
    private static void CheckCityViewportCoverage(string directory)
    {
        var floor = CityGroundArt.Build();
        foreach (var block in CityLayout.Blocks.Where(b => b.Name.StartsWith("Fosso seco")))
            floor.Append(CityPeripheryArt.Block(block), ObliqueProjection.Point(block.Position));
        Rect city = CityLayout.Bounds, terrain = CityGroundArt.TerrainBounds;
        // Dentro e fora do antigo limite usam a mesma funcao linear: densidade de texels identica.
        foreach (float y in new[] {-22f, -10, 0, 22f}) foreach (float edge in new[] {-13f, 13f})
        {
            var left = CityGroundArt.MapUv(new Vector2(edge - 1, y));
            var middle = CityGroundArt.MapUv(new Vector2(edge, y));
            var rightUv = CityGroundArt.MapUv(new Vector2(edge + 1, y));
            Near(middle.x - left.x, rightUv.x - middle.x, "Escala horizontal da textura nao pode mudar na antiga borda.");
            Near(middle.y, left.y, "UV vertical deve ser continuo ao cruzar a antiga borda.");
        }
        foreach (float x in new[] {-13f, 0, 13f}) foreach (float edge in new[] {-22f, 22f})
        {
            var below = CityGroundArt.MapUv(new Vector2(x, edge - 1));
            var middle = CityGroundArt.MapUv(new Vector2(x, edge));
            var above = CityGroundArt.MapUv(new Vector2(x, edge + 1));
            Near(middle.y - below.y, above.y - middle.y, "Escala vertical da textura nao pode mudar na antiga borda.");
            Near(middle.x, below.x, "UV horizontal deve ser continuo ao cruzar a antiga borda.");
        }
        foreach (var block in CityLayout.Blocks.Where(b => b.Name.StartsWith("Muralha ")))
            Assert(block.Solid && CityPeripheryArt.Block(block).Vertices.Count == 0,
                "Limites de movimento devem continuar solidos sem mureta cortando a pintura.");
        var forward = new Vector3(-ObliqueProjection.YawSin * ObliqueProjection.HeightScale,
            ObliqueProjection.YawCos * ObliqueProjection.HeightScale, ObliqueProjection.GroundScale);
        var right = new Vector3(ObliqueProjection.YawCos, ObliqueProjection.YawSin, 0);
        var up = new Vector3(-ObliqueProjection.YawSin * ObliqueProjection.GroundScale,
            ObliqueProjection.YawCos * ObliqueProjection.GroundScale, -ObliqueProjection.HeightScale);
        foreach (float aspect in new[] {9f/16, 3f/4, 1f, 4f/3, 16f/9, 21f/9, 32f/9, 5f})
        {
            float size = ObliqueProjection.FrameSize(6, city, aspect, false, terrain);
            Assert(size > 0 && size <= 6, "Enquadramento coberto deve permanecer valido sem afastar a camera.");
            if (aspect <= 21f/9) Near(size, 6, "Formatos usuais devem conservar o zoom da periferia.");
            foreach (float x in new[] {city.xMin, city.center.x, city.xMax}) foreach (float y in new[] {city.yMin, city.center.y, city.yMax})
            {
                var hero = new Vector2(x, y);
                var camera = ObliqueProjection.CameraPosition(hero, city, size, aspect, false, forward, terrain);
                var focus = camera + forward * ObliqueProjection.Distance;
                Near(focus.x, hero.x, "A extensao deve manter o heroi centrado, inclusive no limite oeste/leste.");
                Near(focus.y, hero.y, "A extensao deve manter o heroi centrado, inclusive no limite norte/sul.");
                foreach (float sx in new[] {-size * aspect, 0, size * aspect}) foreach (float sy in new[] {-size, 0, size})
                {
                    // Raios da camera sobre as malhas reais, sem reutilizar o calculo dos limites.
                    float hit = RayDistance(camera + right * sx + up * sy, forward, floor);
                    Assert(float.IsFinite(hit) && hit > 0.1f && hit < 150,
                        $"Toda a tela deve atingir chao texturizado: aspecto {aspect}, tamanho {size}, heroi ({x}, {y}), tela ({sx}, {sy}), distancia {hit}.");
                }
            }
            var clamped = ObliqueProjection.CameraFocus(new Vector2(city.xMax + 10, city.yMin - 10), city, size, aspect, false, terrain);
            Assert(clamped == new Vector2(city.xMax, city.yMin), "A camera nao pode ultrapassar os limites da cidade.");
        }
        foreach (var map in new WorldNavigation().Maps)
            Assert(map.CoveredGroundBounds.HasValue == (map.Id == WorldNavigation.City),
                "A regra de cobertura so deve estar ativa na periferia nesta entrega.");
    }
    private static void CheckCameraTracking()
    {
        var forward=new Vector3(-ObliqueProjection.YawSin*ObliqueProjection.HeightScale,
            ObliqueProjection.YawCos*ObliqueProjection.HeightScale,ObliqueProjection.GroundScale);
        var directions=new[]{Vector2.down,Vector2.left,Vector2.right,Vector2.up,
            new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)};
        var frameTimes=new[]{new[]{1f/30},new[]{1f/60},new[]{1f/144},new[]{0.008f,0.027f,0.013f,0.022f}};
        const float physicsStep=0.02f;
        foreach(var direction in directions) foreach(var pattern in frameTimes)
        {
            var velocity=ObliqueProjection.GroundDirection(direction.normalized)*4;
            float time=0.5f,maxFocusError=0,maxShiftError=0;
            var presented=velocity*(time-physicsStep);
            var previousCamera=ObliqueProjection.CameraPosition(presented,WorldLayout.Bounds,6.85f,16f/9,false,forward);
            for(int frame=0;frame<160;frame++)
            {
                float dt=pattern[frame%pattern.Length];time+=dt;
                // Simula poses interpoladas entre passos de 50 Hz, com renderizacao regular ou variavel.
                float tick=(float)Math.Floor(time/physicsStep)*physicsStep;
                float alpha=(time-tick)/physicsStep;
                presented=velocity*(tick-physicsStep)+velocity*physicsStep*alpha;
                var camera=ObliqueProjection.CameraPosition(presented,WorldLayout.Bounds,6.85f,16f/9,false,forward);
                var focus=camera+forward*ObliqueProjection.Distance;
                maxFocusError=Math.Max(maxFocusError,Math.Abs(focus.x-presented.x)+Math.Abs(focus.y-presented.y)
                    +Math.Abs(focus.z+ObliqueProjection.HeroCenterHeight));
                maxShiftError=Math.Max(maxShiftError,Math.Abs(camera.x-previousCamera.x-velocity.x*dt)
                    +Math.Abs(camera.y-previousCamera.y-velocity.y*dt));
                previousCamera=camera;
            }
            Assert(maxFocusError<0.0001f,"Camera deve manter o centro do corpo alinhado ao alvo apresentado nas oito direcoes.");
            Assert(maxShiftError<0.0001f,"Pan deve acompanhar cada deslocamento interpolado sem atraso adicional, inclusive com tempos de quadro variaveis.");
        }
        var fixedCamera=ObliqueProjection.CameraPosition(InnLayout.Spawn,InnLayout.Bounds,6,16f/9,true,forward);
        Assert(fixedCamera==ObliqueProjection.CameraPosition(Vector2.zero,InnLayout.Bounds,6,16f/9,true,forward),
            "Interiores com camera fixa nao podem acompanhar a caminhada.");
    }
    private static void CheckSpriteDepth(string directory)
    {
        float s=ObliqueProjection.HeightScale,c=ObliqueProjection.GroundScale;
        var forward=new Vector3(-ObliqueProjection.YawSin*s,ObliqueProjection.YawCos*s,c);
        var horizontal=PixelSpriteLayout.HorizontalCameraForward(forward);
        var right=new Vector3(horizontal.y,-horizontal.x,0);
        var oldUp=new Vector3(-ObliqueProjection.YawSin*c,ObliqueProjection.YawCos*c,-s);
        var newUp=PixelSpriteLayout.UprightUp*PixelSpriteLayout.UprightScaleY;
        var foot=ObliqueProjection.Point(new Vector2(0,-0.5f),0.075f);
        foreach(float x in new[]{-0.3f,0,0.3f}) foreach(float height in new[]{0,0.35f,0.7f,1.25f,1.45f})
        {
            // Os mesmos pixels mantem a posicao na tela, mas deixam de inclinar para dentro do balcao.
            var oldPoint=foot+right*x+oldUp*(height*s);
            var newPoint=foot+right*x+newUp*(height*s);
            var oldScreen=ObliqueProjection.ScreenPlane(new Vector2(oldPoint.x,oldPoint.y),-oldPoint.z);
            var newScreen=ObliqueProjection.ScreenPlane(new Vector2(newPoint.x,newPoint.y),-newPoint.z);
            Near(oldScreen.x,newScreen.x,"Sprite vertical deve conservar a largura e a ancora na tela.");
            Near(oldScreen.y,newScreen.y,"Sprite vertical deve conservar a altura aparente.");
            Near(newPoint.y,foot.y+right.y*x,"Cabeca e pes devem permanecer na mesma linha vertical do mundo.");
        }
        var counter=new ObliqueMeshData();
        counter.Append(ObliqueScenery.BlockData("Balcao",new Vector2(4,0.6f),"614533",true,4),new Vector3(0,0.15f,0));
        var oldHead=foot+oldUp*(1.25f*s);
        var newHead=foot+newUp*(1.25f*s);
        float hit=RayDistance(newHead,forward,counter);
        Assert(float.IsFinite(hit),"Regressao precisa sobrepor a cabeca ao tampo do balcao na tela.");
        float tableDepth=Dot(newHead+forward*hit,forward);
        Assert(Dot(oldHead,forward)>tableDepth,"Orientacao anterior deve reproduzir a cabeca escondida pelo balcao.");
        Assert(Dot(newHead,forward)<tableDepth,"Personagem em frente ao balcao deve aparecer inteiro.");
        var merchantFoot=ObliqueProjection.Point(new Vector2(0,0.9f),0.075f);
        Assert(float.IsFinite(RayDistance(merchantFoot+PixelSpriteLayout.UprightUp*0.35f,-forward,counter)),
            "Balcao deve continuar ocultando a parte inferior de quem esta atras.");
        Assert(!float.IsFinite(RayDistance(merchantFoot+PixelSpriteLayout.UprightUp*1.25f,-forward,counter)),
            "Cabeca acima do balcao deve continuar visivel.");
        var wall=new ObliqueMeshData();wall.Box(0,0.15f,4,0.6f,0,2.4f,Color.white);
        Assert(float.IsFinite(RayDistance(ObliqueProjection.Point(new Vector2(0,1),1.325f),-forward,wall)),
            "Parede alta deve continuar ocultando quem esta atras, sem desenhar sprites sempre por cima.");
        var scene=new ObliqueMeshData();
        scene.Append(ObliqueScenery.BlockData("Piso de madeira",new Vector2(14,10),"A78255",false,-10),Vector3.zero);
        scene.Append(counter,Vector3.zero);
        Export(directory,"ShopDepth",scene,Vector2.zero,2.4f,ObliqueProjection.HeroCenterHeight,
            new[]{Sprite("ArenWalk",3,new Vector2(0,-0.5f)),Sprite("NpcRoster",4,new Vector2(0,0.9f))});
    }
    private static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    private static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
    private static float RayDistance(Vector3 origin,Vector3 direction,ObliqueMeshData mesh)
    {
        float nearest=float.PositiveInfinity;
        for(int i=0;i<mesh.Triangles.Count;i+=3)
        {
            var a=mesh.Vertices[mesh.Triangles[i]];
            var edge1=mesh.Vertices[mesh.Triangles[i+1]]-a;
            var edge2=mesh.Vertices[mesh.Triangles[i+2]]-a;
            var p=Cross(direction,edge2);float determinant=Dot(edge1,p);
            if(Math.Abs(determinant)<0.00001f) continue;
            var offset=origin-a;float u=Dot(offset,p)/determinant;
            var q=Cross(offset,edge1);float v=Dot(direction,q)/determinant;
            float distance=Dot(edge2,q)/determinant;
            if(u>=0&&v>=0&&u+v<=1&&distance>0.00001f) nearest=Math.Min(nearest,distance);
        }
        return nearest;
    }
    private static object Sprite(string atlas,int frame,Vector2 position) => new {atlas,frame,position=new[]{position.x,position.y},footHeight=atlas=="NpcRoster"&&frame==11?0.15f:0.075f};
    private static object WorldIcon(int frame,Vector2 position) => new {atlas="WorldPoiSprites",frame,
        position=new[]{position.x+ObliqueProjection.YawSin*0.3f,position.y-ObliqueProjection.YawCos*0.3f},footHeight=0.015f};
}
