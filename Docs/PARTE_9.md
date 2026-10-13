# Parte 9 — menu inicial e salvamento

Ao iniciar Play em `Assets/Scenes/CidadePrototipo.unity`, o jogo abre um menu com **três slots**. Cada linha oferece Novo jogo e, quando existe uma jornada válida, Continuar. A exploração e seus atalhos ficam suspensos enquanto o menu está aberto.

Novo jogo inicia na ilha com o kit econômico, sem gravar arquivos. Um slot ocupado exige confirmar **Começar nova jornada**; seu save anterior permanece até salvar manualmente a nova jornada. Cancelar conserva a jornada. Os demais slots são independentes. Continuar restaura o último salvamento, incluindo mapa e coordenadas exatas do personagem. Um slot vazio só passa a oferecer Continuar depois do primeiro salvamento manual.

## Quando salva

- Somente manualmente: E na hospedeira → Salvar jornada, ou **F5** no mapa global ou na hospedaria, fora de diálogo/menu/combate.
- Vitórias, segredos, baús, entrega, compras, vendas, equipamentos, matérias e finais não gravam arquivos.
- Os três slots, saves existentes e backups continuam funcionando no mesmo formato.

O salvamento automático foi removido. A posição restaurada é a do último salvamento manual, não necessariamente a última posição antes de sair de Play. Os itens gastos, recompensas e Limit permanecem na sessão e entram no próximo save manual. HP/MP continuam sendo recriados completos em cada encontro, como nas entregas anteriores.

O aviso **Salvo / slot N** confirma a gravação. Salve no mapa global ou na hospedaria antes de parar Play se quiser conservar as alterações. Se a escrita falhar, o jogo mostra a mensagem; tente salvar novamente. Os finais recém-concluídos permanecem apenas na sessão: voltar ao menu e continuar recupera o último save anterior à batalha. Saves antigos que já registram um final continuam reapresentando esse desfecho. Os finais e as outras mecânicas continuam com arte/interface provisórias.

## Dados e arquivos

O save inclui ouro, baús abertos, entrega e reposição já feita, estoque de cada produto, equipamento possuído/equipado, matérias e seus encaixes, consumíveis, Limit individual, poema/segredos/selos, dragão derrotado, armadura única, mapa, posição e final.

Na configuração atual do Windows, os arquivos ficam em:

```text
%USERPROFILE%\AppData\LocalLow\DefaultCompany\dragon_quest\Saves
  slot-1.json
  slot-2.json
  slot-3.json
```

O caminho vem de `Application.persistentDataPath`. Saves são dados locais da aplicação, fora do repositório. Gravação usa arquivo temporário, flush e substituição atômica. A revisão anterior válida fica em `.json.bak`; um principal corrompido pode recuperar essa cópia, com aviso ao carregar. A cópia pode estar um salvamento atrás. Uma versão incompatível bloqueia Continuar, sem regressão silenciosa para um backup antigo.

O formato JSON tem versão explícita. IDs estáveis apontam para o catálogo do jogo; saves não redefinem stats ou habilidades. A restauração valida equipe, propriedade exclusiva, encaixes, estoques, mapa/posição e dependências dos segredos antes de instalar a jornada. `link.xml` preserva os tipos usados por reflexão no serializador em builds com remoção de código.

## Roteiro em Play

1. Pare Play, aguarde a importação e abra `CidadePrototipo`. Inicie Play: confira três slots, ausência de movimentação atrás do menu e nenhum erro vermelho no Console.
2. Escolha Novo jogo em um slot vazio. Confira ilha, 50 moedas, duas poções e um antídoto. Altere matérias e pare Play sem salvar. Ao reiniciar, o slot deve continuar vazio.
3. Comece novamente e vá à hospedaria. Mova o herói até um ponto fácil de reconhecer e use F5. Confira **Salvo / slot N**. Ande para longe e reinicie Play: Continuar deve recuperar a posição exata do F5. Repita pelo diálogo da hospedeira. No mapa global, mova o herói até outro ponto, use F5 e reinicie Play: Continuar deve restaurar o mapa global e a posição salva. Em outros interiores e durante combate, F5 não deve gravar.
4. Abra o baú, compre uma poção e altere um equipamento. Nenhum aviso de save deve aparecer. Reinicie sem salvar: ouro, baú, estoque e equipamento devem voltar ao último save manual. Repita as alterações, salve na hospedaria e recarregue: agora devem permanecer, com a tampa aberta e sem repetir a recompensa.
5. Avance a entrega e os segredos. Retorne à hospedaria e salve antes de recarregar. Confira etapa, reposição, poema, monólitos e escama; alterações posteriores sem save devem ser descartadas ao recarregar.
6. Vença o dragão, equipe a armadura e encaixe matérias nos dois pares. Não deve haver gravação automática. Salve na hospedaria e recarregue: Limit, armadura e combos permanecem, sem outra recompensa do dragão.
7. Faça Novo jogo em um segundo slot e salve manualmente nele. O primeiro slot deve permanecer intacto. Em um slot ocupado, teste Cancelar e confirme uma nova jornada sem salvar: reiniciar deve conservar o save anterior. Depois confirme novamente e salve na hospedaria para testar a substituição.
8. Vença uma batalha ou conclua um final e volte ao menu sem salvar. Continuar deve recuperar o último save manual, anterior ao combate/desfecho. Saves antigos que já registravam um final devem continuar carregando a cena final.

## Validação técnica

`Tools/Test-Saves.ps1` testa o serializador e os arquivos reais fora do Editor, usando uma pasta de testes. No ambiente automático, executar com `-WorkDirectory ../../work/SaveChecks` evita a restrição de escrita na pasta temporária do Windows. Os arquivos de teste ficam fora do Git.

`SaveChecks` cobre roundtrip de toda a jornada, todos os onze mapas, três finais, equipe com espaços vazios, cópias individuais de matérias/equipamentos, combo Raio + All, Limit, passos intermediários, não repetição de recompensas, três slots, backup, escrita interrompida/arquivo em uso, versão incompatível e estados inválidos. A revisão sem autosave também verifica que alterações na jornada carregada conservam o arquivo anterior até uma gravação explícita e que essa gravação persiste os valores acumulados. Resultados atualizados ficam em `VALIDATION.md`.

Clique, teclado, atualização visual dos objetos carregados, caminho real em LocalLow e transição entre menu/exploração/finais ainda precisam do roteiro em Play. Não foi aberta outra instância do Editor para executar estes testes.
