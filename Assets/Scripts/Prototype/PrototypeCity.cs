using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;

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

            Transform city = Group("Cidade");
            Transform ground = Group("Chao e ruas", city);
            Transform buildings = Group("Predios", city);
            Transform obstacles = Group("Obstaculos e limites", city);

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

            var interactables = new List<Interactable>();
            Transform interactions = Group("NPC e bau", city);
            NpcInteraction citizen = CreateNpc("Aldeao", new Vector2(-3.4f, -3), "8D9DB0", interactions);
            citizen.Initialize("Aldeao",
                "Bem-vindo! A loja de itens fica a noroeste da praca. Aproxime-se da porta e pressione E para entrar.",
                "Ha um bau a leste daqui. Talvez voce encontre moedas para sua jornada.",
                "O tirano governa do castelo ao norte. Alguns dizem que os segredos desta cidade escondem algo ainda maior...");
            interactables.Add(citizen);
            interactables.Add(CreateChest(interactions));
            interactables.Add(CreateDoor("Entrada da loja de itens", new Vector2(-9, 2.1f), city, "item-shop", "Entrar na loja de itens"));

            Transform shop = Group("Loja de itens - interior");
            CreateShop(shop, interactables);
            shop.gameObject.SetActive(false);

            GameObject player = CreatePlayer();
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("CidadePrototipo precisa de uma camera com a tag MainCamera.", this);
                return;
            }
            camera.backgroundColor = Hex("263E35");
            CameraFollow cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            cameraFollow.Initialize(player.transform, Bounds);
            var progress = new ExplorationProgress();
            DialogueController dialogue = gameObject.AddComponent<DialogueController>();
            PrototypeLocations locations = gameObject.AddComponent<PrototypeLocations>();
            locations.Initialize(city.gameObject, shop.gameObject, player.GetComponent<PlayerMovement>(), cameraFollow);
            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            interaction.Initialize(new InteractionContext(dialogue, progress, locations.TravelTo), interactables.ToArray());
            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(camera, interaction, dialogue, progress, locations);
        }

        private Transform Group(string groupName, Transform parent = null)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(parent != null ? parent : transform, false);
            return group.transform;
        }

        private GameObject Entity(string entityName, Vector2 position, Transform parent)
        {
            GameObject entity = new GameObject(entityName);
            entity.transform.SetParent(parent, false);
            entity.transform.localPosition = position;
            return entity;
        }

        private NpcInteraction CreateNpc(string npcName, Vector2 position, string color, Transform parent)
        {
            GameObject npc = Entity(npcName, position, parent);
            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.6f);
            collider.sharedMaterial = frictionless;
            Block(npcName + " - corpo", Vector2.zero, new Vector2(0.65f, 0.75f), color, npc.transform, false, 10);
            Block(npcName + " - cabeca", new Vector2(0, 0.4f), new Vector2(0.5f, 0.4f), "E9CC9D", npc.transform, false, 11);
            return npc.AddComponent<NpcInteraction>();
        }

        private ChestInteraction CreateChest(Transform parent)
        {
            GameObject chest = Entity("Bau da praca", new Vector2(3.6f, -3.2f), parent);
            BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.7f);
            collider.sharedMaterial = frictionless;
            Block("Bau - base", Vector2.zero, new Vector2(0.9f, 0.7f), "8A5E37", chest.transform, false, 5);
            GameObject lid = Block("Bau - tampa", new Vector2(0, 0.15f), new Vector2(1f, 0.32f), "C99549", chest.transform, false, 6);
            Block("Bau - fecho", Vector2.zero, new Vector2(0.16f, 0.28f), "F3D378", chest.transform, false, 7);
            ChestInteraction interaction = chest.AddComponent<ChestInteraction>();
            interaction.Initialize("praca-primeiro-bau", 25, lid.GetComponent<SpriteRenderer>());
            return interaction;
        }

        private DoorInteraction CreateDoor(string doorName, Vector2 position, Transform parent, string destination, string prompt)
        {
            GameObject door = Entity(doorName, position, parent);
            Block(doorName + " - marca", Vector2.zero, new Vector2(0.9f, 0.14f), "E4C36E", door.transform, false, 4);
            DoorInteraction interaction = door.AddComponent<DoorInteraction>();
            interaction.Initialize(destination, prompt);
            return interaction;
        }

        private void CreateShop(Transform shop, List<Interactable> interactions)
        {
            Block("Piso de madeira", Vector2.zero, new Vector2(14, 10), "A78255", shop, false, -10);
            for (int y = -4; y <= 4; y++)
                Block("Junta do piso", new Vector2(0, y), new Vector2(14, 0.04f), "80613F", shop, false, -9);
            Block("Tapete", new Vector2(0, -2), new Vector2(3, 4), "8F5557", shop, false, -8);
            Block("Parede norte", new Vector2(0, 4.7f), new Vector2(14, 0.6f), "5A4635", shop, true, 5);
            Block("Parede sul", new Vector2(0, -4.7f), new Vector2(14, 0.6f), "5A4635", shop, true, 5);
            Block("Parede oeste", new Vector2(-6.7f, 0), new Vector2(0.6f, 10), "5A4635", shop, true, 5);
            Block("Parede leste", new Vector2(6.7f, 0), new Vector2(0.6f, 10), "5A4635", shop, true, 5);
            Block("Balcao", new Vector2(0, 0.15f), new Vector2(4, 0.6f), "614533", shop, true, 4);
            foreach (float x in new[] { -4.8f, 4.8f })
            {
                Block("Estante", new Vector2(x, 1.5f), new Vector2(1.5f, 3), "705033", shop, true, 3);
                for (int shelf = 0; shelf < 3; shelf++)
                    Block("Frasco", new Vector2(x, 0.7f + shelf * 0.8f), new Vector2(0.4f, 0.4f), "87B5A3", shop, false, 4);
            }
            NpcInteraction merchant = CreateNpc("Lojista", new Vector2(0, 0.9f), "8DA17F", shop);
            merchant.Initialize("Lojista",
                "Bem-vindo a loja de itens! Pocoes e suprimentos vao ajudar sua equipe nas batalhas.",
                "Ainda estou organizando os estoques. Volte mais tarde para negociar.",
                "Para voltar a cidade, aproxime-se da marca dourada ao sul e pressione E.");
            interactions.Add(merchant);
            interactions.Add(CreateDoor("Saida da loja", new Vector2(0, -4.1f), shop, "city", "Voltar para a cidade"));
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
