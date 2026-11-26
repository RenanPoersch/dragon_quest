using DragonQuest.Content;
using DragonQuest.Equipment;
using DragonQuest.Exploration;

// Entrega a matéria Fênix associada ao presente do viajante.
namespace DragonQuest.Progression
{
    public static class TravelerGift
    {
        // Identifica a cópia dada pelo viajante no inventário da jornada.
        public const string MateriaId = "traveler-phoenix";

        // Consulta o progresso do presente e registra a entrega ao grupo quando ela é aceita.
        public static bool TryGivePhoenix(PartyProgress party, ExplorationProgress progress)
        {
            if (party == null || progress == null || progress.TravelerPhoenixReceived) return false;
            if (!party.TryAddMaterias(new MateriaInstance(MateriaId, MateriaCatalog.SummonMateria))) return false;
            progress.ReceiveTravelerPhoenix();
            return true;
        }
    }
}
