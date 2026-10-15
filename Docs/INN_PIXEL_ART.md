# Pousada: cenário completo em pixel art

O interior inteiro é uma única pintura: piso, paredes de pedra e reboco, vigas, janelas, móveis, tapetes, luz e sombras. A sala comum com lareira e mesa fica à esquerda; as três camas formam uma alcova ao fundo; a recepção fica à direita, com circulação central até a porta inferior. Não há mais uma estrutura de blocos com sprites de móveis espalhados.

Arte criada com o imagegen integrado. O PNG original de 1672×941 está em `Assets/Resources/PixelArt/InnInterior.png`. Prompt completo: `Docs/ArtInputs/InnInterior.prompt.txt`. O arquivo é preservado sem recortes, redimensionamento ou edição, com filtro Point, sem mipmaps e sem compressão.

`InnPixelScenery` apresenta a pintura inteira usando `PaintedInteriorScenery`, também usado pela loja de armas. Contornos da própria imagem acrescentam profundidade ao balcão, à mesa e às divisórias: personagens que passam atrás deles são encobertos corretamente, usando os mesmos pixels do cenário. Os personagens continuam sendo sprites do jogo.

`PrototypeInnLayout` registra o piso acessível e as bases dos móveis em coordenadas da pintura. O movimento acompanha paredes, camas, divisórias e mobiliário, incluindo deslizamento e passos curtos para impedir atravessar obstáculos. A interação de salvar fica na frente do balcão, enquanto a hospedeira aparece atrás dele. O ID `inn`, a ligação com a cidade e o salvamento permanecem; posições antigas inválidas voltam à entrada.

A câmera apresenta a imagem inteira em 16:9, 16:10 e 4:3, com faixas nas proporções diferentes para preservar entrada e móveis. A configuração de enquadramento das florestas continua com seu comportamento anterior.

`Tools > Dragon Quest > Validate Inn` na Unity verifica arte, contornos de profundidade, trajetos da entrada até as camas, sala comum e recepção, limites de movimento, saves e retorno à cidade. Renderiza os três formatos e detalhes de profundidade em `Temp/InnPreview` ou na pasta de `DQ_INN_OUTPUT`. A prévia principal está em `Docs/InnPixelArt.preview.png`.
