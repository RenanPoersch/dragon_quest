using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class QuarryPixelScenery : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Material propMaterial;
        private Shader runeShader;
        private const float TotemHeight = 2.65f;

        public void Build(AncientSecrets progress, List<Interactable> interactions, PhysicsMaterial2D physics = null)
        {
            Texture2D backdrop = Resources.Load<Texture2D>("PixelArt/QuarryBackdrop");
            Texture2D atlas = Resources.Load<Texture2D>("PixelArt/QuarryTotems");
            Shader backgroundShader = Resources.Load<Shader>("ForestPixelBackdrop");
            Shader propShader = Resources.Load<Shader>("PixelCharacter");
            runeShader = Resources.Load<Shader>("QuarryRune");
            if (backdrop == null || atlas == null || backgroundShader == null || propShader == null || runeShader == null)
                throw new InvalidOperationException("Cenario pixel art da pedreira ausente.");
            var painting = new GameObject("Pedreira / paredes de rocha, musgo e luz da floresta");
            painting.transform.SetParent(transform, false);
            Quaternion cameraRotation = Quaternion.AngleAxis(ObliqueProjection.YawDegrees, Vector3.forward)
                * Quaternion.Euler(-ObliqueProjection.TiltDegrees, 0, 0);
            painting.transform.localRotation = cameraRotation;
            painting.transform.localPosition = cameraRotation * Vector3.forward * 20;
            var mesh = new Mesh { name = "Pedreira / pintura continua" };
            mesh.vertices = new[] { new Vector3(-128f/9,-8,0), new Vector3(-128f/9,8,0),
                new Vector3(128f/9,8,0), new Vector3(128f/9,-8,0) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateBounds(); owned.Add(mesh);
            var material = new Material(backgroundShader) { name = "Pedreira / pixel art", mainTexture = backdrop };
            owned.Add(material);
            painting.AddComponent<MeshFilter>().sharedMesh = mesh;
            painting.AddComponent<MeshRenderer>().sharedMaterial = material;
            propMaterial = new Material(propShader) { name = "Totens de rocha / profundidade pixel art" };
            owned.Add(propMaterial);
            Quaternion upright = Quaternion.LookRotation(PixelSpriteLayout.HorizontalCameraForward(cameraRotation * Vector3.forward),
                PixelSpriteLayout.UprightUp);
            for (int i = 0; i < QuarryLayout.Monoliths.Count; i++)
            {
                CityBlock block = QuarryLayout.Monoliths[i];
                MonolithSymbol symbol = (MonolithSymbol)i;
                GameObject anchor = Anchor(block.Name, block.Position);
                AddCollider(anchor, block.Size, physics);
                var visual = new GameObject("Totem de rocha pixel art / " + symbol);
                visual.transform.SetParent(anchor.transform, false);
                visual.transform.localRotation = upright;
                visual.transform.localScale = new Vector3(1, PixelSpriteLayout.UprightScaleY, 1);
                // Atlas order is Moon, Star, Sun; all feet are aligned to their physical anchor.
                int column = symbol == MonolithSymbol.Moon ? 0 : symbol == MonolithSymbol.Star ? 1 : 2;
                Rect frame = FrameBounds(atlas, column);
                Sprite stone = Sprite.Create(atlas, frame, new Vector2(.5f,0), frame.height/TotemHeight, 0, SpriteMeshType.FullRect);
                owned.Add(stone);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = propMaterial; renderer.sprite = stone;
                float runeX = column == 0 ? .01f : column == 1 ? -.053f : -.067f;
                float runeY = TotemHeight * (column == 1 ? .577f : column == 2 ? .559f : .545f);
                GameObject glow = DrawRune(visual.transform, null, Color.white, "Luz do totem / " + symbol, -.025f, runeX, runeY);
                var animation = glow.AddComponent<QuarryRuneAnimation>();
                animation.Initialize(symbol);
                GameObject engraving = DrawRune(visual.transform, animation.DormantSprite, new Color(.66f,.64f,.58f), "Simbolo gravado", -.01f, runeX, runeY, 1);
                animation.BindEngraving(engraving.GetComponent<SpriteRenderer>());
                var interaction = anchor.AddComponent<AncientSecretInteraction>();
                interaction.Initialize(SecretObject.Monolith, progress, symbol, glow);
                interactions.Add(interaction);
            }
            CityBlock plaque = QuarryLayout.Obstacles[QuarryLayout.Obstacles.Count-1];
            GameObject inscription = Anchor(plaque.Name, plaque.Position);
            AddCollider(inscription, plaque.Size, physics);
            var reading = inscription.AddComponent<AncientSecretInteraction>();
            reading.Initialize(SecretObject.Inscription, progress); interactions.Add(reading);
        }

        private GameObject Anchor(string name, Vector2 ground)
        {
            var anchor = new GameObject(name); anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = ground; return anchor;
        }

        private static void AddCollider(GameObject anchor, Vector2 size, PhysicsMaterial2D physics)
        {
            var collider = anchor.AddComponent<BoxCollider2D>(); collider.size = size; collider.sharedMaterial = physics;
        }

        private static Rect FrameBounds(Texture2D atlas, int column)
        {
            int left = column*atlas.width/3, right = (column+1)*atlas.width/3;
            int minX = right, minY = atlas.height, maxX = left, maxY = 0;
            Color32[] pixels = atlas.GetPixels32();
            for (int y = 0; y < atlas.height; y++)
                for (int x = left; x < right; x++)
                    if (pixels[y*atlas.width+x].a > 127)
                    { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
            if (maxX <= minX || maxY <= minY) throw new InvalidOperationException("Totem ausente no atlas: " + column);
            return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
        }

        private GameObject DrawRune(Transform parent, Sprite rune, Color color, string name, float depth, float x, float y, float desaturate = 0)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x,y,depth);
            var renderer = obj.AddComponent<SpriteRenderer>();
            var material = new Material(runeShader) { name = name + " / cor" };
            material.SetColor("_Tint",color); material.SetFloat("_Desaturate",desaturate); owned.Add(material);
            renderer.sharedMaterial = material; renderer.sprite = rune;
            return obj;
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned)
                if (item != null) { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
        }
    }
}
