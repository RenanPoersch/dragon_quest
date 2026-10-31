using DragonQuest.Gameplay.Battle;
using UnityEngine;
using DragonQuest.Interactions;

namespace DragonQuest.Presentation.Dialogue
{
    public sealed class DialogueHud : MonoBehaviour
    {
        private DialogueController dialogue;
        private BattleController battle;
        private DialoguePresentation presentation;

        public void Initialize(DialogueController dialogueController, BattleController battleController)
        {
            dialogue = dialogueController;
            battle = battleController;
        }

        private void OnGUI()
        {
            if (dialogue == null || !dialogue.IsOpen || battle == null || battle.IsBusy) return;
            if (presentation == null) presentation = new DialoguePresentation();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            try
            {
                presentation.Draw(dialogue, Screen.width / scale, Screen.height / scale);
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void OnDestroy() => presentation?.Dispose();
    }
}
