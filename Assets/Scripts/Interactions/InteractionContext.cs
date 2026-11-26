using System;
using DragonQuest.Exploration;
using DragonQuest.Progression;

// Entrega a cada interação os dados e as ações disponíveis durante a exploração.
namespace DragonQuest.Interactions
{
    public sealed class InteractionContext
    {
        public DialogueController Dialogue { get; }
        public ExplorationProgress Progress { get; }
        // Permite solicitar uma viagem informando o identificador do destino.
        public Action<string> TravelTo { get; }
        public Action StartBattle { get; }
        // Consulta se o jogo está ocupado quando a interação precisa tomar uma decisão.
        public Func<bool> IsBusy { get; }
        public Action<string> OpenShop { get; }
        public AncientSecrets Secrets { get; }

        public InteractionContext(DialogueController dialogue, ExplorationProgress progress, Action<string> travelTo,
            Action startBattle = null, Func<bool> isBusy = null, Action<string> openShop = null, AncientSecrets secrets = null)
        {
            Dialogue = dialogue;
            Progress = progress;
            TravelTo = travelTo;
            StartBattle = startBattle;
            IsBusy = isBusy ?? (() => false);
            OpenShop = openShop;
            Secrets = secrets;
        }
    }
}
