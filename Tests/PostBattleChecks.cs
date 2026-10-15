using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;

internal static class PostBattleChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static BattleSession Encounter(PartyProgress party, int experience, int participantCount = 3)
    {
        var actors = party.Members.Take(participantCount).Select(m => m.CreateCombatant()).ToList();
        actors.Add(new CombatantState("reward-enemy", new CombatantDefinition("Enemy", "", 1, 0, 1, 0, 0, 0,
            ElementalAffinities.Neutral, 7, experience, 0, PrototypeBattleFactory.Attack), CombatTeam.Enemy));
        return new BattleSession(() => 1, null, party.Consumables, actors.ToArray());
    }
    private static void Win(BattleSession battle)
    {
        var actor = battle.CurrentActor;
        var attack = actor.Definition.Abilities.First(a => a.Id == "attack");
        Check(battle.TryAct(actor, attack, battle.GetValidTargets(attack)[0], out _),
            "The encounter must accept the finishing blow.");
        Check(battle.Outcome == BattleOutcome.Victory, "The fixture must finish with victory.");
    }
    private static void Victory(PartyProgress party, int experience, bool expectedLevelUp)
    {
        var battle = Encounter(party, experience);
        var result = new PostBattleResults(party, battle);
        var rewards = new BattleRewards(new ExplorationProgress(), party);
        int[] before = party.Members.Select(m => m.Experience).ToArray();
        Check(!result.Complete(party, false) && !result.IsComplete && !result.CanContinue(100), "Live encounters cannot show completed results.");
        Win(battle);
        Check(rewards.TryClaim(battle, out _), "Victory rewards must be awarded.");
        Check(result.Complete(party, true, "Bonus") && result.IsComplete && result.EnemyExperience == experience && result.Gold == 7,
            "Results show the actual enemy XP and awarded gold.");
        Check(result.Members.Count == 3 && result.HasLevelUps == expectedLevelUp && result.BonusMessage == "Bonus", "All participating heroes and bonus text are represented.");
        for (int i = 0; i < result.Members.Count; i++)
        {
            var row = result.Members[i];
            int expectedShare = Math.Min(CharacterProgression.MaxExperience - before[i],
                experience / result.Members.Count + (i < experience % result.Members.Count ? 1 : 0));
            Check(row.GainedExperience == expectedShare, "Every row shows only that hero's share of the total enemy XP.");
            Check(row.BeforeExperience == before[i] && row.At(-1).Total == before[i] && row.At(0).Total == before[i], "Counting starts from the captured XP.");
            var middle = row.At(.6);
            Check(middle.Total >= before[i] && middle.Total <= row.Experience && middle.Progress >= 0 && middle.Progress <= 1, "Animation stays within the awarded range.");
            Check(row.At(100).Total == party.Members[i].Experience && row.At(100).Level == party.Members[i].Level,
                "Animation finishes at the saved hero progression.");
        }
        Check(!result.CanContinue(1.19) && result.CanContinue(3), "Continue waits for reward presentation.");
        Check(result.CanContinue(1.2) != expectedLevelUp, "Level-ups keep the screen open for the celebration.");
        int[] after = party.Members.Select(m => m.Experience).ToArray();
        Check(!result.Complete(party, true) && !rewards.TryClaim(battle, out _) && party.Members.Select(m => m.Experience).SequenceEqual(after),
            "Reopening results cannot duplicate XP or gold.");
    }
    private static void SharedExperience()
    {
        for (int count = 1; count <= 3; count++)
        foreach (int total in new[] { 0, 1, 2, 20, 85, 90, 101 })
        {
            var party = PrototypePartyFactory.Create();
            var battle = Encounter(party, total, count);
            var result = new PostBattleResults(party, battle);
            if (count == 3) battle.Combatants[2].TakeDamage(battle.Combatants[2].Hp);
            Win(battle);
            var rewards = new BattleRewards(new ExplorationProgress(), party);
            Check(rewards.TryClaim(battle, out _) && result.Complete(party, true), "A shared reward completes once.");
            var shares = party.Members.Take(count).Select(m => m.Experience).ToArray();
            Check(shares.Sum() == total && shares.Max() - shares.Min() <= 1,
                "Equal integer shares conserve XP, including zero XP and totals smaller than the party.");
            Check(party.Members.Skip(count).All(m => m.Experience == 0), "Absent heroes do not receive XP or enter the divisor.");
            Check(result.Members.Select(m => m.GainedExperience).SequenceEqual(shares), "Result rows match actual shares, including fallen heroes.");
            var expected = Enumerable.Range(0, count).Select(i => total / count + (i < total % count ? 1 : 0));
            Check(shares.SequenceEqual(expected), "Remainders are assigned deterministically in party order.");
            Check(!rewards.TryClaim(battle, out _) && party.Members.Take(count).Select(m => m.Experience).SequenceEqual(shares),
                "Reopening the result cannot duplicate a shared reward.");
        }
    }
    private static int Main()
    {
        try
        {
            SharedExperience();
            var party = PrototypePartyFactory.Create();
            Victory(party, 80, false);
            Victory(party, 400, true);
            var solo = Encounter(party, 20, 1);
            var soloResult = new PostBattleResults(party, solo);
            Win(solo);
            var fullGold = new BattleRewards(new ExplorationProgress(int.MaxValue), party);
            Check(!fullGold.TryClaim(solo, out _), "A full wallet rejects gold.");
            Check(soloResult.Complete(party, false) && soloResult.Gold == 0 && soloResult.Members.Count == 1 && soloResult.Members[0].GainedExperience == 20,
                "Only participants receive a row; XP remains visible when gold is full.");
            foreach (var member in party.Members) member.Progression.AddExperience(long.MaxValue);
            Victory(party, 100, false);
            Check(new PostBattleExperience(CharacterProgression.MaxExperience).ToNextLevel == 0, "Max-level results do not request more XP.");
            var losingParty = PrototypePartyFactory.Create();
            var hero = losingParty.Members[0].CreateCombatant();
            var enemy = new CombatantState("winner", new CombatantDefinition("Enemy", "", 1000, 0, 1000, 0, 0, 1000,
                ElementalAffinities.Neutral, PrototypeBattleFactory.Attack), CombatTeam.Enemy);
            var defeat = new BattleSession(() => 1, null, losingParty.Consumables, hero, enemy);
            var defeatResult = new PostBattleResults(losingParty, defeat);
            Check(defeat.TryAct(enemy, PrototypeBattleFactory.Attack, hero, out _) && defeat.Outcome == BattleOutcome.Defeat,
                "The losing fixture must finish with defeat.");
            Check(!PostBattleResults.IsVictory(defeat) && !defeatResult.Complete(losingParty, false), "Defeats cannot grant a victory screen.");
            Console.WriteLine(checks + " post-battle reward, XP animation, level-up and duplicate-claim checks passed.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
