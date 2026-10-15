using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Uma unica pintura completa. Os recortes de profundidade reutilizam os mesmos pixels.
    public sealed class InnPixelScenery : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        public static readonly Vector2[][] Foregrounds = {
            new[] { new Vector2(.589f,.589f),new Vector2(.611f,.576f),new Vector2(.65f,.585f),new Vector2(.68f,.600f),
                new Vector2(.84f,.600f),new Vector2(.85f,.58f),new Vector2(.89f,.58f),new Vector2(.915f,.63f),
                new Vector2(.91f,.776f),new Vector2(.603f,.776f),new Vector2(.588f,.73f) },
            new[] { new Vector2(.099f,.57f),new Vector2(.112f,.548f),new Vector2(.123f,.545f),new Vector2(.13f,.575f),
                new Vector2(.145f,.558f),new Vector2(.164f,.54f),new Vector2(.17f,.505f),new Vector2(.18f,.495f),
                new Vector2(.188f,.50f),new Vector2(.193f,.529f),new Vector2(.21f,.535f),new Vector2(.227f,.555f),
                new Vector2(.258f,.585f),new Vector2(.282f,.56f),new Vector2(.297f,.565f),new Vector2(.297f,.602f),
                new Vector2(.287f,.626f),new Vector2(.291f,.677f),new Vector2(.279f,.708f),new Vector2(.266f,.703f),
                new Vector2(.257f,.657f),new Vector2(.239f,.65f),new Vector2(.211f,.674f),new Vector2(.212f,.710f),
                new Vector2(.194f,.713f),new Vector2(.192f,.678f),new Vector2(.158f,.663f),new Vector2(.145f,.697f),
                new Vector2(.13f,.705f),new Vector2(.12f,.693f),new Vector2(.124f,.655f),new Vector2(.107f,.63f) },
            new[] { new Vector2(.50f,.325f),new Vector2(.516f,.32f),new Vector2(.545f,.346f),new Vector2(.591f,.346f),
                new Vector2(.591f,.318f),new Vector2(.611f,.318f),new Vector2(.615f,.474f),new Vector2(.499f,.474f) },
            new[] { new Vector2(.85f,.325f),new Vector2(.872f,.325f),new Vector2(.872f,.345f),new Vector2(.951f,.345f),
                new Vector2(.953f,.475f),new Vector2(.85f,.475f) }
        };
        private static readonly Vector2[] Anchors = { new Vector2(.752f,.765f),new Vector2(.20f,.70f),new Vector2(.555f,.468f),new Vector2(.902f,.468f) };

        public void Build(PhysicsMaterial2D physics = null)
        {
            var painting = Resources.Load<Texture2D>("PixelArt/InnInterior");
            var backdropShader = Resources.Load<Shader>("ForestPixelBackdrop");
            var depthShader = Resources.Load<Shader>("PixelCharacter");
            if (painting == null || backdropShader == null || depthShader == null)
                throw new InvalidOperationException("Pintura completa da Pousada ausente.");
            var backdrop = new Material(backdropShader) { name = "Pousada / pintura integral em pixel art", mainTexture = painting };
            var depth = new Material(depthShader) { name = "Pousada / profundidade da mesma pintura", mainTexture = painting };
            owned.Add(backdrop); owned.Add(depth);
            Quaternion rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees,Vector3.forward) * Quaternion.Euler(-ObliqueProjection.TiltDegrees,0,0);
            Vector2[] corners = { Vector2.zero,Vector2.up,Vector2.one,Vector2.right };
            var vertices = new Vector3[4];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = PrototypeInnLayout.ScreenPoint(corners[i]);
                vertices[i] = rotation * new Vector3(screen.x,screen.y,20);
            }
            Draw("Pousada / cenario completo pintado", vertices, corners, new[] { 0,1,2,0,2,3 }, backdrop);
            for (int i = 0; i < Foregrounds.Length; i++)
            {
                Vector2[] outline = Foregrounds[i];
                Vector2 ground = PrototypeInnLayout.Ground(Anchors[i].x,Anchors[i].y);
                Vector2 foot = PrototypeInnLayout.ScreenPoint(Anchors[i]);
                Vector3 right = new Vector3(ObliqueProjection.YawCos,ObliqueProjection.YawSin,0);
                var points = new Vector3[outline.Length];
                for (int j = 0; j < outline.Length; j++)
                {
                    Vector2 p = PrototypeInnLayout.ScreenPoint(outline[j]);
                    points[j] = (Vector3)ground + right * (p.x-foot.x) + new Vector3(0,0,-(p.y-foot.y)/ObliqueProjection.HeightScale);
                }
                Draw("Pousada / recorte de profundidade " + i,points,outline,Triangulate(outline),depth);
            }
            // Poligonos no chao acompanham exatamente as bases pintadas.
            for (int i = 0; i < PrototypeInnLayout.Fixtures.Length; i++)
            {
                var fixture = new GameObject(i == 0 ? "Base da mesa e cadeiras" : "Base do balcao");
                fixture.transform.SetParent(transform,false);
                var points = new Vector2[PrototypeInnLayout.Fixtures[i].Length];
                for (int j = 0; j < points.Length; j++) { Vector2 uv = PrototypeInnLayout.Fixtures[i][j]; points[j] = PrototypeInnLayout.Ground(uv.x,uv.y); }
                var collider = fixture.AddComponent<PolygonCollider2D>(); collider.points = points; collider.sharedMaterial = physics;
            }
        }

        private void Draw(string name, Vector3[] vertices, Vector2[] image, int[] triangles, Material material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform,false);
            var uv = new Vector2[image.Length]; var colors = new Color[image.Length];
            for (int i = 0; i < uv.Length; i++) { uv[i] = new Vector2(image[i].x,1-image[i].y); colors[i] = Color.white; }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, colors = colors, triangles = triangles };
            mesh.RecalculateBounds(); owned.Add(mesh);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        // Ear clipping permite seguir contornos concavos sem cobrir os vazios ao redor dos moveis.
        public static int[] Triangulate(Vector2[] points)
        {
            var remaining = new List<int>(); var triangles = new List<int>(); float area = 0;
            for (int i = 0; i < points.Length; i++) { remaining.Add(i); Vector2 a=points[i],b=points[(i+1)%points.Length]; area += a.x*b.y-b.x*a.y; }
            float sign = area > 0 ? 1 : -1;
            while (remaining.Count > 3)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    if (Cross(points[a],points[b],points[c])*sign <= .0000001f) continue;
                    bool occupied = false;
                    foreach (int index in remaining)
                    {
                        if (index==a || index==b || index==c) continue;
                        Vector2 p=points[index];
                        if (Cross(points[a],points[b],p)*sign >= 0 && Cross(points[b],points[c],p)*sign >= 0 && Cross(points[c],points[a],p)*sign >= 0) { occupied=true; break; }
                    }
                    if (occupied) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c); remaining.RemoveAt(i); clipped=true; break;
                }
                if (!clipped) throw new InvalidOperationException("Contorno de profundidade invalido na Pousada.");
            }
            triangles.AddRange(remaining); return triangles.ToArray();
        }
        private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        private void OnDestroy()
        {
            for (int i = owned.Count-1; i >= 0; i--)
                if (owned[i] != null) { if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
        }
    }
}
