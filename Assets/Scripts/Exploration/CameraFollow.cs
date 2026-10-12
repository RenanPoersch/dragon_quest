using UnityEngine;

namespace DragonQuest.Exploration
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        private Transform target;
        private Camera view;
        private Rect worldBounds;
        private Rect? coveredGroundBounds;
        private bool fixedFrame;
        private float requestedSize;

        public void Initialize(Transform followTarget, Rect bounds, float orthographicSize = 6f, bool fixedCamera = false, Rect? groundCoverage = null)
        {
            target = followTarget;
            worldBounds = bounds;
            coveredGroundBounds = groundCoverage;
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
        }

        private void LateUpdate()
        {
            if (target == null || view == null) return;
            UpdateSize();
            // target.position ja recebe a interpolacao do Rigidbody2D para este quadro.
            // Uma segunda suavizacao separava a camera da apresentacao do heroi.
            transform.position = GetFollowPosition();
        }

        private void UpdateSize()
        {
            view.orthographicSize = ObliqueProjection.FrameSize(requestedSize, worldBounds, view.aspect, fixedFrame, coveredGroundBounds);
        }

        private Vector3 GetFollowPosition()
        {
            return ObliqueProjection.CameraPosition(target.position, worldBounds,
                view.orthographicSize, view.aspect, fixedFrame, transform.forward, coveredGroundBounds);
        }
    }
}
