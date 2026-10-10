# Parte 5A — matérias, afinidades e Limit Break

Esta página registra a introdução das matérias. A versão atual permite trocar/remover equipamentos, devolvendo suas matérias ao inventário, e usar consumíveis em batalha. Use `PARTE_5B.md` para validar esses comportamentos.

## Usar o menu

1. Saia de Play, aguarde a importação e abra `Assets/Scenes/CidadePrototipo.unity`.
2. Entre em Play e clique na janela Game. Pressione **I** para abrir o menu na cidade ou no interior da loja.
3. Escolha Aren, Lia ou Bram. A classe e seu bônus aparecem acima dos equipamentos.
4. Clique em uma matéria na lista da direita; depois clique no encaixe de destino à esquerda. Pode mudar de personagem antes de clicar no destino.
5. Os encaixes ligados por `--` formam um par. Arma tem um par; armadura, dois. Ligações não atravessam pares nem equipamentos.
6. Transferir uma matéria que já está equipada troca os itens entre origem e destino. Se a origem for o inventário, a matéria antiga do destino volta ao inventário.
7. Para remover, clique no encaixe sem uma matéria selecionada e use **Remover do encaixe**. A prévia das ações é atualizada no menu.
8. Feche com I, Esc ou o botão. Movimento e interações ficam bloqueados enquanto o menu está aberto. O menu não abre em batalha nem sobre diálogo.

## Tipos e combinações

| Tipo / cor | Função | Matérias disponíveis |
| --- | --- | --- |
| Magia / verde | Concede magias e comandos de recuperação | Raio, Cura, Purificação, Vida |
| Suporte / azul | Modifica a verde ou vermelha no mesmo par | All |
| Summon / vermelha | Concede uma invocação | Fênix |
| Aprimoramento físico / roxa | Concede comandos físicos ou modifica dano mágico ligado | Força, Corte, Guardião, Infusão |

All e Infusão sozinhas não concedem ações. A ordem das matérias no par não importa. Duas matérias que não formam combo ainda concedem seus comandos individuais; um modificador incompatível não muda o comando.

| Par | Resultado |
| --- | --- |
| Raio + All | Thunder em todos os inimigos vivos; 9 MP |
| Fênix + All | Invocar Fênix em todos os inimigos vivos; 18 MP |
| Raio + Infusão | Thunder convertido em golpe físico individual; 6 MP |
| Fênix + Infusão | Invocação convertida em golpe físico individual; 12 MP |
| Cura + All | Cura de todos os aliados vivos e feridos; 9 MP |
| Purificação + All | Remove veneno de todos os aliados vivos afetados; 6 MP |
| Vida + All | Revive todos os aliados derrotados; 18 MP |

All multiplica o custo original por 1,5, arredondado para cima, e cobra apenas uma vez por ação. Infusão mantém custo e poder originais, mas usa o atributo ataque. Não converte cura, purificação ou reviver. All não modifica comandos de matérias roxas.

Magia e invocação usam `máximo(1, magia do usuário + poder - defesa do alvo)`. Golpes físicos usam ataque no lugar de magia. Defender reduz ambos pela metade. Proteger intercepta apenas dano físico individual; provocar atrai ofensivas individuais físicas e mágicas. Ataques em área ignoram provocação e interceptação, preservando a defesa individual.

A invocação já funciona como comando de dano, mas sua criatura, animação e áudio serão acrescentados na etapa de arte. Não existem progressão de nível de matéria ou restrições por personagem nesta entrega.

## Classes como bônus

| Personagem / classe | Afinidade |
| --- | --- |
| Aren / atacante | +20% sobre o ataque base da arma equipada |
| Lia / healer | +25% sobre o poder base das ações de magia, invocação e cura |
| Bram / tank | +25% sobre a defesa base da armadura equipada |

As contribuições são arredondadas para baixo. A arma inicial dá 5 de ataque: Aren recebe 6. A armadura dá 6 de defesa: Bram recebe 7. Uma cura de poder 18 recebe poder 22 com Lia; o atributo magia é somado depois. Infusão converte o comando em físico, então esse comando não recebe afinidade mágica. Percentual de reviver e duração de condições não são ampliados pela afinidade.

Qualquer personagem pode equipar qualquer matéria. Guardião pode dar provocar/proteger a Lia; Cura pode permitir que Bram cure. A diferença vem de atributos individuais, equipamento e afinidade. Os personagens mantêm seus atributos iniciais; a classe não decide quais comandos estão disponíveis.

Ataque, defender e Limit Break são comandos comuns. Os demais vêm das matérias. Cada item de matéria tem uma identidade própria: cópias iguais podem ser equipadas separadamente, inclusive para ter Thunder individual e Thunder + All no mesmo personagem.

## Limit Break e carry over

- Cada aliado tem sua própria barra de 0 a 100.
- Ao perder HP, ganha `arredondar para cima(HP realmente perdido × 100 / HP máximo)`, limitado a 100.
- Dano considera a defesa e o HP disponível. Overkill não gera carga extra. Veneno conta como dano; em proteção, quem recebe o golpe acumula a carga.
- Cura e ressurreição não apagam a barra. Morte e resultado da batalha também não a apagam.
- A 100%, Limit Break atinge todos os inimigos com poder físico 35. Gasta toda a barra e nenhum MP. Uma tentativa inválida conserva carga, MP e turno.
- A carga permanece ao retornar à cidade e iniciar outra batalha. HP/MP e condições de batalha reiniciam nesta fase; a barra e a configuração das matérias continuam na equipe.
- Persistência é em memória durante a sessão Play. Sair de Play ou fechar o jogo reinicia esta versão; salvamento em arquivo ainda não está implementado.

## Roteiro de verificação em Play

1. Abra I e confirme as quatro cores, os três personagens, uma arma com dois encaixes e uma armadura com quatro.
2. Transfira Cura de Lia para Bram. A prévia deve mostrar Curar em Bram e retirar Curar de Lia. Mude para Lia e confirme que a classe e afinidade permanecem.
3. Remova Cura de Bram, confira sua volta ao inventário e reequipe em Lia. Remover não deve duplicar ou perder a matéria.
4. Aren começa com Raio + All no segundo par da armadura. Entre em batalha pelo guarda ao norte da fonte; role a lista de ações, selecione Thunder + All e confirme todos os inimigos. Ambos devem perder HP, com gasto único de 9 MP.
5. Volte à cidade e coloque Fênix + All em um mesmo par de Lia. Na batalha seguinte, confirme Invocar Fênix + All e o gasto único de 18 MP.
6. Substitua All por Infusão. Confira o comando físico individual na prévia e na batalha. Teste também Raio + Infusão.
7. Mova All para outro par ou equipamento: a magia deve voltar a alvo único. Inverta as posições da magia e suporte no mesmo par: o combo deve continuar funcionando.
8. Teste Cura + All com dois aliados feridos. Os aliados feridos recebem cura com um custo; aliado com HP cheio não é incluído. Teste ausência de alvo e cancelar sem gastar recursos.
9. Observe a barra de Limit ao receber dano. Para carregá-la, faça os aliados defenderem repetidamente, deixando o guarda e soldado atacarem. Se a equipe perder, volte à cidade e confira as barras em I.
10. Inicie outra batalha. As barras devem conservar a carga. Use Limit Break quando chegar a 100%; todos os inimigos devem receber dano e a barra voltar a zero, sem gasto de MP.
11. Confira que I não abre o menu durante batalha ou diálogo, que o herói não se move enquanto o menu está aberto e que pode voltar a andar ao fechá-lo.
12. Confirme ausência de erros vermelhos no Console, legibilidade dos nomes de combo, rolagem das ações e indicadores de HP/MP/Limit.

## Código e validação

- `Equipment/MateriaDefinition.cs`: categoria, comandos e identidade de cada cópia.
- `Equipment/EquipmentLoadout.cs`: equipamentos e seus pares de encaixes.
- `Equipment/PartyProgress.cs`: equipe, afinidades, inventário, transferência e barras persistentes.
- `Equipment/MateriaResolver.cs`: gera comandos a partir das matérias e resolve os combos.
- `Combat/LimitGauge.cs`: acúmulo e consumo da barra.
- `Combat/BattleSession.cs`: valida os comandos gerados, dano mágico e efeitos em área sobre aliados.
- `Prototype/PrototypePartyFactory.cs`: catálogo e configuração inicial para testar.
- `Prototype/PrototypeMateriaHud.cs`: menu provisório.

Os testes usam o SDK fornecido pela Unity:

```powershell
./Tools/Test-Combat.ps1
./Tools/Test-ExplorationProgress.ps1
```

As 1.153 verificações de combate incluem transferências sem duplicação, comandos retirados ao remover matéria, uso livre por classe, os quatro tipos de combo, ligações independentes, cura em área, afinidades, dano real, carga individual e conservação de Limit ao concluir um encontro e iniciar o seguinte. A contagem inclui verificações de ações em partidas completas; não representa 1.153 cenários separados.

Compilação isolada e testes das regras não confirmam a aparência nem os cliques no Editor. O roteiro em Play permanece necessário. A continuação da parte 5 adicionará troca de armas/armaduras e consumíveis; as compras ficam para a parte 6.
