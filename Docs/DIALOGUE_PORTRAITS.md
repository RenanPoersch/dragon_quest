# Retratos de conversa

Onze ilustrações originais em pixel art, com fisionomia, idade, expressão, silhueta e pose próprias. A aparência dos personagens conserva os trajes e cabelos dos sprites do jogo, com anatomia mais natural.

| Personagem | Direção de personagem e pose | PNG em `Assets/Resources/PixelArt/DialoguePortraits` |
| --- | --- | --- |
| Aren / narrador | Jovem de rosto triangular e olhar determinado, cabeça virada à esquerda e capa diagonal | `aren.png` |
| Aldeão | Robusto, rosto redondo e nariz largo; sorriso aberto e mão acenando | `villager.png` |
| Camponesa | Mulher mais velha, rosto quadrado e olhar perspicaz; cabeça de lado e expressão reservada | `peasant.png` |
| Jardineiro | Idoso magro, rosto comprido e rugas marcadas; curvado sobre uma muda | `gardener.png` |
| Exploradora | Rosto anguloso, pele bronzeada e sardas; queixo erguido, meio sorriso e mapa sob o braço | `explorer.png` |
| Guardião da pedreira | Mandíbula larga, nariz achatado e barba curta; braços cruzados e olhar desconfiado | `quarry-keeper.png` |
| Viajante | Homem mais velho e magro, rosto encovado; olha para trás por cima do ombro | `traveler.png` |
| Hospedeira | Rosto cheio, bochechas rosadas e coque; gargalhada acolhedora com a mão sobre o avental | `innkeeper.png` |
| Tirano | Rei envelhecido de rosto ósseo e nariz adunco; olhar frontal severo e mão com anel junto ao rosto | `tyrant.png` |
| Dragão corrompido | Cabeça frontal larga e baixa, olhar furioso e presas expostas | `dragon.png` |
| Essência liberta | Perfil dourado à esquerda, pescoço em curva e cabeça elevada em serenidade | `dragon-essence.png` |

Produção em 16 de outubro de 2026 com o `image_gen` integrado, uma geração por personagem. `NpcRoster`, `ArenWalk` e `Dragon` servem somente como referência de identidade e traje. Cada rosto é descrito independentemente, incluindo estrutura óssea, idade, assimetria, temperamento e pose. Os PNGs são copiados integralmente, preservando a transparência, sem edição posterior. Os sprites do cenário permanecem com seus arquivos existentes.

O campo azul tem altura fixa de 272 unidades na referência de 720 de altura, independente do tamanho da fala ou do número de opções. O conteúdo excedente usa rolagem. O retrato é um recorte transparente sem fundo ou borda próprios, desenhado sobre o painel com 20% da altura da sua área acima do topo da caixa. A imagem mantém as proporções e se adapta à largura disponível. Nome, fala e escolhas ficam à direita.

A importação usa Point/Clamp, sem mipmaps, compressão, alteração de dimensões ou cópia legível em CPU. `DialoguePortraitCatalog` resolve os personagens e `DialoguePresentation` guarda os retratos carregados durante a sessão.

Prompts completos, referências e hashes de cada original: [ArtInputs/DialoguePortraits/prompts.json](ArtInputs/DialoguePortraits/prompts.json).

`Tools > Dragon Quest > Validate Dialogue Portraits` confere os onze recursos, transparência, resolução nativa, importação, altura fixa e sobreposição exata de 20% em três formatos de tela. As prévias são renderizadas em um projeto Unity isolado da sessão de jogo do usuário.

![Coleção dos onze personagens, com fisionomias e poses próprias](DIALOGUE_PORTRAITS.preview.png)

Validação concluída: scripts compilados com a Unity 6000.6.5f1; 159 verificações de páginas, escolhas e cancelamento; conferência nativa dos onze PNGs e de 17.297.676 pixels originais; 39 capturas de diálogo em 1280 × 720, 960 × 720 e 540 × 960, incluindo fala curta, fala longa e opções. A comparação visual confirmou a altura constante do painel, o retrato transparente sem borda e a separação da coluna de texto. A galeria foi conferida em conjunto para comparar idades, estrutura facial, expressões, silhuetas e poses.
