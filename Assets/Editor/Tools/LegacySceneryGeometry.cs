using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Authoring
{
    // Replaced geometric artwork retained for historical exports, outside the player assembly.
    public static class LegacySceneryGeometry
    {
        public static ObliqueMeshData IslandData() => WorldIslandArt.Ground();

        public static ObliqueMeshData DragonData()
        {
            var mesh=new ObliqueMeshData();
            Color scales=ObliqueMeshData.Hex("863E3D"),bone=ObliqueMeshData.Hex("B99A78"),wing=ObliqueMeshData.Hex("543454");
            mesh.Shadow(0,0,1.5f,1.4f);
            mesh.Box(0,0.1f,1.3f,1.8f,0.6f,1.7f,scales);
            mesh.Roof(0,0.1f,1.3f,1.8f,1.7f,2,scales);
            mesh.Box(0,-0.7f,0.75f,0.15f,0.7f,1.5f,bone);
            foreach(float side in new[]{-1f,1f})
            {
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.65f),ObliqueMeshData.V(side*2.8f,1,2.4f),ObliqueMeshData.V(side*1.6f,-0.5f,1.35f),wing);
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.65f),ObliqueMeshData.V(side*1.6f,-0.5f,1.35f),ObliqueMeshData.V(side*0.7f,-0.9f,1.1f),wing);
                mesh.Triangle(ObliqueMeshData.V(side*0.5f,0.6f,1.68f),ObliqueMeshData.V(side*2.8f,1,2.42f),ObliqueMeshData.V(side*1.6f,0.85f,2.25f),bone);
                foreach(float y in new[]{-0.55f,0.7f})
                {
                    mesh.Box(side*0.65f,y,0.35f,0.42f,0.12f,0.85f,scales);
                    mesh.Box(side*0.65f,y-0.15f,0.42f,0.65f,0.03f,0.25f,bone);
                }
                mesh.Cone(side*0.3f,-1.08f,0.12f,1.92f,2.42f,bone,6,0);
                mesh.Box(side*0.43f,-1.38f,0.08f,0.18f,1.77f,1.88f,ObliqueMeshData.Hex("EBC05E"));
            }
            mesh.Box(0,-1.25f,0.85f,0.75f,1.4f,1.98f,scales);
            mesh.Box(0,-1.7f,0.72f,0.45f,1.4f,1.65f,scales);
            mesh.Box(0,-1.94f,0.62f,0.04f,1.45f,1.52f,ObliqueMeshData.Hex("30282E"));
            mesh.Triangle(ObliqueMeshData.V(-0.36f,1,0.8f),ObliqueMeshData.V(0.36f,1,0.8f),ObliqueMeshData.V(0,3.1f,0.18f),scales);
            for(int i=0;i<4;i++) mesh.Cone(0,0.3f+i*0.32f,0.12f,1.8f,2.25f,bone,5,0);
            return mesh;
        }

        public static ObliqueMeshData PersonBodyData(string hex, bool armed, bool spikyBlond = false)
        {
            var body=new ObliqueMeshData(); Color cloth=ObliqueMeshData.Hex(hex), skin=ObliqueMeshData.Hex("D4B294");
            body.Box(0,0,0.38f,0.26f,0.5f,1.02f,cloth);
            body.Box(0,0,0.2f,0.2f,1.02f,1.12f,skin);
            body.Box(0,-0.015f,0.29f,0.27f,1.1f,1.4f,skin);
            Color hair = ObliqueMeshData.Hex(spikyBlond ? "D9B35D" : "4C4544");
            body.Box(0,0.015f,0.32f,0.29f,1.34f,1.45f,hair);
            if (spikyBlond)
                foreach (float x in new[] {-0.12f, -0.04f, 0.04f, 0.12f})
                    body.Cone(x, 0.015f, 0.075f, 1.41f, 1.58f + (x < 0 ? 0.04f : 0), hair, 4);
            foreach(float x in new[]{-0.25f,0.25f}) body.Box(x,0,0.12f,0.19f,0.56f,0.98f,cloth);
            body.Box(0,0.16f,0.4f,0.05f,0.38f,1.02f,ObliqueMeshData.Hex(armed?"835A4A":"556577"));
            body.Box(0,-0.145f,0.39f,0.025f,0.54f,0.61f,ObliqueMeshData.Hex("B7A374"));
            if(armed)
            { body.Box(0.38f,0.1f,0.06f,0.08f,0.23f,1.02f,ObliqueMeshData.Hex("BCC6C7")); body.Box(0.38f,0.1f,0.23f,0.12f,0.45f,0.5f,ObliqueMeshData.Hex("87775D")); }
            return body;
        }

        public static ObliqueMeshData LegData(float x,Color color)
        {
            var mesh=new ObliqueMeshData();
            mesh.Box(x,0,0.15f,0.17f,-0.35f,0,color);
            mesh.Box(x,-0.06f,0.18f,0.3f,-0.5f,-0.35f,ObliqueMeshData.Hex("49494A"));
            return mesh;
        }

        private static ObliqueMeshData StandingPerson(string hex, bool spikyBlond = false)
        {
            var mesh=PersonBodyData(hex,true,spikyBlond);Color cloth=ObliqueMeshData.Hex(hex);
            mesh.Append(LegData(-0.12f,cloth),new Vector3(0,0,-0.5f));mesh.Append(LegData(0.12f,cloth),new Vector3(0,0,-0.5f));
            return mesh;
        }

        public static ObliqueMeshData KingData(bool possessed=true)
        {
            var mesh=StandingPerson("9A5979");Color gold=ObliqueMeshData.Hex("D8B968");
            mesh.Box(0,0,0.34f,0.3f,1.4f,1.49f,gold);
            foreach(float x in new[]{-0.12f,0.12f}) foreach(float y in new[]{-0.09f,0.09f}) mesh.Cone(x,y,0.06f,1.49f,1.72f,gold,5,0);
            if(possessed) mesh.Box(0,-0.158f,0.2f,0.025f,1.24f,1.29f,ObliqueMeshData.Hex("BBA0ED"));
            return mesh;
        }

        public static ObliqueMeshData SpiritData()
        {
            var mesh=new ObliqueMeshData();Color shadow=ObliqueMeshData.Hex("483858"),bone=ObliqueMeshData.Hex("ADA6BC");
            mesh.Cone(0,0,0.48f,0.25f,1.35f,shadow,7,0.22f);
            mesh.Cone(0,0.12f,0.34f,1.35f,2.1f,shadow,7,0);
            mesh.Box(0,-0.14f,0.39f,0.29f,1.32f,1.65f,bone);
            foreach(float side in new[]{-1f,1f})
            {
                mesh.Triangle(ObliqueMeshData.V(side*0.18f,0,1.25f),ObliqueMeshData.V(side*0.8f,-0.12f,1.1f),ObliqueMeshData.V(side*0.45f,-0.22f,0.72f),shadow);
                mesh.Box(side*0.09f,-0.292f,0.08f,0.025f,1.47f,1.53f,ObliqueMeshData.Hex("D39DFF"));
            }
            mesh.Box(0,-0.292f,0.12f,0.025f,1.37f,1.4f,ObliqueMeshData.Hex("382B44"));
            return mesh;
        }

        public static ObliqueMeshData RuinedCastleData()
        {
            var mesh=new ObliqueMeshData();Color stone=ObliqueMeshData.Hex("666169"),charcoal=ObliqueMeshData.Hex("45404B");
            mesh.Box(0,0,8,4.5f,0,0.3f,stone);
            mesh.Box(-3,1,1.6f,1.6f,0.3f,1.65f,charcoal);mesh.Box(3,1,1.6f,1.6f,0.3f,0.95f,charcoal);
            mesh.Box(0,1.8f,4.3f,0.4f,0.3f,1.1f,stone);
            mesh.Roof(-1,-0.4f,2.8f,1.5f,0.3f,0.75f,charcoal);
            foreach(float x in new[]{-2.5f,0,2.3f})
            {
                mesh.Box(x,-0.8f,0.9f,0.7f,0.3f,0.58f,stone);
                mesh.Cone(x,-0.6f,0.35f,0.58f,1.3f,ObliqueMeshData.Hex("C87740"),6,0);
                mesh.Cone(x,-0.6f,0.16f,0.6f,1,ObliqueMeshData.Hex("EBC16A"),6,0);
            }
            return mesh;
        }

        public static ObliqueMeshData EndingSceneData(GameEnding ending)
        {
            var mesh=new ObliqueMeshData();
            mesh.Plane(0,0,14,10,0,ObliqueMeshData.Hex(ending==GameEnding.CastleDestroyed?"433E47":"89818A"));
            if(ending==GameEnding.CastleDestroyed) {mesh.Append(RuinedCastleData(),Vector3.zero);return mesh;}
            var throne=ObliqueScenery.BlockData("Trono - encosto",new Vector2(1.8f,1.4f),"77576B",true,3);
            mesh.Append(throne,new Vector3(0,3.4f,0));
            if(ending==GameEnding.KingFreed) mesh.Append(KingData(false),new Vector3(0,2.3f,0));
            else
            {
                mesh.Box(0,2.3f,0.5f,1.2f,0,0.24f,ObliqueMeshData.Hex("9A5979"));
                mesh.Cone(0.5f,2.2f,0.2f,0,0.12f,ObliqueMeshData.Hex("D8B968"),7,0.2f);
            }
            foreach(var pair in new[]{("B4A078",0f),("DADECB",-2.2f),("608DA1",2.2f)})
                mesh.Append(StandingPerson(pair.Item1, pair.Item2 == 0),new Vector3(pair.Item2,-1.5f,0));
            if(ending==GameEnding.HeroPossessed)
            {
                mesh.Append(SpiritData(),new Vector3(0,-1.1f,-0.65f));
                mesh.Box(0,-1.658f,0.2f,0.025f,1.24f,1.3f,ObliqueMeshData.Hex("CB9BEF"));
            }
            return mesh;
        }
    }
}
