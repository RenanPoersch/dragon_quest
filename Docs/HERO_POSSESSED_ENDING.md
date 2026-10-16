# Final de possessão — O novo hospedeiro

`GameEnding.HeroPossessed` agora apresenta uma animação automática na sala do trono. A regra de combate continua a mesma: derrotar o tirano antes do espírito possui Aren, inclusive em derrota simultânea dos dois. Os valores do enum e o carregamento de jornadas concluídas continuam compatíveis.

A câmera percorre verticalmente uma pintura contínua: começa no tapete, chega aos degraus e revela Aren de baixo para cima. O percurso dura 27 segundos depois do fade inicial de 1,4 s. Ao final, o herói e o trono ficam centralizados, com o corpo sentado inteiro dentro do enquadramento. A câmera permanece imóvel durante o restante do epílogo.

O texto de `EndingNarrative.HeroPossessedEpilogue` rola na lateral esquerda enquanto a câmera se move. Usa a fonte, cor, sombra, velocidade e recorte gradual das linhas do final das cinzas. Após a última linha sair, há pausa, fade preto de 3 s e 1 s de preto antes de reiniciar a cena e voltar ao menu. A sequência usa tempo não escalado e só começa depois da primeira renderização. Não exige continuar páginas.

Depois da parada da câmera, relâmpagos duplos esparsos iluminam uma silhueta do espírito no fundo preto. Os intervalos variam e a silhueta desaparece completamente entre eles. O shader `PossessedThroneComposite` usa somente o fundo preto da pintura como máscara; trono, Aren e colunas ocultam o espírito. A textura aprovada `SpiritBattle` fornece sua identidade. O compositor não modifica as imagens originais e não aplica flashes brancos na tela inteira. Material, RenderTexture e sombra temporários são descartados ao sair.

## Arte e autoria

`Assets/Resources/PixelArt/Endings/PossessedThrone.png` é o PNG original gerado pelo `imagegen` integrado, copiado integralmente com seu RGBA. Referências: retrato de Aren para identidade e estilo sério, interior da sala para arquitetura e tapete, retrato do tirano para o acabamento. Resolução nativa 1024 × 1536. Importação Point/Clamp, sem compressão, mipmaps ou redimensionamento. Prompt completo em `Docs/ArtInputs/PossessedThrone.prompt.txt`.

## Verificação

- `Tools/Test-HeroPossessed.ps1`: percurso monotônico, câmera imóvel no trono, texto completo, relâmpagos intermitentes, relógio, fade e espera em preto.
- `Tools/Test-FinalOne.ps1`: regressão do final das cinzas.
- `Tools/Test-Castle.ps1`: condições dos três desfechos preservadas.
- `Tools/Test-UnityScripts.ps1`: compilação com as bibliotecas reais da Unity 6000.6.5f1.
- Na Unity: `Tools > Dragon Quest > Validate Possessed Ending` e `Preview Possessed Ending`. A prévia permite animar ou saltar para tapete, escadas, trono, relâmpago, fade e preto.
