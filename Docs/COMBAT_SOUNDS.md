# Sons de combate

As 56 prévias da versão 2 foram aprovadas e copiadas sem alteração dos WAV: PCM16 estéreo, 48 kHz. `CombatSoundManifest.json` preserva os hashes e as durações das prévias. As bases CC0 e os créditos estão em `COMBAT_SOUND_CREDITS.md`.

`BattleSoundCatalog` resolve os IDs reais das habilidades, incluindo nomes preservados por matérias, All, Infusão, atores inimigos numerados, os três limites e as duas resoluções do meteoro. Itens e ações não reconhecidas não tocam um golpe incorreto.

A cena principal cria o controlador de efeitos junto do controlador de música, mantendo as regras de batalha utilizáveis também em cenas/testes sem áudio. A apresentação de uma ação aceita emite um único evento. `BattleSoundController` agenda o som pelo relógio da animação, com o contato do próprio áudio alinhado ao impacto visual. Preparações, sopro, limites e meteoro mantêm a sequência completa desde o início da ação. Os WAV não são recortados, transpostos ou ressintetizados. Oito fontes 2D independentes permitem que as caudas terminem ao começar o golpe seguinte; não alteram o turno ou a música.

As fontes têm volume 0,85 e respeitam o AudioListener/master já existente. Perda de foco, desativação ou saída da batalha interrompe os efeitos; a última cauda do meteoro pode acompanhar a transição ao final. Recarregar um save reinicializa a ligação, sem eventos duplicados ou fontes extras.

Execute `Tools/Test-CombatSounds.ps1` para verificar compilação, integridade/importação dos 56 WAV, resolução das variantes, reprodução e avanço de amostras nas fontes reais da Unity, eventos aceitos/rejeitados, sincronização, foco, reaproveitamento de fontes e continuidade da música em um projeto isolado.
