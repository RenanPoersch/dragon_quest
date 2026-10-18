# Efeitos dos summons e status da equipe

| Summon | Dano em todos os inimigos | Efeito sobre a equipe | MP |
|---|---|---|---:|
| Fênix | Fogo baixo, poder 10 | Revive os derrotados e recupera 80% do HP máximo de cada aliado | 12 |
| Titan | Terra baixo, poder 10 | Shield: reduz todo dano recebido em 50% durante 3 turnos próprios de cada aliado | 16 |
| Bahamut | Caos alto, poder 70 | — | 20 |

A cura da Fênix soma 80% do HP máximo ao HP atual, limitada ao máximo. Quem está derrotado revive com 80% do máximo. O percentual arredonda para cima e não recebe aumento pela afinidade de classe. A recuperação ocorre mesmo se o dano da invocação derrotar o último inimigo. Cada invocação gasta MP e uma ação apenas uma vez.

O Shield reduz ataques físicos, mágicos e veneno. Cada personagem perde uma duração ao concluir seu próprio turno; a ação em que o conjurador invoca Titan não desconta um turno do escudo recém-aplicado. Nova aplicação renova a duração, sem acumular a redução. Defender e Shield combinam suas reduções. Derrota limpa o escudo; personagens derrotados não recebem Shield.

À esquerda da barra de HP aparecem ícones de todos os status ativos simultaneamente: veneno, Shield, Defender, protegido, provocado, correntes, preparação de ataque e derrotado. Os ícones mostram os turnos restantes quando houver duração. Os glifos de pixel são desenhados pelo HUD, sem dependência de fontes de emoji.

Os efeitos de equipe ficam nos dados das habilidades e são preservados ao resolver a matéria, incluindo All e a afinidade do personagem. As animações de invocação continuam iguais; Fênix também exibe o efeito de cura nos aliados.

Validação: compilação completa com bibliotecas da Unity; testes de cura/revive, golpe final, dano elemental, redução física/mágica/veneno, duração por aliado, reaplicação, derrota, matéria e múltiplos status. Testes existentes de summons/cast e animações de magia também passaram. Não foi realizada uma partida na Unity.

Para repetir os testes de regras: Tools/Test-SummonEffects.ps1.
