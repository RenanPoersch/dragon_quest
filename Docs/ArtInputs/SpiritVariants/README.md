# Modelos alternativos do espírito

Quatro propostas em pixel art com formatos e paletas diferentes, criadas para escolha do usuário. Cada PNG contém uma única pose com fundo transparente. São conceitos; a escolha ainda deverá ser adaptada para as animações do jogo.

| Modelo | Forma e cores | Arquivo |
| --- | --- | --- |
| 1 — Chama jade | Chama verde com rosto ósseo e cauda curva | [PNG](01-chama-jade.png) |
| 2 — Fumaça carmesim | Criatura larga com chifres, garras e fumaça vermelha | [PNG](02-fumaca-carmesim.png) |
| 3 — Espectro glacial | Máscara alongada e faixas de energia azuis | [PNG](03-espectro-glacial.png) |
| 4 — Olho âmbar | Olho dourado em aro fragmentado com cauda de fumaça | [PNG](04-olho-ambar.png) |

Geração: ferramenta integrada `image_gen`, uma chamada por modelo. A arte `Assets/Resources/PixelArt/TyrantBattle.png` foi usada somente como referência visual de pixel art. Os prompts completos estão em [prompts.json](prompts.json).

Modelo escolhido pelo usuário: **3 — Espectro glacial**, apenas como aparência. A imagem foi copiada sem alterações para `Assets/Resources/PixelArt/SpiritBattle.png`, usada inteira na exploração, nas quatro fases de apresentação do combate e no retrato de fallback. O espírito mantém o nome, as afinidades neutras e as habilidades originais; a escolha não adiciona elemento de gelo. Os outros três modelos permanecem arquivados para referência.

Validação dos quatro PNGs: arquivos RGBA de 1254 × 1254, alpha de 0 a 255, conteúdo visível sem cortes e cópias idênticas às imagens geradas. A conferência de margens usa alpha ≥ 2/255: os originais preservam alguns pixels residuais com alpha 1/255 fora das silhuetas. O modelo escolhido usa uma pose única compartilhada pelas quatro fases de apresentação, preservando exatamente a arte aprovada.
