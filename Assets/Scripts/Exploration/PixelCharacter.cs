using UnityEngine;

namespace DragonQuest.Exploration
{
    public sealed class PixelCharacter : MonoBehaviour
    {
        private Transform anchor;
        private PlayerMovement movement;
        private SpriteRenderer sprite;
        private PixelSpriteAtlas atlas;
        private PixelSpriteAtlas diagonalAtlas;
        private Camera view;
        private Vector3 previous;
        private float walkedDistance;
        public void Initialize(Transform root, SpriteRenderer renderer, PixelSpriteAtlas source, int frame, bool animated,
            PixelSpriteAtlas diagonalSource = null)
        {
            anchor = root; sprite = renderer; atlas = source; view = Camera.main;
            diagonalAtlas = diagonalSource;
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
            var facing = PixelSpriteLayout.Facing(movement.FacingDirection, diagonalAtlas != null);
            var source = PixelSpriteLayout.IsDiagonal(facing) ? diagonalAtlas : atlas;
            sprite.sprite = source.Sprites[PixelSpriteLayout.WalkFrame(facing, walkedDistance, walking)];
        }
        private void FaceCamera()
        {
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
        }
    }
}
