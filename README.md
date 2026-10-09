# Dragon Quest — demo acadêmica

RPG 2D com exploração de uma cidade de fantasia e batalhas por turnos. Projeto desenvolvido em pequenas entregas, com validação e commits por etapa.

## Ambiente

- Unity Editor **6000.6.5f1** (Unity 6.6).
- C#.
- Template oficial 2D da instalação do Editor, com Universal Render Pipeline.
- Plataforma inicial: Windows.

Use essa versão do Editor para evitar migrações involuntárias.

## Como abrir

1. Clone este repositório ou use a cópia local existente.
2. No Unity Hub, abra **Projects → Add → Add project from disk** e selecione a raiz deste repositório (a pasta que contém `Assets`, `Packages` e `ProjectSettings`).
3. Abra com o Editor **6000.6.5f1** e aguarde a importação dos pacotes e assets.
4. Abra `Assets/Scenes/SampleScene.unity`.
5. Clique em **Play**. Na etapa inicial, a cena é a base vazia do template: ainda não há personagem nem controles.

## Escopo planejado

- Cidade com exploração livre, lojas, NPCs e três segredos.
- Equipe com atacante, healer e tank.
- Combate por rodadas e habilidades próprias de cada papel.
- Inventário, equipamentos e seleção de habilidades aprendidas.
- Afinidades calculadas sobre os valores base de equipamentos ou habilidades.
- Dragão secreto liberado ao concluir os três segredos.
- Armadura especial como recompensa do dragão.
- Final ruim ao derrotar o tirano sem a armadura equipada.
- Caminho bom: usar a armadura, subjugar o tirano e derrotar o espírito que o controla.

## Entregas

| Parte | Resultado |
| --- | --- |
| 0 | Estrutura inicial, projeto 2D e configuração do Git |
| 1 | Movimento, câmera e colisões em mapa provisório |
| 2 | Interação com NPC, baú e entrada/saída de loja |
| 3 | Batalha por turnos com três aliados e um inimigo |
| 4 | Habilidades de ataque, cura e proteção |
| 5 | Inventário, equipamentos, habilidades e afinidades |
| 6 | Compra de itens, equipamentos e magias |
| 7 | Segredos, dragão e recompensa |
| 8 | Tirano, espírito e dois finais |
| 9 | Arte, áudio, balanceamento e executável |

Cada entrega deve ser validada antes do commit e do envio ao GitHub. Sprites e cenários serão criados por conjuntos; as primeiras mecânicas usarão elementos provisórios.

## Organização

- `Assets/Scenes`: cenas do jogo.
- `Assets/Scripts`: regras, integração com Unity e interface.
- `Assets/Art`: sprites, tiles e animações.
- `Assets/Audio`: música e efeitos.
- `Assets/Data`: definições de personagens, habilidades, itens e encontros.
- `Assets/Prefabs`: objetos reutilizáveis.
- `Assets/Settings`: configurações do template 2D.
- `Docs/ASSETS.md`: registro de autoria e licença dos assets.

## Controle de versão

Versionar `Assets` (incluindo `.meta`), `Packages` e `ProjectSettings`. Cache, arquivos locais do editor e builds são ignorados pelo `.gitignore`.

O repositório inclui o projeto editável. O executável será produzido na etapa de entrega final.
