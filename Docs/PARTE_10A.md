# Parte 10A — pixel art sobre cenários 3D

A referência artística desta etapa é o Final Fantasy VI original. A apresentação escolhida combina sprites em pixel art com os cenários 3D e a câmera inclinada já usados na demo.

## Primeiro conjunto de arte

| Arquivo em Assets/Resources/PixelArt | Uso |
| --- | --- |
| ArenWalk.png | Aren com cabelo loiro pontiagudo, quatro direções e poses de caminhada; exploração e batalha |
| ArenDiagonalWalk.png | Quatro diagonais do Aren, com repouso e dois passos; complemento usado na exploração |
| LiaWalk.png | Lia, quatro direções e poses de caminhada; batalha |
| BramWalk.png | Bram, quatro direções e poses de caminhada; batalha |
| NpcRoster.png | Doze sprites: moradores, comerciantes, hospedeira, lenhador, viajante, guarda e rei |
| TerrainAtlas.png | Doze materiais: grama, terra, pavimento, areia, madeira, parede, telhado, basalto, folhagem, água, lava e tecido |

As imagens são originais, geradas com a ferramenta integrada `image_gen`. Os prompts e a revisão de espaçamento estão em `PIXEL_ART_PROMPTS.md`. Não foram importados sprites de Final Fantasy. A folha do Bram e a dos moradores foram corrigidas antes da integração porque algumas poses encostavam nas divisões das células.

Os cinco PNGs iniciais têm 1448 × 1086 pixels, divididos em quatro colunas e três linhas de células de 362 × 362. Nas folhas da equipe, as colunas representam baixo/esquerda/direita/cima e as linhas representam parado/primeiro passo/segundo passo. A leitura do alpha encontra o corpo completo dentro de cada célula, adiciona uma margem e ancora a imagem pelos pés. Isso é feito em memória; os PNGs mantêm sua transparência original.

A folha complementar `ArenDiagonalWalk.png` tem 1447 × 1087 pixels. Suas colunas representam baixo-esquerda, baixo-direita, cima-esquerda e cima-direita, com as mesmas três linhas de animação. O recorte distribui os pixels excedentes entre as células, sem redimensionar a imagem. A exploração usa oito direções relativas à câmera e conserva a orientação diagonal ao parar; batalha e direções cardinais usam a folha original.

Aren troca direção e pose ao andar na ilha e nos interiores. NPCs usam poses estáticas. Lia e Bram aparecem com sprites próprios na batalha; as folhas já incluem suas direções/passos para usos futuros. Os trajes são a aparência inicial fixa desta etapa: trocar equipamento altera stats/encaixes, mas ainda não troca o desenho da armadura ou arma.

Os materiais cobrem terreno da ilha, mar, construções, florestas, ruínas, lojas, hospedaria e vulcão. Há sombreamento por face, sombras transparentes no chão e textura de grama com contraste reduzido. Objetos decorativos menores, dragão, espírito, invocações, arena de batalha e cenas finais ainda têm elementos provisórios e ficam para o próximo conjunto artístico.

## Integração

- `PixelSpriteLayout`: ordem das células, recortes/âncoras, direção relativa à câmera e ciclo de passos.
- `PixelSpriteLibrary`: carrega PNGs, cria sprites e compartilha materiais; o HUD reutiliza as mesmas imagens.
- `PixelCharacter`: mantém o sprite vertical, voltado para a direção horizontal da câmera, e escolhe a pose pela caminhada real, ignorando teleportes como passos.
- `PixelSurface`: seleciona materiais e projeta UVs no chão ou nas paredes usando a altura visual.
- `ObliqueMeshData`: guarda UVs, material e iluminação por face junto às malhas existentes.
- `ObliqueScenery`: cria os renderers, separa sombras transparentes e conserva os volumes da planta.
- `PixelCharacter.shader`, `PixelShadow.shader` e `ObliqueWorld.shader`: sprite com recorte de alpha/profundidade, sombra com blend e atlas de materiais.

Importadores usam Point, nenhuma compressão/mipmap e nenhuma conversão de tamanho NPOT. As folhas de sprites precisam de Read/Write habilitado para analisar seu alpha em runtime. O atlas de materiais usa 64 amostras por célula e repetição a cada duas unidades do cenário, evitando mistura com as células vizinhas.

O corpo físico continua no chão XY e a altura visual em −Z. As imagens ficam em planos verticais que acompanham a direção horizontal da câmera. Uma compensação de escala conserva o tamanho na tela, sem inclinar a cabeça para dentro dos balcões. Paredes e árvores continuam ocultando o personagem conforme a profundidade. O pé é elevado discretamente acima do piso visual, com altura própria para o estrado do rei. Colisões, rotas, posição salva e câmera seguem a planta existente. Saves da parte 9 usam a mesma versão e continuam carregáveis.

## Verificação

Passaram 186 verificações dos PNGs/importadores, 63 de recorte/direção/materiais, 2358 de malhas/câmera, 107 de salvamento, 58 de caminhos da periferia e 269 de viagens/interiores. Os scripts C# compilaram com as bibliotecas da Unity 6000.6.5f1. Prévias técnicas com texturas e sprites foram inspecionadas fora do Editor; elas não são capturas da Unity.

Para repetir a análise dos assets: `python Tools/Inspect-PixelArt.py`. Requer Pillow apenas para a ferramenta de desenvolvimento; o jogo usa os PNGs prontos. Os outros checks usam as ferramentas PowerShell já existentes.

## Roteiro em Play

1. Pare Play, aguarde a importação e abra `CidadePrototipo`. Comece Novo jogo ou Continue um slot existente. Confira o Console: ausência de erros de shader, textura não legível ou atlas cortado.
2. Caminhe com WASD/setas nas oito direções, incluindo W+A, W+D, S+A e S+D. Confira diagonais de frente e costas, alternância dos passos, retorno à pose parada mantendo a orientação e personagem próximo ao centro da câmera. Passe pelas entradas: a chegada deve manter o sprite inteiro e sem salto de animação.
3. Confira casas, grama, chão, mar, madeira e sombras. Passe atrás de paredes/árvores e à frente delas para verificar profundidade e transparência. Não devem aparecer retângulos pretos em torno dos personagens nem cores magenta nos materiais.
4. Converse com camponesa, moradores e vendedores. Confira figuras distintas, lojas e hospedaria. Teste o salvamento pela hospedeira e carregue o mesmo slot.
5. Enfrente a patrulha: Aren, Lia e Bram devem usar suas imagens próprias; derrotados ficam escurecidos. Equipamentos, matérias, itens e Limit devem funcionar como antes.
6. Continue uma jornada com passagem aberta e dragão derrotado. Confira rochas, armadura, revelação do espírito e os finais. O conjunto de sprites para dragão/espírito/finais será refinado na próxima etapa artística.

Importação real, compilação dos shaders pela GPU, sombras/profundidade do renderer 2D da Unity, cliques e aparência durante a caminhada ainda precisam dessa conferência em Play.

Após adicionar as diagonais, passaram 231 verificações dos seis PNGs/importadores, 161 de recorte/direção/materiais e 2358 de malhas/câmera. A compilação dos scripts passou. O atlas novo foi inspecionado visualmente; a troca efetiva dos sprites durante o movimento ainda precisa de Play.
