using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Camera view;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private Texture2D panelTexture;

        public void Initialize(Camera worldCamera) => view = worldCamera;

        private void OnGUI()
        {
            if (view == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;

            DrawLabel(new Vector2(-9, 4.3f), "ITENS", scale);
            DrawLabel(new Vector2(9, 4.3f), "MAGIAS", scale);
            DrawLabel(new Vector2(-9, -5.2f), "HOSPEDARIA", scale);
            DrawLabel(new Vector2(9, -5.2f), "ARMADURAS", scale);
            DrawLabel(new Vector2(0, 8.8f), "CASTELO", scale);

            GUI.DrawTexture(new Rect(16, 16, Mathf.Min(390, width - 32), 94), panelTexture);
            GUI.Label(new Rect(30, 25, width - 60, 27), "DRAGON QUEST  /  EXPLORACAO", titleStyle);
            GUI.Label(new Rect(30, 54, width - 60, 24), "WASD ou setas para andar", bodyStyle);
            GUI.Label(new Rect(30, 78, width - 60, 24), "Parte 1: mapa e personagem provisorios", bodyStyle);

            GUI.DrawTexture(new Rect(16, Screen.height / scale - 54, width - 32, 38), panelTexture);
            GUI.Label(new Rect(28, Screen.height / scale - 47, width - 56, 26),
                "Explore a cidade. Predios, fonte e muralhas bloqueiam a passagem.", bodyStyle);
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
