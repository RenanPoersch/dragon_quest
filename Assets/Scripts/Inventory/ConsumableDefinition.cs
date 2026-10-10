using System;

namespace DragonQuest.Inventory
{
    public enum ItemEffect { RestoreHp, RestoreMp, CleansePoison, Revive }

    public sealed class ConsumableDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemEffect Effect { get; }
        public int Power { get; }

        public ConsumableDefinition(string id, string name, string description, ItemEffect effect, int power)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item precisa de ID.");
            if (!Enum.IsDefined(typeof(ItemEffect), effect)) throw new ArgumentException("Efeito de item desconhecido.");
            if ((effect == ItemEffect.RestoreHp || effect == ItemEffect.RestoreMp) && power <= 0)
                throw new ArgumentException("Recuperacao precisa de valor positivo.");
            if (effect == ItemEffect.Revive && (power < 1 || power > 100)) throw new ArgumentException("Reviver exige percentual de 1 a 100.");
            if (effect == ItemEffect.CleansePoison && power != 0) throw new ArgumentException("Antidoto nao usa poder.");
            Id = id; Name = name; Description = description; Effect = effect; Power = power;
        }
    }
}
