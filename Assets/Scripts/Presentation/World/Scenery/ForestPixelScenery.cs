using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.World.Scenery
{
    // One continuous painting, registered to the existing fixed oblique camera and XY interaction map.
    [DefaultExecutionOrder(130)]
    public sealed class ForestPixelScenery : MonoBehaviour
    {
        private const float ArtHalfHeight = 8f, ArtHalfWidth = 128f / 9f;
        private Mesh mesh;
        private Material material;
        private AncientSecrets secrets;
        private GameObject scale;
        private Mesh scaleMesh;
        private Material scaleMaterial;

        public void Build(AncientSecrets progress = null)
        {
            secrets = progress;
            var texture = Resources.Load<Texture2D>("PixelArt/EastForestBackdrop");
            var shader = Resources.Load<Shader>("ForestPixelBackdrop");
            if (texture == null || shader == null)
                throw new System.InvalidOperationException("Cenario pixel art da floresta leste ausente.");
            material = new Material(shader) { name = "Floresta ancestral / pintura pixel art" };
            material.mainTexture = texture;
            var painting = new GameObject("Floresta / arvores gigantes, musgo e raios de luz");
            painting.transform.SetParent(transform, false);
            var rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees, Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees, 0, 0);
            painting.transform.localRotation = rotation;
            painting.transform.localPosition = rotation * Vector3.forward * 20f;
            mesh = new Mesh { name = "Floresta / tela continua" };
            painting.AddComponent<MeshFilter>().sharedMesh = mesh;
            painting.AddComponent<MeshRenderer>().sharedMaterial = material;
            mesh.vertices = new[] { new Vector3(-ArtHalfWidth,-ArtHalfHeight,0), new Vector3(-ArtHalfWidth,ArtHalfHeight,0),
                new Vector3(ArtHalfWidth,ArtHalfHeight,0), new Vector3(ArtHalfWidth,-ArtHalfHeight,0) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateBounds();
            AddScale(painting.transform);
        }

        private void AddScale(Transform painting)
        {
            var texture = Resources.Load<Texture2D>("PixelArt/DragonScale");
            var shader = Resources.Load<Shader>("ForestScale");
            if (texture == null || shader == null)
                throw new System.InvalidOperationException("Sprite da escama ancestral ausente.");
            scaleMaterial = new Material(shader) { name = "Escama vermelha / sombra do tronco" };
            scaleMaterial.mainTexture = texture;
            scale = new GameObject("Escama ancestral / discreta no oco do tronco");
            scale.transform.SetParent(painting, false);
            Vector2 hollow = ObliqueProjection.ScreenPlane(SecretLayout.HollowTree);
            scale.transform.localPosition = new Vector3(hollow.x + .18f, hollow.y + .10f, -.05f);
            scale.transform.localRotation = Quaternion.Euler(0, 0, -15);
            // About 14 x 20 visible pixels at 720p: nestled in the dark hollow, without a glow.
            scaleMesh = new Mesh { name = "Escama / sprite" };
            scaleMesh.vertices = new[] { new Vector3(-.3f,-.3f,0), new Vector3(-.3f,.3f,0),
                new Vector3(.3f,.3f,0), new Vector3(.3f,-.3f,0) };
            scaleMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            scaleMesh.triangles = new[] { 0,1,2,0,2,3 }; scaleMesh.RecalculateBounds();
            scale.AddComponent<MeshFilter>().sharedMesh = scaleMesh;
            scale.AddComponent<MeshRenderer>().sharedMaterial = scaleMaterial;
            scale.SetActive(secrets == null || !secrets.HasScale);
        }

        private void LateUpdate()
        {
            if (scale != null && secrets != null && scale.activeSelf == secrets.HasScale)
                scale.SetActive(!secrets.HasScale);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            if (scaleMesh != null) Destroy(scaleMesh);
            if (scaleMaterial != null) Destroy(scaleMaterial);
        }
    }
}
