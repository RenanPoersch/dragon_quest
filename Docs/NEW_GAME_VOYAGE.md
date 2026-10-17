# Travessia ao iniciar New game

`New game` agora apresenta uma cena lateral antes de liberar o mapa global. `Continue` mantém o carregamento direto do save. A abertura do título permanece independente.

- A nau entra pela direita e desacelera até a proa parar um pouco antes do centro. O enquadramento próximo usa largura de 1560 na composição de 1280: aproximadamente 47% da nau fica visível, com a popa fora da tela. Velas e parte inferior do casco também continuam além do quadro.
- Aren, Lia e Bram caminham em sequência pelo convés, voltados para a proa à esquerda. Cada herói usa uma escala fixa, com a maior pose lateral ajustada a 96 de altura na composição. `VoyageShipLayers` divide a mesma arte em duas camadas complementares, seguindo a curva da amurada: velas e lado distante atrás dos heróis, lado próximo e casco à frente. Os pés seguem um contorno próprio do piso de madeira, mais baixo que a amurada, especialmente nas posições de Lia e Bram. A amurada encobre as pernas. Não há recorte retangular atravessando os personagens.
- O panorama do mar e das montanhas é o mesmo das ruínas, amostrado com filtro Point. Fundo e reflexos passam para a direita, em sentido coerente com a navegação para a esquerda. A velocidade aumenta durante o deslocamento da embarcação e mantém uma deriva durante a leitura.
- A área completa da narrativa — sombra, cabeçalho e história — só começa a aparecer 0,4 s depois de a nau chegar. O tratamento preto usa a mesma cor, força de 82%, queda horizontal suave e tempos de entrada/saída do texto do FinalOne. A história sobe automaticamente na coluna esquerda e some antes da partida. O texto apresenta a ilha sob a tirania e a busca dos três pela lenda, sem revelar soluções ou finais.
- Ao terminar, há uma pausa de 1,8 s, e a nau parte para a esquerda. O fade para preto usa a mesma curva suave e duração de 3 s do FinalOne, seguido por 1 s de preto. A nau sai inteiramente do quadro antes de o mapa aparecer por fade-in. A sequência dura aproximadamente 62 segundos.
- Movimento, interações, batalha, inventário, cheats e salvamento ficam bloqueados até terminar a transição. O tempo usa `Time.unscaledDeltaTime`, começa após o primeiro desenho e pausa quando o jogo perde o foco. A cena não cria nem substitui saves.

## Arquivos

`NewGameVoyageSequence` concentra tempos, posições, deriva e opacidade da narrativa. `NewGameVoyagePresentation` desenha os planos e restaura o estado do IMGUI. `VoyageShipLayers` produz as camadas traseira e dianteira em memória, com o mesmo registro e sem redesenhar o PNG. `PrototypeSaveController` mantém o bloqueio existente enquanto a travessia está ativa e libera o jogador ao final.

Arte: `Assets/Resources/PixelArt/Intro/VoyageCarrack.png`, gerada com **imagegen integrado**, RGBA 1536 × 1024. Esta revisão reutiliza o original sem alterar o PNG; a apresentação amostra as duas camadas temporárias de 384 × 256 com vizinho mais próximo. Todos os recursos temporários são descartados ao terminar.

## Validação

`Tools/Test-NewGameVoyage.ps1` verifica enquadramento da metade dianteira, tamanho e posição dos heróis, ausência da narrativa durante a chegada, movimento do fundo para a direita, caminhada, rolagem completa, partida e fades com os tempos do final. A validação da Unity reconstrói o PNG pelas duas camadas e confere ausência de sobreposição/perda de pixels, oclusão dos pés e largura das linhas de texto. Execute também `Tools/Test-Intro.ps1`, `Tools/Test-Saves.ps1` e `Tools/Test-UnityScripts.ps1`.

Na Unity, `Tools > Dragon Quest > Preview New Game Voyage` abre a mesma apresentação com pontos de conferência e reprodução. No jogo, selecione `New game`, aguarde a história e confirme que só é possível mover e salvar após o fade-in do mapa. `Continue` deve carregar diretamente e os slots existentes devem permanecer intactos.

Validação da revisão do enquadramento: 1.910 verificações da sequência, 29 da abertura existente, 1.000 do FinalOne e compilação dos scripts passaram. Uma cópia isolada do projeto na Unity 6000.6.5f1 validou a reconstrução da arte nas duas camadas, a oclusão das pernas, o panorama sem emendas e a largura do texto. O teste de integração confirmou New Game, bloqueio de save, relógio não escalado/foco, descarte da apresentação, liberação do mapa, salvamento manual posterior e Continue direto. Dez capturas cobriram chegada sem narrativa, close do convés, leitura, partida sem sombra de texto, fade, preto e revelação do mapa; os enquadramentos foram inspecionados. As capturas locais desta revisão ficam em `Temp/VoyagePreview/CloseForedeck`, fora do Git.

### Correção da borda inferior

O casco ampliado e rotacionado podia deixar um risco de pixels após a faixa inferior, sobretudo com escala fracionária e espaço preto abaixo da composição. A máscara das faixas agora é aplicada por último, fora do recorte IMGUI e em coordenadas da tela; as bordas são arredondadas para dentro. Isso cobre integralmente o espaço preto, inclusive durante o balanço.

Validação: compilação dos scripts e teste de integração passaram. A falha foi reproduzida em 1003 × 831; 22 capturas após a correção, distribuídas entre balanço, leitura e partida, tiveram zero pixels fora da área da cena. Evidências locais em `Temp/VoyagePreview/BorderFix`.

### Apoio dos personagens no convés

O piso tem um perfil separado do corrimão, calibrado na faixa contínua de madeira abaixo dos vãos. A apresentação posiciona o pivô dos pés de cada sprite nesse piso e preserva a mesma escala entre as poses da caminhada. Assim, os recortes diferentes dos atlas de Lia e Bram não alteram o apoio nem o tamanho do corpo a cada passo. O grupo continua compartilhando a translação e a rotação da nau.

Validação desta correção: compilação dos scripts e verificações de tempo passaram. A Unity conferiu contato com pixels opacos da camada dianteira em 123 posições ao longo das três caminhadas. Quatorze capturas do renderizador real foram inspecionadas, cobrindo caminhada, parada, diferentes fases do balanço e partida. Evidências e projeto isolado em `Builds/VoyagePreview`, fora do Git.

## Prompt final da nau — imagegen integrado

Use case: illustration-story. Asset type: actual transparent game sprite for a side-view 16-bit fantasy RPG opening cutscene. Create ONE beautiful wooden Portuguese-style carrack (nau) sailing ship in strict SIDE ELEVATION profile, bow pointing LEFT, tall raised stern on the RIGHT. Entire ship and all sails fit within canvas with transparent padding. Warm hand-drawn SNES RPG pixel art matching Mediterranean blue sea and warm pale clouds: chestnut and golden oak plank hull, dark brown pixel outlines, aged ivory square sails and a smaller triangular sail, 2 main masts, delicate dark rigging and rope ladders, small red pennant extending to the right. Clearly visible long horizontal OPEN WALKABLE FOREDECK along the upper edge of hull on the left half of the vessel, with a low side rail so three small separately rendered hero sprites can walk left on this deck. Keep the foredeck empty and uncluttered. Bow has curved carved timber and short bowsprit pointing left. Rich clustered square pixel highlights, deliberate crisp stepped silhouettes, restrained palette, sparse dithering; logical detail approximately 320 pixels across ship, enlarged nearest neighbor. Wide landscape canvas approximately 3:2. No people, no crew, no characters, no water, no waves, no scenery, no shadows outside vessel, no UI, no text, no emblems or crosses. True transparent background around the ship and between ropes. Flat illustrated 2D sprite, no 3D render, no isometric angle, no perspective bow view, no blur, no photographic detail. Single stationary ship sprite, NOT sprite sheet, no duplicate ships. The wooden deck/hull should be near 74 percent down from the top of the ship silhouette, leaving sails and masts above and hull below.
