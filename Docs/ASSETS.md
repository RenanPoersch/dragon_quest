# Registro de assets

Registrar cada conjunto de sprites, tiles, fontes, áudio ou outros recursos adicionados ao jogo.

| Recurso | Origem / autor | Licença / condições | Arquivos |
| --- | --- | --- | --- |
| Template 2D inicial | Unity, fornecido com Editor 6000.6.5f1 | Consultar licença do template e dos pacotes instalados | Assets/Settings e conteúdo inicial do template |
| Formas provisórias de cenário e herói | Código criado para este projeto, em 8 de outubro de 2026 | Sem recursos de imagem externos; textura branca e cores geradas em execução | Assets/Scripts/Prototype/PrototypeCity.cs |
| Combatentes e arena provisórios | Código criado para este projeto, em 9 de outubro de 2026 | Sem imagens externas; formas e cores desenhadas pela interface | Assets/Scripts/Prototype/PrototypeBattleHud.cs |
| Menu e cores provisórias de matérias | Código criado para este projeto, em 9 de outubro de 2026 | Sem imagens externas; caixas, botões e cores da interface. A invocação usa a apresentação provisória da batalha | Assets/Scripts/Prototype/PrototypeMateriaHud.cs |
| Abas de equipamento/consumíveis e seleção de itens em batalha | Código criado para este projeto, em 10 de outubro de 2026 | Interface provisória desenhada por código; sem imagens externas | Assets/Scripts/Prototype/PrototypeMateriaHud.cs e PrototypeBattleHud.cs |
| Interiores das três lojas e menu Comprar/Vender | Código criado para este projeto, em 10 de outubro de 2026 | Formas, cores e interface provisórias; sem imagens externas | Assets/Scripts/Prototype/PrototypeCity.cs e PrototypeShopHud.cs |
| Cidade ampliada, rio, pontes, jardins, ruínas e moradores | Código criado para este projeto, em 10 de outubro de 2026 | Geometria, cores e textura branca geradas em execução; sem imagens externas | Assets/Scripts/Prototype/PrototypeCityLayout.cs e PrototypeCity.cs |
| Ilha global, ícones pequenos de locais, florestas e interior do castelo | Código criado para este projeto, em 10 de outubro de 2026 | Blocos, cores e textura branca gerados em execução; sem imagens externas ou assets de Final Fantasy | Assets/Scripts/Prototype/PrototypeWorldLayout.cs e PrototypeCity.cs |

Para recursos gerados durante o projeto, registrar a ferramenta, a data, as alterações realizadas e as condições aplicáveis. Para recursos externos, incluir a URL da fonte e a atribuição exigida.

## Sprite do baú — 12 de outubro de 2026

`Assets/Resources/PixelArt/ChestStates.png` foi criado e depois reorientado com a ferramenta integrada `image_gen`: baú original de madeira com ferragens, fechado na célula esquerda e aberto/vazio na direita. A revisão usa o guia geométrico original `Docs/ArtInputs/ChestAxesGuide.png` para acompanhar a perspectiva das casas. O PNG RGBA final de 1774 × 887 foi copiado integralmente, sem edição posterior por outra ferramenta. SHA-256 atual: `1fca66bdb995d0bb7c92d980a98e65f41a503647210f0eb9272b1215966e4b60`; versão inicial: `349b7068a92a5b34566bb67270de8f7b430c3168d2f32c86dac09c56d9b87484`. Prompts completos em `PIXEL_ART_PROMPTS.md`. Nenhum recurso extraído de jogos ou bancos externos foi utilizado.

A Unity recorta as duas células em memória; o arquivo é importado como textura legível, Point/Clamp, sem mipmap, compressão ou redução. A escala vem do quadro fechado e é compartilhada pelos dois estados. A âncora horizontal usa a caixa, para que a tampa levantada não desloque sua base; o pivô inferior alinha cada recorte ao chão, independentemente de sua posição bruta na folha. `ObliqueScenery.Chest` monta o sprite vertical com o material de profundidade já usado pelos personagens, substituindo caixa/tampa geométricas. Sombra e colisor continuam no cenário. `ChestInteraction` acompanha o estado salvo, incluindo a volta ao quadro fechado quando outra jornada ainda não abriu o baú.

## Modelos 2.5D — 10 de outubro de 2026

Os retângulos do cenário e dos personagens foram substituídos por malhas originais de poucos polígonos, geradas por C# em execução. Casas, torres, árvores, montanhas, costa, fonte, mobiliário, baú e personagens vêm de `ObliqueScenery.cs` e `ObliqueMeshData.cs`. O material usa `Assets/Resources/ObliqueWorld.shader`, criado para este projeto, com cores por face e teste de profundidade. Nenhuma imagem ou modelo de Final Fantasy foi importado.

`ObliqueProjection.cs`, `CameraFollow.cs` e `ObliqueCharacter.cs` controlam perspectiva oblíqua, enquadramento e passos do herói. Os modelos são provisórios; texturas e acabamento artístico entram em entregas posteriores. Prévias técnicas exportadas fora do repositório são renderizações dessas malhas, não capturas do Editor.

## Poema e objetos dos segredos — 11 de outubro de 2026

Poema original de três versos em latim e textos de ambientação criados para a lenda fictícia deste projeto, em `LegendPoem.cs` e `AncientSecretInteraction.cs`. Vocabulário consultado em fontes acadêmicas listadas em `Docs/PARTE_7B.md`; o poema não é uma citação de obra histórica.

Altar, chama, monólitos, símbolos de Sol/Lua/Estrela, marcas de garras, abertura entre raízes e duas partes móveis das rochas são malhas originais geradas por código em `ObliqueScenery.cs`. Não foram adicionadas imagens, modelos ou áudios externos.

O altar agora ocupa um cenário local de ruínas, acessível por um ícone de colunas quebradas no mapa global. Piso, paredes antigas, colunas quebradas, pedras e ícone são modelos originais gerados por código em `PrototypeRuinsLayout.cs` e `ObliqueScenery.cs`, adicionados em 11 de outubro de 2026. A capela da cidade permanece como elemento de cenário.

## Vulcão, dragão e hospedaria

Trilha de basalto, lava, arena, rochas, camas e dragão são modelos originais gerados por código em `PrototypeVolcanoLayout.cs`, `PrototypeInnLayout.cs` e `ObliqueScenery.cs`. A essência usa uma pequena chama dourada. Não foram importados assets de Final Fantasy ou de terceiros.

`Assets/Resources/DragonPortrait.png` é uma imagem transparente original para a batalha, rasterizada a partir de `ObliqueScenery.DragonData()` com `Tools/Render-MeshPortrait.py`. Para regenerar: execute `Tools/Test-Oblique.ps1`, localize `DragonPortrait.json` no WorkDirectory e passe esse arquivo e o caminho do PNG ao script Python. O script requer NumPy e Pillow; o PNG salvo já permite executar a demo sem Python.

## Trono e cenas finais

Rei com coroa, espírito, castelo em ruínas e figuras dos três finais são malhas originais de `ObliqueScenery.KingData`, `SpiritData`, `RuinedCastleData` e `EndingSceneData`, adicionadas em 12 de outubro de 2026. Os textos de `EndingNarrative` foram escritos para a demo.

`Assets/Resources/KingPortrait.png` e `SpiritPortrait.png` são retratos transparentes derivados dessas malhas, usando os JSONs exportados por `Test-Oblique.ps1` e `Render-MeshPortrait.py` com a opção `--crop`. O jogo usa os PNGs prontos e não requer Python. Não foram usados modelos, imagens ou sons de terceiros.

## Pixel art — 12 de outubro de 2026

Criados com a ferramenta integrada `image_gen`: `Assets/Resources/PixelArt/ArenWalk.png`, `LiaWalk.png`, `BramWalk.png`, `NpcRoster.png` e `TerrainAtlas.png`. Personagens e materiais foram gerados para este projeto, usando a linguagem visual de FF6 original como referência. Não são assets extraídos do jogo. Prompts completos e revisões em `PIXEL_ART_PROMPTS.md`; os PNGs finais conservam seus canais originais e recortes são feitos somente em memória.

Bram e moradores receberam uma revisão de layout pela mesma ferramenta para manter cada corpo dentro de sua célula. Rascunhos e prévias técnicas ficam fora do Git. Os sprites atuais têm trajes fixos; bosses, invocações, efeitos e cenas finais ainda usam elementos anteriores.

Importadores e shaders foram configurados no projeto para preservar pixels e alpha. `PixelSpriteLayout`, `PixelSpriteLibrary`, `PixelCharacter` e `PixelSurface` foram criados em C# para esta integração. `Inspect-PixelArt.py` apenas lê/valida imagens e exporta metadados para análise; nenhuma imagem de terceiros foi usada.

Revisão do Aren, em 12 de outubro de 2026: `ArenWalk.png` foi editado com a ferramenta integrada `image_gen` para cabelo loiro pontiagudo em todas as doze poses. Roupa, quatro direções, caminhada e transparência foram conservadas. O importador/GUID continua o mesmo; 186 verificações dos atlas/importadores passaram. O prompt de edição foi adicionado a `PIXEL_ART_PROMPTS.md`. A figura geométrica provisória do Aren nos finais também recebeu cabelo dourado com pontas, em `ObliqueScenery`.

Diagonais do Aren, em 12 de outubro de 2026: `Assets/Resources/PixelArt/ArenDiagonalWalk.png` foi criado com a ferramenta integrada `image_gen`, usando a folha atual do Aren como referência para personagem, roupa, cabelo e estilo. São doze poses: quatro direções em três quartos, cada uma com repouso e dois passos. A imagem original de 1447 × 1087 pixels mantém alpha, sem edição posterior; a Unity recorta em memória. A folha cardinal foi conservada. Prompt completo em `PIXEL_ART_PROMPTS.md`.

## Periferia compacta — 12 de outubro de 2026

`Assets/Resources/PixelArt/CityMaterialsAtlas.png` e `CityDetailsAtlas.png` são atlas originais gerados com a ferramenta integrada `image_gen`, usando a imagem enviada pelo usuário como referência de atmosfera e materiais. Os PNGs têm 1448 × 1086 pixels e foram importados sem edição posterior. Não há imagens recortadas da referência no jogo. Prompts em `PERIFERIA_ART_PROMPTS.md`.

Casas, toldos, caldeira, tubos, lampiões, grelhas, vapor, ponte de pedra e fosso seco são malhas originais geradas por C# em `CityPeripheryArt`. Os letreiros, portas e janelas são painéis das fachadas com UVs locais. `PixelGlow.shader` foi criado para a transparência/aditivo da iluminação. A planta compacta foi definida em `PrototypeCityLayout`; as pontes de madeira e o lago da versão anterior foram removidos.

## Fortaleza de pedra — 12 de outubro de 2026

`Assets/Resources/PixelArt/CastleMaterialsAtlas.png` foi gerado com a ferramenta integrada `image_gen`, com materiais originais de pedra, madeira e tecido. O PNG de 1447 × 1087 foi mantido sem edição posterior. Prompt completo em `PERIFERIA_ART_PROMPTS.md`.

`CastleFortressArt.cs` gera as muralhas, torres, ameias, seteiras, arco do portão, porta reforçada, estandartes e tochas. O castelo tem material próprio de alvenaria cinza; ponte/pátio de entrada e ícone global usam o mesmo atlas. O ícone mantém a escala pequena do mapa mundial. Modelos e materiais não foram extraídos de jogos ou bibliotecas externas.

## Chão contínuo da periferia — 12 de outubro de 2026

A pintura original de 965 × 1629 pixels foi criada com a ferramenta integrada `image_gen`. A ferramenta editou um guia geométrico original da planta, usando `CityMaterialsAtlas.png` como referência de materiais e cores. O guia está em `Docs/ArtInputs/CityGroundLayout.png`; os pixels originais estão em `Docs/ArtInputs/CityGroundCore.png`. Essa pintura é a fonte da recomposição atual de `Assets/Resources/PixelArt/CityGroundMap.png`.

A pintura cobre terra, musgo, vielas, pátios, grelhas, leito do fosso e pavimento da ponte. `CityGroundArt.cs` gera as superfícies de terreno e desníveis com UVs de uma única imagem; `CityGround.shader` amostra essa imagem inteira. Casas e ponte continuam sendo volumes 3D. A arte não contém texturas ou imagens extraídas de jogos de terceiros.

## Mapa único com detalhe nativo — 12 de outubro de 2026

`Assets/Resources/PixelArt/CityGroundMap.png` tem 2305 × 2975 pixels. A composição/síntese em Python foi explicitamente autorizada pelo usuário após a geração de imagem não alcançar a resolução solicitada. O núcleo jogável ocupa o retângulo (670, 673, 965, 1629). A primeira composição preservava integralmente o piso original; a revisão atual reposiciona suas faixas de ruas, recompõe o antigo lote do padeiro como pátio livre e estende o leito rochoso por toda a largura. Ponte e trechos adicionais usam amostras nativas da mesma fonte, sem ampliar a imagem de resolução baixa. Grelhas são excluídas das amostras de síntese para não duplicar objetos desenhados.

`Tools/Build-UnifiedCityGround.py` permite reconstruir o PNG com semente fixa; `Docs/ArtInputs/CityGroundBake.json` registra dimensões e hashes. O cenário usa um material e uma textura de chão, com importação Point/Clamp sem redução/compressão. A imagem separada `CityGroundSurroundings.png` foi removida. As muretas externas deixam de ter malha visível; as colisões continuam limitando a área jogável. Modelos da fortaleza e ponte permanecem.

`Docs/ArtInputs/CityGroundCore.png` conserva a fonte original; `CityGroundRebuilt.png` conserva o núcleo da revisão atual para verificar seus pixels no PNG final. Reconstrução: `python Tools/Build-UnifiedCityGround.py --output Assets/Resources/PixelArt/CityGroundMap.png --core-output Docs/ArtInputs/CityGroundRebuilt.png`; copiar o JSON gerado ao lado do PNG para `Docs/ArtInputs/CityGroundBake.json`. Pillow e NumPy são dependências de desenvolvimento; o jogo consome apenas o PNG pronto.

## Sprite do dragão — 12 de outubro de 2026

Assets/Resources/PixelArt/Dragon.png foi criado com a ferramenta integrada image_gen: dragão original em pixel art de RPG, corpo inteiro, asas abertas e pose de batalha, com alpha transparente. Substitui a malha provisória no vulcão e o retrato na batalha; a imagem é recortada em memória e espelhada na batalha para olhar para a equipe. O PNG original permanece sem edição posterior, com filtro Point, sem mipmaps e sem compressão. DragonPortrait.png e DragonData() permanecem como arte provisória antiga e referência dos testes.

## Revisão da ilha — 12 de outubro de 2026
Ver Docs/WORLD_MAP_REVISION.md: terreno claro sem caminhos gerado com image_gen, costa rochosa inclinada, praia e espuma animada original. POIs preservados, com sombras de contato.


## Mapa aprovado — 13 de outubro de 2026
WorldGroundMap.png substituído pela pintura original gerada com image_gen a partir da opção 5. Resolução nativa 1536 × 1024, sem edição posterior. POIs aumentados 10% por pixels por unidade. Prompt e detalhes em WORLD_MAP_APPROVED.md.

## Rochedos e oceano animado — 13 de outubro de 2026
Malhas costeiras e materiais procedurais originais criados por código. WorldCoast.shader anima a água e detalha rocha/areia; WorldSurf.shader anima a espuma. Sem novos recursos externos. Detalhes em WORLD_COAST_ANIMATION.md.

## Ilha com penínsulas — 13 de outubro de 2026
Quatro pinturas originais WorldGroundNW/NE/SW/SE.png geradas com image_gen em 1536 × 1024 nativos cada. Material WorldGround.shader combina as regiões sem editar os PNGs. Nova costa irregular e montanhas ao norte, praias descontínuas, sprites +15% sobre a escala anterior, sombra projetada apenas atrás. Água animada preservada. Prompts completos e validação em WORLD_PENINSULA_FINAL.md.

## Costa plana em pixel art — 13 de outubro de 2026
WorldShoreAtlas.png gerado com image_gen, 1536 × 1024 originais, sem edição posterior. WorldCoast.shader aplica rocha e areia pintadas em planos horizontais; volumes 3D externos removidos. POIs +25% adicionais e sombra de caixa atrás/direita. Prompt e verificações em WORLD_FLAT_PIXEL_COAST.md.

## Mapa mundial contínuo — 13 de outubro de 2026
WorldGroundMap.png foi recriado inteiro em uma geração com image_gen: terreno, montanhas, costa, praia, espuma e oceano no mesmo PNG original de 1536 × 1024. Uma superfície, uma textura, mar estático pintado; quadrantes e materiais costeiros não são carregados. Guia geométrico original em ArtInputs/UnifiedWorldGuide.png. Prompt e verificações em WORLD_UNIFIED_MAP.md.

## Aparência escolhida do espírito

`Assets/Resources/PixelArt/SpiritBattle.png` usa o modelo 3 de `ArtInputs/SpiritVariants/03-espectro-glacial.png`, gerado pela ferramenta integrada `image_gen` e escolhido pelo usuário. O PNG foi copiado sem edição, redimensionamento ou mudança de alpha. Prompts completos em `ArtInputs/SpiritVariants/prompts.json`. O jogo usa essa mesma imagem inteira no castelo, no combate e no retrato de fallback; o antigo `SpiritPortrait.png` derivado de malha permanece apenas como asset histórico. A paleta azul não muda elementos, afinidades ou habilidades.

## Retratos ilustrados de conversa — 16 de outubro de 2026

Onze PNGs originais em `Assets/Resources/PixelArt/DialoguePortraits`, gerados separadamente com o `image_gen` integrado: Aren, aldeão, camponesa, jardineiro, exploradora, guardião da pedreira, viajante, hospedeira, tirano, dragão corrompido e essência liberta. Pixel art com proporções mais realistas; roupas e cabelos preservam a identidade de `NpcRoster`, `ArenWalk` e `Dragon`. Cada personagem tem direção própria de fisionomia, idade, expressão, silhueta e pose; as referências visuais fornecem apenas identidade e traje. Sem imagens extraídas de outros jogos ou bancos externos.

Os arquivos gerados são copiados integralmente, mantendo RGBA e transparência. Importação Point/Clamp, sem mipmap, compressão, redução ou cópia legível em CPU. `DialoguePortraitCatalog` faz a associação de personagens e `DialoguePresentation` desenha os PNGs inteiros. Prompts completos, referências e SHA-256 de cada original em `Docs/ArtInputs/DialoguePortraits/prompts.json`; documentação em `Docs/DIALOGUE_PORTRAITS.md`.

## Final de possessão — 16 de outubro de 2026

`Assets/Resources/PixelArt/Endings/PossessedThrone.png` foi gerado pelo `imagegen` integrado para o epílogo de Aren possuído, usando seu retrato de conversa e a sala do trono como referências de identidade, estilo e arquitetura. PNG original de 1024 × 1536 copiado integralmente, Point/Clamp, sem mipmaps, compressão ou redimensionamento. SHA-256: `59329F8C215BBE4639153521D088ED21C4E6F4EF0B0CD6AB47291709AD689855`. Prompt completo em `ArtInputs/PossessedThrone.prompt.txt`.

A silhueta reutiliza `PixelArt/SpiritBattle`, já escolhido pelo usuário, com composição e iluminação feitas pelo shader original `PossessedThroneComposite`. O fundo, os pixels de Aren e os arquivos aprovados não são editados. Detalhes e roteiro em `HERO_POSSESSED_ENDING.md`.

## Fantasma frontal e raios do final de possessão — 16 de outubro de 2026

`Assets/Resources/PixelArt/Endings/PossessedSpiritFront.png`, `PossessionLightningA.png` e `PossessionLightningB.png` são três gerações separadas do `imagegen` integrado, com transparência real. Os PNGs de 1024 × 1536 foram copiados integralmente, sem editar pixels, alpha ou resolução; Point/Clamp, sem compressão ou mipmaps. O novo fantasma conserva a máscara e as chamas azuis do espírito, em pose reta de frente e com o acabamento sério dos retratos. Os dois raios têm ramificações diferentes e alternam A/B nos pulsos de cada clarão.

Prompts completos, referências e hashes SHA-256 em `Docs/ArtInputs/PossessionSprites.prompts.json`. O quadro final sobe para mostrar o rosto inteiro acima do trono e amplia o fundo preto por composição. O PNG `SpiritBattle.png` não foi alterado. Ver `Docs/HERO_POSSESSED_ENDING.md`.

## Tempestade e cores naturais do espírito — 16 de outubro de 2026

`PossessionLightningC.png` e `PossessionLightningD.png`, em `Assets/Resources/PixelArt/Endings`, acrescentam os canais duplo e diagonal aos quadros A/B existentes. Foram gerados separadamente com o `imagegen` integrado e copiados integralmente, com alpha real e resolução nativa de 1024 × 1536. Importação Point/Clamp, sem compressão, redução ou mipmaps. Prompts, referências e SHA-256 em `Docs/ArtInputs/PossessionStorm.prompts.json`.

A animação agora sorteia posições, tamanhos, inclinações, espelhamento e rajadas de 3–5 piscadas rápidas, usando as quatro formas no fundo preto. O shader preserva o RGB original de `PossessedSpiritFront.png`, com opacidade máxima de 76%; o PNG do espírito não foi editado. Detalhes em `Docs/HERO_POSSESSED_ENDING.md`.
