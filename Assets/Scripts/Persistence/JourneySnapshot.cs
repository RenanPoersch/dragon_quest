using System;
using System.Collections.Generic;
using System.Globalization;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using DragonQuest.Shops;
using UnityEngine;

namespace DragonQuest.Persistence
{
    public sealed class RestoredJourney
    {
        public JourneyData Data { get; internal set; }
        public PartyProgress Party { get; internal set; }
        public ExplorationProgress Progress { get; internal set; }
        public AncientSecrets Secrets { get; internal set; }
        public DragonProgress Dragon { get; internal set; }
        public ShopService Shops { get; internal set; }
        public DeliveryQuest Quest { get; internal set; }
    }

    public static class JourneySnapshot
    {
        public static JourneyData Capture(PartyProgress party, ExplorationProgress progress, AncientSecrets secrets,
            DragonProgress dragon, ShopService shops, DeliveryQuest quest, string map, float x, float y, GameEnding ending = GameEnding.None)
        {
            var members = new List<MemberData>(); var gear = new List<GearData>();
            var materias = new List<MateriaData>(); var items = new List<StackData>(); var stocks = new List<StockData>();
            foreach (var member in party.Members) members.Add(new MemberData
                {Id = member.Id, Weapon = member.Weapon?.Id, Armor = member.Armor?.Id, Limit = member.Limit.SavedCharge, Experience = member.Experience,
                    Hp = member.Hp, Mp = member.Mp, Poisoned = member.IsPoisoned, BoundActions = member.BoundActions});
            foreach (var equipment in party.Equipments)
            {
                var sockets = new string[equipment.Sockets.Count];
                for (int i = 0; i < sockets.Length; i++) sockets[i] = equipment.Sockets[i]?.Id;
                gear.Add(new GearData {Id = equipment.Id, Definition = SaveCatalog.GearId(equipment.Definition), Sockets = sockets});
            }
            foreach (var materia in party.Materias) materias.Add(new MateriaData {Id = materia.Id, Definition = materia.Definition.Id});
            foreach (var item in party.Consumables.Items) items.Add(new StackData {Definition = item.Definition.Id, Quantity = item.Quantity});
            foreach (var shop in shops.Shops)
                foreach (var product in shop.Products) stocks.Add(new StockData {Shop = shop.Id, Product = product.Id, Quantity = product.Stock});
            string[] chests = progress.OpenedChests; Array.Sort(chests, StringComparer.Ordinal);
            return new JourneyData
            {
                Version = JourneyData.CurrentVersion, SavedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Map = map, X = x, Y = y, Ending = (int)ending, Gold = progress.SavedGold, OpenedChests = chests,
                Secrets = new SecretData {KnowsPoem = secrets.KnowsPoem, AltarSolved = secrets.AltarSolved,
                    MonolithStep = secrets.MonolithStep, HasScale = secrets.HasScale, PassageOpen = secrets.PassageOpen},
                DragonDefeated = dragon.IsDefeated, Members = members.ToArray(), Gear = gear.ToArray(),
                Materias = materias.ToArray(), Items = items.ToArray(), Stocks = stocks.ToArray(),
                DeliveryStage = (int)quest.Stage, DeliveryRestocked = shops.HasRestockedMilestone(DeliveryQuest.MilestoneId)
            };
        }

        // Reconstroi uma jornada independente. Falhas nunca alteram a jornada em andamento.
        public static RestoredJourney Restore(JourneyData data)
        {
            Require(data != null && data.Version == JourneyData.CurrentVersion, "Versao de save nao suportada.");
            Require(DateTimeOffset.TryParseExact(data.SavedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Data de save invalida.");
            Require(!data.PlaySeconds.HasValue || data.PlaySeconds.Value >= 0, "Tempo de jogo invalido.");
            Require(data.Gold >= 0 && Enum.IsDefined(typeof(GameEnding), data.Ending), "Ouro ou final invalido.");
            Require(data.Secrets != null, "Segredos ausentes."); var s = data.Secrets;
            Require(s.MonolithStep >= 0 && s.MonolithStep <= 3 && (!s.AltarSolved || s.KnowsPoem)
                && (!s.PassageOpen || s.AltarSolved && s.MonolithStep == 3 && s.HasScale), "Progresso de segredos invalido.");
            Require(!data.DragonDefeated || s.PassageOpen, "Dragao derrotado antes de abrir a passagem.");
            var secrets = new AncientSecrets(); secrets.Restore(s.KnowsPoem, s.AltarSolved, s.MonolithStep, s.HasScale, s.PassageOpen);
            var travel = new PrototypeTravel(secrets);
            Require(travel.TryRestore(data.Map, new Vector2(data.X, data.Y)), "Mapa ou posicao de save invalida.");
            Require(data.Ending == 0 || data.Map == PrototypeTravel.Castle, "Final fora do castelo.");
            Require(data.Ending != (int)GameEnding.KingFreed || data.DragonDefeated, "Libertacao sem armadura draconica.");
            CheckArray(data.OpenedChests, 1); var chestIds = new HashSet<string>();
            foreach (string id in data.OpenedChests) Require(id == "praca-primeiro-bau" && chestIds.Add(id), "Bau desconhecido ou repetido.");
            var progress = new ExplorationProgress(); progress.Restore(data.Gold, data.OpenedChests);
            CheckArray(data.Gear, 512); CheckArray(data.Materias, 512); CheckArray(data.Items, 4); CheckArray(data.Members, 3);
            Require(data.Members.Length == 3, "Equipe incompleta.");
            var gear = new Dictionary<string, EquipmentLoadout>(); var materias = new Dictionary<string, MateriaInstance>();
            foreach (var m in data.Materias)
            {
                Require(m != null && ValidId(m.Id) && !materias.ContainsKey(m.Id), "Materia duplicada ou invalida.");
                materias.Add(m.Id, new MateriaInstance(m.Id, SaveCatalog.Materia(m.Definition)));
            }
            int dragonArmorCount = 0;
            foreach (var g in data.Gear)
            {
                Require(g != null && ValidId(g.Id) && !gear.ContainsKey(g.Id), "Equipamento duplicado ou invalido.");
                var definition = SaveCatalog.Gear(g.Definition);
                CheckArray(g.Sockets, 6); Require(g.Sockets.Length == definition.SocketCount, "Quantidade de encaixes invalida.");
                if (definition == DragonProgress.Armor) { dragonArmorCount++; Require(g.Id == "dragon-armor", "ID da armadura draconica invalido."); }
                gear.Add(g.Id, new EquipmentLoadout(definition, g.Id));
            }
            Require(dragonArmorCount == (data.DragonDefeated ? 1 : 0), "Recompensa do dragao inconsistente.");
            var stacks = new List<ItemStack>(); var itemIds = new HashSet<string>();
            foreach (var item in data.Items)
            {
                Require(item != null && item.Quantity >= 0 && itemIds.Add(item.Definition), "Pilha de item invalida.");
                stacks.Add(new ItemStack(SaveCatalog.Item(item.Definition), item.Quantity));
            }
            var party = PrototypePartyFactory.Create();
            var gearList = new List<EquipmentLoadout>(); foreach (var g in data.Gear) gearList.Add(gear[g.Id]);
            var materiaList = new List<MateriaInstance>(); foreach (var m in data.Materias) materiaList.Add(materias[m.Id]);
            party.RestoreInventory(gearList.ToArray(), materiaList.ToArray(), new ItemInventory(stacks.ToArray()));
            var memberIds = new HashSet<string>(); var usedGear = new HashSet<string>(); var usedMateria = new HashSet<string>();
            foreach (var saved in data.Members)
            {
                Require(saved != null && ValidId(saved.Id) && memberIds.Add(saved.Id) && saved.Limit >= 0 && saved.Limit <= 100, "Membro ou Limit invalido.");
                PartyMember member = null; foreach (var candidate in party.Members) if (candidate.Id == saved.Id) member = candidate;
                Require(member != null, "Personagem desconhecido.");
                member.SetEquipment(EquipmentSlot.Weapon, AssignGear(saved.Weapon, EquipmentSlot.Weapon, gear, usedGear));
                member.SetEquipment(EquipmentSlot.Armor, AssignGear(saved.Armor, EquipmentSlot.Armor, gear, usedGear));
                member.Limit.Restore(saved.Limit);
                member.Progression.Restore(saved.Experience);
            }
            foreach (var g in data.Gear)
                for (int i = 0; i < g.Sockets.Length; i++)
                {
                    string id = g.Sockets[i]; if (id == null) continue;
                    Require(usedGear.Contains(g.Id) && materias.ContainsKey(id) && usedMateria.Add(id), "Materia em encaixe invalido ou duplicado.");
                    gear[g.Id].Set(i, materias[id]);
                }
            // Gear and materia must be installed before validating derived HP/MP limits.
            foreach (var saved in data.Members)
                foreach (var member in party.Members)
                    if (member.Id == saved.Id)
                    {
                        int hp = saved.Hp ?? member.MaxHp, mp = saved.Mp ?? member.MaxMp;
                        Require(hp >= 0 && hp <= member.MaxHp && mp >= 0 && mp <= member.MaxMp
                            && saved.BoundActions >= 0 && saved.BoundActions <= 1
                            && (hp > 0 || !saved.Poisoned && saved.BoundActions == 0), "HP, MP ou condicoes invalidos.");
                        member.RestoreVitals(hp, mp, saved.Poisoned, saved.BoundActions);
                    }
            var shops = PrototypeShopFactory.Create(party, progress); var products = new Dictionary<string, ShopProduct>();
            foreach (var shop in shops.Shops) foreach (var product in shop.Products) products.Add(shop.Id + "/" + product.Id, product);
            CheckArray(data.Stocks, products.Count); Require(data.Stocks.Length == products.Count, "Estoque incompleto.");
            var stockIds = new HashSet<string>();
            foreach (var stock in data.Stocks)
            {
                Require(stock != null, "Estoque ausente."); string key = stock.Shop + "/" + stock.Product;
                Require(products.TryGetValue(key, out var product) && stockIds.Add(key)
                    && stock.Quantity >= 0 && stock.Quantity <= product.InitialStock, "Estoque invalido.");
                product.RestoreStock(stock.Quantity);
            }
            Require(Enum.IsDefined(typeof(DeliveryStage), data.DeliveryStage)
                && data.DeliveryRestocked == (data.DeliveryStage == (int)DeliveryStage.Completed), "Missao/reposicao inconsistente.");
            shops.RestoreDeliveryRestock(data.DeliveryRestocked);
            var quest = new DeliveryQuest(shops); quest.Restore((DeliveryStage)data.DeliveryStage);
            var dragon = new DragonProgress(); dragon.Restore(data.DragonDefeated);
            return new RestoredJourney {Data = data, Party = party, Progress = progress, Secrets = secrets, Dragon = dragon, Shops = shops, Quest = quest};
        }
        private static EquipmentLoadout AssignGear(string id, EquipmentSlot slot, Dictionary<string, EquipmentLoadout> gear, HashSet<string> used)
        {
            if (id == null) return null;
            Require(gear.TryGetValue(id, out var item) && item.Definition.Slot == slot && used.Add(id), "Equipamento ocupado, ausente ou de tipo errado.");
            return item;
        }
        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 128;
        private static void CheckArray<T>(T[] array, int max) => Require(array != null && array.Length <= max, "Lista ausente ou excessiva.");
        private static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
    }
}
