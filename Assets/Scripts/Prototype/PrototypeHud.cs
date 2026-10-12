using UnityEngine;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Progression;

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
        private PrototypeShopController shops;
        private AncientSecrets secrets;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public void Initialize(Camera worldCamera, PlayerInteraction playerInteraction,
            DialogueController dialogueController, ExplorationProgress explorationProgress, PrototypeLocations locationController,
            PrototypeBattleController battleController, PrototypeShopController shopController, AncientSecrets ancientSecrets)
        {
            view = worldCamera;
            interaction = playerInteraction;
            dialogue = dialogueController;
            progress = explorationProgress;
            locations = locationController;
            battle = battleController;
            shops = shopController;
            secrets = ancientSecrets;
        }

        private void OnGUI()
        {
            if (view == null || battle.IsBusy) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            GUI.DrawTexture(new Rect(16, 16, Mathf.Min(590, width - 32), secrets.IsDiscovered ? 172 : 126), panelTexture);
            GUI.Label(new Rect(30, 25, width - 60, 27), "DRAGON QUEST  /  " + locations.LocationName, titleStyle);
            GUI.Label(new Rect(30, 54, width - 60, 24), "WASD/setas: andar  |  E: interagir", bodyStyle);
            GUI.Label(new Rect(30, 78, width - 60, 24), "I: inventario" + (secrets.KnowsPoem ? "  |  P: poema" : "") + "  |  Ouro: " + progress.Gold, bodyStyle);
            GUI.Label(new Rect(30, 104, Mathf.Min(442, width - 60), 34), shops.Quest.Summary, bodyStyle);
            if(secrets.IsDiscovered) GUI.Label(new Rect(30,144,Mathf.Min(548,width-60),34),secrets.Summary,bodyStyle);

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
                        if(GUI.Button(new Rect(30,panelTop+120+i*44,width-60,38),(i+1)+". "+choices[i]))
                        {dialogue.Choose(i);break;}
                }
                GUI.Label(new Rect(30, panelTop + (choosing ? 262 : 145), width - 60, 30), choosing
                    ? "1 / 2 / 3 ou clique: escolher  |  Esc: cancelar"
                    : "E / Espaco / Enter: continuar   |   Esc: fechar   (" + dialogue.PageNumber + "/" + dialogue.PageCount + ")", bodyStyle);
            }
            else
            {
                GUI.DrawTexture(new Rect(16, height - 64, width - 32, 48), panelTexture);
                string prompt = interaction.CurrentPrompt;
                if (string.IsNullOrEmpty(prompt))
                    prompt = locations.IsInShop ? "Use E no lojista para comprar/vender; a saida fica ao sul."
                        : locations.LocationId == PrototypeTravel.World ? "Caminhe ate a entrada do castelo, florestas ou ruinas para entrar automaticamente."
                        : locations.IsInCity ? "Lojas ao redor da praca. Castelo ao norte; saida para a ilha ao sul. Caminhe pelas entradas."
                        : locations.LocationId == PrototypeTravel.Castle ? "O trono fica ao norte; caminhe ate a saida ao sul para voltar a periferia."
                        : locations.LocationId == PrototypeTravel.Ruins ? "Explore as ruinas. O altar fica ao norte; a saida para a ilha fica ao sul."
                        : locations.LocationId == PrototypeTravel.Volcano ? "Siga a trilha entre a lava ate a arena. A saida fica ao sul."
                        : locations.LocationId == PrototypeTravel.Inn ? "F5: salvar jornada / E na hospedeira: salvar ou conversar / I: inventario. Saida ao sul."
                        : "Converse com o morador usando E. A saida para a ilha fica ao sul.";
                GUI.Label(new Rect(28, height - 57, width - 56, 38), prompt, bodyStyle);
            }
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.94f, 0.80f, 0.48f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
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
