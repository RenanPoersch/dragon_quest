using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class ChestInteraction : Interactable
    {
        private string chestId;
        private int goldReward;
        private SpriteRenderer lid;
        private bool opened;

        public override string Prompt => opened ? "Examinar bau aberto" : "Abrir bau";

        public void Initialize(string id, int reward, SpriteRenderer lidRenderer)
        {
            chestId = id;
            goldReward = reward;
            lid = lidRenderer;
        }

        public override void Interact(InteractionContext context)
        {
            if (context.Progress.TryOpenChest(chestId, goldReward))
                context.Dialogue.Open("Bau da praca", "Voce encontrou " + goldReward + " moedas de ouro!");
            else
                context.Dialogue.Open("Bau da praca", "O bau esta vazio. Voce ja recolheu a recompensa.");

            opened = true;
            if (lid != null)
            {
                lid.color = new Color(0.3f, 0.23f, 0.16f);
                lid.transform.localPosition = new Vector3(0, 0.4f, 0);
            }
        }
    }
}
