# Música da intro e das batalhas comuns

A intro usa uma versão chiptune derivada de `videoplayback (1).m4a`, fornecido pelo usuário para esse momento. O arquivo tem 171,154 segundos; seu SHA-256 é `aa6902f6ba21b8d771209d69a772f7a719a5f6ab916b4578b33e027e8f04c559`.

As notas foram extraídas automaticamente da gravação, mantendo seus tempos, alturas e durações detectados, sem criar uma composição substituta, transpor ou quantizar o andamento. A síntese usa ondas de pulso a 25% e triangulares com resolução de amplitude de 8 bits. A extração a partir de uma mixagem é aproximada; os eventos ficam registrados para correção e nova renderização em `Docs/Audio/intro-notes.json`.

`Assets/Resources/Music/intro.ogg` contém a versão completa em estéreo a 44,1 kHz. O fade da gravação é preservado. O loop repete a faixa completa, com o encerramento suave da própria gravação. O importador da Unity usa streaming e não carrega toda a faixa em PCM antecipadamente.

## Batalha normal

`videoplayback (2).m4a`, enviado para a batalha normal, foi convertido integralmente para `Assets/Resources/Music/battle.ogg`. A gravação tem 166,116 segundos e SHA-256 `7fa3b08fc80006c3d09a3921edc5a7368cdb8908394188f786b8b8187f208b20`.

Além das notas detectadas, esta conversão inclui os ataques percussivos extraídos da gravação. Ruído de registrador de 15 bits e ondas triangulares recriam a percussão em timbres de chip, usando os tempos detectados, sem criar um ritmo por grade. A detecção automática de notas e transientes a partir de uma mixagem é aproximada. Os eventos e o relatório ficam em `Docs/Audio/battle-notes.json` e `Docs/Audio/battle-conversion.json`.

A mesma faixa toca na patrulha e nos encontros aleatórios do mapa global, pedreira e floresta. Cada combate começa a música do início e usa loop. Ela termina ao abrir os resultados, na derrota ou ao encerrar o combate. Dragão e chefe final mantêm suas próprias categorias e aguardam os arquivos correspondentes.

## Integração

`PrototypeCity` cria `PrototypeMusicController`, com duas fontes 2D na própria cena. O volume acompanha o fade visual da intro. Pular a intro encerra a música antes da tela New game / Continue; carregar e a travessia de New game continuam em silêncio. Ao destruir a cena, as fontes também são destruídas.

Toda troca de faixa inicia a nova música do começo. Ao voltar a uma faixa anterior, inclusive após silêncio ou combate, ela reinicia sem guardar a posição anterior. Enquanto a mesma faixa continua selecionada, ela segue normalmente.

Intro e batalha comum estão habilitadas em `MusicCuePolicy.IsAvailable`. As demais músicas aguardam os arquivos de referência correspondentes. As composições substitutas anteriores não fazem parte dos assets. O roteamento de mapas, chefes e vitória está preparado para as próximas conversões.

## Reprodução da conversão

`Tools/Convert-AudioTo8Bit.py` usa numpy, scipy, soundfile e imageio-ffmpeg. Para extrair novamente as notas, requer também Basic Pitch com seu modelo ONNX e dependências. Para renderizar usando os eventos já conferidos:

```powershell
python Tools/Convert-AudioTo8Bit.py 'caminho/videoplayback (1).m4a' --notes Docs/Audio/intro-notes.json --output Assets/Resources/Music/intro.ogg --metadata-dir Docs/Audio
python Tools/Convert-AudioTo8Bit.py 'caminho/videoplayback (2).m4a' --notes Docs/Audio/battle-notes.json --output Assets/Resources/Music/battle.ogg --metadata-dir Docs/Audio
```

O script verifica o SHA-256 do arquivo de origem antes de reutilizar eventos. As gravações originais permanecem fora do repositório. Ao converter uma nova faixa com bateria, `--percussion` detecta e grava seus ataques para a síntese. A renderização a partir de `--notes` reutiliza também a percussão registrada, sem repetir a detecção.

## Validação

`Tools/Test-Music.ps1` compila os scripts e testa uma cópia isolada da cena completa, preservando o Editor principal e os saves. Verifica importação, reprodução real, avanço de amostras, loops completos, fade, interrupção ao pular a intro e silêncio no título, Continue e New game. Também testa patrulha, encontros aleatórios das três regiões, reinício a cada encontro, continuidade no mesmo combate, parada nos resultados e na derrota, separação dos chefes e encerramento das fontes. O retorno à música de exploração após combate ou silêncio deve começar do início, sem retomar a posição anterior; esse teste reutiliza um clipe fornecido sem habilitar as faixas ainda pendentes. `Tools > Dragon Quest > Validate Music Assets` confere as importações no Editor.
