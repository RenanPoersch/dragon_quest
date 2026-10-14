# Animações do dragão — 13 de outubro de 2026

`Assets/Resources/PixelArt/DragonClaw.png` e `DragonBreath.png` foram gerados com a ferramenta integrada `image_gen`, usando `Dragon.png` como referência do personagem. São PNGs RGBA de 1536 × 1024, com seis frames em três colunas e duas linhas, na ordem de leitura. Os importadores usam Point, sem mipmaps, sem compressão e com leitura habilitada. Os PNGs preservam a saída original; o recorte acontece em memória.

Garra: repouso, antecipação, garra levantada, corte com arco, extensão e recuperação. Sopro: preparação, garganta brilhando, chama inicial, disparo, dissipação e recuperação. As folhas receberam uma revisão para acrescentar margens transparentes e manter cada pose e efeito dentro da própria célula.

`PixelSpriteLayout.ReadAttack` compartilha o recorte, escala e âncora entre os seis frames. `DragonAttackAnimation` define duração e frame de impacto. A batalha usa a sequência de apresentação existente, bloqueando o próximo comando durante a animação. Preparar sopro usa a folha própria DragonCharge, sem disparar fogo; o sopro completo aparece na ação seguinte. O dragão mantém sua orientação atual na arena.

Prompts finais usados: preservar o dragão vermelho, chifres dourados, barriga clara e asas roxas/vermelhas; gerar seis fases do ataque de garra e seis do sopro em grade 3 × 2 com alpha real; reduzir cada pose e seus efeitos dentro da célula para deixar margens transparentes, manter a escala e alinhar os pés. Sem cenário, texto ou linhas de grade.

Para conferir na Unity, reinicie Play e entre na batalha do dragão. Observe a garra, o turno de preparação e o sopro seguinte. Garra e sopro voltam ao sprite de repouso ao terminar. A preparação mantém a última pose carregada enquanto IsCharging estiver ativo.

## Carregamento do sopro — 14 de outubro de 2026

Assets/Resources/PixelArt/DragonCharge.png foi gerado com image_gen a partir de DragonBreath.png como referência de identidade, cores, escala e posição. PNG original RGBA de 1536 × 1024, seis frames em grade 3 × 2: repouso, preparação, garganta acesa, concentração de brasas, brilho máximo e postura carregada. Importação Point, sem mipmaps ou compressão e com leitura de pixels. A primeira pose calibra o tamanho do corpo, conservando a correção de escala da luta. A animação dura 1,08 s e a pose final permanece até o sopro.

Prompt usado: o mesmo dragão vermelho com chifres dourados, barriga clara e asas roxas/vermelhas; seis poses completas olhando para a direita, brilho crescente na garganta e brasas convergindo para o peito, boca fechada e sem jato de fogo; alpha real, margens transparentes e pés alinhados, sem texto, grade ou cenário.
