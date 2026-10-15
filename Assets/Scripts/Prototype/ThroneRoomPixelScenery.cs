using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class ThroneRoomPixelScenery : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        public void Build()
        {
            var painting=Resources.Load<Texture2D>("PixelArt/ThroneRoomInterior");
            var shader=Resources.Load<Shader>("ForestPixelBackdrop");
            var depthShader=Resources.Load<Shader>("PixelCharacter");
            if(painting==null||shader==null||depthShader==null) throw new InvalidOperationException("Pintura da sala do trono ausente.");
            var backdrop=new Material(shader){name="Sala do trono / pintura integral",mainTexture=painting}; owned.Add(backdrop);
            var foreground=new Material(depthShader){name="Sala do trono / profundidade",mainTexture=painting}; owned.Add(foreground);
            Draw("Sala do trono / pintura frontal",new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},60,backdrop);
            // Os pilares das bordas conservam seus pixels e encobrem sprites que passam atras deles.
            Draw("Pilar esquerdo / profundidade",new[]{new Vector2(0,0),new Vector2(.12f,0),new Vector2(.12f,.53f),
                new Vector2(.10f,.65f),new Vector2(0,.72f)},15,foreground);
            Draw("Pilar direito / profundidade",new[]{new Vector2(.88f,0),new Vector2(1,0),new Vector2(1,.72f),
                new Vector2(.90f,.65f),new Vector2(.88f,.53f)},15,foreground);
        }
        private void Draw(string name,Vector2[] outline,float depth,Material material)
        {
            var vertices=new Vector3[outline.Length];var uv=new Vector2[outline.Length];var colors=new Color[outline.Length];
            for(int i=0;i<outline.Length;i++) { vertices[i]=ThroneRoomLayout.ImagePlane(outline[i],depth);uv[i]=new Vector2(outline[i].x,1-outline[i].y);colors[i]=Color.white; }
            var mesh=new Mesh{name=name,vertices=vertices,uv=uv,colors=colors,triangles=PaintedInteriorScenery.Triangulate(outline)};
            mesh.RecalculateBounds();owned.Add(mesh);
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        private void OnDestroy()
        {
            for(int i=owned.Count-1;i>=0;i--) if(owned[i]!=null)
            { if(Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
        }
    }
}
