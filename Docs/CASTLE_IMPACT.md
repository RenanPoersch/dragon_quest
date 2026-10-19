# Meteoro / Cinzas do Castelo

O desfecho `CastleDestroyed` agora começa com uma abertura cinematográfica antes do epílogo existente:

- 0–3 s: castelo intacto sob nuvens carregadas, chuva e luz difusa de tempestade.
- 3–16 s: meteoro gigantesco emerge acima das nuvens, começa devagar e acelera continuamente até atingir o castelo. Uma arte própria de nuvens vermelhas aparece junto do meteoro, recortada pelos pixels carmesim do céu: castelo, montanhas, janelas e água preservam as cores originais. A cauda de fogo ondula e a própria pintura traz a iluminação vermelha das nuvens, sem banho de cor sobre o cenário. Tremidas contínuas nos dois eixos, com pulsos repetidos, aumentam durante a aproximação.
- 16–16,45 s: impacto no alto do castelo, tremor forte de até 20 pixels de referência e clarão até branco integral, incluindo as barras de cinema. A margem de imagem cobre todo o deslocamento da câmera.
- 16,45–17,85 s: branco sustentado.
- A partir de 17,85 s: o branco revela o epílogo existente. A entrada da colina, o homem sentado, os movimentos, o texto e o retorno ao menu mantêm suas durações.

O relógio usa tempo não escalado e não avança antes do primeiro desenho. A cena é automática, sem legendas ou controles novos. Somente `CastleDestroyed` recebe esta abertura.

Artes novas geradas com a ferramenta integrada de imagens: `Assets/Resources/PixelArt/Endings/CastleBeforeImpact.png`, `EndingMeteor.png` e `CastleRedClouds.png`. Prompts e proveniência em `Docs/ArtInputs/CastleImpact.prompts.json` e `CastleRedClouds.prompt.json`.

O final bom e o epílogo da colina mostram as pinturas sem camada colorida sobre o cenário. O final bom mantém apenas o fundo preto padrão restrito à área de leitura, além da sombra das letras; o epílogo da colina mantém apenas a sombra das letras. As transições para preto/branco continuam fazendo parte da sequência. A apresentação e o filtro do mind control permanecem intactos.

Validação: `Tools/Test-CastleImpact.ps1`, `Tools/Test-FinalOne.ps1`, `Tools/Test-UnityScripts.ps1` e menu Unity `Tools/Dragon Quest/Validate Castle Impact`. O menu `Preview Castle Impact` permite reproduzir e inspecionar a abertura e o epílogo completo.
