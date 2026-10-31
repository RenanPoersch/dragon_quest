using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Content;
using System.Collections.Generic;
using DragonQuest.Audio;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.World
{
    public sealed class WorldBuilder
    {
        private readonly Transform root;
        private readonly ObliqueScenery scenery;
        private readonly PhysicsMaterial2D frictionless;
        private readonly AncientSecrets secrets;
        private readonly DragonProgress dragon;
        private readonly WorldNavigation navigation;
        private readonly WorldActions actions;
        private InnSavePoint innSavePoint;

        public WorldBuilder(Transform root, ObliqueScenery scenery, PhysicsMaterial2D frictionless,
            AncientSecrets secrets, DragonProgress dragon, WorldNavigation navigation, WorldActions actions)
        {
            this.root = root; this.scenery = scenery; this.frictionless = frictionless;
            this.secrets = secrets; this.dragon = dragon; this.navigation = navigation; this.actions = actions;
        }

        public WorldScene Build()
        {
            Transform city = Group("Periferia do castelo");
            Transform ground = Group("Chao e ruas", city);
            scenery.CityGround(ground);
            Transform buildings = Group("Predios", city);
            Transform obstacles = Group("Obstaculos e limites", city);

            foreach (CityBlock block in CityLayout.Blocks)
                Block(block.Name, block.Position, block.Size, block.Color,
                    block.Solid || block.Order >= 0 ? obstacles : ground, block.Solid, block.Order, cityAppearance: true);
            foreach (CityBlock building in CityLayout.Buildings)
                Building(building.Name, building.Position, building.Size, building.Color, buildings);
            scenery.CityStreets(Group("Detalhes urbanos", city));

            var interactables = new List<Interactable>();
            Transform interactions = Group("Moradores e bau", city);
            NpcInteraction citizen = CreateNpc("Aldeão", NpcAppearance.Villager, CityLayout.Citizen, interactions);
            citizen.Initialize(DialogueCatalog.Citizen);
            interactables.Add(citizen);
            GameObject peasant = CreateNpcFigure("Camponesa", NpcAppearance.Peasant, CityLayout.Resident, interactions);
            interactables.Add(AddSecret(peasant, SecretObject.Peasant));
            NpcInteraction traveler = CreateNpc("Viajante", NpcAppearance.Traveler, CityLayout.Traveler, interactions);
            traveler.Initialize(DialogueCatalog.Traveler, CompleteTravelerConversation);
            interactables.Add(traveler);
            NpcInteraction explorer = CreateNpc("Exploradora", NpcAppearance.Explorer, CityLayout.Explorer, interactions);
            explorer.Initialize(DialogueCatalog.Explorer);
            interactables.Add(explorer);
            interactables.Add(CreateChest(interactions));
            CreatePassage("Loja de itens", WorldNavigation.City, city, "item-shop");
            CreatePassage("Loja de materias", WorldNavigation.City, city, "materia-shop");
            CreatePassage("Loja de equipamentos", WorldNavigation.City, city, "equipment-shop");
            CreatePassage("Portao para a ilha", WorldNavigation.City, city, WorldNavigation.World);
            CreatePassage("Entrada do castelo", WorldNavigation.City, city, WorldNavigation.Castle);
            CreatePassage("Hospedaria",WorldNavigation.City,city,WorldNavigation.Inn);
            GameObject guard = Entity("Guarda Real", CityLayout.Guard, city);
            GameObject sentry = Entity("Sentinela da ponte", new Vector2(-1.2f, 0), guard.transform);
            BoxCollider2D guardCollider = sentry.AddComponent<BoxCollider2D>();
            guardCollider.size = new Vector2(0.65f, 0.7f);
            guardCollider.sharedMaterial = frictionless;
            scenery.PixelPerson(sentry.transform, "NpcRoster", PixelSpriteLibrary.NpcFrame(NpcAppearance.Guard));
            GameObject soldier = Entity("Soldado da ponte", new Vector2(1.2f, 0), guard.transform);
            BoxCollider2D soldierCollider = soldier.AddComponent<BoxCollider2D>();
            soldierCollider.size = guardCollider.size;
            soldierCollider.sharedMaterial = frictionless;
            scenery.PixelPerson(soldier.transform, "NpcRoster", PixelSpriteLibrary.NpcFrame(NpcAppearance.Guard));
            GameObject bridgeBarrier = Entity("Bloqueio da patrulha", CityLayout.Bridge - CityLayout.Guard, guard.transform);
            BoxCollider2D barrierCollider = bridgeBarrier.AddComponent<BoxCollider2D>();
            barrierCollider.size = new Vector2(3.7f, .7f);
            barrierCollider.sharedMaterial = frictionless;
            BattleInteraction bridgeGuards = guard.AddComponent<BattleInteraction>();
            interactables.Add(bridgeGuards);

            var mapRoots = new Dictionary<string, GameObject>
            {
                { WorldNavigation.World, CreateWorld(interactables).gameObject },
                { WorldNavigation.City, city.gameObject },
                { WorldNavigation.Castle, CreateCastle(interactables).gameObject },
                { WorldNavigation.WestForest, CreateQuarry(interactables).gameObject },
                { WorldNavigation.EastForest, CreateForest(interactables).gameObject },
                { WorldNavigation.Ruins, CreateRuins(interactables).gameObject },
                { WorldNavigation.Volcano,CreateVolcano(interactables).gameObject },
                { WorldNavigation.Inn,CreateInn(interactables).gameObject }
            };
            string[] shopIds = { "item-shop", "equipment-shop", "materia-shop" };
            string[] shopNames = { "Loja de itens", "Loja de equipamentos", "Loja de matérias" };
            NpcAppearance[] shopAppearances = { NpcAppearance.ItemMerchant, NpcAppearance.EquipmentMerchant, NpcAppearance.MateriaMerchant };
            for (int i = 0; i < shopIds.Length; i++)
            {
                Transform shop = Group(shopNames[i] + " - interior");
                CreateShop(shop, interactables, shopIds[i], shopNames[i], shopAppearances[i]);
                mapRoots.Add(shopIds[i], shop.gameObject);
            }

            var player = CreatePlayer().GetComponent<PlayerMovement>();
            return new WorldScene(mapRoots, player, interactables.ToArray(), bridgeGuards, innSavePoint);
        }

        private Transform Group(string groupName, Transform parent = null)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(parent != null ? parent : root, false);
            return group.transform;
        }

        private GameObject Entity(string entityName, Vector2 position, Transform parent)
        {
            GameObject entity = new GameObject(entityName);
            entity.transform.SetParent(parent, false);
            entity.transform.localPosition = position;
            return entity;
        }

        private void CompleteTravelerConversation(InteractionContext context)
        {
            if (!TravelerGift.TryGivePhoenix(actions.GetParty(), context.Progress)) return;
            MenuSounds.Play(MenuSound.Confirm);
            context.Dialogue.Open(DialogueCatalog.PhoenixReceived);
        }

        private NpcInteraction CreateNpc(string npcName, NpcAppearance appearance, Vector2 position, Transform parent)
            => CreateNpcFigure(npcName, appearance, position, parent).AddComponent<NpcInteraction>();

        private AncientSecretInteraction AddSecret(GameObject target, SecretObject kind,
            MonolithSymbol symbol = MonolithSymbol.Sun, GameObject light = null)
        {
            AncientSecretInteraction interaction = target.AddComponent<AncientSecretInteraction>();
            interaction.Initialize(kind, secrets, symbol, light); return interaction;
        }

        private GameObject CreateNpcFigure(string npcName, NpcAppearance appearance, Vector2 position, Transform parent)
        {
            GameObject npc = Entity(npcName, position, parent);
            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.6f);
            collider.sharedMaterial = frictionless;
            scenery.PixelPerson(npc.transform, "NpcRoster", PixelSpriteLibrary.NpcFrame(appearance));
            return npc;
        }

        private ChestInteraction CreateChest(Transform parent)
        {
            GameObject chest = Entity("Bau da praca", CityLayout.Chest, parent);
            BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.7f);
            collider.sharedMaterial = frictionless;
            System.Action<bool> showState = scenery.Chest(chest.transform);
            ChestInteraction interaction = chest.AddComponent<ChestInteraction>();
            interaction.Initialize("praca-primeiro-bau", 100, showState,
                () => actions.GetProgress()?.HasOpenedChest("praca-primeiro-bau") == true);
            return interaction;
        }

        private void CreatePassage(string doorName, string source, Transform parent, string destination)
        {
            Rect area = navigation.RouteFrom(source, destination).Entrance;
            // Ancora na Hierarchy; a passagem usa a area invisivel declarada em WorldNavigation.
            Entity(doorName, area.center, parent);
        }

        private Transform CreateWorld(List<Interactable> interactions)
        {
            Transform world = Group("Ilha - mapa global");
            GameObject barrier = scenery.World(world);
            foreach (Collider2D collider in world.GetComponentsInChildren<Collider2D>()) collider.sharedMaterial = frictionless;
            GameObject coast = Entity("Costa - limite do mar", Vector2.zero, world);
            EdgeCollider2D shoreline = coast.AddComponent<EdgeCollider2D>();
            var perimeter = new Vector2[WorldLayout.Coast.Count + 1];
            for (int i = 0; i < WorldLayout.Coast.Count; i++) perimeter[i] = WorldLayout.Coast[i];
            perimeter[perimeter.Length - 1] = perimeter[0];
            shoreline.points = perimeter;
            shoreline.edgeRadius = 0.08f;
            shoreline.sharedMaterial = frictionless;

            Transform castle = Entity("Castelo - icone global", WorldLayout.Castle, world).transform;
            scenery.CastleIcon(castle);
            castle.GetComponent<BoxCollider2D>().sharedMaterial = frictionless;
            CreatePassage("Castelo e arredores", WorldNavigation.World, world, WorldNavigation.City);
            Transform quarry = Entity("Pedreira - icone global", WorldLayout.WestForest, world).transform;
            scenery.QuarryIcon(quarry);
            quarry.GetComponent<BoxCollider2D>().sharedMaterial = frictionless;
            CreateForestIcon(world, WorldLayout.EastForest);
            CreatePassage("Pedreira dos monolitos", WorldNavigation.World, world, WorldNavigation.WestForest);
            CreatePassage("Floresta leste", WorldNavigation.World, world, WorldNavigation.EastForest);

            Transform ruins = Entity("Ruinas - icone global",WorldLayout.Ruins,world).transform;
            scenery.RuinsIcon(ruins);
            ruins.GetComponent<BoxCollider2D>().sharedMaterial=frictionless;
            CreatePassage("Ruinas antigas", WorldNavigation.World, world, WorldNavigation.Ruins);

            Transform volcano = Entity("Vulcao - icone global", WorldLayout.Volcano, world).transform;
            scenery.VolcanoIcon(volcano);
            CreatePassage("Entrada do vulcao",WorldNavigation.World,world,WorldNavigation.Volcano);
            GameObject passage = Entity("Passagem oculta", WorldLayout.Trail, world);
            BlockedPassageInteraction gate = passage.AddComponent<BlockedPassageInteraction>();
            gate.Initialize(barrier, secrets); interactions.Add(gate);
            return world;
        }

        private void CreateForestIcon(Transform world, Vector2 position)
        {
            Transform forest = Entity("Floresta - icone global", position, world).transform;
            scenery.ForestIcon(forest);
        }

        private Transform CreateQuarry(List<Interactable> interactions)
        {
            Transform quarry = Group("Pedreira dos monolitos - interior pixel art");
            quarry.gameObject.AddComponent<QuarryPixelScenery>().Build(secrets, interactions, frictionless);
            CreatePassage("Saida da pedreira", WorldNavigation.WestForest, quarry, WorldNavigation.World);
            NpcInteraction worker = CreateNpc("Lenhador", NpcAppearance.QuarryKeeper, QuarryLayout.Worker, quarry);
            worker.Initialize(DialogueCatalog.QuarryKeeper);
            interactions.Add(worker);
            return quarry;
        }

        private Transform CreateForest(List<Interactable> interactions)
        {
            Transform forest = Group("Floresta leste - interior");
            forest.gameObject.AddComponent<ForestPixelScenery>().Build(secrets);
            foreach (CityBlock block in WorldLayout.CreateForestBlocks(true))
            {
                if (!block.Solid || !(block.Name.StartsWith("Limite") || block.Name == "Pedra com escamas")) continue;
                Vector2 position = block.Name == "Pedra com escamas" ? SecretLayout.ClawStone : block.Position;
                GameObject obstacle = Entity(block.Name, position, forest);
                BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
                collider.size = block.Size; collider.sharedMaterial = frictionless;
            }
            CreatePassage("Saida da floresta", WorldNavigation.EastForest, forest, WorldNavigation.World);
            NpcInteraction gardener = CreateNpc("Jardineiro", NpcAppearance.Gardener, EastForestLayout.Gardener, forest);
            gardener.Initialize(DialogueCatalog.Gardener);
            interactions.Add(gardener);
            GameObject stone = Entity("Marcas na pedra", SecretLayout.ClawStone, forest);
            interactions.Add(AddSecret(stone, SecretObject.ClawStone));
            GameObject tree = Entity("Raizes da arvore oca", SecretLayout.HollowTree, forest);
            BoxCollider2D roots = tree.AddComponent<BoxCollider2D>();
            roots.size = new Vector2(1.6f,1f); roots.sharedMaterial = frictionless;
            interactions.Add(AddSecret(tree, SecretObject.HollowTree));
            return forest;
        }

        private Transform CreateCastle(List<Interactable> interactions)
        {
            Transform castle = Group("Castelo - interior");
            castle.gameObject.AddComponent<ThroneRoomPixelScenery>().Build();
            CreatePassage("Saida do castelo", WorldNavigation.Castle, castle, WorldNavigation.City);
            GameObject king=Entity("Rei Rubro",ThroneRoomLayout.King,castle);
            BoxCollider2D collider=king.AddComponent<BoxCollider2D>();collider.size=new Vector2(0.55f,0.6f);collider.sharedMaterial=frictionless;
            scenery.PixelPerson(king.transform, "TyrantSeated");
            king.AddComponent<ThroneRoomDepth>();
            GameObject spirit=Entity("Espirito junto ao trono",ThroneRoomLayout.Spirit,castle);
            scenery.PixelPerson(spirit.transform, "SpiritBattle");
            spirit.AddComponent<ThroneRoomDepth>();spirit.SetActive(false);
            TyrantInteraction tyrant=king.AddComponent<TyrantInteraction>();
            tyrant.Initialize(actions.GetParty,actions.StartCastleBattle,spirit,
                conversation=>actions.BeginAudience(king.transform,conversation));
            interactions.Add(tyrant);
            return castle;
        }

        private Transform CreateRuins(List<Interactable> interactions)
        {
            Transform ruins=Group("Ruinas antigas - altar da lenda");
            Transform altar=ruins.gameObject.AddComponent<RuinsSideScenery>().Build(out GameObject flame);
            interactions.Add(AddSecret(altar.gameObject,SecretObject.Altar,MonolithSymbol.Sun,flame));
            CreatePassage("Saida das ruinas", WorldNavigation.Ruins, ruins, WorldNavigation.World);
            return ruins;
        }

        private Transform CreateVolcano(List<Interactable> interactions)
        {
            Transform volcano = Group("Covil do dragao - caverna lateral");
            var lair = volcano.gameObject.AddComponent<DragonLairScenery>();
            lair.Build();
            CreatePassage("Saida do covil", WorldNavigation.Volcano, volcano, WorldNavigation.World);
            GameObject boss = Entity("Dragao corrompido", VolcanoLayout.Dragon, volcano);
            var model = lair.CreateDragon(boss.transform);
            GameObject glow = lair.CreateEssence(boss.transform);
            DragonInteraction interaction = boss.AddComponent<DragonInteraction>();
            interaction.Initialize(dragon, actions.StartDragonBattle, model.gameObject, glow);
            interactions.Add(interaction);
            return volcano;
        }

        private Transform CreateInn(List<Interactable> interactions)
        {
            Transform inn=Group("Hospedaria - interior");
            inn.gameObject.AddComponent<InnPixelScenery>().Build(frictionless);
            CreatePassage("Saida da hospedaria",WorldNavigation.Inn,inn,WorldNavigation.City);
            InnInteraction keeper=CreateNpcFigure("Hospedeira",NpcAppearance.Innkeeper,InnLayout.Keeper,inn).AddComponent<InnInteraction>();
            keeper.Initialize(actions.OpenInnSaveMenu, actions.RestAtInn);
            interactions.Add(keeper);
            InnInteraction reception=Entity("Recepcao / falar com a hospedeira",InnLayout.Reception,inn).AddComponent<InnInteraction>();
            reception.Initialize(actions.OpenInnSaveMenu, actions.RestAtInn);
            interactions.Add(reception);
            innSavePoint=Entity("Cristal de salvamento",InnLayout.SavePoint,inn).AddComponent<InnSavePoint>();
            return inn;
        }

        private void CreateShop(Transform shop, List<Interactable> interactions, string shopId, string shopName, NpcAppearance appearance)
        {
            Vector2 merchantPoint,counterPoint;
            CompactShopLayout compact=CompactShopLayout.Find(shopId);
            if (shopId == WeaponShopLayout.MapId)
            {
                shop.gameObject.AddComponent<WeaponShopPixelScenery>().Build(frictionless);
                merchantPoint=WeaponShopLayout.Merchant; counterPoint=WeaponShopLayout.Counter;
            }
            else if (compact != null)
            {
                shop.gameObject.AddComponent<CompactShopPixelScenery>().Build(compact,frictionless);
                merchantPoint=compact.Merchant; counterPoint=compact.Counter;
            }
            else throw new System.ArgumentException("Loja sem cenario: "+shopId);
            MerchantInteraction merchant=CreateNpcFigure(shopName,appearance,merchantPoint,shop).AddComponent<MerchantInteraction>();
            merchant.Initialize(shopId); interactions.Add(merchant);
            MerchantInteraction counter=Entity("Balcao / negociar",counterPoint,shop).AddComponent<MerchantInteraction>();
            counter.Initialize(shopId); interactions.Add(counter);
            CreatePassage("Saida da loja",shopId,shop,WorldNavigation.City);
        }

        private void Building(string buildingName, Vector2 position, Vector2 size, string roofColor, Transform parent)
        {
            GameObject building = Entity(buildingName, position, parent);
            BoxCollider2D collider = building.AddComponent<BoxCollider2D>();
            collider.size = size; collider.sharedMaterial = frictionless;
            scenery.CityBuilding(buildingName, size, roofColor, building.transform);
        }

        private GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Heroi");
            player.transform.SetParent(root, false);
            player.transform.position = WorldLayout.Spawn;
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
            Transform parent, bool solid, int sortingOrder, bool cityAppearance = false)
        {
            GameObject block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            if (cityAppearance) scenery.CityBlock(new CityBlock(blockName, position.x, position.y, size.x, size.y, color, solid, sortingOrder), block.transform);
            else scenery.Block(blockName, size, color, solid, sortingOrder, block.transform);
            if (solid)
            {
                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.size = size;
                collider.sharedMaterial = frictionless;
            }
            return block;
        }

    }
}
