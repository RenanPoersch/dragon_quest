# Arquivo de recursos de arte

Versões anteriores conservadas para autoria e referência, fora de `Assets` e do conjunto de recursos usado pelo jogo. Os arquivos e seus `.meta` foram movidos sem alteração de conteúdo em 30/10/2026.

## Conteúdo e substituições

| Pasta | Arquivos | Motivo do arquivamento |
| --- | --- | --- |
| `World` | `WorldGroundNE.png`, `WorldGroundNW.png`, `WorldGroundSE.png`, `WorldGroundSW.png` | As quatro pinturas regionais deram lugar à pintura contínua `WorldGroundMap.png`. |
| `World` | `WorldShoreAtlas.png`, `WorldCoast.shader`, `WorldSurf.shader` | O mundo atual usa `WorldGround.shader` com a pintura contínua; não carrega os antigos materiais costeiros. |
| `Battle` | `ArenMeteorAbsorb.png` | A absorção atual usa `ArenMeteorDraconic.png`. |
| `Endings` | `ElderEndingWalk.png`, `ScorchedEarth.png` | As apresentações atuais usam as artes de discurso/prosperidade e de castelo destruído/ancião sentado, sem carregar estas duas versões. |
| `Ultimates` | `BramCataclysm.png`, `BramCataclysmFrames.json`, `BramCatastrophe.png`, `BramCatastropheFrames.json` | O catálogo atual de atlas de Bram usa `BramBastion`, `BramShieldImpact` e `BramBarrier`. |

## Conferência de referências

Antes do movimento, foram conferidos nomes no código de runtime, ferramentas, arquivos JSON e configurações serializadas, além dos GUIDs em `Assets` e `ProjectSettings`. Nenhum GUID desses recursos tinha consumidor além de seu próprio `.meta`.

As únicas ocorrências técnicas restantes do recurso de absorção antigo eram uma verificação que exige que ele não seja desenhado. As declarações dos próprios shaders também contêm seus nomes; não eram chamadas de carregamento.

Referências em documentos e prompts históricos são registros de autoria e não consumidores do jogo. A documentação histórica pode apontar para o caminho de origem registrado no manifesto.

`SpiritChainsCaster.png` e `SpiritSoulStealOrb.png` continuam em `Assets/Resources/PixelArt/SpiritBoss`: são usados pelos campos `casterResource` e `effectResource` dos JSON de animação e não foram movidos.

## Integridade

São 14 recursos mais seus 14 arquivos `.meta`, totalizando 28 arquivos e 27.290.665 bytes (26,03 MiB). Todos os tamanhos e hashes SHA-256 foram comparados antes e depois do movimento, sem divergências.

O [manifesto](manifest.json) registra o caminho original, o caminho arquivado, o tamanho, o SHA-256 e o GUID de cada arquivo. Os `.meta` ficam conservados junto aos originais para permitir uma eventual restauração com a mesma identidade de asset.

O tamanho informado é o dos arquivos preservados, sem representar uma medição de economia no executável ou na memória.
