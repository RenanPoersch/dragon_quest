using UnityEngine;

namespace DragonQuest.Exploration
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float smoothTime = 0.08f;

        private Transform target;
        private Camera view;
        private Rect worldBounds;
        private Vector3 velocity;
        private bool fixedFrame;
        private float requestedSize;

        public void Initialize(Transform followTarget, Rect bounds, float orthographicSize = 6f, bool fixedCamera = false)
        {
            target = followTarget;
            worldBounds = bounds;
            fixedFrame = fixedCamera;
            view = GetComponent<Camera>();
            view.orthographic = true;
            requestedSize = Mathf.Max(0.1f, orthographicSize);
            UpdateSize();
            view.nearClipPlane = 0.1f;
            view.farClipPlane = 150f;
            transform.rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees, Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees, 0, 0);
            transform.position = GetFollowPosition();
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null || view == null) return;
            UpdateSize();
            transform.position = Vector3.SmoothDamp(
                transform.position, GetFollowPosition(), ref velocity, smoothTime);
        }

        private void UpdateSize()
        {
            view.orthographicSize = ObliqueProjection.FrameSize(requestedSize, worldBounds, view.aspect, fixedFrame);
        }

        private Vector3 GetFollowPosition()
        {
            Vector2 focus = ObliqueProjection.CameraFocus(target.position, worldBounds,
                view.orthographicSize, view.aspect, fixedFrame);
            return ObliqueProjection.Point(focus, fixedFrame ? 0 : ObliqueProjection.HeroCenterHeight)
                - transform.forward * ObliqueProjection.Distance;
        }
    }
}
