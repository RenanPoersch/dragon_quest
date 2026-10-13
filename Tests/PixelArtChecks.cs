using System;
using DragonQuest.Exploration;
using DragonQuest.Prototype;
using UnityEngine;

internal static class PixelArtChecks
{
    private static int checks;
    private static void Assert(bool ok, string text) { checks++; if (!ok) throw new Exception(text); }
    public static void Run()
    {
        foreach (var input in new[] {Vector2.down, Vector2.left, Vector2.right, Vector2.up})
        {
            var expected = input.x < 0 ? PixelDirection.Left : input.x > 0 ? PixelDirection.Right : input.y > 0 ? PixelDirection.Up : PixelDirection.Down;
            Assert(PixelSpriteLayout.Facing(ObliqueProjection.GroundDirection(input)) == expected, "Sprite precisa seguir direcao da tela com yaw da camera.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 0, false) == (int)expected, "Parado usa idle da direcao correta.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 0.41f, true) == 4 + (int)expected, "Primeiro passo.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 1.21f, true) == 8 + (int)expected, "Segundo passo.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 2, false) == (int)expected, "Parar/teleportar volta ao idle.");
        }
        var directions = new[] {
            Vector2.down, Vector2.left, Vector2.right, Vector2.up,
            new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1)
        };
        for (int i = 0; i < directions.Length; i++)
        {
            var ground = ObliqueProjection.GroundDirection(directions[i]);
            var facing = PixelSpriteLayout.Facing(ground, true);
            Assert(facing == (PixelDirection)i, "Oito direcoes devem corresponder aos controles mesmo com a camera rodada.");
            Assert(PixelSpriteLayout.Facing(ground * 0.01f, true) == facing
                && PixelSpriteLayout.Facing(ground * 10f, true) == facing, "Velocidade nao altera a orientacao do sprite.");
            Assert(PixelSpriteLayout.IsDiagonal(facing) == (i >= 4), "Diagonais usam a folha complementar; cardinais mantem a original.");
            if (i >= 4) Assert(!PixelSpriteLayout.IsDiagonal(PixelSpriteLayout.Facing(ground)), "Folhas de quatro direcoes nao podem selecionar um sprite diagonal ausente.");
            int column = i % 4;
            int[] rows = {0, 1, 0, 2, 0};
            for (int step = 0; step < rows.Length; step++)
                Assert(PixelSpriteLayout.WalkFrame(facing, step * 0.4f + 0.01f, true) == rows[step] * 4 + column,
                    "Caminhada deve alternar repouso e os dois pes, sem sair da coluna ou ultrapassar a folha.");
            Assert(PixelSpriteLayout.WalkFrame(facing, 2, false) == column, "Parar ou teleportar conserva a direcao e usa seu repouso.");
        }
        float[] angles = {20, 25, 65, 70, -20, -25, -65, -70};
        PixelDirection[] sectors = {PixelDirection.Right, PixelDirection.UpRight, PixelDirection.UpRight, PixelDirection.Up,
            PixelDirection.Right, PixelDirection.DownRight, PixelDirection.DownRight, PixelDirection.Down};
        for (int i = 0; i < angles.Length; i++)
        {
            double radians = angles[i] * Math.PI / 180;
            var input = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
            Assert(PixelSpriteLayout.Facing(ObliqueProjection.GroundDirection(input), true) == sectors[i],
                "Setores diagonais devem ocupar 45 graus entre os eixos, sem transformar movimento quase horizontal/vertical em diagonal.");
        }
        Assert(PixelSpriteLayout.Facing(Vector2.zero, true) == PixelDirection.Down, "Direcao nula usa repouso cardinal valido.");
        CheckWalking();
        CheckMovementTurns();
        CheckChestFrames();
        CheckWorldIcons();
        var alpha = new byte[40 * 30];
        for (int i = 0; i < 12; i++)
        {
            int x = i % 4 * 10, y = (2 - i / 4) * 10;
            for (int yy = y + 2; yy <= y + 7; yy++) for (int xx = x + 3; xx <= x + 6; xx++) alpha[yy * 40 + xx] = 255;
        }
        var frames = PixelSpriteLayout.Read(40, 30, alpha);
        for (int i = 0; i < frames.Length; i++)
        {
            Assert(frames[i].X == i % 4 * 10 + 2 && frames[i].Y == (2 - i / 4) * 10 + 1, "Recortes devem ler PNG de baixo para cima e devolver ordem superior primeiro.");
            Assert(frames[i].Width == 6 && frames[i].Height == 8 && Math.Abs(frames[i].FootX - 0.5f) < 0.001f, "Recorte com margem e ancora nos pes.");
        }
        var oddAlpha = new byte[41 * 31];
        for (int i = 0; i < 12; i++)
        {
            int x = i % 4 * 41 / 4, y = 31 - (i / 4 + 1) * 31 / 3;
            for (int yy = y + 2; yy <= y + 7; yy++) for (int xx = x + 3; xx <= x + 6; xx++) oddAlpha[yy * 41 + xx] = 255;
        }
        var oddFrames = PixelSpriteLayout.Read(41, 31, oddAlpha);
        for (int i = 0; i < oddFrames.Length; i++)
            Assert(oddFrames[i].X == i % 4 * 41 / 4 + 2 && oddFrames[i].Y == 31 - (i / 4 + 1) * 31 / 3 + 1
                && oddFrames[i].Width == 6 && oddFrames[i].Height == 8,
                "Tamanhos nao divisiveis devem distribuir os pixels restantes e conservar corpos inteiros em todas as celulas.");
        oddAlpha[30 * 41 + 40] = 255;
        bool refusedOdd = false;
        try { PixelSpriteLayout.Read(41, 31, oddAlpha); } catch (ArgumentException) { refusedOdd = true; }
        Assert(refusedOdd, "Ultimo pixel da imagem tambem precisa ser verificado ao recortar a grade.");
        alpha[20 * 40] = 255;
        bool refused = false; try { PixelSpriteLayout.Read(40, 30, alpha); } catch (ArgumentException) { refused = true; }
        Assert(refused, "Sprite que toca a borda nao pode ser recortado silenciosamente.");
        refused = false; try { PixelSpriteLayout.Read(40, 30, new byte[1200]); } catch (ArgumentException) { refused = true; }
        Assert(refused, "Atlas vazio recusado.");
        var floorUV = PixelSurfaces.Project(new Vector3(4, 6, -8), new Vector3(0, 0, 1));
        var wallUV = PixelSurfaces.Project(new Vector3(4, 6, -8), new Vector3(0, 1, 0));
        Assert(floorUV.x == 2 && floorUV.y == 3 && wallUV.x == 2 && wallUV.y == 4, "Chao usa XY, paredes usam altura sem esticar verticalmente.");
        for (int i = 0; i < 12; i++)
        {
            Rect cell = PixelSurfaces.AtlasCell((PixelSurface)i);
            Assert(cell.width == 0.25f && Math.Abs(cell.height - 1f / 3) < 0.0001f && cell.x >= 0 && cell.y >= 0 && cell.xMax <= 1 && cell.yMax <= 1,
                "Materiais ficam dentro de sua celula e respeitam ordem superior.");
        }
        Assert(PixelSurfaces.ForBlock("Trilha de basalto", false, -7) == PixelSurface.Basalt, "Trilha do vulcao continua com aparencia de basalto.");
        Assert(PixelSurfaces.ForBlock("Piso da hospedaria", false, -10) == PixelSurface.Wood, "Hospedaria usa madeira.");
        var source = new ObliqueMeshData {DefaultSurface = PixelSurface.Paving}; source.Plane(0, 0, 2, 2, 0, Color.white);
        var combined = new ObliqueMeshData(); combined.Append(source, new Vector3(4, 5, 0));
        Assert(combined.UVs.Count == combined.Vertices.Count && combined.Surfaces.Count == combined.Vertices.Count
            && combined.Surfaces[0].x == (int)PixelSurface.Paving, "Composicao preserva material e coordenadas da textura.");
        var shadow = new ObliqueMeshData {DefaultSurface=PixelSurface.Roof}; shadow.Shadow(0,0,1,1);
        Assert(shadow.Colors[0].a > 0 && shadow.Colors[0].a < 1 && shadow.Surfaces[0].x == -1, "Sombra deve ser transparente e nao herdar textura do telhado.");
        Console.WriteLine(checks + " verificacoes de sprites, materiais e movimento passaram.");
    }
    private static void CheckWorldIcons()
    {
        var alpha = new byte[1536 * 1024];
        for (int i = 0; i < 5; i++)
        {
            RectInt region = PixelSpriteLayout.WorldIconRegion(i);
            int x = region.xMin + region.width / 2, y = 1024 - region.yMin - region.height / 2;
            for (int yy = y - 12; yy <= y + 12; yy++) for (int xx = x - 10; xx <= x + 10; xx++) alpha[yy * 1536 + xx] = 253;
        }
        var frames = PixelSpriteLayout.ReadWorldIcons(1536,1024,alpha);
        Assert(frames.Length==5,"Folha de POIs deve fornecer os cinco locais, sem uma sexta imagem vazia.");
        foreach (var frame in frames)
            Assert(frame.Width==23&&frame.Height==27&&frame.FootX==0.5f&&frame.Pivot.y==1f/27,
                "POIs conservam margem, ancora central e base apoiada no chao.");
        bool refused = false;
        try { PixelSpriteLayout.ReadWorldIcons(1536,1024,new byte[1536*1024]); } catch (ArgumentException) { refused=true; }
        Assert(refused,"POI ausente deve ser recusado.");
        alpha[900*1536]=253; refused=false;
        try { PixelSpriteLayout.ReadWorldIcons(1536,1024,alpha); } catch (ArgumentException) { refused=true; }
        Assert(refused,"POI encostado no limite da janela nao pode ser cortado silenciosamente.");
    }
    private static void CheckMovementTurns()
    {
        var directions=new[]{Vector2.down,Vector2.left,Vector2.right,Vector2.up,
            new Vector2(-1,-1).normalized,new Vector2(1,-1).normalized,
            new Vector2(-1,1).normalized,new Vector2(1,1).normalized};
        foreach(float dt in new[]{1f/30,1f/50,1f/120}) foreach(var start in directions) foreach(var finish in directions)
        {
            Vector2 current=ObliqueProjection.GroundDirection(start)*4;
            Vector2 target=ObliqueProjection.GroundDirection(finish)*4;
            float maxSpeed=0,maxChange=0;
            for(int step=0;step<(int)Math.Ceiling(0.14f/dt);step++)
            {
                Vector2 next=MovementDynamics.StepVelocity(current,target,dt);
                maxChange=Math.Max(maxChange,Vector2.Distance(current,next));
                maxSpeed=Math.Max(maxSpeed,next.magnitude);
                current=next;
            }
            Assert(maxChange<=80*dt+0.0001f,"Viradas devem ter variacao limitada de velocidade, em vez de salto instantaneo.");
            Assert(maxSpeed<=4.0001f&&Vector2.Distance(current,target)<0.0001f,
                "Todas as transicoes cardinais/diagonais devem atingir a velocidade normal sem aceleracao extra ou oscilacao.");
        }
        foreach(float dt in new[]{1f/30,1f/50,1f/120})
        {
            Vector2 current=Vector2.zero;
            for(int i=0;i<(int)Math.Ceiling(0.06f/dt);i++) current=MovementDynamics.StepVelocity(current,Vector2.right*4,dt);
            Assert(current==Vector2.right*4,"Partida deve atingir a velocidade normal em aproximadamente 0,06 s.");
            for(int i=0;i<(int)Math.Ceiling(0.05f/dt);i++) current=MovementDynamics.StepVelocity(current,Vector2.zero,dt);
            Assert(current==Vector2.zero,"Soltar as teclas deve encerrar o deslocamento em aproximadamente 0,05 s.");
            Assert(MovementDynamics.StepVelocity(Vector2.right*4,Vector2.right*4,dt)==Vector2.right*4,
                "Caminhada continua deve conservar a mesma velocidade, sem suavizacao da camera.");
        }
        Assert(MovementDynamics.StepVelocity(Vector2.right*4,Vector2.up*4,0)==Vector2.right*4,
            "Quadro sem passagem de tempo nao deve mudar a velocidade.");
    }
    private static void CheckChestFrames()
    {
        var alpha=new byte[60*36];
        for(int i=0;i<2;i++) for(int y=4+i*2;y<=17+i*2;y++) for(int x=6+i*30;x<=23+i*30;x++) alpha[y*60+x]=255;
        for(int y=18;y<=23;y++) for(int x=6;x<=23;x++) alpha[y*60+x]=255;
        for(int y=22;y<=32;y++) for(int x=39;x<=56;x++) alpha[y*60+x]=255;
        var frames=PixelSpriteLayout.ReadPropPair(60,36,alpha);
        Assert(frames.Length==2&&frames[1].Height>frames[0].Height,"Bau precisa de estados fechado e aberto, com tampa inteira.");
        Assert(Math.Abs(frames[0].X+frames[0].Width*frames[0].FootX-15)<0.001f
            &&Math.Abs(frames[1].X+frames[1].Width*frames[1].FootX-45)<0.001f,
            "A tampa aberta mais larga nao pode deslocar o centro da caixa.");
        Assert(frames[0].Y!=frames[1].Y&&Math.Abs(frames[0].Pivot.y*frames[0].Height-1)<0.001f
            &&Math.Abs(frames[1].Pivot.y*frames[1].Height-1)<0.001f,
            "Recortes com baselines diferentes na folha devem apoiar a base na mesma ancora do mundo.");
        bool refused=false;try{PixelSpriteLayout.ReadPropPair(60,36,new byte[60*36]);}catch(ArgumentException){refused=true;}
        Assert(refused,"Folha de bau vazia precisa ser recusada.");
    }
    private static void CheckWalking()
    {
        // Tronco fixo e pes alternados: o recorte muda de largura, mas o corpo nao deve saltar de lado.
        var alpha=new byte[120*90];
        for(int i=0;i<12;i++)
        {
            int x=i%4*30,y=(2-i/4)*30;
            for(int yy=7;yy<=26;yy++) for(int xx=10;xx<=17;xx++) alpha[(y+yy)*120+x+xx]=255;
            int boot=i/4==0?14:i/4==1?6:21;
            for(int yy=2;yy<=6;yy++) for(int xx=boot;xx<boot+4;xx++) alpha[(y+yy)*120+x+xx]=255;
        }
        var oldFrames=PixelSpriteLayout.Read(120,90,alpha);
        var frames=PixelSpriteLayout.Read(120,90,alpha,true);
        Assert(Math.Abs((oldFrames[0].X+oldFrames[0].FootX*oldFrames[0].Width)
            -(oldFrames[4].X+oldFrames[4].FootX*oldFrames[4].Width))>5,"Fixture deve reproduzir o salto causado pelo pe em movimento.");
        for(int i=0;i<12;i++)
        {
            Assert(Math.Abs(frames[i].X+frames[i].FootX*frames[i].Width-(i%4*30+14))<0.0001f,
                "Corpo deve permanecer sobre a ancora nas tres poses, inclusive diagonais.");
            Assert(frames[i].Rect==oldFrames[i].Rect,"Estabilizar a ancora nao pode cortar pes, capa ou alterar o recorte.");
        }
        var walk=new PixelWalkCycle();
        walk.Advance(0.41f,0.1f,true,false);
        int step=PixelSpriteLayout.WalkFrame(PixelDirection.UpRight,walk.Distance,walk.Walking);
        Assert(step==7,"Caminhada diagonal deve iniciar o primeiro passo no ritmo novo.");
        walk.Advance(0,1f/144,true,false);
        Assert(walk.Walking&&PixelSpriteLayout.WalkFrame(PixelDirection.UpRight,walk.Distance,true)==step,
            "Um quadro sem deslocamento da fisica nao pode reiniciar a pose.");
        walk.Advance(0,0.12f,true,false);
        Assert(!walk.Walking&&walk.Distance==0,"Encostar numa parede deve parar a animacao em repouso.");
        walk.Advance(0.41f,0.1f,true,false);
        walk.Advance(0.02f,1f/60,false,false);
        Assert(!walk.Walking&&walk.Distance==0,"Soltar as teclas deve parar imediatamente, mesmo durante interpolacao.");
        walk.Advance(0.41f,0.1f,true,false);
        walk.Advance(0.05f,1f/60,true,true);
        Assert(!walk.Walking&&walk.Distance==0,"Teleportes curtos tambem devem reiniciar o passo sem gerar caminhada.");
        walk.Advance(0.65f,0.16f,true,false);
        Assert(walk.Walking,"Deslocamento real em um quadro demorado nao deve ser confundido com teleporte.");
        foreach(int fps in new[]{30,60,144})
        {
            var cycle=new PixelWalkCycle();int changes=0,last=3;
            for(int frame=0;frame<fps;frame++)
            {
                cycle.Advance(4f/fps,1f/fps,true,false);
                int pose=PixelSpriteLayout.WalkFrame(PixelDirection.UpRight,cycle.Distance,cycle.Walking);
                if(pose!=last) changes++;
                last=pose;
            }
            Assert(changes>=9&&changes<=10,"A velocidade inicial deve trocar cerca de dez poses por segundo, independente do FPS.");
        }
        foreach(int fps in new[]{60,144})
        {
            var continuous=new PixelWalkCycle();var sampled=new PixelWalkCycle();
            for(int frame=0;frame<fps;frame++)
            {
                continuous.Advance(4f/fps,1f/fps,true,false);
                sampled.Advance(frame%2==0?8f/fps:0,1f/fps,true,false);
            }
            Assert(sampled.Walking&&Math.Abs(continuous.Distance-sampled.Distance)<0.0001f,
                "Amostras intercaladas com quadros parados devem conservar o ciclo de caminhada.");
        }
    }
}
