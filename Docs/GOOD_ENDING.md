# Final bom — A voz do rei e um reino em paz

`GameEnding.KingFreed` apresenta uma sequência automática em duas pinturas com o acabamento sério e mais realista dos outros finais. Derrotar o espírito enquanto o Rei Rubro está vivo continua libertando o rei. Os valores dos finais e os saves permanecem compatíveis; jornadas concluídas carregadas pelo menu também usam a nova apresentação.

No primeiro momento, o rei de cabelos grisalhos, coroa de rubis e manto roxo discursa da varanda do castelo diante dos moradores. Recupera a própria voz, agradece a Aren, Lia e Bram, reconhece o sofrimento e promete abrir os celeiros, restaurar casas e servir ao povo. No segundo, a promessa se cumpre: pão é compartilhado na feira, famílias convivem, artesãos trabalham e os campos abastecem o reino.

`EndingNarrative.KingFreedSpeech` e `KingFreedEpilogue` guardam o discurso e a conclusão. Ambos rolam na lateral esquerda, com fonte PixelOperator de 28, sombra, recorte suave e velocidade de 29 pixels por segundo, como os outros epílogos. O fundo preto usa o padrão de degradê horizontal e altura de 540 pixels, restrito à área de leitura e sem tonalidade colorida. O discurso é apresentado em texto. Não há áudio de voz gravada. A cena dispensa continuar páginas.

Cada pintura recebe um movimento sutil de aproximação. A primeira fica ancorada no alto para conservar a coroa inteira. O discurso termina por completo antes da pausa de 1,8 s e da dissolução de 3 s. A imagem do povo permanece 1,4 s antes do novo texto. Depois da última linha, há contemplação de 1,8 s, fade de 3 s e 1 s de preto antes do retorno ao menu. O relógio usa tempo não escalado e só começa após a primeira renderização; a duração se ajusta ao número de linhas dos dois textos. A cena conserva proporções e barras pretas em telas com outros formatos.

## Arte

As duas imagens foram criadas separadamente com o `imagegen` integrado e copiadas integralmente para `Assets/Resources/PixelArt/Endings/KingFreedSpeech.png` e `KingdomProsperity.png`. São pinturas de 1672 × 941, importadas em Point/Clamp, sem compressão, mipmaps, redimensionamento ou cópia na CPU. Os PNGs originais não recebem recorte nem edição; enquadramento, movimento, fundo e sombra de texto e dissolução são feitos em tempo de execução. O renderizador preserva a gravação sRGB do render target mesmo quando chamado pelo IMGUI no modo Linear do projeto, evitando o escurecimento que parecia um filtro sobre a imagem.

O retrato do Rei Rubro fornece a identidade do rei, o final de possessão fornece as cores e o acabamento do castelo, e o panorama das cinzas fornece o acabamento de cenário. A segunda imagem usa a primeira como referência de continuidade. Prompts completos, referências e SHA-256: `Docs/ArtInputs/GoodEnding.prompts.json`.

## Conferência

- `Tools/Test-GoodEnding.ps1`: leitura integral dos dois textos, transição, movimentos, relógio independente do framerate e retorno somente depois do preto.
- `Tools/Test-EndingMotion.ps1 -LinearGoodEnding`: compara as cores originais com a renderização no modo Linear real do jogo, reproduzindo o estado de gravação sRGB do IMGUI.
- `Tools/Test-Castle.ps1`: condições dos três finais.
- `Tools/Test-FinalOne.ps1` e `Tools/Test-HeroPossessed.ps1`: regressão dos demais finais.
- `Tools/Test-UnityScripts.ps1`: compilação com bibliotecas reais da Unity 6000.6.5f1.
- Unity: `Tools > Dragon Quest > Validate Good Ending` verifica arte, importação, enquadramento, fonte e tempo de leitura. `Preview Good Ending` permite animar ou saltar para discurso, transição, prosperidade, fade e preto.
- Em Play, derrote o espírito mantendo o rei vivo. Confira o discurso e a feira e aguarde o retorno ao menu. Carregar uma jornada que já terminou com `KingFreed` também deve apresentar a cena.
