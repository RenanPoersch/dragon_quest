using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // A mobilia usa sprites com profundidade; o chao e as colisoes continuam em XY.
    public sealed class InnPixelScenery : MonoBehaviour
    {
        public const int CellPixels = 128, Columns = 4, Rows = 2;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private static readonly RectInt[] Regions = {
            new RectInt(16,136,365,365), new RectInt(390,40,362,444),
            new RectInt(764,158,440,352), new RectInt(1227,38,279,472),
            new RectInt(26,622,435,337), new RectInt(474,525,343,429),
            new RectInt(917,555,200,375), new RectInt(1196,548,304,413)
        };
        private static readonly string[] Names = {
            "Cama com colcha bordada", "Lareira de pedra", "Recepcao e livro de hospedes",
            "Armario de enxoval", "Mesa do cafe", "Janela e cortinas", "Lanterna de parede", "Samambaia"
        };

        public void Build(PhysicsMaterial2D physics = null)
        {
            var source = Resources.Load<Texture2D>("PixelArt/InnPropsAtlas");
            var materials = Resources.Load<Texture2D>("PixelArt/MateriaShopAtlas");
            var shellShader = Resources.Load<Shader>("MateriaShop");
            var propShader = Resources.Load<Shader>("PixelCharacter");
            if (source == null || materials == null || shellShader == null || propShader == null)
                throw new InvalidOperationException("Arte pixel art da Pousada ausente.");

            var roomMaterial = new Material(shellShader) { name = "Pousada / madeira e tecido" };
            roomMaterial.SetTexture("_PixelAtlas", materials); owned.Add(roomMaterial);
            var room = new GameObject("Pousada / assoalho, paredes e tapetes");
            room.transform.SetParent(transform, false);
            ObliqueMeshData data = Shell();
            var mesh = new Mesh { name = room.name };
            mesh.SetVertices(data.Vertices); mesh.SetColors(data.Colors); mesh.SetTriangles(data.Triangles, 0);
            mesh.SetUVs(0, data.UVs); mesh.SetUVs(1, data.Surfaces); mesh.RecalculateBounds(); owned.Add(mesh);
            room.AddComponent<MeshFilter>().sharedMesh = mesh;
            room.AddComponent<MeshRenderer>().sharedMaterial = roomMaterial;

            var texture = new Texture2D(Columns * CellPixels, Rows * CellPixels, TextureFormat.RGBA32, false) {
                name = "Pousada / grade logica de 128 pixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            Color32[] sampled = SampleAtlas(source.GetPixels32(), source.width, source.height);
            texture.SetPixels32(sampled); texture.Apply(false, false); owned.Add(texture);
            var propMaterial = new Material(propShader) { name = "Pousada / sprites pixel art com profundidade" };
            owned.Add(propMaterial);
            var sprites = new Sprite[Names.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                RectInt r = SpriteBounds(i, sampled);
                sprites[i] = Sprite.Create(texture, new Rect(r.x, r.y, r.width, r.height), new Vector2(.5f, 0), CellPixels, 0, SpriteMeshType.FullRect);
                sprites[i].name = Names[i]; owned.Add(sprites[i]);
            }
            foreach (CityBlock block in PrototypeInnLayout.Blocks)
            {
                if (!block.Solid) continue;
                var anchor = new GameObject(block.Name); anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = block.Position;
                var collider = anchor.AddComponent<BoxCollider2D>(); collider.size = block.Size; collider.sharedMaterial = physics;
            }

            foreach (float x in new[] { -4f, 0f, 4f }) Prop(sprites[0], propMaterial, x, 1.3f, 2.3f, 2.3f);
            Prop(sprites[1], propMaterial, -5.1f, -.8f, 2.25f, 2.8f);
            Prop(sprites[2], propMaterial, 4.4f, -1.7f, 3.05f, 2.05f);
            Prop(sprites[3], propMaterial, 5.65f, .85f, 1.65f, 2.65f);
            Prop(sprites[4], propMaterial, -3.65f, -3.2f, 2.65f, 2.1f);
            foreach (float x in new[] { -4f, 0f, 4f }) Prop(sprites[5], propMaterial, x, 4.34f, 1.35f, 1.55f, .95f);
            foreach (float x in new[] { -6f, -2f, 2f, 6f }) Prop(sprites[6], propMaterial, x, 4.32f, .38f, .72f, 1.9f);
            Prop(sprites[7], propMaterial, -5.8f, -3.6f, .95f, 1.25f);
            Prop(sprites[7], propMaterial, 5.8f, -3.6f, .95f, 1.25f);
        }

        private void Prop(Sprite sprite, Material material, float x, float y, float width, float height, float altitude = 0)
        {
            var anchor = new GameObject(sprite.name); anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = new Vector3(x, y, -altitude);
            var visual = new GameObject("Arte / " + sprite.name); visual.transform.SetParent(anchor.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
            visual.transform.localRotation = Quaternion.LookRotation(
                new Vector3(-ObliqueProjection.YawSin, ObliqueProjection.YawCos, 0), PixelSpriteLayout.UprightUp);
            float scale = Math.Min(width / sprite.bounds.size.x, height / sprite.bounds.size.y);
            visual.transform.localScale = new Vector3(scale, scale * PixelSpriteLayout.UprightScaleY, 1);
        }

        // Recortes e registro em memoria; o PNG de origem fica preservado.
        public static Color32[] SampleAtlas(Color32[] source, int width, int height)
        {
            if (source == null || width != 1536 || height != 1024 || source.Length != width * height)
                throw new ArgumentException("Dimensoes do atlas da Pousada invalidas.");
            var pixels = new Color32[Columns * Rows * CellPixels * CellPixels];
            for (int i = 0; i < Regions.Length; i++)
            {
                RectInt r = Regions[i];
                float scale = Math.Min((CellPixels - 6f) / r.width, (CellPixels - 6f) / r.height);
                int dw = Math.Max(1, (int)Math.Round(r.width * scale)), dh = Math.Max(1, (int)Math.Round(r.height * scale));
                int ox = i % Columns * CellPixels + (CellPixels - dw) / 2;
                int oy = (Rows - 1 - i / Columns) * CellPixels + 3;
                for (int y = 0; y < dh; y++) for (int x = 0; x < dw; x++)
                {
                    int sx = r.x + Math.Min(r.width - 1, (int)((x + .5f) * r.width / dw));
                    int sy = r.y + Math.Min(r.height - 1, (int)((y + .5f) * r.height / dh));
                    Color32 color = source[(height - 1 - sy) * width + sx];
                    if (color.a < 128) continue;
                    color.a = 255; pixels[(oy + dh - 1 - y) * Columns * CellPixels + ox + x] = color;
                }
            }
            return pixels;
        }

        public static RectInt SpriteBounds(int index, Color32[] pixels)
        {
            if (index < 0 || index >= Regions.Length || pixels == null || pixels.Length != Columns * Rows * CellPixels * CellPixels)
                throw new ArgumentException("Sprite da Pousada invalido.");
            int left = index % Columns * CellPixels, bottom = (Rows - 1 - index / Columns) * CellPixels;
            int minX = left + CellPixels, minY = bottom + CellPixels, maxX = left - 1, maxY = bottom - 1;
            for (int y = bottom; y < bottom + CellPixels; y++) for (int x = left; x < left + CellPixels; x++)
                if (pixels[y * Columns * CellPixels + x].a >= 128)
                { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
            if (maxX < minX) throw new InvalidOperationException("Sprite vazio na Pousada: " + index);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public static ObliqueMeshData Shell()
        {
            var m = new ObliqueMeshData();
            m.DefaultSurface = (PixelSurface)7; m.Plane(0, 0, 14, 10, .02f, Color.white);
            m.DefaultSurface = PixelSurface.None;
            m.Box(0, 4.7f, 14, .6f, 0, 2.85f, ObliqueMeshData.Hex("C3A77B"));
            m.DefaultSurface = (PixelSurface)0;
            foreach (float x in new[] { -6.7f, 6.7f }) m.Box(x, 0, .6f, 10, 0, .4f, Color.white);
            foreach (float x in new[] { -3.95f, 3.95f }) m.Box(x, -4.7f, 6.1f, .6f, 0, .18f, Color.white);
            foreach (float x in new[] { -6.25f, -2f, 2f, 6.25f }) m.Box(x, 4.32f, .14f, .15f, 0, 2.91f, Color.white);
            m.Box(0, 4.29f, 13.4f, .18f, 2.72f, 2.91f, Color.white);
            m.Box(0, 4.3f, 13.4f, .14f, .16f, .32f, Color.white);
            // Lambris de madeira abaixo das janelas.
            for (int i = 0; i < 34; i++) m.Box(-6.3f + i * .38f, 4.36f, .35f, .08f, .33f, .85f, Color.white);
            m.Box(0, 4.29f, 13.4f, .13f, .82f, .94f, Color.white);
            m.DefaultSurface = PixelSurface.None;
            Color gold = ObliqueMeshData.Hex("C79B5C"), teal = ObliqueMeshData.Hex("315A5A"), thread = ObliqueMeshData.Hex("719589");
            m.Plane(0, -2.1f, 2.8f, 4.65f, .035f, gold);
            m.Plane(0, -2.1f, 2.58f, 4.43f, .04f, teal);
            foreach (float x in new[] { -1.12f, 1.12f }) m.Plane(x, -2.1f, .035f, 4.08f, .045f, thread);
            foreach (float y in new[] { -4.13f, -.07f }) m.Plane(0, y, 2.25f, .035f, .045f, thread);
            for (int i = 0; i < 14; i++) foreach (float y in new[] { -4.48f, .28f })
                m.Plane(-1.25f + i * .19f, y, .045f, .18f, .04f, gold);
            foreach (float x in new[] { -4f, 0f, 4f })
            {
                m.Plane(x, 1.05f, 1.95f, .55f, .03f, gold);
                m.Plane(x, 1.05f, 1.8f, .4f, .035f, teal);
            }
            // Soleira livre no centro da parede sul: acompanha a saida existente.
            m.DefaultSurface = (PixelSurface)0; m.Plane(0, -4.62f, 1.8f, .76f, .03f, Color.white);
            return m;
        }

        private void OnDestroy()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) { if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
        }
    }
}
