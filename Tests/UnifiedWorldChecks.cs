using System;
using System.Linq;
using DragonQuest.Exploration;
using DragonQuest.Prototype;
using UnityEngine;

internal static class UnifiedWorldChecks
{
    static int checks;
    static void Check(bool condition,string message) { checks++; if(!condition) throw new Exception(message); }
    public static void Main()
    {
        var ground=WorldIslandArt.Ground();
        Check(ground.Vertices.Count==6 && ground.Triangles.Count==6,"Mapa inteiro sobre um unico plano de dois triangulos");
        Check(ground.Vertices.All(p=>p.z==0),"Sem volumes ou faixas costeiras 3D");
        Check(ground.Colors.All(c=>c==Color.white),"Imagem sem tintas independentes por bioma");
        var bounds=PrototypeWorldLayout.GroundPaintingBounds;
        for(int i=0;i<ground.Vertices.Count;i++)
        {
            var p=ground.Vertices[i]; var uv=ground.UVs[i];
            Check(Math.Abs(uv.x-(p.x-bounds.xMin)/bounds.width)<.0001f,"UV horizontal de imagem unica");
            Check(Math.Abs(uv.y-(p.y-bounds.yMin)/bounds.height)<.0001f,"UV vertical de imagem unica");
        }
        foreach(var p in new[]{PrototypeWorldLayout.Spawn,PrototypeWorldLayout.Castle,PrototypeWorldLayout.WestForest,PrototypeWorldLayout.EastForest,PrototypeWorldLayout.Ruins,PrototypeWorldLayout.Volcano,PrototypeWorldLayout.Trail})
        {
            Check(PrototypeWorldLayout.IsLand(p),"POIs, trilha e nascimento permanecem em terra");
            Check(bounds.Contains(p),"POIs dentro da imagem pintada");
        }
        var rear=ObliqueProjection.GroundDirection(new Vector2(.22f,.9f));
        var foot=new Vector2(ObliqueProjection.YawSin*.3f,-ObliqueProjection.YawCos*.3f);
        for(int frame=0;frame<5;frame++)
        {
            var shadow=ObliqueScenery.WorldPoiShadowData(frame);
            Check(shadow.Vertices.Count==24,"Caixa de sombra preservada");
            Check(shadow.Vertices.All(p=>(p.x-foot.x)*rear.x+(p.y-foot.y)*rear.y>0),"Sombra apenas atras e direita");
        }
        Console.WriteLine(checks+" verificacoes do mapa continuo e POIs passaram.");
    }
}
