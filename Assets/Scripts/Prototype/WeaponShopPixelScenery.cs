using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class WeaponShopPixelScenery : PaintedInteriorScenery
    {
        public static readonly Vector2[][] Foregrounds = {
            new[] { new Vector2(.351f,.372f),new Vector2(.359f,.365f),new Vector2(.362f,.317f),new Vector2(.370f,.290f),
                new Vector2(.382f,.290f),new Vector2(.391f,.318f),new Vector2(.391f,.357f),new Vector2(.407f,.348f),
                new Vector2(.620f,.348f),new Vector2(.684f,.348f),new Vector2(.691f,.320f),new Vector2(.702f,.313f),
                new Vector2(.716f,.325f),new Vector2(.725f,.355f),new Vector2(.768f,.365f),new Vector2(.772f,.404f),
                new Vector2(.766f,.519f),new Vector2(.357f,.519f),new Vector2(.351f,.427f) },
            new[] { new Vector2(.152f,.315f),new Vector2(.18f,.312f),new Vector2(.209f,.321f),new Vector2(.236f,.329f),
                new Vector2(.215f,.35f),new Vector2(.208f,.383f),new Vector2(.238f,.397f),new Vector2(.238f,.464f),
                new Vector2(.225f,.477f),new Vector2(.163f,.477f),new Vector2(.15f,.463f),new Vector2(.153f,.394f),
                new Vector2(.177f,.38f),new Vector2(.174f,.354f) },
            new[] { new Vector2(.089f,.682f),new Vector2(.105f,.644f),new Vector2(.188f,.647f),new Vector2(.201f,.669f),
                new Vector2(.198f,.761f),new Vector2(.188f,.771f),new Vector2(.093f,.759f) }
        };
        private static readonly Vector2[] Anchors = { new Vector2(.56f,.512f),new Vector2(.195f,.468f),new Vector2(.145f,.765f) };
        public void Build(PhysicsMaterial2D physics = null) => BuildPainting("Loja de armas","WeaponShopInterior",
            WeaponShopLayout.CameraSize,WeaponShopLayout.ArtAspect,Foregrounds,Anchors,WeaponShopLayout.Fixtures,physics);
    }
}
