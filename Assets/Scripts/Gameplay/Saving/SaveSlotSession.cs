using System;
using System.Collections.Generic;
using DragonQuest.Persistence;

namespace DragonQuest.Gameplay.Saving
{
    public sealed class SaveSlotState
    {
        public int Number { get; }
        public bool Occupied { get; }
        public RestoredJourney Preview { get; }
        public string Message { get; }
        public SaveSlotState(int number, bool occupied, RestoredJourney preview, string message)
        { Number = number; Occupied = occupied; Preview = preview; Message = message; }
    }

    public sealed class SaveSlotSession
    {
        private readonly SaveStore store;
        public IReadOnlyList<SaveSlotState> Slots { get; private set; }
        public bool HasLoadableSlot
        {
            get { foreach (var slot in Slots) if (slot.Preview != null) return true; return false; }
        }
        public SaveSlotSession(SaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            var slots = new SaveSlotState[SaveStore.SlotCount];
            for (int i = 0; i < slots.Length; i++) slots[i] = new SaveSlotState(i + 1, false, null, null);
            Slots = Array.AsReadOnly(slots);
        }
        public void Refresh()
        {
            var slots = new SaveSlotState[SaveStore.SlotCount];
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++)
            {
                bool occupied = store.Exists(slot);
                store.TryRead(slot, out var preview, out string message);
                slots[slot - 1] = new SaveSlotState(slot, occupied, preview, message);
            }
            Slots = Array.AsReadOnly(slots);
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
