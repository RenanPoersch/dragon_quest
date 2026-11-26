// Centraliza as identificações de aparência usadas ao montar NPCs no mundo.
namespace DragonQuest.Presentation.World.Characters
{
    // Stable frame indices in NpcRoster; display names never choose an NPC's appearance.
    // Permite que a construção do mundo indique a aparência do NPC por um valor nomeado.
    public enum NpcAppearance
    {
        Villager = 0,
        Peasant = 1,
        Gardener = 2,
        Explorer = 3,
        ItemMerchant = 4,
        EquipmentMerchant = 5,
        MateriaMerchant = 6,
        Innkeeper = 7,
        QuarryKeeper = 8,
        Traveler = 9,
        Guard = 10,
        Tyrant = 11
    }
}
