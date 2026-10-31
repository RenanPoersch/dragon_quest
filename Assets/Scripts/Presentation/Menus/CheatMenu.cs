using DragonQuest.Presentation.Common;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Progression;

namespace DragonQuest.Presentation.Menus
{
    // Run before exploration and combat so opening/closing cannot leak input to the game.
    [DefaultExecutionOrder(-1000)]
    public sealed class CheatMenu : MonoBehaviour
    {
        private const int WindowId = 781399;
        private const int OptionCount = 5;
        private PlayerMovement player;
        private BattleController battle;
        private SaveController saves;
        private ExplorationProgress progress;
        private GUIStyle title, text, small, button, window;
        private int selected, selectedLevel = 1, closedFrame = -1;
        private string message = "";
        public bool IsOpen { get; private set; }
        public bool BlocksInput => IsOpen || closedFrame == Time.frameCount;

        public void Initialize(PlayerMovement hero, BattleController combat,
            SaveController saveController, ExplorationProgress exploration)
        {
            player = hero; battle = combat; saves = saveController; progress = exploration;
            IsOpen = false; closedFrame = -1; selected = 0;
        }

        private void Update()
        {
            if (battle?.Flow?.CanToggleCheats != true || battle.IsEnteringBattle) return;
            if (GameInput.CheatsPressed)
            {
                if (IsOpen) Close();
                else
                {
                    selectedLevel = battle.Party.Members.Count > 0 ? battle.Party.Members[0].Level : 1;
                    IsOpen = true; player.SetMovementEnabled(false); MenuSounds.Play(MenuSound.Confirm);
                }
            }
            else if (IsOpen)
            {
                MenuInput input = GameInput.ReadMenu();
                if (input.Cancel) { Close(); GameInput.ConsumeFrame(); return; }
                int previous = selected;
                if (selected == 2 && input.Horizontal != 0)
                    SetSelectedLevel(selectedLevel + input.Horizontal);
                else if (input.Step != 0) selected = (selected + input.Step + OptionCount) % OptionCount;
                MenuSounds.Navigate(previous, selected);
                if (input.Confirm) Activate(selected);
            }
            if (BlocksInput) GameInput.ConsumeFrame();
        }

        private void Close()
        {
            MenuSounds.Play(MenuSound.Cancel);
            IsOpen = false;
            closedFrame = Time.frameCount;
            // Exploration resumes on the next frame, after the close key has been consumed.
            player.SetMovementEnabled(false);
        }

        private void Activate(int option)
        {
            if (option < OptionCount-1) MenuSounds.Play(MenuSound.Confirm);
            if (option == 0)
            {
                battle.Party.SetInfiniteLimit(!battle.Party.HasInfiniteLimit);
                message = battle.Party.HasInfiniteLimit ? "LIMIT sempre." : "LIMIT normal.";
            }
            else if (option == 1)
            {
                progress.SetInfiniteGold(!progress.HasInfiniteGold);
                message = progress.HasInfiniteGold ? "Gold infinito." : "Gold original.";
            }
            else if (option == 2)
            {
                ApplyLevel();
            }
            else if (option == 3)
            {
                battle.SetRandomEncountersEnabled(!battle.AreRandomEncountersEnabled);
                message = battle.AreRandomEncountersEnabled ? "Encontros aleatorios ON."
                    : "Encontros aleatorios OFF.";
            }
            else Close();
        }

        private void SetSelectedLevel(int level)
        {
            level = Mathf.Clamp(level, 1, CharacterProgression.MaxLevel);
            if (selectedLevel == level) return;
            selectedLevel = level;
            MenuSounds.Play(MenuSound.Navigate);
            ApplyLevel();
        }

        private void ApplyLevel()
        {
            battle.Party.SetLevel(selectedLevel, battle.IsActive ? battle.Session : null);
            message = "Nivel " + selectedLevel + ".";
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
                // Read navigation/confirmation once in Update, including when no IMGUI key event arrives.
                current.Use();
            }
            GUI.color = new Color(.04f, .07f, .14f);
            GUI.DrawTexture(new Rect(0, 0, 660, 576), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 20, 600, 42), "CHEAT ENGINE", title);
            string[] labels = {
                "LIMIT infinito: " + (battle.Party.HasInfiniteLimit ? "LIGADO" : "DESLIGADO"),
                "Gold infinito: " + (progress.HasInfiniteGold ? "LIGADO" : "DESLIGADO"),
                "Nivel: " + selectedLevel,
                "Encontros aleatorios: " + (battle.AreRandomEncountersEnabled ? "ATIVADOS" : "DESATIVADOS"), "Fechar" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(30, 111 + i * 64, 600, 52);
                if ((current.type == EventType.MouseMove || current.type == EventType.MouseDown)
                    && rect.Contains(current.mousePosition)) MenuSounds.Select(ref selected, i);
                GUI.color = selected == i ? new Color(1, .85f, .52f) : Color.white;
                if (i == 2)
                {
                    GUI.Box(rect, GUIContent.none, button);
                    GUI.Label(new Rect(rect.x + 12, rect.y + 8, 174, 36),
                        (selected == i ? "> " : "") + labels[i], text);
                    GUI.Label(new Rect(rect.x + 192, rect.y + 13, 28, 28), "1", small);
                    GUI.Label(new Rect(rect.x + 566, rect.y + 13, 34, 28), CharacterProgression.MaxLevel.ToString(), small);
                    SetSelectedLevel(Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(rect.x + 220, rect.y + 18, 336, 24),
                        selectedLevel, 1, CharacterProgression.MaxLevel)));
                }
                else if (GUI.Button(rect, (selected == i ? "> " : "") + labels[i], button)) Activate(i);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 441, 600, 58), message, text);
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = GameTypography.Style(GUI.skin.label, 34, true);
            title.normal.textColor = new Color(1, .85f, .52f);
            text = GameTypography.Style(GUI.skin.label, 23); text.wordWrap = true;
            text.normal.textColor = Color.white;
            small = GameTypography.Style(text, 19);
            button = GameTypography.Style(GUI.skin.button, 26);
            window = new GUIStyle(GUI.skin.window);
        }
    }
}
