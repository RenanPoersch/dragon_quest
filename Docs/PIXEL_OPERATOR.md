# Tipografia do jogo

O jogo usa **Pixel Operator**, de Jayvee Enaguas (HarvettFox96), nas versões Regular e Bold. Os arquivos originais estão em `Assets/Resources/Fonts`, junto com a licença CC0 1.0 Universal do pacote.

Fonte do pacote: https://www.dafont.com/pixel-operator.font

`PrototypeTypography.Style` centraliza o carregamento dos arquivos locais e a criação dos estilos. Regular é usada em diálogos, descrições, inventário, lojas, saves e informações de batalha. Bold é usada em títulos, nomes destacados, números de dano e nas duas opções da tela inicial. Não depende de uma fonte instalada no Windows.

As fontes são importadas com rasterização com hinting e seus dados são incluídos nos builds. Cada painel mantém seu alinhamento, cores e dimensões; os tamanhos de letra foram ajustados para os glifos da Pixel Operator. Botões de escolhas dos diálogos e estilos derivados com ícones usam a mesma família.

Ao testar em Play, confira a tela inicial, a lista dos três saves, diálogos e escolhas, abas do inventário, lojas, comandos/HP/MP/dano da batalha e textos finais. Verifique também caracteres como `á`, `ã`, `é`, `õ` e `ç`.
