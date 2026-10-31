# Como o jogo funciona: inicialização, estado, interações e apresentação

Guia do código vigente, atualizado em 30/10/2026. Os contratos de conteúdo, saves e gameplay descritos aqui correspondem à estrutura atual.

## 1. A organização principal

O jogo parte de uma cena Unity e monta a maior parte do mundo por código. Os componentes MonoBehaviour recebem chamadas da Unity; as classes de regras, sessões e catálogos são chamadas por esses componentes.

| Responsabilidade | Local principal | Exemplo |
| --- | --- | --- |
| Montar e conectar a aplicação | Bootstrap | GameBootstrap, JourneyInstaller |
| Definir o conteúdo compartilhado | Content | CharacterCatalog, AbilityCatalog, DialogueCatalog, EncounterCatalog |
| Guardar e aplicar regras de combate | Combat | BattleSession, CombatantState, EnemyAI |
| Guardar a equipe e seus equipamentos | Equipment, Inventory, Progression | PartyProgress, EquipmentLoadout, ItemInventory, CharacterProgression |
| Coordenar fluxos da jornada | Gameplay | GameFlowState, BattleResolution, ExplorationSession, SaveSlotSession |
| Receber uma interação do mundo | Interactions | PlayerInteraction, NpcInteraction, MerchantInteraction |
| Converter a jornada para arquivos e reconstruí-la | Persistence | JourneySnapshot, SaveCatalog, SaveStore |
| Desenhar e animar | Presentation | WorldBuilder, DialoguePresentation, BattleArenaPresentation, presenters dos menus |
| Escolher e tocar sons | Audio | MusicCuePolicy, MusicController, BattleSoundController |


## 2. Da cena ao bootstrap

A cena habilitada nos Build Settings é Assets/Scenes/Game.unity. O componente de entrada é [GameEntryPoint](../../Assets/Scripts/Bootstrap/GameEntryPoint.cs#L11).

```text
Unity carrega a cena
  → GameEntryPoint.Awake()
  → new GameBootstrap(gameObject)
  → GameBootstrap.Initialize()
```

Antes da cena, GameDisplay configura a janela do executável desktop em 1920×1080. Callbacks de inicialização também reiniciam estados estáticos de input, menu e notificações. FramePacing aplica 60 FPS no Editor e sincronização vertical no executável.

[GameBootstrap.Initialize](../../Assets/Scripts/Bootstrap/GameBootstrap.cs#L39) executa a composição principal:

1. Cria o controlador dos sons de menu e a configuração de ritmo de quadros.
2. Cria o controlador de saves, a navegação, o renderizador de cenários e o material de física sem atrito.
3. Prepara WorldActions com as ações que os objetos do mundo poderão chamar.
4. Pede ao WorldBuilder para construir mapas, herói e interações.
5. Localiza a MainCamera e instala câmera, diálogo, locais, lojas, finais, descanso, batalha e cheats.
6. Cria GameFlowState e conecta o bloqueio de movimento do jogador.
7. Cria JourneyInstaller e instala uma jornada inicial.
8. Conecta os HUDs, interação do jogador, audiência do rei, save da pousada, músicas e efeitos de batalha.

O bootstrap escolhe quais objetos existem e entrega suas referências. As decisões de dano, de páginas de diálogo ou de compra ficam nos sistemas correspondentes.

## 3. O mundo é construído uma vez

[WorldBuilder.Build](../../Assets/Scripts/Presentation/World/WorldBuilder.cs#L33) cria os grupos de mapas, os GameObjects, os colisores, NPCs, passagens, cenários e o jogador. Ele devolve WorldScene, que reúne mapas, jogador, interagíveis, guardas e ponto de save.

Os layouts guardam posições, limites, áreas de entrada, caminhos e enquadramentos. A aparência dos NPCs é selecionada por NpcAppearance, separada do nome exibido. Os construtores de cenário usam essas definições para criar malhas ou instalar pinturas, sprites e elementos de profundidade. ObliqueScenery transforma dados geométricos em Mesh, Material e objetos renderizáveis.

Há onze mapas: mundo, periferia, trono, pedreira, floresta leste, ruínas, vulcão, pousada e três lojas. Eles são construídos no início. A navegação alterna os grupos ativos; entrar numa loja não carrega outra cena Unity.

WorldActions guarda callbacks como StartDragonBattle e RestAtInn, além de consultas como GetParty. O callback é uma função entregue para uso posterior. A consulta () => battle.Party obtém a equipe atual quando for executada, inclusive depois de carregar um save.

## 4. Instalar uma jornada é diferente de construir a cena

[JourneyInstaller](../../Assets/Scripts/Bootstrap/JourneyInstaller.cs#L50) conecta os sistemas ao progresso e à equipe em uso.

StartNewJourney cria ExplorationProgress com o ouro inicial. O controller de batalha cria a equipe por PartySetup.Create; a loja cria seu serviço por ShopCatalog.Create. PartySetup instancia Aren, Lia, Bram, equipamentos, matérias iniciais e consumíveis.

Restore recebe uma RestoredJourney já reconstruída. Substitui o progresso ativo, restaura os objetos compartilhados de segredos e dragão, reinstala equipe e lojas, reconecta o contexto das interações e aplica mapa/posição.

Os objetos AncientSecrets e DragonProgress usados pelos cenários são atualizados preservando essas referências. Os callbacks GetParty/GetProgress resolvem as instâncias atuais. Essa combinação evita que o mundo continue ligado à equipe anterior depois de um load.

## 5. Quem guarda cada estado

| Estado | Responsável principal |
| --- | --- |
| Membros, XP, HP/MP persistentes, equipamentos e inventário | PartyProgress e PartyMember |
| Atributos base e definições dos conteúdos | Catálogos de Content |
| Nível derivado do XP e limiares | CharacterProgression |
| Ouro, baús abertos, guardas e presente do viajante | ExplorationProgress |
| Poema, altar, monólitos, escama e passagem | AncientSecrets |
| Dragão derrotado e recompensa relacionada | DragonProgress |
| Mapa atual e conexões permitidas | WorldNavigation |
| Turno, participantes, ações e resultado do encontro | BattleSession |
| HP/MP e condições de um participante durante a luta | CombatantState |
| Páginas e escolhas da conversa atual | DialogueSession |
| Estoque e operações das lojas | ShopInventory e ShopService |
| Seleção e fases de cada menu | Sessões/presenters dos menus |
| Página do título/save e slot ativo | SaveController |

Uma definição de equipamento pode ser compartilhada. EquipmentLoadout representa uma unidade possuída, com ID e encaixes próprios. MateriaDefinition define um tipo; MateriaInstance representa uma unidade concreta. Isso permite salvar várias cópias sem confundi-las.

## 6. Como o jogo avança a cada quadro

A Unity chama Update para leitura de comandos e avanço dos controllers; FixedUpdate para movimento físico; LateUpdate para observações como caminhada, câmera, sprites e música; OnGUI para desenhar as interfaces atuais.

Esses grupos não são uma fila única de métodos de todo o projeto. Cada componente tem seu próprio ciclo. Há ordens explícitas em pontos importantes, como PlayerInteraction e CameraFollow.

GameInput lê teclado e gamepad sob demanda, centraliza botões e navegação com repetição e observa o foco. ConsumeGamepadFrame tem nome legado, mas consome também os comandos de teclado centralizados: impede que uma confirmação abra outra tela no mesmo frame.

[GameFlowState](../../Assets/Scripts/Gameplay/GameFlowState.cs#L8) consulta os componentes ativos por funções. Não guarda uma segunda cópia dos seus booleanos. Expõe bloqueios e uma prioridade de input: sem foco → nenhum; depois final, cheats, save, cinematic de descanso, batalha, loja, inventário, diálogo e exploração.

O movimento consulta esse bloqueio tanto em Update como em FixedUpdate. IsBusy não inclui diálogo, porque PlayerInteraction precisa continuar recebendo a confirmação da conversa. BlocksTravel e BlocksMovement incluem diálogo.

PlayerMovement aplica aceleração/desaceleração, colisões e restrições próprias do mapa. Teleport incrementa uma revisão para que outros sistemas distingam transporte de caminhada. PixelCharacter usa deslocamento/direção para escolher a pose; CameraFollow aplica os limites e enquadramento configurados pelo mapa.

## 7. Exemplo completo: falar com um NPC

Na construção do mundo, WorldBuilder cria o NPC, adiciona NpcInteraction e o inicializa com uma definição de DialogueCatalog. A lista desses componentes fica em WorldScene.Interactables.

```text
E ou confirmação do controle
  → GameInput.InteractPressed
  → PlayerInteraction.Update
  → FindNearest
  → NpcInteraction.Interact(context)
  → DialogueController.Open(definition, callback)
  → DialogueSession.OpenIdentified
```

[PlayerInteraction.FindNearest](../../Assets/Scripts/Interactions/PlayerInteraction.cs#L74) percorre os candidatos fornecidos pelo mundo, descarta os que não podem interagir e escolhe o mais próximo dentro do alcance padrão de 1,6 unidades. Esse método não exige que o jogador esteja olhando para o NPC nem faz uma busca global por nome.

InteractionContext entrega acesso a diálogo, progresso, navegação, batalha, loja e segredos. Cada interação usa apenas as partes de que precisa.

[DialogueController.Open](../../Assets/Scripts/Interactions/DialogueController.cs#L34) instala as páginas na DialogueSession e inicia DialogueTextReveal. A sessão guarda texto completo, página, opções e callbacks; o revelador controla a parte já visível.

```text
DialogueHud.OnGUI
  → DialoguePresentation.Draw
  → painel + retrato + nome + VisibleText + opções
```

DialoguePortraitCatalog resolve a imagem do falante. DialoguePresentation carrega/cacheia os retratos e desenha a conversa. Ela também permite selecionar opções pela interface.

Confirmar enquanto o texto é revelado mostra o restante da página. Confirmar novamente avança. Ao terminar a última página, DialogueSession fecha a conversa e chama a função de conclusão. Uma escolha fecha a conversa antes de executar seu callback, permitindo abrir outra conversa ou menu.

Cancelar fecha sem executar o callback de conclusão. Há comportamentos específicos por interação: a patrulha da ponte continua iniciando a batalha mesmo se seu aviso for fechado com Cancelar.

OpenNotice é um aviso breve, com texto revelado imediatamente. Nem todas as falas estão no catálogo: interações de segredos e mensagens dependentes do estado também abrem texto diretamente.

## 8. Todas as famílias de interação

| Interação | Caminho e efeito |
| --- | --- |
| NpcInteraction | Abre definição ou páginas; pode executar uma ação ao concluir. O viajante tenta entregar Phoenix uma única vez e abre um aviso. |
| ChestInteraction | Pede a ExplorationProgress para abrir o ID do baú e conceder ouro; atualiza a aparência e mostra a mensagem. |
| MerchantInteraction | Envia shopId a context.OpenShop, conectado a ShopController.Open. |
| InnInteraction | Abre Salvar/Passar a noite; os callbacks chamam save ou InnRestController. |
| InnSavePoint | Detecta entrada na área do cristal e solicita o menu de save quando permitido; anima seus próprios quadros. |
| AncientSecretInteraction | Liga falas/escolhas a AncientSecrets: aprender a lenda, resolver altar, ativar monólitos e coletar escama. |
| BlockedPassageInteraction | Consulta os requisitos em AncientSecrets; remove a colisão e anima a abertura da barreira quando a passagem é liberada. |
| BattleInteraction | Desafia ao falar ou entrar na região da ponte; inicia o encontro e observa EncounterCompleted para remover os guardas após vitória. |
| DragonInteraction | Acorda o dragão, abre a fala e inicia o chefe ao concluir; depois da vitória mostra a essência. Também expõe o prelúdio para a música. |
| TyrantInteraction | Abre KingConfrontation com StartCastleBattle como conclusão; aciona a audiência visual e revela o espírito conforme a armadura equipada. |

Passagens de mapa são tratadas pela navegação e suas áreas de entrada. Não precisam ser selecionadas por FindNearest.

## 9. Como as portas trocam de mapa

```text
WorldLocationController.Update
  → WorldNavigation.TryEnter(posição, bloqueado)
  → verifica área, conexão e requisitos
  → atualiza mapa atual e posição de chegada
  → WorldLocationController.ApplyMap
```

ApplyMap ativa apenas o grupo correspondente, configura movimento frontal/lateral/oblíquo, restrições do piso, velocidade, sombras e câmera; teleporta o herói e sincroniza os transforms da física. A música observa o mapa atual e acompanha a mudança.

Os saves recebem ajustes de posição seguros ao restaurar mapas cujas plantas mudaram. A entrada no vulcão depende da passagem liberada.

## 10. Como os encontros aleatórios surgem

```text
BattleController.LateUpdate
  → ExplorationSession.Advance
  → RandomEncounterRules.Advance
  → ExplorationStep.Encounter
  → BattleController.StartRandomBattle
  → EncounterCatalog.CreateRegionEncounter
  → StartEncounter
```

[RandomEncounterRules](../../Assets/Scripts/Combat/RandomEncounterRules.cs#L25) sorteia uma distância a percorrer: 18–30 unidades no mundo; 9–16 nas regiões locais elegíveis. Não há rolagem de probabilidade por segundo ou por frame.

| Mapa | Tipo do encontro |
| --- | --- |
| world | Bandidos piratas |
| west-forest | Golens da pedreira |
| east-forest | Lobos |
| Demais mapas | Sem encontro aleatório |

Conforme o jogador caminha, a distância efetiva é descontada do contador. Ao esgotar, o controller sorteia de 1 a 3 inimigos e cria a formação correspondente.

Menus, diálogos, batalhas, cinematics e falta de foco suspendem o avanço aplicável. Teleporte ou troca de mapa reiniciam a medição; deslocamentos acima de 2 unidades numa amostra são ignorados. O retorno do combate também reinicia o agendamento para evitar um encontro imediato na chegada.

ExplorationSession ainda avança o veneno persistente quando o fluxo permite. PartyProgress aplica seus ticks a cada 3 segundos; PartyConditionPresentation recebe PoisonDamage, mostra os números e atualiza o tom do herói. Se todos caírem, a exploração pode acionar a derrota.

## 11. Da formação à batalha

EncounterCatalog cria os combatentes e seleciona as regras apropriadas. PartyMember.CreateCombatant deriva atributos do nível, equipamentos e matérias, resolve comandos com MateriaResolver e restaura HP/MP/condições na nova CombatantState.

PartyProgress continua sendo o estado da jornada. BattleSession contém o estado próprio do encontro. Há recursos compartilhados deliberadamente: o inventário de consumíveis da batalha é o da equipe, e o medidor de LIMIT é passado ao combatente.

[StartEncounter](../../Assets/Scripts/Gameplay/Battle/BattleController.cs#L180) instala a sessão, prepara os resultados, limpa a apresentação anterior, registra o mapa, bloqueia movimento, emite EncounterStarted e inicia BattleEntryTransition.

Nos encontros comuns, a transição captura a câmera e aplica o shader de espiral. No rei, usa fade de 2 segundos para preto, espera de 3 segundos e fade de 2 segundos para a batalha. A revelação espera sinais de quadro preto, batalha desenhada e música preparada.

## 12. Comando, regra, animação e som

```text
BattleHud.Update
  → BattleMenuPresenter: comando e alvo
  → BattleController.TryPlayerAction
  → BattleSession.TryAct
  → BattleActionPresentation.Begin
  → evento ActionPresented
  → BattleSoundController agenda o efeito

BattleHud.OnGUI
  → BattleArenaPresentation + BattleHudGraphics + comandos
```

[BattleSession.TryAct](../../Assets/Scripts/Combat/BattleSession.cs#L220) valida turno, comando, recursos e alvo. Aplica dano/cura/condições, chama as regras especiais, verifica o resultado e avança turnos. O início de uma rodada organiza os vivos pela velocidade.

O cálculo de dano fica em BattleSession.ApplyDamage: ataque ou magia mais poder, curva de defesa, afinidade elemental, crítico, Defender, regras especiais e aplicação ao alvo. A curva de defesa usa o fator 205200 / (205200 + 290D + 79D²), que conserva 100%, 95% e 72% para defesa 0, 10 e 30 antes dos demais modificadores. CombatantState também aplica condições como escudos ao receber o dano.

Normalmente a regra já foi resolvida quando a animação começa. BattleActionPresentation mantém ator, habilidade, alvos, tempos e estados anteriores necessários para apresentar o impacto corretamente. BattleArenaPresentation lê mudanças de vitais para números, poses, cura, morte e efeitos.

Existe tratamento específico para o meteoro destrutivo: o controller adia a resolução até o instante do impacto para que o final não comece antes da carga e do clarão.

EnemyAI escolhe habilidades e alvos para inimigos, respeitando regras especiais. O controller aguarda o tempo visual e a pausa do inimigo antes de executar a ação. Turnos presos/controlados e fuga também passam pela sessão/controller.

BattleCommandMenu mantém as escolhas; BattleMenuPresenter recebe o input e desenha as opções; BattleArenaPresentation desenha o palco; BattleHudGraphics reúne os recursos gráficos compartilhados. Essas partes não concedem XP ou ouro.

## 13. Recompensas e continuidade

BattleResolution recebe a batalha encerrada e captura HP/MP/condições de volta na equipe. BattleRewards guarda recibos por chave fraca, permitindo liberar batalhas encerradas sem perder a proteção contra repetição de ouro ou XP; DragonProgress trata a recompensa do dragão; PostBattleResults conserva os dados necessários para a tela de resultado.

Somente participantes vivos no fim recebem XP. A tela de resultado pode atualizar/desenhar várias vezes sem conceder a recompensa novamente. Fuga não concede ouro/XP.

EncounterCompleted permite que outros sistemas reajam, como os guardas da ponte. Na continuação, ReturnToCity encerra a batalha e libera a exploração; apesar do nome, esse caminho não obriga todo encontro a voltar fisicamente para a cidade.

Uma derrota chama o fluxo de carregar jogo e recarrega a cena. O chefe final segue para o controlador de finais.

## 14. Equipamentos, matérias, inventário e lojas

InventoryMenuPresenter usa InventoryCommand e InventoryPage com um único catálogo de opções; recebe os comandos do inventário e coordena as telas de itens, matérias, equipamentos, status, magia e configuração. InventoryHud é o componente que encaminha Update/OnGUI para ele.

Os objetos de sessão executam as operações de cada menu: ItemMenuSession, MagicMenuSession, MateriaMenuSession e EquipmentMenuSession. Os presenters desenham e fazem a ponte com essas operações.

EquipmentLoadout guarda a unidade de equipamento e seus encaixes. MateriaResolver transforma as matérias em comandos, infusão elemental, alvos e modificadores de custo/poder. PartyMember deriva os atributos sem acumular novamente os mesmos bônus a cada consulta.

Na loja, MerchantInteraction chama ShopController.Open. ShopMenuSession guarda ações, filtro, produto e quantidade. ShopService valida estoque, ouro e propriedade, entrega/remove itens e atualiza o saldo. ShopMenuPresentation desenha a operação. ShopCatalog define a formação inicial das lojas; o estado restaurado usa as quantidades salvas.

## 15. Assets: onde entram e quando são carregados

Os arquivos importados ficam principalmente em Assets/Resources. Não há um AssetManager único: os carregadores são centralizados por família.

```csharp
Resources.Load<Texture2D>("PixelArt/ArenWalk")
```

Esse caminho aponta para Assets/Resources/PixelArt/ArenWalk.png, sem incluir a pasta Resources ou a extensão na chamada. O código solicita o asset importado pela Unity; não abre o PNG bruto manualmente.

| Família | Carregador | Momento principal |
| --- | --- | --- |
| Chão, atlas e shaders do cenário | ObliqueScenery e construtores dos cenários | Durante a construção inicial dos mapas |
| Sprites do mundo | PixelSpriteLibrary / PixelSpriteAtlas | Na primeira solicitação do atlas, inclusive durante o bootstrap |
| Sprites e efeitos de batalha | BattleArenaPresentation e leitores de atlas | Preparação/desenho da batalha e primeira necessidade de cada família |
| Fundo da batalha | BattleArenaPresentation | Ao observar uma nova sessão, usando BattleBackgrounds/<mapa> |
| Retratos do diálogo | DialoguePresentation / DialoguePortraitCatalog | Quando o retrato é necessário |
| Ícones do inventário | InventoryPixelArt / InventoryIconCatalog | Na primeira solicitação da chave |
| Fontes | GameTypography | Na primeira criação de estilo que precisa da fonte |
| Abertura/finais | Apresentações das sequências | Quando o fluxo correspondente é construído/preparado |
| Música | MusicController / MusicCuePolicy | Na primeira seleção de cada faixa; sono também é preparado na inicialização |
| Sons da batalha | BattleSoundController | Pré-carregados no Awake |
| Sons de menu | MenuSoundController | Pré-carregados no Awake |

PixelSpriteLibrary cacheia atlas e cria Sprite a partir de regiões da textura. PixelSpriteLayout calcula recortes/pivôs. PixelCharacter escolhe o sprite apropriado durante a caminhada.

BattleAnimationSheet pode analisar o canal alpha para delimitar quadros, base e tamanho. UltimateSpriteSheet usa os JSON *Frames com coordenadas e pivôs. SpiritBossSpriteSheet também lê referências opcionais de texturas nos JSON: por isso um asset pode estar em uso sem ter seu nome escrito diretamente no C#.

O shader PixelCharacter ajuda na profundidade dos sprites. Outros shaders tratam chão, fundos, efeitos e composição dos finais. Algumas imagens de cenário são pinturas aplicadas em superfícies, combinadas com objetos/camadas para profundidade e colisão.

Texturas de gradiente, malhas, materiais e RenderTextures também são criados pelo código. Esses recursos têm propriedade e descarte explícitos nos sistemas que os criam. Assets importados carregados de Resources são reutilizados; trocar o grupo de mapa ativo não executa uma política própria de descarregamento por mapa.

## 16. Música e efeitos

MusicCuePolicy decide a trilha a partir de abertura, título, viagem, descanso, mapa, batalha, resultado, derrota e final. MusicController observa o estado em LateUpdate, cacheia os clips e usa dois AudioSources para fades/transições.

No rei, KingConfrontation é reconhecido pelo ID do diálogo. A música do trono sofre fade de 2 segundos ao começar a conversa. Depois da fala, a transição fecha a tela; a música final só é liberada depois de um quadro totalmente preto. O começo silencioso da gravação é pulado em 0,66 segundo. A revelação espera a preparação da batalha e da música.

BattleSoundController assina ActionPresented. BattleSoundCatalog resolve a chave do som e seu deslocamento em relação ao impacto. O controller usa oito vozes e PlayScheduled quando necessário, para alinhar o som sem cortar todas as ações anteriores.

MenuSounds é uma fachada estática curta para MenuSoundController. O som de navegação só está habilitado nos contextos definidos pelo código; confirmação e cancelamento também evitam duplicação por múltiplas passagens de OnGUI.

## 17. Saves: capturar, gravar, ler e instalar

```text
SaveController
  → SaveSlotSession
  → JourneySnapshot.Capture
  → JourneyData
  → SaveStore.TryWrite
  → JSON em persistentDataPath/Saves/slot-N.json
```

Há três slots. O snapshot contém IDs de membros/equipamentos/matérias/itens, XP, vitais, estoque, ouro, posição, segredos, dragão, guardas, tempo e final. Não serializa GameObjects, texturas ou referências de cena.

SaveCatalog traduz IDs persistidos para definições dos catálogos. Renomear um texto de exibição é diferente de mudar esse ID.

[SaveStore.TryWrite](../../Assets/Scripts/Persistence/SaveStore.cs#L29) valida o snapshot reconstruindo uma jornada independente, serializa com DataContractJsonSerializer, grava um temporário e substitui o arquivo. Mantém backup do principal validado quando aplicável.

Na leitura, decodifica e valida versão/conteúdo. Pode recuperar o backup quando o principal está indisponível/corrompido; versões incompatíveis recebem tratamento próprio.

```text
Escolher Continuar
  → SaveSlotSession.TryRead
  → SaveStore.TryRead
  → JourneySnapshot.Restore
  → RestoredJourney
  → JourneyInstaller.Restore
  → sistemas reconectados + mapa/posição restaurados
```

SaveSlotSession publica uma coleção somente leitura de SaveSlotState, com número, ocupação, prévia e mensagem por slot. SaveMenuPresenter recebe essa coleção para desenhar as prévias. SaveController controla seleção, sobrescrita, slot ativo, relógio e disponibilidade de salvar. O salvamento manual é permitido pelo fluxo atual no mundo ou na pousada, fora dos estados bloqueados.

## 18. Abertura, pousada, rei e finais

FrontEndPresentation coordena a intro, o fade do título e a viagem de barco. SaveController observa seus passos para liberar a exploração. A jornada inicial já está montada por trás do menu; escolher Novo Jogo inicia a apresentação de entrada.

InnRestController cria as representações temporárias da equipe, conduz até as camas, fecha a tela e chama PartyProgress.RecoverAtInn sob o preto completo. A duração considera a faixa de sono; depois restaura o herói e libera o fluxo.

TyrantAudience apresenta companheiros durante a conversa com o rei sem alterar a composição salva da equipe.

CastleBattleRules implementa IBattleRules e IBattleDamageRules. Decide revelação do espírito, proteção do limiar de HP do rei, início do Protect, contagem do meteoro, absorção e resultado final. O limiar atual é 450 HP; antes do Protect, LimitDamage impede que um golpe pule essa etapa.

Sem portador vivo da armadura no lançamento do meteoro, o resultado é CastleDestroyed. A morte do rei leva a HeroPossessed; derrotar o espírito com o rei vivo leva a KingFreed.

EndingController recebe o resultado, faz a saída da batalha, oculta o mundo e instala a apresentação correspondente. Os objetos Sequence controlam o relógio e as fases; os objetos Presentation carregam/desenham a arte. EndingNarrative guarda os textos.

Ao terminar, AchievementService registra o final em um perfil separado, persistentDataPath/Achievements/endings.json, e recarrega a cena. A abertura pode então mostrar a notificação pendente. As conquistas não dependem de um slot individual da jornada.

## 19. Comunicação e centralização

Há quatro formas principais de comunicação:

1. Referências explícitas: o bootstrap passa player, dialogue, battle e demais dependências por Initialize/construtores.
2. Consultas Func<T>: GetParty e bloqueios consultam o estado atual sem guardar cópias antigas.
3. Ações/callbacks Action: uma conversa recebe o que deve acontecer quando terminar; uma interação recebe como abrir a loja ou iniciar uma batalha.
4. Eventos: EncounterStarted, ActionPresented, EncounterCompleted e PoisonDamage avisam interessados sobre acontecimentos.

A música combina consultas de estado e evento de entrada. Sons de combate seguem eventos. Os HUDs consultam os controllers/sessões. Não existe um barramento global único que processe todas as ações.

| O que está centralizado | Ponto principal |
| --- | --- |
| Composição e conexões iniciais | GameBootstrap |
| Instalação/restauração da jornada | JourneyInstaller |
| Consultas de bloqueio e prioridade de input | GameFlowState |
| Leitura dos controles | GameInput |
| Definições de conteúdo | Catálogos de Content |
| Regras e turnos do encontro | BattleSession e regras especiais |
| Encerramento/recompensas da batalha | BattleResolution e BattleRewards |
| Caminhada, veneno e agendamento de encontros | ExplorationSession e RandomEncounterRules |
| Navegação entre mapas | WorldNavigation / WorldLocationController |
| Páginas/escolhas e revelação do diálogo | DialogueSession / DialogueController |
| Formato e validação do save | JourneyData / JourneySnapshot / SaveCatalog |
| Arquivos dos slots | SaveStore |
| Seleção da música | MusicCuePolicy |
| Assets | Carregadores específicos de cada família |

## 20. Encerramento, ferramentas e localização de mudanças

GameEntryPoint.OnDestroy chama GameBootstrap.Dispose, que libera os recursos de cenário/física e restaura configurações de quadros. Apresentações e componentes liberam seus próprios materiais/texturas gerados, RenderTextures e assinaturas de eventos nos métodos de descarte.

Voltar ao título e o fluxo de derrota recarregam a cena. Os GameObjects da cena são recriados, e o bootstrap monta os sistemas novamente. Alguns catálogos/caches estáticos e o perfil de conquistas têm ciclo próprio; não representam um save automático da jornada.

Assets/Editor contém a configuração de importação de música e a prévia de ultimates. Tools contém utilitários externos de autoria de áudio, chão e quadros. Docs reúne a documentação vigente e as fontes dos recursos em uso. Esses diretórios não são o caminho que recebe o comando de gameplay no executável.

| Se quiser mudar… | Começar por… |
| --- | --- |
| Atributos iniciais | CharacterCatalog |
| Crescimento e níveis | PartyMember / CharacterProgression |
| Habilidades e itens | AbilityCatalog / ItemCatalog / MateriaCatalog / EquipmentCatalog |
| Formação/atributos de inimigos | EncounterCatalog |
| Frequência/região de encontros | RandomEncounterRules |
| Falas fixas | DialogueCatalog; depois o vínculo da interação no WorldBuilder |
| Regra de puzzle | AncientSecrets e sua interação; incluir o novo estado no save se persistente |
| Porta, posição e câmera | Layout correspondente, WorldNavigation e WorldLocationController |
| Dano e turnos | BattleSession; exceções do rei em CastleBattleRules |
| Tempo/visual de ação | BattleActionPresentation, animação e apresentação correspondente |
| Escolha de trilha e fades | MusicCuePolicy / MusicController / sequência visual |
| Dados persistidos | JourneyData, JourneySnapshot e SaveCatalog |

O caminho completo mais frequente é: input → controller/interação → sessão ou regra → mudança de estado → apresentação/áudio; quando precisa persistir, o snapshot converte esse estado em IDs e valores para o arquivo.
