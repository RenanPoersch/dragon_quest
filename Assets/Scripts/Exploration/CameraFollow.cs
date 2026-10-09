using UnityEngine;

namespace DragonQuest.Exploration
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;

        private Transform target;
        private Camera view;
        private Rect worldBounds;
        private Vector3 velocity;

        public void Initialize(Transform followTarget, Rect bounds)
        {
            target = followTarget;
            worldBounds = bounds;
            view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = 6f;
            transform.position = GetBoundedPosition();
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null || view == null) return;
            transform.position = Vector3.SmoothDamp(
                transform.position, GetBoundedPosition(), ref velocity, smoothTime);
        }

        private Vector3 GetBoundedPosition()
        {
            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            float x = worldBounds.width <= halfWidth * 2f
                ? worldBounds.center.x
                : Mathf.Clamp(target.position.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            float y = worldBounds.height <= halfHeight * 2f
                ? worldBounds.center.y
                : Mathf.Clamp(target.position.y, worldBounds.yMin + halfHeight, worldBounds.yMax - halfHeight);
            return new Vector3(x, y, -10f);
        }
    }
}
