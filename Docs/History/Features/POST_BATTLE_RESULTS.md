> Registro histórico anterior ao acabamento final. Consulte [a documentação vigente](../../Current/README.md).

# Resultados de pos-batalha

Apos uma vitoria, o ultimo impacto e o desaparecimento dos inimigos terminam antes de abrir a tela azul. Cada participante tem sprite, nivel, XP ganho e total, progresso e XP ate o proximo nivel. O estado e capturado antes da recompensa; desenhar ou reabrir a tela nao concede XP novamente. Derrotas mantem o fluxo de recuperacao existente.

O cabecalho mostra o XP TOTAL do encontro. Esse total e dividido entre os herois participantes, incluindo caidos; cada fileira mostra apenas a parcela recebida. Sobras de XP inteiro seguem a ordem da equipe: 85 XP entre tres herois rende 29, 28 e 28. Herois fora da batalha nao entram na divisao.

Quem sobe de nivel usa a folha de comemoracao de quatro quadros, com salto e Up! acima do personagem. Fontes Pixel Operator locais. Enter, Espaco ou o botao continuam depois da contagem de XP e da celebracao; o final do castelo segue o fluxo existente.

Teste automatizado: Tools/Test-PostBattle.ps1. Sprites originais e prompts: Docs/ArtReferences/ArtInputs/HeroVictory.prompt.json. Conferencia visual em Play pendente.
