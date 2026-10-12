using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Dados de malha independentes do renderer, tambem usados na verificacao e na previa.
    public sealed class ObliqueMeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Color> Colors = new List<Color>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<Vector2> Surfaces = new List<Vector2>();
        public PixelSurface DefaultSurface { get; set; } = PixelSurface.None;

        public static Vector3 V(float x, float y, float height) => new Vector3(x, y, -height);
        public static Color Hex(string hex)
        {
            int rgb = Convert.ToInt32(hex, 16);
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1);
        }

        public void Append(ObliqueMeshData data, Vector3 offset)
        {
            int start=Vertices.Count;
            foreach(Vector3 p in data.Vertices) Vertices.Add(new Vector3(p.x+offset.x,p.y+offset.y,p.z+offset.z));
            Colors.AddRange(data.Colors);
            UVs.AddRange(data.UVs); Surfaces.AddRange(data.Surfaces);
            foreach(int index in data.Triangles) Triangles.Add(start+index);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, bool shade = true, PixelSurface surface = PixelSurface.Auto)
        {
            float ux = b.x - a.x, uy = b.y - a.y, uz = b.z - a.z;
            float vx = c.x - a.x, vy = c.y - a.y, vz = c.z - a.z;
            float nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
            float length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            float light = !shade || length < 0.00001f ? 1 : 0.62f + 0.38f * Math.Max(0, (-nx * 0.45f - ny * 0.35f - nz * 0.82f) / length);
            if (shade)
            {
                color = new Color(color.r * light, color.g * light, color.b * light, color.a);
            }
            if (surface == PixelSurface.Auto) surface = DefaultSurface;
            int index = Vertices.Count;
            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
            Colors.Add(color); Colors.Add(color); Colors.Add(color);
            var normal = new Vector3(nx, ny, nz);
            UVs.Add(PixelSurfaces.Project(a, normal)); UVs.Add(PixelSurfaces.Project(b, normal)); UVs.Add(PixelSurfaces.Project(c, normal));
            for (int i = 0; i < 3; i++) Surfaces.Add(new Vector2((int)surface, light));
            Triangles.Add(index); Triangles.Add(index + 1); Triangles.Add(index + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, bool shade = true, PixelSurface surface = PixelSurface.Auto)
        { Triangle(a, b, c, color, shade, surface); Triangle(a, c, d, color, shade, surface); }

        public void Box(float x, float y, float width, float depth, float bottom, float top, Color color)
        {
            float l = x - width / 2, r = x + width / 2, s = y - depth / 2, n = y + depth / 2;
            Quad(V(l,s,top), V(l,n,top), V(r,n,top), V(r,s,top), color);
            Quad(V(l,s,bottom), V(l,s,top), V(r,s,top), V(r,s,bottom), color);
            Quad(V(r,n,bottom), V(r,n,top), V(l,n,top), V(l,n,bottom), color);
            Quad(V(l,n,bottom), V(l,n,top), V(l,s,top), V(l,s,bottom), color);
            Quad(V(r,s,bottom), V(r,s,top), V(r,n,top), V(r,n,bottom), color);
        }

        public void Plane(float x, float y, float width, float depth, float height, Color color)
            => Quad(V(x-width/2,y-depth/2,height), V(x-width/2,y+depth/2,height),
                V(x+width/2,y+depth/2,height), V(x+width/2,y-depth/2,height), color, false);

        public void Roof(float x, float y, float width, float depth, float baseHeight, float peak, Color color)
        {
            float l=x-width/2, r=x+width/2, s=y-depth/2, n=y+depth/2;
            Quad(V(l,s,baseHeight), V(l,n,baseHeight), V(x,n,peak), V(x,s,peak), color);
            Quad(V(x,s,peak), V(x,n,peak), V(r,n,baseHeight), V(r,s,baseHeight), color);
            Triangle(V(l,s,baseHeight), V(x,s,peak), V(r,s,baseHeight), color);
            Triangle(V(r,n,baseHeight), V(x,n,peak), V(l,n,baseHeight), color);
        }

        public void Cone(float x, float y, float radius, float bottom, float top, Color color,
            int sides = 7, float topRadius = 0)
        {
            for (int i=0; i<sides; i++)
            {
                double a=i*Math.PI*2/sides, b=(i+1)*Math.PI*2/sides;
                Vector3 p=V(x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius,bottom);
                Vector3 q=V(x+(float)Math.Cos(b)*radius,y+(float)Math.Sin(b)*radius,bottom);
                Vector3 u=V(x+(float)Math.Cos(a)*topRadius,y+(float)Math.Sin(a)*topRadius,top);
                Vector3 v=V(x+(float)Math.Cos(b)*topRadius,y+(float)Math.Sin(b)*topRadius,top);
                if (topRadius == 0) Triangle(p,u,q,color);
                else { Quad(p,u,v,q,color); Triangle(V(x,y,top),v,u,color); }
            }
        }

        public void Shadow(float x, float y, float radiusX, float radiusY)
        {
            Color color=new Color(0, 0, 0, 0.24f);
            for(int i=0;i<12;i++)
            {
                double a=i*Math.PI/6, b=(i+1)*Math.PI/6;
                Triangle(V(x,y,0.035f), V(x+(float)Math.Cos(b)*radiusX,y+(float)Math.Sin(b)*radiusY,0.035f),
                    V(x+(float)Math.Cos(a)*radiusX,y+(float)Math.Sin(a)*radiusY,0.035f),color,false,PixelSurface.None);
            }
        }

        public void Polygon(IReadOnlyList<Vector2> points, float height, Color color)
        {
            var remaining=new List<int>();
            for(int i=0;i<points.Count;i++) remaining.Add(i);
            int attempts=0;
            while(remaining.Count>2 && attempts++<points.Count*points.Count)
            {
                bool clipped=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    Vector2 a=points[remaining[(i+remaining.Count-1)%remaining.Count]], b=points[remaining[i]], c=points[remaining[(i+1)%remaining.Count]];
                    if(Cross(a,b,c)<=0.00001f) continue;
                    bool occupied=false;
                    foreach(int index in remaining)
                    {
                        Vector2 p=points[index];
                        if(p.Equals(a)||p.Equals(b)||p.Equals(c)) continue;
                        if(Cross(a,b,p)>=0 && Cross(b,c,p)>=0 && Cross(c,a,p)>=0) { occupied=true; break; }
                    }
                    if(occupied) continue;
                    float variation=0.98f+(remaining[i]%4)*0.01f;
                    Triangle(V(a.x,a.y,height), V(c.x,c.y,height), V(b.x,b.y,height),
                        new Color(color.r*variation,color.g*variation,color.b*variation,1),false);
                    remaining.RemoveAt(i); clipped=true; break;
                }
                if(!clipped) throw new InvalidOperationException("Contorno nao pode ser triangulado.");
            }
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
    }
}
