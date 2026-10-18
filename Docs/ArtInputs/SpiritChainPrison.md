# Correntes persistentes no alvo

Pedido: heroi acorrentado fica sem acoes ate as correntes receberem um acerto de qualquer intensidade.

Asset: `Assets/Resources/PixelArt/SpiritBoss/SpiritChainPrison.png`.
Gerado com ImageGen nativo; PNG RGBA original preservado, sem recolorir, redesenhar ou recortar pixels.
Referencia de material: `SpiritChains.png`. A corrente unica do conjurador permanece como aprovada.
Atlas 1512 x 1040, quatro colunas por duas linhas, celulas de 378 x 520.
Linha superior: quatro poses de brilho em correntes fechadas. Inferior: quatro poses de quebra.
Importacao Point, sem compressao ou mipmaps, alfa transparente e tamanho nativo.

O overlay fica sobre o torso do alvo ate o impacto libertador. Ataques fisicos, magias, efeitos sem dano,
acoes de recuperacao e itens quebram a prisao. O acerto e absorvido pelas correntes; nao fere o prisioneiro.
Ataques em area tambem atingem as prisoes aliadas, sem causar dano aos aliados.
Veneno e passagem de turno nao sao acertos. O campo de save BoundActions continua compativel (0/1), sem contagem regressiva.

## Prompt

Use case: game-asset sprite sheet.
Primary request: Create a NEW transparent pixel-art animation atlas for a chained hero status and chains breaking on impact in a dark fantasy JRPG final boss battle.
Input image: style/material reference only, especially the blue-black iron chains. Do NOT include the ghost or any character.
Composition: exactly EIGHT frames, evenly spaced FOUR columns by TWO rows, identical square cells, generous transparent gutters and borders. No labels, no grid lines, no background. All frames are overlays with transparent empty center so a hero sprite remains visible behind them. Center registered consistently.
Subject: two heavy blackened-steel chains crossing diagonally around the torso and wrists, a compact metal lock at waist, small wrist shackles. Roughly a tall oval silhouette, narrower than its height. Iron dark navy with crisp silver highlights and faint electric cyan rune gleams; leave head and feet and large spaces between links unobscured. No solid prison plate, no opaque disk, no cage walls.
Frames top row 1–4: SAME closed crossed chains and shackles in the same location, subtle four-frame cyan rune glint traveling along links; chains visibly restrain an invisible humanoid. Keep geometry stable, no expanding or disappearing.
Frames bottom row 5–8: locked chains cracking with white-blue impact spark, then links splitting and flying outward, then several broken links falling, finally only a few tiny falling links and spark remnants. Break expands within own cell with all fragments fully inside generous margins.
Style: polished detailed 16-bit pixel art with clear pixel clusters and dark outlines, matching reference iron. Strong readable silhouette at ~80x110 screen pixels. All eight sprites fully contained, no clipping, no stretching beyond cell. Real transparent alpha including all empty interior spaces. No people, no ghost, no text, no scenery.
