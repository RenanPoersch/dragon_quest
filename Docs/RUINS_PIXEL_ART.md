# Ruínas laterais desenhadas em pixel art

O mapa das ruínas é um corredor lateral com avanço à esquerda, câmera móvel, três degraus e interação com o altar da lenda. O movimento usa 1,15 vezes a velocidade normal. O altar e o livro foram reduzidos para 65% do tamanho anterior.

## Arte

Os arquivos `Assets/Resources/PixelArt/RuinsPaintedPanorama.png` e `RuinsPaintedColumns.png` foram criados com a ferramenta ImageGen integrada. O panorama contém mar, ilhas montanhosas e nuvens desenhados; o atlas contém um pilar completo e um quebrado, com contornos, fissuras, musgo e transparência.

A importação preserva os arquivos originais, sem compressão ou mipmaps. O código amostra a imagem por vizinho mais próximo em uma grade fixa, com filtro Point: panorama com 193 pixels de altura, coluna completa com 160 e coluna quebrada com 86. O atlas é recortado pelas áreas dos sprites; o canal alfa remove o fundo. Os pilares distantes recebem uma paleta mais escura. Os planos possuem deslocamentos diferentes, e os pilares em primeiro plano encobrem o herói por profundidade.

O altar mantém o ponto de interação e o progresso da missão. O livro de pedra conserva as inscrições; a chama aparece sobre ele após a solução. Saves da planta antiga reaparecem na entrada segura.

## Validação

`Tools/Test-Ruins.ps1` compila os scripts atuais e verifica direção, ritmo, degraus, recuperação de saves e retorno à ilha. Na Unity, `Tools > Dragon Quest > Validate Ruins` renderiza uma cena isolada e verifica os assets e o altar menor. As imagens são salvas em `Temp/RuinsPreview`, ou no diretório definido por `DQ_RUINS_OUTPUT`. A cena aberta do jogo é preservada.

## Prompts usados — ImageGen integrado

### Panorama

Use case: illustration-story. Asset type: an actual 2D hand-drawn PIXEL ART background panorama texture for a side-scrolling fantasy RPG, not a scene mockup. Generate a wide 3:1 panoramic bitmap, detailed 16-bit SNES-era pixel illustration with a native visual pixel grid equivalent to 512 x 192 pixels enlarged with crisp nearest-neighbor edges. A beautiful ancient Greek Mediterranean coast: layered jagged distant blue and lavender island mountains rise from a deep teal sea, tiny hand-drawn rocky outcrops, textured clusters of pixels describing vegetation on the islands and stone strata, white wave crests and sea reflections, warm pale clouds shaped with irregular pixel clusters in a blue late afternoon sky. Clear side elevation view. Horizon of the sea at about 65 percent down from the top; lower 35 percent is sea. Atmospheric depth using limited color palettes, hand-placed clustered pixels and sparse dithering, subtle warm sunlight, lovely deliberate irregular silhouettes. This is a DRAWING of an environment for a pixel-art game, flat 2D painted sprites. Avoid any 3D, polygon facets, geometric triangles, vector graphics, smooth gradient bands, glossy materials, photorealism, antialiasing, or blurred pixels. No foreground columns, no temple, no ground walkway, no altar, no characters, no UI, no text. Fill entire canvas with the illustrated panorama, no border.

### Pilares — fundo transparente

Use case: illustration-story. Asset type: transparent 2D PIXEL ART sprite sheet for an ancient Greek ruined temple in a side-scrolling RPG. Canvas portrait 2:3. Exactly TWO separate freestanding Doric Greek stone column sprites side by side, one in the LEFT HALF of the canvas and one in the RIGHT HALF. Each column fully fits inside its half with clear transparent gutters on all sides. Full tall column on left with chipped rectangular capital, hand-carved fluted shaft, eroded stone slabs and base. Broken half-height column on right with a jagged collapsed top, similar shaft and base, a few small attached stone fragments at its foot. Both bases aligned along the same bottom baseline with transparent padding below. Column sprites drawn as genuine flat 2D 16-bit RPG pixel art with visible square pixel clusters, crisp dark brown outlines, a small warm sandstone palette of ivory, ochre, muted tan and dark desaturated brown, irregular moss-green pixel patches near cracks and base. Rich ARTIST-DRAWN chips, stylized cracks, uneven fluting and sparse clustered highlights. Left pillar should resemble a sprite about 40px wide and 160px tall enlarged by nearest-neighbor, right about 40px wide and 90px tall. Orthographic front-facing side-game elevation, completely flat 2D silhouette and hand-painted sprite shading, same drawing language as a classic SNES RPG hero. NOT a 3D render, not smooth cylinders, no perspectival depth, no polygon geometry, no procedural straight stripes, no vector shape graphics, no antialiasing or blur. Real transparent background. No sky, no ground plane, no cast shadows, no checkerboard baked into image, no text, no labels, no characters, no UI. Only the two isolated pixel art column sprites.
