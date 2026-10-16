# Cristal de salvamento animado da Pousada

A Pousada tem um cristal azul-turquesa flutuante sobre um círculo luminoso, bem à esquerda da sala, alinhado à faixa central do piso. O jogador se aproxima e pressiona **E** para gravar o slot atual. A ação usa `PrototypeSaveController.SaveManual`, com a mesma validação e mensagem de resultado do salvamento existente. A hospedeira e a recepção conservam suas conversas de salvamento.

O cristal é um sprite independente da pintura do ambiente. Tem oito quadros de rotação, brilhos e partículas, reproduzidos a 6 quadros por segundo, em loop. Todos compartilham a mesma escala. A base é registrada pelo círculo inferior, para a animação ficar plantada no chão. Não há colisor: o ponto luminoso pode ser atravessado e fica fora da passagem central. O salvamento ocorre na interação, e não ao andar sobre ele ou durante a animação.

`InnSavePoint` controla apresentação, animação, recursos criados em memória e chamada de save. `PrototypeInnLayout.SavePoint` posiciona o cristal e `SaveApproach` registra um ponto próximo para validar o acesso. `PrototypeCity.CreateInn` cria o objeto e o inclui na lista de interações da Pousada.

## Arte

- [InnSavePoint.png](../Assets/Resources/PixelArt/InnSavePoint.png), PNG original RGBA de 1536×1024, grade 4×2 com oito células de 384×512.
- [Prompt integral](ArtInputs/InnSavePoint.prompt.txt). Criado com o imagegen integrado, em modo de geração com a pintura da Pousada como referência de acabamento e paleta.
- [Prévia na Pousada](InnPixelArt.preview.png), renderizada pela Unity.

PNG preservado byte a byte, com transparência, Point/Clamp, leitura habilitada, sem mipmaps, compressão ou redução. As células são amostradas em memória; o arquivo fonte não recebe edição. O material `PixelCharacter` usa o mesmo recorte de alpha e profundidade dos sprites do jogo.

## Verificação

Scripts do jogo compilaram e passaram 145 verificações de salvamento. `Tools > Dragon Quest > Validate Inn` concluiu com código 0: oito quadros com conteúdo diferente, ordem temporal, retorno ao primeiro quadro, escala e pivôs estáveis, ausência de salvamentos automáticos, uma chamada de save por interação, alcance do ponto luminoso, caminhos livres e três formatos de tela. As cenas renderizadas e os oito quadros em uso ficam em `Temp/InnSaveLeftPreview` nesta revisão, ou na pasta indicada por `DQ_INN_OUTPUT`.
