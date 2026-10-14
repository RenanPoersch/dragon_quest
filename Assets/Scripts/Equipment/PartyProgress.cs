using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Inventory;
using DragonQuest.Progression;

namespace DragonQuest.Equipment
{
    public enum CharacterClass { Attacker, Healer, Tank }

    public sealed class PartyMember
    {
        public string Id { get; }
        public string Name { get; }
        public CharacterClass Class { get; }
        public CombatantDefinition BaseStats { get; }
        public EquipmentLoadout Weapon { get; private set; }
        public EquipmentLoadout Armor { get; private set; }
        public LimitGauge Limit { get; } = new LimitGauge();
        public CharacterProgression Progression { get; } = new CharacterProgression();
        public int Level => Progression.Level;
        public int Experience => Progression.Experience;
        public int Speed => Grow(BaseStats.Speed, 1);
        public double CriticalChance => Math.Min(1, BaseStats.CriticalChance + (Level - 1) * .0025);
        private int Grow(int value, int perLevel) => (int)Math.Min(int.MaxValue, (long)value + (Level - 1) * perLevel);
        private int HpGrowth => Class == CharacterClass.Tank ? 14 : Class == CharacterClass.Attacker ? 10 : 7;
        private int MpGrowth => Class == CharacterClass.Healer ? 5 : 2;
        private int AttackGrowth => Class == CharacterClass.Attacker ? 3 : 1;
        private int DefenseGrowth => Class == CharacterClass.Tank ? 3 : 1;
        private int MagicGrowth => Class == CharacterClass.Healer ? 3 : 1;
        public string AffinityDescription => Class == CharacterClass.Attacker ? "+20% no ataque base da arma"
            : Class == CharacterClass.Healer ? "+25% no poder de magia, invocacao e cura" : "+25% na defesa base da armadura";
        public int Attack => AttackWith(Weapon, Armor);
        public int Defense => DefenseWith(Weapon, Armor);
        public int Magic => MagicWith(Weapon, Armor);
        public int MaxHp => StatWithMaterias(Grow(BaseStats.MaxHp, HpGrowth), MateriaModifier.HpUp, Weapon, Armor);
        public int MaxMp => StatWithMaterias(Grow(BaseStats.MaxMp, MpGrowth), MateriaModifier.MpUp, Weapon, Armor);
        public ElementalAffinities Affinities => ElementalAffinities.Combine(BaseStats.Affinities,
            Weapon?.Definition.Affinities, Armor?.Definition.Affinities,
            new ElementalAffinities(resistances: new List<DamageElement>(new HashSet<DamageElement>(MateriaResolver.InfusedElements(Armor))).ToArray()));

        private static int StatWithMaterias(long value, MateriaModifier modifier, EquipmentLoadout weapon, EquipmentLoadout armor)
        {
            long percent = 100;
            foreach (var equipment in new[] { weapon, armor })
                if (equipment != null)
                    foreach (var materia in equipment.Sockets)
                        if (materia != null && materia.Definition.Modifier == modifier) percent += 20;
            return (int)Math.Min(int.MaxValue, (long)value * percent / 100);
        }

        public PartyMember(string id, string name, CharacterClass characterClass, CombatantDefinition stats,
            EquipmentDefinition weapon, EquipmentDefinition armor)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Membro precisa de ID.");
            if (!Enum.IsDefined(typeof(CharacterClass), characterClass)) throw new ArgumentException("Classe desconhecida.");
            Id = id; Name = name; Class = characterClass;
            BaseStats = stats ?? throw new ArgumentNullException(nameof(stats));
            if (weapon == null || armor == null || weapon.Slot != EquipmentSlot.Weapon || armor.Slot != EquipmentSlot.Armor)
                throw new ArgumentException("Arma e armadura iniciais precisam dos tipos corretos.");
            Weapon = new EquipmentLoadout(weapon, id + "/weapon"); Armor = new EquipmentLoadout(armor, id + "/armor");
        }

        private int Bonus(int value, CharacterClass affinity, int percent) => Class == affinity ? (int)Math.Min(int.MaxValue, (long)value * (100 + percent) / 100) : value;
        public int AttackWith(EquipmentLoadout weapon, EquipmentLoadout armor) => StatWithMaterias((long)Grow(BaseStats.Attack, AttackGrowth)
            + Bonus(weapon?.Definition.AttackBonus ?? 0, CharacterClass.Attacker, 20) + (armor?.Definition.AttackBonus ?? 0), MateriaModifier.AttackUp, weapon, armor);
        public int DefenseWith(EquipmentLoadout weapon, EquipmentLoadout armor) => StatWithMaterias((long)Grow(BaseStats.Defense, DefenseGrowth)
            + (weapon?.Definition.DefenseBonus ?? 0) + Bonus(armor?.Definition.DefenseBonus ?? 0, CharacterClass.Tank, 25), MateriaModifier.DefenseUp, weapon, armor);
        public int MagicWith(EquipmentLoadout weapon, EquipmentLoadout armor) => (int)Math.Min(int.MaxValue, (long)Grow(BaseStats.Magic, MagicGrowth)
            + (weapon?.Definition.MagicBonus ?? 0) + (armor?.Definition.MagicBonus ?? 0));
        internal void SetEquipment(EquipmentSlot slot, EquipmentLoadout item)
        {
            if (slot == EquipmentSlot.Weapon) Weapon = item; else Armor = item;
        }

        public CombatantState CreateCombatant()
        {
            var abilities = MateriaResolver.Resolve(this);
            var commands = new AbilityDefinition[abilities.Count];
            abilities.CopyTo(commands);
            var stats = new CombatantDefinition(Name, BaseStats.Role, MaxHp, MaxMp,
                Attack, Defense, Magic, Speed, Affinities, BaseStats.GoldReward, BaseStats.ExperienceReward, CriticalChance, commands);
            return new CombatantState(Id, stats, CombatTeam.Party, Limit);
        }
    }

    public sealed class PartyProgress
    {
        private readonly List<PartyMember> members;
        private readonly List<MateriaInstance> materias;
        private readonly List<EquipmentLoadout> equipments = new List<EquipmentLoadout>();
        public IReadOnlyList<PartyMember> Members { get; }
        public IReadOnlyList<MateriaInstance> Materias { get; }
        public IReadOnlyList<EquipmentLoadout> Equipments { get; }
        public ItemInventory Consumables { get; private set; }
        public event Action Changed;

        // Usado somente depois de validar um snapshot, em uma equipe ainda fora do jogo.
        internal void RestoreInventory(EquipmentLoadout[] gear, MateriaInstance[] materia, ItemInventory items)
        {
            equipments.Clear(); equipments.AddRange(gear);
            materias.Clear(); materias.AddRange(materia); Consumables = items;
        }

        public PartyProgress(PartyMember[] party, MateriaInstance[] inventory,
            EquipmentLoadout[] spareEquipment = null, ItemInventory consumables = null)
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
            ids.Clear();
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout item in new[] { member.Weapon, member.Armor })
                    if (item != null) RegisterEquipment(item, ids);
            if (spareEquipment != null)
                foreach (EquipmentLoadout item in spareEquipment) RegisterEquipment(item, ids);
            Equipments = equipments.AsReadOnly();
            Consumables = consumables ?? new ItemInventory();
        }

        internal string AwardExperience(BattleSession battle, long experience)
        {
            var participants = new HashSet<string>();
            foreach (CombatantState combatant in battle.Combatants)
                if (combatant.Team == CombatTeam.Party) participants.Add(combatant.Id);
            var report = new List<string>();
            foreach (PartyMember member in members)
            {
                if (!participants.Contains(member.Id)) continue;
                int before = member.Experience;
                int levels = member.Progression.AddExperience(experience);
                if (levels > 0) report.Add(member.Name + " chegou ao nivel " + member.Level + "!");
                if (member.Experience != before) report.Add(member.Name + " +" + (member.Experience - before) + " XP.");
            }
            if (report.Count > 0) Changed?.Invoke();
            return string.Join(" ", report);
        }

        private void RegisterEquipment(EquipmentLoadout item, HashSet<string> ids)
        {
            if (item == null || !ids.Add(item.Id)) throw new ArgumentException("Equipamentos nulos ou duplicados.");
            equipments.Add(item);
        }

        public bool TryAddEquipments(params EquipmentLoadout[] additions)
        {
            if (additions == null || additions.Length == 0) return false;
            var ids = new HashSet<string>();
            foreach (EquipmentLoadout item in equipments) ids.Add(item.Id);
            foreach (EquipmentLoadout item in additions)
                if (item == null || !ids.Add(item.Id)) return false;
            equipments.AddRange(additions); Changed?.Invoke(); return true;
        }

        public bool TryAddMaterias(params MateriaInstance[] additions)
        {
            if (additions == null || additions.Length == 0) return false;
            var ids = new HashSet<string>();
            foreach (MateriaInstance item in materias) ids.Add(item.Id);
            foreach (MateriaInstance item in additions)
                if (item == null || !ids.Add(item.Id)) return false;
            materias.AddRange(additions); Changed?.Invoke(); return true;
        }

        public bool IsEquipped(EquipmentLoadout item)
        {
            if (item == null) return false;
            foreach (PartyMember member in members)
                if (member.Weapon == item || member.Armor == item) return true;
            return false;
        }

        public bool IsEquipped(MateriaInstance materia)
        {
            if (materia == null) return false;
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
                {
                    if (equipment == null) continue;
                    foreach (MateriaInstance socket in equipment.Sockets) if (socket == materia) return true;
                }
            return false;
        }

        public bool TryRemoveOwnedEquipment(EquipmentLoadout item)
        {
            if (item == null || item.Definition.IsStoryItem || IsEquipped(item)) return false;
            foreach (MateriaInstance socket in item.Sockets) if (socket != null) return false;
            bool removed = equipments.Remove(item); if (removed) Changed?.Invoke(); return removed;
        }

        public bool TryRemoveOwnedMateria(MateriaInstance item)
        {
            bool removed = item != null && !IsEquipped(item) && materias.Remove(item);
            if (removed) Changed?.Invoke(); return removed;
        }

        public string LocationOf(EquipmentLoadout item)
        {
            foreach (PartyMember member in members)
                if (member.Weapon == item || member.Armor == item) return member.Name;
            return "Inventario";
        }

        public bool TryEquipEquipment(PartyMember member, EquipmentLoadout item, out string message)
        {
            if (member == null || !members.Contains(member) || item == null || !equipments.Contains(item))
            { message = "Personagem ou equipamento invalido."; return false; }
            EquipmentLoadout previous = item.Definition.Slot == EquipmentSlot.Weapon ? member.Weapon : member.Armor;
            if (previous == item) { message = "Equipamento ja em uso."; return false; }
            PartyMember source = null;
            foreach (PartyMember owner in members)
                if (owner.Weapon == item || owner.Armor == item) source = owner;
            // Toda troca desequipa as materias dos equipamentos envolvidos, sem perder as instancias.
            previous?.ClearMaterias();
            item.ClearMaterias();
            if (source != null) source.SetEquipment(item.Definition.Slot, previous);
            member.SetEquipment(item.Definition.Slot, item);
            Changed?.Invoke();
            message = item.Definition.Name + " equipado. Materias dos equipamentos trocados voltaram ao inventario.";
            return true;
        }

        public bool TryRemoveEquipment(PartyMember member, EquipmentSlot slot, out string message)
        {
            if (member == null || !members.Contains(member) || !Enum.IsDefined(typeof(EquipmentSlot), slot))
            { message = "Personagem ou tipo de equipamento invalido."; return false; }
            EquipmentLoadout item = slot == EquipmentSlot.Weapon ? member.Weapon : member.Armor;
            if (item == null) { message = "Nao ha equipamento neste espaco."; return false; }
            item.ClearMaterias();
            member.SetEquipment(slot, null);
            Changed?.Invoke();
            message = "Equipamento e suas materias voltaram ao inventario.";
            return true;
        }

        public string LocationOf(MateriaInstance materia)
        {
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
                {
                    if (equipment == null) continue;
                    for (int i = 0; i < equipment.Sockets.Count; i++)
                        if (equipment.Sockets[i] == materia) return member.Name + " / " + equipment.Definition.Name + " / " + (i + 1);
                }
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
                {
                    if (item == null) continue;
                    for (int i = 0; i < item.Sockets.Count; i++)
                        if (item.Sockets[i] == materia) { source = item; sourceSlot = i; }
                }
            // Troca atomica; a materia antiga vai para a origem ou fica no inventario.
            if (source != null) source.Set(sourceSlot, equipment.Sockets[slot]);
            equipment.Set(slot, materia);
            Changed?.Invoke();
            message = materia.Definition.Name + " equipada em " + member.Name + ".";
            return true;
        }

        public bool TryUnequip(PartyMember member, EquipmentLoadout equipment, int slot, out string message)
        {
            if (!ValidDestination(member, equipment, slot) || equipment.Sockets[slot] == null)
            { message = "Escolha um encaixe ocupado."; return false; }
            equipment.Set(slot, null); Changed?.Invoke(); message = "Materia devolvida ao inventario."; return true;
        }

        private bool ValidDestination(PartyMember member, EquipmentLoadout equipment, int slot) => member != null && members.Contains(member) && equipment != null
            && (equipment == member.Weapon || equipment == member.Armor) && equipment.IsValidSocket(slot);
    }
}
