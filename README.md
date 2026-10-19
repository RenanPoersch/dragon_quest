# Dragon Quest — demo acadêmica

RPG com apresentação 2.5D, exploração de uma ilha de fantasia e batalhas por turnos. Projeto desenvolvido em pequenas entregas, com validação e commits por etapa.

Direção visual atual: **sprites e texturas em pixel art inspirados no FF6 original, com cenários 3D e câmera inclinada**. A parte 10A adiciona equipe, moradores e um primeiro atlas de materiais; veja `Docs/PARTE_10A.md` para testar. Bosses, efeitos e cenas finais ainda incluem arte provisória.

A periferia atual é um **bairro compacto de fantasia industrial**, com chão pintado numa textura única, vielas, madeira/metal gastos, telhados de lona e iluminação pintada nas texturas das janelas e letreiros. Ao norte, uma única ponte de pedra leva à entrada automática de uma **fortaleza de pedra, com torres altas, ameias e portão em arco**. Veja `Docs/PERIFERIA_PIXEL_ART.md` para a planta, arte e testes.

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
5. Clique em **Play**: a tela inicial preta mostra a escama ancestral centralizada e as opções **New game** e **Continue**. **New game** apresenta uma travessia lateral com a nau e os três heróis, acompanhada da história à esquerda, antes de liberar o mapa; **Continue** abre os três slots, onde **Back** ou **Esc** retorna à tela inicial.
6. Clique dentro da janela **Game** e use **WASD ou as setas** para andar; **E** interage com objetos próximos, **I** abre o inventário e **F5** salva no mapa global ou na hospedaria.
7. Durante a jornada, a tecla **`** (backquote) abre e fecha o **Cheat Engine**. Use clique ou setas + Enter: **LIMIT infinito** mantém o Limit pronto para todos; **Gold infinito** permite comprar e descansar sem gastar; **Nível 99** eleva toda a equipe e atualiza os atributos, inclusive no combate atual. **Encontros aleatórios** permite desativar ou reativar as batalhas ao caminhar; batalhas por interação e chefes continuam disponíveis, e um combate em andamento continua normalmente. Ao reativar, a contagem de distância reinicia para evitar um encontro imediato. **Esc** fecha o menu. LIMIT, gold e encontros valem nesta sessão; ao desligar LIMIT e gold, a carga e o saldo originais retornam. O nível 99 permanece se você salvar a jornada normalmente. O menu bloqueia movimento, interações e ações de combate enquanto está aberto.

## Controle

Conecte um controle reconhecido pelo Input System da Unity; ele pode ser conectado ou reconectado durante a partida. Teclado e mouse continuam disponíveis. No Windows, a compatibilidade depende do dispositivo e do driver: controles Xbox/XInput e modelos PlayStation reconhecidos como Gamepad usam o mesmo mapeamento por posição dos botões.

| Ação | Controle |
| --- | --- |
| Andar / navegar | Analógico esquerdo ou direcional |
| Interagir / avançar diálogo / confirmar / atacar | Botão inferior: A no Xbox, X no PlayStation |
| Voltar / fechar | Botão direito: B no Xbox, Círculo no PlayStation |
| Inventário | Botão superior: Y no Xbox, Triângulo no PlayStation; ou Start/Options |
| Salvar | Cristal ou hospedeira na hospedaria; F5 no teclado |
| Rolar texto de diálogo longo | Analógico direito |
| Abrir / fechar Cheat Engine | Select/Back/View/Share; também R3 |

Nos menus de inventário e loja, o direcional percorre os botões em sequência; cima/esquerda volta e baixo/direita avança. O botão selecionado recebe uma borda dourada, opções indisponíveis são puladas e listas rolam para manter a seleção visível. Confirme o produto, quantidade, compra/venda, equipamento ou encaixe pelo mesmo botão inferior. A tela inicial, Continue, escolha de slot, confirmação de sobrescrita, escolhas de diálogo, batalha e resultados também aceitam controle.

Na batalha, selecione a primeira linha (**Ataque/LIMIT**) e pressione **A ou seta para a esquerda** para trocá-la por **Defender**. **D ou seta para a direita**, nessa linha, restaura Ataque ou LIMIT conforme a carga. As demais linhas continuam disponíveis. Alternar a primeira linha não gasta o turno; confirme para defender. O direcional e o analógico do controle usam as mesmas direções, e Esc/botão direito também desfaz a troca.

Validação e roteiro de teste: [Docs/CONTROLLER_SUPPORT.md](Docs/CONTROLLER_SUPPORT.md).

## Parte 7A — ilha e cenários locais

A demo começa no **mapa global de uma pequena ilha arredondada e irregular, cercada pelo mar**. Chão e POIs usam pixel art na mesma direção da periferia, com ícones um pouco mais altos que o herói. Caminhe até a área de entrada dos pontos para mudar automaticamente para os cenários locais:

- Castelo no mapa global → **periferia do castelo**, bairro de 26 × 44 com viela central, lojas, moradores, missão e patrulha.
- Ponte de pedra ao norte e porta do castelo → **interior do castelo**, com salão, trono e confronto final da parte 8.
- Pedreira dos monólitos a noroeste → interior da antiga floresta oeste, conservado nesta etapa.
- Floresta a leste → cenário próprio com clareira, árvores e moradores.
- Ruínas antigas ao sudoeste da ilha → cenário próprio com colunas quebradas e o altar da lenda.
- Vulcão a nordeste, exatamente oposto às ruínas → bloqueado por uma barreira mágica circular e três selos. Depois de abri-la com E no acesso ao sul da barreira, o ícone leva ao caminho de lava e à arena do dragão.
- Hospedaria próxima à entrada sul → interior com camas; a equipe retorna para lá após perder para o dragão.

Entradas e saídas, incluindo lojas, funcionam ao caminhar pela área de passagem, sem confirmar com E. Chegadas ficam fora dessas áreas para evitar retorno imediato; diálogos, inventário, lojas abertas e batalhas suspendem a viagem. E continua sendo usado para conversar, examinar objetos e desfazer os selos da trilha. Ouro, inventário, equipamentos, missão, estoques e Limit usam a mesma sessão entre cenários.

A apresentação usa **câmera oblíqua e modelos com volume**, com sprites em pixel art para herói/moradores desde a parte 10A. A ilha tem costa elevada, mar texturizado e chão contínuo; os POIs usam sprites próprios e a cordilheira foi retirada. Casas da periferia têm paredes, telhados e janelas texturizados. No mapa mundial, a câmera acompanha o herói perto do centro da tela. Na periferia, ela também acompanha o herói mantendo o ângulo. Cenários pequenos mantêm seus enquadramentos fixos. WASD/setas acompanham as direções da tela.

Abra `Assets/Scenes/CidadePrototipo.unity` e siga `Docs/MAPA_GLOBAL_PIXEL_ART.md` para a ilha atual e `Docs/PERIFERIA_PIXEL_ART.md` para a periferia. `Docs/PARTE_7A.md` e `Docs/VISUAL_25D.md` registram a implantação anterior da ilha/apresentação. A batalha e os menus conservam sua interface atual.

## Parte 7B — três segredos e poema em latim

A camponesa do bairro residencial recita um poema original de três versos em latim e explica cada frase, sem identificar a resposta do altar. O poema e suas explicações podem ser ouvidos ao conversar com ela.

- Altar nas ruínas antigas: saia da periferia e caminhe até o ponto de interesse ao sudoeste da ilha. Dentro das ruínas, escolha o verso que corresponde à imagem de chama e reino destruído, com clique ou teclas 1/2/3.
- Pedreira a noroeste: interprete outro verso do mesmo poema para ativar os monólitos. Erros reiniciam sem punição; o cenário interno continua com a arte da floresta oeste nesta etapa.
- Floresta leste: marcas de garras apontam para uma árvore oca que guarda a Escama ancestral, um objeto de história protegido contra venda.

O contador fica oculto até concluir o primeiro segredo. Depois dos três, use **E** ao sul da barreira do vulcão, a nordeste: a escama desfaz os selos, a barreira se dissipa e o acesso à cratera fica livre. A escama permanece no inventário e o progresso persiste entre mapas da sessão.

Veja `Docs/PARTE_7B.md` para o poema, soluções e roteiro de teste em Play.

## Parte 7C — vulcão, dragão e armadura

O vulcão tem um caminho curto entre lava e rochas até o **Dragão corrompido**, resistente a Fogo e fraco a Gelo. Ele alterna garra, preparação e sopro em área, com um turno de aviso para escolher Defender. A vitória liberta sua essência e concede uma única **Armadura dracônica** e as 100 moedas desse inimigo.

A armadura dá **+16 de defesa, resistência a Fogo e quatro encaixes em dois pares ligados**. Qualquer personagem pode usá-la; o bônus do tank aumenta sua defesa base para +20. Ela é protegida contra venda e precisa ser equipada para revelar o espírito. Na derrota, a equipe retorna à hospedaria, conservando Limit e progresso; consumíveis gastos continuam gastos. Veja `Docs/PARTE_7C.md` para testar.

## Parte 8 — Rei Rubro, espírito e três finais

Use E no Rei Rubro e conclua a conversa para iniciar o confronto. Com a Armadura dracônica equipada, o espírito aparece desde o início e possui turno próprio, usando **Drenar vida, Correntes e Caos Magic em área**.

O Rei Rubro usa golpes físicos fortes sem elemento. Aos **20% de HP**, inicia uma conjuração de **cinco turnos próprios**: Protect no primeiro, carga crescente nos três seguintes e queda no quinto. Protect reduz o dano recebido pela metade, com efeito de escudo ao ser atingido; Shield aparece completo antes da pose de conjuração. Entre os turnos permanece apenas o sprite do rei; números flutuantes de 5 a 1 mostram o progresso, com fonte ligeiramente maior que o dano. O meteoro aparece somente no lançamento final. Um portador vivo da armadura absorve o meteoro inteiro; revivê-lo antes do lançamento restaura essa proteção.

- **Final bom:** derrotar o espírito com o Rei Rubro vivo. O rei justo é libertado, agradece à equipe e retoma seu governo.
- **Final ruim:** meteoro lançado sem portador vivo; a equipe e o Rei Rubro são aniquilados e o castelo é destruído.
- **Terceiro final:** matar o Rei Rubro primeiro; o espírito possui Aren. Matar os dois simultaneamente em área também leva a esse desfecho.

Os três finais usam animações automáticas com narrativa lateral e fade preto antes de voltar ao menu. O final bom apresenta o discurso do rei libertado e a prosperidade do povo em duas pinturas; veja `Docs/GOOD_ENDING.md`. Os outros desfechos estão em `Docs/FINAL_ONE.md` e `Docs/HERO_POSSESSED_ENDING.md`. Derrota comum volta à hospedaria e permite outra tentativa. Veja `Docs/PARTE_8.md` para as regras e o roteiro em Play. Arte, áudio, acabamento e executável ainda estão pendentes.

### Salvamento — parte 9

A tela inicial oferece New game, que apresenta a travessia de abertura antes da exploração (veja `Docs/NEW_GAME_VOYAGE.md`), e Continue, que abre a seleção dos três slots. O salvamento é somente manual: na inn, andar sobre o círculo do cristal abre a tela dos três slots, e a gravação exige escolher um destino. Depois de salvar ou cancelar, saia do círculo e volte para reabrir. Também é possível falar com a hospedeira ou usar F5 no mapa global ou na hospedaria. Novo jogo começa sem gravar e usa o primeiro slot livre; se os três estiverem ocupados, a escolha de destino e a confirmação de substituição aparecem ao salvar manualmente. Vitórias, segredos, baús, inventário e finais não salvam. Carregar recupera mapa, posição livre do último save, equipamentos, encaixes, Limit, estoques e progresso. Se uma posição da periferia antiga estiver fora ou bloqueada na planta compacta, a equipe reaparece na rua central. Combates em andamento não são gravados.

Os saves ficam em `Application.persistentDataPath/Saves`, fora do Git, com backup da revisão anterior válida. Jornadas concluídas reabrem seu final. Veja `Docs/PARTE_9.md` para os detalhes e a conferência em Play.

## Parte 6C — recompensas e missão de entrega

O ouro da vitória é a soma das recompensas dos inimigos derrotados: **Guarda do Rei Rubro: 25 moedas; Soldado: 10 moedas**. A patrulha atual rende 35, e outras formações terão totais conforme seus inimigos. O resultado mostra a composição, com pagamento único por batalha; derrota não paga. O baú concede **1.000 moedas** uma única vez por jornada. Um save que já registra esse baú aberto não recebe outra recompensa.

Na loja de itens, clique em **Aceitar entrega**. Leve o pacote ao vendedor de matérias, clique em **Entregar pacote** e volte à primeira loja para **Receber 30 moedas**. A conclusão repõe somente os consumíveis do vendedor, uma vez; equipamentos e matérias conservam os estoques restantes. O HUD mostra o objetivo atual.

As 50 moedas iniciais, a missão, o baú e uma vitória somam 1.115: permitem comprar Fogo (50) e All (90), além de outros suprimentos, sem repetir encontros. Monte o combo no inventário com I, em um par ligado.

Veja `Docs/PARTE_6C.md` para o roteiro de economia. As três entregas da parte 6 estão implementadas; os testes automáticos passaram, e os cliques/aparência aguardam confirmação no Editor. O roteiro do mapa atual está em `Docs/PARTE_7A.md`.

## Parte 6B — lojas e economia inicial

Há três lojas ao longo da viela: procure os letreiros ITENS, MATERIA e ARMAS. Entre automaticamente ao caminhar até a porta e use E no vendedor para abrir Comprar/Vender. Esc fecha o menu; a saída fica ao sul do interior.

- Comece com 50 moedas, 2 poções, 1 antídoto, equipamento simples e cinco matérias iniciais. Extras de teste foram retirados do jogo.
- Todos os produtos têm estoque limitado. Compras vão ao inventário e cada equipamento/matéria tem uma identidade própria.
- Venda por 50% do preço por unidade, arredondado para baixo. Objetos equipados precisam ser desequipados antes; objetos de história são protegidos.
- Quantidade, preço total, estoque e descrições aparecem antes da confirmação. Equipamentos mostram atributos base e encaixes ligados.
- Entrar/sair e abrir/fechar lojas conserva ouro e estoque. Objetos vendidos não criam estoque de recompra.

Veja `Docs/PARTE_6B.md` para as regras de comércio, `Docs/PLANO_LOJAS.md` para os catálogos e `Docs/PARTE_6C.md` para validar a versão atual.

## Parte 6A — elementos e afinidades

Fogo e Gelo estão disponíveis como matérias verdes, junto de Raio. As três magias têm poder 22 e custo 6 MP. Fênix causa dano de Fogo. Fraquezas causam 150% do dano normal; resistências, 50%.

- Guarda do Rei Rubro: fraco a Raio, resistente a Fogo. Soldado: fraco a Fogo, resistente a Gelo.
- All mantém o elemento e calcula a afinidade de cada alvo. Infusão mantém o elemento e passa a usar ataque físico.
- As fichas dos inimigos mostram afinidades; o relatório da ação indica elemento e resultado. A prévia de comandos no inventário também mostra o elemento.
- Limit continua acumulando sobre o HP realmente perdido após defesa e resistência.

Veja `Docs/PARTE_6A.md` para as regras elementais e `Docs/PARTE_6B.md` para validar a versão atual, com compras e kit econômico.

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
| Verde | Magia | Fogo, Gelo, Raio (Thunder), Cura, Purificação, Vida |
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
- Baú parcialmente atrás da oficina das lanternas: 1.000 moedas uma única vez por jornada.
- Porta da loja de itens a noroeste: E entra no interior.
- Conversa com lojista e saída ao sul, usando E.
- Ouro e baú aberto preservados ao entrar/sair da loja.
- Movimento suspenso enquanto o diálogo está aberto; E, Espaço ou Enter avançam, e Esc fecha.

Teste a entrega com o roteiro em `Docs/PARTE_2.md`. A cena continua sendo `CidadePrototipo`. Compras foram adicionadas na parte 6 e salvamento em disco na parte 9.

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
- `Assets/Scripts/Exploration/CameraFollow.cs`: acompanha a posição interpolada do herói ou mantém o enquadramento fixo do ambiente.
- `Assets/Scripts/Prototype/PrototypeCity.cs`: mapa provisório, obstáculos e herói.
- `Assets/Scripts/Prototype/PrototypeCityLayout.cs`: dimensões, bairros, construções, ruas, rio e pontes do mapa ampliado.
- `Assets/Scripts/Prototype/PrototypeWorldLayout.cs`: ilha, costa, ícones globais, florestas e interior do castelo.
- `Assets/Scripts/Prototype/PrototypeTravel.cs`: mapas, conexões permitidas e posições de chegada/retorno.
- `Assets/Scripts/Prototype/PrototypeHud.cs`: apresenta somente conversas abertas, usando o painel azul com retrato, nome, fala e escolhas de `DialoguePresentation`.

Veja `Docs/PARTE_1.md` para o roteiro de verificação.

## Escopo planejado

- Cidade com exploração livre, lojas, NPCs e três segredos.
- Equipe com atacante, healer e tank.
- Combate por rodadas, comandos básicos e habilidades concedidas por matérias.
- Inventário, equipamentos e comandos concedidos por matérias equipadas em pares ligados.
- Quatro tipos de matéria: magia, suporte, summon e aprimoramento físico.
- Limit Break individual com carga mantida entre batalhas.
- Afinidades calculadas sobre os valores base de equipamentos ou habilidades.
- Dragão secreto liberado ao concluir os três segredos.
- Armadura especial como recompensa do dragão.
- Final ruim com castelo destruído pelo meteoro sem absorção.
- Caminho bom: usar a armadura e derrotar o espírito preservando o rei vivo.
- Terceiro final: matar o Rei Rubro primeiro entrega Aren ao espírito.

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
| 6A | Fogo, Gelo, fraquezas, resistências e combos elementais |
| 6B | Três lojas com estoques limitados, compras, vendas e kit econômico |
| 6C | Recompensas de batalha, primeira missão e reposição por progresso |
| 7A | Ilha global, periferia, florestas, interior do castelo e viagens |
| 7B | Poema em latim, três segredos e abertura da trilha do vulcão |
| 7C | Interior do vulcão, batalha do dragão e armadura especial |
| 8 | Rei Rubro, espírito, meteoro e três finais |
| 9 | Menu inicial, três slots e salvamento manual na hospedaria |
| 10A | Equipe/moradores em pixel art e materiais nos cenários 3D |
| 10B | Refinamento artístico, bosses, efeitos e interface |
| 10C | Áudio, balanceamento, validação final e executável |

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
