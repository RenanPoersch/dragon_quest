using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class WeaponShopPixelScenery : PaintedInteriorScenery
    {
        public static readonly Vector2[][] Foregrounds = {
            new[] { new Vector2(0.34f,0.345f),new Vector2(0.362f,0.345f),new Vector2(0.363f,0.298f),new Vector2(0.38f,0.278f),new Vector2(0.397f,0.3f),new Vector2(0.397f,0.335f),new Vector2(0.6f,0.335f),new Vector2(0.616f,0.317f),new Vector2(0.644f,0.317f),new Vector2(0.66f,0.34f),new Vector2(0.745f,0.34f),new Vector2(0.775f,0.291f),new Vector2(0.79f,0.296f),new Vector2(0.775f,0.34f),new Vector2(0.79f,0.34f),new Vector2(0.79f,0.515f),new Vector2(0.345f,0.515f) },
            new[] { new Vector2(0.137f,0.348f),new Vector2(0.156f,0.341f),new Vector2(0.221f,0.35f),new Vector2(0.236f,0.365f),new Vector2(0.215f,0.39f),new Vector2(0.196f,0.392f),new Vector2(0.2f,0.414f),new Vector2(0.235f,0.425f),new Vector2(0.234f,0.48f),new Vector2(0.222f,0.506f),new Vector2(0.15f,0.507f),new Vector2(0.138f,0.49f),new Vector2(0.138f,0.432f),new Vector2(0.162f,0.416f),new Vector2(0.169f,0.39f),new Vector2(0.145f,0.379f) },
            new[] { new Vector2(0.1f,0.7f),new Vector2(0.117f,0.675f),new Vector2(0.207f,0.675f),new Vector2(0.22f,0.696f),new Vector2(0.219f,0.774f),new Vector2(0.1f,0.78f) }
        };
        private static readonly Vector2[] Anchors = new[] { new Vector2(0.56f,0.512f),new Vector2(0.185f,0.5f),new Vector2(0.16f,0.776f) };
        public void Build(PhysicsMaterial2D physics = null) => BuildPainting("Loja de armas","WeaponShopInterior",
            WeaponShopLayout.CameraSize,WeaponShopLayout.ArtAspect,Foregrounds,Anchors,WeaponShopLayout.Fixtures,physics);
    }
}
