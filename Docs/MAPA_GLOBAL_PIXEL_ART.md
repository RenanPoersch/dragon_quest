# Mapa global — ilha e POIs em pixel art

A ilha tem contorno oval irregular, com enseadas suaves, cercada pelo mar. Chão e trilhas usam uma pintura contínua de 1536 × 1024; os cinco locais usam sprites próprios na mesma direção artística da periferia. A câmera inclinada conserva o zoom de 6,85 e o acompanhamento do herói.

| Local | Posição global | Entrada |
| --- | --- | --- |
| Castelo | (0; 0) | Periferia, como antes |
| Pedreira dos monólitos | (−8; 5), noroeste | Interior da antiga floresta oeste |
| Floresta leste | (8; −2,5) | Interior da floresta leste |
| Ruínas | (−8; −6), sudoeste | Altar da lenda |
| Vulcão | (8; 6), exatamente oposto às ruínas | Caminho de lava e dragão, após abrir os selos |

O interior da pedreira ainda conserva as árvores e os monólitos da versão anterior. Esta entrega altera seu ícone e localização global. IDs dos mapas e segredos foram conservados para os saves existentes.

## Barreira do vulcão

A cordilheira e as rochas da passagem foram retiradas do mapa global. Uma barreira translúcida circular, com faixas e fragmentos de runas, envolve o vulcão. Seu raio visual e físico é 2,45; bloqueia aproximações por todos os lados.

O ponto de interação fica ao sul da barreira, em (8; 2,5). Use E com os três segredos concluídos: a Escama ancestral desfaz os selos, o colisor desliga e a barreira se dissipa em 0,8 s. A escama permanece no inventário. Caminhar pela entrada do vulcão passa a trocar de cenário automaticamente. Ao carregar, barreira e colisor recuperam juntos o estado aberto ou fechado.

## Assets e importação

| Arquivo | Uso |
| --- | --- |
| Assets/Resources/PixelArt/WorldGroundMap.png | Pintura opaca do terreno e caminhos, mapeada uma única vez na ilha |
| Assets/Resources/PixelArt/WorldPoiSprites.png | Castelo, pedreira, floresta, ruínas e vulcão, com transparência |
| Docs/ArtInputs/WorldGroundGuide.png | Guia original das posições das trilhas, usado só na geração |
| Docs/ArtInputs/WorldPoiGuide.png | Guia original de composição dos sprites, usado só na geração |
| Docs/ArtInputs/WorldArtGeneration.json | Prompts, referências, dimensões, janelas e hashes das imagens finais |

As duas pinturas foram geradas com a ferramenta integrada image_gen e copiadas integralmente para o projeto. Nenhum PNG gerado foi editado em Python. Os ícones usam cinco janelas de origem adaptadas à disposição real da folha; o recorte ocorre em memória, sem perder partes das silhuetas. O importador usa Point/Clamp, sem mipmap, compressão ou redimensionamento. O chão reutiliza CityGround.shader; os POIs usam o mesmo material com teste de profundidade dos personagens. A barreira usa PixelGlow, sem luz dinâmica ou novo shader.

A malha e o colisor da costa compartilham o mesmo contorno. O retorno das ruínas fica mais para dentro da ilha, fora da área de entrada. Ao carregar saves globais antigos, uma posição que passou a ficar no mar ou dentro de um POI volta à chegada livre; uma posição dentro da barreira fechada volta ao ponto de exame. Progresso, ouro e objetos continuam no mesmo save.

## Conferir em Play

1. Pare Play, aguarde a importação dos PNGs/scripts e inicie novamente. Confira ausência de erros vermelhos ou materiais magenta.
2. Na ilha, confira o chão em pixel art e os novos ícones, sem labels ou marcações de entrada. Ande junto à costa, incluindo diagonais: o herói deve permanecer em terra.
3. Visite pedreira a noroeste, floresta a leste e ruínas a sudoeste. As entradas continuam automáticas; os retornos devem ficar em terra e não causar ciclos de entrada/saída.
4. Examine a barreira no acesso ao sul do vulcão. Tente aproximar-se também pelos lados: o bloqueio deve acompanhar o círculo.
5. Após os três segredos, use E no acesso e confira a dissipação da barreira, a entrada automática no vulcão e o retorno livre.
6. Salve manualmente na hospedaria e recarregue jornadas com os selos fechados e abertos; confira a barreira e a passagem correspondente.

Compilação e testes passaram: 603 verificações de sprites/movimento, 6.517 de perspectiva/câmera/malhas, 318 de PNGs/importadores, 73 de caminhos da cidade, 358 de viagens/interiores e 113 de salvamento. Prévias técnicas de toda a ilha, chegada, ruínas e vulcão foram inspecionadas fora da Unity. Elas não confirmam renderização ou física nativa; esses pontos usam o roteiro em Play acima.
