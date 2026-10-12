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
