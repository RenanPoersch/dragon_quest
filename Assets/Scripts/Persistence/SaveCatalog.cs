using DragonQuest.Content;
using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Persistence
{
    public static class SaveCatalog
    {
        private static readonly Dictionary<string, EquipmentDefinition> gear = new Dictionary<string, EquipmentDefinition>
        {
            {"initial-weapon", EquipmentCatalog.InitialWeapon}, {"initial-armor", EquipmentCatalog.InitialArmor}, {"iron-sword", EquipmentCatalog.IronSword},
            {"runic-staff", EquipmentCatalog.RunicStaff}, {"iron-armor", EquipmentCatalog.IronArmor}, {"arcane-robe", EquipmentCatalog.ArcaneRobe}, {"dragon-armor", EquipmentCatalog.DragonArmor},
            {"power-ring", EquipmentCatalog.PowerRing}, {"guardian-bracelet", EquipmentCatalog.GuardianBracelet}, {"arcane-pendant", EquipmentCatalog.ArcanePendant}
        };
        private static readonly Dictionary<string, MateriaDefinition> materia = new Dictionary<string, MateriaDefinition>();
        private static readonly Dictionary<string, ConsumableDefinition> items = new Dictionary<string, ConsumableDefinition>();
        static SaveCatalog()
        {
            foreach (var definition in new[] {MateriaCatalog.ThunderMateria, MateriaCatalog.FireMateria, MateriaCatalog.IceMateria, MateriaCatalog.CureMateria, MateriaCatalog.CleanseMateria,
                MateriaCatalog.ReviveMateria, MateriaCatalog.SummonMateria, MateriaCatalog.AllMateria, MateriaCatalog.InfuseMateria, MateriaCatalog.HeavyMateria, MateriaCatalog.CleaveMateria, MateriaCatalog.GuardianMateria, MateriaCatalog.EarthMateria, MateriaCatalog.ChaosMateria, MateriaCatalog.BahamutMateria, MateriaCatalog.TitanMateria, MateriaCatalog.DefenseMateria, MateriaCatalog.HpMateria, MateriaCatalog.MpMateria})
                materia.Add(definition.Id, definition);
            foreach (var definition in new[] {ItemCatalog.Potion, ItemCatalog.Ether, ItemCatalog.Antidote, ItemCatalog.PhoenixFeather}) items.Add(definition.Id, definition);
        }
        public static string GearId(EquipmentDefinition definition)
        {
            foreach (var pair in gear) if (pair.Value == definition) return pair.Key;
            throw new ArgumentException("Equipamento fora do catalogo de salvamento.");
        }
        public static EquipmentDefinition Gear(string id) => Find(gear, id);
        public static MateriaDefinition Materia(string id) => Find(materia, id);
        public static ConsumableDefinition Item(string id) => Find(items, id);
        private static T Find<T>(Dictionary<string, T> definitions, string id)
        {
            if (id == null || !definitions.TryGetValue(id, out T value)) throw new ArgumentException("Definicao desconhecida: " + id);
            return value;
        }
    }
}
