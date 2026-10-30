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

## Persistência

Os IDs gravados nos saves permanecem estáveis. Exemplos: `aren`, `potion`, `dragon-armor` e os IDs das matérias e lojas. Nomes de exibição e localização dos arquivos não substituem esses identificadores.

`SaveCatalog` continua relacionando as chaves persistidas às definições compartilhadas. Esta reorganização não altera a versão nem o formato do save. O projeto continua usando a assembly de runtime existente, preservando também as referências de `Persistence/link.xml`.

## Apresentação e ferramentas

Os cenários são montados por `Presentation/World/WorldBuilder`. `ObliqueScenery` conserva a geometria e as utilidades usadas pelo jogo e pelas ferramentas de autoria. Os geradores sem chamadas de chama do altar, símbolos antigos, marcas de garras, oco, essência do dragão e ícones antigos foram retirados.

O trecho comentado de enquadramento esticado da entrada do rei permanece disponível para a decisão de acabamento. As alterações anteriores de música, finais, menus e combate não fazem parte destas etapas de reorganização.
