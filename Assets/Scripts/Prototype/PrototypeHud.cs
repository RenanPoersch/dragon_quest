using UnityEngine;
using DragonQuest.Interactions;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PlayerInteraction interaction;
        private DialogueController dialogue;
        private PrototypeBattleController battle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle choiceStyle;
        private Texture2D panelTexture;

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
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (dialogue.IsOpen)
            {
                bool choosing=dialogue.HasChoices;
                float panelTop = height - (choosing ? 320 : 205);
                GUI.DrawTexture(new Rect(16, panelTop, width - 32, choosing ? 304 : 189), panelTexture);
                GUI.Label(new Rect(30, panelTop + 12, width - 60, 27), dialogue.Title, titleStyle);
                GUI.Label(new Rect(30, panelTop + 48, width - 60, choosing ? 64 : 89), dialogue.Text, bodyStyle);
                if(choosing)
                {
                    var choices=dialogue.Choices;
                    for(int i=0;i<choices.Count;i++)
                        if(GUI.Button(new Rect(30,panelTop+120+i*44,width-60,38),(i+1)+". "+choices[i], choiceStyle))
                        {dialogue.Choose(i);break;}
                }
                GUI.Label(new Rect(30, panelTop + (choosing ? 262 : 145), width - 60, 30), choosing
                    ? "1 / 2 / 3 ou clique: escolher  |  Esc: cancelar"
                    : "E / Espaco / Enter: continuar   |   Esc: fechar   (" + dialogue.PageNumber + "/" + dialogue.PageCount + ")", bodyStyle);
            }
            else
            {
                string prompt = interaction.CurrentPrompt;
                if (!string.IsNullOrEmpty(prompt))
                {
                    float promptWidth = Mathf.Min(380, width - 32);
                    float left = (width - promptWidth) / 2;
                    GUI.DrawTexture(new Rect(left, height - 64, promptWidth, 48), panelTexture);
                    GUI.Label(new Rect(left + 12, height - 57, promptWidth - 24, 38), prompt, bodyStyle);
                }
            }
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = PrototypeTypography.Style(GUI.skin.label, 24, true);
            titleStyle.normal.textColor = new Color(0.94f, 0.80f, 0.48f);
            bodyStyle = PrototypeTypography.Style(GUI.skin.label, 20); bodyStyle.wordWrap = true;
            choiceStyle = PrototypeTypography.Style(GUI.skin.button, 20);
            bodyStyle.normal.textColor = new Color(0.93f, 0.93f, 0.88f);
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.08f, 0.12f, 0.11f, 0.92f));
            panelTexture.Apply();
        }

        private void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
        }
    }
}
