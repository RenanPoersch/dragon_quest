# Parte 4 — habilidades de cada papel

## Abrir e jogar

1. Saia de Play e aguarde a Unity importar e compilar os scripts.
2. Abra `Assets/Scenes/CidadePrototipo.unity` e entre em Play.
3. Clique na janela Game, aproxime-se do guarda vermelho ao norte da fonte e pressione **E**.
4. Confira três aliados à esquerda e dois inimigos à direita.
5. Clique na habilidade e depois no alvo. Corte amplo pede uma confirmação para todos os inimigos; defender age imediatamente.

## Equipe atual

| Personagem | HP | MP | Habilidades |
| --- | --- | --- | --- |
| Aren — atacante | 100 | 28 | Ataque, golpe forte, corte amplo, defender |
| Lia — healer | 80 | 60 | Ataque, curar, purificar, reviver, defender |
| Bram — tank | 150 | 30 | Ataque, provocar, proteger, defender |

O guarda tem 180 HP e 24 MP; o soldado tem 90 HP. A ordem inicial é Aren, Lia, guarda, Bram, soldado. Os recursos foram ajustados para experimentar as habilidades. Estes números ainda são provisórios.

| Habilidade | Custo | Efeito |
| --- | --- | --- |
| Ataque | 0 MP | Dano físico em um inimigo |
| Golpe forte | 4 MP | Dano físico com poder 14 em um inimigo |
| Corte amplo | 7 MP | Dano físico com poder 5 em cada inimigo vivo; paga uma vez |
| Defender | 0 MP | Metade do dano físico, arredondada para cima, até o próximo turno do usuário |
| Curar | 6 MP | Recupera 18 + magia do usuário; aceita aliado vivo e ferido |
| Purificar | 4 MP | Remove veneno de um aliado vivo, inclusive da própria Lia |
| Reviver | 12 MP | Ressuscita um aliado derrotado com 30% do HP máximo, arredondado para cima |
| Provocar | 3 MP | Um inimigo direciona seus golpes individuais ao usuário por duas ações do inimigo |
| Proteger | 4 MP | O usuário recebe o próximo golpe individual contra outro aliado; expira no início do próximo turno do protetor |

## Condições e inimigos

- O guarda alterna **golpe venenoso → ataque → varredura**. O veneno custa 4 MP e a varredura custa 5 MP. Sem MP suficiente, ele usa outra ofensiva disponível; ataque básico continua gratuito.
- O soldado usa ataque básico. Os inimigos escolhem o aliado válido com menor proporção de HP; provocar limita os alvos dos golpes individuais.
- Veneno causa **5% do HP máximo**, arredondado para cima, ao fim de cada ação do afetado, durante três ações. Defender não reduz esse dano. Reaplicar renova a duração sem acumular dano. Purificar remove o efeito antes do dano de fim de turno.
- Proteger usa a defesa do protetor e consome a proteção naquele golpe. Se o golpe interceptado aplica veneno, o protetor recebe o veneno novo. Veneno já aplicado permanece no personagem afetado.
- Ataques em área atingem todos os adversários vivos, mesmo sob provocação, e não são interceptados por proteger. A defesa individual ainda reduz o dano de cada personagem.
- Morte remove as condições do derrotado. Morte do protetor ou provocador desfaz seus vínculos com os outros combatentes.
- Reviver não dá um turno extra. Se o aliado ainda possui uma posição futura na rodada, pode agir nela. Se sua posição passou ou ele ficou fora da ordem montada no início da rodada, retorna na próxima rodada, por velocidade.
- Uma ação inválida não consome MP, turno nem duração de condições. O veneno não continua depois de uma ação que já encerrou a batalha.

As barras mostram **VENENO** e **PROVOCADO** com o número de ações restantes, além de **DEFESA** e **PROTEGIDO**. A descrição da habilidade aparece ao selecioná-la.

## Roteiro em Play

1. No primeiro turno de Aren, use corte amplo e confirme todos os inimigos. As duas barras devem diminuir; Aren gasta 7 MP uma vez.
2. No turno de Lia, use ataque. O guarda deve usar golpe venenoso em Aren. Confira a indicação VENENO 3.
3. Com Bram, use proteger em Aren. O ataque seguinte do soldado deve acertar Bram; Aren conserva o veneno já aplicado.
4. Na rodada seguinte, faça Aren defender. O veneno ainda causa 5 HP ao fim da ação. Com Lia, purifique Aren: gaste 4 MP e remova o indicador.
5. Use provocar com Bram no guarda. Nos dois próximos turnos do guarda, golpes individuais devem escolher Bram; a varredura ainda atinge a equipe inteira.
6. Selecione purificar sem aliados envenenados ou reviver sem derrotados. Deve aparecer a mensagem apropriada, sem gasto de MP; cancele e escolha outra ação.
7. Em uma nova batalha, deixe Aren atacar o soldado e faça Lia e Bram defenderem, sem curar, purificar ou provocar. Quando Aren for derrotado, use reviver com Lia. Ele retorna com 30 HP e o turno segue sem duplicação.
8. Confira cura, MP insuficiente, vitória e derrota, retorno à cidade, interações anteriores e nova batalha com recursos completos e sem condições antigas.
9. Confirme que os cinco botões de Lia estão visíveis, os dois inimigos aparecem e não existem erros vermelhos no Console.

## Código e verificações

- `Assets/Scripts/Combat/AbilityDefinition.cs`: tipo de efeito, alvo, custo e veneno aplicado pelo ataque.
- `Assets/Scripts/Combat/CombatantState.cs`: condições e recursos próprios de cada combatente.
- `Assets/Scripts/Combat/BattleSession.cs`: efeitos, interceptação, duração e ordem dos turnos.
- `Assets/Scripts/Combat/EnemyAI.cs`: alternância de ações e escolha de alvo.
- `Assets/Scripts/Prototype/PrototypeBattleFactory.cs`: habilidades e dados do encontro atual.
- `Assets/Scripts/Prototype/PrototypeBattleHud.cs`: seleção, descrições e indicadores provisórios.
- `Tests/BattleChecks.cs`: regressão das regras básicas com o encontro original como fixture.
- `Tests/AbilityChecks.cs`: efeitos novos, interações entre habilidades e estratégias completas no encontro atual.

Na raiz do projeto, execute:

```powershell
./Tools/Test-Combat.ps1
./Tools/Test-ExplorationProgress.ps1
```

A parte 4 passou em 542 verificações de combate e 15 de exploração, além de compilação isolada com as bibliotecas da Unity 6000.6.5f1. Essa contagem inclui as verificações realizadas durante partidas completas. O roteiro em Play permanece necessário para confirmar aparência e integração.

## Próximas entregas

A equipe ainda reinicia com recursos completos a cada encontro. Inventário, equipamentos, afinidades, compras, recompensas e progressão da história entram nas partes seguintes. A arte continua provisória e o mapa conserva sua área atual.
