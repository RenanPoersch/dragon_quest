using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypePostBattleHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private readonly HeroVictoryAnimation sprites = new HeroVictoryAnimation();
        private Texture2D pixel;
        private GUIStyle text, title, value, right, small, up;
        public void Initialize(PrototypeBattleController battleController) => controller = battleController;
        private void Update()
        {
            if (controller == null || !controller.IsPostBattleOpen || !Application.isFocused) return;
            var keys = Keyboard.current;
            if (keys != null && (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame
                || keys.escapeKey.wasPressedThisFrame)) controller.ReturnToCity();
        }
        private void OnGUI()
        {
            if (controller == null || !controller.IsPostBattleOpen) return;
            Draw(controller.PostBattle, Time.unscaledTime - controller.PostBattleStartedAt);
        }
        // Shared by the live game and the Editor validation capture.
        public void Draw(PostBattleResults result, double elapsed)
        {
            if (result == null || !result.IsComplete) return;
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.008f, .012f, .035f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Panel(new Rect(110, 28, 1060, 54));
            GUI.Label(new Rect(140, 39, 990, 36), "Experiencia recebida!", title);
            Panel(new Rect(110, 82, 530, 54)); Panel(new Rect(640, 82, 530, 54));
            GUI.Label(new Rect(144, 93, 150, 32), "XP TOTAL", text);
            GUI.Label(new Rect(304, 93, 300, 32), "+" + result.EnemyExperience, right);
            GUI.Label(new Rect(674, 93, 150, 32), "OURO", text);
            GUI.Label(new Rect(834, 93, 300, 32), "+" + result.Gold, right);
            float rowHeight = 146;
            for (int i = 0; i < result.Members.Count; i++) DrawMember(result.Members[i], 136 + i * rowHeight, rowHeight, elapsed);
            if (!string.IsNullOrEmpty(result.BonusMessage))
            {
                float footer = 136 + result.Members.Count * rowHeight;
                Panel(new Rect(110, footer, 1060, 54));
                GUI.Label(new Rect(142, footer + 12, 990, 28), result.BonusMessage, small);
            }
            GUI.matrix = previous;
        }
        private void DrawMember(PostBattleMemberResult member, float y, float height, double elapsed)
        {
            Panel(new Rect(110, y, 1060, height));
            var state = member.At(elapsed);
            bool leveledUp = state.Level > member.BeforeLevel;
            var figure = new Rect(138, y + 34, 166, 96);
            sprites.Draw(figure, member.Id, leveledUp, elapsed);
            if (leveledUp)
            {
                var label = new Rect(figure.center.x - 68, y + 7, 136, 30);
                up.normal.textColor = new Color(.04f, .03f, .12f);
                GUI.Label(new Rect(label.x + 2, label.y + 2, label.width, label.height), "Up!", up);
                up.normal.textColor = new Color(1f, .91f, .30f); GUI.Label(label, "Up!", up);
            }
            GUI.Label(new Rect(332, y + 21, 320, 36), member.Name, title);
            GUI.Label(new Rect(376, y + 76, 268, 32), "Nivel: " + state.Level, text);
            value.normal.textColor = new Color(.81f, .88f, 1f);
            GUI.Label(new Rect(332, y + 112, 325, 24), "+" + (state.Total - member.BeforeExperience) + " XP", value);
            GUI.Label(new Rect(710, y + 22, 120, 32), "EXP:", text);
            GUI.Label(new Rect(840, y + 22, 290, 32), state.Total.ToString(), right);
            Rect bar = new Rect(910, y + 68, 218, 14);
            Fill(bar, new Color(.77f, .78f, .89f)); Fill(new Rect(bar.x + 2, bar.y + 2, bar.width - 4, bar.height - 4), new Color(.16f, .15f, .31f));
            Fill(new Rect(bar.x + 2, bar.y + 2, (bar.width - 4) * state.Progress, bar.height - 4), new Color(.96f, .66f, .70f));
            GUI.Label(new Rect(680, y + 96, 360, 32), state.Level == CharacterProgression.MaxLevel ? "Nivel maximo" : "Proximo nivel:", text);
            if (state.Level < CharacterProgression.MaxLevel) GUI.Label(new Rect(1010, y + 96, 120, 32), state.ToNextLevel.ToString(), right);
        }
        private void Panel(Rect area)
        {
            Fill(area, new Color(.015f, .015f, .05f));
            Fill(new Rect(area.x + 2, area.y + 2, area.width - 4, area.height - 4), new Color(.79f, .80f, .90f));
            Rect inner = new Rect(area.x + 5, area.y + 5, area.width - 10, area.height - 10);
            for (int row = 0; row < 24; row++)
                Fill(new Rect(inner.x, inner.y + inner.height * row / 24, inner.width, inner.height / 24 + 1),
                    Color.Lerp(new Color(.075f, .12f, .59f), new Color(.025f, .028f, .27f), row / 23f));
        }
        private void Fill(Rect area, Color color)
        { var previous = GUI.color; GUI.color = color; GUI.DrawTexture(area, pixel); GUI.color = previous; }
        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            text = PrototypeTypography.Style(GUI.skin.label, 28, true);
            title = PrototypeTypography.Style(text, 32, true);
            value = PrototypeTypography.Style(text, 22);
            small = PrototypeTypography.Style(text, 24);
            right = new GUIStyle(text) { alignment = TextAnchor.MiddleRight };
            up = PrototypeTypography.Style(text, 36, true); up.alignment = TextAnchor.MiddleCenter;
            foreach (var style in new[] { text, title, value, small, right, up }) style.normal.textColor = Color.white;
        }
        private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
