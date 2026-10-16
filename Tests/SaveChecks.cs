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
        recaptured.PlaySeconds = data.PlaySeconds;
        Assert(Encoding.UTF8.GetString(SaveStore.Encode(data)) == Encoding.UTF8.GetString(SaveStore.Encode(recaptured)), "Roundtrip deve preservar todo o snapshot, incluindo IDs e encaixes.");
        return restored;
    }
    private static void CheckPlayTime()
    {
        var data = Initial(); data.PlaySeconds = 7 * 3600 + 2 * 60;
        Assert(RoundTrip(data).Data.PlaySeconds == data.PlaySeconds, "Tempo de jogo persiste no save.");
        string legacy = Encoding.UTF8.GetString(SaveStore.Encode(Initial()));
        Assert(!legacy.Contains("PlaySeconds") && SaveStore.Decode(Encoding.UTF8.GetBytes(legacy)).PlaySeconds == null,
            "Saves anteriores carregam sem inventar um tempo de jogo.");
        data.PlaySeconds = 0;
        Assert(RoundTrip(data).Data.PlaySeconds == 0, "Jornada recem-salva conserva zero segundos.");
        Invalid(data, d => d.PlaySeconds = -1, "Tempo de jogo negativo");
    }
    private static void CheckCheatPersistence()
    {
        var journey = JourneySnapshot.Restore(Initial());
        journey.Party.Members[0].Limit.Restore(42);
        journey.Party.SetInfiniteLimit(true);
        journey.Progress.SetInfiniteGold(true);
        journey.Party.SetMaxLevel();
        var data = JourneySnapshot.Capture(journey.Party, journey.Progress, journey.Secrets, journey.Dragon,
            journey.Shops, journey.Quest, journey.Data.Map, journey.Data.X, journey.Data.Y);
        var loaded = RoundTrip(data);
        Assert(loaded.Progress.Gold == 50 && !loaded.Progress.HasInfiniteGold, "Unlimited gold never overwrites the saved wallet.");
        Assert(loaded.Party.Members[0].Limit.Charge == 42 && !loaded.Party.HasInfiniteLimit
            && loaded.Party.Members.All(m => !m.Limit.IsInfinite), "Unlimited LIMIT preserves original saved charges and resets on load.");
        Assert(loaded.Party.Members.All(m => m.Level == 99), "Level 99 survives save/load with the normal progression format.");
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
    private static void CheckLevelPersistence()
    {
        var journey = JourneySnapshot.Restore(Initial());
        int[] experience = { 100, 525, 5000 };
        for (int i = 0; i < 3; i++) journey.Party.Members[i].Progression.AddExperience(experience[i]);
        var snapshot = JourneySnapshot.Capture(journey.Party, journey.Progress, journey.Secrets, journey.Dragon,
            journey.Shops, journey.Quest, PrototypeTravel.World, 0, -7);
        var restored = RoundTrip(snapshot);
        for (int i = 0; i < 3; i++)
        {
            var before = journey.Party.Members[i]; var after = restored.Party.Members[i];
            Assert(after.Experience == experience[i] && after.Level == before.Level && after.MaxHp == before.MaxHp
                && after.MaxMp == before.MaxMp && after.Attack == before.Attack && after.Defense == before.Defense
                && after.Magic == before.Magic && after.Speed == before.Speed && after.CriticalChance == before.CriticalChance,
                "XP individual, nivel e todos os atributos derivados persistem sem reaplicar crescimento.");
        }
        restored.Party.Members[0].Progression.AddExperience(150);
        Assert(restored.Party.Members[0].Level == 3 && journey.Party.Members[0].Level == 2, "Jornada carregada evolui independentemente.");
        string legacy = System.Text.RegularExpressions.Regex.Replace(Encoding.UTF8.GetString(SaveStore.Encode(snapshot)),
            "\"Experience\":\\d+,", "");
        var old = JourneySnapshot.Restore(SaveStore.Decode(Encoding.UTF8.GetBytes(legacy)));
        Assert(old.Party.Members.All(m => m.Level == 1 && m.Experience == 0), "Saves anteriores sem XP permanecem carregaveis no nivel 1.");
        Invalid(snapshot, d => d.Members[0].Experience = -1, "XP negativo");
        Invalid(snapshot, d => d.Members[0].Experience = CharacterProgression.MaxExperience + 1, "XP acima do limite");
    }
    private static void CheckConditionPersistence()
    {
        var journey = JourneySnapshot.Restore(Initial());
        journey.Party.Members[0].RestoreVitals(37, 9, true, 1);
        journey.Party.Members[1].RestoreVitals(0, 0, false, 0);
        var data = JourneySnapshot.Capture(journey.Party, journey.Progress, journey.Secrets, journey.Dragon,
            journey.Shops, journey.Quest, PrototypeTravel.World, 0, -7);
        var restored = RoundTrip(data);
        var hero = restored.Party.Members[0]; var healer = restored.Party.Members[1];
        Assert(hero.Hp == 37 && hero.Mp == 9 && hero.IsPoisoned && hero.BoundActions == 1, "HP, MP, veneno e correntes persistem no save.");
        Assert(healer.Hp == 0 && healer.Mp == 0 && !healer.IsAlive, "Zero HP e MP nao sao confundidos com campos ausentes.");
        restored.Party.AdvancePoison(3);
        Assert(hero.Hp == 32 && hero.IsPoisoned && journey.Party.Members[0].Hp == 37, "Veneno continua depois de carregar sem alterar jornada original.");
        Invalid(data, d => d.Members[0].Hp = -1, "HP negativo");
        Invalid(data, d => d.Members[0].Hp = 9999, "HP acima do maximo");
        Invalid(data, d => d.Members[0].Mp = -1, "MP negativo");
        Invalid(data, d => d.Members[0].Mp = 9999, "MP acima do maximo");
        Invalid(data, d => d.Members[0].BoundActions = -1, "Correntes negativas");
        Invalid(data, d => d.Members[0].BoundActions = 2, "Correntes acima do limite");
        Invalid(data, d => d.Members[1].Poisoned = true, "Veneno em derrotado");
        string legacy = System.Text.RegularExpressions.Regex.Replace(Encoding.UTF8.GetString(SaveStore.Encode(Initial())),
            "\"(Hp|Mp)\":\\d+,", "");
        var old = JourneySnapshot.Restore(SaveStore.Decode(Encoding.UTF8.GetBytes(legacy)));
        Assert(old.Party.Members.All(m => m.Hp == m.MaxHp && m.Mp == m.MaxMp && !m.IsPoisoned), "Saves antigos sem vitais carregam com recursos cheios.");
        var member = journey.Party.Members[2];
        member.Progression.AddExperience(525);
        var materia = new MateriaInstance("save-hp", PrototypePartyFactory.HpMateria);
        journey.Party.TryAddMaterias(materia); journey.Party.TryEquip(member, member.Armor, 0, materia, out _);
        member.RestoreVitals(member.MaxHp, member.MaxMp, true, 0);
        var grown = JourneySnapshot.Capture(journey.Party, journey.Progress, journey.Secrets, journey.Dragon,
            journey.Shops, journey.Quest, PrototypeTravel.World, 0, -7);
        var equipped = RoundTrip(grown).Party.Members[2];
        Assert(equipped.Hp == member.MaxHp && equipped.Mp == member.MaxMp && equipped.IsPoisoned,
            "Validacao de vitais considera nivel e materias instaladas antes de restaurar HP/MP.");
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
        var oldCity = Initial(); oldCity.Map = PrototypeTravel.City; oldCity.X = -25.5f; oldCity.Y = -3;
        var legacyCity = RoundTrip(oldCity);
        Assert(legacyCity.Progress.Gold == 50 && legacyCity.Data.Map == PrototypeTravel.City
            && PrototypeCityLayout.SafeSavedPosition(new UnityEngine.Vector2(oldCity.X, oldCity.Y)) == PrototypeCityLayout.Spawn,
            "Jornada da cidade ampla deve continuar carregavel, conservando progresso e usando chegada livre no bairro compacto.");
        Invalid(oldCity, d => d.X = -40, "Posicao fora dos limites atuais e antigos da periferia");
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
    private static void CheckInventoryEvents()
    {
        var party = PrototypePartyFactory.Create(); int changes = 0; party.Changed += () => changes++;
        party.TryRemoveEquipment(party.Members[0], EquipmentSlot.Armor, out _);
        Assert(changes == 1, "Desequipar dispara evento.");
        party.TryEquipEquipment(party.Members[0], party.Equipments.First(g => g.Id == "aren/armor"), out _);
        Assert(changes == 2, "Equipar dispara evento.");
        party.TryEquip(party.Members[0], party.Members[0].Armor, 0, party.Materias.First(m => m.Id == "start-thunder"), out _);
        Assert(changes == 3, "Encaixar materia dispara evento.");
        party.TryUnequip(party.Members[0], party.Members[0].Armor, 0, out _); Assert(changes == 4, "Remover materia dispara evento.");
    }
    private static void CheckManualPersistence(string directory)
    {
        var store = new SaveStore(directory);
        var initial = Initial();
        Assert(!Directory.Exists(directory) && !store.Exists(1), "Preparar slots nao cria arquivos de save.");
        Assert(store.TryWrite(1, initial, out string initialError), "Primeiro salvamento explicito: " + initialError);
        byte[] previous = File.ReadAllBytes(store.SlotPath(1));
        Assert(store.TryRead(1, out var session, out _), "Carregar jornada para alterar na sessao.");
        session.Progress.TryOpenChest("praca-primeiro-bau", 25);
        session.Secrets.LearnPoem();
        session.Party.TryRemoveEquipment(session.Party.Members[0], EquipmentSlot.Armor, out _);
        session.Party.Members[0].Limit.Restore(42);
        session.Shops.TryBuy(session.Shops.FindShop("item-shop"), session.Shops.FindShop("item-shop").Products[0], 1, out _);
        session.Quest.TryInteract(session.Shops.FindShop("item-shop"), out _);
        var changed = JourneySnapshot.Capture(session.Party, session.Progress, session.Secrets, session.Dragon,
            session.Shops, session.Quest, PrototypeTravel.Inn, PrototypeInnLayout.Spawn.x, PrototypeInnLayout.Spawn.y);
        Assert(previous.SequenceEqual(File.ReadAllBytes(store.SlotPath(1))) && !File.Exists(store.SlotPath(1) + ".bak"),
            "Eventos de bau, inventario, loja, entrega e segredo nao gravam nem criam backup.");
        Assert(store.TryRead(1, out var saved, out _) && saved.Progress.Gold == 50 && saved.Party.Members[0].Armor != null
            && saved.Party.Members[0].Limit.Charge == 0 && !saved.Secrets.KnowsPoem,
            "Recarregar antes de salvar recupera a jornada anterior.");
        Assert(store.TryWrite(1, changed, out _), "Salvamento explicito registra as alteracoes acumuladas.");
        Assert(store.TryRead(1, out saved, out _) && saved.Progress.Gold == session.Progress.Gold
            && saved.Party.Members[0].Armor == null && saved.Party.Members[0].Limit.Charge == 42 && saved.Secrets.KnowsPoem
            && saved.Data.Map == PrototypeTravel.Inn,
            "Salvar e carregar preservam o progresso acumulado e a posicao na hospedaria.");
        Assert(previous.SequenceEqual(File.ReadAllBytes(store.SlotPath(1) + ".bak")), "Salvamento manual conserva o backup anterior.");
        Assert(!store.Exists(2) && !store.Exists(3), "Salvar um slot nao cria as outras jornadas.");
    }
    public static int Main(string[] args)
    {
        try
        {
            CheckDomain(); CheckInventoryEvents(); CheckLevelPersistence(); CheckConditionPersistence(); CheckCheatPersistence(); CheckPlayTime();
            CheckManualPersistence(Path.Combine(args[0], "manual-" + Guid.NewGuid().ToString("N")));
            CheckFiles(Path.Combine(args[0], "saves-" + Guid.NewGuid().ToString("N")));
            Console.WriteLine("Salvamento: " + checks + " verificacoes passaram."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); return 1; }
    }
}
