using UnityEngine;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Cenário descartável para validar exploração antes dos tiles e sprites finais.
    public sealed class PrototypeCity : MonoBehaviour
    {
        private static readonly Rect Bounds = new Rect(-18f, -12f, 36f, 24f);
        private Texture2D squareTexture;
        private Sprite squareSprite;
        private PhysicsMaterial2D frictionless;

        private void Awake()
        {
            squareTexture = new Texture2D(1, 1) { name = "PrototypePixel", filterMode = FilterMode.Point };
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();
            squareSprite = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            frictionless = new PhysicsMaterial2D("PrototypeFrictionless") { friction = 0f, bounciness = 0f };

            Transform ground = Group("Chao e ruas");
            Transform buildings = Group("Predios - entradas na parte 2");
            Transform obstacles = Group("Obstaculos e limites");

            Block("Gramado", Vector2.zero, new Vector2(36, 24), "45634D", ground, false, -10);
            Block("Rua norte-sul", Vector2.zero, new Vector2(3.5f, 22), "C2AC80", ground, false, -9);
            Block("Rua leste-oeste", Vector2.zero, new Vector2(35, 3.5f), "C2AC80", ground, false, -9);
            Block("Praca", Vector2.zero, new Vector2(8, 7), "D1C19B", ground, false, -8);

            Building("Loja de itens", new Vector2(-9, 4.5f), new Vector2(5, 4), "A86B49", buildings);
            Building("Loja de magias", new Vector2(9, 4.5f), new Vector2(5, 4), "71638E", buildings);
            Building("Hospedaria", new Vector2(-9, -5), new Vector2(5, 4), "7F8D68", buildings);
            Building("Armaduras", new Vector2(9, -5), new Vector2(5, 4), "627C8C", buildings);
            Building("Castelo do tirano", new Vector2(0, 9), new Vector2(7, 4), "636B79", buildings);

            Block("Fonte - borda", Vector2.zero, new Vector2(2.6f, 2.6f), "747A80", obstacles, true, 2);
            Block("Fonte - agua", Vector2.zero, new Vector2(2.05f, 2.05f), "5BA5B6", obstacles, false, 3);
            Block("Fonte - centro", Vector2.zero, new Vector2(0.6f, 0.6f), "C8D2CB", obstacles, false, 4);
            Block("Banco oeste", new Vector2(-3, 0), new Vector2(0.6f, 1.6f), "805E43", obstacles, true, 2);
            Block("Banco leste", new Vector2(3, 0), new Vector2(0.6f, 1.6f), "805E43", obstacles, true, 2);

            Block("Muralha norte", new Vector2(0, 11.7f), new Vector2(36, 0.6f), "434B55", obstacles, true, 5);
            Block("Muralha sul", new Vector2(0, -11.7f), new Vector2(36, 0.6f), "434B55", obstacles, true, 5);
            Block("Muralha oeste", new Vector2(-17.7f, 0), new Vector2(0.6f, 24), "434B55", obstacles, true, 5);
            Block("Muralha leste", new Vector2(17.7f, 0), new Vector2(0.6f, 24), "434B55", obstacles, true, 5);

            foreach (Vector2 position in new[] { new Vector2(-14, 8), new Vector2(14, 8), new Vector2(-14, -8), new Vector2(14, -8) })
            {
                Block("Arvore - copa", position, new Vector2(1.8f, 1.8f), "294936", obstacles, true, 2);
                Block("Arvore - detalhe", position + new Vector2(-0.15f, 0.15f), new Vector2(1.1f, 1.1f), "3A6141", obstacles, false, 3);
            }

            GameObject player = CreatePlayer();
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("CidadePrototipo precisa de uma camera com a tag MainCamera.", this);
                return;
            }
            camera.backgroundColor = Hex("263E35");
            camera.gameObject.AddComponent<CameraFollow>().Initialize(player.transform, Bounds);
            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(camera);
        }

        private Transform Group(string groupName)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(transform, false);
            return group.transform;
        }

        private void Building(string buildingName, Vector2 position, Vector2 size, string roofColor, Transform parent)
        {
            Block(buildingName, position, size, "D8C9A5", parent, true, 1);
            Block(buildingName + " - telhado", position + Vector2.up * 0.35f,
                new Vector2(size.x + 0.25f, size.y - 1.1f), roofColor, parent, false, 2);
            Block(buildingName + " - porta fechada", position + Vector2.down * (size.y * 0.5f - 0.4f),
                new Vector2(0.85f, 0.8f), "41382F", parent, false, 3);
            Block(buildingName + " - janela oeste", position + new Vector2(-1.5f, -0.65f),
                new Vector2(0.55f, 0.6f), "E6C97C", parent, false, 3);
            Block(buildingName + " - janela leste", position + new Vector2(1.5f, -0.65f),
                new Vector2(0.55f, 0.6f), "E6C97C", parent, false, 3);
        }

        private GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Heroi");
            player.transform.SetParent(transform, false);
            player.transform.position = new Vector3(0, -4, 0);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.6f);
            collider.sharedMaterial = frictionless;
            player.AddComponent<PlayerMovement>();

            // As partes visuais são filhas; a escala do objeto físico permanece 1.
            Block("Sombra", Vector2.zero, new Vector2(0.85f, 0.65f), "334638", player.transform, false, 9).transform.localPosition = new Vector3(0, -0.2f, 0);
            Block("Capa", Vector2.zero, new Vector2(0.75f, 0.8f), "BB6949", player.transform, false, 10);
            Block("Tunica", Vector2.zero, new Vector2(0.55f, 0.65f), "DDBA59", player.transform, false, 11);
            Block("Cabeca", Vector2.zero, new Vector2(0.5f, 0.4f), "E9CC9D", player.transform, false, 12).transform.localPosition = new Vector3(0, 0.35f, 0);
            Block("Cabelo", Vector2.zero, new Vector2(0.52f, 0.15f), "4B3832", player.transform, false, 13).transform.localPosition = new Vector3(0, 0.5f, 0);
            return player;
        }

        private GameObject Block(string blockName, Vector2 position, Vector2 size, string color,
            Transform parent, bool solid, int sortingOrder)
        {
            GameObject block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = Hex(color);
            renderer.sortingOrder = sortingOrder;
            if (solid)
            {
                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.sharedMaterial = frictionless;
            }
            return block;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private void OnDestroy()
        {
            if (squareSprite != null) Destroy(squareSprite);
            if (squareTexture != null) Destroy(squareTexture);
            if (frictionless != null) Destroy(frictionless);
        }
    }
}
