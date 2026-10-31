> Registro histórico de implementação. As regras e instruções vigentes estão em [Docs/Current](../../Current/README.md).

# Vida, MP e condicoes persistentes

HP e MP atuais, veneno e a proxima acao bloqueada por Correntes sao guardados em cada membro da equipe ao encerrar um encontro. A proxima batalha, inclusive chefes, usa esses valores. Personagens a zero HP entram derrotados, ficam fora da ordem de turnos e podem ser revividos. Defesa, Shield e vinculos com inimigos derrotados terminam com o encontro.

Veneno nao expira por turnos: causa 5% do HP maximo, arredondado para cima, no fim de cada acao do afetado e a cada 3 segundos de exploracao ativa, inclusive parado. Menus, dialogos e perda de foco pausam o relogio. Trocar de mapa nao remove o efeito. Antidoto/Purificar curam veneno; chegar a zero HP limpa as condicoes. Se a equipe inteira cair, recupera-se na hospedaria, como na derrota em batalha, mantendo Limit.

Sprites: veneno verde, Correntes azul/lilas e Provocar vermelho/laranja. Quando ha mais de uma condicao, veneno tem prioridade, seguido de Correntes e Provocar. Derrotados mantem sua apresentacao de derrota. Dano de DOT tem metadado proprio e numeros flutuantes roxos, na batalha e no mapa. No mapa o sprite de Aren reflete suas condicoes; os numeros identificam cada membro atingido.

O inventario mostra HP/MP atuais e permite usar consumiveis no personagem selecionado durante exploracao. Saves guardam os vitais e as condicoes; campos opcionais preservam a leitura de saves anteriores, que iniciam com HP/MP cheios.

Validacao: `Tools/Test-PartyConditions.ps1`, `Tools/Test-Saves.ps1`, `Tools/Test-UnityScripts.ps1` e `Tools/Test-PostBattle.ps1`. A suite geral de combate ja falha no HEAD anterior em uma expectativa antiga de conversao fisica de Thunder por Infusao; reproduzida em copia temporaria do commit original.
