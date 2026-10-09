using System;
using System.Collections.Generic;

namespace DragonQuest.Equipment
{
    public sealed class EquipmentDefinition
    {
        public string Name { get; }
        public int AttackBonus { get; }
        public int DefenseBonus { get; }
        public int SocketCount { get; }

        public EquipmentDefinition(string name, int attackBonus, int defenseBonus, int socketCount)
        {
            if (attackBonus < 0 || defenseBonus < 0 || socketCount < 0 || socketCount > 6 || socketCount % 2 != 0)
                throw new ArgumentException("Equipamento exige bonus positivos e pares completos de encaixes, ate seis.");
            Name = name; AttackBonus = attackBonus; DefenseBonus = defenseBonus; SocketCount = socketCount;
        }
    }

    public sealed class EquipmentLoadout
    {
        private readonly MateriaInstance[] sockets;
        public EquipmentDefinition Definition { get; }
        public IReadOnlyList<MateriaInstance> Sockets { get; }

        public EquipmentLoadout(EquipmentDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            sockets = new MateriaInstance[definition.SocketCount];
            Sockets = Array.AsReadOnly(sockets);
        }

        // Pares (0,1), (2,3), (4,5). Nao liga equipamentos diferentes.
        public int LinkedSocket(int slot)
        {
            if (!IsValidSocket(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
            return slot ^ 1;
        }
        public bool IsValidSocket(int slot) => slot >= 0 && slot < sockets.Length;
        internal void Set(int slot, MateriaInstance materia) => sockets[slot] = materia;
    }
}
