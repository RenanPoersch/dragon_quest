using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeShopLayout
    {
        public const float WidthScale = 5f / 7f, DepthScale = .7f, CameraSize = 4.3f;
        public static readonly Vector3 InteriorScale = new Vector3(WidthScale, DepthScale, 1);
        public static readonly Rect Bounds = new Rect(-5, -3.5f, 10, 7);
        public static readonly Rect LegacyBounds = new Rect(-7, -5, 14, 10);
        public static readonly Vector2 Spawn = new Vector2(0, -2.7f * DepthScale);
        public static readonly Vector2 Exit = new Vector2(0, -3.8f * DepthScale);
    }
}
