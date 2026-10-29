# Cristal de salvamento animado da Pousada

A Pousada tem um cristal azul-turquesa flutuante sobre um círculo luminoso, bem à esquerda da sala, alinhado à faixa central do piso. Basta andar sobre o círculo para abrir a tela de seleção dos três slots, mesmo com um slot atual definido. Não é preciso pressionar **E**. A gravação só acontece após escolher um slot e confirmar a substituição quando ele já estiver ocupado. A hospedeira e a recepção conservam suas conversas de salvamento.

O cristal é um sprite independente da pintura do ambiente. Tem oito quadros de rotação, brilhos e partículas, reproduzidos a 6 quadros por segundo, em loop. Todos compartilham a mesma escala. A base é registrada pelo círculo inferior, para a animação ficar plantada no chão. Não há colisor: o ponto luminoso pode ser atravessado e fica fora da passagem central. Pisar no círculo abre a tela uma única vez; depois de salvar ou cancelar, é preciso sair dele e voltar para reabrir. A animação não salva. Diálogos, inventário, combate, outros menus e perda de foco bloqueiam a abertura. Carregar uma jornada sobre o círculo não reabre a tela: é preciso sair e pisar nele novamente.

O piso caminhável ao redor do cristal inclui espaço para contorná-lo pelos quatro lados. O recuo foi ampliado para evitar bloqueios invisíveis junto ao círculo, considerando a largura e a altura da base do personagem.

`InnSavePoint` controla apresentação, animação, recursos criados em memória e entrada do jogador na área de raio 0,4. Ele chama `PrototypeSaveController.OpenInnSaveMenu`, que pausa a movimentação e abre a seleção sem gravar. `PrototypeInnLayout.SavePoint` posiciona o cristal e `SaveApproach` registra um ponto próximo para validar o acesso. `PrototypeCity.CreateInn` cria o objeto; ele não participa das interações por tecla E.

## Arte

- [InnSavePoint.png](../Assets/Resources/PixelArt/InnSavePoint.png), PNG original RGBA de 1536×1024, grade 4×2 com oito células de 384×512.
- [Prompt integral](ArtInputs/InnSavePoint.prompt.txt). Criado com o imagegen integrado, em modo de geração com a pintura da Pousada como referência de acabamento e paleta.
- [Prévia na Pousada](InnPixelArt.preview.png), renderizada pela Unity.

PNG preservado byte a byte, com transparência, Point/Clamp, leitura habilitada, sem mipmaps, compressão ou redução. As células são amostradas em memória; o arquivo fonte não recebe edição. O material `PixelCharacter` usa o mesmo recorte de alpha e profundidade dos sprites do jogo.

## Verificação

Scripts do jogo compilaram e passaram 145 verificações de salvamento. `Tools > Dragon Quest > Validate Inn` concluiu com código 0: oito quadros com conteúdo diferente, ordem temporal, retorno ao primeiro quadro, escala e pivôs estáveis, ausência de gravação automática, abertura ao pisar, trava até sair do círculo, bloqueio durante outros menus, carregamento sobre o círculo e alcance do ponto luminoso, caminhos livres e três formatos de tela. As cenas renderizadas e os oito quadros em uso ficam em `Temp/InnPreview` nesta revisão, ou na pasta indicada por `DQ_INN_OUTPUT`.
