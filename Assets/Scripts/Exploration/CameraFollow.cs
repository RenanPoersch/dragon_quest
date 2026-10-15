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
        private bool sideView;
        private float sideHeight;
        private bool perspectiveView;
        private Vector3 perspectivePosition;
        private Quaternion perspectiveRotation;
        private float perspectiveFieldOfView, perspectiveAspect;
        public void SetPerspectiveView(bool enabled, Vector3 position = default, Quaternion rotation = default,
            float fieldOfView = 45f, float referenceAspect = 16f/9f)
        {
            if(perspectiveView!=enabled)
            {
                if(view==null) view=GetComponent<Camera>();
                view.rect=new Rect(0,0,1,1); view.ResetAspect();
            }
            perspectiveView=enabled; perspectivePosition=position; perspectiveRotation=rotation;
            perspectiveFieldOfView=fieldOfView; perspectiveAspect=referenceAspect;
        }
        public static float PerspectiveFieldOfView(float fieldOfView,float displayAspect,float referenceAspect)
        {
            double halfTan=System.Math.Tan(fieldOfView*System.Math.PI/360);
            return (float)(2*System.Math.Atan(halfTan*System.Math.Max(1,referenceAspect/System.Math.Max(.1f,displayAspect)))*180/System.Math.PI);
        }
        private bool artworkFrame, artworkContain;
        private float artworkAspect = 16f / 9f;

        public void SetArtworkFrame(bool enabled, bool contain = false, float referenceAspect = 16f / 9f)
        {
            if (artworkFrame == enabled && artworkContain == contain && Mathf.Approximately(artworkAspect, referenceAspect)) return;
            artworkFrame = enabled; artworkContain = contain; artworkAspect = referenceAspect;
            if (view == null) view = GetComponent<Camera>();
            view.rect = new Rect(0,0,1,1);
            view.ResetAspect();
        }

        public static Rect ArtworkViewport(float displayAspect)
            => new Rect(0,0,1,1);

        public static float ArtworkFrameSize(float halfHeight, float displayAspect, bool contain = false, float referenceAspect = 16f / 9f)
            => (float)(contain ? System.Math.Max(halfHeight, halfHeight*referenceAspect/System.Math.Max(.1f,displayAspect))
                : System.Math.Min(halfHeight, halfHeight*referenceAspect/System.Math.Max(.1f,displayAspect)));

        public void SetSideView(bool enabled, float height = 0)
        { sideView = enabled; sideHeight = height; }

        public void Initialize(Transform followTarget, Rect bounds, float orthographicSize = 6f, bool fixedCamera = false, Rect? groundCoverage = null)
        {
            target = followTarget;
            worldBounds = bounds;
            coveredGroundBounds = groundCoverage;
            fixedFrame = fixedCamera;
            view = GetComponent<Camera>();
            view.orthographic = !perspectiveView;
            requestedSize = Mathf.Max(0.1f, orthographicSize);
            UpdateSize();
            view.nearClipPlane = 0.1f;
            view.farClipPlane = 150f;
            transform.rotation = perspectiveView ? perspectiveRotation : sideView ? Quaternion.identity : Quaternion.AngleAxis(ObliqueProjection.YawDegrees, Vector3.forward)
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
            if (perspectiveView)
            {
                float width=view.targetTexture != null ? view.targetTexture.width : Screen.width;
                float height=view.targetTexture != null ? view.targetTexture.height : Screen.height;
                view.rect=new Rect(0,0,1,1); view.aspect=width/Mathf.Max(1,height);
                view.fieldOfView=PerspectiveFieldOfView(perspectiveFieldOfView,view.aspect,perspectiveAspect);
                return;
            }
            if (artworkFrame)
            {
                float width=view.targetTexture != null ? view.targetTexture.width : Screen.width;
                float height=view.targetTexture != null ? view.targetTexture.height : Screen.height;
                float aspect=width/Mathf.Max(1,height);
                view.rect=ArtworkViewport(aspect);
                view.aspect=aspect;
                view.orthographicSize=ArtworkFrameSize(requestedSize,aspect,artworkContain,artworkAspect);
                return;
            }
            view.orthographicSize = sideView ? requestedSize
                : ObliqueProjection.FrameSize(requestedSize, worldBounds, view.aspect, fixedFrame, coveredGroundBounds);
        }

        private Vector3 GetFollowPosition()
        {
            if (perspectiveView) return perspectivePosition;
            if (sideView)
            {
                float halfWidth = view.orthographicSize * view.aspect;
                float x = worldBounds.width <= halfWidth * 2 ? worldBounds.center.x
                    : Mathf.Clamp(target.position.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
                return new Vector3(x, sideHeight, -30);
            }
            Vector3 position = ObliqueProjection.CameraPosition(target.position, worldBounds,
                view.orthographicSize, view.aspect, fixedFrame, transform.forward, coveredGroundBounds);
            // Anchor the proportional crop to the bottom so the forest exit stays on screen.
            return artworkFrame && !artworkContain ? position + transform.up*(view.orthographicSize-requestedSize) : position;
        }
    }
}
