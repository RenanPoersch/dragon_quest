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

`Assets/Resources/PixelArt/CityGroundMap.png` é uma pintura original e opaca de 965 × 1629 pixels, criada com a ferramenta integrada `image_gen`. A ferramenta editou um guia geométrico original da planta, usando `CityMaterialsAtlas.png` como referência de materiais e cores. O guia está em `Docs/ArtInputs/CityGroundLayout.png`; o prompt completo está em `PERIFERIA_ART_PROMPTS.md`. O PNG final foi copiado sem edição posterior, confirmado pela igualdade do SHA-256 com a saída original.

A pintura cobre toda a periferia, em vez de repetir células de materiais: terra, musgo, vielas, pátios, grelhas, leito do fosso e pavimento da ponte. `CityGroundArt.cs` gera quatro superfícies com UVs normalizados da planta; `CityGround.shader` amostra a imagem inteira. Casas e paredes continuam sendo volumes 3D. A arte não contém texturas ou imagens extraídas de jogos de terceiros.

## Extensão visual do terreno — 12 de outubro de 2026

`Assets/Resources/PixelArt/CityGroundSurroundings.png` é uma pintura opaca de terra e musgo de 1105 × 1424 pixels, criada com a ferramenta integrada `image_gen`. O PNG final foi conservado sem edição posterior, com SHA-256 igual à saída original. A edição usou uma referência de expansão gerada a partir do piso existente e removeu todas as ruas/fundações da referência. Prompts completos em `PERIFERIA_ART_PROMPTS.md`.

A nova imagem aparece apenas nas quatro superfícies exteriores, num terreno visual de 62 × 80 unidades. O piso central original continua intacto, assim como a planta de movimento de 26 × 44. As duas pinturas usam o shader de chão existente e importação Point/Clamp, sem mipmaps/compressão/redimensionamento. Não há repetição da cidade na faixa exterior.
