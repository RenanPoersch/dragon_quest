using DragonQuest.Content;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Presentation.World;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using DragonQuest.Shops;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    // Owns the connections that must follow the active journey rather than the scene's first party.
    public sealed class JourneyInstaller
    {
        private readonly WorldScene world;
        private readonly PrototypeSaveController saves;
        private readonly DialogueController dialogue;
        private readonly PrototypeBattleController battle;
        private readonly PrototypeShopController shops;
        private readonly PrototypeLocations locations;
        private readonly PrototypeCheatMenu cheats;
        private readonly InnRestController innRest;
        private readonly AncientSecrets secrets;
        private readonly DragonProgress dragon;
        private readonly PrototypeEndingController endings;
        private PlayerInteraction interaction;
        private PrototypeHud hud;

        public ExplorationProgress Progress { get; private set; }

        public JourneyInstaller(WorldScene world, PrototypeSaveController saves, DialogueController dialogue,
            PrototypeBattleController battle, PrototypeShopController shops, PrototypeLocations locations,
            PrototypeCheatMenu cheats, InnRestController innRest, AncientSecrets secrets, DragonProgress dragon,
            PrototypeEndingController endings)
        {
            this.world = world; this.saves = saves; this.dialogue = dialogue; this.battle = battle;
            this.shops = shops; this.locations = locations; this.cheats = cheats; this.innRest = innRest;
            this.secrets = secrets; this.dragon = dragon; this.endings = endings;
        }

        public void StartNewJourney()
        {
            Progress = new ExplorationProgress(ShopCatalog.StartingGold);
            ConnectSystems(null, null);
        }

        public void BindExploration(PlayerInteraction playerInteraction, PrototypeHud dialogueHud)
        {
            interaction = playerInteraction; hud = dialogueHud;
            ConnectExploration();
        }

        public void Restore(RestoredJourney journey)
        {
            Progress = journey.Progress;
            world.BridgeGuards.RestoreState();
            var savedSecrets = journey.Secrets;
            secrets.Restore(savedSecrets.KnowsPoem, savedSecrets.AltarSolved, savedSecrets.MonolithStep,
                savedSecrets.HasScale, savedSecrets.PassageOpen);
            dragon.Restore(journey.Dragon.IsDefeated);
            ConnectSystems(journey.Party, journey.Shops);
            ConnectExploration();
            locations.Restore(journey.Data.Map, new Vector2(journey.Data.X, journey.Data.Y));
        }

        private bool OtherMenuOpen() => shops.IsOpen || saves.IsMenuVisible || cheats.BlocksInput || innRest.IsPlaying;

        private void ConnectSystems(PartyProgress party, ShopService shopService)
        {
            battle.Initialize(world.Player, dialogue, Progress, OtherMenuOpen,
                dragon, secrets, saves.ReturnToLoadGame, endings.Show, party);
            shops.Initialize(world.Player, dialogue, battle, Progress, shopService);
            cheats.Initialize(world.Player, battle, saves, Progress);
        }

        private void ConnectExploration()
        {
            interaction.Initialize(new InteractionContext(dialogue, Progress, locations.TravelTo,
                battle.StartBattle, () => battle.IsBusy, shops.Open, secrets), world.Interactables);
            hud.Initialize(dialogue, battle);
        }
    }
}
