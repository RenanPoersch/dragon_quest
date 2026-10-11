using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Shops;

namespace DragonQuest.Prototype
{
    public static class PrototypeShopFactory
    {
        public const int StartingGold = 50;
        public static ShopService Create(PartyProgress party, ExplorationProgress progress)
        {
            var items = new ShopInventory("item-shop", "Loja de itens", ShopCategory.Items,
                new ShopProduct("potion", PrototypePartyFactory.Potion, 10, 12),
                new ShopProduct("ether", PrototypePartyFactory.Ether, 25, 6),
                new ShopProduct("antidote", PrototypePartyFactory.Antidote, 6, 6),
                new ShopProduct("phoenix-feather", PrototypePartyFactory.PhoenixFeather, 50, 2));
            var equipment = new ShopInventory("equipment-shop", "Loja de equipamentos", ShopCategory.Equipment,
                new ShopProduct("iron-sword", PrototypePartyFactory.IronSword, 60, 2),
                new ShopProduct("runic-staff", PrototypePartyFactory.RunicStaff, 80, 2),
                new ShopProduct("iron-armor", PrototypePartyFactory.IronArmor, 70, 2),
                new ShopProduct("arcane-robe", PrototypePartyFactory.ArcaneRobe, 100, 2),
                // Sem estoque de compra; registram o valor de venda das pecas iniciais.
                new ShopProduct("initial-weapon", PrototypePartyFactory.InitialWeapon, 30, 0),
                new ShopProduct("initial-armor", PrototypePartyFactory.InitialArmor, 40, 0));
            var materia = new ShopInventory("materia-shop", "Loja de materias", ShopCategory.Materia,
                new ShopProduct("fire", PrototypePartyFactory.FireMateria, 50, 2),
                new ShopProduct("ice", PrototypePartyFactory.IceMateria, 50, 2),
                new ShopProduct("thunder", PrototypePartyFactory.ThunderMateria, 50, 2),
                new ShopProduct("cure", PrototypePartyFactory.CureMateria, 50, 2),
                new ShopProduct("cleanse", PrototypePartyFactory.CleanseMateria, 30, 2),
                new ShopProduct("revive", PrototypePartyFactory.ReviveMateria, 120, 1),
                new ShopProduct("all", PrototypePartyFactory.AllMateria, 90, 2),
                new ShopProduct("heavy", PrototypePartyFactory.HeavyMateria, 40, 2),
                new ShopProduct("cleave", PrototypePartyFactory.CleaveMateria, 70, 1),
                new ShopProduct("guardian", PrototypePartyFactory.GuardianMateria, 60, 1),
                new ShopProduct("infuse", PrototypePartyFactory.InfuseMateria, 80, 1));
            return new ShopService(party, progress, items, equipment, materia);
        }
    }
}
