# Músicas do jogo

O jogo usa as gravações originais fornecidas pelo usuário. Os M4A foram decodificados para WAV PCM de 16 bits, em estéreo a 44,1 kHz, mantendo a gravação completa, seus timbres, andamento e dinâmica. Não há transcrição de notas, ressíntese ou tratamento 8-bit nos áudios usados pelo jogo. A Unity importa essas faixas em PCM com streaming, sem outra compressão com perdas e sem carregar a faixa inteira antecipadamente.

## Arquivos usados

- Intro: `videoplayback (1).m4a` → `Assets/Resources/Music/intro.wav`, com 171,154 segundos. SHA-256 da origem: `aa6902f6ba21b8d771209d69a772f7a719a5f6ab916b4578b33e027e8f04c559`.
- Batalha normal: `videoplayback (2).m4a` → `Assets/Resources/Music/battle.wav`, com 166,116 segundos. SHA-256 da origem: `7fa3b08fc80006c3d09a3921edc5a7368cdb8908394188f786b8b8187f208b20`.
- Pós-vitória: `videoplayback (3).m4a` → `Assets/Resources/Music/victory.wav`, com 54,126 segundos. SHA-256 da origem: `780fda0406faf78663ad47cbfdc50f508f48329b0d6e105b5c11af1e8a7f33ed`.
- Mundo aberto: `videoplayback (4).m4a` → `Assets/Resources/Music/world.wav`, com 410,459 segundos. SHA-256 da origem: `02d06a0ade6c1346bf11e03f8db4988982c1799a28d58a8441042225fafb6166`.
- Vila ao redor do castelo: `videoplayback (5).m4a` → `Assets/Resources/Music/village.wav`, com 157,153 segundos. SHA-256 da origem: `cea1d7eb1810280baf12fcaf455ad03e16c58b97ab6c4677fb0889c331029e45`.
- Luta do dragão: `videoplayback (6).m4a` → `Assets/Resources/Music/dragon.wav`, com 152,137 segundos. SHA-256 da origem: `83aaf9b2be598abd17e8ac9d2fa4634e4b526792a4e60d40454c7a049f80d6a1`.
- Batalha final do tirano: `videoplayback (7).m4a` → `Assets/Resources/Music/finalboss.wav`, com 440,483 segundos. SHA-256 da origem: `069c32d34065bb54d3ffc07fb5982b6a7a026c9b74be5ad6af9b124ab1c65ed0`.
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

As versões 8-bit foram removidas dos assets. Os scripts e relatórios da experiência anterior permanecem apenas como histórico e não participam da reprodução.

## Integração

`PrototypeCity` cria `PrototypeMusicController`, com duas fontes 2D na própria cena. O volume acompanha o fade visual da intro. Pular a intro encerra a música antes da tela New game / Continue; a seleção de saves continua em silêncio. A intro na nau usa `voyage.wav` enquanto `IsVoyagePlaying` está ativo. Ao destruir a cena, as fontes também são destruídas.

A gravação de batalha toca na patrulha e nos encontros aleatórios do mapa global, pedreira e floresta. Cada combate começa a música do início e usa loop. Ela termina ao abrir os resultados, na derrota ou ao encerrar o combate. Dragão e tirano usam suas gravações próprias.

Quando a tela de resultados da vitória aparece, `victory.wav` começa do início, com uma transição de 0,18 segundo a partir da batalha. A gravação completa se repete enquanto os resultados ficam abertos e para ao continuar a exploração. A derrota permanece sem música de vitória.

`world.wav` toca ao explorar o mapa global (`world`). A gravação completa usa loop e cede lugar à batalha quando um encontro começa. Ao continuar depois dos resultados de vitória, ou sair e voltar ao mapa global, a faixa recomeça do início. Ela não toca nos menus New game / Continue nem nas outras regiões.

`village.wav` toca na vila ao redor do castelo (`city`, Periferia do Castelo) e nos seus interiores: hospedaria, loja de itens, loja de matérias e loja de equipamentos. A faixa completa usa loop. Entrar em um interior e voltar à vila mantém a mesma música em andamento; ao sair para outra região e retornar, ela começa do início. A sala do trono usa sua própria gravação.

Floresta leste (`east-forest`), ruínas (`ancient-ruins`) e pedreira (`west-forest`) usam respectivamente `forest.wav`, `ruins.wav` e `quarry.wav`, em loop durante a exploração. Encontros comuns continuam usando `battle.wav`; voltar ao cenário reinicia sua música do começo.

O covil (`volcano`) permanece em silêncio durante a exploração e as primeiras falas do dragão. `DragonInteraction.IsBattlePrelude` sinaliza o último segundo da escrita da última página da conversa do confronto. A música `dragon.wav` entra nesse trecho e mantém a mesma fonte e posição quando a conversa termina e `StartDragonBattle` abre o combate. Avançar a escrita rapidamente preserva o avanço imediato da conversa; a última página já revelada também permite a entrada da música, sem impor uma espera ao jogador. Cancelar ou substituir a conversa cancela a entrada, e a essência liberta não ativa música de batalha.

`finalboss.wav` começa ao iniciar a batalha do tirano e repete em loop durante esse combate. A sala do trono fora de combate usa `throne.wav`, sem alterar a música da vila.

`defeat.wav` toca após a animação de derrota, enquanto a tela aguarda o jogador continuar. Ao retornar à hospedaria, a música da vila volta do início; essa faixa não toca na seleção normal de saves.

`dragonfreed.wav` começa após continuar na tela de vitória do dragão, quando o covil apresenta a essência liberta. A fanfarra normal permanece na tela de resultados. A faixa da essência segue enquanto o jogador permanecer no covil libertado; antes de vencer o dragão, a exploração e as primeiras falas continuam em silêncio.

Ao passar a noite, a música da vila continua durante a caminhada até as camas. `sleep.wav` entra no fade para dormir e toca uma única vez. `InnRestController` mantém a noite até completar os 9,195 segundos da gravação, retomando a música da vila no despertar. Um novo descanso começa o jingle do início.

`PrototypeEndingController.CurrentEnding` seleciona a trilha do desfecho: `goodending.wav` para `KingFreed`, `mindcontrolending.wav` para `HeroPossessed` e `meteorending.wav` para `CastleDestroyed`. Cada faixa cobre sua apresentação e repete enquanto o final estiver aberto. Ao voltar ao título, as fontes da cena são encerradas.

Toda troca de faixa inicia a nova música do começo. Ao voltar a uma faixa anterior, inclusive após silêncio ou combate, ela reinicia sem guardar a posição anterior. Enquanto a mesma faixa continua selecionada, ela segue normalmente. O loop repete cada gravação completa, incluindo seu encerramento original.

As dezoito gravações fornecidas estão habilitadas em `MusicCuePolicy.IsAvailable`. Título e Continue para carregar saves permanecem em silêncio. `MusicStoryState` informa nau, descanso, dragão libertado e final ativo à política de seleção.

## Reprodução da importação

Os arquivos M4A originais permanecem na pasta Downloads. Os WAV utilizados pelo jogo estão versionados no projeto. Para gerá-los novamente com FFmpeg:

```powershell
ffmpeg -i 'caminho/videoplayback (1).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/intro.wav
ffmpeg -i 'caminho/videoplayback (2).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/battle.wav
ffmpeg -i 'caminho/videoplayback (3).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/victory.wav
ffmpeg -i 'caminho/videoplayback (4).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/world.wav
ffmpeg -i 'caminho/videoplayback (5).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/village.wav
ffmpeg -i 'caminho/videoplayback (6).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/dragon.wav
ffmpeg -i 'caminho/videoplayback (7).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/finalboss.wav
ffmpeg -i 'caminho/videoplayback (8).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/forest.wav
ffmpeg -i 'caminho/videoplayback (9).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/ruins.wav
ffmpeg -i 'caminho/videoplayback (10).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/quarry.wav
ffmpeg -i 'caminho/videoplayback (11).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/voyage.wav
ffmpeg -i 'caminho/videoplayback (12).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/sleep.wav
ffmpeg -i 'caminho/videoplayback (13).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/defeat.wav
ffmpeg -i 'caminho/videoplayback (14).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/dragonfreed.wav
ffmpeg -i 'caminho/videoplayback (15).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/goodending.wav
ffmpeg -i 'caminho/videoplayback (16).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/mindcontrolending.wav
ffmpeg -i 'caminho/videoplayback (17).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/meteorending.wav
ffmpeg -i 'caminho/videoplayback (18).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/throne.wav
```

## Validação

`Tools/Test-Music.ps1` compila os scripts e testa uma cópia isolada da cena completa, preservando o Editor principal e os saves. Verifica importação PCM, duração completa, estéreo, reprodução real, avanço de amostras, loops, fade, interrupção ao pular a intro e silêncio no título, Continue e New game. Também testa o mundo aberto, seu loop completo, continuidade durante a exploração e reinício ao voltar da vitória ou da vila. Confere a gravação da vila, seu loop completo, continuidade na hospedaria e no retorno à rua, além do roteamento das lojas. Inclui patrulha, encontros aleatórios das três regiões, reinício a cada encontro, continuidade no mesmo combate, transição para a música de vitória na tela real de resultados, loop e continuidade nessa tela, troca ao continuar ou na derrota, separação dos chefes e encerramento das fontes. `Tools > Dragon Quest > Validate Music Assets` confere as importações no Editor.

A validação conjunta também cobre as três novas regiões e seus loops, o silêncio no covil e nas primeiras falas, a entrada musical antes de terminar a última fala, o cancelamento e a substituição da conversa, a continuidade da mesma fonte de áudio ao carregar a luta do dragão, sua vitória e a música da essência liberta, além da faixa e do loop exclusivos da batalha do tirano. `Tools/Test-Secrets.ps1` confere a escrita do diálogo e o tempo restante por elementos de texto, incluindo acentos e emoji.

A validação do lote 11–18 cobre a nau e seu término, derrota e retorno à hospedaria, a música da essência após o dragão, a sala do trono e sua troca para combate, o jingle de descanso sem loop e a espera antes do despertar, além dos três finais reais e o silêncio ao voltar ao título.
