using UnityEngine;

namespace DragonQuest.Exploration
{
    public sealed class PixelCharacter : MonoBehaviour
    {
        private Transform anchor;
        private PlayerMovement movement;
        private SpriteRenderer sprite;
        private PixelSpriteAtlas atlas;
        private Camera view;
        private Vector3 previous;
        private float walkedDistance;
        public void Initialize(Transform root, SpriteRenderer renderer, PixelSpriteAtlas source, int frame, bool animated)
        {
            anchor = root; sprite = renderer; atlas = source; view = Camera.main;
            movement = animated ? root.GetComponent<PlayerMovement>() : null;
            previous = root.position; sprite.sprite = atlas.Sprites[frame]; FaceCamera();
        }
        private void LateUpdate()
        {
            FaceCamera();
            if (movement == null) return;
            float distance = Vector2.Distance(anchor.position, previous); previous = anchor.position;
            bool walking = distance > 0.0001f && distance < 0.5f;
            if (walking) walkedDistance += distance;
            else walkedDistance = 0;
            sprite.sprite = atlas.Sprites[PixelSpriteLayout.WalkFrame(PixelSpriteLayout.Facing(movement.FacingDirection), walkedDistance, walking)];
        }
        private void FaceCamera()
        {
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
        }
    }
}
