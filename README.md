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
5. Clique em **Play**, clique dentro da janela **Game** e use **WASD ou as setas** para andar; **E** interage com objetos próximos e **I** abre o inventário.

## Parte 5B — equipamentos e consumíveis

O menu aberto com **I** tem abas **Matérias**, **Equipamentos** e **Itens**. Na aba Equipamentos, escolha o personagem, selecione uma arma ou armadura, confira os atributos e clique em Equipar selecionado. Também é possível remover arma e armadura.

- Trocar ou remover equipamento **desequipa todas as matérias dele**, devolvendo-as ao inventário. Os encaixes do novo equipamento começam vazios.
- Trocar equipamento entre dois personagens devolve as matérias dos dois equipamentos envolvidos. As matérias do outro equipamento de cada personagem permanecem.
- Espada de ferro, cajado rúnico, armadura de ferro e manto arcano permitem experimentar atributos e diferentes quantidades de pares.
- Na batalha, clique em **Itens**, escolha um consumível e confirme um aliado válido. Cada uso gasta uma unidade e o turno; tentativas inválidas ou canceladas não gastam recursos.
- Poção recupera 40 HP; éter recupera 15 MP; antídoto remove veneno; Pluma da Fênix revive com 30% do HP máximo.
- Estoque, equipamento, matérias e Limit permanecem entre batalhas da mesma sessão. O estoque de consumíveis não é reposto automaticamente.

Veja `Docs/PARTE_5B.md` para o roteiro em Play. Compras e reposição por lojas entram na parte 6; arte e salvamento em disco continuam para entregas posteriores.

## Parte 5A — matérias, combos e Limit Break

As habilidades especiais vêm das matérias equipadas. Qualquer personagem pode usar qualquer matéria; a classe concede apenas afinidade. Abra o menu com **I**, escolha um personagem, selecione uma matéria na lista e clique no encaixe de destino.

| Cor | Tipo | Exemplos |
| --- | --- | --- |
| Verde | Magia | Raio (Thunder), Cura, Purificação, Vida |
| Azul | Suporte | All: aplica a matéria ligada a todos os alvos válidos |
| Vermelha | Summon | Fênix: concede uma invocação de dano |
| Roxa | Aprimoramento físico | Força, Corte, Guardião e Infusão |

- Cada equipamento tem pares ligados; combinações funcionam dentro do mesmo par.
- **Thunder + All:** magia em todos os inimigos, 9 MP uma vez.
- **Fênix + All:** invocação em todos os inimigos, 18 MP uma vez.
- **Thunder ou Fênix + Infusão:** converte o comando de dano em golpe físico, usando ataque.
- **Cura + All:** cura todos os aliados vivos e feridos.
- Aren ganha +20% sobre o ataque base da arma; Lia, +25% sobre o poder base de magia, invocação e cura; Bram, +25% sobre a defesa base da armadura. Não existem restrições de matéria por classe.
- Limit Break acumula com o HP realmente perdido e pode ser usado a 100%. A barra de cada personagem permanece entre batalhas da mesma sessão, inclusive depois de derrota ou ressurreição.
- Ataque, defender e Limit Break são comandos comuns. Remover uma matéria retira seus comandos da próxima batalha.

A configuração inicial preserva os comandos usados na parte 4 por meio de matérias e já oferece Thunder + All em Aren. O inventário inclui matérias extras para experimentar os combos. A parte 5B adiciona troca de equipamentos e consumíveis; as compras ficam para a parte 6.

Veja `Docs/PARTE_5A.md` para as regras introduzidas nessa etapa e `Docs/PARTE_5B.md` para validar a versão atual. A arte de matérias e invocação permanece provisória.

## Parte 4 — habilidades e condições

A parte 4 estabeleceu o encontro com um guarda e um soldado, as habilidades iniciais e as condições junto às barras de HP/MP. Na versão atual, esses comandos são concedidos pelas matérias da parte 5A.

- **Aren:** corte amplo atinge todos os inimigos, além do ataque e golpe forte.
- **Lia:** curar, purificar veneno e reviver aliados derrotados.
- **Bram:** provocar um inimigo e proteger outro aliado de um golpe individual.
- O guarda alterna golpe venenoso, ataque e varredura em área; o soldado usa ataque básico.
- Confirmação única para ataque em área, descrições e seleção de alvos apropriados para cada habilidade.

Veja `Docs/PARTE_4.md` para o registro das regras desta etapa. Use `Docs/PARTE_5A.md` para validar a versão atual. A cidade mantém o tamanho atual.

## Parte 3 — base da batalha por turnos

A parte 3 estabeleceu a equipe com Aren (atacante), Lia (healer) e Bram (tank). Clique nas ações e nos alvos; os inimigos agem automaticamente no turno deles.

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
- Inventário, equipamentos e comandos concedidos por matérias equipadas em pares ligados.
- Quatro tipos de matéria: magia, suporte, summon e aprimoramento físico.
- Limit Break individual com carga mantida entre batalhas.
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
| 4 | Ataque em área, purificar, reviver, provocar e proteger |
| 5A | Matérias livres por classe, encaixes ligados, combos, afinidades e Limit Break |
| 5B | Troca de equipamentos e inventário de consumíveis |
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
