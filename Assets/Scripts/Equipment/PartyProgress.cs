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
        public EquipmentLoadout Accessory { get; private set; }
        public EquipmentLoadout EquipmentIn(EquipmentSlot slot) => slot == EquipmentSlot.Weapon ? Weapon
            : slot == EquipmentSlot.Armor ? Armor : slot == EquipmentSlot.Accessory ? Accessory : null;
        public LimitGauge Limit { get; } = new LimitGauge();
        public CharacterProgression Progression { get; } = new CharacterProgression();
        private int? currentHp, currentMp;
        public int Hp => (currentHp = Math.Min(currentHp ?? MaxHp, MaxHp)).Value;
        public int Mp => (currentMp = Math.Min(currentMp ?? MaxMp, MaxMp)).Value;
        public bool IsPoisoned { get; private set; }
        public int BoundActions { get; private set; }
        public bool IsAlive => Hp > 0;

        internal void RestoreVitals(int hp, int mp, bool poisoned, int boundActions)
        {
            currentHp = Math.Max(0, Math.Min(MaxHp, hp));
            currentMp = Math.Max(0, Math.Min(MaxMp, mp));
            IsPoisoned = currentHp > 0 && poisoned;
            BoundActions = currentHp > 0 ? Math.Max(0, boundActions) : 0;
        }

        internal void CaptureVitals(CombatantState state) => RestoreVitals(state.Hp, state.Mp, state.IsPoisoned, state.BoundActions);

        public int Level => Progression.Level;
        public int Experience => Progression.Experience;
        public int Speed => Grow(BaseStats.Speed, 1);
        public double CriticalChance => Math.Min(1, BaseStats.CriticalChance + (Level - 1) * .0025);
        private int Grow(int value, int perLevel) => (int)Math.Min(int.MaxValue, (long)value + (Level - 1) * perLevel);
        private int HpGrowth => Class == CharacterClass.Tank ? 14 : Class == CharacterClass.Attacker ? 10 : 7;
        private int MpGrowth => Class == CharacterClass.Healer ? 5 : 2;
        private int AttackGrowth => 1;
        private int DefenseGrowth => 1;
        private int MagicGrowth => Class == CharacterClass.Healer ? 2 : 1;
        public string AffinityDescription => Class == CharacterClass.Attacker ? "+20% no ataque base da arma"
            : Class == CharacterClass.Healer ? "+25% no poder de magia, invocacao e cura" : "+25% na defesa base da armadura";
        public int Attack => AttackWith(Weapon, Armor);
        public int Defense => DefenseWith(Weapon, Armor);
        public int Magic => MagicWith(Weapon, Armor);
        public int MaxHp => MaxHpWith(Weapon, Armor);
        public int MaxMp => MaxMpWith(Weapon, Armor);
        public int MaxHpWith(EquipmentLoadout weapon, EquipmentLoadout armor)
            => StatWithMaterias(Grow(BaseStats.MaxHp, HpGrowth), MateriaModifier.HpUp, weapon, armor);
        public int MaxMpWith(EquipmentLoadout weapon, EquipmentLoadout armor)
            => StatWithMaterias(Grow(BaseStats.MaxMp, MpGrowth), MateriaModifier.MpUp, weapon, armor);
        public ElementalAffinities Affinities => ElementalAffinities.Combine(BaseStats.Affinities,
            Weapon?.Definition.Affinities, Armor?.Definition.Affinities,
            Accessory?.Definition.Affinities,
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
        public int AttackWith(EquipmentLoadout weapon, EquipmentLoadout armor) => AttackWith(weapon, armor, Accessory);
        public int AttackWith(EquipmentLoadout weapon, EquipmentLoadout armor, EquipmentLoadout accessory, bool preserveEquippedMaterias = false) => StatWithMaterias((long)Grow(BaseStats.Attack, AttackGrowth)
            + Bonus(weapon?.Definition.AttackBonus ?? 0, CharacterClass.Attacker, 20) + (armor?.Definition.AttackBonus ?? 0)
            + (accessory?.Definition.AttackBonus ?? 0), MateriaModifier.AttackUp,
                preserveEquippedMaterias ? Weapon : weapon, preserveEquippedMaterias ? Armor : armor);
        public int DefenseWith(EquipmentLoadout weapon, EquipmentLoadout armor) => DefenseWith(weapon, armor, Accessory);
        public int DefenseWith(EquipmentLoadout weapon, EquipmentLoadout armor, EquipmentLoadout accessory, bool preserveEquippedMaterias = false) => StatWithMaterias((long)Grow(BaseStats.Defense, DefenseGrowth)
            + (weapon?.Definition.DefenseBonus ?? 0) + Bonus(armor?.Definition.DefenseBonus ?? 0, CharacterClass.Tank, 25)
            + (accessory?.Definition.DefenseBonus ?? 0), MateriaModifier.DefenseUp,
                preserveEquippedMaterias ? Weapon : weapon, preserveEquippedMaterias ? Armor : armor);
        public int MagicWith(EquipmentLoadout weapon, EquipmentLoadout armor) => MagicWith(weapon, armor, Accessory);
        public int MagicWith(EquipmentLoadout weapon, EquipmentLoadout armor, EquipmentLoadout accessory) => (int)Math.Min(int.MaxValue, (long)Grow(BaseStats.Magic, MagicGrowth)
            + (weapon?.Definition.MagicBonus ?? 0) + (armor?.Definition.MagicBonus ?? 0) + (accessory?.Definition.MagicBonus ?? 0));
        internal void SetEquipment(EquipmentSlot slot, EquipmentLoadout item)
        {
            if (slot == EquipmentSlot.Weapon) Weapon = item;
            else if (slot == EquipmentSlot.Armor) Armor = item;
            else if (slot == EquipmentSlot.Accessory) Accessory = item;
        }

        public CombatantState CreateCombatant()
        {
            var abilities = MateriaResolver.Resolve(this);
            var commands = new AbilityDefinition[abilities.Count];
            abilities.CopyTo(commands);
            var stats = new CombatantDefinition(Name, BaseStats.Role, MaxHp, MaxMp,
                Attack, Defense, Magic, Speed, Affinities, BaseStats.GoldReward, BaseStats.ExperienceReward, CriticalChance, commands);
            var state = new CombatantState(Id, stats, CombatTeam.Party, Limit);
            state.RestoreVitals(Hp, Mp, IsPoisoned, BoundActions);
            return state;
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
        public event Action<PartyMember, int> PoisonDamage;
        public bool HasInfiniteLimit { get; private set; }

        public void SetInfiniteLimit(bool enabled)
        {
            HasInfiniteLimit = enabled;
            foreach (var member in members) member.Limit.SetInfinite(enabled);
            Changed?.Invoke();
        }

        public void SetMaxLevel(BattleSession activeBattle = null)
        {
            foreach (var member in members)
            {
                CombatantState participant = null;
                if (activeBattle != null)
                    foreach (var state in activeBattle.Combatants)
                        if (state.Team == CombatTeam.Party && state.Id == member.Id) { participant = state; break; }
                if (participant != null) member.CaptureVitals(participant);
                int hp = member.Hp, mp = member.Mp;
                member.Progression.AddExperience(CharacterProgression.MaxExperience);
                member.RestoreVitals(hp, mp, member.IsPoisoned, member.BoundActions);
                participant?.RefreshDefinition(member.CreateCombatant().Definition);
            }
            Changed?.Invoke();
        }

        public const float PoisonTickSeconds = 3f;
        private float poisonElapsed;
        public bool HasLivingMembers
        {
            get { foreach (var member in members) if (member.IsAlive) return true; return false; }
        }

        public void CaptureBattleState(BattleSession battle)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            foreach (var state in battle.Combatants)
                if (state.Team == CombatTeam.Party)
                    foreach (var member in members)
                        if (member.Id == state.Id) member.CaptureVitals(state);
            Changed?.Invoke();
        }

        public void AdvancePoison(float seconds, bool paused = false)
        {
            if (paused || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            bool poisoned = false;
            foreach (var member in members) poisoned |= member.IsAlive && member.IsPoisoned;
            if (!poisoned) { poisonElapsed = 0; return; }
            poisonElapsed += seconds;
            while (poisonElapsed >= PoisonTickSeconds && poisoned)
            {
                poisonElapsed -= PoisonTickSeconds;
                poisoned = false;
                foreach (var member in members)
                {
                    if (!member.IsAlive || !member.IsPoisoned) continue;
                    var state = member.CreateCombatant();
                    int damage = state.TickPoison();
                    member.CaptureVitals(state);
                    PoisonDamage?.Invoke(member, damage);
                    poisoned |= member.IsPoisoned;
                }
                Changed?.Invoke();
            }
            if (!poisoned) poisonElapsed = 0;
        }

        public void RecoverAtInn()
        {
            foreach (var member in members) member.RestoreVitals(member.MaxHp, member.MaxMp, false, 0);
            poisonElapsed = 0;
            Changed?.Invoke();
        }

        public bool TryUseItem(ConsumableDefinition item, PartyMember member, out string message)
        {
            if (member == null || !members.Contains(member) || item == null || Consumables.QuantityOf(item) == 0)
            { message = "Item ou personagem indisponivel."; return false; }
            bool valid = item.Effect == ItemEffect.Revive ? !member.IsAlive : member.IsAlive &&
                (item.Effect == ItemEffect.RestoreHp ? member.Hp < member.MaxHp :
                 item.Effect == ItemEffect.RestoreMp ? member.Mp < member.MaxMp : member.IsPoisoned);
            if (!valid) { message = "Este personagem nao precisa deste item."; return false; }
            var state = member.CreateCombatant();
            if (!Consumables.TryConsume(item)) { message = "Item esgotado."; return false; }
            if (item.Effect == ItemEffect.RestoreHp) state.RecoverHp(item.Power);
            else if (item.Effect == ItemEffect.RestoreMp) state.RecoverMp(item.Power);
            else if (item.Effect == ItemEffect.CleansePoison) state.PoisonTurns = 0;
            else
            {
                state.ClearConditions();
                state.RecoverHp((int)(((long)state.Definition.MaxHp * item.Power + 99) / 100));
            }
            member.CaptureVitals(state);
            message = item.Name + " usado em " + member.Name + ".";
            Changed?.Invoke();
            return true;
        }

        public static bool IsFieldMagic(AbilityDefinition ability) => ability != null
            && ability.MenuGroup == AbilityMenuGroup.Magic && !ability.UsesLimit
            && (ability.Targets == TargetRule.Ally || ability.Targets == TargetRule.AllAllies)
            && (ability.Effect == AbilityEffect.Heal || ability.Effect == AbilityEffect.Cleanse || ability.Effect == AbilityEffect.Revive);

        public bool CanCastMagic(PartyMember caster, string abilityId, out AbilityDefinition ability, out string message)
        {
            ability = null;
            if (caster == null || !members.Contains(caster) || !caster.IsAlive || caster.BoundActions > 0)
            { message = "Este personagem nao pode conjurar."; return false; }
            // Resolve again so unequipping a materia also revokes its field spell and linked cost.
            foreach (var command in MateriaResolver.Resolve(caster))
                if (command.Id == abilityId && IsFieldMagic(command)) { ability = command; break; }
            if (ability == null) { message = "Magia indisponivel."; return false; }
            if (caster.Mp < ability.MpCost) { message = "MP insuficiente."; return false; }
            message = ""; return true;
        }

        public static bool IsMagicTarget(AbilityDefinition ability, PartyMember target)
        {
            if (!IsFieldMagic(ability) || target == null) return false;
            if (ability.Effect == AbilityEffect.Revive) return !target.IsAlive;
            return target.IsAlive && (target.BoundActions > 0 || (ability.Effect == AbilityEffect.Heal
                ? target.Hp < target.MaxHp : target.IsPoisoned));
        }

        public bool TryCastMagic(PartyMember caster, string abilityId, PartyMember target, out string message)
        {
            if (!CanCastMagic(caster, abilityId, out var ability, out message)) return false;
            if (target == null || !members.Contains(target))
            { message = "Personagem indisponivel."; return false; }
            var targets = new List<PartyMember>();
            foreach (var member in members)
                if ((ability.Targets == TargetRule.AllAllies || member == target) && IsMagicTarget(ability, member)) targets.Add(member);
            if (targets.Count == 0) { message = "Nenhum alvo precisa desta magia."; return false; }

            foreach (var member in targets)
            {
                var state = member.CreateCombatant();
                state.TryBreakChains();
                if (ability.Effect == AbilityEffect.Heal)
                    state.RecoverHp((int)Math.Min(int.MaxValue, (long)ability.Power + caster.Magic));
                else if (ability.Effect == AbilityEffect.Cleanse) state.PoisonTurns = 0;
                else
                {
                    state.ClearConditions();
                    state.RecoverHp((int)(((long)state.Definition.MaxHp * ability.Power + 99) / 100));
                }
                member.CaptureVitals(state);
            }
            // Subtract after recovery, including when the caster is one of the targets.
            caster.RestoreVitals(caster.Hp, caster.Mp - ability.MpCost, caster.IsPoisoned, caster.BoundActions);
            message = ability.Name + " / " + (ability.Targets == TargetRule.AllAllies ? "Equipe" : target.Name);
            Changed?.Invoke();
            return true;
        }

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
                foreach (EquipmentLoadout item in new[] { member.Weapon, member.Armor, member.Accessory })
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
            var recipients = new List<PartyMember>();
            foreach (PartyMember member in members) if (participants.Contains(member.Id)) recipients.Add(member);
            if (recipients.Count == 0) return string.Empty;
            long share = experience / recipients.Count, remainder = experience % recipients.Count;
            var report = new List<string>();
            for (int i = 0; i < recipients.Count; i++)
            {
                PartyMember member = recipients[i];
                int before = member.Experience;
                int levels = member.Progression.AddExperience(share + (i < remainder ? 1 : 0));
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
                if (member.Weapon == item || member.Armor == item || member.Accessory == item) return true;
            return false;
        }

        public bool IsEquipped(MateriaInstance materia)
        {
            if (materia == null) return false;
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor, member.Accessory })
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
                if (member.Weapon == item || member.Armor == item || member.Accessory == item) return member.Name;
            return "Inventario";
        }

        public bool TryEquipEquipment(PartyMember member, EquipmentLoadout item, out string message)
        {
            if (member == null || !members.Contains(member) || item == null || !equipments.Contains(item))
            { message = "Personagem ou equipamento invalido."; return false; }
            EquipmentLoadout previous = member.EquipmentIn(item.Definition.Slot);
            if (previous == item) { message = "Equipamento ja em uso."; return false; }
            PartyMember source = null;
            foreach (PartyMember owner in members)
                if (owner.Weapon == item || owner.Armor == item || owner.Accessory == item) source = owner;
            // Toda troca desequipa as materias dos equipamentos envolvidos, sem perder as instancias.
            previous?.ClearMaterias();
            item.ClearMaterias();
            if (source != null) source.SetEquipment(item.Definition.Slot, previous);
            member.SetEquipment(item.Definition.Slot, item);
            Changed?.Invoke();
            message = item.Definition.Slot == EquipmentSlot.Accessory ? item.Definition.Name + " equipado em " + member.Name + "."
                : item.Definition.Name + " equipado. Materias dos equipamentos trocados voltaram ao inventario.";
            return true;
        }

        public bool TryRemoveEquipment(PartyMember member, EquipmentSlot slot, out string message)
        {
            if (member == null || !members.Contains(member) || !Enum.IsDefined(typeof(EquipmentSlot), slot))
            { message = "Personagem ou tipo de equipamento invalido."; return false; }
            EquipmentLoadout item = member.EquipmentIn(slot);
            if (item == null) { message = "Nao ha equipamento neste espaco."; return false; }
            item.ClearMaterias();
            member.SetEquipment(slot, null);
            Changed?.Invoke();
            message = slot == EquipmentSlot.Accessory ? "Acessorio devolvido ao inventario." : "Equipamento e suas materias voltaram ao inventario.";
            return true;
        }

        public string LocationOf(MateriaInstance materia)
        {
            foreach (PartyMember member in members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor, member.Accessory })
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
