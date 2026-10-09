using System;
using DragonQuest.Exploration;

namespace DragonQuest.Interactions
{
    public sealed class InteractionContext
    {
        public DialogueController Dialogue { get; }
        public ExplorationProgress Progress { get; }
        public Action<string> TravelTo { get; }

        public InteractionContext(DialogueController dialogue, ExplorationProgress progress, Action<string> travelTo)
        {
            Dialogue = dialogue;
            Progress = progress;
            TravelTo = travelTo;
        }
    }
}
