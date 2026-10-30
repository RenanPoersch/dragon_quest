using DragonQuest.Presentation.Battle;
using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleHud : MonoBehaviour
    {
        private readonly BattleMenuPresenter commands = new BattleMenuPresenter();
        public BattleMenuPresenter Menu => commands;
        private readonly BattleHudGraphics graphics = new BattleHudGraphics();
        private BattleArenaPresentation arena;
        public BattleArenaPresentation Arena => arena;

        private PrototypeBattleController controller;

        public void Initialize(PrototypeBattleController battleController, PixelSpriteLibrary sprites = null)
        {
            controller = battleController; commands.Initialize(battleController);
            if (arena == null) arena = new BattleArenaPresentation(graphics, commands);
            arena.Initialize(battleController, sprites);
        }

        private void ObserveBattle()
        {
            commands.ObserveBattle();
            arena.ObserveBattle();
        }

        private void Update()
        {
            if (controller == null || !controller.IsActive || controller.IsPostBattleOpen) { arena?.ClearNumbers(); return; }
            ObserveBattle();
            commands.UpdateInput();
        }

        private void OnGUI()
        {
            if (controller == null || !controller.IsActive || controller.IsPostBattleOpen
                || controller.EntryTransition?.HidesBattle == true) return;
            // Keyboard commands belong to Update; IMGUI must not confirm the new selection again.
            if (Event.current.isKey) Event.current.Use();
            graphics.EnsureCreated(); arena.Prepare(); ObserveBattle();
            GUISkin previousSkin = GUI.skin;
            GUI.skin = graphics.Skin;
            graphics.RectBox(new Rect(0, 0, Screen.width, Screen.height), new Color(.015f, .02f, .045f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Matrix4x4 arenaMatrix = GUI.matrix;
            arena.Draw();
            GUI.matrix = arenaMatrix;
            graphics.Panel(new Rect(20, 426, 1240, 60));
            if (controller.IsPresentingAction && controller.IsPresentingUltimate)
                GUI.Label(new Rect(40, 426, 1200, 60), controller.PresentingAbility.Name, graphics.Centered);
            else
                GUI.Label(new Rect(40, 438, 1200, 40), controller.PresentedMessage, graphics.Text);
            // Equal halves: all commands/target choices stay on the left, party status stays on the right.
            graphics.Panel(new Rect(20, 496, 620, 204)); graphics.Panel(new Rect(640, 496, 620, 204));
            DrawPartyStatus();
            if (controller.IsPresentingAction)
            {
                GUI.Label(new Rect(40, 512, 570, 30), controller.PresentingActor.Definition.Name, graphics.Title);
                if (!controller.IsCastleEncounter || controller.PresentingActor.Team == CombatTeam.Party)
                    GUI.Label(new Rect(40, 556, 570, 55), controller.PresentingAbility.Name, graphics.Text);
            }
            else if (controller.Session.Outcome != BattleOutcome.None)
            {
                GUI.Label(new Rect(40, 508, 575, 30), controller.Session.Outcome == BattleOutcome.Completed ? "DESFECHO"
                    : controller.Session.Outcome == BattleOutcome.Victory ? "VITORIA!" : "DERROTA", graphics.Title);
                GUI.Label(new Rect(40, 546, 575, 75), controller.OutcomeDescription, graphics.Text);
                if (controller.Session.Outcome != BattleOutcome.Defeat
                    && CommandButton(new Rect(40, 645, 565, 36), controller.ReturnLabel, true, true)) controller.ReturnToCity();
            }
            else if (commands.Actor != null && commands.Actor.Team == CombatTeam.Enemy)
            {
                GUI.Label(new Rect(40, 512, 570, 30), "Turno de " + commands.Actor.Definition.Name, graphics.Title);
            }
            else if (commands.Actor != null) commands.DrawActions(graphics.Title, graphics.Small, CommandButton);
            if (!string.IsNullOrEmpty(controller.ErrorMessage)) GUI.Label(new Rect(40, 668, 560, 25), controller.ErrorMessage, graphics.Small);
            GUI.skin = previousSkin;
            GUI.matrix = previous;
            controller.EntryTransition?.MarkBattleReady();
            // Cobre tambem os paineis e as barras externas no mesmo instante do dano.
            if (controller.MeteorWhiteOpacity > 0)
            {
                GUI.matrix = Matrix4x4.identity;
                graphics.RectBox(new Rect(0, 0, Screen.width, Screen.height), Color.white);
                GUI.matrix = arenaMatrix;
                GUI.BeginGroup(new Rect(0, 0, 1280, 416));
                arena.DrawFloatingNumbers(true);
                GUI.EndGroup();
                GUI.matrix = previous;
            }
        }

        private void DrawPartyStatus()
        {
            GUI.Label(new Rect(850, 508, 170, 28), "HP", graphics.Centered);
            GUI.Label(new Rect(1030, 508, 120, 28), "MP", graphics.Centered);
            GUI.Label(new Rect(1155, 508, 80, 28), "LIMIT", graphics.Centered);
            int i = 0;
            foreach (CombatantState participant in controller.Session.Combatants)
            {
                if (participant.Team != CombatTeam.Party) continue;
                float y = 544 + i++ * 48;
                if (participant == commands.Actor) graphics.RectBox(new Rect(650, y - 2, 600, 45), new Color(.16f, .29f, .65f));
                GUI.Label(new Rect(665, y, 95, 25), participant.Definition.Name, graphics.Text);
                var beforeSalvation = controller.PendingSalvationState(participant);
                graphics.StatusIcons.Draw(770, y + 2, beforeSalvation?.Statuses ?? CombatStatuses.Active(participant));
                graphics.DrawBar(new Rect(850, y + 4, 170, 25), beforeSalvation?.Hp ?? participant.Hp, participant.Definition.MaxHp, new Color(.18f, .60f, .47f));
                graphics.DrawBar(new Rect(1030, y + 4, 120, 25), participant.Mp, participant.Definition.MaxMp, new Color(.20f, .45f, .82f));
                graphics.DrawBar(new Rect(1160, y + 4, 75, 25), participant.Limit.Charge, 100, new Color(.62f, .28f, .75f));
            }
        }

        private bool CommandButton(Rect rect, string label, bool focused, bool enabled, Texture2D icon = null, bool limitReady = false)
            => graphics.CommandButton(rect, label, focused, enabled, commands.CanAcceptMenuInput, icon, limitReady);

        private void OnDestroy() => graphics.Dispose();
    }
}
