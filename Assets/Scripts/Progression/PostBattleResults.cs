using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Progression
{
    public readonly struct PostBattleExperience
    {
        public readonly int Total, Level, ToNextLevel;
        public readonly float Progress;
        public PostBattleExperience(int total)
        {
            Total = total; Level = 1;
            while (Level < CharacterProgression.MaxLevel && total >= CharacterProgression.ExperienceForLevel(Level + 1)) Level++;
            int start = CharacterProgression.ExperienceForLevel(Level);
            int next = Level == CharacterProgression.MaxLevel ? start : CharacterProgression.ExperienceForLevel(Level + 1);
            ToNextLevel = next - total;
            Progress = next == start ? 1 : (float)(total - start) / (next - start);
        }
    }

    public sealed class PostBattleMemberResult
    {
        public string Id { get; }
        public string Name { get; }
        public int BeforeExperience { get; }
        public int BeforeLevel { get; }
        public int Experience { get; private set; }
        public int Level { get; private set; }
        public int GainedExperience => Experience - BeforeExperience;
        public bool LeveledUp => Level > BeforeLevel;
        internal PostBattleMemberResult(PartyMember member)
        {
            Id = member.Id; Name = member.Name; BeforeExperience = Experience = member.Experience;
            BeforeLevel = Level = member.Level;
        }
        internal void Complete(PartyMember member) { Experience = member.Experience; Level = member.Level; }
        public PostBattleExperience At(double elapsed)
        {
            double progress = Math.Max(0, Math.Min(1, elapsed / PostBattleResults.CountDuration));
            progress = 1 - (1 - progress) * (1 - progress);
            return new PostBattleExperience(BeforeExperience + (int)Math.Floor(GainedExperience * progress));
        }
    }

    // Captures XP before the reward, so a screen repaint never grants XP or loses the level-up comparison.
    public sealed class PostBattleResults
    {
        public const double CountDuration = 1.2;
        private readonly BattleSession battle;
        private readonly List<PostBattleMemberResult> members = new List<PostBattleMemberResult>();
        public IReadOnlyList<PostBattleMemberResult> Members { get; }
        public long EnemyExperience { get; private set; }
        public long Gold { get; private set; }
        public bool IsComplete { get; private set; }
        public string BonusMessage { get; private set; }
        public bool HasLevelUps { get { foreach (var member in members) if (member.LeveledUp) return true; return false; } }
        public bool CanContinue(double elapsed) => IsComplete && elapsed >= (HasLevelUps ? 2.8 : CountDuration);
        public static bool IsVictory(BattleSession battle) => battle != null && (battle.Outcome == BattleOutcome.Victory
            || battle.Outcome == BattleOutcome.Completed && battle.Rules is CastleBattleRules castle && castle.Ending == GameEnding.KingFreed);
        public PostBattleResults(PartyProgress party, BattleSession battle)
        {
            if (party == null || battle == null) throw new ArgumentNullException(nameof(party));
            this.battle = battle;
            var ids = new HashSet<string>();
            foreach (var actor in battle.Combatants) if (actor.Team == CombatTeam.Party) ids.Add(actor.Id);
            foreach (var member in party.Members) if (ids.Contains(member.Id)) members.Add(new PostBattleMemberResult(member));
            Members = members.AsReadOnly();
        }
        public bool Complete(PartyProgress party, bool goldReceived, string bonusMessage = "")
        {
            if (IsComplete || !IsVictory(battle)) return false;
            foreach (var row in members)
                foreach (var member in party.Members) if (row.Id == member.Id) { row.Complete(member); break; }
            foreach (var enemy in battle.Combatants)
            {
                if (enemy.Team != CombatTeam.Enemy || enemy.IsAlive) continue;
                EnemyExperience += enemy.Definition.ExperienceReward;
                if (goldReceived) Gold += enemy.Definition.GoldReward;
            }
            BonusMessage = bonusMessage; IsComplete = true;
            return true;
        }
    }
}
