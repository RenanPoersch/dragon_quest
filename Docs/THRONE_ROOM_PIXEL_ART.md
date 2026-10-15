# Sala do trono — frontal monumental

Implementação da alternativa **2**, escolhida pelo usuário: cenário inteiro em pixel art, com a aproximação frontal e baixa, colunas altas, vitral azul, estandartes violeta, tochas e um trono dourado sobre uma escadaria.

A câmera da exploração usa perspectiva real. Os sprites conservam sua escala física e ficam naturalmente menores ao caminhar em direção ao trono, crescendo ao retornar à entrada. O movimento usa eixos frontais: W/cima avança, S/baixo recua e A/D deslocam lateralmente. O ritmo da caminhada acompanha a distância física percorrida, e as direções cardinais/diagonais usam a orientação da nova câmera.

`ThroneRoomLayout` calibra a projeção sobre o cenário escolhido e define piso, limites, escadaria, entrada, saída, rei e espírito. A física e as interações continuam no plano XY. `ThroneRoomDepth` eleva a apresentação dos personagens e suas sombras na escadaria, sem alterar seus colisores. `ThroneRoomPixelScenery` desenha a pintura integral com camadas de profundidade nos pilares laterais.

`CameraFollow` recebe o perfil de perspectiva dessa sala. A pintura inteira cabe em 16:9, 16:10 e 4:3. Ao sair, a câmera ortográfica, a orientação dos controles e a altura original do herói são restauradas. Os desfechos também restabelecem explicitamente a câmera usada nas cenas finais. A batalha continua usando o sistema existente.

O ID `castle`, a fachada da cidade, o diálogo do tirano, o início do combate, a revelação do espírito pela Armadura dracônica e a recuperação na Pousada continuam conectados aos mesmos serviços. Posições antigas de save que caem fora do piso novo reaparecem na entrada. As antigas peças geométricas do interior deixaram de ser instanciadas; ícones e fachadas do castelo continuam em uso.

## Artes e prompts

- [ThroneRoomInterior.png](../Assets/Resources/PixelArt/ThroneRoomInterior.png): cópia byte a byte da [alternativa 2 aprovada](ArtInputs/ThroneRoomVariants/02-frontal-monumental.png), original 1672×941. Criada com o imagegen integrado, em modo de geração. [Prompt integral](ArtInputs/ThroneRoomInterior.prompt.txt).
- [TyrantSeated.png](../Assets/Resources/PixelArt/TyrantSeated.png): rei sentado, isolado e transparente, sem cadeira. Criado com o imagegen integrado, em modo de edição, usando `TyrantThrone.png` como alvo e referência de identidade. Assim o personagem ocupa o trono da pintura sem desenhar uma segunda cadeira. [Prompt integral](ArtInputs/TyrantSeated.prompt.txt).
- [Prévia na entrada](ThroneRoomPixelArt.preview.png) e [prévia perto do trono](ThroneRoomDepth.preview.png), renderizadas pela Unity com a mesma câmera e os mesmos sprites usados no jogo.

PNGs originais preservados sem edição ou redução. Importação Point/Clamp, sem mipmaps nem compressão. O rei é recortado somente em memória pela biblioteca de sprites; a imagem fonte permanece intacta.

## Verificação

`Tools > Dragon Quest > Validate Throne Room` renderiza a entrada, o meio do salão, a escadaria, a aproximação ao rei e o espírito revelado. Em 1280×720, o herói mede aproximadamente 104, 77, 64 e 61 pixels de altura nesses quatro pontos: a redução é contínua e vem da perspectiva, com escala do sprite e tamanho do colisor constantes.

A validação verifica rotas conectadas, limites de movimento, acesso à saída, alcance do diálogo, chamada única do combate após a conversa, revelação do espírito por equipamento, restauração de saves, troca de modos da câmera e os três formatos de tela. Os relatórios, imagens e medições ficam em `Temp/ThroneRoomPreview` ou `DQ_THRONE_OUTPUT`. Em seguida são executadas as validações das lojas de itens, matérias, armas e Pousada como regressão.

Na revisão, compilaram os scripts com as bibliotecas da Unity e passaram 139 verificações de salvamento e 605 verificações do castelo/Shield/meteoro. As quatro validações visuais por lotes concluíram com código 0.
