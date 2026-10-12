using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    // Monta mapas, fisica e interacoes; a apresentacao 2.5D fica em ObliqueScenery.
    public sealed class PrototypeCity : MonoBehaviour
    {
        private ObliqueScenery scenery;
        private PhysicsMaterial2D frictionless;
        private readonly AncientSecrets secrets = new AncientSecrets();
        private PrototypeTravel navigation;
        private PrototypeBattleController battle;
        private PrototypeSaveController saves;
        private ExplorationProgress progress;
        private readonly DragonProgress dragon = new DragonProgress();

        private void Awake()
        {
            saves = gameObject.AddComponent<PrototypeSaveController>();
            navigation = new PrototypeTravel(secrets);
            scenery = new ObliqueScenery();
            frictionless = new PhysicsMaterial2D("PrototypeFrictionless") { friction = 0f, bounciness = 0f };

            Transform city = Group("Periferia do castelo");
            Transform ground = Group("Chao e ruas", city);
            Transform buildings = Group("Predios", city);
            Transform obstacles = Group("Obstaculos e limites", city);

            foreach (CityBlock block in PrototypeCityLayout.Blocks)
                Block(block.Name, block.Position, block.Size, block.Color,
                    block.Solid || block.Order >= 0 ? obstacles : ground, block.Solid, block.Order);
            foreach (CityBlock building in PrototypeCityLayout.Buildings)
                Building(building.Name, building.Position, building.Size, building.Color, buildings);

            var interactables = new List<Interactable>();
            Transform interactions = Group("Moradores e bau", city);
            NpcInteraction citizen = CreateNpc("Aldeao", PrototypeCityLayout.Citizen, "8D9DB0", interactions);
            citizen.Initialize("Aldeao",
                "Bem-vindo a periferia do castelo! Itens a noroeste, materias a nordeste e equipamentos a sudeste. Caminhe ate a porta para entrar e use E no vendedor.",
                "Ha um bau a leste daqui. Talvez voce encontre moedas para sua jornada.",
                "O bairro residencial fica a oeste, os jardins a nordeste e as ruinas a sudeste. Ao sul, tres pontes atravessam o rio.",
                "O tirano governa do castelo ao norte. Alguns dizem que os segredos desta cidade escondem algo ainda maior...");
            interactables.Add(citizen);
            GameObject peasant = CreateNpcFigure("Camponesa", PrototypeCityLayout.Resident, "B77D8B", interactions);
            interactables.Add(AddSecret(peasant, SecretObject.Peasant));
            NpcInteraction gardener = CreateNpc("Jardineiro", PrototypeCityLayout.Gardener, "86A96B", interactions);
            gardener.Initialize("Jardineiro", "Os jardins ja foram abertos a todos. Hoje quase ninguem caminha por aqui.",
                "Seguindo esta rua para o sul voce atravessa a ponte leste e chega as ruinas antigas.");
            interactables.Add(gardener);
            NpcInteraction explorer = CreateNpc("Exploradora", PrototypeCityLayout.Explorer, "B79458", interactions);
            explorer.Initialize("Exploradora", "Estas ruinas existiam antes do castelo. Repare no pedestal entre as paredes quebradas.",
                "Encontrei marcas de escamas nas pedras. As lendas falam de um dragao, mas ele nunca aparece para qualquer viajante.");
            interactables.Add(explorer);
            interactables.Add(CreateChest(interactions));
            CreatePassage("Entrada da loja de itens", PrototypeTravel.City, city, "item-shop");
            CreatePassage("Entrada da loja de materias", PrototypeTravel.City, city, "materia-shop");
            CreatePassage("Entrada da loja de equipamentos", PrototypeTravel.City, city, "equipment-shop");
            CreatePassage("Portao para a ilha", PrototypeTravel.City, city, PrototypeTravel.World);
            CreatePassage("Entrada do castelo", PrototypeTravel.City, city, PrototypeTravel.Castle);
            CreatePassage("Entrada da hospedaria",PrototypeTravel.City,city,PrototypeTravel.Inn);
            GameObject guard = Entity("Guarda do tirano", PrototypeCityLayout.Guard, city);
            BoxCollider2D guardCollider = guard.AddComponent<BoxCollider2D>();
            guardCollider.size = new Vector2(0.65f, 0.7f);
            guardCollider.sharedMaterial = frictionless;
            scenery.PixelPerson(guard.transform, "NpcRoster", 10);
            interactables.Add(guard.AddComponent<BattleInteraction>());

            var mapRoots = new Dictionary<string, GameObject>
            {
                { PrototypeTravel.World, CreateWorld(interactables).gameObject },
                { PrototypeTravel.City, city.gameObject },
                { PrototypeTravel.Castle, CreateCastle(interactables).gameObject },
                { PrototypeTravel.WestForest, CreateForest(interactables, false).gameObject },
                { PrototypeTravel.EastForest, CreateForest(interactables, true).gameObject },
                { PrototypeTravel.Ruins, CreateRuins(interactables).gameObject },
                { PrototypeTravel.Volcano,CreateVolcano(interactables).gameObject },
                { PrototypeTravel.Inn,CreateInn(interactables).gameObject }
            };
            string[] shopIds = { "item-shop", "equipment-shop", "materia-shop" };
            string[] shopNames = { "Loja de itens", "Loja de equipamentos", "Loja de materias" };
            string[] shopColors = { "87B5A3", "91A6B3", "85CD90" };
            for (int i = 0; i < shopIds.Length; i++)
            {
                Transform shop = Group(shopNames[i] + " - interior");
                CreateShop(shop, interactables, shopIds[i], shopNames[i], shopColors[i]);
                mapRoots.Add(shopIds[i], shop.gameObject);
            }

            GameObject player = CreatePlayer();
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("CidadePrototipo precisa de uma camera com a tag MainCamera.", this);
                return;
            }
            camera.backgroundColor = Hex("263E35");
            CameraFollow cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            progress = new ExplorationProgress(PrototypeShopFactory.StartingGold);
            DialogueController dialogue = gameObject.AddComponent<DialogueController>();
            PrototypeLocations locations = gameObject.AddComponent<PrototypeLocations>();
            PrototypeShopController shops = gameObject.AddComponent<PrototypeShopController>();
            PrototypeEndingController endings=gameObject.AddComponent<PrototypeEndingController>();
            endings.Initialize(mapRoots,player.GetComponent<PlayerMovement>(),camera,cameraFollow,scenery);
            battle = gameObject.AddComponent<PrototypeBattleController>();
            battle.Initialize(player.GetComponent<PlayerMovement>(), dialogue, progress, () => shops.IsOpen || saves.IsMenuVisible,dragon,secrets,locations.RecoverAtInn,endings.Show);
            shops.Initialize(player.GetComponent<PlayerMovement>(), dialogue, battle, progress);
            locations.Initialize(mapRoots, player.GetComponent<PlayerMovement>(), cameraFollow, navigation,
                () => dialogue.IsOpen || battle.IsBusy);
            gameObject.AddComponent<PrototypeShopHud>().Initialize(shops);
            gameObject.AddComponent<PrototypeBattleHud>().Initialize(battle, scenery.PixelSprites);
            gameObject.AddComponent<PrototypeMateriaHud>().Initialize(battle, secrets);
            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            interaction.Initialize(new InteractionContext(dialogue, progress, locations.TravelTo,
                battle.StartBattle, () => battle.IsBusy, shops.Open, secrets), interactables.ToArray());
            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(camera, interaction, dialogue, progress, locations, battle, shops, secrets);
            saves.Initialize(player.GetComponent<PlayerMovement>(), battle, shops, locations, dialogue, progress, secrets, dragon, journey =>
            {
                progress = journey.Progress;
                var s = journey.Secrets;
                secrets.Restore(s.KnowsPoem, s.AltarSolved, s.MonolithStep, s.HasScale, s.PassageOpen);
                dragon.Restore(journey.Dragon.IsDefeated);
                battle.Initialize(player.GetComponent<PlayerMovement>(), dialogue, progress, () => shops.IsOpen || saves.IsMenuVisible,
                    dragon, secrets, locations.RecoverAtInn, endings.Show, journey.Party);
                shops.Initialize(player.GetComponent<PlayerMovement>(), dialogue, battle, progress, journey.Shops, journey.Quest);
                interaction.Initialize(new InteractionContext(dialogue, progress, locations.TravelTo,
                    battle.StartBattle, () => battle.IsBusy, shops.Open, secrets), interactables.ToArray());
                hud.Initialize(camera, interaction, dialogue, progress, locations, battle, shops, secrets);
                locations.Restore(journey.Data.Map, new Vector2(journey.Data.X, journey.Data.Y));
            });
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
            => CreateNpcFigure(npcName, position, color, parent).AddComponent<NpcInteraction>();

        private AncientSecretInteraction AddSecret(GameObject target, SecretObject kind,
            MonolithSymbol symbol = MonolithSymbol.Sun, GameObject light = null)
        {
            AncientSecretInteraction interaction = target.AddComponent<AncientSecretInteraction>();
            interaction.Initialize(kind, secrets, symbol, light); return interaction;
        }

        private GameObject CreateNpcFigure(string npcName, Vector2 position, string color, Transform parent)
        {
            GameObject npc = Entity(npcName, position, parent);
            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.6f);
            collider.sharedMaterial = frictionless;
            scenery.PixelPerson(npc.transform, "NpcRoster", PixelSpriteLibrary.NpcFrame(npcName));
            return npc;
        }

        private ChestInteraction CreateChest(Transform parent)
        {
            GameObject chest = Entity("Bau da praca", PrototypeCityLayout.Chest, parent);
            BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.7f);
            collider.sharedMaterial = frictionless;
            Transform lid = scenery.Chest(chest.transform);
            ChestInteraction interaction = chest.AddComponent<ChestInteraction>();
            interaction.Initialize("praca-primeiro-bau", 25, () => lid.localRotation = Quaternion.Euler(65, 0, 0),
                () => progress?.HasOpenedChest("praca-primeiro-bau") == true);
            return interaction;
        }

        private void CreatePassage(string doorName, string source, Transform parent, string destination)
        {
            Rect area = navigation.RouteFrom(source, destination).Entrance;
            GameObject door = Entity(doorName, area.center, parent);
            Block(doorName + " - marca", Vector2.zero, new Vector2(0.9f, 0.14f), "E4C36E", door.transform, false, 4);
        }

        private Transform CreateWorld(List<Interactable> interactions)
        {
            Transform world = Group("Ilha - mapa global");
            GameObject rocks = scenery.World(world);
            foreach (BoxCollider2D collider in world.GetComponentsInChildren<BoxCollider2D>()) collider.sharedMaterial = frictionless;
            GameObject coast = Entity("Costa - limite do mar", Vector2.zero, world);
            EdgeCollider2D shoreline = coast.AddComponent<EdgeCollider2D>();
            var perimeter = new Vector2[PrototypeWorldLayout.Coast.Count + 1];
            for (int i = 0; i < PrototypeWorldLayout.Coast.Count; i++) perimeter[i] = PrototypeWorldLayout.Coast[i];
            perimeter[perimeter.Length - 1] = perimeter[0];
            shoreline.points = perimeter;
            shoreline.edgeRadius = 0.08f;
            shoreline.sharedMaterial = frictionless;

            Transform castle = Entity("Castelo - icone global", PrototypeWorldLayout.Castle, world).transform;
            scenery.CastleIcon(castle);
            castle.GetComponent<BoxCollider2D>().sharedMaterial = frictionless;
            CreatePassage("Castelo e arredores", PrototypeTravel.World, world, PrototypeTravel.City);
            CreateForestIcon(world, PrototypeWorldLayout.WestForest);
            CreateForestIcon(world, PrototypeWorldLayout.EastForest);
            CreatePassage("Floresta oeste", PrototypeTravel.World, world, PrototypeTravel.WestForest);
            CreatePassage("Floresta leste", PrototypeTravel.World, world, PrototypeTravel.EastForest);

            Transform ruins = Entity("Ruinas - icone global",PrototypeWorldLayout.Ruins,world).transform;
            scenery.RuinsIcon(ruins);
            ruins.GetComponent<BoxCollider2D>().sharedMaterial=frictionless;
            CreatePassage("Ruinas antigas", PrototypeTravel.World, world, PrototypeTravel.Ruins);

            Transform volcano = Entity("Vulcao - icone global", PrototypeWorldLayout.Volcano, world).transform;
            scenery.VolcanoIcon(volcano);
            CreatePassage("Entrada do vulcao",PrototypeTravel.World,world,PrototypeTravel.Volcano);
            GameObject passage = Entity("Passagem oculta", PrototypeWorldLayout.Trail, world);
            BlockedPassageInteraction gate = passage.AddComponent<BlockedPassageInteraction>();
            gate.Initialize(rocks, secrets); interactions.Add(gate);
            return world;
        }

        private void CreateForestIcon(Transform world, Vector2 position)
        {
            Transform forest = Entity("Floresta - icone global", position, world).transform;
            scenery.ForestIcon(forest);
        }

        private Transform CreateForest(List<Interactable> interactions, bool eastern)
        {
            Transform forest = Group(eastern ? "Floresta leste - interior" : "Floresta oeste - interior");
            DrawBlocks(PrototypeWorldLayout.CreateForestBlocks(eastern), forest);
            CreatePassage("Saida da floresta", eastern ? PrototypeTravel.EastForest : PrototypeTravel.WestForest, forest, PrototypeTravel.World);
            NpcInteraction inhabitant = CreateNpc(eastern ? "Viajante" : "Lenhador", new Vector2(1.5f, 1), eastern ? "C19862" : "879C6B", forest);
            inhabitant.Initialize(eastern ? "Viajante" : "Lenhador", eastern
                ? "Encontrei marcas incomuns numa pedra ao norte. Nao parecem ter sido feitas por nenhum animal que eu conheca."
                : "Os tres monolitos sao mais antigos que estas arvores. Uma inscricao sobrevive na pedra perto deles.", eastern
                ? "Minha familia dizia que a floresta guarda aquilo que os homens esquecem. Nunca entendi muito bem."
                : "Uma camponesa da periferia ainda canta as palavras dos antigos. Talvez voce tambem as conheca.");
            interactions.Add(inhabitant);
            if (!eastern)
            {
                for (int i=0; i<PrototypeSecretLayout.WestMonoliths.Count; i++)
                {
                    CityBlock block=PrototypeSecretLayout.WestMonoliths[i];
                    Transform monolith=forest.Find(block.Name);
                    MonolithSymbol symbol=PrototypeSecretLayout.SymbolAt(i);
                    interactions.Add(AddSecret(monolith.gameObject,SecretObject.Monolith,symbol,scenery.MonolithRune(symbol,monolith)));
                }
                interactions.Add(AddSecret(Entity("Inscricao da floresta",PrototypeSecretLayout.Inscription,forest),SecretObject.Inscription));
            }
            else
            {
                GameObject stone=Entity("Marcas na pedra",PrototypeSecretLayout.ClawStone,forest);
                scenery.ClawMarks(stone.transform);
                interactions.Add(AddSecret(stone,SecretObject.ClawStone));
                GameObject tree=Entity("Raizes da arvore oca",PrototypeSecretLayout.HollowTree,forest);
                scenery.Hollow(tree.transform);
                interactions.Add(AddSecret(tree,SecretObject.HollowTree));
            }
            return forest;
        }

        private Transform CreateCastle(List<Interactable> interactions)
        {
            Transform castle = Group("Castelo - interior");
            DrawBlocks(PrototypeWorldLayout.CastleBlocks, castle);
            CreatePassage("Saida do castelo", PrototypeTravel.Castle, castle, PrototypeTravel.City);
            GameObject king=Entity("Tirano",PrototypeWorldLayout.Throne,castle);
            BoxCollider2D collider=king.AddComponent<BoxCollider2D>();collider.size=new Vector2(0.55f,0.6f);collider.sharedMaterial=frictionless;
            scenery.PixelPerson(king.transform, "NpcRoster", 11);
            GameObject spirit=Entity("Espirito junto ao trono",PrototypeWorldLayout.Throne+Vector2.right*1.8f,castle);
            scenery.Spirit(spirit.transform);spirit.SetActive(false);
            TyrantInteraction tyrant=king.AddComponent<TyrantInteraction>();
            tyrant.Initialize(()=>battle?.Party,()=>battle.StartCastleBattle(),spirit);
            interactions.Add(tyrant);
            return castle;
        }

        private Transform CreateRuins(List<Interactable> interactions)
        {
            Transform ruins=Group("Ruinas antigas - altar da lenda");
            DrawBlocks(PrototypeRuinsLayout.Blocks,ruins);
            Transform altar=ruins.Find("Altar antigo");
            interactions.Add(AddSecret(altar.gameObject,SecretObject.Altar,MonolithSymbol.Sun,scenery.AltarFlame(altar)));
            CreatePassage("Saida das ruinas", PrototypeTravel.Ruins, ruins, PrototypeTravel.World);
            return ruins;
        }

        private void DrawBlocks(IReadOnlyList<CityBlock> blocks, Transform parent)
        {
            foreach (CityBlock block in blocks) Block(block.Name, block.Position, block.Size, block.Color, parent, block.Solid, block.Order);
        }

        private Transform CreateVolcano(List<Interactable> interactions)
        {
            Transform volcano=Group("Vulcao - trilha e arena");
            DrawBlocks(PrototypeVolcanoLayout.Blocks,volcano);
            CreatePassage("Saida do vulcao",PrototypeTravel.Volcano,volcano,PrototypeTravel.World);
            GameObject boss=Entity("Dragao corrompido",PrototypeVolcanoLayout.Dragon,volcano);
            BoxCollider2D collider=boss.AddComponent<BoxCollider2D>(); collider.size=new Vector2(1.8f,1.8f);collider.sharedMaterial=frictionless;
            Transform model=scenery.Dragon(boss.transform);
            Transform glow=scenery.DragonEssence(boss.transform);
            DragonInteraction interaction=boss.AddComponent<DragonInteraction>();
            interaction.Initialize(dragon,()=>battle.StartDragonBattle(),model.gameObject,glow.gameObject);
            interactions.Add(interaction);
            return volcano;
        }

        private Transform CreateInn(List<Interactable> interactions)
        {
            Transform inn=Group("Hospedaria - interior");
            DrawBlocks(PrototypeInnLayout.Blocks,inn);
            CreatePassage("Saida da hospedaria",PrototypeTravel.Inn,inn,PrototypeTravel.City);
            InnInteraction keeper=CreateNpcFigure("Hospedeira",PrototypeInnLayout.Keeper,"B89673",inn).AddComponent<InnInteraction>();
            keeper.Initialize(() => saves.SaveManual());
            interactions.Add(keeper);
            return inn;
        }

        private void CreateShop(Transform shop, List<Interactable> interactions, string shopId, string shopName, string displayColor)
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
                    Block("Mercadoria provisoria", new Vector2(x, 0.7f + shelf * 0.8f), new Vector2(0.4f, 0.4f), displayColor, shop, false, 4);
            }
            MerchantInteraction merchant = CreateNpcFigure(shopName, new Vector2(0, 0.9f), displayColor, shop).AddComponent<MerchantInteraction>();
            merchant.Initialize(shopId, shopName);
            interactions.Add(merchant);
            CreatePassage("Saida da loja", shopId, shop, PrototypeTravel.City);
        }

        private void Building(string buildingName, Vector2 position, Vector2 size, string roofColor, Transform parent)
        {
            GameObject building = Entity(buildingName, position, parent);
            BoxCollider2D collider = building.AddComponent<BoxCollider2D>();
            collider.size = size; collider.sharedMaterial = frictionless;
            scenery.Building(buildingName, size, roofColor, building.transform);
        }

        private GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Heroi");
            player.transform.SetParent(transform, false);
            player.transform.position = PrototypeWorldLayout.Spawn;
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.6f);
            collider.sharedMaterial = frictionless;
            player.AddComponent<PlayerMovement>();

            // O modelo tem altura em -Z; o objeto fisico permanece no chao XY, escala 1.
            scenery.PixelPerson(player.transform, "ArenWalk", animated: true);
            return player;
        }

        private GameObject Block(string blockName, Vector2 position, Vector2 size, string color,
            Transform parent, bool solid, int sortingOrder)
        {
            GameObject block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            scenery.Block(blockName, size, color, solid, sortingOrder, block.transform);
            if (solid)
            {
                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.size = size;
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
            scenery?.Dispose();
            if (frictionless != null) Destroy(frictionless);
        }
    }
}
