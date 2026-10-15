using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class InnPixelScenery : PaintedInteriorScenery
    {
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

        public void Build(PhysicsMaterial2D physics = null) => BuildPainting("Pousada","InnInterior",
            PrototypeInnLayout.CameraSize,PrototypeInnLayout.ArtAspect,Foregrounds,Anchors,PrototypeInnLayout.Fixtures,physics);
    }
}
