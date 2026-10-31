using DragonQuest.Progression;
using DragonQuest.Content;
using DragonQuest.Authoring;
using System;
using System.Text;
using DragonQuest.Equipment;

internal static class EquipmentMenuChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static EquipmentLoadout Find(PartyProgress party, string id)
    { foreach (var item in party.Equipments) if (item.Id == id) return item; throw new Exception("Missing gear: " + id); }
    private static void Select(EquipmentMenuSession menu, EquipmentLoadout item)
    { for (int i = 0; i < menu.Items.Count; i++) if (menu.Items[i] == item) { menu.SelectItem(i); return; } throw new Exception("Gear not in category."); }
    private static string Snapshot(PartyProgress party)
    {
        var text = new StringBuilder();
        foreach (var member in party.Members) text.Append(member.Id).Append(member.Weapon?.Id).Append(member.Armor?.Id).Append(member.Accessory?.Id)
            .Append(member.Hp).Append('/').Append(member.Mp).Append(':').Append(member.Experience).Append(':').Append(member.Limit.Charge);
        foreach (var item in party.Equipments)
        { text.Append('|').Append(item.Id); foreach (var materia in item.Sockets) text.Append(',').Append(materia?.Id ?? "--"); }
        foreach (var materia in party.Materias) text.Append('|').Append(materia.Id);
        return text.ToString();
    }
    private static void SameStats(EquipmentMenuStats expected, EquipmentMenuStats actual, string context)
    { for (int i = 0; i < EquipmentMenuStats.Count; i++) Check(expected[i] == actual[i], context + " stat " + i); }
    private static void AccurateSwapPreview()
    {
        var party = PartyFixture.Create(); var menu = new EquipmentMenuSession();
        var lia = party.Members[1]; var bram = party.Members[2]; var staff = Find(party, "runic-staff"); var old = lia.Weapon;
        party.TryEquip(lia, old, 0, party.Materias[19], out _); party.TryEquip(lia, old, 1, party.Materias[20], out _);
        party.TryEquipEquipment(bram, staff, out _); party.TryEquip(bram, staff, 0, party.Materias[0], out _);
        menu.Reset(party, 1);
        Check(menu.SelectedItem == old && !menu.CanApply, "Opening selects the current equipment.");
        SameStats(menu.CurrentStats, menu.PreviewStats, "Current equipment keeps all materia bonuses");
        string before = Snapshot(party); Check(!menu.Apply(party) && Snapshot(party) == before, "Re-equipping current gear does not clear sockets.");
        Select(menu, staff); var preview = menu.PreviewStats; var current = menu.CurrentStats;
        Check(preview.Attack < current.Attack && preview.Magic > current.Magic, "The same candidate can show red attack and green magic deltas.");
        Check(current.MaxHp == 96 && preview.MaxHp == 80 && current.MaxMp == 72 && preview.MaxMp == 60,
            "Preview accounts for HP/MP materia bonuses removed from the old equipment.");
        Check(preview.Sockets == current.Sockets + 2, "Socket gain is compared numerically.");
        menu.PreviewStats.ToString(); menu.Refresh(party); menu.Move(1); menu.Move(-1);
        Check(Snapshot(party) == before, "Preview, navigation and refresh do not mutate any party equipment, materias or vitals.");
        int changes = 0; party.Changed += () => changes++;
        Check(menu.Apply(party) && changes == 1, "Confirmation makes exactly one domain change.");
        Check(lia.Weapon == staff && bram.Weapon == old && party.Equipments.Count == 10 && party.Materias.Count == 21,
            "Cross-character exchange preserves equipment ownership and inventory counts.");
        SameStats(preview, menu.CurrentStats, "Confirmed swap matches preview");
        Check(staff.Sockets[0] == null && old.Sockets[0] == null && old.Sockets[1] == null,
            "Both exchanged loadouts return their materias to inventory.");
        Check(!menu.CanApply && !menu.Apply(party) && changes == 1, "Repeated confirmation cannot equip twice.");
    }
    private static void RemovalAndRounding()
    {
        var party = PartyFixture.Create(); var menu = new EquipmentMenuSession(); var aren = party.Members[0];
        party.TryEquip(aren, aren.Armor, 0, party.Materias[19], out _);
        party.TryEquip(aren, aren.Armor, 1, party.Materias[20], out _);
        menu.Reset(party); Select(menu, Find(party, "iron-sword"));
        Check(menu.PreviewStats.MaxHp == menu.CurrentStats.MaxHp && menu.PreviewStats.MaxMp == menu.CurrentStats.MaxMp,
            "Materias on the unchanged armor retain their HP/MP bonuses.");
        menu.SelectCategory(EquipmentMenuCategory.Armor, party); Select(menu, null);
        var preview = menu.PreviewStats; string before = Snapshot(party); var weapon = aren.Weapon;
        Check(preview.Defense < menu.CurrentStats.Defense && preview.MaxHp < menu.CurrentStats.MaxHp, "Removal previews stat losses.");
        Check(Snapshot(party) == before && menu.Apply(party) && aren.Armor == null && aren.Weapon == weapon,
            "Removing armor keeps the weapon and changes only on confirmation.");
        SameStats(preview, menu.CurrentStats, "Removal matches preview");
        Check(!menu.CanApply && !menu.Apply(party), "Removing an empty slot is disabled.");
        party = PartyFixture.Create(); aren = party.Members[0];
        aren.Progression.AddExperience(100); party.TryEquip(aren, aren.Weapon, 0, party.Materias[19], out _);
        menu.Reset(party); Select(menu, Find(party, "iron-sword"));
        Check(menu.PreviewStats.MaxHp == aren.MaxHpWith(null, aren.Armor), "Level growth uses exact base stats rather than dividing rounded maxima.");
        preview = menu.PreviewStats; menu.Apply(party); SameStats(preview, menu.CurrentStats, "Level growth preview");
    }
    private static void CategoriesAndRefresh()
    {
        var party = PartySetup.Create(); var menu = new EquipmentMenuSession(); menu.Reset(party);
        foreach (var item in menu.Items) Check(item == null || item.Definition.Slot == EquipmentSlot.Weapon, "Weapon category contains only weapons.");
        string before = Snapshot(party); menu.ChangeMember(-1, party);
        Check(menu.Member == party.Members[2], "Character navigation wraps backwards.");
        menu.ChangeMember(1, party); Check(menu.Member == party.Members[0], "Character navigation wraps forwards.");
        menu.ChangeCategory(1, party);
        foreach (var item in menu.Items) Check(item == null || item.Definition.Slot == EquipmentSlot.Armor, "Armor category contains only armor.");
        menu.ChangeCategory(1, party);
        Check(menu.Category == EquipmentMenuCategory.Accessories && menu.Items.Count == 1 && menu.SelectedItem == null && !menu.CanApply && !menu.Apply(party),
            "The empty accessory slot cannot be removed twice.");
        SameStats(menu.CurrentStats, menu.PreviewStats, "Empty accessory category");
        menu.Move(1); menu.ChangeCategory(1, party); Check(menu.Category == EquipmentMenuCategory.Weapons, "Category navigation wraps.");
        menu.SelectItem(-1); menu.SelectItem(999); Check(Snapshot(party) == before, "Invalid selection and category browsing preserve state.");
        var extra = new EquipmentLoadout(EquipmentCatalog.IronSword, "temporary-candidate"); party.TryAddEquipments(extra);
        menu.Refresh(party); Select(menu, extra); Check(party.TryRemoveOwnedEquipment(extra), "Pending item can leave inventory externally.");
        menu.Refresh(party); Check(!menu.CanApply && !menu.Apply(party), "Missing pending item falls back to current gear and cannot equip another item.");
        menu.Reset(party); menu.SelectItem(0); menu.Move(-1); Check(menu.ItemIndex == menu.Items.Count - 1, "List navigation wraps safely.");
    }
    public static int Main()
    {
        try { AccurateSwapPreview(); RemovalAndRounding(); CategoriesAndRefresh(); Console.WriteLine("EQUIPMENT_MENU_CHECKS_OK: " + checks + " checks."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
