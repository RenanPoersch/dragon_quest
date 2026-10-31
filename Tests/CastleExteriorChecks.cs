using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Animations;
using DragonQuest.Exploration.Layouts;
using System;
using System.Linq;
using UnityEngine;

internal static class CastleExteriorChecks
{
    private static int checks;
    private static void Assert(bool condition, string text) { checks++; if (!condition) throw new Exception(text); }
    public static void Main()
    {
        var oldButtress = new ObliqueMeshData();
        oldButtress.Box(0, 0, 2, 1, 0, 2, Color.white);
        oldButtress.Box(0, -.45f, .5f, .1f, 0, 1.5f, Color.white);
        Assert(CoplanarOverlap(oldButtress, 6, 36) > .01, "O detector reproduz o contraforte antigo coplanar com a fachada.");
        Assert(CoplanarOverlap(oldButtress, 0, 3) < .000005, "Uma diagonal compartilhada nao conta como sobreposicao.");
        CheckCastleMasonry();
        foreach (float time in new[] {0f, .11f, .79f, .8f, 10.43f, -1f, float.NaN, float.PositiveInfinity})
            Assert(RuinsFlameAnimation.FrameAt(time) >= 0 && RuinsFlameAnimation.FrameAt(time) < 8, "Quadro de chama valido.");
        Assert(RuinsFlameAnimation.FrameAt(.11f) == 1 && RuinsFlameAnimation.FrameAt(.8f) == 0, "Chama avanca e repete.");
        Console.WriteLine(checks + " verificacoes do exterior do castelo passaram: nenhuma face opaca sobreposta nas tres construcoes e ciclo da chama preservado.");
    }
    private static void CheckCastleMasonry()
    {
        foreach (var building in CityLayout.Buildings.Where(b => CityPeripheryArt.IsRoyalCastle(b.Name)))
        {
            var mesh = CastleFortressArt.Building(building.Name, building.Size).Body;
            Assert(mesh.Vertices.Count == mesh.UVs.Count && mesh.Vertices.Count == mesh.Surfaces.Count, "Buffers completos: " + building.Name);
            double maxOverlap = 0; int worstI = -1, worstJ = -1;
            for (int i = 0; i < mesh.Triangles.Count; i += 3)
                for (int j = i + 3; j < mesh.Triangles.Count; j += 3)
                {
                    if (mesh.Colors[mesh.Triangles[i]].a < 1 || mesh.Colors[mesh.Triangles[j]].a < 1) continue;
                    double overlap = CoplanarOverlap(mesh, i, j);
                    if (overlap > maxOverlap) { maxOverlap = overlap; worstI = i / 3; worstJ = j / 3; }
                }
            Assert(maxOverlap < .000005, building.Name + ": faces sobrepostas causam z-fighting (triangulos "
                + worstI + ", " + worstJ + ", area " + maxOverlap + ").");
        }
    }

    // Clip coplanar triangles in their dominant 2D plane; touching edges have zero area.
    private static double CoplanarOverlap(ObliqueMeshData mesh, int i, int j)
    {
        var a = mesh.Vertices[mesh.Triangles[i]];
        var b = mesh.Vertices[mesh.Triangles[i + 1]];
        var c = mesh.Vertices[mesh.Triangles[i + 2]];
        var normal = Cross(b - a, c - a);
        float length = (float)Math.Sqrt(Dot(normal, normal));
        if (length < .000001f) return 0;
        normal /= length;
        var clip = new Vector3[3];
        for (int k = 0; k < 3; k++)
        {
            clip[k] = mesh.Vertices[mesh.Triangles[j + k]];
            if (Math.Abs(Dot(clip[k] - a, normal)) > .00001f) return 0;
        }
        int axis = Math.Abs(normal.x) > Math.Abs(normal.y) && Math.Abs(normal.x) > Math.Abs(normal.z) ? 0
            : Math.Abs(normal.y) > Math.Abs(normal.z) ? 1 : 2;
        Vector2 Project(Vector3 p) => axis == 0 ? new Vector2(p.y, p.z) : axis == 1 ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
        double Side(Vector2 u, Vector2 v, Vector2 p) => (double)(v.x - u.x) * (p.y - u.y) - (double)(v.y - u.y) * (p.x - u.x);
        var points = new System.Collections.Generic.List<Vector2> {Project(a), Project(b), Project(c)};
        var edges = clip.Select(Project).ToArray();
        double winding = Math.Sign(Side(edges[0], edges[1], edges[2]));
        for (int edge = 0; edge < 3 && points.Count > 0; edge++)
        {
            var input = points; points = new System.Collections.Generic.List<Vector2>();
            var u = edges[edge]; var v = edges[(edge + 1) % 3];
            var previous = input[input.Count - 1]; double before = Side(u, v, previous) * winding;
            foreach (var current in input)
            {
                double after = Side(u, v, current) * winding;
                if ((before >= 0) != (after >= 0)) points.Add(previous + (current - previous) * (float)(before / (before - after)));
                if (after >= 0) points.Add(current);
                previous = current; before = after;
            }
        }
        double area = 0;
        for (int k = 0; k < points.Count; k++)
        {
            var p = points[k]; var q = points[(k + 1) % points.Count]; area += (double)p.x * q.y - (double)p.y * q.x;
        }
        return Math.Abs(area) / 2;
    }

    private static float Dot(Vector3 a, Vector3 b) => a.x*b.x + a.y*b.y + a.z*b.z;
    private static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y*b.z-a.z*b.y, a.z*b.x-a.x*b.z, a.x*b.y-a.y*b.x);
}