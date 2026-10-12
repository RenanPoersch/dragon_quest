# Periferia compacta — vielas e ponte do castelo

A periferia foi redesenhada a partir da referência enviada pelo usuário: bairro antigo de fantasia industrial, construções próximas, becos, metal oxidado, madeira, lonas remendadas e luzes amarelas/verdes. A apresentação mantém os cenários 3D, a câmera inclinada e os personagens em pixel art.

## Planta e passagens

- Bairro de 26 × 44 unidades, alongado de sul para norte, com 22 construções. A antiga planta de 72 × 48 foi substituída por uma viela central sinuosa e acessos curtos às fachadas.
- Entrada da ilha ao sul. Hospedaria e equipamentos ficam primeiro; itens e matérias ficam mais adiante. Camponesa no lado oeste, exploradora no beco leste e jardineiro no pequeno jardim entre casas.
- Lago, rio e três pontes de madeira foram retirados. Há uma única ponte de pedra ao norte, sobre um fosso seco, com parapeitos e passagem até o portão do castelo.
- Fortaleza de pedra com muralha central mais larga, duas torres robustas, torre de menagem recuada, ameias, seteiras, estandartes e grande portão em arco. Torres chegam a 8,02 unidades de altura; as construções comuns têm aproximadamente 3–4 unidades.
- Chegar ao portão depois da ponte troca automaticamente para o interior do castelo. Lojas e hospedaria também mantêm entrada automática; retornos ficam fora das áreas de passagem.
- Não há nomes flutuantes de lugares. ITENS, MATERIA, ARMAS e POUSADA aparecem em letreiros físicos presos às fachadas/toldos.

`PrototypeCityLayout` centraliza a planta, portas, retornos e moradores. `PrototypeTravel` usa essas coordenadas; o ícone global do castelo e os interiores mantêm suas conexões e IDs.

## Arte e integração

| Arquivo | Conteúdo |
| --- | --- |
| Assets/Resources/PixelArt/CityMaterialsAtlas.png | Doze materiais gastos: musgo, terra, pavimento, areia, madeira, paredes, metal ondulado, ferro, folhagem, água, vidro e lona |
| Assets/Resources/PixelArt/CityDetailsAtlas.png | Letreiros, portas, janelas, ventilação, brasão fictício e grelha |
| Assets/Resources/PixelArt/CastleMaterialsAtlas.png | Alvenaria cinza, granito, pavimento, pedras de remate, porta de carvalho e estandartes |
| Assets/Scripts/Prototype/CityPeripheryArt.cs | Geometria das casas, caldeira da loja de itens, fachadas, viela, ponte, fosso, tubulações, lampiões e vapor |
| Assets/Scripts/Prototype/CastleFortressArt.cs | Fortaleza de pedra, ameias, arco do portão, seteiras, estandartes e ícone miniatura |
| Assets/Resources/PixelGlow.shader | Luzes no chão e vapor com transparência/aditivo e teste de profundidade |

Os dois atlas da periferia têm 1448 × 1086; o atlas do castelo tem 1447 × 1087. Foram gerados pela ferramenta integrada image_gen e mantidos sem alteração. Importadores usam Point, nenhuma compressão, mipmap ou redimensionamento NPOT. Prompts completos em `PERIFERIA_ART_PROMPTS.md`; origem em `ASSETS.md`.

A periferia usa materiais próprios. Os painéis recebem UVs locais para mostrar uma célula inteira, sem repetir letras. Sombras continuam separadas; luzes e vapor usam material próprio. O shader do cenário usa brilho padrão 1, com 0,8 nas casas e 0,95 na fortaleza, ponte/pátio e ícone do castelo. O ícone global tem ameias e mantém altura de 1,95, um pouco acima do herói.

A muralha central tem planta de 9,5 × 4,5, com duas torres de 3,4 × 3,6. O portão permanece alinhado ao fim da ponte e à passagem automática existente. O castelo tem terraços e ameias de pedra; as antigas coberturas industriais foram substituídas. Não houve mudança no zoom da câmera.

O terreno visual é interrompido no fosso para mostrar a profundidade; colliders impedem andar fora da ponte. A ponte tem piso na altura do caminho e parapeitos sólidos. Tubulações cruzam a rua acima do personagem. Pequenos lampiões decorativos não adicionam obstáculos à navegação.

## Jornadas anteriores

IDs de mapas, lojas, baú, inventário e segredos foram conservados. Saves anteriores do mapa amplo continuam carregáveis. Se a coordenada antiga ficou fora do bairro ou passou a coincidir com casa, fosso, morador ou outro obstáculo, a equipe reaparece na rua central. Uma posição livre no mapa atual permanece na mesma coordenada. Não é necessário reiniciar a jornada.

## Verificação

Após a revisão do castelo, passaram 249 verificações dos nove PNGs/importadores, 161 de sprites/materiais, 2116 de malhas/câmera, 73 de caminhos da periferia, 269 de viagens/interiores e 110 de salvamento. A busca por caminhos usa amostragem de 0,125 unidade para examinar as vielas estreitas com margem além do collider do herói. Toda área caminhável amostrada está conectada, incluindo moradores, portas, retornos e ponte.

Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1. ObliqueWorld e PixelGlow compilaram externamente nos estágios de vértices/fragmentos com d3dcompiler_47 e os headers URP, sob as mesmas adaptações descritas em VALIDATION.md. Esse teste não substitui o renderer DirectX 12 da Unity.

Prévias técnicas da entrada, mercado, bairro da capela e ponte foram renderizadas a partir das malhas e texturas e inspecionadas fora do Editor. Não são capturas da Unity. A inspeção permitiu ajustar a visibilidade dos letreiros e impedir que o piso escondesse o fosso.

A fortaleza teve prévias geral e junto ao portão inspecionadas. A vista geral usa enquadramento de apresentação para mostrar toda a construção; a vista próxima usa o enquadramento do jogo. Torres altas podem ocupar mais que a altura da tela ao chegar ao portão.

## Conferência em Play

1. Pare Play, aguarde a importação/compilação e abra CidadePrototipo. Continue seu slot ou inicie uma jornada. Confira o Console sem novos erros.
2. Entre pelo ícone global do castelo. A chegada deve mostrar o bairro compacto, com pousada/armas próximas e sem lago, rio ou pontes de madeira.
3. Siga a viela, entre nas três lojas e na hospedaria e retorne. Confira portas, letreiros, corpo do herói e ruas entre as casas, inclusive ao andar nas diagonais.
4. Converse com os moradores, consulte o poema e abra o baú se ainda não tiver sido coletado. Verifique que progresso, ouro e estoque foram conservados.
5. Siga ao norte, atravesse a ponte de pedra e caminhe até a porta central do castelo. O cenário deve mudar automaticamente. Saia do interior e confira o retorno ao lado norte da ponte, sem um ciclo de entrada/saída.
6. Confira profundidade dos telhados, sombras, letreiros e luzes, e aparência da textura durante a caminhada. GPU, interação e fluidez no Editor ainda precisam desta conferência.
