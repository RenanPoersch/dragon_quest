using DragonQuest.Progression;
using DragonQuest.Content;
using DragonQuest.Authoring;
using System;
using System.Collections.Generic;
using DragonQuest.Equipment;

internal static class MateriaMenuChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static int Find(MateriaMenuSession menu, EquipmentLoadout gear, int index)
    { for (int i = 0; i < menu.Sockets.Count; i++) if (menu.Sockets[i].Equipment == gear && menu.Sockets[i].Index == index) return i; throw new Exception("Socket missing."); }
    private static int Find(MateriaMenuSession menu, MateriaInstance materia)
    { for (int i = 0; i < menu.Materias.Count; i++) if (menu.Materias[i] == materia) return i; throw new Exception("Materia missing."); }
    private static int Count(PartyProgress party, MateriaInstance materia)
    {
        int count = 0; foreach (var member in party.Members) foreach (var gear in new[] { member.Weapon, member.Armor })
            if (gear != null) foreach (var item in gear.Sockets) if (item == materia) count++;
        return count;
    }
    private static void SelectionAndExchange()
    {
        var party = PartySetup.Create(); var menu = new MateriaMenuSession(); menu.Reset(party);
        Check(menu.Sockets.Count == 18 && menu.Materias.Count == 5, "The menu exposes both equipment rows for all three allies.");
        var aren = party.Members[0]; var lia = party.Members[1];
        var source = aren.Weapon.Sockets[0]; var previous = lia.Weapon.Sockets[0];
        menu.SelectMateria(Find(menu, source));
        Check(menu.BeginEquip(party) && menu.ChoosingSocket, "Selecting a materia moves the cursor to sockets.");
        menu.SelectSocket(Find(menu, lia.Weapon, 0)); menu.CancelSocket();
        Check(aren.Weapon.Sockets[0] == source && lia.Weapon.Sockets[0] == previous && !menu.ChoosingSocket,
            "Cancelling leaves both characters' materias unchanged.");
        Check(!menu.ConfirmSocket(party), "Confirmation outside socket selection cannot equip anything.");
        menu.BeginEquip(party); menu.SelectSocket(Find(menu, lia.Weapon, 0));
        Check(menu.ConfirmSocket(party) && !menu.ChoosingSocket, "Confirming a socket equips once and returns to the list.");
        Check(lia.Weapon.Sockets[0] == source && aren.Weapon.Sockets[0] == previous, "Cross-character exchange remains atomic.");
        Check(Count(party, source) == 1 && Count(party, previous) == 1 && party.Materias.Count == 5,
            "Exchanging sockets preserves unique ownership and inventory count.");
        menu.BeginEquip(party); menu.SelectSocket(Find(menu, lia.Weapon, 0));
        Check(!menu.ConfirmSocket(party) && Count(party, source) == 1 && menu.Message.Length > 0,
            "Equipping the same materia in its current socket is rejected without duplication.");
    }
    private static void NavigationAndSixSockets()
    {
        var party = PartyFixture.Create(); var menu = new MateriaMenuSession(); menu.Reset(party);
        var lia = party.Members[1]; EquipmentLoadout robe = null;
        foreach (var gear in party.Equipments) if (gear.Definition == EquipmentCatalog.ArcaneRobe) robe = gear;
        Check(party.TryEquipEquipment(lia, robe, out _), "Six-socket fixture equips the robe.");
        menu.Refresh(party); Check(menu.Sockets.Count == 20, "The target list reflects the new socket count.");
        menu.BeginEquip(party); menu.SelectSocket(Find(menu, robe, 5));
        menu.MoveVertical(-1);
        Check(menu.SelectedSocket.Equipment == lia.Weapon && menu.SelectedSocket.Index == 1,
            "Vertical navigation preserves the column where possible and clamps shorter rows.");
        menu.SelectSocket(0); menu.Move(-1);
        Check(menu.SocketIndex == menu.Sockets.Count - 1, "Horizontal socket navigation wraps backwards.");
        menu.Move(1); Check(menu.SocketIndex == 0, "Horizontal socket navigation wraps forwards.");
        menu.SelectSocket(Find(menu, robe, 5)); var materia = menu.SelectedMateria;
        Check(menu.ConfirmSocket(party) && robe.Sockets[5] == materia, "The sixth socket is a valid destination.");
        Check(robe.LinkedSocket(5) == 4 && Count(party, materia) == 1, "The last linked pair and unique identity remain intact.");
        menu.Arrange(party);
        Check(menu.SelectedMateria == materia && !menu.ChoosingSocket, "Arrange preserves the selected instance.");
        var expected = new[] { "chaos", "cure", "fire", "ice", "cleanse", "thunder", "thunder", "earth", "revive",
            "all", "all", "bahamut", "phoenix", "titan", "cleave", "defense", "heavy", "guardian", "hp", "infuse", "mp" };
        Check(menu.Materias.Count == expected.Length, "Arrange keeps every materia, including duplicate definitions.");
        for (int i = 0; i < expected.Length; i++)
            Check(menu.Materias[i].Definition.Id == expected[i], "Arrange groups green, blue, red and purple, alphabetically within each color.");
        var originalFire = menu.Materias[2];
        var addition = new MateriaInstance("new-fire", MateriaCatalog.FireMateria);
        Check(party.TryAddMaterias(addition), "A new materia can enter the arranged inventory.");
        menu.Refresh(party);
        Check(menu.SelectedMateria == materia && menu.Materias.Count == expected.Length + 1
            && Math.Abs(Find(menu, addition) - Find(menu, originalFire)) == 1,
            "Refresh keeps the selected instance and inserts new materia beside its matching color and name.");
    }
    private static void RemovalAndRefresh()
    {
        var party = PartySetup.Create(); var menu = new MateriaMenuSession(); menu.Reset(party);
        Check(menu.BeginRemove(party) && menu.Removing && menu.ChoosingSocket, "Remove selects an occupied socket.");
        var target = menu.SelectedSocket; var materia = target.Equipment.Sockets[target.Index];
        Check(menu.ConfirmSocket(party) && target.Equipment.Sockets[target.Index] == null && party.Materias.Count == 5,
            "Removing returns the same materia to inventory without changing ownership.");
        Check(Count(party, materia) == 0 && menu.ChoosingSocket, "Removal stays active while other occupied sockets exist.");
        Check(!menu.ConfirmSocket(party) && menu.Message.Length > 0, "Removing an empty socket is a safe rejected action.");
        menu.CancelSocket(); menu.SelectMateria(Find(menu, materia)); menu.BeginEquip(party);
        Check(party.TryRemoveOwnedMateria(materia), "The pending unassigned materia can leave the inventory externally.");
        menu.Refresh(party); Check(!menu.ChoosingSocket, "Refresh cancels a missing materia rather than equipping another one.");
        menu.BeginEquip(party); target = menu.SelectedSocket;
        Check(party.TryRemoveEquipment(target.Member, target.Equipment.Definition.Slot, out _), "Pending target can be removed externally.");
        menu.Refresh(party); Check(!menu.ChoosingSocket, "Refresh cancels a missing destination.");
        foreach (var member in party.Members) foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) party.TryRemoveEquipment(member, slot, out _);
        menu.Refresh(party); Check(menu.Sockets.Count == 0 && !menu.BeginEquip(party) && !menu.BeginRemove(party), "Characters without equipment expose no fake sockets.");
        party = new PartyProgress(new[] { party.Members[0] }, new MateriaInstance[0]); menu.Reset(party); menu.Move(1); menu.MoveVertical(1);
        Check(menu.SelectedMateria == null && menu.SelectedSocket == null && !menu.ConfirmSocket(party), "An empty party inventory is safe to browse.");
    }
    private static void Submenus()
    {
        var party = PartyFixture.Create(); var menu = new MateriaMenuSession(); menu.Reset(party);
        var selected = menu.SelectedMateria; var equipped = party.Members[0].Weapon.Sockets[0];
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 1 && menu.Removing && menu.ChoosingSocket && menu.BrowsingSubmenus,
            "Right changes Equip to Remove and selects an occupied socket without removing it.");
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 2 && !menu.Removing && !menu.ChoosingSocket && menu.SelectedMateria == selected,
            "Another Right changes Remove to Arrange instead of moving a socket.");
        Check(party.Members[0].Weapon.Sockets[0] == equipped, "Browsing submenus cannot mutate equipment.");
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 0 && !menu.Removing, "Right wraps Arrange to Equip.");
        menu.MoveSubmenu(-1, party);
        Check(menu.SubmenuIndex == 2, "Left wraps Equip to Arrange.");
        menu.MoveSubmenu(-1, party); menu.FocusContent(); int socket = menu.SocketIndex;
        menu.Move(1);
        Check(menu.SubmenuIndex == 1 && !menu.BrowsingSubmenus && menu.SocketIndex != socket,
            "After entering Remove content, horizontal navigation selects sockets within that context.");
        menu.ShowEquip();
        Check(menu.SubmenuIndex == 0 && !menu.Removing && !menu.ChoosingSocket, "Equip cancels removal and restores its context.");
        menu.Reset(party);
        Check(menu.SubmenuIndex == 0 && !menu.BrowsingSubmenus, "Reopening resets the submenu.");
    }
    public static int Main()
    {
        try { SelectionAndExchange(); NavigationAndSixSockets(); RemovalAndRefresh(); Submenus(); Console.WriteLine("MATERIA_MENU_CHECKS_OK: " + checks + " checks."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
