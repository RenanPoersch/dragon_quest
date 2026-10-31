# Progressão de personagens

O XP total pertence a cada PartyMember. CharacterProgression deriva o nível desse total; os atributos são calculados a partir do nível e dos bônus atuais. Carregar o jogo não soma novamente o crescimento.

## Experiência

O XP dos inimigos derrotados é dividido entre os heróis que participaram e **terminaram vivos**. Caídos e personagens fora da batalha não recebem XP nem entram no divisor. Sobras inteiras seguem a ordem da equipe. Por exemplo, 85 XP entre três sobreviventes rende 29/28/28; com dois sobreviventes, 43/42, e o caído recebe zero.

Vitórias comuns e a libertação do rei concedem XP. Derrotas, fuga e os outros finais não concedem XP. A entrega acontece uma única vez, inclusive se o ouro não couber no saldo e seu pagamento for tentado depois. BattleRewards mantém recibos associados por chave fraca à sessão do encontro.

O nível máximo é 99. O total necessário ao nível N é 25 × (N − 1) × (N + 2). O custo do próximo nível é 100 + 50 × (nível atual − 1). Uma recompensa pode subir vários níveis e conserva o excedente até o limite de 247.450 XP.

## Atributos sem bônus no nível 1

| Herói | HP | MP | Ataque | Defesa | Magia | Velocidade | Crítico |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Aren | 100 | 28 | 22 | 7 | 4 | 14 | 5% |
| Lia | 80 | 60 | 9 | 5 | 20 | 12 | 5% |
| Bram | 150 | 30 | 14 | 13 | 3 | 8 | 5% |

## Crescimento por nível

| Herói | HP | MP | Ataque | Defesa | Magia | Velocidade | Crítico |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Aren | +10 | +2 | +1 | +1 | +1 | +1 | +0,25 ponto percentual |
| Lia | +7 | +5 | +1 | +1 | +2 | +1 | +0,25 ponto percentual |
| Bram | +14 | +2 | +1 | +1 | +1 | +1 | +0,25 ponto percentual |

Equipamentos, afinidades de classe e matérias alteram os valores derivados. Aren tem afinidade com o ataque da arma, Lia com o poder das habilidades mágicas/curativas e Bram com a defesa da armadura. O conteúdo base está em CharacterCatalog; a derivação fica em PartyMember e MateriaResolver.

O save guarda XP e os vitais persistentes. Saves anteriores sem XP começam no nível 1; vitais opcionais ausentes são reconstruídos pelos máximos derivados. A chance crítica é limitada a 100% e o multiplicador de dano crítico é 1,25.
