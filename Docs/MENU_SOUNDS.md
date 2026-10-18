# Sons de menu

Os quatro WAV aprovados nas prévias são usados diretamente, sem ressíntese, mudança de volume no arquivo ou compressão com perdas. Os assets em `Assets/Resources/MenuSounds` preservam PCM16 mono a 48 kHz e ficam pré-carregados, pois são efeitos curtos.

| Ação | Prévia escolhida | Asset | Duração |
| --- | --- | --- | --- |
| Navegar | `B_confirmar.wav` | `navigate.wav` | 0,165 s |
| Confirmar | Subida rápida, opção 2 da segunda rodada | `confirm.wav` | 0,185 s |
| Cancelar/voltar | `C_confirmar.wav` | `cancel.wav` | 0,145 s |
| New Game | Arpejo maior uma oitava acima, C6–E6–G6–C7 | `newgame.wav` | 0,460 s |

`PrototypeCity` cria um objeto de áudio de menu na própria cena. Ele possui uma fonte 2D sem loop, com volume 0,6, independente das duas fontes da música. O controle de volume já existente usa `AudioListener.volume` e também afeta os efeitos. Ao sair da cena, o objeto é destruído.

Os menus de título/saves, inventário (itens, matérias, equipamento, status e configuração), lojas, cheats e batalha disparam o efeito na ação correspondente. Mover para outra opção usa navegar; Enter/clique em uma ação usa confirmar; voltar, cancelar ou sair usa cancelar. New Game dispara somente seu arpejo, uma vez, antes do fade para a nau. Um cursor parado e telas bloqueadas não disparam áudio.

A mesma ação não reinicia o som nas várias passagens do IMGUI. Quando navegação e confirmação ocorrem no mesmo frame, confirmar tem prioridade; cancelar tem prioridade sobre confirmar e New Game sobre os demais. Os efeitos respeitam o foco e não alteram a reprodução da música.

`Tools/Test-MenuSounds.ps1` compila todos os scripts e abre um projeto isolado com os quatro efeitos. Verifica o hash de cada prévia escolhida, importação PCM, pré-carregamento, duração, reprodução real e avanço de amostras. Exercita os controladores reais do título, New Game, inventário/submenus, lojas, cheats e batalha com teclado e controle virtual, além do bloqueio por foco, prioridade no mesmo frame e continuidade da música. O Editor principal e os saves do usuário são preservados.
