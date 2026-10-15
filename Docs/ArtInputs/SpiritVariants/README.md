# Modelos alternativos do espírito

Quatro propostas em pixel art com formatos e paletas diferentes, criadas para escolha do usuário. Cada PNG contém uma única pose com fundo transparente. São conceitos; a escolha ainda deverá ser adaptada para as animações do jogo.

| Modelo | Forma e cores | Arquivo |
| --- | --- | --- |
| 1 — Chama jade | Chama verde com rosto ósseo e cauda curva | [PNG](01-chama-jade.png) |
| 2 — Fumaça carmesim | Criatura larga com chifres, garras e fumaça vermelha | [PNG](02-fumaca-carmesim.png) |
| 3 — Espectro glacial | Máscara alongada e faixas de energia azuis | [PNG](03-espectro-glacial.png) |
| 4 — Olho âmbar | Olho dourado em aro fragmentado com cauda de fumaça | [PNG](04-olho-ambar.png) |

Geração: ferramenta integrada `image_gen`, uma chamada por modelo. A arte `Assets/Resources/PixelArt/TyrantBattle.png` foi usada somente como referência visual de pixel art. Os prompts completos estão em [prompts.json](prompts.json).

Nenhum recurso de `Assets` nem referência de execução foi alterado nesta atividade. Os quatro modelos permanecem disponíveis para comparação antes de aplicar a escolha.

Validação dos quatro PNGs: arquivos RGBA de 1254 × 1254, alpha de 0 a 255, conteúdo visível sem cortes e cópias idênticas às imagens geradas. A conferência de margens usa alpha ≥ 2/255: os originais preservam alguns pixels residuais com alpha 1/255 fora das silhuetas. Esses conceitos ainda precisarão de preparação da folha de animações após a escolha.
