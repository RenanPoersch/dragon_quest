using DragonQuest.Inventory;

// Reúne as definições de consumíveis usados pelo inventário e pelas lojas.
namespace DragonQuest.Content
{
    public static class ItemCatalog
    {
        // Define o consumível que recupera pontos de vida.
        public static readonly ConsumableDefinition Potion = new ConsumableDefinition("potion", "Pocao", "Recupera 40 HP de um aliado vivo e ferido. Usa uma unidade e um turno.", ItemEffect.RestoreHp, 40);
        // Define separadamente o consumível que recupera pontos de magia.
        public static readonly ConsumableDefinition Ether = new ConsumableDefinition("ether", "Eter", "Recupera 15 MP de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.RestoreMp, 15);
        public static readonly ConsumableDefinition Antidote = new ConsumableDefinition("antidote", "Antidoto", "Remove veneno de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.CleansePoison, 0);
        public static readonly ConsumableDefinition PhoenixFeather = new ConsumableDefinition("phoenix-feather", "Pluma da Fenix", "Revive um aliado derrotado com 30% do HP maximo. Usa uma unidade e um turno.", ItemEffect.Revive, 30);
    }
}
