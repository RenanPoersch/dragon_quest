> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../../Current/README.md).

# Arquivo dos sprites antigos das lojas

As três lojas agora usam pinturas completas: [itens e matérias](../COMPACT_SHOPS_PIXEL_ART.md) e [armas](../WEAPON_SHOP_PIXEL_ART.md). A [Pousada](../INN_PIXEL_ART.md) também usa cenário integral.

Os atlas e renderizadores abaixo foram aposentados e removidos do jogo. Este diretório conserva o prompt e a prévia históricos como registro da implementação anterior.

## Registro da implementação anterior — 13/10/2026

ShopPropsAtlas.png foi criado com image_gen integrado. Prompt integral em PROMPT.md. PNG original RGBA 1254x1254 preservado, com 20 desenhos: estantes/balcoes de pocoes, materias e armas, armaduras, livro, cajados, orb, estandarte, orrerio, bigorna, barras, lanterna, cortina, escada e bonsai. Nenhuma parte da imagem de referencia do usuario foi extraida.

ShopPixelArt.cs substitui a decoracao geometrica na loja de itens, de materias e de equipamentos. A casca do ambiente conserva piso/paredes/tapete com atlas pixel art existente; mercadorias, moveis e ornamentos usam SpriteRenderer e PixelCharacter.shader (alpha recortado, escrita/teste de profundidade). Os sprites ficam verticais e voltados a camera, compensando a projecao e conservando proporcoes; ancoras estao fora da escala nao uniforme do interior compacto.

Importacao Point/Clamp, RGBA legivel, sem mipmaps, compressao ou redimensionamento. Janelas explicitas medidas no PNG; componente conectado dominante isola cada desenho. Em memoria, criamos grade 5x4 com celulas de 96px e margens transparentes; nunca alteramos o PNG fonte. Alpha e binario para evitar bordas suavizadas. O resultado em memoria pode ser inspecionado em ShopProps-runtime-preview.png.

Integrado no projeto indicado pelo PROJECT.md:
- Assets/Resources/PixelArt/ShopPropsAtlas.png e .meta
- Assets/Scripts/Game/ShopPixelArt.cs
- Assets/Scripts/Presentation/World/Rendering/ObliqueScenery.cs

GameEntryPoint.cs nao foi substituido nesta revisao: suas chamadas para as tres lojas e seus colisores/interacoes ja existentes continuam utilizados. Builders geometricos antigos permanecem para compatibilidade de testes, mas nao desenham mais mobilia ou mercadoria nas lojas em execucao.

Verificacao: todos os scripts Unity compilaram. Os 20 objetos estao presentes, inteiros dentro das celulas e com margem. Alpha binario e dimensoes dos recortes validados. Layouts: 10 sprites na loja de itens, 12 na loja de materias e 14 na loja de armas. Caminhos de entrada, vendedor e saida verificados com dimensoes do jogador e colisores existentes nas tres lojas. A previa mostra o atlas de sprites, nao uma captura da Unity.
