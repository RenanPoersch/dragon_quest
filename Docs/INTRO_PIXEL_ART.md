# Abertura em pixel art

A cena `CidadePrototipo` abre com a paisagem antes do menu `New game / Continue`. O controle do jogador permanece bloqueado pelo menu durante a abertura.

- Paisagem: fade-in de 3,2 segundos. A arte em 6:5 foi recomposta para o frame mais alto do Game view, com a árvore terminando abaixo do título. A imagem é sempre exibida inteira, com escala proporcional e sem recorte ou zoom; se a janela tiver outra proporção, somente os pixels das bordas se estendem pelo espaço excedente, sem cortar ou deformar a composição.
- Título `DRAGON QUEST`: começa em 2,6 segundos e aparece letra por letra em posições fixas, usando Pixel Operator Bold. O título preto e seu traço foram elevados juntos, com o topo em 40 na referência 1280 × 720.
- Oito pássaros independentes voam em círculos no céu, com centros, raios, velocidades e fases diferentes. Os seis sprites de bater de asas continuam animados e se espelham quando o movimento horizontal muda de direção. Não existem pássaros pintados no fundo.
- A abertura permanece na tela, com os pássaros animados, até o jogador pressionar para continuar.
- Enter, Espaço, Esc, clique esquerdo, toque ou botão sul do controle iniciam um fade-out de 0,85 segundo; depois aparece o menu existente. O primeiro instante ignora entradas acidentais; manter a tecla pressionada não seleciona `New game` ao terminar.
- Tempo não escalado: a abertura funciona mesmo com `Time.timeScale = 0`. O relógio começa depois do primeiro quadro renderizado, para o carregamento dos mapas não consumir o fade-in.

`IntroSequence` concentra os tempos. `PrototypeIntroPresentation` mantém fundo, título e sprites em camadas separadas e restaura o estado do IMGUI após desenhar. `PrototypeSaveController` inicia a apresentação, bloqueia entradas até sua conclusão e libera o menu original. Os sprites criados em runtime são destruídos junto com o controlador.

## Assets

- `Assets/Resources/PixelArt/Intro/Sunset.png`: fundo opaco, 1374 × 1145, proporção 6:5; castelo, lago, três aventureiros e árvore mais baixa foram recompostos para caber na imagem inteira.
- `Assets/Resources/PixelArt/Intro/BirdFlight.png`: atlas RGBA, 2172 × 724, seis células horizontais de 362 × 724. As margens transparentes preservam o alinhamento da animação.
- Texturas com Point, sem mipmaps, sem compressão e sem redimensionamento para potência de dois.

Arte gerada com a ferramenta integrada **imagegen**, usando a imagem anexada pelo usuário como referência visual e a primeira paisagem como entrada para a revisão do enquadramento e da árvore. O título é texto do jogo, não está gravado na imagem.

## Validação

Execute `Tools/Test-Intro.ps1` para os testes de fade, título, permanência da abertura e continuação manual em diferentes momentos. Execute `Tools/Test-UnityScripts.ps1` para compilar todos os scripts contra as bibliotecas da Unity e `Tools/Test-Saves.ps1` para verificar a persistência existente.

Na revisão do fade-in e da continuação manual, os testes existentes foram alinhados ao novo comportamento. Nesta revisão do enquadramento, a arte foi inspecionada visualmente e as alterações foram revisadas pelo diff; testes e compilação não foram executados, mantendo a preferência do usuário.

Em Play, aguarde sem pressionar nada e observe que a abertura permanece visível; depois pressione para continuar e observe o fade-out. Repita continuando durante o fade-in. Mantenha Enter ou o mouse pressionado durante a transição: o menu deve esperar a liberação sem selecionar uma opção. Confira também `New game`, retorno dos slots, `Continue` e as proporções 16:9 e 4:3.

## Prompts finais

Os prompts completos da paisagem atual e dos pássaros estão em `INTRO_IMAGE_PROMPTS.md`.
