> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Exterior do castelo — 17/10/2026

Os contrafortes antigos terminavam no mesmo plano da fachada. O piso do terraço e os quatro parapeitos também repetiam faces na mesma profundidade, inclusive nos cantos. A câmera em movimento fazia essas faces disputarem o depth buffer (z-fighting).

Os contrafortes agora têm relevo. Cada terraço é uma casca única, com piso rebaixado, faces internas/externas e topo do parapeito sem sobreposição. As ameias começam no topo do parapeito, e o terraço da menagem começa exatamente onde a parede termina.

As duas tochas do portão têm placa de fixação, braço de ferro, cabo e cesto com aro de metal. O fogo usa `RuinsAltarFlame` e `RuinsFlameAnimation`, com os oito quadros a 10 fps. Os sprites ficam verticais, com profundidade real e base ancorada ao cesto. Uma defasagem de três quadros evita que as tochas queimem em sincronia e é preservada ao reativar a cidade. O altar conserva a fase inicial padrão de zero.

## Validação

- `Tools/Test-CastleExterior.ps1`: 17 verificações aprovadas; compila todos os scripts do jogo e verifica cada par de triângulos opacos do castelo e das duas torres. O teste reproduziu a falha antes da correção (triângulos 14/34; área sobreposta aproximada de 0,0361). Inclui uma geometria de controle que reproduz o contraforte antigo e distingue arestas compartilhadas de áreas sobrepostas.
- `Tools/Dragon Quest/Validate Castle Exterior`, no Editor: 41 verificações nativas aprovadas, incluindo oito quadros por tocha, posição fixa, orientação vertical, profundidade, filtro Point, fases distintas e reativação. Gera 12 capturas em `Temp/CastleExteriorPreview`.
- `Tools/Test-Ruins.ps1`: compilação e regressão de chama, movimento, escadas, câmera, save e retorno aprovadas.
- Inspeção das capturas nativas da vista geral e do portão aprovada na Unity 6000.6.5f1 / DirectX 12.
- `Test-Oblique.ps1` continua parando na expectativa antiga de câmera fixa, já documentada em `PERIFERIA_PIXEL_ART.md`. Essa asserção pertence a outro ambiente; não foi alterada nesta correção.

Para conferir em Play, reinicie a execução e atravesse a ponte: observe a fachada e as ameias enquanto caminha nas diagonais e junto às laterais. As duas tochas devem animar com as bases presas aos cestos. As capturas nativas não substituem esse percurso interativo completo.
