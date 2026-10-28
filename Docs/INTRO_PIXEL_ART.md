# Abertura em pixel art

A cena `CidadePrototipo` abre com a paisagem antes do menu `New game / Continue`. O controle do jogador permanece bloqueado pelo menu durante a abertura.

- Paisagem: fade-in de 3,2 segundos. A arte foi adaptada para Full HD (1920 × 1080, 16:9), com extensão natural da paisagem nas laterais e a árvore abaixo do título. A imagem é exibida inteira com escala proporcional; outras proporções de janela recebem faixas pretas, sem esticar os pixels das bordas. Fundo, título e pássaros compartilham o mesmo enquadramento 16:9.
- Título `DRAGON QUEST`: começa em 2,6 segundos e aparece letra por letra em posições fixas, usando Pixel Operator Bold. O título preto e seu traço foram deslocados juntos 50 px para baixo na exibição 1920 × 1080, com o topo em 110 px.
- Oito pássaros independentes voam em círculos no céu, com centros, raios, velocidades e fases diferentes. Os sprites foram reduzidos a 50% da largura e da altura anteriores, preservando o centro visual e as trajetórias de voo. Os seis sprites de bater de asas continuam animados e se espelham quando o movimento horizontal muda de direção. Não existem pássaros pintados no fundo.
- A abertura permanece na tela, com os pássaros animados, até o jogador pressionar para continuar.
- Enter, Espaço, Esc, clique esquerdo, toque ou botão sul do controle iniciam um fade-out de 0,85 segundo; depois aparece o menu existente. O primeiro instante ignora entradas acidentais; manter a tecla pressionada não seleciona `New game` ao terminar.
- O menu `New game / Continue` surge do preto com fade-in de 1,2 segundo após a intro. A escama tem 50% da largura e da altura anteriores; seu centro, as duas opções e o cursor foram deslocados 50 px para cima em 1920 × 1080. As áreas de clique acompanham as novas posições. A transição inclui o símbolo, as opções e o cursor; a seleção fica bloqueada até terminar. O relógio começa no primeiro quadro do menu e usa tempo não escalado.
- Tempo não escalado: a abertura funciona mesmo com `Time.timeScale = 0`. O relógio começa depois do primeiro quadro renderizado, para o carregamento dos mapas não consumir o fade-in.

`IntroSequence` concentra os tempos. `PrototypeIntroPresentation` mantém fundo, título e sprites em camadas separadas e restaura o estado do IMGUI após desenhar. `PrototypeSaveController` inicia a apresentação, bloqueia entradas até sua conclusão e libera o menu original. Os sprites criados em runtime são destruídos junto com o controlador.

## Assets

- `Assets/Resources/PixelArt/Intro/Sunset.png`: fundo opaco, 1920 × 1080, proporção 16:9; preserva castelo, lago, três aventureiros e árvore, com a paisagem ampliada nas laterais.
- `Assets/Resources/PixelArt/Intro/BirdFlight.png`: atlas RGBA, 2172 × 724, seis células horizontais de 362 × 724. As margens transparentes preservam o alinhamento da animação.
- Texturas com Point, sem mipmaps, sem compressão e sem redimensionamento para potência de dois.

Arte gerada com a ferramenta integrada **imagegen**, usando a paisagem anterior como entrada de edição para a adaptação a Full HD. A exportação final usa escala uniforme e amostragem Point. O título é texto do jogo, não está gravado na imagem.

## Validação

Execute `Tools/Test-Intro.ps1` para os testes de fade, título, permanência da abertura e continuação manual em diferentes momentos. Execute `Tools/Test-UnityScripts.ps1` para compilar todos os scripts contra as bibliotecas da Unity e `Tools/Test-Saves.ps1` para verificar a persistência existente.

Na revisão Full HD, a arte final foi inspecionada visualmente e suas dimensões 1920 × 1080 foram verificadas. A compilação dos scripts e as 29 verificações de `Tools/Test-Intro.ps1` passaram. O ajuste proporcional foi conferido para 1920 × 1080, 1280 × 720, 1024 × 768, 2560 × 1080 e 1080 × 1920; em 16:9 não há faixas, e nas demais proporções a escala permanece uniforme. Essa conferência de dimensões não substitui a inspeção em Play descrita abaixo.

Em Play, aguarde sem pressionar nada e observe que a abertura permanece visível; depois pressione para continuar e observe o fade-out. Repita continuando durante o fade-in. Mantenha Enter ou o mouse pressionado durante a transição: o menu deve esperar a liberação sem selecionar uma opção. Confira também `New game`, retorno dos slots, `Continue` e as proporções 16:9 e 4:3.

## Prompts finais

Os prompts completos da paisagem atual e dos pássaros estão em `INTRO_IMAGE_PROMPTS.md`.
