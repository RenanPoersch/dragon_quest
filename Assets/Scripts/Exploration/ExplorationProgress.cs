using System;
using System.Collections.Generic;

namespace DragonQuest.Exploration
{
    // Estado da sessão separado dos objetos do cenário; persiste ao entrar/sair da loja.
    public sealed class ExplorationProgress
    {
        private readonly HashSet<string> openedChests = new HashSet<string>();

        private int gold;
        public bool HasInfiniteGold { get; private set; }
        public int Gold => HasInfiniteGold ? int.MaxValue : gold;
        internal int SavedGold => gold;
        public void SetInfiniteGold(bool enabled)
        {
            if (HasInfiniteGold == enabled) return;
            HasInfiniteGold = enabled;
            Changed?.Invoke();
        }
        public event Action Changed;
        public bool TravelerPhoenixReceived { get; private set; }
        internal void RestoreTravelerPhoenix(bool received) => TravelerPhoenixReceived = received;
        internal void ReceiveTravelerPhoenix()
        {
            if (TravelerPhoenixReceived) return;
            TravelerPhoenixReceived = true;
            Changed?.Invoke();
        }
        public bool CastleGuardsDefeated { get; private set; }
        internal void RestoreCastleGuards(bool defeated) => CastleGuardsDefeated = defeated;
        public void DefeatCastleGuards()
        {
            if (CastleGuardsDefeated) return;
            CastleGuardsDefeated = true;
            Changed?.Invoke();
        }
        internal string[] OpenedChests => new List<string>(openedChests).ToArray();
        internal void Restore(int gold, string[] chests)
        { this.gold = gold; openedChests.Clear(); foreach (string id in chests) openedChests.Add(id); }

        public ExplorationProgress(int initialGold = 0)
        {
            if (initialGold < 0) throw new ArgumentOutOfRangeException(nameof(initialGold));
            gold = initialGold;
        }

        public bool CanReceiveGold(int amount) => amount > 0 && (HasInfiniteGold || gold <= int.MaxValue - amount);
        public bool TryAddGold(int amount)
        {
            if (!CanReceiveGold(amount)) return false;
            if (!HasInfiniteGold) gold += amount;
            Changed?.Invoke(); return true;
        }
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || amount > Gold) return false;
            if (!HasInfiniteGold) gold -= amount;
            Changed?.Invoke(); return true;
        }

        public bool HasOpenedChest(string id) => openedChests.Contains(id);

        public bool TryOpenChest(string id, int goldReward)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Bau precisa de ID.", nameof(id));
            if (goldReward < 0) throw new ArgumentOutOfRangeException(nameof(goldReward));
            if (openedChests.Contains(id)) return false;
            int total = HasInfiniteGold ? gold : checked(gold + goldReward);
            openedChests.Add(id);
            gold = total;
            Changed?.Invoke();
            return true;
        }
    }
}
