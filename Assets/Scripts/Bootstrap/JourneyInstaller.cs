using DragonQuest.Presentation.Menus;
using DragonQuest.Presentation.Dialogue;
using DragonQuest.Presentation.Cinematics.Endings;
using DragonQuest.Gameplay.Shops;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay;
using DragonQuest.Content;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Presentation.World;
using DragonQuest.Progression;
using DragonQuest.Shops;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    // Owns the connections that must follow the active journey rather than the scene's first party.
    public sealed class JourneyInstaller
    {
        private readonly WorldScene world;
        private readonly SaveController saves;
        private readonly DialogueController dialogue;
        private readonly BattleController battle;
        private readonly ShopController shops;
        private readonly WorldLocationController locations;
        private readonly CheatMenu cheats;
        private readonly AncientSecrets secrets;
        private readonly DragonProgress dragon;
        private readonly EndingController endings;
        private PlayerInteraction interaction;
        private DialogueHud hud;
        private readonly GameFlowState flow;

        public ExplorationProgress Progress { get; private set; }

        public JourneyInstaller(WorldScene world, SaveController saves, DialogueController dialogue,
            BattleController battle, ShopController shops, WorldLocationController locations,
            CheatMenu cheats, AncientSecrets secrets, DragonProgress dragon,
            EndingController endings, GameFlowState flow)
        {
            this.world = world; this.saves = saves; this.dialogue = dialogue; this.battle = battle;
            this.shops = shops; this.locations = locations; this.cheats = cheats;
            this.secrets = secrets; this.dragon = dragon; this.endings = endings;
            this.flow = flow ?? throw new System.ArgumentNullException(nameof(flow));
        }

        public void StartNewJourney()
        {
            Progress = new ExplorationProgress(ShopCatalog.StartingGold);
            ConnectSystems(null, null);
        }

        public void BindExploration(PlayerInteraction playerInteraction, DialogueHud dialogueHud)
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

        private void ConnectSystems(PartyProgress party, ShopService shopService)
        {
            battle.Initialize(world.Player, dialogue, Progress,
                dragon, secrets, saves.ReturnToLoadGame, endings.Show, party);
            shops.Initialize(world.Player, battle, Progress, shopService);
            cheats.Initialize(world.Player, battle, saves, Progress);
        }

        private void ConnectExploration()
        {
            interaction.Initialize(new InteractionContext(dialogue, Progress, locations.TravelTo,
                battle.StartBattle, () => flow.IsBusy, shops.Open, secrets), world.Interactables);
            hud.Initialize(dialogue, battle);
        }
    }
}
