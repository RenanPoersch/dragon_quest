using DragonQuest.Progression;
using System;
using System.Collections.Generic;

namespace DragonQuest.Equipment
{
    public enum EquipmentMenuCategory { Weapons, Armor, Accessories }

    public sealed class EquipmentMenuStats
    {
        public int Attack { get; }
        public int Defense { get; }
        public int Magic { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int Speed { get; }
        public int Sockets { get; }
        public const int Count = 7;
        public int this[int index] => index == 0 ? Attack : index == 1 ? Defense : index == 2 ? Magic
            : index == 3 ? MaxHp : index == 4 ? MaxMp : index == 5 ? Speed : index == 6 ? Sockets
            : throw new ArgumentOutOfRangeException(nameof(index));

        public EquipmentMenuStats(PartyMember member, EquipmentLoadout weapon, EquipmentLoadout armor, EquipmentLoadout accessory,
            bool preserveEquippedMaterias = false)
        {
            if (member == null) return;
            Attack = member.AttackWith(weapon, armor, accessory, preserveEquippedMaterias);
            Defense = member.DefenseWith(weapon, armor, accessory, preserveEquippedMaterias);
            Magic = member.MagicWith(weapon, armor, accessory);
            MaxHp = preserveEquippedMaterias ? member.MaxHp : member.MaxHpWith(weapon, armor);
            MaxMp = preserveEquippedMaterias ? member.MaxMp : member.MaxMpWith(weapon, armor); Speed = member.Speed;
            Sockets = (weapon?.Sockets.Count ?? 0) + (armor?.Sockets.Count ?? 0);
        }
    }

    public sealed class EquipmentMenuSession
    {
        private readonly List<EquipmentLoadout> items = new List<EquipmentLoadout>();
        private EquipmentLoadout previewItem, previewEquipment;
        public IReadOnlyList<EquipmentLoadout> Items { get; }
        public int MemberIndex { get; private set; }
        public PartyMember Member { get; private set; }
        public EquipmentMenuCategory Category { get; private set; }
        public int ItemIndex { get; private set; }
        public bool HasSelection => items.Count > 0;
        public EquipmentLoadout SelectedItem => HasSelection ? items[ItemIndex] : null;
        public EquipmentSlot Slot => Category == EquipmentMenuCategory.Weapons ? EquipmentSlot.Weapon
            : Category == EquipmentMenuCategory.Armor ? EquipmentSlot.Armor : EquipmentSlot.Accessory;
        public EquipmentLoadout CurrentItem => Member?.EquipmentIn(Slot);
        public bool CanApply => Member != null && HasSelection && SelectedItem != CurrentItem;
        public string Message { get; private set; } = "";

        public EquipmentMenuSession() { Items = items.AsReadOnly(); }
        public void Reset(PartyProgress party, int member = 0)
        {
            items.Clear(); MemberIndex = Math.Max(0, Math.Min(member, party.Members.Count - 1));
            Category = EquipmentMenuCategory.Weapons; ItemIndex = 0; Message = "";
            Refresh(party); SelectCurrent();
        }
        public void Refresh(PartyProgress party)
        {
            var selected = SelectedItem; bool hadSelection = HasSelection;
            MemberIndex = Math.Max(0, Math.Min(MemberIndex, party.Members.Count - 1));
            Member = party.Members.Count == 0 ? null : party.Members[MemberIndex];
            items.Clear();
            if (Member != null)
            {
                items.Add(null); // The empty slot is a real removal preview.
                foreach (var item in party.Equipments) if (item.Definition.Slot == Slot) items.Add(item);
            }
            int index = items.IndexOf(selected);
            ItemIndex = hadSelection && index >= 0 ? index : Math.Max(0, items.IndexOf(CurrentItem));
        }
        private void SelectCurrent() { ItemIndex = Math.Max(0, items.IndexOf(CurrentItem)); }
        public void SelectItem(int index)
        { if (index >= 0 && index < items.Count) { ItemIndex = index; Message = ""; } }
        public void Move(int step)
        { if (items.Count > 0) SelectItem((ItemIndex + step % items.Count + items.Count) % items.Count); }
        public void SelectCategory(EquipmentMenuCategory category, PartyProgress party)
        {
            if (!Enum.IsDefined(typeof(EquipmentMenuCategory), category) || Category == category) return;
            Category = category; items.Clear(); Message = ""; Refresh(party); SelectCurrent();
        }
        public void ChangeCategory(int step, PartyProgress party)
            => SelectCategory((EquipmentMenuCategory)(((int)Category + step % 3 + 3) % 3), party);
        public void ChangeMember(int step, PartyProgress party)
        {
            if (party.Members.Count == 0) return;
            MemberIndex = (MemberIndex + step % party.Members.Count + party.Members.Count) % party.Members.Count;
            items.Clear(); Message = ""; Refresh(party); SelectCurrent();
        }
        public EquipmentMenuStats CurrentStats => new EquipmentMenuStats(Member, Member?.Weapon, Member?.Armor, Member?.Accessory);
        public EquipmentMenuStats PreviewStats
        {
            get
            {
                EquipmentLoadout weapon = Member?.Weapon, armor = Member?.Armor, accessory = Member?.Accessory;
                if (CanApply)
                {
                    var item = SelectedItem;
                    if (previewItem != item)
                    {
                        previewItem = item;
                        // Equipping clears the incoming equipment's materias. Preview an independent empty loadout.
                        previewEquipment = item == null ? null : new EquipmentLoadout(item.Definition, "equipment-menu-preview");
                    }
                    if (Category == EquipmentMenuCategory.Weapons) weapon = item == null ? null : previewEquipment;
                    else if (Category == EquipmentMenuCategory.Armor) armor = item == null ? null : previewEquipment;
                    else accessory = item == null ? null : previewEquipment;
                }
                return new EquipmentMenuStats(Member, weapon, armor, accessory);
            }
        }
        public bool Apply(PartyProgress party)
        {
            Refresh(party);
            if (!CanApply) return false;
            string result;
            bool changed = SelectedItem == null
                ? party.TryRemoveEquipment(Member, Slot, out result)
                : party.TryEquipEquipment(Member, SelectedItem, out result);
            Message = result; Refresh(party); return changed;
        }
    }
}
