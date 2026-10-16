# Travessia ao iniciar New game

`New game` agora apresenta uma cena lateral antes de liberar o mapa global. `Continue` mantém o carregamento direto do save. A abertura do título permanece independente.

- A nau entra pela direita e desacelera até a proa parar um pouco antes do centro, deixando a coluna esquerda livre para a narrativa.
- Aren, Lia e Bram caminham em sequência pelo convés, voltados para a proa à esquerda. São os atlas de caminhada do jogo, com passos durante o deslocamento e pose parada ao chegar. A amurada fica à frente dos pés.
- O panorama do mar e das montanhas é o mesmo das ruínas, amostrado com filtro Point. Há balanço leve da nau, movimento lento do fundo, reflexos e esteira.
- A história sobe automaticamente na coluna esquerda. A partida espera a última linha sair por completo. O texto apresenta a ilha sob a tirania e a busca dos três pela lenda, sem revelar soluções ou finais.
- Ao terminar, a nau parte para a esquerda, sai inteiramente do quadro e a cena escurece. Após um breve intervalo preto, o mapa aparece por fade-in. A sequência dura aproximadamente 53 segundos.
- Movimento, interações, batalha, inventário, cheats e salvamento ficam bloqueados até terminar a transição. O tempo usa `Time.unscaledDeltaTime`, começa após o primeiro desenho e pausa quando o jogo perde o foco. A cena não cria nem substitui saves.

## Arquivos

`NewGameVoyageSequence` concentra tempos e posições. `NewGameVoyagePresentation` desenha os planos e restaura o estado do IMGUI. `PrototypeSaveController` mantém o bloqueio existente enquanto a travessia está ativa e libera o jogador ao final.

Arte nova: `Assets/Resources/PixelArt/Intro/VoyageCarrack.png`, gerada com **imagegen integrado**, RGBA 1536 × 1024. O original permanece intacto; a apresentação amostra uma textura temporária de 384 × 256 com vizinho mais próximo. Todos os recursos temporários são descartados ao terminar.

## Validação

`Tools/Test-NewGameVoyage.ps1` verifica chegada, parada, caminhada em sequência, rolagem completa, partida para a esquerda e ambos os fades. Execute também `Tools/Test-Intro.ps1`, `Tools/Test-Saves.ps1` e `Tools/Test-UnityScripts.ps1`.

Na Unity, `Tools > Dragon Quest > Preview New Game Voyage` abre a mesma apresentação com pontos de conferência e reprodução. No jogo, selecione `New game`, aguarde a história e confirme que só é possível mover e salvar após o fade-in do mapa. `Continue` deve carregar diretamente e os slots existentes devem permanecer intactos.

Validação desta entrega: 1.049 verificações da sequência, 29 da abertura existente, 145 de salvamento e compilação dos scripts passaram. Uma cópia isolada do projeto na Unity 6000.6.5f1 confirmou New Game, bloqueio de save, relógio não escalado/foco, descarte da apresentação, liberação do mapa, salvamento manual posterior e Continue direto. Oito capturas cobriram chegada, caminhada, leitura, partida, preto e revelação do mapa; os enquadramentos foram inspecionados. As capturas locais ficam em `Temp/VoyagePreview`, fora do Git.

## Prompt final da nau — imagegen integrado

Use case: illustration-story. Asset type: actual transparent game sprite for a side-view 16-bit fantasy RPG opening cutscene. Create ONE beautiful wooden Portuguese-style carrack (nau) sailing ship in strict SIDE ELEVATION profile, bow pointing LEFT, tall raised stern on the RIGHT. Entire ship and all sails fit within canvas with transparent padding. Warm hand-drawn SNES RPG pixel art matching Mediterranean blue sea and warm pale clouds: chestnut and golden oak plank hull, dark brown pixel outlines, aged ivory square sails and a smaller triangular sail, 2 main masts, delicate dark rigging and rope ladders, small red pennant extending to the right. Clearly visible long horizontal OPEN WALKABLE FOREDECK along the upper edge of hull on the left half of the vessel, with a low side rail so three small separately rendered hero sprites can walk left on this deck. Keep the foredeck empty and uncluttered. Bow has curved carved timber and short bowsprit pointing left. Rich clustered square pixel highlights, deliberate crisp stepped silhouettes, restrained palette, sparse dithering; logical detail approximately 320 pixels across ship, enlarged nearest neighbor. Wide landscape canvas approximately 3:2. No people, no crew, no characters, no water, no waves, no scenery, no shadows outside vessel, no UI, no text, no emblems or crosses. True transparent background around the ship and between ropes. Flat illustrated 2D sprite, no 3D render, no isometric angle, no perspective bow view, no blur, no photographic detail. Single stationary ship sprite, NOT sprite sheet, no duplicate ships. The wooden deck/hull should be near 74 percent down from the top of the ship silhouette, leaving sails and masts above and hull below.
