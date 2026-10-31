using DragonQuest.Presentation.World.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.World.Scenery
{
    // Pintura integral com contornos de profundidade e bases fisicas registrados em UV.
    public abstract class PaintedInteriorScenery : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        protected void BuildPainting(string name,string resource,float halfHeight,float aspect,
            Vector2[][] foregrounds,Vector2[] anchors,Vector2[][] fixtures,PhysicsMaterial2D physics)
        {
            var painting = Resources.Load<Texture2D>("PixelArt/"+resource);
            var backdropShader = Resources.Load<Shader>("ForestPixelBackdrop");
            var depthShader = Resources.Load<Shader>("PixelCharacter");
            if (painting == null || backdropShader == null || depthShader == null)
                throw new InvalidOperationException("Pintura completa ausente: "+name);
            var backdrop = new Material(backdropShader) { name = name+" / pintura integral",mainTexture = painting };
            var depth = new Material(depthShader) { name = name+" / profundidade da pintura",mainTexture = painting };
            owned.Add(backdrop); owned.Add(depth);
            Quaternion rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees,Vector3.forward)*Quaternion.Euler(-ObliqueProjection.TiltDegrees,0,0);
            Vector2[] corners = { Vector2.zero,Vector2.up,Vector2.one,Vector2.right };
            var vertices = new Vector3[4];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = Screen(corners[i],halfHeight,aspect);
                vertices[i] = rotation*new Vector3(screen.x,screen.y,20);
            }
            Draw(name+" / cenario completo pintado",vertices,corners,new[] { 0,1,2,0,2,3 },backdrop);
            for (int i = 0; i < foregrounds.Length; i++)
            {
                Vector2[] outline = foregrounds[i];
                Vector2 ground = Ground(anchors[i],halfHeight,aspect), foot = Screen(anchors[i],halfHeight,aspect);
                Vector3 right = new Vector3(ObliqueProjection.YawCos,ObliqueProjection.YawSin,0);
                var points = new Vector3[outline.Length];
                for (int j = 0; j < outline.Length; j++)
                {
                    Vector2 p = Screen(outline[j],halfHeight,aspect);
                    points[j] = (Vector3)ground+right*(p.x-foot.x)+new Vector3(0,0,-(p.y-foot.y)/ObliqueProjection.HeightScale);
                }
                Draw(name+" / recorte de profundidade "+i,points,outline,Triangulate(outline),depth);
            }
            for (int i = 0; i < fixtures.Length; i++)
            {
                var fixture = new GameObject(name+" / base do movel "+i); fixture.transform.SetParent(transform,false);
                var points = new Vector2[fixtures[i].Length];
                for (int j = 0; j < points.Length; j++) points[j] = Ground(fixtures[i][j],halfHeight,aspect);
                var collider = fixture.AddComponent<PolygonCollider2D>(); collider.points = points; collider.sharedMaterial = physics;
            }
        }
        private static Vector2 Screen(Vector2 uv,float halfHeight,float aspect) => new Vector2((uv.x-.5f)*halfHeight*2*aspect,(.5f-uv.y)*halfHeight*2);
        private static Vector2 Ground(Vector2 uv,float halfHeight,float aspect)
        {
            Vector2 screen = Screen(uv,halfHeight,aspect);
            return ObliqueProjection.GroundDirection(new Vector2(screen.x,screen.y/ObliqueProjection.GroundScale));
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
                if (!clipped) throw new InvalidOperationException("Contorno de profundidade invalido na pintura.");
            }
            triangles.AddRange(remaining); return triangles.ToArray();
        }
        private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        protected virtual void OnDestroy()
        {
            for (int i = owned.Count-1; i >= 0; i--)
                if (owned[i] != null) { if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
        }
    }
}
