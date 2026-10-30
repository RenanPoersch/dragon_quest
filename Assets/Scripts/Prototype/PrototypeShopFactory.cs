using DragonQuest.Content;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Shops;

namespace DragonQuest.Prototype
{
    public static class PrototypeShopFactory
    {
        public const int StartingGold = ShopCatalog.StartingGold;
        public static ShopService Create(PartyProgress party, ExplorationProgress progress) => ShopCatalog.Create(party, progress);
    }
}
