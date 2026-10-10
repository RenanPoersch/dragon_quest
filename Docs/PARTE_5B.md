# Parte 5B — troca de equipamento e uso de itens

## Menu na cidade

Saia de Play, aguarde a compilação e inicie `Assets/Scenes/CidadePrototipo.unity`. Clique na janela Game e pressione **I**. O inventário contém três abas:

- **Matérias:** equipar, remover e transferir matérias entre encaixes ativos. Role a área dos encaixes para equipamentos com mais pares.
- **Equipamentos:** trocar ou remover arma e armadura, comparar ataque/defesa/magia e consultar a configuração atual de comandos.
- **Itens:** consultar o estoque compartilhado e a descrição dos consumíveis. O uso ocorre na batalha.

I, Esc ou o botão fecham o menu. Movimento e interações permanecem bloqueados enquanto o inventário está aberto. Ele não abre em batalha nem sobre um diálogo.

## Regra de matérias ao trocar equipamento

**Trocar ou remover um equipamento desequipa todas as matérias dele e as devolve ao inventário.** Os novos encaixes ficam vazios. Reequipar o equipamento antigo não restaura matérias ou combos automaticamente.

Trocar uma arma não desmonta a armadura, e trocar uma armadura não desmonta a arma. Se escolher o equipamento de outro aliado, os personagens trocam os equipamentos daquele tipo, e as matérias dos dois equipamentos voltam ao inventário. Se o destino estava sem equipamento, o outro aliado fica com aquele espaço vazio.

Selecionar o equipamento que já está em uso é uma operação sem mudança; suas matérias permanecem equipadas. Falhas de validação também não alteram equipamento ou matérias. Cada cópia de equipamento pertence ao inventário uma única vez, e não pode estar em dois personagens ao mesmo tempo.

É possível ficar sem arma e/ou armadura. O personagem usa seus atributos base, mantendo ataque, defender e Limit Break. Matérias só podem ser colocadas em equipamentos ativos. As classes continuam permitindo uso livre; a afinidade altera o bônus, sem restringir itens ou matérias. Limit não é apagado ao mudar a configuração.

## Equipamentos de teste

| Equipamento | Tipo | Ataque base | Defesa base | Magia | Encaixes ligados |
| --- | --- | --- | --- | --- | --- |
| Arma inicial | Arma | 5 | 0 | 0 | 2 / 1 par |
| Armadura inicial | Armadura | 0 | 6 | 0 | 4 / 2 pares |
| Espada de ferro | Arma | 9 | 0 | 0 | 2 / 1 par |
| Cajado rúnico | Arma | 3 | 0 | 6 | 4 / 2 pares |
| Armadura de ferro | Armadura | 0 | 10 | 0 | 2 / 1 par |
| Manto arcano | Armadura | 0 | 3 | 4 | 6 / 3 pares |

Os equipamentos extras já estão no inventário para teste. Os atributos resultam da base individual mais os bônus equipados. Aren aumenta em 20% o ataque da arma; Bram aumenta em 25% a defesa da armadura, com arredondamento para baixo. Bônus de magia do equipamento somam ao atributo magia; Lia mantém +25% no poder base de magia, invocação e cura.

## Consumíveis em batalha

No turno de um aliado, clique em **Itens**, selecione o item e depois o alvo. A descrição aparece na área de ações. Clique em Cancelar seleção ou volte para Habilidades para desistir sem consumir recursos.

| Item | Estoque inicial | Efeito | Alvo válido |
| --- | --- | --- | --- |
| Poção | 6 | Recupera 40 HP, limitada ao máximo | Aliado vivo e ferido |
| Éter | 3 | Recupera 15 MP, limitado ao máximo | Aliado vivo com MP abaixo do máximo |
| Antídoto | 3 | Remove veneno | Aliado vivo envenenado |
| Pluma da Fênix | 2 | Revive com 30% do HP máximo, arredondado para cima | Aliado derrotado |

O próprio usuário pode receber poção, éter ou antídoto. Cada uso válido consome uma unidade e o turno, sem gasto de MP ou Limit. Itens não recebem bônus de classe nem modificadores de matéria: All não transforma uma poção em cura em área.

Poção não revive, antídoto não cura HP e éter não aumenta o MP máximo. Alvos inválidos, falta de estoque, ações fora de turno ou depois do resultado não consomem item nem turno. Inimigos não usam o estoque da equipe.

Veneno ainda causa dano ao fim da ação de usar um item. Usar antídoto no próprio usuário remove o veneno antes desse dano. Reviver por item segue a mesma ordem de turnos de reviver por matéria, sem criar turnos adicionais nem limpar Limit.

Equipamentos, matérias, estoque e Limit permanecem durante a sessão Play, incluindo retorno à cidade e nova batalha. Nesta fase, HP/MP e condições reiniciam a cada encontro, mas o estoque **não** reinicia. Compras ainda não estão disponíveis; sair de Play inicia outra sessão de teste com o inventário inicial. Não há salvamento em disco.

## Roteiro de teste em Play

1. Abra I e confira as abas, os três personagens e os quatro equipamentos extras na lista.
2. Com Aren, selecione Espada de ferro e confirme Equipar selecionado. Ataque deve passar de 28 para 32. A matéria Força deve voltar ao inventário, retirando Golpe forte da configuração; as matérias da armadura permanecem.
3. Recoloque Força em um encaixe da espada. Remova a arma pela aba Equipamentos. Confirme que Força volta ao inventário e Aren fica com ataque base 22. Reequipar a espada deve deixar os encaixes vazios.
4. Transfira um equipamento de outro aliado. Confirme que as matérias dos dois equipamentos envolvidos voltam ao inventário, sem desaparecer ou duplicar.
5. Equipe o cajado rúnico e o manto arcano em Lia. Magia deve chegar a 30. Na aba Matérias, role até o último par da armadura e equipe Raio + All nos encaixes 5 e 6. Confira Thunder + All na prévia.
6. Remova a armadura de Lia. As matérias de todos os seis encaixes devem voltar ao inventário. Confirme também que ficar sem arma e armadura não causa erros ao iniciar uma batalha.
7. Para testar poção, deixe o guarda/soldado ferir um aliado. No turno de um aliado, abra Itens, escolha Poção e confirme o ferido. A quantidade cai uma unidade e o turno passa; HP não ultrapassa o máximo.
8. Use uma habilidade que gasta MP e depois recupere com Éter. Teste Antídoto em um envenenado e Pluma da Fênix em um derrotado.
9. Selecione um item sem alvos válidos e depois cancele. Quantidade e turno devem permanecer. Item esgotado deve ficar desabilitado.
10. Ao usar item estando envenenado, confira o dano de fim de turno. Antídoto no próprio usuário deve evitar esse dano. A barra de Limit permanece e só cresce se houver nova perda de HP.
11. Termine a batalha e volte à cidade. Confira o estoque na aba Itens e os equipamentos na aba Equipamentos. Nova batalha deve usar a configuração atual e conservar as quantidades restantes.
12. Confirme rolagem e legibilidade dos encaixes/listas, retorno do movimento ao fechar I e ausência de erros vermelhos no Console.

## Código e verificações

- `Equipment/EquipmentLoadout.cs`: tipo, identidade, atributos e encaixes do equipamento.
- `Equipment/PartyProgress.cs`: propriedade dos equipamentos, troca/remoção, retorno de matérias e atributos equipados.
- `Inventory/ConsumableDefinition.cs`: definição e efeito dos consumíveis.
- `Inventory/ItemInventory.cs`: pilhas, quantidades, validação e consumo.
- `Combat/BattleSession.cs`: alvos e uso de itens, com o mesmo fim de turno das habilidades.
- `Prototype/PrototypeMateriaHud.cs`: inventário em três abas e comparações.
- `Prototype/PrototypeBattleHud.cs`: seleção de itens na batalha.
- `Tests/EquipmentItemChecks.cs`: regras de equipamento e consumíveis.

Execute na raiz do projeto:

```powershell
./Tools/Test-Combat.ps1
./Tools/Test-ExplorationProgress.ps1
```

Passaram 1.247 verificações de combate/inventário e 15 de exploração. A contagem inclui verificações durante partidas completas, não 1.247 cenários separados. Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1. A aparência, os cliques e o bloqueio de movimento aguardam confirmação em Play pelo usuário.

A próxima etapa adicionará compras e reposição nas lojas. Arte, ampliação da cidade, progressão da história, salvamento e executável permanecem nas entregas seguintes.
