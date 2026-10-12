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
        private PixelWalkCycle walk;
        private int teleportRevision;
        public void Initialize(Transform root, SpriteRenderer renderer, PixelSpriteAtlas source, int frame, bool animated,
            PixelSpriteAtlas diagonalSource = null)
        {
            anchor = root; sprite = renderer; atlas = source; view = Camera.main;
            diagonalAtlas = diagonalSource;
            movement = animated ? root.GetComponent<PlayerMovement>() : null;
            teleportRevision = movement != null ? movement.TeleportRevision : 0;
            previous = root.position; walk = default; sprite.sprite = atlas.Sprites[frame]; FaceCamera();
        }
        private void LateUpdate()
        {
            FaceCamera();
            if (movement == null) return;
            float distance = Vector2.Distance(anchor.position, previous); previous = anchor.position;
            bool teleported = teleportRevision != movement.TeleportRevision;
            teleportRevision = movement.TeleportRevision;
            walk.Advance(distance, Time.deltaTime, movement.HasMovementInput, teleported);
            var facing = PixelSpriteLayout.Facing(movement.FacingDirection, diagonalAtlas != null);
            var source = PixelSpriteLayout.IsDiagonal(facing) ? diagonalAtlas : atlas;
            int pose = PixelSpriteLayout.WalkFrame(facing, walk.Distance, walk.Walking);
            var next = source.Sprites[pose];
            if (sprite.sprite != next) sprite.sprite = next;
        }
        private void FaceCamera()
        {
            if (view == null) view = Camera.main;
            if (view != null)
            {
                // O sprite permanece vertical: inclina-lo com a camera empurra a cabeca para dentro dos objetos.
                transform.rotation = Quaternion.LookRotation(PixelSpriteLayout.HorizontalCameraForward(view.transform.forward), PixelSpriteLayout.UprightUp);
                // Conserva tamanho e proporcoes vistos na tela, compensando a projecao obliqua.
                transform.localScale = new Vector3(1, PixelSpriteLayout.UprightScaleY, 1);
            }
        }
    }
}
