# Pedreira dos monólitos

A pedreira usa uma pintura contínua no mesmo enquadramento e estilo pixel art da floresta leste: rocha talhada em terraços, musgo, vegetação e luz do sol. `QuarryBackdrop.png` foi criado com a imagem da floresta como referência pelo imagegen integrado. `QuarryTotems.png` contém três pedras separadas com transparência; os símbolos são sprites de pixels gerados pelo jogo sobre as faces gravadas. Os prompts completos ficam em `Docs/ArtInputs/QuarryBackdrop.prompt.txt` e `QuarryTotems.prompt.txt`.

`QuarryLayout` registra o piso, a entrada, a saída inferior, o guardião, a inscrição e os três totens na pintura. O movimento respeita as paredes de rocha, os blocos do primeiro plano e as bases dos totens. Os personagens e totens usam o mesmo shader de sprites com profundidade. Saves em posições bloqueadas são recuperados na entrada; o ID `west-forest` e o progresso salvo dos monólitos são preservados.

O desafio existente continua sendo Lua → Estrela → Sol. Aproximar-se e pressionar E toca a pedra. A inscrição junto à entrada fornece o verso da lenda. A Lua brilha em azul claro, a Estrela em verde claro e o Sol em dourado. Uma ordem errada apaga as luzes e permite tentar novamente; completar mantém todos acesos e conta como um dos três sinais antigos para abrir a passagem do vulcão.

`Tools/Test-Quarry.ps1` compila todos os scripts do jogo e verifica navegação, alcance de E, colisões, recuperação de posição, retorno à ilha e sequência do desafio. `Tools > Dragon Quest > Validate Quarry` na Unity cria uma cena isolada e renderiza os totens e a pintura em 16:9, 16:10 e 4:3, verificando os estados das luzes. As imagens vão para `Temp/QuarryPreview` ou para `DQ_QUARRY_OUTPUT`.
