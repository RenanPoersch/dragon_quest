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
        private bool artworkFrame;

        public void SetArtworkFrame(bool enabled)
        {
            if (artworkFrame == enabled) return;
            artworkFrame = enabled;
            if (view == null) view = GetComponent<Camera>();
            view.rect = new Rect(0,0,1,1);
            view.ResetAspect();
        }

        public static Rect ArtworkViewport(float displayAspect)
            => new Rect(0,0,1,1);

        public static float ArtworkFrameSize(float halfHeight, float displayAspect)
            => (float)System.Math.Min(halfHeight, halfHeight*(16f/9f)/System.Math.Max(.1f,displayAspect));

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
            if (artworkFrame)
            {
                float width=view.targetTexture != null ? view.targetTexture.width : Screen.width;
                float height=view.targetTexture != null ? view.targetTexture.height : Screen.height;
                float aspect=width/Mathf.Max(1,height);
                view.rect=ArtworkViewport(aspect);
                view.aspect=aspect;
                view.orthographicSize=ArtworkFrameSize(requestedSize,aspect);
                return;
            }
            view.orthographicSize = ObliqueProjection.FrameSize(requestedSize, worldBounds, view.aspect, fixedFrame, coveredGroundBounds);
        }

        private Vector3 GetFollowPosition()
        {
            Vector3 position = ObliqueProjection.CameraPosition(target.position, worldBounds,
                view.orthographicSize, view.aspect, fixedFrame, transform.forward, coveredGroundBounds);
            // Anchor the proportional crop to the bottom so the forest exit stays on screen.
            return artworkFrame ? position + transform.up*(view.orthographicSize-requestedSize) : position;
        }
    }
}
