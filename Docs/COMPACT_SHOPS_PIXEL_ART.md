# Lojas compactas de itens e matérias

As duas lojas têm cenários completos em pixel art: piso, paredes, iluminação, balcão, prateleiras e mercadorias fazem parte da pintura. A loja de itens usa madeira clara, verde e frascos de poções, éteres e ervas. A loja de matérias usa violeta, madeira escura e orbes coloridos. O corredor central conecta a entrada ao atendimento sem espalhar objetos pela passagem.

As plantas 4:3 usam altura de câmera de 3,6 unidades. A [loja de armas](WEAPON_SHOP_PIXEL_ART.md) e a [Pousada](INN_PIXEL_ART.md) foram depois redesenhadas com a mesma metragem compacta. Todas mantêm a mesma escala física dos personagens. A área de cada sala corresponde a aproximadamente 39% das antigas versões largas de armas e Pousada. A câmera contém a pintura inteira em telas 16:9, 16:10 e 4:3, com margens quando necessário.

Imagens criadas com o imagegen integrado, em modo de geração, e preservadas nos PNGs originais de 1448×1086, sem edição ou redimensionamento:

- [Arte de itens](../Assets/Resources/PixelArt/ItemShopInterior.png), [prompt integral](ArtInputs/ItemShopInterior.prompt.txt) e [prévia da Unity](ItemShopPixelArt.preview.png).
- [Arte de matérias](../Assets/Resources/PixelArt/MateriaShopInterior.png), [prompt integral](ArtInputs/MateriaShopInterior.prompt.txt) e [prévia da Unity](MateriaShopPixelArt.preview.png).

Importação Point/Clamp, sem mipmaps nem compressão. `CompactShopLayout` registra as duas plantas em UV, incluindo piso, limites para a dimensão do personagem, balcão, vendedor, atendimento e passagem. `CompactShopPixelScenery` usa `PaintedInteriorScenery`, compartilhado com Pousada e armas. Uma camada com os mesmos pixels da pintura dá profundidade ao balcão e encobre a parte inferior do vendedor.

Os IDs `item-shop` e `materia-shop`, fachadas, catálogos, preços, estoques, compra e venda permanecem conectados aos mesmos serviços do jogo. É possível interagir na frente do balcão ou com o vendedor. Saves antigos de lojas reaparecem na entrada segura da nova planta.

## Recursos aposentados

Após converter as três lojas, os GUIDs dos recursos antigos foram conferidos contra cenas, materiais e configurações. Foram removidos os atlas `ShopPropsAtlas.png` e `MateriaShopAtlas.png`, o shader `MateriaShop.shader` e os renderizadores `ShopPixelArt.cs`, `MateriaShopArt.cs` e `ArcaneMateriaShopArt.cs`, com seus metadados. `PrototypeShopLayout.cs` também saiu; os limites necessários à compatibilidade de saves antigos ficam em `PrototypeTravel`. A Pousada e armas já haviam removido seus renderizadores exclusivos antigos. Prompts e documentos históricos continuam como registro.

## Validação

`Tools > Dragon Quest > Validate Compact Shops` verifica as duas pinturas e seus contornos, tamanho das plantas, colisões, rotas da entrada até balcão e saída, movimento nas bordas, atendimento, compra e venda de poção e matéria, saves antigos e ausência dos recursos aposentados. Renderiza os três formatos e posições próximas ao balcão e às estantes em `Temp/CompactShopPreview` (ou `DQ_COMPACT_SHOP_OUTPUT`). Executa também as validações da loja de armas e da Pousada como regressão.

Os scripts do jogo são compilados com as bibliotecas da Unity, e `Tools/Test-Saves.ps1` verifica persistência. A validação por lotes usa uma cópia temporária do projeto para preservar o Editor aberto.

Verificação em 15/10/2026: Unity concluiu as três validações com código 0; 135 verificações de salvamento passaram; scripts e teste de mapas atualizado compilaram. As prévias de armas e Pousada ficaram idênticas, pixel por pixel, às versões aprovadas.
