# Parte 7A — ilha global e cenários locais

A exploração tem três escalas: **ilha global → periferia do castelo → interior do castelo ou lojas**. A ilha é pequena e cercada pelo mar. No mapa global, os ícones do castelo e das florestas medem aproximadamente 1,5 unidade de altura, um pouco acima do herói, cuja figura tem cerca de 1,1.

A cidade de 72 × 48 unidades agora representa a periferia do castelo. Conserva praça, três lojas, moradores, missão de entrega, baú, rio, pontes e patrulha. O ponto global do castelo leva à entrada sul dessa periferia; caminhar para norte e usar a porta do castelo muda para seu interior.

## Mapas e viagens

| Mapa | Entradas e saídas | Conteúdo |
| --- | --- | --- |
| Ilha global | Castelo e duas florestas | Costa, mar, trilhas, ícones pequenos e vulcão ao norte |
| Periferia do castelo | Ilha pelo portão sul; castelo pela porta ao norte; três lojas | Cidade ampliada, economia, moradores e combate de patrulha |
| Interior do castelo | Entrada/saída para a periferia | Salão, colunas, tapete, trono e diálogo do tirano |
| Floresta oeste | Entrada/saída para a ilha | Árvores, clareira, lenhador e pedra antiga |
| Floresta leste | Entrada/saída para a ilha | Árvores, clareira, viajante e pedra com escamas |
| Três lojas | Entrada/saída para a periferia, cada uma na sua porta | Itens, equipamentos e matérias com os estoques existentes |

Use **E** perto de um ícone ou de uma marca dourada para trocar de cenário. Os retornos globais ficam perto do ícone visitado; sair do castelo devolve à sua porta na periferia, e sair de uma loja devolve àquela loja.

Uma passagem entre as rochas ao norte do castelo global pode ser examinada. Ela revela uma trilha até o vulcão, ainda bloqueada por três selos. A costa tem colisão e impede caminhar sobre o mar; a cordilheira e as rochas impedem contornar o bloqueio. O mapa visitável do vulcão entra na parte 7C.

Os oito ambientes são grupos independentes dentro da cena CidadePrototipo.unity; somente o atual fica ativo. O herói, câmera e controladores da sessão ficam fora desses grupos. Assim, uma viagem não recria inventário, ouro, missão, estoques, equipamento ou Limit. A câmera usa os limites/zoom definidos para cada mapa. A separação em arquivos de cena poderá ser feita quando os cenários tiverem arte final.

## Roteiro em Play

1. Saia de Play, aguarde a importação e abra Assets/Scenes/CidadePrototipo.unity na Unity 6000.6.5f1. Confira o Console e entre em Play. A demo deve começar na ilha, ao sul do pequeno castelo.
2. Clique em Game e caminhe com WASD/setas. Circule a ilha e tente ir ao mar: a costa deve impedir a saída. Compare a escala do herói com os ícones do castelo e das florestas.
3. Aproxime-se do pequeno castelo central e pressione E. O cenário deve mudar para a periferia, perto do portão sul. Siga pela ponte central até a praça e as lojas.
4. Entre e saia das três lojas. Confira a chegada diante da mesma porta, ouro/estoque preservados e câmera novamente seguindo a periferia inteira. Teste compras, inventário com I e a missão de entrega conforme PARTE_6C.md.
5. Vá ao pátio do castelo ao norte. O guarda ainda inicia a batalha da patrulha. Vença e retorne; o ouro deve somar Guarda 25 + Soldado 10 uma única vez.
6. Continue até a marca dourada diante da porta do castelo e use E. O cenário deve mudar para um salão com colunas, tapete e trono. Aproxime-se do tirano e converse; sua batalha ainda pertence à parte 8.
7. Use a saída ao sul do salão. O herói deve voltar à entrada do castelo na periferia. Vá ao portão sul da cidade e use E: volte à ilha perto do pequeno castelo, sem mudar ouro/inventário/Limit.
8. Vá à floresta oeste e use E no ícone. Explore a clareira, converse com o lenhador e use a saída sul. O retorno deve ficar junto à floresta oeste na ilha. Repita na floresta leste: seu retorno deve ficar junto ao outro ícone.
9. Procure a vegetação entre o castelo global e as montanhas do norte. Use E na passagem oculta, leia sobre os três selos e tente atravessar: as rochas devem bloquear. Circule a cordilheira para confirmar que o vulcão não pode ser alcançado pelo mar ou pelas laterais.
10. Repita ilha → periferia → loja → periferia → castelo → periferia → ilha. Confira câmera, posição de retorno, estado preservado, ausência de interações com objetos dos ambientes inativos e ausência de erros vermelhos no Console.

## Código e validação

- PrototypeWorldLayout.cs: costa da ilha, blocos do mapa global, ícones pequenos, trilha bloqueada, florestas e salão do castelo.
- PrototypeCityLayout.cs: bairros, edifícios e geometria da periferia ampliada.
- PrototypeTravel.cs: definições dos oito mapas e 14 rotas direcionais, com chegada/retorno específicos. Somente ligações declaradas são aceitas; ilha não permite entrar diretamente em lojas ou no interior do castelo.
- PrototypeCity.cs: monta os grupos do cenário, costa com EdgeCollider2D, marcas das entradas e interações. O herói é criado uma vez.
- PrototypeLocations.cs: ativa somente o destino, reposiciona o herói e ajusta câmera/cor de fundo sem recriar a sessão.
- PrototypeHud.cs: nomes e orientações conforme o ambiente ativo.
- BlockedPassageInteraction.cs: diálogo da trilha oculta, mantendo o bloqueio nesta fase.

Passaram **57 verificações geométricas da periferia** e **88 da ilha, interiores e viagens**. Incluem pontos de entrada, retornos, mar/baía bloqueados, trilha acessível para exame, vulcão inacessível, saídas das florestas, acesso ao trono, escala dos ícones, rotas inválidas, isolamento entre sessões e ciclos repetidos de viagem. Caminhos usam amostragem de 0,5 unidade e margem para o collider do herói.

Também passaram **1.518 verificações de combate/inventário/lojas/progresso**, **15 de exploração**, e a compilação dos scripts com as bibliotecas da Unity 6000.6.5f1. A contagem de combate inclui ações durante partidas completas. Execute Tools/Test-CityLayout.ps1, Tools/Test-UnityScripts.ps1, Tools/Test-Combat.ps1 e Tools/Test-ExplorationProgress.ps1 para reproduzir.

As formas são provisórias e geradas por código. Os testes não executam câmera, Rigidbody2D, cliques ou ativação de grupos na Unity; o roteiro em Play verifica esses pontos e a preservação real de estado nas viagens. Os três segredos e a abertura da trilha foram adicionados na parte 7B: veja `PARTE_7B.md`. Este roteiro da parte 7A considera a passagem antes de resolver os segredos. Interior do vulcão, dragão, tirano/espírito, finais, arte final e salvamento em disco ficam nas entregas seguintes.
