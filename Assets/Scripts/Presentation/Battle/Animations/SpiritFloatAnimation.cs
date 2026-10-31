using DragonQuest.Presentation.World.Characters;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    // Moves only the sprite; its parent keeps the floor depth, interaction anchor and ground shadow.
    public sealed class SpiritFloatAnimation : MonoBehaviour
    {
        public const float Period = 2.8f;
        private const float Amplitude = .06f;
        private Vector3 origin;
        private float bodyHeight, elapsed;

        public static void Attach(Transform visual)
        {
            var pivot = new GameObject("Flutuacao do espirito").transform;
            pivot.SetParent(visual.parent, false);
            visual.SetParent(pivot, false);
            visual.gameObject.AddComponent<SpiritFloatAnimation>();
        }

        private void Awake()
        {
            origin = transform.localPosition;
            bodyHeight = GetComponent<SpriteRenderer>().sprite.bounds.size.y * PixelSpriteLayout.UprightScaleY;
        }

        // Fraction of body height, shared by exploration and the battle HUD. Starts at rest without a jump.
        public static float HeightAt(float seconds)
            => Amplitude * (1 - Mathf.Cos(Mathf.Repeat(seconds, Period) * (2 * Mathf.PI / Period)));

        public void ShowAt(float seconds)
            => transform.localPosition = origin + Vector3.back * (bodyHeight * HeightAt(seconds));

        private void LateUpdate()
        {
            elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, Period);
            ShowAt(elapsed);
        }

        private void OnEnable() { elapsed = 0; ShowAt(0); }
        private void OnDisable() { elapsed = 0; transform.localPosition = origin; }
    }
}
