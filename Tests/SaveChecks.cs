using System;
using System.IO;
using System.Linq;
using System.Text;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;

internal static class SaveChecks
{
    private static int checks;
    private static void Assert(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static JourneyData Initial()
    {
        var party = PrototypePartyFactory.Create(); var progress = new ExplorationProgress(50);
        var shops = PrototypeShopFactory.Create(party, progress);
        return JourneySnapshot.Capture(party, progress, new AncientSecrets(), new DragonProgress(), shops,
            new DeliveryQuest(shops), PrototypeTravel.World, 0, -7);
    }
    private static JourneyData Clone(JourneyData data) => SaveStore.Decode(SaveStore.Encode(data));
    private static void Invalid(JourneyData original, Action<JourneyData> corrupt, string description)
    {
        var data = Clone(original); corrupt(data);
        try { JourneySnapshot.Restore(data); }
        catch (ArgumentException) { Assert(true, description); return; }
        throw new Exception("Save invalido aceito: " + description);
    }
    private static RestoredJourney RoundTrip(JourneyData data)
    {
        var restored = JourneySnapshot.Restore(SaveStore.Decode(SaveStore.Encode(data)));
        var recaptured = JourneySnapshot.Capture(restored.Party, restored.Progress, restored.Secrets, restored.Dragon,
            restored.Shops, restored.Quest, data.Map, data.X, data.Y, (GameEnding)data.Ending);
        recaptured.SavedUtc = data.SavedUtc;
        Assert(Encoding.UTF8.GetString(SaveStore.Encode(data)) == Encoding.UTF8.GetString(SaveStore.Encode(recaptured)), "Roundtrip deve preservar todo o snapshot, incluindo IDs e encaixes.");
        return restored;
    }
    private static JourneyData Advanced()
    {
        var party = PrototypePartyFactory.CreateTestParty(); var progress = new ExplorationProgress(1000);
        var shops = PrototypeShopFactory.Create(party, progress); var quest = new DeliveryQuest(shops);
        shops.TryBuy(shops.FindShop("equipment-shop"), shops.FindShop("equipment-shop").Products[0], 2, out _);
        shops.TryBuy(shops.FindShop("materia-shop"), shops.FindShop("materia-shop").Products[0], 2, out _);
        shops.TryBuy(shops.FindShop("item-shop"), shops.FindShop("item-shop").Products[0], 3, out _);
        quest.TryInteract(shops.FindShop("item-shop"), out _); quest.TryInteract(shops.FindShop("materia-shop"), out _);
        quest.TryInteract(shops.FindShop("item-shop"), out _);
        shops.TryBuy(shops.FindShop("item-shop"), shops.FindShop("item-shop").Products[0], 1, out _);
        var secrets = new AncientSecrets(); secrets.LearnPoem(); secrets.TrySolveAltar(LegendPoem.Flame);
        secrets.Activate(MonolithSymbol.Moon); secrets.Activate(MonolithSymbol.Star); secrets.Activate(MonolithSymbol.Sun);
        secrets.TryCollectScale(); secrets.TryOpenPassage(); progress.TryOpenChest("praca-primeiro-bau", 25);
        var dragon = new DragonProgress(); dragon.Restore(true);
        var armor = new EquipmentLoadout(DragonProgress.Armor, "dragon-armor"); party.TryAddEquipments(armor);
        party.TryEquipEquipment(party.Members[2], armor, out _);
        party.TryEquip(party.Members[2], armor, 2, party.Materias.First(m => m.Definition == PrototypePartyFactory.ThunderMateria), out _);
        party.TryEquip(party.Members[2], armor, 3, party.Materias.First(m => m.Definition == PrototypePartyFactory.AllMateria), out _);
        party.Members[0].Limit.Restore(73); party.Members[1].Limit.Restore(100); party.Members[2].Limit.Restore(12);
        party.TryRemoveEquipment(party.Members[1], EquipmentSlot.Weapon, out _);
        party.TryRemoveEquipment(party.Members[0], EquipmentSlot.Armor, out _);
        party.Consumables.TryRemove(PrototypePartyFactory.Ether, 1);
        var sale = shops.SaleEntries(shops.FindShop("equipment-shop")).First(e => e.Equipment?.Id == "iron-sword");
        shops.TrySell(shops.FindShop("equipment-shop"), sale, 1, out _);
        return JourneySnapshot.Capture(party, progress, secrets, dragon, shops, quest, PrototypeTravel.Volcano, 0.4f, -2.5f);
    }
    private static void CheckDomain()
    {
        var fresh = RoundTrip(Initial()); Assert(fresh.Progress.Gold == 50 && fresh.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 2, "Kit inicial restaurado.");
        var data = Advanced(); var restored = RoundTrip(data);
        Assert(restored.Dragon.HasEquippedArmor(restored.Party), "Armadura mantem a definicao canonica para revelar espirito.");
        Assert(restored.Party.Members[2].CreateCombatant().Definition.Abilities.Any(a => a.Element == DamageElement.Lightning && a.Targets == TargetRule.AllEnemies), "Combo Raio + All continua funcional depois de carregar.");
        Assert(restored.Party.Members[1].Weapon == null && restored.Party.Members[0].Armor == null, "Espacos vazios permanecem vazios.");
        Assert(restored.Party.Members.Select(m => m.Limit.Charge).SequenceEqual(new[] {73, 100, 12}), "Limit individual preservado.");
        Assert(!restored.Progress.TryOpenChest("praca-primeiro-bau", 25), "Carregar nao permite receber o bau novamente.");
        Assert(!restored.Quest.TryInteract(restored.Shops.FindShop("item-shop"), out _), "Missao/restock concluido nao repete recompensa.");
        Assert(restored.Shops.FindShop("item-shop").Products[0].Stock == 11 && restored.Shops.FindShop("equipment-shop").Products[0].Stock == 0,
            "Reposicao feita e estoque consumido de equipamentos preservados.");
        restored.Party.Members[0].Limit.Spend(); Assert(data.Members[0].Limit == 73, "Restauracao independente do snapshot original.");
        foreach (GameEnding ending in new[] {GameEnding.KingFreed, GameEnding.CastleDestroyed, GameEnding.HeroPossessed})
        { var final = Clone(data); final.Map = PrototypeTravel.Castle; final.X = 0; final.Y = 4; final.Ending = (int)ending; RoundTrip(final); }
        for (int stage = 0; stage <= 2; stage++)
        { var partial = Initial(); partial.DeliveryStage = stage; var state = RoundTrip(partial); Assert((int)state.Quest.Stage == stage, "Passos intermediarios da entrega persistem."); }
        foreach (int step in new[] {1, 2}) { var partial = Initial(); partial.Secrets.MonolithStep = step; Assert(RoundTrip(partial).Secrets.MonolithStep == step, "Enigma parcial persiste."); }
        foreach (var map in new PrototypeTravel(restored.Secrets).Maps)
        {
            var destination = Clone(data); destination.Map = map.Id; destination.X = map.Spawn.x; destination.Y = map.Spawn.y;
            var savedMap = RoundTrip(destination);
            Assert(savedMap.Data.Map == map.Id && savedMap.Data.X == map.Spawn.x && savedMap.Data.Y == map.Spawn.y, "Mapa e coordenadas exatas persistem em todos os interiores.");
        }
        Invalid(data, d => d.Version = 99, "Versao desconhecida");
        Invalid(data, d => d.SavedUtc = "ontem", "Data invalida");
        Invalid(data, d => d.Gold = -1, "Ouro negativo");
        Invalid(data, d => d.Map = "missing", "Mapa inexistente");
        Invalid(data, d => d.X = 1000, "Posicao fora do mapa");
        Invalid(data, d => d.X = float.NaN, "Posicao nao numerica");
        Invalid(data, d => d.Members[0].Limit = 101, "Limit fora do intervalo");
        Invalid(data, d => d.Members[1].Id = d.Members[0].Id, "Personagem duplicado");
        Invalid(data, d => d.Members[1].Weapon = d.Members[0].Weapon, "Mesmo equipamento em dois membros");
        Invalid(data, d => d.Members[0].Weapon = "ausente", "Equipamento nao possuido");
        Invalid(data, d => d.Members[0].Weapon = "dragon-armor", "Armadura em espaco de arma");
        Invalid(data, d => d.Gear[1].Id = d.Gear[0].Id, "Equipamento duplicado");
        Invalid(data, d => d.Gear[0].Definition = "unknown", "Definicao desconhecida");
        Invalid(data, d => d.Gear[0].Sockets = new string[0], "Encaixes incompletos");
        Invalid(data, d => d.Materias[1].Id = d.Materias[0].Id, "Materia duplicada");
        Invalid(data, d => d.Gear.First(g => g.Id == "dragon-armor").Sockets[0] = d.Gear.First(g => g.Id == "dragon-armor").Sockets[2], "Materia em dois encaixes");
        Invalid(data, d => d.Gear.First(g => g.Id == "lia/weapon").Sockets[0] = d.Materias[0].Id, "Materia em equipamento guardado");
        Invalid(data, d => d.Items[0].Quantity = -1, "Pilha negativa");
        Invalid(data, d => d.Stocks[0].Quantity = 999, "Estoque acima do inicial");
        Invalid(data, d => d.Stocks[1].Product = d.Stocks[0].Product, "Estoque repetido");
        Invalid(data, d => d.DeliveryRestocked = false, "Entrega concluida sem reposicao");
        Invalid(data, d => d.Secrets.MonolithStep = 1, "Passagem sem tres segredos");
        Invalid(data, d => d.DragonDefeated = false, "Armadura sem dragao");
        Invalid(data, d => d.OpenedChests = new[] {"praca-primeiro-bau", "praca-primeiro-bau"}, "Bau repetido");
        Invalid(data, d => d.Ending = 42, "Final desconhecido");
        var locked = Initial(); locked.Map = PrototypeTravel.Volcano;
        Invalid(locked, d => { }, "Vulcao fechado");
    }
    private static void CheckFiles(string directory)
    {
        var store = new SaveStore(directory); var initial = Initial(); var advanced = Advanced();
        Assert(!store.Exists(1) && !store.TryRead(1, out _, out _), "Slot vazio.");
        for (int slot = 1; slot <= 3; slot++)
        { Assert(store.TryWrite(slot, initial, out string error), "Criar slot: " + error); Assert(store.TryRead(slot, out _, out string warning) && warning == null, "Ler slot valido."); }
        Assert(store.TryWrite(1, advanced, out _), "Substituicao atomica.");
        Assert(store.TryRead(1, out var loaded, out _) && loaded.Data.Map == PrototypeTravel.Volcano, "Save atualizado.");
        Assert(store.TryRead(2, out loaded, out _) && loaded.Progress.Gold == 50, "Slots independentes.");
        File.WriteAllText(store.SlotPath(1), "{save incompleto");
        Assert(store.TryRead(1, out loaded, out string message) && loaded.Progress.Gold == 50 && message != null, "Recuperar copia anterior depois de corrupcao.");
        Assert(store.TryWrite(1, advanced, out _), "Salvar depois de recuperacao.");
        Assert(JourneySnapshot.Restore(SaveStore.Decode(File.ReadAllBytes(store.SlotPath(1) + ".bak"))).Progress.Gold == 50, "Corrupcao nao substitui backup valido.");
        var future = Clone(advanced); future.Version = 99;
        File.WriteAllBytes(store.SlotPath(1), SaveStore.Encode(future));
        Assert(!store.TryRead(1, out _, out message) && message.Contains("Versao"), "Outra versao nao regride silenciosamente para o backup.");
        Assert(store.TryWrite(1, advanced, out _), "Substituicao explicita pode criar uma jornada da versao atual.");
        var invalid = Clone(advanced); invalid.Gold = -1;
        Assert(!store.TryWrite(1, invalid, out _) && store.TryRead(1, out loaded, out _) && loaded.Data.Gold == advanced.Gold, "Falha de validacao conserva save anterior.");
        using (var locked = new FileStream(store.SlotPath(1), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert(!store.TryWrite(1, initial, out _), "Arquivo em uso recusa substituicao.");
        Assert(store.TryRead(1, out loaded, out _) && loaded.Data.Gold == advanced.Gold && !File.Exists(store.SlotPath(1) + ".tmp"), "Falha real de escrita conserva principal e limpa temporario.");
        File.WriteAllText(store.SlotPath(3) + ".tmp", "interrompido");
        Assert(store.TryRead(3, out loaded, out _) && loaded.Progress.Gold == 50, "Arquivo temporario nao vira save.");
        Assert(store.TryWrite(3, advanced, out _) && !File.Exists(store.SlotPath(3) + ".tmp"), "Nova escrita limpa temporario.");
        File.WriteAllText(store.SlotPath(2), "{}");
        Assert(!store.TryRead(2, out _, out message) && message.Contains("indisponivel"), "Campos obrigatorios ausentes sao recusados sem backup.");
        string blocked = Path.Combine(directory, "bloqueado"); File.WriteAllText(blocked, "arquivo");
        Assert(!new SaveStore(blocked).TryWrite(1, initial, out message) && message.Contains("Nao foi possivel"), "Falha de disco gera mensagem e nao quebra o jogo.");
        foreach (int slot in new[] {0, 4, -1})
        { try { store.SlotPath(slot); throw new Exception("Slot invalido aceito."); } catch (ArgumentOutOfRangeException) { Assert(true, "Limites de slot."); } }
    }
    private static void CheckPolicy()
    {
        var policy = new AutosavePolicy(); policy.Changed(true); Assert(!policy.Pending, "Gastar item no meio da batalha nao salva.");
        policy.BattleCompleted(BattleOutcome.Defeat); Assert(!policy.Pending, "Derrota nao dispara autosave.");
        policy.BattleCompleted(BattleOutcome.Victory); Assert(policy.Pending, "Vitoria dispara autosave.");
        policy.Acknowledge(); Assert(!policy.Pending, "Uma transacao gera uma escrita.");
        policy.Changed(false); Assert(policy.Pending, "Inventario/segredo fora de combate salva.");
        policy.BattleCompleted(BattleOutcome.Completed); Assert(policy.Pending, "Desfecho final e registrado.");
        var party = PrototypePartyFactory.Create(); int changes = 0; party.Changed += () => changes++;
        party.TryRemoveEquipment(party.Members[0], EquipmentSlot.Armor, out _);
        Assert(changes == 1, "Desequipar dispara evento.");
        party.TryEquipEquipment(party.Members[0], party.Equipments.First(g => g.Id == "aren/armor"), out _);
        Assert(changes == 2, "Equipar dispara evento.");
        party.TryEquip(party.Members[0], party.Members[0].Armor, 0, party.Materias.First(m => m.Id == "start-thunder"), out _);
        Assert(changes == 3, "Encaixar materia dispara evento.");
        party.TryUnequip(party.Members[0], party.Members[0].Armor, 0, out _); Assert(changes == 4, "Remover materia dispara evento.");
    }
    public static int Main(string[] args)
    {
        try
        {
            CheckDomain(); CheckPolicy(); CheckFiles(Path.Combine(args[0], "saves-" + Guid.NewGuid().ToString("N")));
            Console.WriteLine("Salvamento: " + checks + " verificacoes passaram."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); return 1; }
    }
}
