# Parte 6A — elementos, fraquezas e resistências

Fogo, Gelo e Raio são matérias verdes de dano mágico com poder base 22 e custo 6 MP. Fênix é uma invocação de Fogo, com poder 35 e custo 12 MP. Qualquer personagem pode equipá-las.

Esta página registra a entrega 6A. Na versão 6B, a equipe começa com o kit econômico e compra matérias nas lojas; use `PARTE_6B.md` para validar a versão atual.

## Regras

- Fraqueza: 150% do dano normal. Resistência: 50%. Os outros elementos causam 100%.
- Primeiro se calcula atributo ofensivo + poder - defesa, com mínimo de 1. Depois se aplica a afinidade elemental, arredondando para baixo. Defender reduz o resultado pela metade, arredondando para cima. O mínimo final continua em 1.
- All mantém o elemento e calcula cada alvo separadamente. Fogo/Gelo/Raio + All custam 9 MP uma vez por ação; Fênix + All custa 18 MP.
- Infusão mantém o elemento, passa a usar ataque físico e pode ser interceptada por Proteger. Nesse caso, contam a defesa e a afinidade de quem recebeu o golpe.
- A afinidade mágica de Lia continua dando +25% sobre o poder base de magia e cura. O comando transformado em físico por Infusão usa seu poder base original.
- Limit, ataques físicos comuns e veneno não têm elemento nesta entrega. A carga de Limit continua dependendo do HP efetivamente perdido, depois de todas as reduções.
- As afinidades pertencem ao combatente e são copiadas para o estado de cada batalha. Não criam restrições de matéria por classe.

| Inimigo atual | Fraqueza | Resistência |
| --- | --- | --- |
| Guarda do tirano | Raio | Fogo |
| Soldado | Fogo | Gelo |

Gelo causa dano normal no guarda e reduzido no soldado; os próximos encontros poderão ter outros perfis. As afinidades aparecem nas fichas dos inimigos, e o relatório da ação identifica elemento, fraqueza e resistência.

## Roteiro em Play

1. Abra `Assets/Scenes/CidadePrototipo.unity` no Editor 6000.6.5f1. Aguarde a compilação e confirme ausência de erros vermelhos no Console.
2. Entre em Play e pressione I. Fogo e Gelo estão na lista de matérias; role a lista se necessário.
3. Em Aren, coloque Fogo no encaixe 3 da armadura, ao lado de All no encaixe 4. A prévia deve mostrar Fogo + All, 9 MP, poder 22, elemento Fogo.
4. Feche o menu e interaja com o guarda ao norte da fonte usando E. Confira as afinidades nas fichas do guarda e do soldado.
5. No primeiro turno de Aren, use Fogo + All: o guarda perde 9 HP (resistência), o soldado perde 30 HP (fraqueza), e Aren gasta 9 MP. O relatório deve indicar os dois resultados.
6. Retorne à cidade depois de concluir a batalha. Troque Fogo por Gelo no mesmo encaixe e inicie outro encontro: Gelo + All causa 18 no guarda e 10 no soldado, gastando 9 MP.
7. Troque a matéria por Raio no mesmo par. Thunder + All causa 27 no guarda e 20 no soldado.
8. Experimente Fogo/Gelo/Raio ligados a Infusão. O comando deve ser individual, custar 6 MP e manter o elemento, usando o ataque do personagem.
9. Transfira as matérias a Lia ou Bram. Os comandos devem continuar disponíveis e as classes devem conceder somente os bônus já descritos.
10. Confira que consumíveis, troca/remoção de equipamento e conservação de Limit continuam funcionando.

Nesta entrega, o inventário generoso de teste permanece para experimentar os combos. O kit econômico (2 poções, 1 antídoto), as 50 moedas iniciais e a distribuição de equipamento/matérias serão aplicados junto das lojas na parte 6B. O planejamento acordado está em `PLANO_LOJAS.md`.

## Código e validação

- `Assets/Scripts/Combat/ElementalAffinities.cs`: elementos e perfis imutáveis de afinidade.
- `AbilityDefinition.cs` e `CombatantDefinition.cs`: elemento dos comandos e perfil do combatente.
- `BattleSession.cs`: multiplicadores, arredondamento, proteção e relatório de dano.
- `Equipment/MateriaResolver.cs`: elemento conservado em All e Infusão.
- `PrototypePartyFactory.cs` e `PrototypeBattleFactory.cs`: matérias novas e perfis do encontro atual.
- `PrototypeBattleHud.cs` e `PrototypeMateriaHud.cs`: informações elementais na interface.
- `Tests/ElementalChecks.cs`: regras elementais e interações com os sistemas existentes.

Execute `Tools/Test-Combat.ps1` e `Tools/Test-ExplorationProgress.ps1` no PowerShell. Passaram 1.362 verificações de combate e 15 de exploração. A contagem inclui verificações durante partidas completas, não 1.362 cenários separados. Todos os scripts do jogo também compilaram com as bibliotecas da Unity 6000.6.5f1.

A verificação visual/interativa no Editor e a geração de executável permanecem para validação posterior. A próxima entrega é a parte 6B: as três lojas, compras, vendas e o kit inicial econômico.
