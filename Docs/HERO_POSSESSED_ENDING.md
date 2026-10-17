# Final de possessão — O novo hospedeiro

`GameEnding.HeroPossessed` agora apresenta uma animação automática na sala do trono. A regra de combate continua a mesma: derrotar o tirano antes do espírito possui Aren, inclusive em derrota simultânea dos dois. Os valores do enum e o carregamento de jornadas concluídas continuam compatíveis.

A câmera percorre verticalmente uma pintura contínua: começa no tapete, chega aos degraus e revela Aren de baixo para cima. O percurso dura 27 segundos depois do fade inicial de 1,4 s. O enquadramento final sobe mais e mantém Aren e o trono no centro horizontal, na parte inferior da tela. O rosto inteiro do fantasma aparece acima da coroa. O compositor acrescenta 18% de área preta acima da pintura, sem redimensionar ou editar o PNG original, e a câmera permanece imóvel durante o restante do epílogo.

O texto de `EndingNarrative.HeroPossessedEpilogue` rola na lateral esquerda enquanto a câmera se move. Usa a fonte, cor, sombra, velocidade e recorte gradual das linhas do final das cinzas. Após a última linha sair, há pausa, fade preto de 3 s e 1 s de preto antes de reiniciar a cena e voltar ao menu. A sequência usa tempo não escalado e só começa depois da primeira renderização. Não exige continuar páginas.

Depois da parada da câmera, relâmpagos duplos esparsos iluminam a silhueta do espírito no fundo preto. Cada clarão usa dois sprites alternados: `PossessionLightningA` no primeiro pulso e `PossessionLightningB` no segundo. A próxima ocorrência repete A/B; a escolha depende do relógio, mantendo a ordem mesmo se houver perda de quadros. Há uma pausa escura entre os pulsos e entre as ocorrências.

O novo `PossessedSpiritFront` fica reto, com a máscara e os olhos voltados de frente, os ombros nivelados e os braços ao lado do corpo. O sprite conserva a identidade do espírito azul e usa o acabamento sério dos retratos de conversa. A aparição desaparece completamente quando o raio se apaga. O shader `PossessedThroneComposite` usa o fundo preto da pintura e a área preta adicional como máscara; trono, Aren e colunas ocultam o espírito. O compositor não modifica as imagens originais e não aplica flashes brancos na tela inteira. Material, RenderTexture e sombra temporários são descartados ao sair.

## Arte e autoria

`Assets/Resources/PixelArt/Endings/PossessedThrone.png` é o PNG original gerado pelo `imagegen` integrado, copiado integralmente com seu RGBA. Referências: retrato de Aren para identidade e estilo sério, interior da sala para arquitetura e tapete, retrato do tirano para o acabamento. Resolução nativa 1024 × 1536. Importação Point/Clamp, sem compressão, mipmaps ou redimensionamento. Prompt completo em `Docs/ArtInputs/PossessedThrone.prompt.txt`.

Os três novos PNGs de 1024 × 1536, `PossessedSpiritFront.png`, `PossessionLightningA.png` e `PossessionLightningB.png`, ficam na mesma pasta. Foram gerados separadamente com o `imagegen` integrado e copiados integralmente, preservando transparência real, pixels e resolução nativa. Importação Point/Clamp, sem compressão ou mipmaps. Prompts completos, referências e SHA-256 em `Docs/ArtInputs/PossessionSprites.prompts.json`. O sprite `SpiritBattle` continua sendo usado normalmente na batalha e não foi alterado.

## Verificação

- `Tools/Test-HeroPossessed.ps1`: percurso monotônico mais alto, câmera imóvel no trono, texto completo, alternância A/B determinística, pausas escuras, relógio, fade e espera em preto.
- `Tools/Test-FinalOne.ps1`: regressão do final das cinzas.
- `Tools/Test-Castle.ps1`: condições dos três desfechos preservadas.
- `Tools/Test-UnityScripts.ps1`: compilação com as bibliotecas reais da Unity 6000.6.5f1.
- Na Unity: `Tools > Dragon Quest > Validate Possessed Ending` e `Preview Possessed Ending`. A prévia permite animar ou saltar para tapete, escadas, trono, raio A, raio B, fade e preto. A validação verifica o alpha dos três sprites, o rosto acima da coroa, a extensão preta e os pixels do herói preservados nos dois quadros de raio.
