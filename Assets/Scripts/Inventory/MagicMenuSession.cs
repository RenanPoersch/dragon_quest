using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Inventory
{
    public sealed class MagicMenuSession
    {
        private readonly List<AbilityDefinition> spells = new List<AbilityDefinition>();
        public IReadOnlyList<AbilityDefinition> Spells { get; }
        public int CasterIndex { get; private set; }
        public int SpellIndex { get; private set; }
        public int TargetIndex { get; private set; }
        public bool ChoosingCaster { get; private set; } = true;
        public bool ChoosingTarget { get; private set; }
        public string Message { get; private set; } = "";
        public AbilityDefinition SelectedSpell => spells.Count == 0 ? null : spells[SpellIndex];
        public MagicMenuSession() { Spells = spells.AsReadOnly(); }
        public PartyMember Caster(PartyProgress party) => party.Members.Count == 0 ? null : party.Members[CasterIndex];

        public void Reset(PartyProgress party)
        {
            spells.Clear(); CasterIndex = SpellIndex = TargetIndex = 0;
            ChoosingCaster = true; ChoosingTarget = false; Message = "";
            Refresh(party);
        }

        public void Refresh(PartyProgress party)
        {
            string selected = SelectedSpell?.Id;
            CasterIndex = Math.Max(0, Math.Min(CasterIndex, party.Members.Count - 1));
            TargetIndex = Math.Max(0, Math.Min(TargetIndex, party.Members.Count - 1));
            spells.Clear();
            var caster = Caster(party);
            if (caster != null)
                foreach (var ability in MateriaResolver.Resolve(caster))
                    if (PartyProgress.IsFieldMagic(ability)) spells.Add(ability);
            int index = spells.FindIndex(ability => ability.Id == selected);
            SpellIndex = index >= 0 ? index : Math.Max(0, Math.Min(SpellIndex, spells.Count - 1));
            if (ChoosingTarget && (index < 0 || !CanUse(party, SelectedSpell))) ChoosingTarget = false;
        }

        public bool CanUse(PartyProgress party, AbilityDefinition spell)
            => spell != null && party.CanCastMagic(Caster(party), spell.Id, out _, out _);

        public void SelectCaster(int index, PartyProgress party)
        {
            if (index < 0 || index >= party.Members.Count) return;
            if (CasterIndex != index) { CasterIndex = index; SpellIndex = 0; spells.Clear(); }
            ChoosingCaster = true; ChoosingTarget = false; Message = ""; Refresh(party);
        }

        public void ConfirmCaster() { ChoosingCaster = false; Message = ""; }
        public void SelectSpell(int index)
        {
            if (ChoosingCaster || index < 0 || index >= spells.Count) return;
            SpellIndex = index; ChoosingTarget = false; Message = "";
        }
        public bool BeginUse(PartyProgress party)
        {
            if (ChoosingCaster || SelectedSpell == null) return false;
            if (!party.CanCastMagic(Caster(party), SelectedSpell.Id, out _, out string error))
            { Message = error; return false; }
            ChoosingTarget = true; Message = ""; return true;
        }
        public void SelectTarget(int index, PartyProgress party)
        {
            if (!ChoosingTarget || index < 0 || index >= party.Members.Count) return;
            TargetIndex = index; Message = "";
        }
        public void Move(int step, PartyProgress party)
        {
            int count = ChoosingCaster || ChoosingTarget ? party.Members.Count : spells.Count;
            if (count == 0) return;
            int current = ChoosingCaster ? CasterIndex : ChoosingTarget ? TargetIndex : SpellIndex;
            int index = (current + step % count + count) % count;
            if (ChoosingCaster) SelectCaster(index, party);
            else if (ChoosingTarget) SelectTarget(index, party);
            else SelectSpell(index);
        }
        public bool ConfirmTarget(PartyProgress party)
        {
            if (!ChoosingTarget || SelectedSpell == null || party.Members.Count == 0) return false;
            bool used = party.TryCastMagic(Caster(party), SelectedSpell.Id, party.Members[TargetIndex], out string result);
            Message = result; Refresh(party); return used;
        }
        public bool Cancel()
        {
            Message = "";
            if (ChoosingTarget) { ChoosingTarget = false; return true; }
            if (!ChoosingCaster) { ChoosingCaster = true; return true; }
            return false;
        }
    }
}
