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

Persistência, lojas, ícones de inventário e a recompensa do viajante já consultam os catálogos diretamente. A criação da jornada e a navegação ainda passam pelos componentes existentes de `Prototype`; a extração dos catálogos é a primeira etapa da reorganização.

## Persistência

Os IDs gravados nos saves permanecem estáveis. Exemplos: `aren`, `potion`, `dragon-armor` e os IDs das matérias e lojas. Nomes de exibição e localização dos arquivos não substituem esses identificadores.

`SaveCatalog` continua relacionando as chaves persistidas às definições compartilhadas. Esta reorganização não altera a versão nem o formato do save. O projeto continua usando a assembly de runtime existente, preservando também as referências de `Persistence/link.xml`.

## Apresentação e ferramentas

Os cenários ainda são montados pelo bootstrap existente em `PrototypeCity`. `ObliqueScenery` conserva a geometria e as utilidades usadas pelo jogo e pelas ferramentas de autoria. Os geradores sem chamadas de chama do altar, símbolos antigos, marcas de garras, oco, essência do dragão e ícones antigos foram retirados.

O trecho comentado de enquadramento esticado da entrada do rei permanece disponível para a decisão de acabamento. As alterações anteriores de música, finais, menus e combate não fazem parte desta etapa de extração dos catálogos.
