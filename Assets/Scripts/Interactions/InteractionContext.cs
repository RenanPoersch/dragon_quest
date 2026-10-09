using System;
using DragonQuest.Exploration;

namespace DragonQuest.Interactions
{
    public sealed class InteractionContext
    {
        public DialogueController Dialogue { get; }
        public ExplorationProgress Progress { get; }
        public Action<string> TravelTo { get; }
        public Action StartBattle { get; }
        public Func<bool> IsBusy { get; }

        public InteractionContext(DialogueController dialogue, ExplorationProgress progress, Action<string> travelTo,
            Action startBattle = null, Func<bool> isBusy = null)
        {
            Dialogue = dialogue;
            Progress = progress;
            TravelTo = travelTo;
            StartBattle = startBattle;
            IsBusy = isBusy ?? (() => false);
        }
    }
}
