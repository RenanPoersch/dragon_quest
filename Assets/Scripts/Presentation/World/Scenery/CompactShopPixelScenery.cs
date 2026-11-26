using DragonQuest.Exploration.Layouts;
using UnityEngine;

// Usa a composição de interior pintado para os mapas que compartilham o formato de loja compacta.
namespace DragonQuest.Presentation.World.Scenery
{
    public sealed class CompactShopPixelScenery : PaintedInteriorScenery
    {
        // Recebe do layout o nome, a arte e a geometria necessários para montar esta loja.
        public void Build(CompactShopLayout layout,PhysicsMaterial2D physics=null) => BuildPainting(layout.Name,layout.Resource,
            CompactShopLayout.CameraSize,CompactShopLayout.ArtAspect,layout.Foregrounds,layout.ForegroundAnchors,layout.Fixtures,physics);
    }
}
