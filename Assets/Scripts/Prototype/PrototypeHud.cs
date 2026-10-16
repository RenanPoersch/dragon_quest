using UnityEngine;
using DragonQuest.Interactions;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PlayerInteraction interaction;
        private DialogueController dialogue;
        private PrototypeBattleController battle;
        private DialoguePresentation presentation;

        public void Initialize(PlayerInteraction playerInteraction, DialogueController dialogueController,
            PrototypeBattleController battleController)
        {
            interaction = playerInteraction;
            dialogue = dialogueController;
            battle = battleController;
        }

        private void OnGUI()
        {
            if (interaction == null || dialogue == null || battle == null || battle.IsBusy) return;
            if (presentation == null) presentation = new DialoguePresentation();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            try
            {
                float width = Screen.width / scale, height = Screen.height / scale;
                if (dialogue.IsOpen) presentation.Draw(dialogue, width, height);
                else presentation.DrawPrompt(interaction.CurrentPrompt, width, height);
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void OnDestroy() => presentation?.Dispose();
    }
}
