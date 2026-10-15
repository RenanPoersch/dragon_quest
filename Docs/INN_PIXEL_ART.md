# Pousada em pixel art

O interior usa assoalho de madeira, lambris, vigas aparentes e tapetes verdes com bordas douradas. Os sprites incluem três camas com colchas bordadas, lareira de pedra, balcão com livro de hóspedes, armário de enxoval, mesa de café, janelas com cortinas, lanternas e samambaias.

`InnPixelScenery` monta o ambiente na mesma projeção oblíqua dos personagens. A arte tem transparência e profundidade; os sprites podem encobrir personagens que passam atrás dos móveis. O PNG original de 1536×1024 fica preservado em `Assets/Resources/PixelArt/InnPropsAtlas.png`; os oito objetos são registrados e amostrados com vizinho mais próximo numa grade lógica de 128 pixels por célula, somente em memória. Texturas usam filtro Point, sem mipmaps ou compressão. A madeira reaproveita `MateriaShopAtlas`, mantendo o estilo das lojas.

`PrototypeInnLayout` mantém as três camas, a entrada, a saída, o ID do mapa e a posição da hospedeira. Os móveis têm colisão, com corredor central livre e acesso ao balcão. Saves antigos que ficariam dentro dos novos móveis voltam à entrada; posições válidas são mantidas. A conversa e o salvamento da jornada continuam em `InnInteraction`.

Arte criada com o imagegen integrado, com transparência. Prompt completo: `Docs/ArtInputs/InnPropsAtlas.prompt.txt`.

Na Unity, `Tools > Dragon Quest > Validate Inn` verifica sprites, transparência, colisões, caminhos até as camas e o balcão, recuperação de saves e a opção de salvar da hospedeira. Também renderiza prévias em 16:9, 16:10 e 4:3 em `Temp/InnPreview` (ou na pasta definida por `DQ_INN_OUTPUT`).
