# Planejamento aprovado — parte 6

Decisões acordadas em 10 de outubro de 2026. As partes 6A, 6B e 6C têm commits separados. Preços e quantidades são o balanceamento inicial e podem ser ajustados ao jogar.

## Estrutura e regras

- Três lojas: itens, equipamentos e matérias, com entrada, vendedor e saída na cidade.
- Todos os produtos têm estoque limitado. Consumíveis retornam ao estoque inicial em marcos da história, uma vez por marco; equipamentos e matérias não repõem.
- Ouro vem de batalhas, baús e pequenas missões. Vitória no encontro atual: 35 moedas; derrota: zero. Baú atual: 25 moedas. Primeira missão de entrega entre comerciantes: 30 moedas; futuras missões: 30 a 60.
- Cada vendedor compra objetos de sua categoria por 50% do preço, arredondado para baixo. Objetos em uso precisam ser desequipados antes; armadura do dragão e outros itens de história são protegidos.
- Compra vai ao inventário. Equipar continua sendo feito no menu de inventário, devolvendo as matérias dos equipamentos trocados.
- Menu Comprar/Vender com ouro, preço, estoque, quantidade e descrição. Equipamentos mostram atributos base e pares ligados antes da compra.
- Magias básicas, suportes e aprimoramentos físicos são vendidos. Summons e matérias raras vêm da exploração.

## Início da equipe

50 moedas, 2 poções e 1 antídoto. Cada personagem tem arma/armadura simples. Aren inicia com Força e Raio; Lia com Cura e Purificação; Bram com Guardião. As matérias podem ser transferidas livremente. Extras de teste, Fênix, armas e armaduras melhores ficam para compra ou exploração.

## Catálogos

| Loja de itens | Preço | Estoque |
| --- | ---: | ---: |
| Poção (40 HP) | 10 | 12 |
| Éter (15 MP) | 25 | 6 |
| Antídoto | 6 | 6 |
| Pluma da Fênix (revive 30% HP) | 50 | 2 |

| Equipamento | Atributos base | Pares | Preço | Estoque |
| --- | --- | ---: | ---: | ---: |
| Espada de ferro | +9 ataque | 1 | 60 | 2 |
| Cajado rúnico | +3 ataque, +6 magia | 2 | 80 | 2 |
| Armadura de ferro | +10 defesa | 1 | 70 | 2 |
| Manto arcano | +3 defesa, +4 magia | 3 | 100 | 2 |

| Matéria | Preço | Estoque |
| --- | ---: | ---: |
| Fogo, Gelo, Raio | 50 cada | 2 de cada |
| Cura | 50 | 2 |
| Purificação | 30 | 2 |
| Vida | 120 | 1 |
| All | 90 | 2 |
| Força | 40 | 2 |
| Corte | 70 | 1 |
| Guardião | 60 | 1 |
| Infusão | 80 | 1 |

## Entregas

- 6A: Fogo/Gelo/Raio, fraqueza x1,5, resistência x0,5, elementos preservados em All/Infusão e informação na interface.
- 6B: três lojas, compras/vendas, estoques e kit econômico inicial.
- 6C: recompensas de batalha, primeira missão entre comerciantes e reposição por progresso.

HP/MP continuam completos no início dos encontros nesta fase. Ouro, inventário, estoque e progresso ficam em memória durante Play; o salvamento em disco entra em uma entrega posterior.

As três entregas foram implementadas em commits separados. O roteiro da versão atual está em `PARTE_6C.md`; os preços continuam sujeitos ao balanceamento após o teste em Play.
