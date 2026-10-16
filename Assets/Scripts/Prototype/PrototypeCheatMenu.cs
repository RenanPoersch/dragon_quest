using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Run before exploration and combat so opening/closing cannot leak input to the game.
    [DefaultExecutionOrder(-1000)]
    public sealed class PrototypeCheatMenu : MonoBehaviour
    {
        private const int WindowId = 781399;
        private const int OptionCount = 5;
        private PlayerMovement player;
        private PrototypeBattleController battle;
        private PrototypeSaveController saves;
        private ExplorationProgress progress;
        private GUIStyle title, text, small, button, window;
        private int selected, closedFrame = -1;
        private string message = "Escolha uma opcao.";
        public bool IsOpen { get; private set; }
        public bool BlocksInput => IsOpen || closedFrame == Time.frameCount;

        public void Initialize(PlayerMovement hero, PrototypeBattleController combat,
            PrototypeSaveController saveController, ExplorationProgress exploration)
        {
            player = hero; battle = combat; saves = saveController; progress = exploration;
            IsOpen = false; closedFrame = -1; selected = 0;
        }

        private void Update()
        {
            if (battle == null || saves.IsMenuVisible || battle.HasEnded || !Application.isFocused) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else { IsOpen = true; player.SetMovementEnabled(false); }
            }
            else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        private void Close()
        {
            IsOpen = false;
            closedFrame = Time.frameCount;
            // Exploration resumes on the next frame, after the close key has been consumed.
            player.SetMovementEnabled(false);
        }

        private void Activate(int option)
        {
            if (option == 0)
            {
                battle.Party.SetInfiniteLimit(!battle.Party.HasInfiniteLimit);
                message = battle.Party.HasInfiniteLimit ? "LIMIT sempre pronto para toda a equipe." : "LIMIT voltou ao normal.";
            }
            else if (option == 1)
            {
                progress.SetInfiniteGold(!progress.HasInfiniteGold);
                message = progress.HasInfiniteGold ? "Compras e hospedagem sem gastar gold." : "Gold voltou ao saldo original.";
            }
            else if (option == 2)
            {
                battle.Party.SetMaxLevel(battle.IsActive ? battle.Session : null);
                message = "Toda a equipe esta no nivel 99. Salve a jornada para manter os niveis.";
            }
            else if (option == 3)
            {
                battle.SetRandomEncountersEnabled(!battle.AreRandomEncountersEnabled);
                message = battle.AreRandomEncountersEnabled ? "Encontros aleatorios reativados."
                    : "Sem encontros aleatorios. Batalhas por interacao continuam disponiveis.";
            }
            else Close();
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            EnsureStyles();
            int previousDepth = GUI.depth;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.depth = -200;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(0, 0, 0, .72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.ModalWindow(WindowId, new Rect(310, 72, 660, 576), DrawMenu, "", window);
            GUI.matrix = previousMatrix; GUI.depth = previousDepth; GUI.color = previousColor;
        }

        private void DrawMenu(int id)
        {
            var current = Event.current;
            if (current.type == EventType.KeyDown)
            {
                KeyCode key = current.keyCode;
                current.Use();
                if (key == KeyCode.UpArrow || key == KeyCode.W) selected = (selected + OptionCount - 1) % OptionCount;
                else if (key == KeyCode.DownArrow || key == KeyCode.S) selected = (selected + 1) % OptionCount;
                else if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space) Activate(selected);
                if (!IsOpen) return;
            }
            GUI.color = new Color(.04f, .07f, .14f);
            GUI.DrawTexture(new Rect(0, 0, 660, 576), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 20, 600, 42), "CHEAT ENGINE", title);
            GUI.Label(new Rect(30, 65, 600, 28), "` abre/fecha  |  Esc fecha  |  Setas + Enter ou clique", small);
            string[] labels = {
                "LIMIT infinito: " + (battle.Party.HasInfiniteLimit ? "LIGADO" : "DESLIGADO"),
                "Gold infinito: " + (progress.HasInfiniteGold ? "LIGADO" : "DESLIGADO"),
                "Nivel 99 / toda a equipe",
                "Encontros aleatorios: " + (battle.AreRandomEncountersEnabled ? "ATIVADOS" : "DESATIVADOS"), "Fechar" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(30, 111 + i * 64, 600, 52);
                if ((current.type == EventType.MouseMove || current.type == EventType.MouseDown)
                    && rect.Contains(current.mousePosition)) selected = i;
                GUI.color = selected == i ? new Color(1, .85f, .52f) : Color.white;
                if (GUI.Button(rect, (selected == i ? "> " : "") + labels[i], button)) Activate(i);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 441, 600, 58), message, text);
            GUI.Label(new Rect(30, 514, 600, 48),
                "LIMIT, gold e encontros valem nesta sessao. Nivel 99 permanece ao salvar.", small);
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = PrototypeTypography.Style(GUI.skin.label, 34, true);
            title.normal.textColor = new Color(1, .85f, .52f);
            text = PrototypeTypography.Style(GUI.skin.label, 23); text.wordWrap = true;
            text.normal.textColor = Color.white;
            small = PrototypeTypography.Style(text, 19);
            button = PrototypeTypography.Style(GUI.skin.button, 26);
            window = new GUIStyle(GUI.skin.window);
        }
    }
}
