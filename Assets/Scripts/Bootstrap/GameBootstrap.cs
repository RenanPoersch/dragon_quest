using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.Menus;
using DragonQuest.Presentation.Dialogue;
using DragonQuest.Presentation.Cinematics.Exploration;
using DragonQuest.Presentation.Cinematics.Endings;
using DragonQuest.Presentation.Battle;
using DragonQuest.Gameplay.Shops;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay;
using DragonQuest.Input;
using System;
using DragonQuest.Audio;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Presentation.World;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    public sealed class GameBootstrap : IDisposable
    {
        // Objeto da cena que recebe os componentes dos sistemas do jogo.
        private readonly GameObject host;
        // Guarda o estado dos enigmas, itens e passagens secretas da jornada.
        private readonly AncientSecrets secrets = new AncientSecrets();
        // Guarda o progresso do confronto com o dragão.
        private readonly DragonProgress dragon = new DragonProgress();
        // Aplica as configurações de taxa de quadros e sincronização vertical, restauradas em Dispose.
        private FramePacing framePacing;
        // Fornece os recursos visuais usados na construção do mundo e nas cenas.
        private ObliqueScenery scenery;
        // Material físico sem atrito nem quique usado pelo construtor do mundo.
        private PhysicsMaterial2D frictionless;
        // Coordena os combates e mantém acesso ao grupo de personagens.
        private BattleController battle;
        // Controla o salvamento, o carregamento e o menu de gravação na estalagem.
        private SaveController saves;
        // Controla a sequência de descanso do grupo na estalagem.
        private InnRestController innRest;
        // Controla a cena em que o grupo se apresenta diante do Rei Rubro.
        private TyrantAudience audience;
        // Conecta os sistemas ao progresso da jornada ao iniciar ou restaurar uma partida.
        private JourneyInstaller journey;
        // Impede que os recursos do bootstrap sejam liberados mais de uma vez.
        private bool disposed;

        public GameBootstrap(GameObject host) { this.host = host; }

        public void Initialize()
        {
            new GameObject("Menu sounds").AddComponent<MenuSoundController>();
            framePacing = new FramePacing();
            saves = host.AddComponent<SaveController>();
            // Serviço de navegação do mundo, que consulta os segredos da jornada.
            var navigation = new WorldNavigation(secrets);
            scenery = new ObliqueScenery();
            frictionless = new PhysicsMaterial2D("WorldFrictionless") { friction = 0f, bounciness = 0f };

            // Reúne as ações que o mundo pode disparar, como combates, audiência e descanso.
            var actions = new WorldActions(() => battle?.Party, () => journey?.Progress,
                () => battle.StartCastleBattle(), () => battle.StartDragonBattle(),
                (king, conversation) => audience.Begin(king, conversation),
                () => saves.OpenInnSaveMenu(), () => innRest.Begin());
            // Agrupa os mapas, o jogador e os pontos de interação criados pelo construtor do mundo.
            var world = new WorldBuilder(host.transform, scenery, frictionless, secrets, dragon, navigation, actions).Build();
            // Câmera principal da cena, necessária para configurar a apresentação do mundo.
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Game precisa de uma camera com a tag MainCamera.", host);
                return;
            }
            camera.backgroundColor = new Color32(0x26, 0x3E, 0x35, 0xFF);
            // Componente usado pelos locais e pelas cenas de encerramento para acompanhar o jogador.
            var cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            // Controla os diálogos usados na exploração e nas demais interações.
            var dialogue = host.AddComponent<DialogueController>();
            // Controla o local atual, as viagens entre mapas e a ligação com a câmera.
            var locations = host.AddComponent<WorldLocationController>();
            // Controla as lojas e suas interações com o grupo e a jornada.
            var shops = host.AddComponent<ShopController>();
            // Controla as cenas de encerramento usando os mapas, o jogador e a câmera.
            var endings = host.AddComponent<EndingController>();
            endings.Initialize(world.Maps, world.Player, cameraFollow);
            innRest = host.AddComponent<InnRestController>();
            battle = host.AddComponent<BattleController>();
            // Menu de trapaças cujo estado também participa do bloqueio de entrada do jogador.
            var cheats = host.AddComponent<CheatMenu>();

            // Centraliza as permissões de movimento, viagem e interação conforme os sistemas ativos.
            var flow = new GameFlowState(() => battle.HasEnded, () => battle.IsActive, () => battle.IsMenuOpen,
                () => dialogue.IsOpen, () => shops.IsOpen, () => saves.IsMenuVisible, () => cheats.BlocksInput,
                () => innRest.IsPlaying, () => GameInput.IsFocused);
            battle.SetFlow(flow);
            world.Player.SetMovementGate(() => !flow.BlocksMovement);
            journey = new JourneyInstaller(world, saves, dialogue, battle, shops, locations,
                cheats, secrets, dragon, endings, flow);
            journey.StartNewJourney();

            audience = new GameObject("Party diante do Rei Rubro").AddComponent<TyrantAudience>();
            audience.transform.SetParent(host.transform, false);
            audience.Initialize(world.Player, () => battle.Party, scenery);
            locations.Initialize(world.Maps, world.Player, cameraFollow, navigation,
                () => !flow.CanTravel);
            host.AddComponent<ShopHud>().Initialize(shops);
            battle.SetLocationProvider(() => locations.LocationId);
            world.BridgeGuards.Initialize(world.Player, dialogue, battle, () => journey.Progress);
            host.AddComponent<BattleHud>().Initialize(battle, scenery.PixelSprites);
            host.AddComponent<InventoryHud>().Initialize(battle, secrets);
            // Componente do jogador que executa as interações com os objetos do mundo.
            var interaction = world.Player.gameObject.AddComponent<PlayerInteraction>();
            // Interface visual dos diálogos, vinculada aos sistemas da jornada.
            var hud = host.AddComponent<DialogueHud>();
            journey.BindExploration(interaction, hud);
            saves.Initialize(world.Player, battle, shops, locations, journey.Progress,
                secrets, dragon, journey.Restore);
            innRest.Initialize(world.Player, () => battle.Party, scenery,
                () => locations.LocationId == WorldNavigation.Inn, () => !flow.CanInteract);
            world.SavePoint.Initialize(world.Player, () => !flow.CanOpenSaveMenu, saves.OpenInnSaveMenu);
            host.AddComponent<MusicController>().Initialize(saves, locations, battle);
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
