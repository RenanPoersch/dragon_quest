using UnityEngine;
using DragonQuest.Exploration;
using DragonQuest.Interactions;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Camera view;
        private PlayerInteraction interaction;
        private DialogueController dialogue;
        private ExplorationProgress progress;
        private PrototypeLocations locations;
        private PrototypeBattleController battle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private Texture2D panelTexture;

        public void Initialize(Camera worldCamera, PlayerInteraction playerInteraction,
            DialogueController dialogueController, ExplorationProgress explorationProgress, PrototypeLocations locationController,
            PrototypeBattleController battleController)
        {
            view = worldCamera;
            interaction = playerInteraction;
            dialogue = dialogueController;
            progress = explorationProgress;
            locations = locationController;
            battle = battleController;
        }

        private void OnGUI()
        {
            if (view == null || battle.IsActive) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (locations.IsInShop)
            {
                DrawLabel(new Vector2(0, 1.9f), "LOJISTA", scale);
                DrawLabel(new Vector2(0, -3.7f), "SAIDA", scale);
            }
            else
            {
                DrawLabel(new Vector2(-9, 4.3f), "ITENS", scale);
                DrawLabel(new Vector2(9, 4.3f), "MAGIAS", scale);
                DrawLabel(new Vector2(-9, -5.2f), "HOSPEDARIA", scale);
                DrawLabel(new Vector2(9, -5.2f), "ARMADURAS", scale);
                DrawLabel(new Vector2(0, 8.8f), "CASTELO", scale);
                DrawLabel(new Vector2(-3.4f, -2.1f), "ALDEAO", scale);
                DrawLabel(new Vector2(3.6f, -2.3f), "BAU", scale);
                DrawLabel(new Vector2(0, 6.2f), "GUARDA", scale);
            }

            GUI.DrawTexture(new Rect(16, 16, Mathf.Min(420, width - 32), 94), panelTexture);
            GUI.Label(new Rect(30, 25, width - 60, 27), "DRAGON QUEST  /  " + locations.LocationName, titleStyle);
            GUI.Label(new Rect(30, 54, width - 60, 24), "WASD ou setas para andar", bodyStyle);
            GUI.Label(new Rect(30, 78, width - 60, 24), "E para interagir  |  Ouro: " + progress.Gold, bodyStyle);

            if (dialogue.IsOpen)
            {
                float panelTop = height - 205;
                GUI.DrawTexture(new Rect(16, panelTop, width - 32, 189), panelTexture);
                GUI.Label(new Rect(30, panelTop + 12, width - 60, 27), dialogue.Title, titleStyle);
                GUI.Label(new Rect(30, panelTop + 48, width - 60, 89), dialogue.Text, bodyStyle);
                GUI.Label(new Rect(30, panelTop + 145, width - 60, 30),
                    "E / Espaco / Enter: continuar   |   Esc: fechar   (" + dialogue.PageNumber + "/" + dialogue.PageCount + ")", bodyStyle);
            }
            else
            {
                GUI.DrawTexture(new Rect(16, height - 64, width - 32, 48), panelTexture);
                string prompt = interaction.CurrentPrompt;
                if (string.IsNullOrEmpty(prompt))
                    prompt = locations.IsInShop ? "Fale com o lojista ou procure a saida ao sul." : "Aproxime-se do aldeao, do bau ou da porta da loja de itens.";
                GUI.Label(new Rect(28, height - 57, width - 56, 38), prompt, bodyStyle);
            }
            GUI.matrix = previousMatrix;
        }

        private void DrawLabel(Vector2 worldPosition, string text, float scale)
        {
            Vector3 screen = view.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
            Rect rect = new Rect(screen.x / scale - 75, (Screen.height - screen.y) / scale - 13, 150, 26);
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(rect, text, labelStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.94f, 0.80f, 0.48f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            bodyStyle.normal.textColor = new Color(0.93f, 0.93f, 0.88f);
            labelStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
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
