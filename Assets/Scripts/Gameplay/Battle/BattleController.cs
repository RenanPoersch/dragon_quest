using DragonQuest.Presentation.Menus;
using DragonQuest.Presentation.Cinematics.Endings;
using DragonQuest.Gameplay;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay.Exploration;
using DragonQuest.Presentation.World;
using DragonQuest.Content;
using DragonQuest.Presentation.Battle;
using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Inventory;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Input;

// Coordena o ciclo do combate na cena, da entrada ao resultado e ao retorno à exploração.
namespace DragonQuest.Gameplay.Battle
{
    public sealed class BattleController : MonoBehaviour
    {
        // Mantém a apresentação da ação em andamento separada das regras da batalha.
        private readonly BattleActionPresentation actionPresentation = new BattleActionPresentation();
        // Expõe ao HUD e ao áudio os dados visuais da ação atual.
        public BattleActionPresentation ActionPresentation => actionPresentation;

        // Referência ao jogador da cena. O controlador lê sua posição e libera ou bloqueia seu movimento.
        private PlayerMovement movement;
        // Apresenta na exploração as condições do grupo, como os efeitos visuais do veneno.
        private readonly PartyConditionPresentation conditions = new PartyConditionPresentation();
        // Acompanha a exploração que antecede os encontros aleatórios.
        private ExplorationSession exploration;
        // Aplica o resultado ao grupo, concede as recompensas e prepara a tela pós-batalha.
        private BattleResolution resolution;
        // Representa o inimigo cuja próxima ação foi preparada.
        private CombatantState pendingEnemy;
        // Instante em que o inimigo do turno pode agir após a pausa de apresentação.
        private float enemyActionTime;
        // Marca um meteoro destrutivo que será aplicado no impacto visual.
        private bool pendingMeteorImpact;
        // Tempo de preparação da fuga e duração da saída visual depois que ela é confirmada.
        // Os dois intervalos são medidos com o relógio sem escala de tempo do Unity.
        private const float EscapePreparationDuration = .75f, EscapeDepartureDuration = .45f;
        // Mensagem exibida quando o jogador tenta fugir de um chefe.
        private const string BossEscapeMessage = "Nao e possivel fugir de chefes.";
        // Instante em que a tentativa de fuga entrou em preparação.
        public float EscapeStartedAt { get; private set; }
        // Instante em que a sessão confirmou a fuga.
        public float EscapeCompletedAt { get; private set; }
        // Indica que a preparação ou a animação de saída está em andamento.
        public bool IsEscaping => IsActive && (Session.Outcome == BattleOutcome.None && Session.EscapePending || Session.Outcome == BattleOutcome.Escaped);
        // Progresso da saída após a fuga, entre 0 e 1, usado para encerrar a apresentação.
        public float EscapeDeparture => Session?.Outcome == BattleOutcome.Escaped
            ? Mathf.Clamp01((Time.unscaledTime - EscapeCompletedAt) / EscapeDepartureDuration) : 0;
        // Controlador de diálogos da cena, usado também na conversa após vencer o dragão.
        private DialogueController dialogue;
        // Consulta o estado atual do jogo para saber quem pode receber entrada.
        public GameFlowState Flow { get; private set; }
        public void SetFlow(GameFlowState flow) => Flow = flow;
        // A batalha recebe comandos quando é a dona atual da entrada no fluxo.
        public bool IsBattleInputBlocked => Flow?.CanReceiveInput(GameInputOwner.Battle) != true;
        // Referência ao menu de depuração anexado ao mesmo objeto da cena.
        private CheatMenu cheatMenu;
        // Consulta se o menu de depuração está bloqueando a entrada neste frame.
        public bool IsCheatMenuBlocking => cheatMenu != null && cheatMenu.BlocksInput;
        // Estado dos segredos compartilhado pelo bootstrap, consultado antes do encontro do dragão.
        private AncientSecrets secrets;
        // Ação configurada para mostrar o carregamento quando o grupo é derrotado.
        private System.Action showLoadGame;
        // Ação configurada para mostrar a cena do final alcançado no castelo.
        private System.Action<GameEnding> showEnding;
        // Tipo de encontro aleatório atual, usado no título e na apresentação do combate.
        public RandomEncounterType RandomEncounter { get; private set; }
        // Avisa os assinantes quando o resultado foi resolvido pela primeira vez.
        public event System.Action<BattleOutcome> EncounterCompleted;
        // Avisa que a sessão do encontro foi preparada. A música usa este evento.
        public event System.Action EncounterStarted;
        // Avisa que começou a apresentação de uma ação. Os efeitos sonoros usam este evento.
        public event System.Action ActionPresented;
        // Marca um fim de jornada, como derrota do grupo ou encerramento do castelo.
        public bool HasEnded {get;private set;}
        // Expõe as regras específicas do castelo quando a sessão atual usa esse tipo de combate.
        public CastleBattleRules CastleRules => Session?.Rules as CastleBattleRules;
        // Indica se a sessão atual é regida pelas regras do castelo.
        public bool IsCastleEncounter => CastleRules!=null;
        // Estado do dragão e da recompensa, usado para liberar e concluir o encontro.
        public DragonProgress Dragon { get; private set; }
        // Identifica se o encontro atual foi iniciado como a batalha do dragão.
        public bool IsDragonEncounter { get; private set; }
        // Escolhe o título conforme o encontro e a revelação do espírito no castelo.
        public string BattleTitle => IsCastleEncounter ? (CastleRules.IsSpiritRevealed?"REI RUBRO E ESPIRITO":"REI RUBRO")
            :IsDragonEncounter ? "DRAGAO CORROMPIDO" : RandomEncounterRules.Title(RandomEncounter);
        // Texto do botão que encerra a tela de resultado ou encaminha o jogador ao final.
        public string ReturnLabel => IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed?"Ver final"
            :"Continuar explorando";
        // Monta a descrição conforme o resultado da sessão e a mensagem de recompensa.
        public string OutcomeDescription => Session.Outcome==BattleOutcome.Completed?EndingNarrative.Title(CastleRules.Ending):Session.Outcome == BattleOutcome.Victory
            ? (IsDragonEncounter ? "A essencia do dragao foi libertada. " : "Os inimigos foram vencidos. ") + RewardMessage
            : Session.Outcome == BattleOutcome.Escaped ? "A equipe fugiu sem receber ouro ou experiencia."
            : "Sua equipe foi vencida...";

        // Função que consulta o mapa no momento da chamada. Usa o mapa global como padrão.
        private System.Func<string> currentLocation = () => WorldNavigation.World;
        // Mapa registrado no começo do encontro, usado para montar a apresentação da batalha.
        public string BattleLocationId { get; private set; } = WorldNavigation.World;
        // Recebe a função que consulta o mapa ativo durante a exploração.
        public void SetLocationProvider(System.Func<string> provider) => currentLocation = provider ?? (() => WorldNavigation.World);
        // Consulta se os encontros aleatórios estão habilitados na sessão de exploração.
        public bool AreRandomEncountersEnabled => exploration?.IsEnabled ?? true;
        // Muda a permissão de encontros aleatórios para o mapa atual.
        public void SetRandomEncountersEnabled(bool enabled) => exploration?.SetEnabled(enabled, currentLocation());
        // Componente responsável pela transição visual entre exploração e arena.
        public BattleEntryTransition EntryTransition { get; private set; }
        // Indica se a transição de entrada está em andamento.
        public bool IsEnteringBattle => EntryTransition != null && EntryTransition.IsPlaying;
        // Indica se o encontro ainda está ativo, incluindo a apresentação do resultado.
        public bool IsActive { get; private set; }
        // Indica se o menu de inventário do mundo está aberto.
        public bool IsMenuOpen { get; private set; }
        // Permite que o inventário cancele sua seleção interna antes de fechar o menu inteiro.
        public System.Func<bool> TryCancelInventorySelection { get; set; }
        // Consulta o fluxo para saber se a exploração está ocupada. Sem fluxo, trata o jogo como ocupado.
        public bool IsBusy => Flow?.IsBusy ?? true;
        // Grupo persistente da jornada. Pode vir de uma partida carregada.
        public PartyProgress Party { get; private set; }
        // Sessão do encontro atual, com participantes, turnos e resultado.
        public BattleSession Session { get; private set; }
        // Mensagem da tentativa de ação mais recente para exibição na interface.
        public string ErrorMessage { get; private set; }
        // Expõe o serviço que registra as recompensas já concedidas.
        public BattleRewards Rewards => resolution?.Rewards;
        // Texto de recompensa produzido ao resolver o resultado do encontro.
        public string RewardMessage => resolution?.RewardMessage;
        // Registro do estado do grupo antes e depois das recompensas.
        public PostBattleResults PostBattle { get; private set; }
        // Indica que a interface de resultados já está aberta.
        public bool IsPostBattleOpen { get; private set; }
        // Instante em que a tela de resultados começou a ser exibida.
        public float PostBattleStartedAt { get; private set; }
        // Estes dados descrevem a ação que está sendo exibida. O turno da sessão pode já ter avançado.
        public CombatantState PresentingActor => actionPresentation.PresentingActor;
        public AbilityDefinition PresentingAbility => actionPresentation.PresentingAbility;
        public bool IsPresentingConfusedAction => actionPresentation.IsPresentingConfusedAction;
        // Alvos usados no desenho dos efeitos, incluindo quem intercepta um golpe físico.
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingTargets => actionPresentation.PresentingTargets;
        // Membros do lado do autor que recebem a apresentação de um efeito em grupo.
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingPartyTargets => actionPresentation.PresentingPartyTargets;
        // Alvos que estavam derrotados antes da recuperação e entram na apresentação de reanimação.
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingRevivedTargets => actionPresentation.PresentingRevivedTargets;
        // Indica que a técnica de Lia está em exibição e ainda não chegou ao impacto visual.
        private bool AwaitingSalvationImpact => actionPresentation.AwaitingSalvationImpact(IsPresentingAction, ActionImpactAt);
        // Fornece ao HUD o estado do personagem antes do impacto da técnica de Lia.
        public BattleActionPresentation.SalvationCastState PendingSalvationState(CombatantState member)
            => actionPresentation.PendingSalvationState(member, IsPresentingAction, ActionImpactAt);
        // Mostra a conjuração ou a fuga durante a apresentação e, nos demais quadros, a mensagem da sessão.
        public string PresentedMessage => AwaitingSalvationImpact
            ? PresentingActor.Definition.Name + " esta conjurando " + PresentingAbility.Name + "..."
            : IsEscaping && !IsPresentingAction ? "A equipe esta fugindo..." : Session.LastMessage;
        // Sinais usados pela arena para escolher a animação da ação em andamento.
        public bool IsPresentingUltimate => actionPresentation.IsPresentingUltimate;
        public bool IsPresentingSummon => actionPresentation.IsPresentingSummon;
        public bool IsPresentingSpell => actionPresentation.IsPresentingSpell;
        public bool IsPresentingDragonAttack => actionPresentation.IsPresentingDragonAttack;
        // Instante de início e duração da ação exibida.
        public float ActionStartedAt => actionPresentation.ActionStartedAt;
        public float ActionDuration => actionPresentation.ActionDuration;
        // Reconhece o meteoro que destrói o castelo enquanto o impacto está pendente ou já definiu esse final.
        public bool IsDestructiveMeteor => IsActive
            && CastleMeteorPresentation.IsMeteor(PresentingActor?.Id, PresentingAbility)
            && (pendingMeteorImpact || CastleRules?.Ending == GameEnding.CastleDestroyed);
        // Fornece a intensidade do clarão do meteoro para a apresentação.
        public float MeteorWhiteOpacity => actionPresentation.MeteorWhiteOpacity(IsDestructiveMeteor);
        public float ActionEndsAt => actionPresentation.EndsAt(IsDestructiveMeteor);
        // Indica se a ação ainda está sendo exibida e precisa manter os turnos em espera.
        public bool IsPresentingAction => IsActive && actionPresentation.IsPlaying(IsDestructiveMeteor);
        // Instante em que a apresentação deve mostrar o impacto da ação.
        public float ActionImpactAt => actionPresentation.ImpactAt(IsDestructiveMeteor);
        // Índice do quadro de animação calculado para a ação atual.
        public int AttackPose => actionPresentation.AttackPose;

        // Liga o controlador aos sistemas da jornada e aos componentes visuais usados pelo combate.
        public void Initialize(PlayerMovement hero, DialogueController dialogueController, ExplorationProgress progress,
            DragonProgress dragonProgress = null,
            AncientSecrets ancientSecrets = null, System.Action defeatPresentation = null, System.Action<GameEnding> endingPresentation = null,
            PartyProgress restoredParty = null)
        {
            movement = hero; dialogue = dialogueController;
            cheatMenu = GetComponent<CheatMenu>();
            // Usa o grupo restaurado quando há um save. Uma nova jornada recebe o grupo inicial.
            Party = restoredParty ?? PartySetup.Create();
            conditions.Bind(hero, Party);
            // Reutiliza os componentes de apresentação já presentes na cena ou os adiciona ao mesmo objeto.
            var postBattleHud = GetComponent<PostBattleHud>();
            if (postBattleHud == null) postBattleHud = gameObject.AddComponent<PostBattleHud>();
            postBattleHud.Initialize(this);
            EntryTransition = GetComponent<BattleEntryTransition>();
            if (EntryTransition == null) EntryTransition = gameObject.AddComponent<BattleEntryTransition>();
            Dragon = dragonProgress ?? new DragonProgress(); secrets = ancientSecrets; showLoadGame = defeatPresentation;
            showEnding=endingPresentation;
            // Cria uma sessão de exploração com a fonte de sorteio do Unity para os encontros.
            exploration = new ExplorationSession(Party, currentLocation(), () => UnityEngine.Random.value);
            resolution = new BattleResolution(Party, progress, Dragon);
        }

        // Reaplica um final gravado e encaminha o jogador à sua apresentação.
        public void RestoreEnding(GameEnding ending)
        {
            if (ending == GameEnding.None) return;
            HasEnded = true; movement.SetMovementEnabled(false); showEnding?.Invoke(ending);
        }

        // Fecha o inventário e libera movimento se o fluxo voltou à exploração.
        public void CloseMenu()
        {
            IsMenuOpen = false;
            movement.SetMovementEnabled(Flow?.CanInteract == true);
        }

        // Inicia o encontro padrão quando a exploração permite começar uma batalha.
        public void StartBattle()
        {
            if (Flow?.CanStartBattle != true || !Party.HasLivingMembers) return;
            StartEncounter(EncounterCatalog.CreateEncounter(Party),false);
        }

        // Inicia o encontro aleatório do mapa atual quando o fluxo permite e há membros vivos.
        public bool StartRandomBattle()
        {
            if (!AreRandomEncountersEnabled || Flow?.CanStartBattle != true || !Party.HasLivingMembers) return false;
            // O mapa determina o tipo de inimigo. O encontro recebe um grupo sorteado de um a três.
            RandomEncounterType type = RandomEncounterRules.TypeAt(currentLocation());
            if (type == RandomEncounterType.None) return false;
            StartEncounter(EncounterCatalog.CreateRegionEncounter(Party, type, UnityEngine.Random.Range(1, 4)), false);
            RandomEncounter = type;
            return true;
        }

        // O Unity chama LateUpdate depois de Update em cada frame.
        // Na exploração, este método acompanha o deslocamento e trata derrota e encontros aleatórios.
        private void LateUpdate()
        {
            if (movement == null || dialogue == null) return;
            // A sessão recebe posição, versão do teleporte, intenção de movimento e bloqueio da exploração.
            var step = exploration.Advance(currentLocation(), movement.transform.position, movement.TeleportRevision,
                movement.HasMovementInput, Time.deltaTime, Flow?.BlocksMovement ?? true);
            conditions.RefreshTint();
            // A derrota do grupo tem prioridade. Um encontro sinalizado segue para StartRandomBattle.
            if (step == ExplorationStep.Defeat) FinishDefeat();
            else if (step == ExplorationStep.Encounter) StartRandomBattle();
        }
        private void OnGUI() => conditions.Draw(IsBusy || dialogue == null || dialogue.IsOpen);
        private void OnDestroy() => conditions.Dispose();

        // Exige a passagem aberta e o dragão ainda disponível antes de iniciar o encontro secreto.
        public void StartDragonBattle()
        {
            if (Flow?.CanStartBattle != true || !Party.HasLivingMembers || Dragon.IsDefeated || secrets?.PassageOpen != true) return;
            StartEncounter(EncounterCatalog.CreateDragonEncounter(Party),true);
        }

        // Prepara os dados e a transição de entrada usados por todos os tipos de encontro.
        private void StartEncounter(BattleSession encounter, bool dragonEncounter)
        {
            // Consome o comando do frame para ele não ser reutilizado ao entrar na batalha.
            GameInput.ConsumeFrame();
            Session = encounter; IsDragonEncounter = dragonEncounter;
            EscapeStartedAt = EscapeCompletedAt = 0;
            // Captura o estado inicial do grupo para comparar os valores na tela de resultados.
            PostBattle = new PostBattleResults(Party, encounter);
            IsPostBattleOpen = false; PostBattleStartedAt = 0;
            // Limpa dados do encontro anterior antes de registrar o mapa e iniciar a nova sessão.
            RandomEncounter = RandomEncounterType.None;
            actionPresentation.Reset();
            pendingMeteorImpact = false;
            BattleLocationId = currentLocation();
            resolution.Begin();
            IsActive = true;
            pendingEnemy = null;
            ErrorMessage = string.Empty;
            // Para o movimento antes de avisar os assinantes e iniciar a transição visual.
            movement.SetMovementEnabled(false);
            EncounterStarted?.Invoke();
            EntryTransition.Begin();
        }

        // Inicia o encontro do castelo com as regras específicas do Rei Rubro.
        public void StartCastleBattle()
        {
            if(Flow?.CanStartBattle!=true||!Party.HasLivingMembers) return;
            StartEncounter(EncounterCatalog.CreateCastle(Party),false);
        }

        // Recebe uma habilidade do jogador e delega sua execução à sessão de combate.
        public bool TryPlayerAction(AbilityDefinition ability, CombatantState target)
        {
            if (IsEnteringBattle || IsBattleInputBlocked) return false;
            if (!IsActive || IsPresentingAction || IsEscaping || GameInput.EscapeHeld || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            // Guarda quem está agindo e prepara os alvos que a animação precisa mostrar.
            CombatantState actor = Session.CurrentActor;
            target = Session.ResolveLivingEnemyTarget(ability, target);
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            // Uma ação em área usa todos os alvos válidos. Um golpe físico pode ser interceptado pelo protetor.
            if (ability != null && (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies))
                effectTargets.AddRange(Session.GetValidTargets(ability));
            else if (target != null)
                effectTargets.Add(ability != null && ability.Effect == AbilityEffect.PhysicalDamage
                    && !target.IsBound && target.ProtectedBy != null && target.ProtectedBy.IsAlive ? target.ProtectedBy : target);
            // Captura o estado visual anterior à ação para mostrar recuperação e correntes no momento certo.
            var revivedTargets = actionPresentation.CaptureRecovery(ability, effectTargets);
            actionPresentation.CaptureChains(Session.Combatants);
            // A sessão valida e executa a ação. O controlador apresenta o resultado aceito ou informa o erro.
            bool accepted = Session.TryAct(actor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                actionPresentation.CapturePartyTargets(ability, actor, Session.Combatants, revivedTargets);
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            return accepted;
        }

        // Aplica um consumível no turno do jogador e prepara sua apresentação quando a sessão aceita o uso.
        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (IsEnteringBattle || IsBattleInputBlocked) return false;
            if (!IsActive || IsPresentingAction || IsEscaping || GameInput.EscapeHeld || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            actionPresentation.CaptureChains(Session.Combatants);
            bool accepted = Session.TryUseItem(actor, item, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                // Cria uma ação visual com o nome do item para reaproveitar o fluxo de apresentação.
                PresentAction(actor, new AbilityDefinition("use-item", item.Name, "Usa um item.", 0, 0, AbilityEffect.Defend, TargetRule.Self),
                    new[] { target });
                AwardIfComplete();
            }
            return accepted;
        }

        // O HUD consulta estes métodos para desenhar as correntes e o momento da quebra.
        public bool ChainsVisible(CombatantState member)
            => actionPresentation.ChainsVisible(member, IsPresentingAction, ActionImpactAt);
        public float ChainBreakAge(CombatantState member)
            => actionPresentation.ChainBreakAge(member, IsPresentingAction, ActionImpactAt);
        public bool IsChainBreakTarget(CombatantState member)
            => actionPresentation.IsChainBreakTarget(member, IsPresentingAction);

        // Inicia a apresentação e avisa os assinantes após registrar a ação e seus alvos.
        private void PresentAction(CombatantState actor, AbilityDefinition ability,
            System.Collections.Generic.IReadOnlyList<CombatantState> targets)
        {
            actionPresentation.Begin(actor, ability, targets);
            ActionPresented?.Invoke();
        }

        // Aplica os efeitos do resultado e só emite EncounterCompleted na primeira resolução.
        private void AwardIfComplete()
        {
            if (resolution.Resolve(Session, IsDragonEncounter, PostBattle)) EncounterCompleted?.Invoke(Session.Outcome);
        }

        // Lê se o comando de fuga está pressionado e marca o instante em que a preparação começa.
        private void UpdateEscapeInput()
        {
            if (!IsActive || Session.Outcome != BattleOutcome.None) return;
            bool held = !IsCheatMenuBlocking && GameInput.EscapeHeld;
            bool wasPending = Session.EscapePending;
            if (Session.SetEscapeHeld(held, IsPresentingAction) && !wasPending)
            {
                EscapeStartedAt = Time.unscaledTime;
                ErrorMessage = string.Empty;
            }
            // Exibe a mensagem de chefe durante a tentativa e limpa o aviso ao sair dessa condição.
            if (held && !Session.CanEscape) ErrorMessage = BossEscapeMessage;
            else if (ErrorMessage == BossEscapeMessage) ErrorMessage = string.Empty;
        }

        // Avança a tentativa após a preparação e encerra a saída quando sua animação termina.
        private bool AdvanceEscape()
        {
            if (!IsActive) return false;
            if (Session.Outcome == BattleOutcome.Escaped)
            {
                if (EscapeDeparture >= 1) ReturnToCity();
                return true;
            }
            // A sessão só tenta a fuga se a preparação terminou e as regras permitem usar um turno para fugir.
            if (!Session.EscapePending || Time.unscaledTime - EscapeStartedAt < EscapePreparationDuration
                || !Session.TryTakeEscapeTurn(out _)) return false;
            if (Session.Outcome == BattleOutcome.Escaped) EscapeCompletedAt = Time.unscaledTime;
            AwardIfComplete();
            return true;
        }

        // Aplica o meteoro destrutivo quando a animação chega ao impacto.
        private void ResolveMeteorImpact()
        {
            if (!IsActive || !pendingMeteorImpact || Time.unscaledTime < ActionImpactAt) return;
            pendingMeteorImpact = false;
            if (Session.TryAct(PresentingActor, PresentingAbility, PresentingActor, out string message))
            { ErrorMessage = string.Empty; AwardIfComplete(); }
            else ErrorMessage = message;
        }

        // Encerra a jornada após a derrota e chama o fluxo de carregamento.
        private void FinishDefeat()
        {
            if (HasEnded) return;
            if (IsActive) AwardIfComplete();
            HasEnded = true; IsActive = false; IsMenuOpen = false; IsPostBattleOpen = false;
            pendingEnemy = null; exploration.ResetWalk();
            movement.SetMovementEnabled(false);
            GameInput.ConsumeFrame();
            showLoadGame?.Invoke();
        }

        // Entrega o final às cinemáticas e mantém a arena ativa durante o escurecimento de saída.
        private void FinishCastleBattle()
        {
            if (HasEnded)
            {
                if (GetComponent<EndingController>()?.IsFadingBattle != true) IsActive = false;
                return;
            }
            HasEnded = true; pendingEnemy = null;
            movement.SetMovementEnabled(false);
            showEnding?.Invoke(CastleRules.Ending);
            if (GetComponent<EndingController>()?.IsFadingBattle != true) IsActive = false;
        }

        // Update é chamado pelo Unity a cada frame.
        // Aqui ele avança fuga, apresentações, resultados, inventário e turnos de inimigos.
        private void Update()
        {
            // Se a jornada acabou, só acompanha a saída visual do castelo enquanto a batalha está ativa.
            if (HasEnded) { if (IsActive) FinishCastleBattle(); return; }
            // A transição de entrada suspende os turnos e descarta a ação inimiga agendada.
            if (IsEnteringBattle) { pendingEnemy = null; return; }
            UpdateEscapeInput();
            if (IsCheatMenuBlocking) { pendingEnemy = null; return; }
            // Verifica o impacto do meteoro antes de decidir se a apresentação deve ceder lugar à cinemática.
            ResolveMeteorImpact();
            if (IsDestructiveMeteor && CastleRules.Ending == GameEnding.CastleDestroyed
                && CastleMeteorPresentation.ShouldCutToCinematic(Time.unscaledTime - ActionStartedAt))
            { FinishCastleBattle(); return; }
            // Espera o fim da ação visual antes de avançar o turno ou abrir o resultado.
            if (IsPresentingAction) return;
            if (IsActive && Session.Outcome == BattleOutcome.Defeat)
            { FinishDefeat(); return; }
            // The final action finishes before the cinematic; no result confirmation interrupts it.
            if (IsActive && IsCastleEncounter && Session.Outcome == BattleOutcome.Completed)
            { FinishCastleBattle(); return; }
            // As regras da sessão resolvem os turnos forçados por correntes ou controle mental.
            if (IsActive && Session.CurrentActor?.IsBound == true)
            { Session.TryTakeBoundTurn(out _); AwardIfComplete(); return; }
            if (IsActive && Session.CurrentActor?.MindControlTurns > 0)
            {
                actionPresentation.CaptureChains(Session.Combatants);
                // Guarda o ator antes que a sessão avance o turno ao executar a ação forçada.
                CombatantState confused = Session.CurrentActor;
                if (Session.TryTakeMindControlTurn(System.Math.Min(UnityEngine.Random.value, .999999d), out var victim, out _))
                {
                    PresentAction(confused, BattleSession.MindControlAttack, new[] { victim });
                    ErrorMessage = string.Empty; AwardIfComplete();
                    return;
                }
            }
            // Uma atualização de fuga consome esta etapa do frame e tem prioridade sobre o pós-batalha.
            if (AdvanceEscape()) return;
            // Abre a tela de resultados uma vez e registra o instante em que a apresentação começou.
            if (IsActive && PostBattle?.IsComplete == true)
            {
                if (!IsPostBattleOpen) { IsPostBattleOpen = true; PostBattleStartedAt = Time.unscaledTime; }
                return;
            }
            // No fluxo permitido, trata a navegação do inventário e consome a entrada usada.
            if (Flow?.CanToggleInventory == true)
            {
                if (IsMenuOpen && GameInput.CancelPressed) { if (TryCancelInventorySelection?.Invoke() != true) CloseMenu(); GameInput.ConsumeFrame(); }
                else if (GameInput.InventoryPressed)
                {
                    if (IsMenuOpen) CloseMenu();
                    else { IsMenuOpen = true; movement.SetMovementEnabled(false); }
                    GameInput.ConsumeFrame();
                }
            }
            // A etapa abaixo só prepara uma ação quando o ator atual é um inimigo.
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Enemy)
            { pendingEnemy = null; return; }
            // Ao detectar um novo ator inimigo, começa a pausa de 0,9 segundo antes da ação.
            if (pendingEnemy != Session.CurrentActor)
            {
                pendingEnemy = Session.CurrentActor;
                enemyActionTime = Time.unscaledTime + 0.9f;
            }
            if (Time.unscaledTime < enemyActionTime) return;
            CombatantState actor = Session.CurrentActor;
            // A IA escolhe a habilidade e o alvo a partir dos combatentes da sessão.
            AbilityDefinition ability = EnemyAI.ChooseAbility(Session);
            CombatantState selectedTarget = EnemyAI.ChooseTarget(Session, ability, System.Math.Min(UnityEngine.Random.value, .999999d));
            // Monta a lista usada pela animação. Ela pode incluir todos os alvos ou quem interceptou o golpe.
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null)
            {
                var valid = Session.GetValidTargets(ability);
                if (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies) effectTargets.AddRange(valid);
                else if (selectedTarget != null)
                    effectTargets.Add(ability.Effect == AbilityEffect.PhysicalDamage
                        && !selectedTarget.IsBound && selectedTarget.ProtectedBy != null && selectedTarget.ProtectedBy.IsAlive ? selectedTarget.ProtectedBy : selectedTarget);
            }
            if (CastleMeteorPresentation.IsMeteor(actor.Id, ability))
            {
                // Todos os derrotados esperam o impacto; a absorcao aponta para o portador real.
                effectTargets.Clear();
                CombatantState bearer = CastleRules.LivingArmorBearer(Session);
                if (bearer != null) effectTargets.Add(bearer);
                else effectTargets.AddRange(Session.Combatants);
            }
            string message = "Inimigo sem acao valida.";
            actionPresentation.CaptureChains(Session.Combatants);
            if (CastleMeteorPresentation.IsMeteor(actor.Id, ability) && CastleRules.LivingArmorBearer(Session) == null)
            {
                // O HP e o resultado so mudam quando a carga termina, junto com o clarao.
                pendingMeteorImpact = true;
                PresentAction(actor, ability, effectTargets);
                pendingEnemy = null;
                return;
            }
            // As demais ações aplicam a regra da sessão antes de iniciar sua apresentação visual.
            bool accepted = ability != null && selectedTarget != null && Session.TryAct(actor, ability, selectedTarget, out message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            pendingEnemy = null;
        }

        // Encerra a batalha e devolve o controle à exploração quando o resultado permite.
        // O retorno normal mantém o mapa ativo. Derrota e conclusão do castelo seguem seus próprios fluxos.
        public void ReturnToCity()
        {
            if (IsEnteringBattle || IsCheatMenuBlocking) return;
            if (!IsActive || IsPresentingAction || Session.Outcome == BattleOutcome.None) return;
            if (Session.Outcome == BattleOutcome.Defeat) { FinishDefeat(); return; }
            // A vitória só permite continuar depois que a tela de resultados estiver pronta para ser fechada.
            if (PostBattleResults.IsVictory(Session) && (!IsPostBattleOpen || !PostBattle.CanContinue(Time.unscaledTime - PostBattleStartedAt))) return;
            IsPostBattleOpen = false;
            if(IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed)
            {
                FinishCastleBattle(); return;
            }
            AwardIfComplete();
            IsActive = false;
            // Reinicia a distância até o próximo encontro no mapa atual antes de liberar o movimento.
            exploration.Reset(currentLocation());
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
            // Após vencer o dragão, encadeia a fala da essência e o aviso da armadura.
            if (IsDragonEncounter && Session.Outcome == BattleOutcome.Victory)
            {
                dialogue.Open(DialogueCatalog.DragonEssence, () =>
                    {
                        MenuSounds.Play(MenuSound.Confirm);
                        dialogue.Open(DialogueCatalog.DraconicArmorReceived);
                    });
                movement.SetMovementEnabled(false);
            }
        }

    }
}
