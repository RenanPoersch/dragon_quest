# Organização do código

## Conteúdo do jogo

`Assets/Scripts/Content` contém as definições compartilhadas, em C# e sem dependências de cenas ou apresentação Unity:

- `CharacterCatalog`: identidade e atributos base de Aren, Lia e Bram; cria membros independentes para cada jornada.
- `AbilityCatalog`: habilidades básicas, magias e comandos compartilhados. Ataque e Defender têm uma única definição base; a infusão ainda resolve o comando elemental próprio da arma.
- `EquipmentCatalog`: equipamentos iniciais, compráveis e armadura dracônica.
- `ItemCatalog`: consumíveis.
- `MateriaCatalog`: matérias e suas relações com habilidades/modificadores.

As definições são compartilhadas. HP, MP, experiência, encaixes, quantidades, estoques e outros estados de uma jornada continuam nos objetos de sessão correspondentes.

`PrototypePartyFactory` monta a equipe e o inventário inicial. `PrototypeBattleFactory` monta os encontros. Seus campos antigos encaminham para as mesmas instâncias dos catálogos, conservando a compatibilidade dos consumidores durante a reorganização. Novos consumidores de definições devem usar diretamente `DragonQuest.Content`.

Persistência, lojas, ícones de inventário e a recompensa do viajante já consultam os catálogos diretamente. A navegação e os controllers de apresentação ainda usam os componentes existentes de `Prototype`.

## Inicialização, mundo e jornada

`PrototypeCity` permanece como componente de entrada da cena, com o mesmo GUID. Ele delega a inicialização e a liberação dos recursos para `Bootstrap/GameBootstrap`.

- `GameBootstrap`: cria os sistemas, conecta os HUDs, áudio e navegação e mantém a propriedade dos recursos de cenário, física e ritmo de quadros.
- `Presentation/World/WorldBuilder`: monta os mapas, colisores, jogador e interações. Não instala uma jornada nem carrega saves.
- `WorldScene`: reúne os objetos criados que precisam ser conectados aos sistemas da partida.
- `WorldActions`: fornece as ações das interações e consultas à equipe/progresso atuais, sem prender os cenários ao primeiro estado da sessão.
- `JourneyInstaller`: usa o mesmo caminho de conexão de batalha, lojas e cheats para a jornada inicial e a carregada. Atualiza o contexto das interações e do diálogo, restaura os segredos e a recompensa do dragão e aplica o mapa/posição carregados.

Os mesmos objetos de segredos e progresso do dragão permanecem ligados aos cenários; seus dados são restaurados ao carregar a jornada. Equipe, ouro e serviço de lojas passam a apontar para o estado carregado. Os callbacks de baú, patrulha e chefes consultam esse estado atual.

## Título, abertura e salvamento

`Presentation/Menus/FrontEndPresentation` controla a abertura, os fades do menu, a notificação de conquistas e a viagem inicial. O novo jogo espera o preto ser efetivamente desenhado antes de criar a apresentação da viagem. Essa classe não acessa arquivos de save.

`TitleMenuPresentation` desenha a escama e os botões do título, com os estilos e posições originais. O controller continua cuidando da seleção e dos comandos, compartilhados com a navegação dos slots.

`PrototypeSaveController` mantém a integração com a partida, a escolha dos slots, a confirmação de sobrescrita e as operações de salvar/carregar. Ele delega os tempos e o desenho da abertura às apresentações. As propriedades públicas de visibilidade e ganho de música permanecem disponíveis para os sistemas de áudio e bloqueio de entrada.

## Comandos e apresentação da batalha

`Presentation/Battle/BattleMenuPresenter` mantém o estado do menu: comandos, submenus de magia/invocação/itens, alternância para Defender, ação e alvo selecionados, rolagem e último inimigo preferido. Ele processa os controles e encaminha as ações aceitas ao controller da batalha. O desenho das listas usa os mesmos estilos e botões fornecidos pelo HUD.

`PrototypeBattleHud` continua como componente da cena e responsável pela arena, lutadores, animações, números flutuantes e status. Consulta o ator e a seleção do presenter para desenhar os indicadores e encaminha os cliques nos lutadores ao menu. O presenter atualiza a seleção quando a batalha, o ator, a definição dos comandos ou a disponibilidade do LIMIT mudam.

`Combat/BattleCommandMenu` conserva o agrupamento dos comandos por origem da matéria. A execução e as regras de ações, custos e alvos permanecem em `BattleSession`; esta separação não altera essas regras.

## Persistência

Os IDs gravados nos saves permanecem estáveis. Exemplos: `aren`, `potion`, `dragon-armor` e os IDs das matérias e lojas. Nomes de exibição e localização dos arquivos não substituem esses identificadores.

`SaveCatalog` continua relacionando as chaves persistidas às definições compartilhadas. Esta reorganização não altera a versão nem o formato do save. O projeto continua usando a assembly de runtime existente, preservando também as referências de `Persistence/link.xml`.

## Apresentação e ferramentas

Os cenários são montados por `Presentation/World/WorldBuilder`. `ObliqueScenery` conserva a geometria e as utilidades usadas pelo jogo e pelas ferramentas de autoria. Os geradores sem chamadas de chama do altar, símbolos antigos, marcas de garras, oco, essência do dragão e ícones antigos foram retirados.

O trecho comentado de enquadramento esticado da entrada do rei permanece disponível para a decisão de acabamento. As alterações anteriores de música, finais, menus e combate não fazem parte destas etapas de reorganização.
