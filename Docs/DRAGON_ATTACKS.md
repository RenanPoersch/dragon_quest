# Animações do dragão — 13 de outubro de 2026

`Assets/Resources/PixelArt/DragonClaw.png` e `DragonBreath.png` foram gerados com a ferramenta integrada `image_gen`, usando `Dragon.png` como referência do personagem. São PNGs RGBA de 1536 × 1024, com seis frames em três colunas e duas linhas, na ordem de leitura. Os importadores usam Point, sem mipmaps, sem compressão e com leitura habilitada. Os PNGs preservam a saída original; o recorte acontece em memória.

Garra: repouso, antecipação, garra levantada, corte com arco, extensão e recuperação. Sopro: preparação, garganta brilhando, chama inicial, disparo, dissipação e recuperação. As folhas receberam uma revisão para acrescentar margens transparentes e manter cada pose e efeito dentro da própria célula.

`PixelSpriteLayout.ReadAttack` compartilha o recorte, escala e âncora entre os seis frames. `DragonAttackAnimation` define duração e frame de impacto. A batalha usa a sequência de apresentação existente, bloqueando o próximo comando durante a animação. Preparar sopro mostra somente os dois primeiros frames, sem disparar fogo; o sopro completo aparece na ação seguinte. O dragão mantém sua orientação atual na arena.

Prompts finais usados: preservar o dragão vermelho, chifres dourados, barriga clara e asas roxas/vermelhas; gerar seis fases do ataque de garra e seis do sopro em grade 3 × 2 com alpha real; reduzir cada pose e seus efeitos dentro da célula para deixar margens transparentes, manter a escala e alinhar os pés. Sem cenário, texto ou linhas de grade.

Para conferir na Unity, reinicie Play e entre na batalha do dragão. Observe a garra, o turno de preparação e o sopro seguinte. Cada ataque volta ao sprite de repouso ao terminar.
