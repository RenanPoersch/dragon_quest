# Música da intro

A intro usa uma versão chiptune derivada de `videoplayback (1).m4a`, fornecido pelo usuário para esse momento. O arquivo tem 171,154 segundos; seu SHA-256 é `aa6902f6ba21b8d771209d69a772f7a719a5f6ab916b4578b33e027e8f04c559`.

As notas foram extraídas automaticamente da gravação, mantendo seus tempos, alturas e durações detectados, sem criar uma composição substituta, transpor ou quantizar o andamento. A síntese usa ondas de pulso a 25% e triangulares com resolução de amplitude de 8 bits. A extração a partir de uma mixagem é aproximada; os eventos ficam registrados para correção e nova renderização em `Docs/Audio/intro-notes.json`.

`Assets/Resources/Music/intro.ogg` contém a versão completa em estéreo a 44,1 kHz. O fade da gravação é preservado. O loop repete a faixa completa, com o encerramento suave da própria gravação. O importador da Unity usa streaming e não carrega toda a faixa em PCM antecipadamente.

## Integração

`PrototypeCity` cria `PrototypeMusicController`, com duas fontes 2D na própria cena. O volume acompanha o fade visual da intro. Pular a intro encerra a música antes da tela New game / Continue; carregar e a travessia de New game continuam em silêncio. Ao destruir a cena, as fontes também são destruídas.

Somente a intro está habilitada em `MusicCuePolicy.IsAvailable`. As demais músicas aguardam os arquivos de referência correspondentes. As composições substitutas anteriores não fazem parte dos assets. O roteamento de mapas, combates, chefes e vitória está preparado para as próximas conversões.

## Reprodução da conversão

`Tools/Convert-AudioTo8Bit.py` usa numpy, scipy, soundfile e imageio-ffmpeg. Para extrair novamente as notas, requer também Basic Pitch com seu modelo ONNX e dependências. Para renderizar usando os eventos já conferidos:

```powershell
python Tools/Convert-AudioTo8Bit.py 'caminho/videoplayback (1).m4a' --notes Docs/Audio/intro-notes.json --output Assets/Resources/Music/intro.ogg --metadata-dir Docs/Audio
```

O script verifica o SHA-256 do arquivo de origem antes de reutilizar eventos. A gravação original permanece fora do repositório. O relatório da conversão está em `Docs/Audio/intro-conversion.json`.

## Validação

`Tools/Test-Music.ps1` compila os scripts e testa uma cópia isolada da cena completa, preservando o Editor principal e os saves. Verifica importação, reprodução real, avanço de amostras, fim e reinício do loop, fade, interrupção ao pular a intro, silêncio no título, Continue e New game, ausência das faixas rejeitadas e encerramento das fontes. Também pode executar `Tools > Dragon Quest > Validate Music Assets` no Editor para conferir a importação.
