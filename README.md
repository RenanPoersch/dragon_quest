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
4. Abra `Assets/Scenes/CidadePrototipo.unity`.
5. Clique em **Play**, clique dentro da janela **Game** e use **WASD ou as setas** para andar; **E** interage com objetos próximos.

## Parte 3 — batalha por turnos

Ao norte da fonte, aproxime-se do guarda vermelho e pressione **E**. A equipe contém Aren (atacante), Lia (healer) e Bram (tank). Clique nas ações e nos alvos; o inimigo age automaticamente no turno dele.

- Ataque básico e defesa para todos, golpe forte para Aren e cura para Lia.
- Rodadas ordenadas por velocidade, com HP/MP individuais.
- Seleção de alvos vivos e válidos e validação do custo antes da ação.
- Vitória ou derrota seguida de retorno à cidade, preservando o progresso de exploração.
- Cada nova batalha inicia com HP/MP completos; não há recompensas ou consumo de itens nesta entrega.

Veja `Docs/PARTE_3.md` para regras e teste em Play. As regras independentes da Unity ficam em `Assets/Scripts/Combat`; os componentes visuais provisórios ficam em `Assets/Scripts/Prototype`.

## Parte 2 — interações

- Aldeão a sudoeste da fonte com diálogo em páginas.
- Baú a sudeste: 25 moedas uma única vez por sessão.
- Porta da loja de itens a noroeste: E entra no interior.
- Conversa com lojista e saída ao sul, usando E.
- Ouro e baú aberto preservados ao entrar/sair da loja.
- Movimento suspenso enquanto o diálogo está aberto; E, Espaço ou Enter avançam, e Esc fecha.

Teste a entrega com o roteiro em `Docs/PARTE_2.md`. A cena continua sendo `CidadePrototipo`; o estado reinicia ao sair de Play. Compras e salvamento serão adicionados em etapas posteriores.

## Parte 1 — exploração

A cidade provisória é criada pelo componente `PrototypeCity` ao iniciar Play. No modo de edição, a cena contém câmera, luz e o objeto que monta o protótipo. Durante Play, a hierarquia mostra o herói, ruas, prédios e obstáculos. Esses objetos temporários desaparecem ao sair de Play; isso é esperado nesta etapa.

- Movimento em oito direções, com velocidade diagonal limitada.
- Colisões com prédios, árvores, fonte, bancos e muralhas.
- Câmera seguindo o herói e limitada à área do mapa.
- Controles e nomes dos locais exibidos na tela.
- A parte 1 estabeleceu movimento e colisões; a parte 2 adiciona a entrada da loja de itens.

O cenário usa formas coloridas geradas pelo código, sem imagens externas. Sprites e tiles finais serão adicionados nas próximas entregas.

### Onde está o código

- `Assets/Scripts/Exploration/PlayerMovement.cs`: teclado, velocidade e movimento físico.
- `Assets/Scripts/Exploration/CameraFollow.cs`: acompanhamento e limites da câmera.
- `Assets/Scripts/Prototype/PrototypeCity.cs`: mapa provisório, obstáculos e herói.
- `Assets/Scripts/Prototype/PrototypeHud.cs`: controles e placas dos locais.

Veja `Docs/PARTE_1.md` para o roteiro de verificação.

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
