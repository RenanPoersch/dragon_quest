using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Prototype;

namespace DragonQuest.Progression
{
    public static class TravelerGift
    {
        public const string MateriaId = "traveler-phoenix";

        public static bool TryGivePhoenix(PartyProgress party, ExplorationProgress progress)
        {
            if (party == null || progress == null || progress.TravelerPhoenixReceived) return false;
            if (!party.TryAddMaterias(new MateriaInstance(MateriaId, PrototypePartyFactory.SummonMateria))) return false;
            progress.ReceiveTravelerPhoenix();
            return true;
        }
    }
}
