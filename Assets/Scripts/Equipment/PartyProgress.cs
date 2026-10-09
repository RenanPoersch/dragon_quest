using System;
using System.Collections.Generic;
using DragonQuest.Combat;

namespace DragonQuest.Equipment
{
    public enum CharacterClass { Attacker, Healer, Tank }

    public sealed class PartyMember
    {
        public string Id { get; }
        public string Name { get; }
        public CharacterClass Class { get; }
        public CombatantDefinition BaseStats { get; }
        public EquipmentLoadout Weapon { get; }
        public EquipmentLoadout Armor { get; }
        public LimitGauge Limit { get; } = new LimitGauge();
        public string AffinityDescription => Class == CharacterClass.Attacker ? "+20% no ataque base da arma"
            : Class == CharacterClass.Healer ? "+25% no poder de magia, invocacao e cura" : "+25% na defesa base da armadura";
        public int Attack => BaseStats.Attack + Bonus(Weapon.Definition.AttackBonus, CharacterClass.Attacker, 20) + Armor.Definition.AttackBonus;
        public int Defense => BaseStats.Defense + Weapon.Definition.DefenseBonus + Bonus(Armor.Definition.DefenseBonus, CharacterClass.Tank, 25);

        public PartyMember(string id, string name, CharacterClass characterClass, CombatantDefinition stats,
            EquipmentDefinition weapon, EquipmentDefinition armor)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Membro precisa de ID.");
            if (!Enum.IsDefined(typeof(CharacterClass), characterClass)) throw new ArgumentException("Classe desconhecida.");
            Id = id; Name = name; Class = characterClass;
            BaseStats = stats ?? throw new ArgumentNullException(nameof(stats));
            Weapon = new EquipmentLoadout(weapon); Armor = new EquipmentLoadout(armor);
        }

        private int Bonus(int value, CharacterClass affinity, int percent) => Class == affinity ? (int)((long)value * (100 + percent) / 100) : value;

        public CombatantState CreateCombatant()
        {
            var abilities = MateriaResolver.Resolve(this);
            var commands = new AbilityDefinition[abilities.Count];
            abilities.CopyTo(commands);
            var stats = new CombatantDefinition(Name, BaseStats.Role, BaseStats.MaxHp, BaseStats.MaxMp,
                Attack, Defense, BaseStats.Magic, BaseStats.Speed, commands);
            return new CombatantState(Id, stats, CombatTeam.Party, Limit);
        }
    }

    public sealed class PartyProgress
    {
        private readonly List<PartyMember> members;
        private readonly List<MateriaInstance> materias;
        public IReadOnlyList<PartyMember> Members { get; }
        public IReadOnlyList<MateriaInstance> Materias { get; }

        public PartyProgress(PartyMember[] party, MateriaInstance[] inventory)
        {
            if (party == null || inventory == null) throw new ArgumentNullException(nameof(party));
            var ids = new HashSet<string>();
            foreach (PartyMember member in party)
                if (member == null || !ids.Add(member.Id)) throw new ArgumentException("Membros nulos ou duplicados.");
            ids.Clear();
            foreach (MateriaInstance materia in inventory)
                if (materia == null || !ids.Add(materia.Id)) throw new ArgumentException("Materias nulas ou duplicadas.");
            members = new List<PartyMember>(party); materias = new List<MateriaInstance>(inventory);
            Members = members.AsReadOnly(); Materias = materias.AsReadOnly();
        }

        public string LocationOf(MateriaInstance materia)
        {
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
                    for (int i = 0; i < equipment.Sockets.Count; i++)
                        if (equipment.Sockets[i] == materia) return member.Name + " / " + equipment.Definition.Name + " / " + (i + 1);
            return "Inventario";
        }

        public bool TryEquip(PartyMember member, EquipmentLoadout equipment, int slot, MateriaInstance materia, out string message)
        {
            if (!ValidDestination(member, equipment, slot) || materia == null || !materias.Contains(materia))
            { message = "Materia ou encaixe invalido."; return false; }
            if (equipment.Sockets[slot] == materia) { message = "Materia ja equipada neste encaixe."; return false; }
            EquipmentLoadout source = null;
            int sourceSlot = -1;
            foreach (PartyMember owner in members)
                foreach (EquipmentLoadout item in new[] { owner.Weapon, owner.Armor })
                    for (int i = 0; i < item.Sockets.Count; i++)
                        if (item.Sockets[i] == materia) { source = item; sourceSlot = i; }
            // Troca atomica; a materia antiga vai para a origem ou fica no inventario.
            if (source != null) source.Set(sourceSlot, equipment.Sockets[slot]);
            equipment.Set(slot, materia);
            message = materia.Definition.Name + " equipada em " + member.Name + ".";
            return true;
        }

        public bool TryUnequip(PartyMember member, EquipmentLoadout equipment, int slot, out string message)
        {
            if (!ValidDestination(member, equipment, slot) || equipment.Sockets[slot] == null)
            { message = "Escolha um encaixe ocupado."; return false; }
            equipment.Set(slot, null); message = "Materia devolvida ao inventario."; return true;
        }

        private bool ValidDestination(PartyMember member, EquipmentLoadout equipment, int slot) => member != null && members.Contains(member)
            && (equipment == member.Weapon || equipment == member.Armor) && equipment.IsValidSocket(slot);
    }
}
