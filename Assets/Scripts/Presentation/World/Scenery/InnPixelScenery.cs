using DragonQuest.Exploration.Layouts;
using UnityEngine;

namespace DragonQuest.Presentation.World.Scenery
{
    public sealed class InnPixelScenery : PaintedInteriorScenery
    {
        public static readonly Vector2[][] Foregrounds = {
            new[] { new Vector2(0.57f,0.305f),new Vector2(0.584f,0.305f),new Vector2(0.586f,0.282f),new Vector2(0.602f,0.27f),new Vector2(0.62f,0.281f),new Vector2(0.63f,0.306f),new Vector2(0.805f,0.306f),new Vector2(0.817f,0.273f),new Vector2(0.83f,0.27f),new Vector2(0.822f,0.305f),new Vector2(0.884f,0.305f),new Vector2(0.895f,0.35f),new Vector2(0.892f,0.488f),new Vector2(0.575f,0.49f) }
        };
        private static readonly Vector2[] Anchors = new[] { new Vector2(0.735f,0.485f) };
        public void Build(PhysicsMaterial2D physics = null) => BuildPainting("Pousada","InnInterior",
            InnLayout.CameraSize,InnLayout.ArtAspect,Foregrounds,Anchors,InnLayout.Fixtures,physics);
    }
}
