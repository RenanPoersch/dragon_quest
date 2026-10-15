# Abertura em pixel art

A cena `CidadePrototipo` abre com a paisagem antes do menu `New game / Continue`. O controle do jogador permanece bloqueado pelo menu durante a abertura.

- Paisagem: fade-in de 1,8 segundo, preservando a composição e a proporção em outras resoluções.
- Título `DRAGON QUEST`: começa em 1,45 segundo e aparece letra por letra em posições fixas, usando Pixel Operator Bold.
- Oito pássaros independentes percorrem o céu, com seis sprites de bater de asas, tamanhos, velocidades e fases diferentes. Não existem pássaros pintados no fundo.
- Em 7,8 segundos começa um fade-out de 0,85 segundo; depois aparece o menu existente.
- Enter, Espaço, Esc, clique esquerdo, toque ou botão sul do controle antecipam a transição. O primeiro instante ignora entradas acidentais; manter a tecla pressionada não seleciona `New game` ao terminar.
- Tempo não escalado: a abertura funciona mesmo com `Time.timeScale = 0`. O relógio começa depois do primeiro quadro renderizado, para o carregamento dos mapas não consumir o fade-in.

`IntroSequence` concentra os tempos. `PrototypeIntroPresentation` mantém fundo, título e sprites em camadas separadas e restaura o estado do IMGUI após desenhar. `PrototypeSaveController` inicia a apresentação, bloqueia entradas até sua conclusão e libera o menu original. Os sprites criados em runtime são destruídos junto com o controlador.

## Assets

- `Assets/Resources/PixelArt/Intro/Sunset.png`: fundo opaco, 1672 × 941.
- `Assets/Resources/PixelArt/Intro/BirdFlight.png`: atlas RGBA, 2172 × 724, seis células horizontais de 362 × 724. As margens transparentes preservam o alinhamento da animação.
- Texturas com Point, sem mipmaps, sem compressão e sem redimensionamento para potência de dois.

Arte gerada com a ferramenta integrada **imagegen**, usando a imagem anexada pelo usuário como referência visual para o fundo. O título é texto do jogo, não está gravado na imagem.

## Validação

Execute `Tools/Test-Intro.ps1` para os testes de fade, título, avanço automático e antecipação em diferentes momentos. Execute `Tools/Test-UnityScripts.ps1` para compilar todos os scripts contra as bibliotecas da Unity e `Tools/Test-Saves.ps1` para verificar a persistência existente.

Nesta atividade passaram a compilação de todos os scripts, 28 verificações da sequência e 135 verificações de salvamento. Um executável Windows isolado também validou a importação sem compressão, as seis regiões distintas do atlas, Point sem mipmaps, avanço automático e antecipação com `Time.timeScale = 0`, bloqueio da tecla mantida pressionada, liberação da entrada, `New game`, `Continue`, retorno e início pelo slot. Esses testes de integração rodaram em modo batch, simulando a primeira apresentação do quadro, pois esse modo não envia Repaint ao IMGUI; a conferência visual em Play segue o roteiro abaixo.

Em Play, observe a sequência inteira; repita antecipando no fade-in e depois do título. Mantenha Enter ou o mouse pressionado durante a transição: o menu deve esperar a liberação sem selecionar uma opção. Confira também `New game`, retorno dos slots, `Continue` e as proporções 16:9 e 4:3.

## Prompts finais

Os dois prompts completos usados pela ferramenta estão em `INTRO_IMAGE_PROMPTS.md`.
