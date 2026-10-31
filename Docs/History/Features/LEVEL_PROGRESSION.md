> Registro histórico anterior ao acabamento final. Consulte [a documentação vigente](../../Current/README.md).

# Níveis, evolução de atributos e críticos

Todos os heróis de uma nova jornada começam no nível 1, com 0 XP e 5% de chance de crítico. A progressão pertence a cada `PartyMember`, permanece entre mapas e batalhas e é incluída no salvamento manual.

## XP e níveis

O XP dos inimigos derrotados é somado e dividido igualmente entre os heróis participantes, incluindo aliados que terminaram caídos. Sobras inteiras são distribuídas de 1 em 1 XP na ordem da equipe, preservando o total. Heróis fora da batalha não entram no divisor nem recebem XP. Uma batalha em andamento ou uma derrota não concede XP, mesmo com inimigos já eliminados. Reabrir o resultado não repete o pagamento.

| Inimigo | XP por unidade |
| --- | ---: |
| Lobo da floresta | 20 |
| Soldado | 25 |
| Bandido pirata | 30 |
| Golem da pedreira | 50 |
| Guarda do Rei Rubro | 60 |
| Dragão corrompido | 300 |
| Rei Rubro | 250 |
| Espírito maligno | 500 |

A patrulha rende 85 XP no total: 29 para o primeiro herói e 28 para cada um dos outros dois. Três piratas rendem 90 XP no total: 30 por herói. Libertar o rei divide os 500 XP do espírito derrotado entre os participantes, com o Rei Rubro vivo. Os finais de possessão e destruição não são vitórias e não concedem XP. A regra anterior de ouro dos finais é preservada.

Os valores ficam em `CombatantDefinition.ExperienceReward`; mudar uma formação altera sua recompensa conforme os inimigos presentes. O saldo de ouro cheio não impede ganhar XP. Se o pagamento de ouro for tentado novamente, o XP não se repete.

A curva inicial custa `100 + 50 × (nível atual − 1)` XP para o próximo nível: 100 para chegar ao nível 2, mais 150 para o 3, mais 200 para o 4. O XP total necessário ao nível N é `25 × (N − 1) × (N + 2)`. Uma recompensa pode subir vários níveis e conserva a sobra de XP. O limite inicial é nível 99, com 247.450 XP totais; valores excedentes são descartados sem overflow.

## Crescimento inicial por nível

| Classe | HP | MP | Ataque | Defesa | Magia | Velocidade |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Atacante / Aren | +10 | +2 | +1 | +1 | +1 | +1 |
| Healer / Lia | +7 | +5 | +1 | +1 | +2 | +1 |
| Tank / Bram | +14 | +2 | +1 | +1 | +1 | +1 |

Todos ganham também **+0,25 ponto percentual de chance de crítico por nível**: 5% no nível 1, 5,25% no nível 2 e 29,5% no nível 99. São valores iniciais de balanceamento, concentrados em `PartyProgress.cs` e `CharacterProgression.cs`.

O crescimento entra antes dos bônus de equipamentos e dos percentuais das matérias. Trocar ou remover equipamento não altera nível nem XP. Os atributos são derivados do nível a partir dos valores base: carregar um save não acumula o crescimento novamente. A próxima batalha usa os novos atributos e mantém a regra existente de iniciar com HP/MP completos.

## Críticos

Ataques físicos, magias, invocações ofensivas e drenagem podem causar crítico. O multiplicador é fixo em **1,25×**, com sorteio individual por alvo em ações de área. A chance pertence ao atacante; proteção usa a defesa e as afinidades de quem intercepta.

A defesa usa uma curva percentual calibrada: `fator = 205200 / (205200 + 290 × defesa + 79 × defesa²)`. Com dano original de 100, defesa 0 conserva 100, defesa 10 conserva 95 e defesa 30 conserva 72. A continuação arredondada para baixo é: defesa 20 → 84, 40 → 59, 50 → 49 e 100 → 20. A mesma fórmula vale para aliados, inimigos, dano físico, magia, invocação e drenagem.

A ordem de cálculo é ataque/magia e poder → dano inimigo ×4 → defesa percentual → afinidade elemental → crítico → Defender → Shield. O multiplicador inimigo entra antes do arredondamento da defesa, para um golpe de 100 causar exatamente 95 ou 72 nos pontos definidos. Defesa, afinidade e crítico arredondam para baixo; as reduções de 50% arredondam para cima. O mínimo após a defesa é 1 para aliados e 4 para inimigos. Cura, itens, veneno e o meteoro roteirizado não usam essa curva de defesa nem fazem sorteio de crítico. Limit e drenagem usam o HP realmente perdido.

O dano flutuante existente recebe a marca de crítico e usa sua cor de destaque. O relatório da ação indica `critico x1,25`. A apresentação definitiva de nível e XP fica para outra etapa; o relatório de vitória já registra o XP e os níveis ganhos.

## Salvamento e validação

O save inclui somente o XP total de cada membro; nível e atributos são reconstruídos a partir dele. O novo campo é opcional, mantendo a leitura dos saves anteriores da versão 1: heróis sem XP salvo começam no nível 1. XP negativo ou acima do limite é recusado.

- `Tools/Test-LevelProgression.ps1`: limiares, sobra e vários níveis, crescimento por classe, equipamentos/matérias, críticos físicos/mágicos/drenagem, área, afinidades, defesa, Shield, overflow, XP por inimigo, aliados caídos, derrotas, finais e pagamento único.
- `Tools/Test-Defense.ps1`: curva percentual para aliados e inimigos, defesa crescente, dano físico/mágico/drenagem, afinidades, crítico, Defender, Shield, dano mínimo e overflow.
- `Tools/Test-Saves.ps1`: roundtrip de XP individual e atributos, evolução independente da jornada carregada, saves antigos e valores inválidos, além das verificações anteriores de persistência.
- `Tools/Test-UnityScripts.ps1`: compilação dos scripts com as bibliotecas da Unity, sem iniciar outra instância do Editor.

A suíte histórica `Tools/Test-Combat.ps1` ainda para na expectativa antiga de Infusão converter Thunder em dano físico. Essa falha foi reproduzida numa cópia do código anterior a esta entrega; a suíte atual `Tools/Test-MateriaRevision.ps1` valida a regra vigente e passou.
