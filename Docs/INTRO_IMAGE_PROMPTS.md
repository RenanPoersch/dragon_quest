# Prompts de geração da abertura

Ferramenta: imagegen integrada. Referência: imagem anexada pelo usuário nesta atividade.

## Paisagem

```text
Use case: stylized-concept.
Asset type: production background plate for the opening cinematic of a Unity pixel-art JRPG, landscape 16:9, ideally 1536x864.
Input image: the user's attached image is a composition, palette and mood REFERENCE, not an edit target.
Primary request: Reimagine this reference as exquisite hand-crafted 16-bit pixel art. Sweeping coral, peach and golden sunset under lavender and blue-gray cloud bands. A distant dark medieval castle with elegant pointed towers on the lower left across a lake. Three small adventurer silhouettes seated on a sloping dark grassy hill in the lower right, looking toward the castle; a large tree silhouettes the right edge. Warm luminous horizontal clouds reflected in the water, layered distant mountains, nostalgic quiet fantasy mood.
Composition: wide landscape adaptation of the reference. Keep the upper-middle central sky open and calm, with generous negative space for a game title added in-engine. Foreground and castle occupy the bottom third, tall tree at right edge, richly composed sky.
Style: true crisp pixel art with a coherent coarse pixel grid like a 640x360 scene displayed at integer scale, deliberate stepped clusters, selective dithering, limited harmonious palette. Detailed but readable silhouettes, strong graphic composition.
Constraints: background image only. NO birds anywhere (birds are separate animated game sprites), NO text, NO lettering, NO title, NO logo, NO watermark, NO UI, NO borders. Do not reproduce the words in the reference. No painterly brushstrokes, smooth gradients, blurry edges or vector rendering. Opaque full-bleed image.
```

## Pássaros

```text
Use case: stylized-concept.
Asset type: transparent game sprite sheet for flying birds in a 16-bit pixel-art sunset intro.
Primary request: one horizontal strip of SIX sequential animation frames of the SAME small bird in flight seen from slightly below and in profile, flying toward the LEFT. Frames show a clean looping wing flap: wings high, wings diagonal up, wings fully extended level, wings diagonal down, wings low, wings returning level. Recognizable small natural crow/swallow silhouette with little head, short pointed beak facing left, tapered tail at right, two wings. Consistent body position and scale across all six frames.
Layout: exactly SIX equal square cells in a single row, no gaps, each sprite centered fully inside its cell with ample transparent margin. Total aspect ratio 6:1. Each cell corresponds to a 32x32 pixel art sprite enlarged with nearest-neighbor. The bird occupies roughly 22x20 original pixels. Strong crisp single-pixel edges, coherent square pixel grid. Flat very dark navy silhouette (#202331) with one subtle slate edge shade. Transparent background.
Constraints: all SIX frames MUST be fully separate, same alignment and same bird. NO labels, NO text, NO grid lines, NO borders, NO shadow, NO glow, NO scenery, NO other objects. Background must be actual alpha transparency. Pixel-art sprites only, no smooth or painterly edges.
```
