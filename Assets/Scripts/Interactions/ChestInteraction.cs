using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class ChestInteraction : Interactable
    {
        private string chestId;
        private int goldReward;
        private System.Action openLid;
        private bool opened;
        private System.Func<bool> wasOpened;

        public override string Prompt => opened ? "Examinar bau aberto" : "Abrir bau";

        public void Initialize(string id, int reward, System.Action showOpened, System.Func<bool> isOpened = null)
        {
            chestId = id;
            goldReward = reward;
            openLid = showOpened;
            wasOpened = isOpened;
        }

        private void Update()
        {
            if (!opened && wasOpened?.Invoke() == true) { opened = true; openLid?.Invoke(); }
        }

        public override void Interact(InteractionContext context)
        {
            if (context.Progress.TryOpenChest(chestId, goldReward))
                context.Dialogue.Open("Bau da praca", "Voce encontrou " + goldReward + " moedas de ouro!");
            else
                context.Dialogue.Open("Bau da praca", "O bau esta vazio. Voce ja recolheu a recompensa.");

            opened = true;
            openLid?.Invoke();
        }
    }
}
