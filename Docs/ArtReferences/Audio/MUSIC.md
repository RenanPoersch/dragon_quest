# Músicas do jogo

O jogo usa as gravações originais fornecidas pelo usuário. Os M4A foram decodificados para WAV PCM de 16 bits, em estéreo a 44,1 kHz, mantendo seus timbres, andamento e dinâmica. As primeiras dezoito faixas são completas; a sala do dragão usa somente um ciclo do trecho inicial, conforme solicitado. Não há transcrição de notas, ressíntese ou tratamento 8-bit nos áudios usados pelo jogo. A Unity importa essas faixas em PCM com streaming, sem outra compressão com perdas e sem carregar a faixa inteira antecipadamente.

## Arquivos usados

- Intro: `videoplayback (1).m4a` → `Assets/Resources/Music/intro.wav`, com 171,154 segundos. SHA-256 da origem: `aa6902f6ba21b8d771209d69a772f7a719a5f6ab916b4578b33e027e8f04c559`.
- Batalha normal: `videoplayback (2).m4a` → `Assets/Resources/Music/battle.wav`, com 166,116 segundos. SHA-256 da origem: `7fa3b08fc80006c3d09a3921edc5a7368cdb8908394188f786b8b8187f208b20`.
- Pós-vitória: `videoplayback (3).m4a` → `Assets/Resources/Music/victory.wav`, com 54,126 segundos. SHA-256 da origem: `780fda0406faf78663ad47cbfdc50f508f48329b0d6e105b5c11af1e8a7f33ed`.
- Mundo aberto: `videoplayback (4).m4a` → `Assets/Resources/Music/world.wav`, com 410,459 segundos. SHA-256 da origem: `02d06a0ade6c1346bf11e03f8db4988982c1799a28d58a8441042225fafb6166`.
- Vila ao redor do castelo: `videoplayback (5).m4a` → `Assets/Resources/Music/village.wav`, com 157,153 segundos. SHA-256 da origem: `cea1d7eb1810280baf12fcaf455ad03e16c58b97ab6c4677fb0889c331029e45`.
- Luta do dragão: `videoplayback (6).m4a` → `Assets/Resources/Music/dragon.wav`, com 152,137 segundos. SHA-256 da origem: `83aaf9b2be598abd17e8ac9d2fa4634e4b526792a4e60d40454c7a049f80d6a1`.
- Batalha final do Rei Rubro: `videoplayback (7).m4a` → `Assets/Resources/Music/finalboss.wav`, com 440,483 segundos. SHA-256 da origem: `069c32d34065bb54d3ffc07fb5982b6a7a026c9b74be5ad6af9b124ab1c65ed0`.
- Floresta: `videoplayback (8).m4a` → `Assets/Resources/Music/forest.wav`, com 171,154 segundos. SHA-256 da origem: `7a72972bdd11e72b46d9c5efdd6017acc788cc5468e09180b3475caa795d19ef`.
- Ruínas: `videoplayback (9).m4a` → `Assets/Resources/Music/ruins.wav`, com 134,142 segundos. SHA-256 da origem: `732815b20fea0cc38cd3d45129e41e49684ace4891e65fd65c9d7870d9336166`.
- Pedreira: `videoplayback (10).m4a` → `Assets/Resources/Music/quarry.wav`, com 167,114 segundos. SHA-256 da origem: `9587d427f07aedf2c85e752dc4d83f166dda66167ac0d2a0d60c99d07537aea0`.

- Intro na nau: `videoplayback (11).m4a` → `Assets/Resources/Music/voyage.wav`, com 190,171 segundos. SHA-256 da origem: `57ae4648e3b248afbeb0cd2bdfb77d7f653cf02c97a612dfe9a0978438870fe4`.
- Dormir na hospedaria: `videoplayback (12).m4a` → `Assets/Resources/Music/sleep.wav`, com 9,195 segundos. SHA-256 da origem: `529156b24bb4650b5fd3c3db353a6431cef956cf8b2357e7c78ccfa35a29d113`.
- Continue após morrer: `videoplayback (13).m4a` → `Assets/Resources/Music/defeat.wav`, com 36,130 segundos. SHA-256 da origem: `ed9ed8dbd50b3a89d6e79814687c2032dc4eff8072c68edbb55f662d48e3f0e9`.
- Dragão derrotado / essência liberta: `videoplayback (14).m4a` → `Assets/Resources/Music/dragonfreed.wav`, com 299,143 segundos. SHA-256 da origem: `03dfa035140a1de4b18ce16afa4b3b815b7267fe26ffce007a38ca5b8a8d502e`.
- Final bom: `videoplayback (15).m4a` → `Assets/Resources/Music/goodending.wav`, com 155,156 segundos. SHA-256 da origem: `063d0c00a786371561c010607957eec9c008dcefd0e7c0a2025a6f35989ae2ff`.
- Final mind control: `videoplayback (16).m4a` → `Assets/Resources/Music/mindcontrolending.wav`, com 196,162 segundos. SHA-256 da origem: `73b9f0ab4dc46630893e77d28e56d140ff5b5d8efd354116d1662cf5dee06a26`.
- Final do meteoro: `videoplayback (17).m4a` → `Assets/Resources/Music/meteorending.wav`, com 241,116 segundos. SHA-256 da origem: `36f876975819b5c7445514957ba1d0c5d23179176188d54d63bac08a1402ce0e`.
- Sala do trono: `videoplayback (18).m4a` → `Assets/Resources/Music/throne.wav`, com 242,161 segundos. SHA-256 da origem: `e16efdb220d095ce93978f43fce5b0ef8f6cba1a41508c499b72b78e2f52d9ef`.

- Sala do dragão: `videoplayback (19).m4a` → `Assets/Resources/Music/dragonlair.wav`, loop de 8,267211 segundos, recortado de 8,480000 a 16,747211 segundos. SHA-256 da origem: `342c44435e6edf40839c2e62a19b5b5ef7db226f79d8394546570674fce44de6`.


## Reprodução

MusicCuePolicy escolhe a faixa conforme abertura, mapa, descanso, batalha, resultado e final. MusicController mantém dois AudioSources para transições e cacheia as faixas solicitadas. MusicImportSettings preserva PCM, estéreo, streaming e taxa original.

Na conversa com o rei, a música do trono faz fade para silêncio. A faixa final começa depois de um quadro totalmente preto, antes da revelação da batalha. O covil usa o ciclo inicial de dragonlair.wav, preparado por Tools/Build-DragonLairLoop.py. Os ganhos das cinematics acompanham seus relógios sem regravar os WAV.
