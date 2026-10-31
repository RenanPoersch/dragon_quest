> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Prompts de geração da abertura

Ferramenta: imagegen integrada, sem CLI. Entrada da revisão atual: a paisagem já existente no projeto.

## Paisagem atual — Full HD 1920 × 1080

Entrada de edição: `Assets/Resources/PixelArt/Intro/Sunset.png` anterior ao commit `2eae473`. A ferramenta retornou 1672 × 941; a exportação final usa escala uniforme com amostragem Point para 1920 × 1080, com ajuste central inferior a um pixel na altura. Saída integrada no mesmo caminho, preservando o GUID do asset.

```text
Edit the attached existing pixel-art fantasy game opening background. The ONLY requested change is adapting its canvas to Full HD 1920 x 1080 pixels, landscape 16:9, by naturally extending the scenery horizontally on BOTH sides. Produce exactly 1920x1080 if supported, otherwise an exact 16:9 image at higher resolution. Preserve the entire existing artwork as the central scene, scaled uniformly to fit the 1080-pixel height; do not crop the original top, bottom, castle, lake, tree or three seated adventurers. Keep all original subjects, their poses, clothes, identities, their relative proportions and distance, the huge purple-blue sky, pink-orange sunset clouds, setting sun, distant mountains, castle on the left, shimmering lake and three adventurers under the tree on the right. Extend cloud formations, distant landscape, lake and dark grassy foreground naturally into the added side regions, with no duplicated edge columns, no mirror reflection of the artwork, no smears, no blurred filler, no warped or stretched subjects, no decorative borders. Match the existing crisp pixel-art style, original palette, lighting and detail. Keep the large open sky in the upper center for the title that the game renders separately. Do not add any text, logos, titles, HUD, extra characters, birds or new buildings. This is an edit target, not a general style reference. Preserve this specific existing scene as faithfully as possible.
```

## Paisagem anterior — enquadramento 6:5 e árvore mais baixa

Entrada de edição: a paisagem anterior, preservada no histórico do commit `12eab36`. Saída integrada: `Assets/Resources/PixelArt/Intro/Sunset.png`, 1374 × 1145. Ferramenta imagegen integrada, sem CLI.

```text
Use case: precise-object-edit.
Asset type: revised production background plate for a Unity pixel-art JRPG intro, composed to be shown ENTIRELY within the game frame.
Input image 1: EDIT TARGET, the current sunset background. Keep its scene, palette, pixel-art style and overall identity.
Primary request: Recompose this landscape into a 6:5 canvas, ideally 1440x1200, to fit the taller game frame WITHOUT any zoom or cropping. Preserve the COMPLETE castle on the lower left, lake and reflection, mountains, three small adventurers on the lower-right hill, and a tree at the right. Reflow their spacing for the new canvas; do not crop a wider image into this ratio.
CRITICAL TREE CHANGE: make the tree much SHORTER. The very highest leaf of its canopy must be at or BELOW 44 percent of the image height measured down from the top. Absolutely no branches or leaves anywhere in the upper 40 percent of the image. The whole tree canopy must fit within the right side of the frame, ending well below the separate game-title area. Its trunk rises naturally from the bottom-right hill; keep the three adventurers visible beside it.
Title-safe space: the whole upper 35-40 percent is unobstructed lavender/blue-gray sunset sky with soft horizontal pixel cloud bands and a calm, lighter central region where a BLACK title will be added in-engine. Do not draw the title. No trees or castle towers in this upper area.
Composition: entire main subjects comfortably inside the image borders with breathing room: full castle silhouette at lower left, entire group of three heroes at lower right, complete canopy of the smaller tree at lower right. Keep the horizon and warm coral/peach/gold sunset across the lower-middle, lake reflection at bottom-left and dark foreground slope at bottom-right. Keep the extreme top edge quiet and evenly colored lavender sky; a thin uninterrupted dark foreground ground silhouette can meet the bottom edge. Compose the scene to look complete as-is.
Style: retain the precise existing 16-bit JRPG pixel-art look, deliberate stepped pixel clusters, limited harmonious lavender, salmon, orange, gold and dark-navy palette. Crisp coherent square pixel grid; no painterly brush strokes or blur.
Constraints: opaque full-bleed scene, NO text, NO logo, NO UI, NO borders or black letterbox bars, NO birds (birds remain separate animated sprites in Unity), NO added characters, no cropped subjects. Exactly three existing adventurers. Preserve the peaceful nostalgic mood.
```

## Pássaros

```text
Use case: stylized-concept.
Asset type: transparent game sprite sheet for flying birds in a 16-bit pixel-art sunset intro.
Primary request: one horizontal strip of SIX sequential animation frames of the SAME small bird in flight seen from slightly below and in profile, flying toward the LEFT. Frames show a clean looping wing flap: wings high, wings diagonal up, wings fully extended level, wings diagonal down, wings low, wings returning level. Recognizable small natural crow/swallow silhouette with little head, short pointed beak facing left, tapered tail at right, two wings. Consistent body position and scale across all six frames.
Layout: exactly SIX equal square cells in a single row, no gaps, each sprite centered fully inside its cell with ample transparent margin. Total aspect ratio 6:1. Each cell corresponds to a 32x32 pixel art sprite enlarged with nearest-neighbor. The bird occupies roughly 22x20 original pixels. Strong crisp single-pixel edges, coherent square pixel grid. Flat very dark navy silhouette (#202331) with one subtle slate edge shade. Transparent background.
Constraints: all SIX frames MUST be fully separate, same alignment and same bird. NO labels, NO text, NO grid lines, NO borders, NO shadow, NO glow, NO scenery, NO other objects. Background must be actual alpha transparency. Pixel-art sprites only, no smooth or painterly edges.
```
