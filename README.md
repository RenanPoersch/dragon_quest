# Dragon Quest — demo acadêmica

RPG com apresentação 2.5D, exploração de uma ilha de fantasia e batalhas por turnos. Projeto desenvolvido em pequenas entregas, com validação e commits por etapa.

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

## Parte 7A — ilha e cenários locais

A demo começa no **mapa global de uma pequena ilha cercada pelo mar**. Castelo e florestas aparecem como ícones um pouco mais altos que o herói. Caminhe até a área de entrada dos pontos para mudar automaticamente para os cenários locais:

- Castelo no mapa global → **periferia do castelo**, reaproveitando a cidade de 72 × 48, com lojas, moradores, rio, pontes, missão e patrulha.
- Porta ao norte da periferia → **interior do castelo**, com salão e trono. O tirano tem diálogo nesta entrega; a batalha e os finais entram na parte 8.
- Duas florestas no mapa global → cenários próprios com clareira, árvores e moradores.
- Ruínas antigas ao sudoeste da ilha → cenário próprio com colunas quebradas e o altar da lenda.
- Passagem escondida ao norte → trilha para o vulcão, bloqueada por rochas e três selos. O dragão entra em uma entrega posterior.

Entradas e saídas, incluindo lojas, funcionam ao caminhar pela área de passagem, sem confirmar com E. Chegadas ficam fora dessas áreas para evitar retorno imediato; diálogos, inventário, lojas abertas e batalhas suspendem a viagem. E continua sendo usado para conversar, examinar objetos e desfazer os selos da trilha. Ouro, inventário, equipamentos, missão, estoques e Limit usam a mesma sessão entre cenários.

A apresentação agora usa **câmera oblíqua e modelos com volume**, substituindo os retângulos vistos de cima. A ilha tem costa elevada, montanhas, mar e ícones pequenos de locais; casas têm paredes, telhados e janelas; personagens têm um modelo simples com passos animados. No mapa mundial, a câmera está aproximadamente duas vezes mais próxima no enquadramento e acompanha o herói perto do centro da tela. Na periferia, ela também acompanha o herói mantendo o ângulo. Florestas, ruínas, castelo e lojas usam enquadramentos fixos. WASD/setas acompanham as direções da tela.

Abra `Assets/Scenes/CidadePrototipo.unity` e siga `Docs/PARTE_7A.md` e `Docs/VISUAL_25D.md` para testar em Play. Os modelos ainda são provisórios, criados por código. A batalha e os menus conservam sua interface atual.

## Parte 7B — três segredos e poema em latim

A camponesa do bairro residencial recita um poema original de três versos em latim e explica cada frase, sem identificar a resposta do altar. Termine a conversa para consultar o poema e suas explicações com **P**.

- Altar nas ruínas antigas: saia da periferia e use E no ponto de interesse ao sudoeste da ilha. Dentro das ruínas, escolha o verso que corresponde à imagem de chama e reino destruído, com clique ou teclas 1/2/3.
- Floresta oeste: interprete outro verso do mesmo poema para ativar os monólitos. Erros reiniciam sem punição.
- Floresta leste: marcas de garras apontam para uma árvore oca que guarda a Escama ancestral, um objeto de história protegido contra venda.

O contador fica oculto até concluir o primeiro segredo. Depois dos três, use **E** na passagem ao norte da ilha: a escama desfaz os selos, as rochas se afastam e a trilha fica livre até o vulcão. A escama permanece no inventário e o progresso persiste entre mapas da sessão.

Veja `Docs/PARTE_7B.md` para o poema, soluções e roteiro de teste em Play. A próxima entrega é o interior do vulcão, o dragão e sua armadura; a cratera atual tem uma interação de ambientação.

## Parte 6C — recompensas e missão de entrega

O ouro da vitória é a soma das recompensas dos inimigos derrotados: **Guarda do tirano: 25 moedas; Soldado: 10 moedas**. A patrulha atual rende 35, e outras formações terão totais conforme seus inimigos. O resultado mostra a composição, com pagamento único por batalha; derrota não paga. O baú conserva sua recompensa única de 25 moedas.

Na loja de itens, clique em **Aceitar entrega**. Leve o pacote ao vendedor de matérias, clique em **Entregar pacote** e volte à primeira loja para **Receber 30 moedas**. A conclusão repõe somente os consumíveis do vendedor, uma vez; equipamentos e matérias conservam os estoques restantes. O HUD mostra o objetivo atual.

As 50 moedas iniciais, a missão, o baú e uma vitória somam 140: permitem comprar Fogo (50) e All (90) sem repetir encontros. Monte o combo no inventário com I, em um par ligado.

Veja `Docs/PARTE_6C.md` para o roteiro de economia. As três entregas da parte 6 estão implementadas; os testes automáticos passaram, e os cliques/aparência aguardam confirmação no Editor. O roteiro do mapa atual está em `Docs/PARTE_7A.md`.

## Parte 6B — lojas e economia inicial

Há três lojas: itens a noroeste, matérias a nordeste e equipamentos a sudeste. Use E na porta e depois no vendedor para abrir Comprar/Vender. Esc fecha o menu; a saída fica ao sul do interior.

- Comece com 50 moedas, 2 poções, 1 antídoto, equipamento simples e cinco matérias iniciais. Extras de teste foram retirados do jogo.
- Todos os produtos têm estoque limitado. Compras vão ao inventário e cada equipamento/matéria tem uma identidade própria.
- Venda por 50% do preço por unidade, arredondado para baixo. Objetos equipados precisam ser desequipados antes; objetos de história são protegidos.
- Quantidade, preço total, estoque e descrições aparecem antes da confirmação. Equipamentos mostram atributos base e encaixes ligados.
- Entrar/sair e abrir/fechar lojas conserva ouro e estoque. Objetos vendidos não criam estoque de recompra.

Veja `Docs/PARTE_6B.md` para as regras de comércio, `Docs/PLANO_LOJAS.md` para os catálogos e `Docs/PARTE_6C.md` para validar a versão atual.

## Parte 6A — elementos e afinidades

Fogo e Gelo estão disponíveis como matérias verdes, junto de Raio. As três magias têm poder 22 e custo 6 MP. Fênix causa dano de Fogo. Fraquezas causam 150% do dano normal; resistências, 50%.

- Guarda do tirano: fraco a Raio, resistente a Fogo. Soldado: fraco a Fogo, resistente a Gelo.
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
- `Assets/Scripts/Prototype/PrototypeCityLayout.cs`: dimensões, bairros, construções, ruas, rio e pontes do mapa ampliado.
- `Assets/Scripts/Prototype/PrototypeWorldLayout.cs`: ilha, costa, ícones globais, florestas e interior do castelo.
- `Assets/Scripts/Prototype/PrototypeTravel.cs`: mapas, conexões permitidas e posições de chegada/retorno.
- `Assets/Scripts/Prototype/PrototypeHud.cs`: controles e placas dos locais.

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
| 6A | Fogo, Gelo, fraquezas, resistências e combos elementais |
| 6B | Três lojas com estoques limitados, compras, vendas e kit econômico |
| 6C | Recompensas de batalha, primeira missão e reposição por progresso |
| 7A | Ilha global, periferia, florestas, interior do castelo e viagens |
| 7B | Poema em latim, três segredos e abertura da trilha do vulcão |
| 7C | Interior do vulcão, batalha do dragão e armadura especial |
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
