# Loja de armas em pixel art

O interior completo é uma pintura única, com paredes de pedra e madeira, piso, iluminação, balcão, forja, bigorna, expositores de espadas, machados, escudos, lanças e armaduras. O corredor central liga a porta inferior ao atendimento. O acabamento segue a Pousada.

Arte criada com o imagegen integrado, preservada no PNG original de 1672×941 em `Assets/Resources/PixelArt/WeaponShopInterior.png`. Prompt completo: `Docs/ArtInputs/WeaponShopInterior.prompt.txt`. Importação Point/Clamp, sem mipmaps, compressão ou redimensionamento. A prévia da Unity está em `Docs/WeaponShopPixelArt.preview.png`.

`WeaponShopLayout` registra piso acessível, móveis, entrada, saída e vendedor sobre a pintura. `WeaponShopPixelScenery` acrescenta profundidade ao balcão, à bigorna e ao baú usando os mesmos pixels da imagem. `PaintedInteriorScenery` é o renderizador compartilhado com a Pousada; a planta e a arte aprovadas da Pousada permanecem iguais.

O atendimento na frente do balcão abre a loja `equipment-shop` existente, com o mesmo catálogo, preços, estoques, compra e venda. A conversa direta com o vendedor também fica disponível. A porta continua ligada à mesma fachada da cidade. Saves antigos da loja reaparecem na entrada da nova planta. A câmera mantém o cenário inteiro em 16:9, 16:10 e 4:3.

Limpeza: removidos `WeaponShopArt.cs` e seu metadado, a chamada ao interior antigo e a disposição dos sprites de armas. Os oito recortes exclusivos de armas deixaram de ser amostrados ou instanciados. `ShopPropsAtlas.png`, `MateriaShopAtlas.png` e os shaders compartilhados continuam atendendo às lojas de itens e matérias; os PNGs compartilhados ficam preservados. O atlas antigo exclusivo da Pousada já foi removido na revisão anterior.

`Tools > Dragon Quest > Validate Weapon Shop` verifica arte, contornos, colisões, caminhos, limites de movimento, entrada e saída, atendimento, compra, venda, saves antigos e sprites das outras lojas. Renderiza os três formatos e detalhes na pasta `Temp/WeaponShopPreview`, ou em `DQ_WEAPON_SHOP_OUTPUT`, e executa a validação completa da Pousada como regressão.
