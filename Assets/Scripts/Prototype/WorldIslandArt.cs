using System;
using System.Linq;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class WorldIslandArt
    {
        public static ObliqueMeshData Ground()
        {
            var mesh = new ObliqueMeshData();
            mesh.Plane(0, 0, 80, 65, 0, Color.white);
            Rect bounds = PrototypeWorldLayout.GroundPaintingBounds;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                mesh.Colors[i] = Color.white;
                mesh.UVs[i] = new Vector2((mesh.Vertices[i].x - bounds.xMin) / bounds.width,
                    (mesh.Vertices[i].y - bounds.yMin) / bounds.height);
            }
            return mesh;
        }

        public static ObliqueMeshData Ward()
        {
            var mesh = new ObliqueMeshData();
            const int segments = 48;
            float radius = PrototypeWorldLayout.WardRadius;
            for (int i = 0; i < segments; i++)
            {
                double a = i * Math.PI * 2 / segments, b = (i + 1) * Math.PI * 2 / segments;
                float ax = (float)Math.Cos(a), ay = (float)Math.Sin(a), bx = (float)Math.Cos(b), by = (float)Math.Sin(b);
                // Faixas discretas, sem luz dinamica ou textura de baixa resolucao ampliada.
                for (int band = 0; band < 6; band++)
                {
                    float bottom = 0.05f + band * 0.29f, top = bottom + 0.29f;
                    var color = new Color(0.48f, 0.3f, 0.78f, 0.075f - band * 0.009f);
                    mesh.Quad(ObliqueMeshData.V(ax * radius, ay * radius, bottom), ObliqueMeshData.V(ax * radius, ay * radius, top),
                        ObliqueMeshData.V(bx * radius, by * radius, top), ObliqueMeshData.V(bx * radius, by * radius, bottom), color, false);
                }
                foreach (float height in new[] { 0.055f, 0.82f, 1.72f })
                    mesh.Quad(ObliqueMeshData.V(ax * radius, ay * radius, height), ObliqueMeshData.V(ax * radius, ay * radius, height + 0.035f),
                        ObliqueMeshData.V(bx * radius, by * radius, height + 0.035f), ObliqueMeshData.V(bx * radius, by * radius, height),
                        new Color(0.57f, 0.7f, 0.95f, height < 0.1f ? 0.8f : 0.38f), false);
                if (i % 4 != 0) continue;
                // Pequenos fragmentos luminosos lembram runas, sem placas ou labels.
                for (int step = 0; step < 4; step++)
                {
                    double angle = a + (step % 2 == 0 ? 0 : Math.PI / segments);
                    double end = angle + Math.PI / segments;
                    float bottom = 0.45f + step * 0.13f;
                    mesh.Quad(ObliqueMeshData.V((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, bottom),
                        ObliqueMeshData.V((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, bottom + 0.07f),
                        ObliqueMeshData.V((float)Math.Cos(end) * radius, (float)Math.Sin(end) * radius, bottom + 0.07f),
                        ObliqueMeshData.V((float)Math.Cos(end) * radius, (float)Math.Sin(end) * radius, bottom),
                        new Color(0.7f, 0.81f, 1, 0.55f), false);
                }
            }
            return mesh;
        }
    }
}
