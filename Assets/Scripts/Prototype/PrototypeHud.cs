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
        private GUIStyle labelStyle;
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

            if (locations.IsInShop)
            {
                DrawLabel(new Vector2(0, 1.9f), "LOJISTA", scale);
                DrawLabel(new Vector2(0, -3.7f), "SAIDA", scale);
            }
            else if (locations.IsInCity)
            {
                DrawLabel(new Vector2(-9, 4.3f), "ITENS", scale);
                DrawLabel(new Vector2(9, 4.3f), "MATERIAS", scale);
                DrawLabel(new Vector2(-9, -5.2f), "HOSPEDARIA", scale);
                DrawLabel(new Vector2(9, -5.2f), "EQUIPAMENTOS", scale);
                DrawLabel(PrototypeCityLayout.Castle, "CASTELO", scale);
                DrawLabel(PrototypeCityLayout.Citizen + Vector2.up * 0.9f, "ALDEAO", scale);
                DrawLabel(PrototypeCityLayout.Chest + Vector2.up * 0.9f, "BAU", scale);
                DrawLabel(PrototypeCityLayout.Guard + Vector2.up * 0.9f, "GUARDA", scale);
                DrawLabel(PrototypeCityLayout.Resident + Vector2.up * 0.9f, "CAMPONESA", scale);
                DrawLabel(PrototypeCityLayout.Gardener + Vector2.up * 0.9f, "JARDINEIRO", scale);
                DrawLabel(PrototypeCityLayout.Explorer + Vector2.up * 0.9f, "EXPLORADORA", scale);
                DrawLabel(new Vector2(-24, 21), "CAPELA ANTIGA", scale);
                DrawLabel(new Vector2(24, -19.5f), "PEDESTAL", scale);
                DrawLabel(PrototypeWorldLayout.CityExit, "SAIDA PARA A ILHA", scale);
                DrawLabel(PrototypeWorldLayout.CastleDoor, "ENTRAR NO CASTELO", scale);
            }
            else if (locations.LocationId == PrototypeTravel.World)
            {
                DrawLabel(PrototypeWorldLayout.Castle + Vector2.up * 1.5f, "CASTELO E ARREDORES", scale);
                DrawLabel(PrototypeWorldLayout.WestForest + Vector2.up * 1.5f, "FLORESTA OESTE", scale);
                DrawLabel(PrototypeWorldLayout.EastForest + Vector2.up * 1.5f, "FLORESTA LESTE", scale);
                DrawLabel(PrototypeWorldLayout.Ruins + Vector2.up * 1.5f,"RUINAS ANTIGAS",scale);
                DrawLabel(PrototypeWorldLayout.Volcano + Vector2.up * 1.4f, "VULCAO", scale);
            }
            else if (locations.LocationId == PrototypeTravel.Castle)
            {
                DrawLabel(PrototypeWorldLayout.Throne + Vector2.up, "TIRANO", scale);
                DrawLabel(PrototypeWorldLayout.CastleExit, "SAIDA PARA A PERIFERIA", scale);
            }
            else if(locations.LocationId==PrototypeTravel.Ruins)
            {
                DrawLabel(PrototypeSecretLayout.Altar+Vector2.up*0.8f,"ALTAR ANTIGO",scale);
                DrawLabel(PrototypeRuinsLayout.Exit,"SAIDA PARA A ILHA",scale);
            }
            else
            {
                DrawLabel(new Vector2(1.5f, 1.9f), locations.LocationId == PrototypeTravel.EastForest ? "VIAJANTE" : "LENHADOR", scale);
                DrawLabel(PrototypeWorldLayout.LocalExit, "SAIDA PARA A ILHA", scale);
            }

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
                        : "Converse com o morador usando E. A saida para a ilha fica ao sul.";
                GUI.Label(new Rect(28, height - 57, width - 56, 38), prompt, bodyStyle);
            }
            GUI.matrix = previousMatrix;
        }

        private void DrawLabel(Vector2 worldPosition, string text, float scale)
        {
            Vector3 screen = view.WorldToScreenPoint(ObliqueProjection.Point(worldPosition, 1.5f));
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
