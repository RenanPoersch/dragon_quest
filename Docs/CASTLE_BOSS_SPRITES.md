# Tirano e espírito no castelo

O interior do castelo usa `TyrantThrone.png`: o rei normal sentado, sem maça ou pose de combate. A imagem inclui o trono e preserva a identidade do rei em `NpcRoster`. Os antigos blocos do trono ficam sem renderização e conservam seus colisores. O espírito usa a imagem única de `SpiritBattle.png`, copiada sem alterações do modelo 3 escolhido em `ArtInputs/SpiritVariants/03-espectro-glacial.png`. O azul é apenas visual: o nome, as afinidades neutras e as habilidades Drenar vida, Correntes e Caos Magic continuam iguais.

`PixelSpriteLayout.ReadSingle` lê o espírito inteiro para a exploração. No combate, `PrototypeBattleSprites` usa essa mesma pose completa nas quatro fases de apresentação, sem dividir a silhueta. O retrato de fallback também carrega `PixelArt/SpiritBattle`. A imagem de 1254 × 1254 preserva exatamente os pixels e o alpha do modelo aprovado, com o importador e GUID existentes.

Os sprites usam o material `PixelCharacter`, orientação vertical acompanhando a câmera e pivô na base. Alturas de referência: 2,15 para o conjunto rei/trono e 2,1 para o espírito, com a compensação oblíqua usada pelos demais personagens. A base do tirano conserva a altura anterior de 0,15. Collider, posição, diálogo e início do combate permanecem iguais. `TyrantInteraction` continua revelando o espírito somente quando alguém equipa a Armadura dracônica.

Validação técnica desta revisão: scripts compilados com as bibliotecas da Unity 6000.6.5f1, 518 verificações do castelo/Protect/meteoro e 102 de artes/importadores. A imagem do trono é RGBA, tem margens transparentes, importação Point/Clamp e permanece byte a byte igual à imagem gerada. A folha única é recortada em memória por `ReadSingle`, sem modificar a arte.

A suíte geral `Test-Combat.ps1` continua falhando no caso anterior de Infusão/Thunder. A conferência nativa em Play permanece pendente: reiniciar a execução, visitar o castelo, conferir o rei sentado no trono e equipar/remover a Armadura dracônica para alternar a visibilidade do espírito. Efeitos e roteiro do meteoro em `TYRANT_ATTACKS.md`.

Validação da troca do espírito: scripts compilados com as bibliotecas da Unity 6000.6.5f1; 518 verificações do castelo/Protect/meteoro passaram. PNG conferido byte a byte contra o modelo escolhido, com transparência, silhueta inteira, Point/Clamp, leitura, ausência de compressão/mipmaps/redução e afinidades/habilidades originais. A conferência visual em Play permanece pendente.
