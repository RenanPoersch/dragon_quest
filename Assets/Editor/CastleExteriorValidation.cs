using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DragonQuest.Exploration;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CastleExteriorValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Castle Exterior")]
    public static void Run()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/CastleExteriorPreview"));
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        var assets = new List<UnityEngine.Object>();
        RenderTexture target = null;
        var previous = RenderTexture.active;
        int checks = 0;
        void Check(bool valid, string message) { checks++; if (!valid) throw new Exception(message); }
        try
        {
            var root = new GameObject("Castle exterior preview");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var stone = new Material(Resources.Load<Shader>("ObliqueWorld")); assets.Add(stone);
            stone.SetTexture("_PixelAtlas", Resources.Load<Texture2D>("PixelArt/CastleMaterialsAtlas"));
            stone.SetFloat("_Brightness", .95f);
            var fire = new Material(Resources.Load<Shader>("PixelCharacter")); assets.Add(fire);
            var ground = new Material(Resources.Load<Shader>("CityGround")); assets.Add(ground);
            ground.SetTexture("_MainTex", Resources.Load<Texture2D>("PixelArt/CityGroundMap"));
            void Model(string name, ObliqueMeshData data, Vector2 position, Material material)
            {
                var go = new GameObject(name); go.transform.SetParent(root.transform, false);
                go.transform.localPosition = ObliqueProjection.Point(position);
                var mesh = new Mesh {name = name}; assets.Add(mesh);
                mesh.SetVertices(data.Vertices); mesh.SetColors(data.Colors); mesh.SetTriangles(data.Triangles, 0);
                mesh.SetUVs(0, data.UVs); mesh.SetUVs(1, data.Surfaces); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                if (name == "Castelo do tirano") ObliqueScenery.CastleTorches(PrototypeCityLayout.Buildings.First(b => b.Name == name).Size, go.transform, fire);
            }
            Model("Ground", CityGroundArt.Build(), Vector2.zero, ground);
            foreach (var block in PrototypeCityLayout.Blocks.Where(b => b.Name.StartsWith("Ponte de pedra") || b.Name == "Patio da entrada do castelo"))
                Model(block.Name, CityPeripheryArt.Block(block), block.Position, stone);
            foreach (var building in PrototypeCityLayout.Buildings.Where(b => CityPeripheryArt.IsRoyalCastle(b.Name)))
                Model(building.Name, CastleFortressArt.Building(building.Name, building.Size).Body, building.Position, stone);
            var flames = root.GetComponentsInChildren<RuinsFlameAnimation>();
            Check(flames.Length == 2, "Exactly two castle torches");
            Check(flames[0].CurrentFrame == 0 && flames[1].CurrentFrame == 3, "Independent initial flame phases");
            flames[1].gameObject.SetActive(false); flames[1].gameObject.SetActive(true);
            Check(flames[1].CurrentFrame == 3, "Flame phase survives city reactivation");
            foreach (var flame in flames)
            {
                var renderer = flame.GetComponent<SpriteRenderer>();
                Vector3 anchor = flame.transform.position;
                for (int frame = 0; frame < RuinsFlameAnimation.FrameCount; frame++)
                {
                    flame.ShowAt((frame + .1f) / RuinsFlameAnimation.FramesPerSecond);
                    Check(flame.CurrentFrame == frame && renderer.sprite != null, "All eight sprites are used");
                    Check(flame.transform.position == anchor, "Flame anchor remains fixed");
                }
                Check(renderer.sharedMaterial.shader.name == "DragonQuest/PixelCharacter", "Flame uses depth-tested sprite material");
                Check(renderer.sprite.texture.filterMode == FilterMode.Point, "Point filtering preserved");
                Check(Vector3.Dot(flame.transform.up, Vector3.back) > .999f, "Flame is vertical in the world");
            }
            var cameraGo = new GameObject("Castle preview camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic = true; camera.aspect = 16f/9; camera.nearClipPlane = .1f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .15f, .2f);
            camera.transform.rotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees, Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees, 0, 0);
            target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
            void Capture(string name, Vector2 focus, float height, float size)
            {
                camera.orthographicSize = size;
                camera.transform.position = ObliqueProjection.Point(focus, height) - camera.transform.forward * ObliqueProjection.Distance;
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                try
                {
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(pixels); }
            }
            flames[0].ShowAt(0); flames[1].ShowAt(.31f);
            Capture("castle-overview", new Vector2(0, 18), 3.6f, 6.5f);
            foreach (float x in new[] {-2f, 0f, 2f})
                Capture("castle-approach-" + x, new Vector2(x, 13), ObliqueProjection.HeroCenterHeight, 6);
            for (int frame = 0; frame < RuinsFlameAnimation.FrameCount; frame++)
            {
                flames[0].ShowAt((frame + .1f) / RuinsFlameAnimation.FramesPerSecond);
                flames[1].ShowAt(((frame + 3) % 8 + .1f) / RuinsFlameAnimation.FramesPerSecond);
                Capture("castle-gate-" + frame, new Vector2(0, 16.5f), 2.2f, 2.6f);
            }
            string result = checks + " native castle checks passed; 12 previews in " + output;
            File.WriteAllText(Path.Combine(output, "validation.txt"), result);
            Debug.Log(result);
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            foreach (var asset in assets) if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}