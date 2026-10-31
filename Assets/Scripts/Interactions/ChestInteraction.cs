using UnityEngine;
using DragonQuest.Content;

namespace DragonQuest.Interactions
{
    public sealed class ChestInteraction : Interactable
    {
        private string chestId;
        private int goldReward;
        private System.Action<bool> showState;
        private bool opened;
        private System.Func<bool> wasOpened;


        public void Initialize(string id, int reward, System.Action<bool> setVisualState, System.Func<bool> isOpened = null)
        {
            chestId = id;
            goldReward = reward;
            showState = setVisualState;
            wasOpened = isOpened;
            opened = wasOpened?.Invoke() == true;
            showState?.Invoke(opened);
        }

        private void Update()
        {
            bool savedState = wasOpened?.Invoke() ?? opened;
            if (savedState != opened) { opened = savedState; showState?.Invoke(opened); }
        }

        public override void Interact(InteractionContext context)
        {
            if (context.Progress.TryOpenChest(chestId, goldReward))
                context.Dialogue.Open(DialogueCatalog.ChestGold(goldReward));
            else
                context.Dialogue.Open(DialogueCatalog.ChestEmpty);

            opened = true;
            showState?.Invoke(true);
        }
    }
}
