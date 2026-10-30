using DragonQuest.Gameplay;
using DragonQuest.Input;
using System;
using DragonQuest.Audio;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Presentation.World;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    public sealed class GameBootstrap : IDisposable
    {
        private readonly GameObject host;
        private readonly AncientSecrets secrets = new AncientSecrets();
        private readonly DragonProgress dragon = new DragonProgress();
        private PrototypeFramePacing framePacing;
        private ObliqueScenery scenery;
        private PhysicsMaterial2D frictionless;
        private PrototypeBattleController battle;
        private PrototypeSaveController saves;
        private InnRestController innRest;
        private TyrantAudience audience;
        private JourneyInstaller journey;
        private bool disposed;

        public GameBootstrap(GameObject host) { this.host = host; }

        public void Initialize()
        {
            new GameObject("Menu sounds").AddComponent<MenuSoundController>();
            framePacing = new PrototypeFramePacing();
            saves = host.AddComponent<PrototypeSaveController>();
            var navigation = new PrototypeTravel(secrets);
            scenery = new ObliqueScenery();
            frictionless = new PhysicsMaterial2D("PrototypeFrictionless") { friction = 0f, bounciness = 0f };

            var actions = new WorldActions(() => battle?.Party, () => journey?.Progress,
                () => battle.StartCastleBattle(), () => battle.StartDragonBattle(),
                (king, conversation) => audience.Begin(king, conversation),
                () => saves.OpenInnSaveMenu(), () => innRest.Begin());
            var world = new WorldBuilder(host.transform, scenery, frictionless, secrets, dragon, navigation, actions).Build();
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("CidadePrototipo precisa de uma camera com a tag MainCamera.", host);
                return;
            }
            camera.backgroundColor = new Color32(0x26, 0x3E, 0x35, 0xFF);
            var cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            var dialogue = host.AddComponent<DialogueController>();
            var locations = host.AddComponent<PrototypeLocations>();
            var shops = host.AddComponent<PrototypeShopController>();
            var endings = host.AddComponent<PrototypeEndingController>();
            endings.Initialize(world.Maps, world.Player, cameraFollow);
            innRest = host.AddComponent<InnRestController>();
            battle = host.AddComponent<PrototypeBattleController>();
            var cheats = host.AddComponent<PrototypeCheatMenu>();

            var flow = new GameFlowState(() => battle.HasEnded, () => battle.IsActive, () => battle.IsMenuOpen,
                () => dialogue.IsOpen, () => shops.IsOpen, () => saves.IsMenuVisible, () => cheats.BlocksInput,
                () => innRest.IsPlaying, () => GameInput.IsFocused);
            battle.SetFlow(flow);
            world.Player.SetMovementGate(() => !flow.BlocksMovement);
            journey = new JourneyInstaller(world, saves, dialogue, battle, shops, locations,
                cheats, innRest, secrets, dragon, endings, flow);
            journey.StartNewJourney();

            audience = new GameObject("Party diante do Rei Rubro").AddComponent<TyrantAudience>();
            audience.transform.SetParent(host.transform, false);
            audience.Initialize(world.Player, () => battle.Party, scenery);
            locations.Initialize(world.Maps, world.Player, cameraFollow, navigation,
                () => flow.BlocksTravel);
            host.AddComponent<PrototypeShopHud>().Initialize(shops);
            battle.SetLocationProvider(() => locations.LocationId);
            world.BridgeGuards.Initialize(world.Player, dialogue, battle, () => journey.Progress);
            host.AddComponent<PrototypeBattleHud>().Initialize(battle, scenery.PixelSprites);
            host.AddComponent<PrototypeMateriaHud>().Initialize(battle, secrets);
            var interaction = world.Player.gameObject.AddComponent<PlayerInteraction>();
            var hud = host.AddComponent<PrototypeHud>();
            journey.BindExploration(interaction, hud);
            saves.Initialize(world.Player, battle, shops, locations, dialogue, journey.Progress,
                secrets, dragon, journey.Restore);
            innRest.Initialize(world.Player, () => battle.Party, scenery,
                () => locations.LocationId == PrototypeTravel.Inn, () => flow.BlocksTravel);
            world.SavePoint.Initialize(world.Player, () => flow.BlocksTravel, saves.OpenInnSaveMenu);
            host.AddComponent<PrototypeMusicController>().Initialize(saves, locations, battle);
            host.AddComponent<BattleSoundController>().Initialize(battle);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            framePacing?.Dispose();
            scenery?.Dispose();
            if (frictionless != null) UnityEngine.Object.Destroy(frictionless);
        }
    }
}
