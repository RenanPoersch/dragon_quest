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
            Assert(PixelSpriteLayout.WalkFrame(expected, 0.23f, true) == 4 + (int)expected, "Primeiro passo.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 0.67f, true) == 8 + (int)expected, "Segundo passo.");
            Assert(PixelSpriteLayout.WalkFrame(expected, 2, false) == (int)expected, "Parar/teleportar volta ao idle.");
        }
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
        Console.WriteLine(checks + " verificacoes de sprites e materiais passaram.");
    }
}
