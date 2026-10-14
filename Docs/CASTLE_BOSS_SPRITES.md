# Tirano e espírito no castelo

O interior do castelo usa a postura inicial de `TyrantBattle.png` e `SpiritBattle.png`, as mesmas artes já registradas no combate. O tirano substitui a célula antiga de `NpcRoster`; o espírito substitui a malha geométrica ao lado do trono.

`PixelSpriteLayout.ReadBattleIdle` encontra o intervalo transparente próximo da primeira divisão da folha e recorta somente a postura inicial em memória. A segunda pose do espírito começa antes de um quarto da imagem, portanto uma grade regular incluiria parte de outro desenho. Os PNGs originais, cores e alpha permanecem intactos.

Os sprites usam o material `PixelCharacter`, orientação vertical acompanhando a câmera e pivô na base. Alturas de referência: 1,65 para o tirano e 2,1 para o espírito, com a compensação oblíqua usada pelos demais personagens. A base do tirano conserva a altura anterior de 0,15. Collider, posição, diálogo e início do combate permanecem iguais. `TyrantInteraction` continua revelando o espírito somente quando alguém equipa a Armadura dracônica.

Validação técnica: scripts compilados com as bibliotecas da Unity 6000.6.5f1; 603 verificações existentes de sprites/materiais/movimento passaram. Uma conferência adicional com o alpha real das duas folhas passou em 1.633 verificações, incluindo contorno inteiro, exclusão das outras poses, pivô, folha vazia, dados inválidos e registro das artes de batalha. Importadores Point/Clamp, leitura, alpha, ausência de compressão/mipmaps/redução e hashes dos PNGs foram conferidos.

As suítes gerais existentes já falham antes desta alteração: `Test-Oblique.ps1` no enquadramento do vulcão e `Test-Combat.ps1` na Infusão de Thunder. A falha de enquadramento foi reproduzida também com os quatro scripts originais de HEAD. A conferência nativa em Play permanece pendente: reiniciar a execução, visitar o castelo, conferir os dois personagens e equipar/remover a Armadura dracônica para alternar a visibilidade do espírito.
