# Música da intro e das batalhas comuns

O jogo usa as gravações originais fornecidas pelo usuário. Os M4A foram decodificados para WAV PCM de 16 bits, em estéreo a 44,1 kHz, mantendo a gravação completa, seus timbres, andamento e dinâmica. Não há transcrição de notas, ressíntese ou tratamento 8-bit nos áudios usados pelo jogo. A Unity importa essas faixas em PCM com streaming, sem outra compressão com perdas e sem carregar a faixa inteira antecipadamente.

## Arquivos usados

- Intro: `videoplayback (1).m4a` → `Assets/Resources/Music/intro.wav`, com 171,154 segundos. SHA-256 da origem: `aa6902f6ba21b8d771209d69a772f7a719a5f6ab916b4578b33e027e8f04c559`.
- Batalha normal: `videoplayback (2).m4a` → `Assets/Resources/Music/battle.wav`, com 166,116 segundos. SHA-256 da origem: `7fa3b08fc80006c3d09a3921edc5a7368cdb8908394188f786b8b8187f208b20`.

As versões 8-bit foram removidas dos assets. Os scripts e relatórios da experiência anterior permanecem apenas como histórico e não participam da reprodução.

## Integração

`PrototypeCity` cria `PrototypeMusicController`, com duas fontes 2D na própria cena. O volume acompanha o fade visual da intro. Pular a intro encerra a música antes da tela New game / Continue; carregar e a travessia de New game continuam em silêncio. Ao destruir a cena, as fontes também são destruídas.

A gravação de batalha toca na patrulha e nos encontros aleatórios do mapa global, pedreira e floresta. Cada combate começa a música do início e usa loop. Ela termina ao abrir os resultados, na derrota ou ao encerrar o combate. Dragão e chefe final mantêm suas próprias categorias e aguardam os arquivos correspondentes.

Toda troca de faixa inicia a nova música do começo. Ao voltar a uma faixa anterior, inclusive após silêncio ou combate, ela reinicia sem guardar a posição anterior. Enquanto a mesma faixa continua selecionada, ela segue normalmente. O loop repete cada gravação completa, incluindo seu encerramento original.

Intro e batalha comum estão habilitadas em `MusicCuePolicy.IsAvailable`. As demais músicas aguardam os arquivos de referência correspondentes. O roteamento de mapas, chefes e vitória está preparado para as próximas gravações.

## Reprodução da importação

Os arquivos M4A originais permanecem na pasta Downloads. Os WAV utilizados pelo jogo estão versionados no projeto. Para gerá-los novamente com FFmpeg:

```powershell
ffmpeg -i 'caminho/videoplayback (1).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/intro.wav
ffmpeg -i 'caminho/videoplayback (2).m4a' -map 0:a:0 -map_metadata -1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact Assets/Resources/Music/battle.wav
```

## Validação

`Tools/Test-Music.ps1` compila os scripts e testa uma cópia isolada da cena completa, preservando o Editor principal e os saves. Verifica importação PCM, duração completa, estéreo, reprodução real, avanço de amostras, loops, fade, interrupção ao pular a intro e silêncio no título, Continue e New game. Também testa patrulha, encontros aleatórios das três regiões, reinício a cada encontro, continuidade no mesmo combate, parada nos resultados e na derrota, separação dos chefes e encerramento das fontes. O retorno à música de exploração após combate ou silêncio deve começar do início; esse teste reutiliza um clipe fornecido sem habilitar as faixas ainda pendentes. `Tools > Dragon Quest > Validate Music Assets` confere as importações no Editor.
