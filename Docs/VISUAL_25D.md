# Apresentação 2.5D

O mundo passa a ser visto em ângulo, com modelos simples de poucos polígonos e cores por face. A direção escolhida usa o tamanho reduzido dos locais no mapa global e ambientes locais com volume. Os modelos são originais e provisórios; esta entrega define a perspectiva, antes do acabamento artístico.

## O que muda

- Ilha: superfície contínua, costa facetada elevada sobre o mar, faixa de espuma, montanhas e cratera do vulcão. O contorno visível também define o colisor do mar.
- Ícones: pequeno castelo com torres e telhados, e conjuntos de árvores. São ligeiramente mais altos que o personagem global.
- Periferia: casas com paredes, telhados, portas, janelas e chaminés; árvores, fonte, bancos, pontes e ruínas com volume.
- Interiores: paredes, colunas, trono, balcões e estantes. As paredes ao sul são baixas para permitir enxergar a entrada.
- Personagens: corpo, cabeça, braços, pernas, capa e espada simples. O herói muda de orientação e alterna os passos ao caminhar.
- Baú: tampa articulada ao abrir; conserva a recompensa única.
- Câmera: inclinação de 55 graus em relação à vista perpendicular ao chão e rotação lateral de 22 graus. Projeção ortográfica oblíqua, sem rotação controlada pelo jogador. Ilha e ambientes pequenos têm enquadramento fixo; a periferia acompanha o herói. O tamanho do enquadramento fixo considera a proporção da janela.
- Controles: WASD/setas movimentam o herói de acordo com os eixos da tela.

## Como o código funciona

`PrototypeCityLayout` e `PrototypeWorldLayout` continuam descrevendo a planta do mapa, com posições e áreas de colisão. `PrototypeCity` conecta a planta às portas, moradores, lojas e batalhas.

`ObliqueScenery` cria os modelos a partir da planta. `ObliqueMeshData` armazena vértices, triângulos e cores por face, permitindo verificar a geometria sem iniciar o Editor. Os modelos são malhas criadas em execução, não arquivos importados do Final Fantasy. O material usa `Assets/Resources/ObliqueWorld.shader`, compatível com o renderer 2D URP existente, com profundidade para ordenar volumes.

O chão físico continua em XY usando Rigidbody2D e Collider2D. A altura visual usa -Z. `ObliqueProjection` centraliza projeção, orientação dos controles e limites da câmera. A câmera inclinada mostra o relevo sem migrar os sistemas de colisão e interação para física 3D. `ObliqueCharacter` anima o modelo filho do objeto físico.

Ouro, inventário, estoques, matérias, equipamentos, missão e Limit continuam nos mesmos controladores. Batalhas e menus ainda usam a interface provisória anterior. A trilha do vulcão continua bloqueada; esta mudança visual não implementa os três segredos nem o dragão.

## Conferir no Editor

1. Saia de Play, aguarde a importação dos scripts e do shader e abra `Assets/Scenes/CidadePrototipo.unity`.
2. Inicie Play. Confirme a ilha vista em ângulo e o personagem sobre o chão; verifique se o Console apresenta erro de shader ou material rosa.
3. Caminhe com WASD e setas, incluindo diagonais. Confira a direção na tela, os passos e as colisões com mar e montanhas.
4. Use E no castelo global. Explore a periferia, contorne casas, árvores, fonte e bancos e atravesse as três pontes.
5. Entre nas três lojas, converse com os vendedores, abra o inventário e compre/venda um produto. Saia e confira ouro e estoques.
6. Abra o baú. Confira a tampa aberta, as 25 moedas e a ausência de uma segunda recompensa ao examiná-lo novamente.
7. Entre no castelo ao norte. Confira colunas, paredes, trono, diálogo do tirano e saída.
8. Volte à ilha, entre nas duas florestas e examine a trilha bloqueada.
9. Experimente a janela Game em 16:9 e 4:3. Confira os pontos de interesse e a legibilidade do HUD.
10. Inicie uma batalha na periferia e confirme inventário/matérias, recompensa e retorno à exploração.

## Validação desta entrega

`Tools/Test-Oblique.ps1` compila os scripts com as bibliotecas da Unity e verifica geometria, triangulação da ilha, dimensões dos ícones, orientação do movimento, limites e enquadramento da câmera. Também exporta malhas para prévias técnicas fora de `Assets`.

As prévias usam as mesmas malhas e a projeção do código, com um rasterizador externo. Não são capturas da Unity e não verificam importação do shader, GPU, HUD, animação ou física ao vivo. Esses pontos aguardam o roteiro de Play acima.

Também foram executados os testes existentes de caminhos/viagens, combate e progresso. Modelos, interface de batalha, texturas, iluminação e áudio continuam sujeitos a acabamento nas próximas entregas.
