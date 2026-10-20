# Periferia compacta — vielas e ponte do castelo

## Terreno cobrindo todo o enquadramento

A área jogável continua com 26 × 44 unidades, limitada por colisores invisíveis. O terreno visual conserva aproximadamente 62,10 × 80,36 unidades e a mesma calibração de UVs. A pintura atual `CityGroundMap.png` tem **2688 × 3472 pixels nativos**, acima dos 2305 × 2975 da pintura original. A planta, os modelos, a posição e os materiais de todas as casas permanecem idênticos, conferidos por SHA-256. Os valores antigos em `PrototypeCityLayout` continuam como calibração normalizada da planta, e não como limite da resolução da nova imagem.

A geração integrada de imagens produziu doze trechos de **1254 × 1254** pixels, reduzidos para seus espaços de 1024 × 1024 e unidos em coordenadas fixas por costuras de menor erro. Nenhuma fonte foi ampliada. A nova arte remove os lotes escuros retangulares e dá bordas irregulares aos caminhos e à vegetação. A imagem completa montada está em `Docs/ArtInputs/CityGroundNatural.png`; o conjunto de prompts está em `CityGroundNatural.prompt.txt` e as dimensões, posições, hashes dos trechos selecionados e hashes dos arquivos preservados estão em `CityGroundNativeGeneration.json`. As fontes individuais e prévias de trabalho permanecem na pasta local `terrain-native-task` do chat.

`python Tools/Build-UnifiedCityGround.py --output Assets/Resources/PixelArt/CityGroundMap.png --core-output Docs/ArtInputs/CityGroundRebuilt.png` verifica a resolução e o manifesto e copia a pintura pronta **byte a byte**, sem redimensionar, deformar, deslocar os caminhos ou aplicar correção de UV. O manifesto `CityGroundBake.json` registra a resolução efetiva. A fonte de 1104 × 1425 ampliada e seu remapeamento, usados na primeira tentativa, foram substituídos. A luz refletida segue os letreiros reais: MATERIA ciano/turquesa, ITENS amarelo, ARMAS laranja e POUSADA âmbar. Toda essa luz está integrada à textura.

O foco da câmera acompanha diretamente o personagem e fica limitado ao retângulo da cidade. O cálculo da cobertura considera rotação, inclinação, altura do foco e rebaixo do fosso. Formatos de 9:16 até 21:9 conservam o tamanho 6; em formatos mais largos, o tamanho é limitado ao que a faixa de terreno consegue cobrir. Essa regra está ativa somente na periferia.

Em Play, parar e reiniciar após a importação. Conferir aproximação dos quatro limites e dos cantos, testar 16:9 e um formato largo no Game view, e entrar/sair de uma loja e do castelo. Toda a tela deve continuar coberta por terreno/cenário, sem mudança da escala dos detalhes ao cruzar o antigo limite visual. O herói deve parar no limite da área jogável, sem mureta desenhada. Ponte e fortaleza conservam seus elementos de pedra. Prévias externas e testes de raios sobre a geometria não substituem essa conferência no renderer da Unity.

A periferia foi redesenhada a partir da referência enviada pelo usuário: bairro antigo de fantasia industrial, construções próximas, becos, metal oxidado, madeira, lonas remendadas e luzes amarelas/verdes. A apresentação mantém os cenários 3D, a câmera inclinada e os personagens em pixel art.

## Planta e passagens

- Bairro de 26 × 44 unidades, alongado de sul para norte, com 22 construções. As fileiras foram afastadas horizontalmente, abrindo a rua central e conservando os acessos às fachadas. A casa do padeiro fica na fileira externa leste, deixando livre o pátio da loja de armas.
- Entrada da ilha ao sul. Hospedaria e equipamentos ficam primeiro; itens e matérias ficam mais adiante. Camponesa no lado oeste, exploradora no beco leste e viajante no pequeno jardim entre casas. O jardineiro fica na clareira da Floresta Leste.
- O viajante fica em (8,45; 7,8), um pouco acima e à direita do ponto anterior. Ao concluir sua conversa sobre a pedra e o objeto vermelho, entrega a matéria Phoenix, toca o som de aceitar e abre a caixa azul central “Recebeu Phoenix”. A entrega acontece uma única vez por jornada e persiste no save, mesmo se a matéria for vendida.
- Lago, rio e três pontes de madeira foram retirados. Há uma única ponte de pedra ao norte, sobre um fosso seco, com parapeitos e passagem até o portão do castelo. O leito e suas paredes se prolongam por toda a largura do terreno visual; não terminam na borda da cidade caminhável.
- Fortaleza de pedra com muralha central mais larga, duas torres robustas, torre de menagem recuada, ameias, seteiras, estandartes e grande portão em arco. Torres chegam a 8,02 unidades de altura; as construções comuns têm aproximadamente 3–4 unidades.
- Chegar ao portão depois da ponte troca automaticamente para o interior do castelo. Lojas e hospedaria também mantêm entrada automática; retornos ficam fora das áreas de passagem.
- Passagens não têm marcações no chão. As áreas das três lojas e da hospedaria ficam centradas nos painéis das portas, com faixa acessível pela frente. Caminhar junto a uma janela não entra na loja. A saída sul cobre toda a largura caminhável do bairro; basta alcançar a faixa inferior para voltar à ilha.
- Não há nomes flutuantes de lugares. ITENS, MATERIA, ARMAS e POUSADA aparecem em letreiros físicos presos às fachadas/toldos.
- O baú fica parcialmente atrás do canto da oficina das lanternas, em (2; −2,5). A exploradora está na rua central, em (0,4; 1,7), visível por inteiro. O painel fixo de título/controles/missão e a faixa de dicas gerais foram retirados. O progresso dos segredos pode ser consultado em I → Itens após a primeira descoberta.

`PrototypeCityLayout` centraliza a planta, portas, retornos e moradores. `PrototypeTravel` usa essas coordenadas; o ícone global do castelo e os interiores mantêm suas conexões e IDs.

## Arte e integração

| Arquivo | Conteúdo |
| --- | --- |
| Assets/Resources/PixelArt/CityMaterialsAtlas.png | Doze materiais gastos: musgo, terra, pavimento, areia, madeira, paredes, metal ondulado, ferro, folhagem, água, vidro e lona |
| Assets/Resources/PixelArt/CityDetailsAtlas.png | Letreiros, portas, janelas, ventilação, brasão fictício e grelha |
| Assets/Resources/PixelArt/CastleMaterialsAtlas.png | Alvenaria cinza, granito, pavimento, pedras de remate, porta de carvalho e estandartes |
| Assets/Resources/PixelArt/CityGroundMap.png | Pintura contínua de toda a planta: terra, trilhas orgânicas, musgo, grama, luz refletida dos estabelecimentos, leito do fosso e pavimento da ponte |
| Assets/Resources/PixelArt/ChestStates.png | Dois sprites transparentes do baú: fechado e aberto/vazio |
| Assets/Scripts/Prototype/CityGroundArt.cs | Superfícies do chão e UVs normalizados da planta, usando a imagem inteira uma vez |
| Assets/Resources/CityGround.shader | Material opaco do piso, sem divisão em células nem repetição |
| Assets/Scripts/Prototype/CityPeripheryArt.cs | Geometria das casas, caldeira da loja de itens, fachadas, ponte, paredes do fosso, tubulações e vapor |
| Assets/Scripts/Prototype/CastleFortressArt.cs | Fortaleza de pedra, ameias, arco do portão, seteiras, estandartes e ícone miniatura |
| Assets/Resources/PixelGlow.shader | Vapor das chaminés com transparência/aditivo e teste de profundidade |

Os dois atlas da periferia têm 1448 × 1086; o atlas do castelo tem 1447 × 1087. Foram gerados pela ferramenta integrada image_gen e mantidos sem alteração. Importadores usam Point, nenhuma compressão, mipmap ou redimensionamento NPOT. Prompts completos em `PERIFERIA_ART_PROMPTS.md`; origem em `ASSETS.md`.

O chão usa um único PNG de 2305 × 2975 pixels, cobrindo aproximadamente 62,10 × 80,36 unidades. A fonte original de 965 × 1629, pintada com image_gen sobre o guia `ArtInputs/CityGroundLayout.png`, está preservada em `ArtInputs/CityGroundCore.png`. A versão atual foi recomposta em Python, com autorização do usuário, para acompanhar as casas afastadas, o pátio livre de armas e o fosso prolongado. Amostras conservam sua escala nativa, sem ampliar uma imagem pequena. `ArtInputs/CityGroundRebuilt.png` registra o núcleo recomposto; `Tools/Build-UnifiedCityGround.py` reconstrói a imagem e `ArtInputs/CityGroundBake.json` registra dimensões e hashes.

Os antigos planos repetidos e pátios sobrepostos foram substituídos pela pintura. Cada posição XY corresponde à mesma posição na imagem. O importador usa Point, Clamp, limite 4096 e nenhuma compressão, mipmap ou redução. As muretas externas não têm malha visível; seus colisores continuam limitando o movimento.

A imagem é aplicada a quatro superfícies simples num único renderer/material: chão ao sul, chão ao norte, leito rebaixado do fosso e pavimento sobre a ponte. O leito fica 0,7 unidade abaixo; a pintura do tabuleiro fica a 0,058, ligeiramente acima do volume de pedra. A ponte mantém parapeitos e volume 3D. A textura acompanha a planta existente; alterar a disposição dos caminhos exige atualizar a pintura.

A periferia usa materiais próprios. Os painéis recebem UVs locais para mostrar uma célula inteira, sem repetir letras. A iluminação das janelas e letreiros está pintada no atlas das fachadas; postes, lampiões salientes e círculos aditivos no chão foram retirados. É iluminação incorporada à arte, não um bake de lightmaps da Unity. Os mapas continuam montados em Play e usam o shader de materiais existente. Sombras e vapor usam materiais separados. O shader do cenário usa brilho padrão 1, com 0,8 nas casas e 0,95 na fortaleza, ponte/pátio e ícone do castelo. O ícone global tem ameias e mantém altura de 1,95, um pouco acima do herói.

Os personagens permanecem verticais no mundo, acompanhando apenas a direção horizontal da câmera. O tamanho aparente é conservado por compensação de escala. Isso corrige a cabeça que desaparecia dentro do balcão quando o sprite copiava também a inclinação da câmera. O teste de profundidade permanece ativo: objetos ainda escondem o corpo quando ele está atrás deles.

A muralha central tem planta de 9,5 × 4,5, com duas torres de 3,4 × 3,6. O portão permanece alinhado ao fim da ponte e à passagem automática existente. O castelo tem terraços e ameias de pedra; as antigas coberturas industriais foram substituídas. Não houve mudança no zoom da câmera.

O terreno visual é interrompido no fosso para mostrar a profundidade; colliders impedem andar fora da ponte. A ponte tem piso na altura do caminho e parapeitos sólidos. A tubulação suspensa permanece sobre o mercado; o trecho superior foi retirado porque se projetava como um risco no chão junto ao guarda e à ponte.

## Jornadas anteriores

IDs de mapas, lojas, baú, inventário e segredos foram conservados. Saves anteriores do mapa amplo continuam carregáveis. Se a coordenada antiga ficou fora do bairro ou passou a coincidir com casa, fosso, morador ou outro obstáculo, a equipe reaparece na rua central. Uma posição livre no mapa atual permanece na mesma coordenada. Não é necessário reiniciar a jornada.

## Verificação

Após a revisão do castelo, passaram 249 verificações dos nove PNGs/importadores, 161 de sprites/materiais, 2116 de malhas/câmera, 73 de caminhos da periferia, 269 de viagens/interiores e 110 de salvamento. A busca por caminhos usa amostragem de 0,125 unidade para examinar as vielas estreitas com margem além do collider do herói. Toda área caminhável amostrada está conectada, incluindo moradores, portas, retornos e ponte.

Na correção da iluminação e da profundidade dos sprites, os scripts compilaram e passaram 249 verificações dos PNGs/importadores, 161 de sprites/materiais e 2167 de malhas/câmera/profundidade. As novas verificações reproduzem a cabeça escondida na orientação anterior, conferem o sprite inteiro na frente do balcão e a oclusão correta atrás de balcão e parede. Prévias técnicas do balcão antes/depois e da periferia sem postes foram inspecionadas fora da Unity.

Com o chão contínuo, passaram 259 verificações de PNGs/importadores, 197 de sprites/materiais, 2313 de malhas/câmera, 73 de caminhos e 269 de viagens/interiores. Conferidos UVs sem repetição/inversão, cobertura de entradas e vielas, alturas do fosso/ponte e ausência dos pisos antigos sobre a pintura. Scripts e dois estágios do shader novo compilaram externamente. Prévias da entrada, mercado e ponte foram inspecionadas fora da Unity; aplicação real no Editor permanece pendente de Play.

Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1. ObliqueWorld e PixelGlow compilaram externamente nos estágios de vértices/fragmentos com d3dcompiler_47 e os headers URP, sob as mesmas adaptações descritas em VALIDATION.md. Esse teste não substitui o renderer DirectX 12 da Unity.

Prévias técnicas da entrada, mercado, bairro da capela e ponte foram renderizadas a partir das malhas e texturas e inspecionadas fora do Editor. Não são capturas da Unity. A inspeção permitiu ajustar a visibilidade dos letreiros e impedir que o piso escondesse o fosso.

A fortaleza teve prévias geral e junto ao portão inspecionadas. A vista geral usa enquadramento de apresentação para mostrar toda a construção; a vista próxima usa o enquadramento do jogo. Torres altas podem ocupar mais que a altura da tela ao chegar ao portão.

## Conferência em Play

1. Pare Play, aguarde a importação/compilação e abra CidadePrototipo. Continue seu slot ou inicie uma jornada. Confira o Console sem novos erros.
2. Entre pelo ícone global do castelo. A chegada deve mostrar o bairro compacto, com pousada/armas próximas e sem lago, rio ou pontes de madeira.
3. Siga a viela, entre nas três lojas e na hospedaria e retorne. Confira portas, letreiros, corpo do herói e ruas entre as casas, inclusive ao andar nas diagonais. As poses devem conservar o alinhamento do corpo e parar ao soltar as teclas ou ficar bloqueado. Encoste na frente e nas laterais do balcão: a cabeça deve permanecer inteira. Por trás de móveis e paredes, o corpo deve ser ocultado normalmente.
   Confira também que não há faixas douradas de entrada/saída. Aproximar-se da porta deve entrar; passar diante de uma janela ou distante no pátio deve continuar no bairro. Ao voltar, o herói fica fora da área de entrada. Experimente sair pelo sul tanto no centro quanto nas laterais caminháveis; voltar da ilha deve permitir permanecer na periferia.
4. Converse com os moradores e com a exploradora na rua central, consulte o poema e procure o baú parcialmente atrás da oficina das lanternas. A perspectiva do baú deve acompanhar as casas. Ao abrir, troca para o sprite aberto/vazio e concede 1.000 moedas; interagir novamente não concede outra recompensa. Saves que já registram o baú aberto conservam esse estado, sem novo pagamento. Carregue jornadas com e sem o baú coletado e confira a imagem correspondente. Confira a exploração com a área inferior livre e pressione E perto dos moradores e objetos para interagir. O progresso dos segredos fica em I → Itens depois da primeira descoberta.
5. Siga ao norte, atravesse a ponte de pedra e caminhe até a porta central do castelo. O cenário deve mudar automaticamente. Saia do interior e confira o retorno ao lado norte da ponte, sem um ciclo de entrada/saída.
6. Confira o pátio livre diante de ARMAS e as fileiras afastadas, com a mesma definição do chão aprovado anteriormente. Caminhe até a ponte: não deve aparecer o risco de tubulação junto ao guarda. Siga pelas laterais do castelo: o fosso e as paredes devem continuar para fora da tela, sem um término retangular. A revisão atual foi conferida em prévias técnicas; a aparência no renderer da Unity ainda precisa deste Play.

## Correção de resolução e cor da luz — 16/10/2026

- Resolução real de 2688 × 3472 (9.332.736 pixels), com 36,1% mais pixels que os 2305 × 2975 originais. Os doze trechos gerados de 1254 × 1254 foram somente reduzidos para os espaços de montagem. A fonte completa e a textura de runtime possuem o mesmo hash e os mesmos bytes.
- Retirado completamente o remapeamento da tentativa anterior. A calibração da planta e os arquivos de geometria, casas, fortaleza e atlas de fachadas/materiais foram preservados byte a byte. Nenhuma construção foi movida, redesenhada ou substituída.
- Luzes pintadas com cores das fachadas, nas portas atuais: MATERIA `#41E7E2`, ITENS `#F8D43B`, ARMAS `#FFA51C`, POUSADA `#FFD278`. As regiões de reflexão também foram verificadas por canais de cor na textura final.
- Textura Point, sem mipmaps, sem compressão, com limite de 4096 e sem redução NPOT. O GUID não mudou. A ponte e o fosso continuam nas coordenadas existentes.
- Compilação dos scripts aprovada; 1.714 verificações específicas de terreno/cidade/câmera e 73 verificações de caminhos aprovadas. A instalação da pintura foi reproduzida sem alteração de bytes.
- `CityGroundDusk.preview.png` mostra a prévia técnica de ITENS/MATERIA, incluindo o reflexo ciano. Também foram inspecionadas as áreas de ARMAS, POUSADA e ponte. São rasterizações das malhas e texturas reais; a conferência no renderer da Unity em Play permanece pendente.
- Permanecem fora do escopo os problemas antigos dos executáveis gerais: `Test-Oblique.ps1` tem uma expectativa desatualizada de câmera fixa de outro ambiente; `Test-CityLayout.ps1` omite uma dependência de `DragonQuest.Exploration`. As verificações específicas existentes foram compiladas separadamente com a biblioteca atual do jogo, sem editar esses testes.
