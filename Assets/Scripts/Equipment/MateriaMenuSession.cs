using DragonQuest.Progression;
using System;
using System.Collections.Generic;

namespace DragonQuest.Equipment
{
    public sealed class MateriaMenuSocket
    {
        public PartyMember Member { get; }
        public EquipmentLoadout Equipment { get; }
        public int Index { get; }
        public MateriaMenuSocket(PartyMember member, EquipmentLoadout equipment, int index)
        { Member = member; Equipment = equipment; Index = index; }
    }

    public sealed class MateriaMenuSession
    {
        private readonly List<MateriaInstance> materias = new List<MateriaInstance>();
        private readonly List<MateriaMenuSocket> sockets = new List<MateriaMenuSocket>();
        private bool arranged;
        public IReadOnlyList<MateriaInstance> Materias { get; }
        public IReadOnlyList<MateriaMenuSocket> Sockets { get; }
        public int MateriaIndex { get; private set; }
        public int SocketIndex { get; private set; }
        public bool ChoosingSocket { get; private set; }
        public bool Removing { get; private set; }
        public int SubmenuIndex { get; private set; }
        public bool BrowsingSubmenus { get; private set; }
        public string Message { get; private set; } = "";
        public MateriaInstance SelectedMateria => materias.Count == 0 ? null : materias[MateriaIndex];
        public MateriaMenuSocket SelectedSocket => sockets.Count == 0 ? null : sockets[SocketIndex];

        public MateriaMenuSession() { Materias = materias.AsReadOnly(); Sockets = sockets.AsReadOnly(); }
        public void Reset(PartyProgress party)
        {
            materias.Clear(); sockets.Clear(); MateriaIndex = SocketIndex = 0; arranged = false;
            SubmenuIndex = 0;
            CancelSocket(); Refresh(party);
        }
        public void Refresh(PartyProgress party)
        {
            var materia = SelectedMateria; var socket = SelectedSocket;
            materias.Clear(); foreach (var item in party.Materias) materias.Add(item);
            if (arranged) materias.Sort((a, b) =>
            {
                // MateriaType groups colors in green, blue, red, purple order.
                int color = a.Definition.Type.CompareTo(b.Definition.Type);
                return color != 0 ? color : StringComparer.OrdinalIgnoreCase.Compare(a.Definition.Name, b.Definition.Name);
            });
            int index = materias.IndexOf(materia);
            bool lostMateria = materia != null && index < 0;
            MateriaIndex = index >= 0 ? index : Math.Max(0, Math.Min(MateriaIndex, materias.Count - 1));
            sockets.Clear();
            foreach (var member in party.Members)
                foreach (var gear in new[] { member.Weapon, member.Armor })
                    if (gear != null)
                        for (int i = 0; i < gear.Sockets.Count; i++) sockets.Add(new MateriaMenuSocket(member, gear, i));
            index = socket == null ? -1 : sockets.FindIndex(s => s.Equipment == socket.Equipment && s.Index == socket.Index);
            bool lostSocket = socket != null && index < 0;
            SocketIndex = index >= 0 ? index : Math.Max(0, Math.Min(SocketIndex, sockets.Count - 1));
            if (sockets.Count == 0 || materias.Count == 0 && !Removing || ChoosingSocket && (lostSocket || lostMateria && !Removing)) CancelSocket();
        }
        public void SelectMateria(int index)
        {
            if (index < 0 || index >= materias.Count) return;
            MateriaIndex = index; CancelSocket();
        }
        public bool BeginEquip(PartyProgress party)
        {
            SubmenuIndex = 0; BrowsingSubmenus = false;
            Refresh(party); Removing = false;
            if (SelectedMateria == null || sockets.Count == 0)
            { Message = sockets.Count == 0 ? "Nenhum encaixe disponivel." : "Nenhuma materia no inventario."; return false; }
            ChoosingSocket = true; Message = ""; return true;
        }
        public bool BeginRemove(PartyProgress party)
        {
            SubmenuIndex = 1; BrowsingSubmenus = false;
            Refresh(party);
            int index = sockets.FindIndex(s => s.Equipment.Sockets[s.Index] != null);
            if (index < 0) { CancelSocket(); Message = "Nenhuma materia equipada."; return false; }
            Removing = ChoosingSocket = true; SocketIndex = index; Message = ""; return true;
        }
        public void SelectSocket(int index)
        {
            if (!ChoosingSocket || index < 0 || index >= sockets.Count) return;
            SocketIndex = index; BrowsingSubmenus = false; Message = "";
        }
        public void Move(int step)
        {
            int count = ChoosingSocket ? sockets.Count : materias.Count;
            if (count == 0) return;
            int index = ((ChoosingSocket ? SocketIndex : MateriaIndex) + step % count + count) % count;
            if (ChoosingSocket) SelectSocket(index); else SelectMateria(index);
        }
        public void MoveVertical(int step)
        {
            if (!ChoosingSocket || sockets.Count == 0) { Move(step); return; }
            var rows = new List<EquipmentLoadout>();
            foreach (var socket in sockets) if (!rows.Contains(socket.Equipment)) rows.Add(socket.Equipment);
            var current = SelectedSocket;
            int row = (rows.IndexOf(current.Equipment) + step % rows.Count + rows.Count) % rows.Count;
            var next = rows[row]; int column = Math.Min(current.Index, next.Sockets.Count - 1);
            SelectSocket(sockets.FindIndex(s => s.Equipment == next && s.Index == column));
        }
        public bool ConfirmSocket(PartyProgress party)
        {
            if (!ChoosingSocket || SelectedSocket == null) return false;
            var target = SelectedSocket; string result;
            bool changed = Removing
                ? party.TryUnequip(target.Member, target.Equipment, target.Index, out result)
                : party.TryEquip(target.Member, target.Equipment, target.Index, SelectedMateria, out result);
            if (changed && !Removing) ChoosingSocket = false;
            Refresh(party);
            if (Removing && !sockets.Exists(s => s.Equipment.Sockets[s.Index] != null)) { ChoosingSocket = false; Removing = false; }
            Message = result; return changed;
        }
        public void CancelSocket() { ChoosingSocket = Removing = BrowsingSubmenus = false; Message = ""; }
        public void ShowEquip() { CancelSocket(); SubmenuIndex = 0; }
        public void Arrange(PartyProgress party) { arranged = true; CancelSocket(); SubmenuIndex = 2; Refresh(party); }
        public void MoveSubmenu(int step, PartyProgress party)
        {
            int next = (SubmenuIndex + step % 3 + 3) % 3;
            if (next == 1) BeginRemove(party);
            else if (next == 2) Arrange(party);
            else ShowEquip();
            BrowsingSubmenus = true;
        }
        public void FocusContent() { BrowsingSubmenus = false; }
    }
}
