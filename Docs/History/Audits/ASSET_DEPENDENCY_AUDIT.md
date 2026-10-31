> Registro histórico de implementação. As regras e instruções vigentes estão em [Docs/Current](../../Current/README.md).

# Assets e dependências — 30/10/2026

A revisão conferiu referências de código, GUIDs em assets e metadados, componentes nativos nas cenas e o grafo de dependências instalado. A cena de entrada continua sendo `Assets/Scenes/Game.unity`, com o mesmo GUID e perfil de build Windows.

## Limpeza aplicada

- Retirados `SampleScene`, a cena/template Lit 2D, a pasta `Welcome` e a configuração antiga do tutorial.
- Retirado `Resources/SpiritPortrait.png`, sem leitura pelo jogo. O espírito atual continua usando `PixelArt/SpiritBattle.png`; o exportador histórico conserva sua geometria fora do runtime.
- Retiradas as pastas vazias `Art`, `Audio`, `Data` e `Prefabs`, junto de seus metadados. A exclusão foi concluída após fechar o Editor.
- Preservados os backups em `Assets/_Recovery`, os shaders, pinturas, sprites, fontes, sons e os caminhos atuais de Resources.

São 27 arquivos retirados, aproximadamente 4,57 MiB do projeto. Esse valor inclui conteúdo de Editor e não equivale a uma redução medida no executável.

## Dependências

Foram removidas 11 dependências diretas sem uso: Animation 2D, Aseprite, PSD Importer, SpriteShape, Tilemap, Tilemap Extras, 2D Tooling, Version Control/Collab Proxy, Timeline, Visual Scripting e o framework de tutoriais. As referências restantes encontradas pertenciam ao próprio material de template retirado. Não há fontes PSD/PSB/Aseprite, animações por bones, grafos visuais, timelines ou componentes de tilemap no conteúdo mantido.

O lock passou de 47 para 29 entradas. Foram preservadas as versões dos pacotes mantidos, incluindo URP, Input System, Sprite, UGUI, integrações de IDE e infraestrutura de validação. O lock conserva apenas dependências alcançáveis pelo manifesto e suas profundidades foram recalculadas.

Os módulos nativos `audio`, `physics2d`, `imgui`, `imageconversion` e `jsonserialize`, versão `1.0.0`, foram declarados diretamente. O jogo usa essas APIs e não deve depender da instalação de um pacote de autoria para mantê-las disponíveis.

## Música e tamanho

| Conteúdo fonte | Quantidade/tamanho |
| --- | --- |
| Música | 19 WAVs; 572,24 MiB |
| Pixel art em Resources | 258,74 MiB |

Todas as músicas e suas configurações foram preservadas: streaming, PCM, sample rate original e sem preload. A compressão não foi alterada nesta limpeza. Os valores acima medem arquivos fonte, não RAM ou tamanho de um player. Não há novo build de player nesta etapa para comparar o resultado importado; a verificação em Unity isolado da etapa anterior ficou limitada pela inicialização da licença.

## Limites da revisão

As referências retiradas não aparecem no conteúdo mantido. A validação inclui compilação do runtime e do Editor, conferência dos includes de shaders e consistência do grafo com os manifestos dos pacotes instalados. Reimportação e build completos ainda precisam de uma instância do Unity com a licença inicializada. O menu de cheats permanece com o comportamento aprovado.
