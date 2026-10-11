using System;
using System.Collections.Generic;

namespace DragonQuest.Exploration
{
    // Estado da sessão separado dos objetos do cenário; persiste ao entrar/sair da loja.
    public sealed class ExplorationProgress
    {
        private readonly HashSet<string> openedChests = new HashSet<string>();

        public int Gold { get; private set; }

        public ExplorationProgress(int initialGold = 0)
        {
            if (initialGold < 0) throw new ArgumentOutOfRangeException(nameof(initialGold));
            Gold = initialGold;
        }

        public bool CanReceiveGold(int amount) => amount > 0 && Gold <= int.MaxValue - amount;
        public bool TryAddGold(int amount)
        {
            if (!CanReceiveGold(amount)) return false;
            Gold += amount; return true;
        }
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || amount > Gold) return false;
            Gold -= amount; return true;
        }

        public bool HasOpenedChest(string id) => openedChests.Contains(id);

        public bool TryOpenChest(string id, int goldReward)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Bau precisa de ID.", nameof(id));
            if (goldReward < 0) throw new ArgumentOutOfRangeException(nameof(goldReward));
            if (openedChests.Contains(id)) return false;
            int total = checked(Gold + goldReward);
            openedChests.Add(id);
            Gold = total;
            return true;
        }
    }
}
