using System;
using DragonQuest.Persistence;

namespace DragonQuest.Gameplay.Saving
{
    public sealed class SaveSlotSession
    {
        private readonly SaveStore store;
        public RestoredJourney[] Previews { get; } = new RestoredJourney[SaveStore.SlotCount];
        public string[] Messages { get; } = new string[SaveStore.SlotCount];
        public bool[] Occupied { get; } = new bool[SaveStore.SlotCount];
        public SaveSlotSession(SaveStore store) { this.store = store; }
        public void Refresh()
        {
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++)
            {
                Occupied[slot - 1] = store.Exists(slot);
                store.TryRead(slot, out Previews[slot - 1], out Messages[slot - 1]);
            }
        }
        public int FirstEmptySlot()
        {
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++) if (!store.Exists(slot)) return slot;
            return 0;
        }
        public bool Exists(int slot) => store.Exists(slot);
        public bool TryRead(int slot, out RestoredJourney journey, out string warning) => store.TryRead(slot, out journey, out warning);
        public bool TryWrite(int slot, Func<JourneyData> capture, out string error)
        {
            try { return store.TryWrite(slot, capture(), out error); }
            catch (Exception ex) { error = "Nao foi possivel salvar: " + ex.Message; return false; }
        }
    }
}
