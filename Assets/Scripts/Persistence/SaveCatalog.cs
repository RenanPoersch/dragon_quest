using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Progression;
using P = DragonQuest.Prototype.PrototypePartyFactory;

namespace DragonQuest.Persistence
{
    public static class SaveCatalog
    {
        private static readonly Dictionary<string, EquipmentDefinition> gear = new Dictionary<string, EquipmentDefinition>
        {
            {"initial-weapon", P.InitialWeapon}, {"initial-armor", P.InitialArmor}, {"iron-sword", P.IronSword},
            {"runic-staff", P.RunicStaff}, {"iron-armor", P.IronArmor}, {"arcane-robe", P.ArcaneRobe}, {"dragon-armor", DragonProgress.Armor}
        };
        private static readonly Dictionary<string, MateriaDefinition> materia = new Dictionary<string, MateriaDefinition>();
        private static readonly Dictionary<string, ConsumableDefinition> items = new Dictionary<string, ConsumableDefinition>();
        static SaveCatalog()
        {
            foreach (var definition in new[] {P.ThunderMateria, P.FireMateria, P.IceMateria, P.CureMateria, P.CleanseMateria,
                P.ReviveMateria, P.SummonMateria, P.AllMateria, P.InfuseMateria, P.HeavyMateria, P.CleaveMateria, P.GuardianMateria})
                materia.Add(definition.Id, definition);
            foreach (var definition in new[] {P.Potion, P.Ether, P.Antidote, P.PhoenixFeather}) items.Add(definition.Id, definition);
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
