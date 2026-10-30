using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;

namespace DragonQuest.Gameplay.Battle
{
    public sealed class BattleResolution
    {
        private readonly PartyProgress party;
        private readonly DragonProgress dragon;
        private bool notifiedCompletion;
        public BattleRewards Rewards { get; }
        public string RewardMessage { get; private set; }
        public BattleResolution(PartyProgress party, ExplorationProgress progress, DragonProgress dragon)
        { this.party = party; this.dragon = dragon; Rewards = new BattleRewards(progress, party); }
        public void Begin() { notifiedCompletion = false; RewardMessage = string.Empty; }
        public bool Resolve(BattleSession session, bool dragonEncounter, PostBattleResults postBattle)
        {
            if (session == null || session.Outcome == BattleOutcome.None) return false;
            var castleRules = session.Rules as CastleBattleRules;
            if (!notifiedCompletion) party.CaptureBattleState(session);
            if (session.Outcome == BattleOutcome.Victory)
            {
                if (dragonEncounter) dragon.TryComplete(session,party);
                if (!Rewards.HasClaimed(session))
                {
                    Rewards.TryClaim(session,out string message);
                    RewardMessage = message + (dragonEncounter && dragon.IsDefeated ? " Armadura draconica recebida!" : "");
                }
            }
            else if (session.Outcome == BattleOutcome.Completed && castleRules?.Ending == GameEnding.KingFreed)
            {
                if (Rewards.TryClaimExperience(session, out string message)) RewardMessage = message;
            }
            if (PostBattleResults.IsVictory(session) && !postBattle.IsComplete)
                postBattle.Complete(party, Rewards.HasClaimed(session),
                    dragonEncounter && dragon.IsDefeated ? "Armadura draconica recebida!" : "");
            if (notifiedCompletion) return false;
            notifiedCompletion = true; return true;
        }
    }
}
