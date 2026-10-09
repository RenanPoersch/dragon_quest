using UnityEngine;
using DragonQuest.Combat;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleHud : MonoBehaviour
    {
        private PrototypeBattleController controller;
        private Texture2D pixel;
        private GUIStyle title;
        private GUIStyle text;
        private GUIStyle small;
        private GUIStyle centered;
        private GUIStyle button;
        private CombatantState observedActor;
        private AbilityDefinition selected;

        public void Initialize(PrototypeBattleController battleController) => controller = battleController;

        private void OnGUI()
        {
            if (controller == null || !controller.IsActive) return;
            EnsureStyles();
            RectBox(new Rect(0, 0, Screen.width, Screen.height), new Color(0.035f, 0.07f, 0.09f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));

            BattleSession battle = controller.Session;
            CombatantState actor = battle.CurrentActor;
            if (actor != observedActor) { observedActor = actor; selected = null; }

            Panel(new Rect(40, 20, 1200, 70));
            GUI.Label(new Rect(58, 31, 650, 30), "BATALHA / PATRULHA DO TIRANO", title);
            GUI.Label(new Rect(58, 62, 700, 23), "Cada personagem age uma vez por rodada. Escolha uma acao e um alvo.", small);
            GUI.Label(new Rect(880, 40, 340, 34), "RODADA " + battle.Round, centered);

            int partyIndex = 0;
            int enemyIndex = 0;
            foreach (CombatantState participant in battle.Combatants)
            {
                if (participant.Team == CombatTeam.Enemy)
                    DrawStatus(new Rect(880, 135 + enemyIndex++ * 155, 360, 120), participant, participant == actor);
                else DrawStatus(new Rect(40, 110 + partyIndex++ * 118, 315, 112), participant, participant == actor);
            }

            RectBox(new Rect(385, 110, 455, 270), new Color(0.13f, 0.22f, 0.23f));
            RectBox(new Rect(385, 315, 455, 65), new Color(0.27f, 0.34f, 0.29f));
            int fighterIndex = 0;
            int enemyFigureIndex = 0;
            foreach (CombatantState participant in battle.Combatants)
            {
                Rect figure;
                if (participant.Team == CombatTeam.Party)
                {
                    figure = new Rect(445 + (fighterIndex == 1 ? -20 : 0), 135 + fighterIndex * 68, 44, 67);
                    fighterIndex++;
                }
                else figure = new Rect(725, 145 + enemyFigureIndex++ * 115, 50, 77);
                DrawFighter(figure, participant);
                if (participant == actor)
                    GUI.Label(new Rect(figure.x - 38, figure.y - 24, figure.width + 76, 24), "TURNO", centered);
            }

            Panel(new Rect(385, 394, 855, 66));
            GUI.Label(new Rect(400, 402, 825, 51), battle.LastMessage, text);
            Panel(new Rect(40, 480, 1200, 205));

            if (battle.Outcome != BattleOutcome.None)
            {
                GUI.Label(new Rect(60, 495, 1155, 42), battle.Outcome == BattleOutcome.Victory ? "VITORIA!" : "DERROTA", title);
                GUI.Label(new Rect(60, 547, 1155, 45), battle.Outcome == BattleOutcome.Victory
                    ? "A patrulha foi vencida. Sua equipe pode continuar explorando a cidade."
                    : "Sua equipe foi vencida. Volte a cidade para se reorganizar e tentar novamente.", text);
                if (GUI.Button(new Rect(60, 615, 300, 48), "Voltar para a cidade", button)) controller.ReturnToCity();
            }
            else if (actor.Team == CombatTeam.Enemy)
            {
                GUI.Label(new Rect(60, 508, 1140, 35), "Turno do inimigo", title);
                AbilityDefinition intention = EnemyAI.ChooseAbility(battle);
                GUI.Label(new Rect(60, 560, 1140, 50), actor.Definition.Name + " prepara "
                    + (intention == null ? "sua acao" : intention.Name) + "...", text);
                if (!string.IsNullOrEmpty(controller.ErrorMessage))
                    GUI.Label(new Rect(60, 620, 1140, 30), controller.ErrorMessage, small);
            }
            else DrawActions(actor, battle);

            GUI.matrix = previous;
        }

        private void DrawActions(CombatantState actor, BattleSession battle)
        {
            GUI.Label(new Rect(60, 490, 290, 30), "Turno de " + actor.Definition.Name, title);
            for (int i = 0; i < actor.Definition.Abilities.Count; i++)
            {
                AbilityDefinition ability = actor.Definition.Abilities[i];
                string label = ability.Name + (ability.MpCost > 0 ? "  /  " + ability.MpCost + " MP" : string.Empty);
                if (selected == ability) label = "> " + label;
                bool previousEnabled = GUI.enabled;
                GUI.enabled = actor.Mp >= ability.MpCost;
                bool clicked = GUI.Button(new Rect(60, 525 + i * 31, 255, 28), label, button);
                GUI.enabled = previousEnabled;
                if (!clicked) continue;
                if (ability.Targets == TargetRule.Self)
                {
                    if (controller.TryPlayerAction(ability, actor)) selected = null;
                    return;
                }
                selected = ability;
            }

            GUI.Label(new Rect(350, 490, 295, 30), selected == null ? "Escolha uma acao" : "Escolha o alvo", title);
            if (selected != null)
            {
                var targets = battle.GetValidTargets(selected);
                if (selected.Targets == TargetRule.AllEnemies && targets.Count > 0)
                {
                    if (GUI.Button(new Rect(350, 530, 290, 38), "Todos os inimigos (" + targets.Count + ")", button))
                    {
                        if (controller.TryPlayerAction(selected, targets[0])) selected = null;
                        return;
                    }
                }
                else for (int i = 0; i < targets.Count; i++)
                {
                    CombatantState target = targets[i];
                    if (GUI.Button(new Rect(350, 530 + i * 46, 290, 38), target.Definition.Name, button))
                    {
                        if (controller.TryPlayerAction(selected, target)) selected = null;
                        return;
                    }
                }
                if (targets.Count == 0)
                    GUI.Label(new Rect(350, 533, 290, 105), NoTargetsMessage(selected), text);
                GUI.Label(new Rect(680, 525, 520, 90), selected.Description, text);
                if (GUI.Button(new Rect(680, 625, 180, 38), "Cancelar selecao", button)) selected = null;
            }
            else GUI.Label(new Rect(680, 533, 520, 95), "Escolha uma habilidade para ver seu efeito e os alvos. Defender age imediatamente. Corte amplo exige confirmar todos os inimigos.", text);

            if (!string.IsNullOrEmpty(controller.ErrorMessage))
                GUI.Label(new Rect(680, 604, 520, 20), controller.ErrorMessage, small);
        }

        private static string NoTargetsMessage(AbilityDefinition ability)
        {
            if (ability.Effect == AbilityEffect.Heal) return "Nenhum aliado vivo e ferido para curar.";
            if (ability.Effect == AbilityEffect.Cleanse) return "Nenhum aliado vivo com veneno para purificar.";
            if (ability.Effect == AbilityEffect.Revive) return "Nenhum aliado derrotado para reviver.";
            return "Nenhum alvo disponivel para esta habilidade.";
        }

        private void DrawStatus(Rect rect, CombatantState participant, bool active)
        {
            Panel(rect);
            if (active) RectBox(new Rect(rect.x, rect.y, 4, rect.height), new Color(0.95f, 0.8f, 0.4f));
            DrawFighter(new Rect(rect.x + 14, rect.y + 26, 42, 66), participant);
            GUI.Label(new Rect(rect.x + 70, rect.y + 8, rect.width - 80, 25), participant.Definition.Name, text);
            GUI.Label(new Rect(rect.x + 70, rect.y + 33, rect.width - 80, 20), participant.Definition.Role
                + (!participant.IsAlive ? " / DERROTADO" : string.Empty), small);
            DrawBar(new Rect(rect.x + 70, rect.y + 54, rect.width - 86, 18), participant.Hp,
                participant.Definition.MaxHp, new Color(0.26f, 0.66f, 0.44f), "HP");
            DrawBar(new Rect(rect.x + 70, rect.y + 75, rect.width - 86, 16), participant.Mp,
                participant.Definition.MaxMp, new Color(0.26f, 0.48f, 0.78f), "MP");
            string conditions = participant.IsDefending ? "DEFESA " : string.Empty;
            if (participant.PoisonTurns > 0) conditions += "VENENO " + participant.PoisonTurns + " ";
            if (participant.TauntTurns > 0) conditions += "PROVOCADO " + participant.TauntTurns + " ";
            if (participant.ProtectedBy != null) conditions += "PROTEGIDO";
            GUI.Label(new Rect(rect.x + 70, rect.y + 93, rect.width - 80, 17), conditions, small);
        }

        private void DrawBar(Rect rect, int value, int maximum, Color color, string label)
        {
            RectBox(rect, new Color(0.06f, 0.10f, 0.12f));
            float fraction = maximum == 0 ? 0 : Mathf.Clamp01((float)value / maximum);
            RectBox(new Rect(rect.x, rect.y, rect.width * fraction, rect.height), color);
            GUI.Label(rect, label + " " + value + "/" + maximum, centered);
        }

        private void DrawFighter(Rect rect, CombatantState participant)
        {
            Color clothes = participant.Team == CombatTeam.Enemy ? new Color(0.69f, 0.29f, 0.27f)
                : participant.Id == "lia" ? new Color(0.8f, 0.86f, 0.74f)
                : participant.Id == "bram" ? new Color(0.37f, 0.58f, 0.7f) : new Color(0.85f, 0.68f, 0.28f);
            if (!participant.IsAlive) clothes = new Color(0.3f, 0.32f, 0.34f);
            RectBox(new Rect(rect.x, rect.y + rect.height * 0.30f, rect.width, rect.height * 0.45f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.25f, rect.y, rect.width * 0.5f, rect.height * 0.28f),
                participant.IsAlive ? new Color(0.9f, 0.77f, 0.59f) : new Color(0.39f, 0.4f, 0.41f));
            RectBox(new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
            RectBox(new Rect(rect.x + rect.width * 0.58f, rect.y + rect.height * 0.78f, rect.width * 0.3f, rect.height * 0.22f), clothes);
        }

        private void Panel(Rect rect) => RectBox(rect, new Color(0.085f, 0.14f, 0.18f));

        private void RectBox(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            centered = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            title.normal.textColor = new Color(0.96f, 0.81f, 0.48f);
            text.normal.textColor = Color.white;
            small.normal.textColor = new Color(0.78f, 0.84f, 0.84f);
            centered.normal.textColor = Color.white;
        }

        private void OnDestroy()
        {
            if (pixel != null) Destroy(pixel);
        }
    }
}
