using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonQuest.Exploration;
using DragonQuest.Prototype;
using UnityEngine;

internal static class ObliqueChecks
{
    private static int checks;
    private static void Assert(bool condition,string text) { checks++; if(!condition) throw new Exception(text); }
    private static void Near(float a,float b,string text) => Assert(Math.Abs(a-b)<0.0001f,text);

    private static void Validate(ObliqueMeshData data,string name)
    {
        Assert(data.Vertices.Count==data.Colors.Count && data.Triangles.Count%3==0,name+": buffers incompatíveis.");
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

    private static void Export(string directory,string name,ObliqueMeshData data,Vector2 focus,float size,float focusHeight=0)
    {
        File.WriteAllText(Path.Combine(directory,name+".json"),JsonSerializer.Serialize(new {
            vertices=data.Vertices.Select(p=>new[]{p.x,p.y,p.z}),colors=data.Colors.Select(c=>new[]{c.r,c.g,c.b}),
            triangles=data.Triangles,focus=new[]{focus.x,focus.y},size,focusHeight,
            groundScale=ObliqueProjection.GroundScale,heightScale=ObliqueProjection.HeightScale,
            yawCos=ObliqueProjection.YawCos,yawSin=ObliqueProjection.YawSin
        }));
    }
    private static void Person(ObliqueMeshData scene,Vector2 point,string color,bool armed)
    {
        scene.Append(ObliqueScenery.PersonBodyData(color,armed),ObliqueProjection.Point(point));
        foreach(float x in new[]{-0.12f,0.12f})
            scene.Append(ObliqueScenery.LegData(x,ObliqueMeshData.Hex(color)),ObliqueProjection.Point(point,0.5f));
    }
    private static void AddBlock(ObliqueMeshData scene,CityBlock block)
        => scene.Append(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),ObliqueProjection.Point(block.Position));

    public static void Main(string[] args)
    {
        string directory=args[0];
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
        var travel=new PrototypeTravel();
        foreach(var map in travel.Maps)
        {
            Assert(map.FixedCamera==(map.Id!=PrototypeTravel.City&&map.Id!=PrototypeTravel.World&&map.Id!=PrototypeTravel.Volcano),"Ilha, cidade e trilha do vulcao seguem o heroi; ambientes menores usam enquadramento fixo.");
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
        foreach(var point in new[]{PrototypeWorldLayout.Spawn,PrototypeWorldLayout.Castle,PrototypeWorldLayout.WestForest,
            PrototypeWorldLayout.EastForest,PrototypeWorldLayout.Ruins,PrototypeWorldLayout.Volcano,new Vector2(-14,0),new Vector2(12,-6)})
            foreach(float aspect in new[]{16f/9,4f/3,0.75f})
            {
                Vector2 focus=ObliqueProjection.CameraFocus(point,PrototypeWorldLayout.Bounds,travel.Current.CameraSize,aspect,false);
                Vector2 body=ObliqueProjection.ScreenPlane(new Vector2(point.x-focus.x,point.y-focus.y),ObliqueProjection.HeroCenterHeight);
                Vector2 anchor=ObliqueProjection.ScreenPlane(Vector2.zero,ObliqueProjection.HeroCenterHeight);
                Near(body.x,0,"Herói precisa ficar no centro horizontal da ilha, inclusive ao nascer e junto à costa.");
                Near(body.y-anchor.y,0,"Foco elevado deve alinhar centro do corpo verticalmente.");
            }
        foreach(var point in new[]{new Vector2(-35,23),new Vector2(35,23),new Vector2(-35,-23),new Vector2(35,-23)})
            foreach(float aspect in new[]{16f/9,4f/3})
            {
                Vector2 focus=ObliqueProjection.CameraFocus(point,PrototypeCityLayout.Bounds,7.5f,aspect,false);
                Vector2 relative=new Vector2(point.x-focus.x,point.y-focus.y);
                var foot=ObliqueProjection.ScreenPlane(relative);var head=ObliqueProjection.ScreenPlane(relative,1.45f);
                Assert(Math.Abs(foot.x)<7.5f*aspect&&Math.Abs(foot.y)<7.5f&&Math.Abs(head.y)<7.5f,"Câmera deve manter pés/cabeça visíveis nas extremidades.");
                Assert(PrototypeCityLayout.Bounds.Contains(focus),"Acompanhamento nos cantos deve manter foco dentro da periferia.");
            }
        foreach(float aspect in new[]{16f/9,4f/3})
        {
            float size=ObliqueProjection.FrameSize(8,PrototypeRuinsLayout.Bounds,aspect,true);
            var focus=ObliqueProjection.CameraFocus(PrototypeSecretLayout.Altar+Vector2.down,PrototypeRuinsLayout.Bounds,size,aspect,true);
            var flame=ObliqueProjection.ScreenPlane(new Vector2(PrototypeSecretLayout.Altar.x-focus.x,PrototypeSecretLayout.Altar.y-focus.y),1.32f);
            Assert(Math.Abs(flame.x)<size*aspect&&Math.Abs(flame.y)<size,"Câmera fixa das ruinas deve mostrar chama do altar.");
        }

        var world=ObliqueScenery.IslandData(); Validate(world,"Ilha e mar");
        // Area triangulada da superficie precisa corresponder ao contorno concavo.
        var inner=PrototypeWorldLayout.Coast.Select(p=>new Vector2(p.x*0.955f,p.y*0.955f)).ToArray();
        var surface=new ObliqueMeshData();surface.Polygon(inner,0,ObliqueMeshData.Hex("71806A"));
        double polygonArea=0,triangleArea=0;
        for(int i=0;i<inner.Length;i++) {var a=inner[i];var b=inner[(i+1)%inner.Length];polygonArea+=(double)a.x*b.y-(double)b.x*a.y;}
        for(int i=0;i<surface.Vertices.Count;i+=3)
        {var a=surface.Vertices[i];var b=surface.Vertices[i+1];var c=surface.Vertices[i+2];triangleArea+=Math.Abs((double)(b.x-a.x)*(c.y-a.y)-(double)(b.y-a.y)*(c.x-a.x));}
        Assert(Math.Abs(polygonArea-triangleArea)<0.001,"Triangulação deve preencher a ilha sem sobreposição nem buracos.");
        foreach(var block in PrototypeWorldLayout.Blocks)
        {
            if(block.Name=="Mar"||block.Name.StartsWith("Costa")||block.Name.StartsWith("Ilha -")) continue;
            var data=ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order);Validate(data,block.Name);AddBlock(world,block);
        }
        var castle=ObliqueScenery.CastleIconData();Validate(castle,"Castelo global");
        var forest=ObliqueScenery.ForestIconData();Validate(forest,"Bosque global");
        var volcano=ObliqueScenery.VolcanoIconData();Validate(volcano,"Vulcão global");
        var ruinsIcon=ObliqueScenery.RuinsIconData();Validate(ruinsIcon,"Ruinas globais");
        foreach(var icon in new[]{castle,forest})
            Assert(-icon.Vertices.Min(p=>p.z)>1.45f&&-icon.Vertices.Min(p=>p.z)<=2,"Ícones precisam ser ligeiramente mais altos que o herói.");
        world.Append(castle,ObliqueProjection.Point(PrototypeWorldLayout.Castle));
        world.Append(forest,ObliqueProjection.Point(PrototypeWorldLayout.WestForest));
        world.Append(forest,ObliqueProjection.Point(PrototypeWorldLayout.EastForest));
        world.Append(volcano,ObliqueProjection.Point(PrototypeWorldLayout.Volcano));
        world.Append(ruinsIcon,ObliqueProjection.Point(PrototypeWorldLayout.Ruins));
        Person(world,PrototypeWorldLayout.Spawn,"B4A078",true);
        MapDefinition worldMap=travel.Current;
        float worldSize=ObliqueProjection.FrameSize(worldMap.CameraSize,worldMap.Bounds,16f/9,worldMap.FixedCamera);
        Vector2 worldFocus=ObliqueProjection.CameraFocus(PrototypeWorldLayout.Spawn,worldMap.Bounds,worldSize,16f/9,worldMap.FixedCamera);
        Export(directory,"WorldOblique",world,worldFocus,worldSize,ObliqueProjection.HeroCenterHeight);

        var city=new ObliqueMeshData();
        foreach(var block in PrototypeCityLayout.Blocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(city,block);}
        foreach(var building in PrototypeCityLayout.Buildings)
        {
            var data=ObliqueScenery.BuildingData(building.Size,building.Color,building.Name.Contains("Castelo")||building.Name.Contains("Torre"));
            Validate(data,building.Name);city.Append(data,ObliqueProjection.Point(building.Position));
        }
        Validate(ObliqueScenery.PersonBodyData("B4A078",true),"Herói");
        foreach(float x in new[]{-0.12f,0.12f})Validate(ObliqueScenery.LegData(x,ObliqueMeshData.Hex("B4A078")),"Perna");
        Person(city,PrototypeCityLayout.Spawn,"B4A078",true);
        Person(city,PrototypeCityLayout.Citizen,"8D9DB0",false);Person(city,PrototypeCityLayout.Guard,"9B7770",true);
        Export(directory,"CityOblique",city,new Vector2(0,0),7.5f);
        var hall=new ObliqueMeshData();
        foreach(var block in PrototypeWorldLayout.CastleBlocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(hall,block);}
        Person(hall,PrototypeWorldLayout.CastleSpawn,"B4A078",true);
        var king=ObliqueScenery.KingData();var spirit=ObliqueScenery.SpiritData();
        Validate(king,"Tirano e coroa");Validate(ObliqueScenery.KingData(false),"Rei liberto");Validate(spirit,"Espirito maligno");
        Assert(king.Vertices.All(p=>p.z<=0),"Botas do rei nao podem ficar abaixo do piso.");
        hall.Append(king,ObliqueProjection.Point(PrototypeWorldLayout.Throne));
        hall.Append(spirit,ObliqueProjection.Point(PrototypeWorldLayout.Throne+Vector2.right*1.8f));
        Export(directory,"KingPortrait",king,Vector2.zero,1.1f,0.85f);
        Export(directory,"SpiritPortrait",spirit,Vector2.zero,1.4f,1.05f);
        Export(directory,"CastleOblique",hall,Vector2.zero,9);
        var ruins=new ObliqueMeshData();
        foreach(var block in PrototypeRuinsLayout.Blocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(ruins,block);}
        Person(ruins,PrototypeRuinsLayout.Spawn,"B4A078",true);
        Export(directory,"RuinsOblique",ruins,Vector2.zero,ObliqueProjection.FrameSize(8,PrototypeRuinsLayout.Bounds,16f/9,true));
        var dragon=ObliqueScenery.DragonData();Validate(dragon,"Dragao e asas");
        Export(directory,"DragonPortrait",dragon,new Vector2(0,0.5f),2.6f,1);
        var cavern=new ObliqueMeshData();
        foreach(var block in PrototypeVolcanoLayout.Blocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(cavern,block);}
        cavern.Append(dragon,ObliqueProjection.Point(PrototypeVolcanoLayout.Dragon));
        Person(cavern,new Vector2(0,5),"B4A078",true);
        Export(directory,"VolcanoOblique",cavern,new Vector2(0,5),7.5f,ObliqueProjection.HeroCenterHeight);
        var inn=new ObliqueMeshData();
        foreach(var block in PrototypeInnLayout.Blocks)
        {Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);AddBlock(inn,block);}
        Person(inn,PrototypeInnLayout.Spawn,"B4A078",true);Person(inn,PrototypeInnLayout.Keeper,"B89673",false);
        Export(directory,"InnOblique",inn,Vector2.zero,ObliqueProjection.FrameSize(6,PrototypeInnLayout.Bounds,16f/9,true));
        foreach(var ending in new[]{DragonQuest.Progression.GameEnding.KingFreed,DragonQuest.Progression.GameEnding.CastleDestroyed,DragonQuest.Progression.GameEnding.HeroPossessed})
        {
            var data=ObliqueScenery.EndingSceneData(ending);Validate(data,"Cena final / "+ending);
            Assert(data.Vertices.All(p=>p.z<=0),"Atores das cenas finais devem ficar sobre o piso.");
            Export(directory,"Ending"+ending,data,Vector2.zero,5.8f);
        }
        foreach(bool eastern in new[]{false,true})
            foreach(var block in PrototypeWorldLayout.CreateForestBlocks(eastern))Validate(ObliqueScenery.BlockData(block.Name,block.Size,block.Color,block.Solid,block.Order),block.Name);
        Validate(ObliqueScenery.BlockData("Balcao",new Vector2(4,0.6f),"614533",true,4),"Balcão");
        Validate(ObliqueScenery.BlockData("Estante",new Vector2(1.5f,3),"705033",true,3),"Estante");
        Console.WriteLine(checks+" verificacoes de perspectiva, camera e malhas passaram.");
    }
}
