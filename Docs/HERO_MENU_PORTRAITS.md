# Retratos dos heróis nos menus

Todos os menus usam os retratos ilustrados com proporções mais realistas, no mesmo acabamento de pixel art dos diálogos. Aren reutiliza `Assets/Resources/PixelArt/DialoguePortraits/aren.png`. Lia e Bram têm novos retratos em `Assets/Resources/PixelArt/HeroPortraits`, com seus cabelos, roupas e cores preservados.

`HeroPortraitCatalog` associa o ID do herói à imagem, carrega cada textura uma vez e desenha o PNG inteiro com proporção preservada. O inventário e seus menus de Item, Materia, Equip, Status e Magic, as lojas e os slots de Save/Continue usam esse catálogo. O escurecimento de personagens em KO continua nos menus da equipe.

Os retratos de Lia e Bram foram gerados individualmente com o image_gen integrado, usando o sprite de caminhada como referência de identidade e o retrato de Aren como referência de estilo. Os PNGs originais foram copiados sem recorte ou redimensionamento e preservam a transparência. A importação usa Point/Clamp, sem compressão, mipmaps ou cópia legível em CPU.

Prompts completos, referências e hashes: [ArtInputs/HeroMenuPortraits.prompts.json](ArtInputs/HeroMenuPortraits.prompts.json).

Validação: compilação dos scripts com as bibliotecas da Unity, importação dos três recursos em projeto Unity isolado e inspeção visual dos menus em 1280 × 720.
