> Registro histórico anterior ao acabamento final. Consulte [a documentação vigente](../../Current/README.md).

# Organização do código

## Conteúdo do jogo

`Assets/Scripts/Content` contém as definições compartilhadas, em C# e sem dependências de cenas ou apresentação Unity:

- `CharacterCatalog`: identidade e atributos base de Aren, Lia e Bram; cria membros independentes para cada jornada.
- `AbilityCatalog`: habilidades básicas, magias e comandos compartilhados. Ataque e Defender têm uma única definição base; a infusão ainda resolve o comando elemental próprio da arma.
- `EquipmentCatalog`: equipamentos iniciais, compráveis e armadura dracônica.
- `ItemCatalog`: consumíveis.
- `MateriaCatalog`: matérias e suas relações com habilidades/modificadores.

As definições são compartilhadas. HP, MP, experiência, encaixes, quantidades, estoques e outros estados de uma jornada continuam nos objetos de sessão correspondentes.

`EncounterCatalog` monta patrulhas, encontros regionais e chefes com as mesmas regras e atributos. `PartySetup` monta a equipe inicial, e `ShopCatalog` cria estoques independentes para cada jornada. A restauração de saves usa esses pontos diretamente, sem depender das factories de versão inicial. As factories antigas encaminham para eles durante a migração.

`DialogueCatalog` reúne as falas fixas dos moradores, da patrulha e do confronto com o rei. Cada `DialogueDefinition` possui ID estável e entrega uma cópia de suas páginas; texto e progresso da conversa permanecem independentes.

`PartySetup` monta a equipe e o inventário inicial. `EncounterCatalog` monta os encontros. Seus campos antigos encaminham para as mesmas instâncias dos catálogos, conservando a compatibilidade dos consumidores durante a reorganização. Novos consumidores de definições devem usar diretamente `DragonQuest.Content`.

Persistência, lojas, ícones de inventário e a recompensa do viajante já consultam os catálogos diretamente. A navegação e os controllers de apresentação ainda usam os componentes existentes de `Game`.

## Inicialização, mundo e jornada

`GameEntryPoint` permanece como componente de entrada da cena, com o mesmo GUID. Ele delega a inicialização e a liberação dos recursos para `Bootstrap/GameBootstrap`.

- `GameBootstrap`: cria os sistemas, conecta os HUDs, áudio e navegação e mantém a propriedade dos recursos de cenário, física e ritmo de quadros.
- `Presentation/World/WorldBuilder`: monta os mapas, colisores, jogador e interações. Não instala uma jornada nem carrega saves.
- `WorldScene`: reúne os objetos criados que precisam ser conectados aos sistemas da partida.
- `WorldActions`: fornece as ações das interações e consultas à equipe/progresso atuais, sem prender os cenários ao primeiro estado da sessão.
- `JourneyInstaller`: usa o mesmo caminho de conexão de batalha, lojas e cheats para a jornada inicial e a carregada. Atualiza o contexto das interações e do diálogo, restaura os segredos e a recompensa do dragão e aplica o mapa/posição carregados.

Os mesmos objetos de segredos e progresso do dragão permanecem ligados aos cenários; seus dados são restaurados ao carregar a jornada. Equipe, ouro e serviço de lojas passam a apontar para o estado carregado. Os callbacks de baú, patrulha e chefes consultam esse estado atual.

## Mundo e exploração

`Exploration/Layouts` reúne as plantas dos mapas, áreas caminháveis, limites, pontos de chegada e posições das interações. `Exploration/WorldNavigation` mantém as rotas e a restauração de posições, incluindo os limites aceitos de saves antigos. Movimento e progresso da exploração continuam em `Exploration`.

`Presentation/World` reúne a apresentação do mundo:

- `Scenery`: cenários pintados, superfícies e composição da ilha, periferia, lojas e interiores.
- `Rendering`: criação de malhas, materiais, superfícies de pixel art e projeção oblíqua.
- `Characters`: sprites e apresentação dos personagens, incluindo profundidade no salão do rei.
- `Animations`: animações das runas da pedreira, chama das ruínas e dragão no covil.
- `CameraFollow` e `WorldLocationController`: enquadramento e aplicação visual de cada mapa durante viagens e carregamentos.

As sequências de descanso na hospedaria e audiência com o rei ficam em `Presentation/Cinematics/Exploration`. O cristal de salvamento fica em `Interactions/InnSavePoint`, pois controla a entrada do jogador na área que abre o menu de save.

Esta organização preserva os nomes de classes, namespaces, código e GUIDs dos scripts movidos. Os caminhos de Resources, IDs de mapas, regras de colisão e dados persistidos permanecem os mesmos.

## Fluxo e entrada

`Gameplay/GameFlowState` consulta os estados atuais de batalha, inventário, diálogo, lojas, save, descanso, cheats e finais. Centraliza os bloqueios de movimento e viagens e identifica o sistema que pode receber entrada. Não mantém cópias dessas flags; a mesma coordenação continua válida ao carregar outra jornada.

O bootstrap liga essa coordenação ao movimento, viagens e controllers. O bloqueio do movimento é consultado tanto na leitura da entrada quanto no passo de física. `PlayerInteraction` continua tratando o diálogo antes de liberar a exploração; o consumo do frame permanece em `GameInput`, impedindo que a mesma confirmação ative duas ações.

Conversas abertas por `DialogueDefinition` carregam seu ID até a conclusão ou cancelamento. Música e audiência reconhecem o confronto com o rei por `DialogueCatalog.KingConfrontation.Id`, sem depender do nome apresentado. A sequência de entrada conserva o fade, o preto efetivamente desenhado, a confirmação da música e a batalha pronta antes da revelação.

## Título, abertura e salvamento

`Presentation/Menus/FrontEndPresentation` controla a abertura, os fades do menu, a notificação de conquistas e a viagem inicial. O novo jogo espera o preto ser efetivamente desenhado antes de criar a apresentação da viagem. Essa classe não acessa arquivos de save.

`TitleMenuPresentation` desenha a escama e os botões do título, com os estilos e posições originais. O controller continua cuidando da seleção e dos comandos, compartilhados com a navegação dos slots.

`SaveController` mantém a integração com a partida, a escolha dos slots, a confirmação de sobrescrita e as operações de salvar/carregar. Ele delega os tempos e o desenho da abertura às apresentações. As propriedades públicas de visibilidade e ganho de música permanecem disponíveis para os sistemas de áudio e bloqueio de entrada.

`Gameplay/Saving/SaveSlotSession` concentra a leitura das prévias, ocupação dos slots e acesso ao armazenamento, incluindo falhas de captura e gravação. `SaveMenuPresenter` possui os recursos gráficos das telas de título, slots e sobrescrita e consulta a seleção atual por callbacks, sem manter uma segunda seleção. O controller conserva a instalação da jornada e os comandos de salvar/carregar.

`InventoryHud` é o componente de entrada da cena; delega entrada e desenho a `InventoryMenuPresenter`, que possui a navegação e as sessões dos submenus. Os componentes preservam seus GUIDs. A navegação de teclado/controle continua sendo processada em Update, com o consumo do frame compartilhado com os demais sistemas.

As apresentações de inventário, equipamentos, itens, magia, matérias, status, loja e slots, junto dos seus ícones e retratos, ficam em `Presentation/Menus`. Os estilos e a navegação compartilhados ficam em `Presentation/Common`; a apresentação e os retratos de conversa ficam em `Presentation/Dialogue`.

`Presentation/Cinematics/Opening` reúne a abertura, a viagem de novo jogo e suas sequências. `Presentation/Cinematics/Endings` reúne as sequências e apresentações dos finais, a composição das pinturas e a transição de saída da batalha para essas cenas. Os componentes que orquestram a partida e os finais continuam nas localizações existentes de `Game` nesta etapa.

## Comandos e apresentação da batalha

`Presentation/Battle/BattleMenuPresenter` mantém o estado do menu: comandos, submenus de magia/invocação/itens, alternância para Defender, ação e alvo selecionados, rolagem e último inimigo preferido. Ele processa os controles e encaminha as ações aceitas ao controller da batalha. O desenho das listas usa os mesmos estilos e botões fornecidos pelo HUD.

`BattleArenaPresentation` desenha o cenário de batalha, lutadores, animações, indicadores de turno/alvo e números flutuantes. Mantém as poses de inimigos derrotados e os tempos visuais de impactos, conjuração e meteoro. Consulta a seleção do menu e encaminha os cliques nos lutadores ao presenter.

`BattleHudGraphics` reúne os estilos, a skin, o cursor e os elementos compartilhados da interface. Arena e painéis usam as mesmas instâncias. Ao encerrar a cena, libera a skin, o pixel de desenho e as texturas de ícones geradas em runtime; os assets carregados de Resources e a biblioteca de sprites da exploração conservam seus proprietários existentes.

`BattleHud` permanece como componente da cena: coordena menu e arena, desenha mensagens e status e confirma que a batalha foi desenhada antes da revelação. O presenter atualiza a seleção quando a batalha, o ator, a definição dos comandos ou a disponibilidade do LIMIT mudam.

`Combat/BattleCommandMenu` conserva o agrupamento dos comandos por origem da matéria. A execução e as regras de ações, custos e alvos permanecem em `BattleSession`; esta separação não altera essas regras.

`BattleActionPresentation` mantém o ator, a habilidade e os alvos apresentados, os tempos de início/impacto/fim e os estados anteriores necessários para mostrar cura, reviver e quebra de correntes no momento correto. Centraliza a escolha dos tempos de ataques, magias, invocações e ultimates, sem executar ações nem alterar HP/MP.

`BattleController` continua orquestrando encontros, turnos, recompensas e finais. Consulta a apresentação para aguardar os efeitos e aplicar o meteoro adiado no impacto. O evento `ActionPresented` continua sendo disparado pelo controller após iniciar o estado visual, preservando a integração com o áudio. As propriedades usadas pelo HUD permanecem disponíveis como consultas delegadas à apresentação.

`Gameplay/Battle/BattleResolution` é responsável pela captura do estado final da equipe, entrega de recompensas, armadura do dragão e conclusão dos resultados, notificando a conclusão uma única vez. O controller conserva os eventos públicos e a ordem de apresentação dos finais.

`Gameplay/Exploration/ExplorationSession` controla o avanço do veneno e dos encontros por caminhada, incluindo a distinção entre movimento e teleporte. `Presentation/World/PartyConditionPresentation` assina os eventos da equipe atual e desenha os números e tons das condições. Ao trocar de jornada ou encerrar a cena, libera a assinatura da equipe anterior.

## Persistência

Os IDs gravados nos saves permanecem estáveis. Exemplos: `aren`, `potion`, `dragon-armor` e os IDs das matérias e lojas. Nomes de exibição e localização dos arquivos não substituem esses identificadores.

`SaveCatalog` continua relacionando as chaves persistidas às definições compartilhadas. Esta reorganização não altera a versão nem o formato do save. O projeto continua usando a assembly de runtime existente, preservando também as referências de `Persistence/link.xml`.

## Apresentação e ferramentas

Os arquivos de batalha estão agrupados em `Presentation/Battle`: `Animations` contém animações e leitura de quadros; `Sprites` contém os renderizadores e folhas dos lutadores. Cores, ícones de status, brilho de LIMIT e apresentação do meteoro ficam junto aos presenters e recursos compartilhados da batalha.

As classes existentes conservam seus nomes, namespaces e arquivos `.meta` originais nesta etapa. A mudança física de pasta não altera os GUIDs das referências Unity nem os caminhos dos assets carregados de Resources.

O preview de ultimates fica em `Assets/Editor/Tools/UltimatePreviewRenderer.cs`, usado pela janela de autoria `UltimatePreviewWindow`. Não participa do código de runtime. O antigo componente `UltimatePreviewPlayer`, sem referências no projeto, foi retirado. A preparação de estados artificiais fica restrita à ferramenta de Editor, sem abrir os setters internos do modelo de combate.

Os cenários são montados por `Presentation/World/WorldBuilder`. `ObliqueScenery` conserva a geometria e as utilidades usadas pelo jogo e pelas ferramentas de autoria. Os geradores sem chamadas de chama do altar, símbolos antigos, marcas de garras, oco, essência do dragão e ícones antigos foram retirados.

Os modelos geométricos substituídos de personagens, dragão e finais ficam em `Assets/Editor/Tools/LegacySceneryGeometry.cs`, no namespace `DragonQuest.Authoring`. O exportador oblíquo inclui esse arquivo explicitamente; esses modelos não entram na assembly do jogador. `ObliqueScenery` conserva os construtores ainda usados no mundo atual. Os wrappers sem chamadas e o componente antigo `ObliqueCharacter`, sem referências de código ou GUID, foram removidos.

A inicialização dos finais recebe apenas mapas, jogador e câmera de acompanhamento. Os antigos parâmetros de câmera e geometria não eram utilizados e foram retirados junto de seus argumentos nos chamadores.

A cena de exemplo e o conteúdo de boas-vindas/tutorial do template foram retirados. `Packages/manifest.json` declara os pacotes usados e os módulos nativos necessários ao jogo; o lock preserva as versões mantidas e somente seu grafo alcançável. A revisão de assets, dependências e tamanho das fontes está em `ASSET_DEPENDENCY_AUDIT.md`. A cena principal, os recursos ativos e a importação das músicas foram preservados.

O trecho comentado de enquadramento esticado da entrada do rei permanece disponível para a decisão de acabamento. As alterações anteriores de música, finais, menus e combate não fazem parte destas etapas de reorganização.
