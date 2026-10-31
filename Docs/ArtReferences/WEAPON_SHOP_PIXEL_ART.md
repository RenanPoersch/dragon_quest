> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Loja de armas compacta em pixel art

O interior foi redesenhado no mesmo tamanho das lojas de itens e matérias: planta 4:3, altura de câmera de 3,6 unidades e a mesma escala física dos personagens. A área projetada é aproximadamente 39% da antiga versão larga. A sala compacta reúne balcão, forja embutida à esquerda, bigorna, baú, expositores de espadas, machados, escudos e lanças e uma armadura lateral. O corredor central liga a porta ao atendimento.

Arte criada com o imagegen integrado, em modo de geração com referências: a loja de itens fornece metragem visual e câmera; a versão anterior de armas fornece tema e materiais. O PNG original de 1447×1087 está preservado em [WeaponShopInterior.png](../../Assets/Resources/PixelArt/WeaponShopInterior.png). [Prompt integral](ArtInputs/WeaponShopInterior.prompt.txt) e [prévia da Unity](WeaponShopPixelArt.preview.png). Importação Point/Clamp, sem mipmaps, compressão ou redução. A pintura larga foi substituída no mesmo recurso, mantendo seu GUID; a versão anterior continua no histórico Git.

`WeaponShopLayout` registra o piso, as bases dos móveis, a entrada, a saída e o atendimento. A margem de colisão corresponde ao personagem no ambiente menor. `WeaponShopPixelScenery` acrescenta profundidade ao balcão, à bigorna e ao baú usando os mesmos pixels da imagem, por meio de `PaintedInteriorScenery`.

O ID `equipment-shop`, a fachada da cidade, catálogo, preços, estoques, compra e venda permanecem conectados ao mesmo serviço. Saves da planta antiga e da pintura larga são aceitos pelos limites históricos; posições que caem fora do piso novo voltam à entrada. A câmera contém a sala inteira em 16:9, 16:10 e 4:3.

A comparação de atributos na loja altera apenas os valores base do equipamento destacado e mantém os bônus das matérias atualmente equipadas, mesmo quando o candidato tem menos encaixes. As afinidades de classe continuam consideradas. A prévia não move equipamentos ou matérias; a troca efetiva pelo menu de equipamentos mantém sua regra de devolver as matérias ao inventário.

`Tools > Dragon Quest > Validate Weapon Shop` verifica pintura, contornos, colisões, caminhos, limites de movimento, igualdade de metragem com as lojas pequenas, saves anteriores, entrada, saída, atendimento, compra e venda. Renderiza os três formatos e aproximações ao balcão, forja e expositores em `Temp/WeaponShopPreview` (ou `DQ_WEAPON_SHOP_OUTPUT`) e executa a validação da Pousada.

Os atlas, shader e renderizadores antigos das lojas já haviam sido removidos; os dois cenários largos foram agora substituídos pelos novos PNGs compactos. Recursos atuais de itens, matérias e sala do trono permanecem iguais. Registro da primeira conversão em [COMPACT_SHOPS_PIXEL_ART.md](COMPACT_SHOPS_PIXEL_ART.md).
