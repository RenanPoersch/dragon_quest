using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;

internal static class CheatChecks
{
    private static int checks;
    private static void Check(bool value, string description)
    { checks++; if (!value) throw new Exception(description); }

    private static CombatantState Enemy(int hp = 1000000, int gold = 50, int xp = 100)
        => new CombatantState("enemy", new CombatantDefinition("Enemy", "Test", hp, 0, 1, 0, 0, 1,
            ElementalAffinities.Neutral, gold, xp, 0, MateriaResolver.Attack, MateriaResolver.Defend), CombatTeam.Enemy);

    private static void Limits()
    {
        var party = PrototypePartyFactory.Create();
        var actors = party.Members.Select(m => m.CreateCombatant()).ToArray();
        var enemy = Enemy();
        var battle = new BattleSession(() => 1, null, party.Consumables, actors.Concat(new[] { enemy }).ToArray());
        party.Members[0].Limit.Restore(37);
        Check(!party.HasInfiniteLimit && !actors[0].Limit.IsReady, "Cheats start disabled.");
        party.SetInfiniteLimit(true);
        Check(actors.All(a => a.Limit.IsReady) && !enemy.Limit.IsReady, "Existing party actors share unlimited LIMIT; enemies do not.");
        Check(BattleCommandMenu.Root(actors[0])[0].Ability?.UsesLimit == true, "Ready LIMIT replaces Attack in the command menu immediately.");
        int casts = 0;
        for (int turn = 0; turn < 8; turn++)
        {
            var actor = battle.CurrentActor;
            bool ally = actor.Team == CombatTeam.Party;
            var command = ally ? actor.Definition.Abilities.First(a => a.UsesLimit) : MateriaResolver.Defend;
            Check(battle.TryAct(actor, command, ally ? enemy : actor, out string message), "Repeated LIMIT action: " + message);
            if (ally) { casts++; Check(actor.Limit.Charge == 100, "Spending LIMIT must keep it ready."); }
        }
        Check(casts == 6, "All three members can use LIMIT again next round.");
        actors[0].TakeDamage(1);
        Check(party.Members.All(m => m.CreateCombatant().Limit.IsReady), "Unlimited charge carries into future encounters.");
        party.SetInfiniteLimit(false);
        Check(actors[0].Limit.Charge == 37 && actors.Skip(1).All(a => a.Limit.Charge == 0), "Disabling restores each original charge.");
        actors[0].Limit.Restore(100); actors[0].Limit.Spend();
        Check(actors[0].Limit.Charge == 0, "Normal LIMIT consumes its charge again.");
    }

    private static void Gold()
    {
        var party = PrototypePartyFactory.Create();
        var progress = new ExplorationProgress(50);
        var shops = PrototypeShopFactory.Create(party, progress);
        var shop = shops.FindShop("item-shop");
        var product = shop.Products.First(p => p.Item == PrototypePartyFactory.Potion);
        progress.SetInfiniteGold(true);
        Check(progress.Gold == int.MaxValue && progress.CanReceiveGold(int.MaxValue), "Unlimited wallet accepts income even while full.");
        Check(progress.TrySpendGold(int.MaxValue) && progress.TrySpendGold(int.MaxValue), "Spending never exhausts unlimited gold.");
        Check(!progress.TrySpendGold(0) && !progress.TryAddGold(-1), "Unlimited gold still rejects invalid amounts.");
        int stock = product.Stock, inventory = party.Consumables.QuantityOf(product.Item);
        Check(shops.TryBuy(shop, product, stock, out _), "Can purchase the entire available stock.");
        Check(product.Stock == 0 && party.Consumables.QuantityOf(product.Item) == inventory + stock,
            "Free purchases still add items and consume shop stock.");
        Check(!shops.TryBuy(shop, product, 1, out _), "Infinite gold does not bypass sold-out stock.");
        var sale = shops.SaleEntries(shop).First(e => e.Item == product.Item);
        Check(shops.TrySell(shop, sale, 1, out _), "Sales succeed without a full-wallet overflow.");
        Check(progress.TryOpenChest("test-chest", int.MaxValue) && !progress.TryOpenChest("test-chest", 1),
            "Chests remain claimable exactly once without overflowing.");
        var hero = party.Members[0].CreateCombatant(); var enemy = Enemy(1);
        var battle = new BattleSession(() => 1, null, party.Consumables, hero, enemy);
        Check(battle.TryAct(hero, MateriaResolver.Attack, enemy, out _), "Reward fixture wins.");
        var rewards = new BattleRewards(progress, party);
        Check(rewards.TryClaim(battle, out _) && rewards.HasClaimed(battle), "Victory rewards settle with unlimited gold.");
        Check(hero.Limit.Charge == 0 && party.Members[0].Experience == 100, "Gold cheat preserves normal LIMIT and XP rewards.");
        Check(!rewards.TryClaim(battle, out _), "Reward claims remain one-time.");
        Check(progress.Gold == int.MaxValue, "Purchases, sales, chests and rewards keep the unlimited balance.");
        progress.SetInfiniteGold(false);
        Check(progress.Gold == 50 && !progress.TrySpendGold(51), "Disabling restores the original wallet and affordability rules.");
        Check(progress.TryAddGold(10) && progress.TrySpendGold(5) && progress.Gold == 55, "Normal income and spending resume.");
        var full = new ExplorationProgress(int.MaxValue);
        Check(!full.TryAddGold(1), "Normal overflow protection remains active.");
    }

    private static void Levels()
    {
        var party = PrototypePartyFactory.Create();
        var actors = party.Members.Select(m => m.CreateCombatant()).ToArray();
        var enemy = Enemy();
        actors[0].TakeDamage(20); actors[0].SpendMp(3); actors[0].PoisonTurns = 1;
        actors[1].BoundActions = 2; actors[1].IsDefending = true; actors[1].GrantDamageShield(3, false);
        actors[2].TakeDamage(actors[2].Hp);
        var battle = new BattleSession(actors.Concat(new[] { enemy }).ToArray());
        var current = battle.CurrentActor;
        int enemyAttack = enemy.Definition.Attack, enemyHp = enemy.Hp;
        var weapon = party.Members[0].Weapon;
        party.SetMaxLevel(battle);
        Check(party.Members.All(m => m.Level == 99 && m.Experience == CharacterProgression.MaxExperience), "Every party member reaches level 99.");
        Check(actors[0].Definition.MaxHp == party.Members[0].MaxHp && actors[0].Definition.Attack == party.Members[0].Attack,
            "Current battle receives the actual level-99 stats.");
        Check(actors[0].Hp == 80 && actors[0].Mp == 25 && actors[0].IsPoisoned, "Level cheat preserves live damage, MP and poison.");
        Check(actors[1].BoundActions == 2 && actors[1].IsDefending && actors[1].ShieldTurns == 3,
            "Updating stats preserves temporary combat conditions.");
        Check(!actors[2].IsAlive && !party.Members[2].IsAlive, "Level 99 does not silently revive defeated members.");
        Check(ReferenceEquals(battle.CurrentActor, current) && battle.Round == 1, "The current turn and round remain intact.");
        Check(enemy.Definition.Attack == enemyAttack && enemy.Hp == enemyHp, "Enemy stats remain unchanged.");
        int attack = actors[0].Definition.Attack;
        party.SetMaxLevel(battle);
        Check(actors[0].Definition.Attack == attack && ReferenceEquals(weapon, party.Members[0].Weapon),
            "Repeated activation does not stack stats or replace equipment.");
        Check(actors.All(a => a.Definition.MaxHp == party.Members.First(m => m.Id == a.Id).CreateCombatant().Definition.MaxHp),
            "Existing and future encounters agree on the new attributes.");
        var fresh = PrototypePartyFactory.Create();
        fresh.SetMaxLevel();
        Check(fresh.Members.All(m => m.Level == 99), "Level cheat also works outside combat.");
        Check(!fresh.HasInfiniteLimit && !new ExplorationProgress().HasInfiniteGold, "Other cheats stay independent.");
    }

    public static int Main()
    {
        try { Limits(); Gold(); Levels(); Console.WriteLine("Cheats: " + checks + " verificacoes passaram."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
