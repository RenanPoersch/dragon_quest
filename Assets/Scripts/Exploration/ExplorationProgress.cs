using System;
using System.Collections.Generic;

namespace DragonQuest.Exploration
{
    // Estado da sessão separado dos objetos do cenário; persiste ao entrar/sair da loja.
    public sealed class ExplorationProgress
    {
        private readonly HashSet<string> openedChests = new HashSet<string>();

        public int Gold { get; private set; }

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
