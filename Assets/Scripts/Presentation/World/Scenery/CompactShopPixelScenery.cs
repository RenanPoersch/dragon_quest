using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CompactShopPixelScenery : PaintedInteriorScenery
    {
        public void Build(CompactShopLayout layout,PhysicsMaterial2D physics=null) => BuildPainting(layout.Name,layout.Resource,
            CompactShopLayout.CameraSize,CompactShopLayout.ArtAspect,layout.Foregrounds,layout.ForegroundAnchors,layout.Fixtures,physics);
    }
}
