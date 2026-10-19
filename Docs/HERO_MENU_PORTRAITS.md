# Retratos dos heróis nos menus

Todos os menus usam os retratos ilustrados com proporções mais realistas, no mesmo acabamento de pixel art dos diálogos. Aren reutiliza `Assets/Resources/PixelArt/DialoguePortraits/aren.png`. Lia e Bram têm novos retratos em `Assets/Resources/PixelArt/HeroPortraits`, com seus cabelos, roupas e cores preservados.

Cada herói tem fisionomia, expressão e pose próprias:

| Herói | Rosto e expressão | Pose |
| --- | --- | --- |
| Aren | Rosto triangular jovem e expressão séria | Três quartos, olhando para a direita da imagem |
| Lia | Rosto arredondado, bochechas cheias, nariz curto, sardas e sorriso acolhedor | Frontal, cabeça levemente inclinada, mão segurando o cajado ao lado do rosto |
| Bram | Rosto largo e maduro, nariz robusto, barba curta e meio sorriso | Perfil para a esquerda, braços cruzados sobre a armadura |

`HeroPortraitCatalog` associa o ID do herói à imagem, carrega cada textura uma vez e desenha o PNG inteiro com proporção preservada. O inventário e seus menus de Item, Materia, Equip, Status e Magic, as lojas e os slots de Save/Continue usam esse catálogo. O escurecimento de personagens em KO continua nos menus da equipe.

Os retratos de Lia e Bram foram refeitos individualmente com o image_gen integrado, usando apenas o próprio sprite de caminhada como referência de identidade e traje. As fisionomias, expressões e poses foram descritas separadamente, sem usar o retrato de outro herói como modelo. Os PNGs originais foram copiados sem recorte ou redimensionamento e preservam a transparência. A importação usa Point/Clamp, sem compressão, mipmaps ou cópia legível em CPU.

Prompts completos, referências e hashes: [ArtInputs/HeroMenuPortraits.prompts.json](ArtInputs/HeroMenuPortraits.prompts.json).

Validação da revisão: importação dos três recursos em projeto Unity isolado, comparação visual com Aren e nove capturas dos menus em 1280 × 720. Conferidos resolução nativa, transparência, Point/Clamp, ausência de compressão e mipmaps, enquadramento e escurecimento em KO.
