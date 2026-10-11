# Parte 6C — recompensas, entrega e reposição

A economia das três lojas agora recebe ouro de batalhas, baú e uma pequena missão. Elementos e lojas foram registrados nos commits anteriores, 6A e 6B.

## Regras

- A vitória concede automaticamente a soma do ouro definido em cada inimigo derrotado, antes de retornar à cidade: Guarda do tirano 25 moedas e Soldado 10, totalizando 35 na patrulha atual. O resultado identifica os inimigos, seus valores e o total. Outras formações terão recompensas diferentes. Derrota, mesmo após eliminar parte dos inimigos, não concede ouro nem remove o saldo existente.
- Cada cópia de um inimigo conta separadamente. Aliados não entram na recompensa; inimigos sem ouro podem ter valor zero. Valores negativos são recusados, e uma soma acima do limite não concede pagamento parcial.
- A recompensa pertence àquela batalha: novas vitórias podem pagar novamente, mas reler o resultado ou retornar à cidade não duplica o pagamento.
- O baú da praça continua dando 25 moedas uma vez por sessão.
- A missão de entrega começa no vendedor de itens. Aceite o pacote, entregue ao vendedor de matérias e volte ao primeiro vendedor para receber 30 moedas.
- O pacote é um objeto de história representado pelo progresso da missão. Não aparece como mercadoria, não ocupa encaixes e não pode ser vendido.
- Ao receber a recompensa da missão, os consumíveis do vendedor retornam às quantidades iniciais: 12 poções, 6 éteres, 6 antídotos e 2 plumas. Isso repõe a loja, não o inventário do jogador.
- A reposição acontece uma vez por marco e substitui o estoque restante pelo estoque inicial, sem somar unidades. A entrega também paga uma única vez.
- Equipamentos e matérias conservam o estoque restante, mesmo depois da missão. Abrir/fechar lojas e vencer batalhas não repõe produtos.
- O HUD da cidade e o menu do vendedor mostram a etapa da missão. As ações aparecem somente no vendedor e no passo corretos.
- Saldo no limite impede o recebimento sem concluir a recompensa pendente ou estourar o inteiro. Ouro, missão, estoques e equipamentos ficam em memória durante Play; sair de Play reinicia a demo.

Novos marcos de reposição poderão ser ligados ao avanço da história nas próximas entregas. A entrega entre comerciantes não conta como um dos três segredos do dragão.

## Roteiro em Play

1. Abra `Assets/Scenes/CidadePrototipo.unity` na Unity 6000.6.5f1. Aguarde a compilação e confira o Console antes de entrar em Play.
2. Confirme 50 moedas, 2 poções e 1 antídoto. O HUD deve orientar a procurar o vendedor de itens para a entrega.
3. Entre na loja de itens a noroeste, use E no vendedor e compre 1 poção. Ouro: 40; estoque do vendedor: 11; seu estoque: 3.
4. Clique em **Aceitar entrega**, no topo direito do menu. Feche e saia. O HUD deve orientar a levar o pacote à loja de matérias, a nordeste.
5. Entre na loja de matérias e clique em **Entregar pacote**. Ouro permanece 40; a orientação passa a pedir o retorno ao vendedor de itens.
6. Volte à loja de itens e clique em **Receber 30 moedas**. Ouro: 70; estoque do vendedor: 12 poções novamente; seu estoque permanece 3. O HUD informa conclusão.
7. Compre mais uma poção: ouro 60 e estoque do vendedor 11. Saia e volte: o estoque deve continuar 11, sem outra recompensa ou opção de aceitar a mesma missão.
8. Abra o baú e vença a patrulha ao norte da fonte. Ouro deve aumentar em 25 e depois em 35. A tela de vitória deve mostrar **+35 moedas: Guarda do tirano (25) + Soldado (10)**; aguardar nela e retornar à cidade não pode duplicar o pagamento.
9. Vença outra patrulha com os mesmos dois inimigos: são mais 35 legítimos, sem reposição de estoque. Uma derrota conserva o saldo e não paga recompensa.
10. Confira que as três portas devolvem o herói aos locais corretos e que o inventário, as compras e as interações continuam disponíveis depois dos encontros.

### Caminho para testar Fogo + All sem repetir batalhas

Em outra sessão de Play, sem compras anteriores:

1. Complete a missão (+30), abra o baú (+25) e vença uma patrulha (+35). Com as 50 iniciais, haverá **140 moedas**.
2. Compre **Fogo por 50** e **All por 90** na loja de matérias. Restam zero moedas e uma unidade de cada produto na loja.
3. Feche o vendedor e pressione I. Em Aren, coloque Fogo e All no segundo par da armadura (encaixes 3 e 4).
4. Inicie uma nova batalha: Fogo + All deve custar 9 MP, causar 9 de dano no guarda resistente e 30 no soldado vulnerável. A partir daí, outras escolhas de compra podem ser comparadas com equipamento e suprimentos.

## Código e validação

- `Assets/Scripts/Combat/CombatantDefinition.cs`: propriedade `GoldReward`, configurada por inimigo em `PrototypeBattleFactory.cs`. Definições sem valor explícito assumem zero.
- `Assets/Scripts/Progression/BattleRewards.cs`: soma das recompensas dos inimigos derrotados, composição no resultado e registro de pagamento por batalha vencida.
- `Progression/DeliveryQuest.cs`: etapas, pacote, pagamento único e marco de reposição.
- `Shops/ShopService.cs`: reposição exclusiva de consumíveis, uma vez por ID de marco.
- `PrototypeBattleController.cs`: concede recompensa ao concluir ações do jogador, itens ou IA; `PrototypeBattleHud.cs` mostra o resultado.
- `PrototypeShopController.cs` / `PrototypeShopHud.cs`: ações de aceitar, entregar e receber; `PrototypeHud.cs` mostra o objetivo.
- `Tests/ProgressionChecks.cs`: recompensas, isolamento, falhas, reposição, seleções repetidas e economia do primeiro encontro.

Após o ajuste de ouro por inimigo, passaram **1.518 verificações de combate/inventário/lojas/progresso** e **15 de exploração**. A contagem inclui verificações durante partidas completas. Todos os scripts compilaram com as bibliotecas da Unity 6000.6.5f1.

Os testes incluem uma vitória com a equipe econômica sem compras nem uso de consumíveis, seguida de missão, baú, compra de Fogo/All e reconhecimento do combo na batalha seguinte. Incluem recompensas diferentes por tipo de inimigo, soma de várias instâncias, exclusão de aliados, valores zero/negativos, limites numéricos e eliminação parcial seguida de derrota. Também incluem repetição de chamadas sem ouro duplicado, estoque restrito às categorias corretas e recompensa pendente em caso de saldo no limite.

Execute `Tools/Test-Combat.ps1`, `Tools/Test-ExplorationProgress.ps1` e `Tools/Test-UnityScripts.ps1`. A aparência e os cliques na Unity ainda precisam do roteiro em Play; não foi gerado executável.

A próxima etapa planejada é ampliar a cidade antes de criar os três segredos, o dragão e os caminhos finais.
