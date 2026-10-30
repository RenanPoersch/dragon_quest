using DragonQuest.Content;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Shops;

namespace DragonQuest.Content
{
    public static class ShopCatalog
    {
        public const int StartingGold = 50;
        public static ShopService Create(PartyProgress party, ExplorationProgress progress)
        {
            var items = new ShopInventory("item-shop", "Loja de itens", ShopCategory.Items,
                new ShopProduct("potion", ItemCatalog.Potion, 10, 12),
                new ShopProduct("ether", ItemCatalog.Ether, 25, 6),
                new ShopProduct("antidote", ItemCatalog.Antidote, 6, 6),
                new ShopProduct("phoenix-feather", ItemCatalog.PhoenixFeather, 50, 2));
            var equipment = new ShopInventory("equipment-shop", "Loja de equipamentos", ShopCategory.Equipment,
                new ShopProduct("iron-sword", EquipmentCatalog.IronSword, 60, 2),
                new ShopProduct("runic-staff", EquipmentCatalog.RunicStaff, 80, 2),
                new ShopProduct("iron-armor", EquipmentCatalog.IronArmor, 70, 2),
                new ShopProduct("arcane-robe", EquipmentCatalog.ArcaneRobe, 100, 2),
                new ShopProduct("power-ring", EquipmentCatalog.PowerRing, 90, 2),
                new ShopProduct("guardian-bracelet", EquipmentCatalog.GuardianBracelet, 90, 2),
                new ShopProduct("arcane-pendant", EquipmentCatalog.ArcanePendant, 90, 2),
                // Sem estoque de compra; registram o valor de venda das pecas iniciais.
                new ShopProduct("initial-weapon", EquipmentCatalog.InitialWeapon, 30, 0),
                new ShopProduct("initial-armor", EquipmentCatalog.InitialArmor, 40, 0));
            var materia = new ShopInventory("materia-shop", "Loja de materias", ShopCategory.Materia,
                new ShopProduct("fire", MateriaCatalog.FireMateria, 50, 2),
                new ShopProduct("ice", MateriaCatalog.IceMateria, 50, 2),
                new ShopProduct("thunder", MateriaCatalog.ThunderMateria, 50, 2),
                new ShopProduct("cure", MateriaCatalog.CureMateria, 50, 2),
                new ShopProduct("cleanse", MateriaCatalog.CleanseMateria, 30, 2),
                new ShopProduct("revive", MateriaCatalog.ReviveMateria, 120, 1),
                new ShopProduct("all", MateriaCatalog.AllMateria, 90, 2),
                new ShopProduct("heavy", MateriaCatalog.HeavyMateria, 40, 2),
                new ShopProduct("cleave", MateriaCatalog.CleaveMateria, 70, 1),
                new ShopProduct("guardian", MateriaCatalog.GuardianMateria, 60, 1),
                new ShopProduct("infuse", MateriaCatalog.InfuseMateria, 80, 1),
                new ShopProduct("earth", MateriaCatalog.EarthMateria, 50, 2),
                new ShopProduct("chaos", MateriaCatalog.ChaosMateria, 80, 2),
                new ShopProduct("bahamut", MateriaCatalog.BahamutMateria, 250, 1),
                new ShopProduct("titan", MateriaCatalog.TitanMateria, 180, 1),
                new ShopProduct("defense", MateriaCatalog.DefenseMateria, 40, 2),
                new ShopProduct("hp", MateriaCatalog.HpMateria, 60, 2),
                new ShopProduct("mp", MateriaCatalog.MpMateria, 60, 2));
            return new ShopService(party, progress, items, equipment, materia);
        }
    }
}
