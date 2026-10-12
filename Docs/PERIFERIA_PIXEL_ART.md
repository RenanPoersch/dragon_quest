# Periferia compacta — vielas e ponte do castelo

A periferia foi redesenhada a partir da referência enviada pelo usuário: bairro antigo de fantasia industrial, construções próximas, becos, metal oxidado, madeira, lonas remendadas e luzes amarelas/verdes. A apresentação mantém os cenários 3D, a câmera inclinada e os personagens em pixel art.

## Planta e passagens

- Bairro de 26 × 44 unidades, alongado de sul para norte, com 22 construções. A antiga planta de 72 × 48 foi substituída por uma viela central sinuosa e acessos curtos às fachadas.
- Entrada da ilha ao sul. Hospedaria e equipamentos ficam primeiro; itens e matérias ficam mais adiante. Camponesa no lado oeste, exploradora no beco leste e jardineiro no pequeno jardim entre casas.
- Lago, rio e três pontes de madeira foram retirados. Há uma única ponte de pedra ao norte, sobre um fosso seco, com parapeitos e passagem até o portão do castelo.
- Chegar ao portão depois da ponte troca automaticamente para o interior do castelo. Lojas e hospedaria também mantêm entrada automática; retornos ficam fora das áreas de passagem.
- Não há nomes flutuantes de lugares. ITENS, MATERIA, ARMAS e POUSADA aparecem em letreiros físicos presos às fachadas/toldos.

`PrototypeCityLayout` centraliza a planta, portas, retornos e moradores. `PrototypeTravel` usa essas coordenadas; o ícone global do castelo e os interiores mantêm suas conexões e IDs.

## Arte e integração

| Arquivo | Conteúdo |
| --- | --- |
| Assets/Resources/PixelArt/CityMaterialsAtlas.png | Doze materiais gastos: musgo, terra, pavimento, areia, madeira, paredes, metal ondulado, ferro, folhagem, água, vidro e lona |
| Assets/Resources/PixelArt/CityDetailsAtlas.png | Letreiros, portas, janelas, ventilação, brasão fictício e grelha |
| Assets/Scripts/Prototype/CityPeripheryArt.cs | Geometria das casas, caldeira da loja de itens, fachadas, viela, ponte, fosso, tubulações, lampiões e vapor |
| Assets/Resources/PixelGlow.shader | Luzes no chão e vapor com transparência/aditivo e teste de profundidade |

As imagens de 1448 × 1086 foram geradas pela ferramenta integrada image_gen e mantidas sem alteração. Importadores usam Point, nenhuma compressão, mipmap ou redimensionamento NPOT. Prompts completos em `PERIFERIA_ART_PROMPTS.md`; origem em `ASSETS.md`.

A periferia usa materiais próprios. Os painéis recebem UVs locais para mostrar uma célula inteira, sem repetir letras. Sombras continuam separadas; luzes e vapor usam material próprio. O shader do cenário ganhou um multiplicador de brilho, com padrão 1 e valor 0,8 apenas nos volumes da periferia. Ilha e outros interiores conservam seus materiais.

O terreno visual é interrompido no fosso para mostrar a profundidade; colliders impedem andar fora da ponte. A ponte tem piso na altura do caminho e parapeitos sólidos. Tubulações cruzam a rua acima do personagem. Pequenos lampiões decorativos não adicionam obstáculos à navegação.

## Jornadas anteriores

IDs de mapas, lojas, baú, inventário e segredos foram conservados. Saves anteriores do mapa amplo continuam carregáveis. Se a coordenada antiga ficou fora do bairro ou passou a coincidir com casa, fosso, morador ou outro obstáculo, a equipe reaparece na rua central. Uma posição livre no mapa atual permanece na mesma coordenada. Não é necessário reiniciar a jornada.

## Verificação

Passaram 243 verificações dos oito PNGs/importadores, 161 de sprites/materiais, 2107 de malhas/câmera, 73 de caminhos da periferia, 269 de viagens/interiores e 110 de salvamento. A busca por caminhos usa amostragem de 0,125 unidade para examinar as vielas estreitas com margem além do collider do herói. Toda área caminhável amostrada está conectada, incluindo moradores, portas, retornos e ponte.

Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1. ObliqueWorld e PixelGlow compilaram externamente nos estágios de vértices/fragmentos com d3dcompiler_47 e os headers URP, sob as mesmas adaptações descritas em VALIDATION.md. Esse teste não substitui o renderer DirectX 12 da Unity.

Prévias técnicas da entrada, mercado, bairro da capela e ponte foram renderizadas a partir das malhas e texturas e inspecionadas fora do Editor. Não são capturas da Unity. A inspeção permitiu ajustar a visibilidade dos letreiros e impedir que o piso escondesse o fosso.

## Conferência em Play

1. Pare Play, aguarde a importação/compilação e abra CidadePrototipo. Continue seu slot ou inicie uma jornada. Confira o Console sem novos erros.
2. Entre pelo ícone global do castelo. A chegada deve mostrar o bairro compacto, com pousada/armas próximas e sem lago, rio ou pontes de madeira.
3. Siga a viela, entre nas três lojas e na hospedaria e retorne. Confira portas, letreiros, corpo do herói e ruas entre as casas, inclusive ao andar nas diagonais.
4. Converse com os moradores, consulte o poema e abra o baú se ainda não tiver sido coletado. Verifique que progresso, ouro e estoque foram conservados.
5. Siga ao norte, atravesse a ponte de pedra e caminhe até a porta central do castelo. O cenário deve mudar automaticamente. Saia do interior e confira o retorno ao lado norte da ponte, sem um ciclo de entrada/saída.
6. Confira profundidade dos telhados, sombras, letreiros e luzes, e aparência da textura durante a caminhada. GPU, interação e fluidez no Editor ainda precisam desta conferência.
