> Registro histórico de implementação. As regras e instruções vigentes estão em [Docs/Current](../../Current/README.md).

# Parte 9 — menu inicial e salvamento

Ao iniciar Play em `Assets/Scenes/Game.unity`, o jogo abre uma **tela preta com a escama ancestral centralizada**, seguida de **New game** e **Continue**. Continue muda para a tela dos **três saves**, com local e progresso; slots vazios ou indisponíveis ficam desabilitados. New game inicia a nova jornada diretamente. Na seleção de Continue, **Back** ou **Esc** retorna à tela inicial. Setas ou W/S selecionam opções; Enter ou Espaço confirma. A exploração e seus atalhos ficam suspensos enquanto o menu está aberto.

Novo jogo inicia na ilha com o kit econômico, sem gravar arquivos, e usa o primeiro slot livre para o salvamento manual. Se os três estiverem ocupados, o primeiro salvamento manual abre a seleção de destino e pede confirmação de **Salvar jornada** antes de substituir um save. Isso também ocorre se o slot escolhido automaticamente passar a ser ocupado antes do primeiro salvamento. Cancelar a confirmação conserva os arquivos; Back ou Esc na seleção retorna à jornada. Os demais slots são independentes. Continuar restaura o último salvamento, incluindo mapa e coordenadas exatas do personagem. Um slot vazio só passa a oferecer Continuar depois do primeiro salvamento manual.

## Quando salva

- Na hospedaria, andar sobre o círculo do cristal abre a seleção dos três slots. Só escolher um slot grava a jornada; slots ocupados pedem confirmação. Ao salvar ou cancelar, saia do círculo e volte para reabrir a tela.
- Também é possível salvar manualmente com E na hospedeira → Salvar jornada, ou **F5** no mapa global ou na hospedaria, fora de diálogo/menu/combate.
- Vitórias, segredos, baús, compras, vendas, equipamentos, matérias e finais não gravam arquivos.
- Os três slots, saves existentes e backups continuam funcionando no mesmo formato.

O salvamento automático foi removido. A posição restaurada é a do último salvamento manual, não necessariamente a última posição antes de sair de Play. Os itens gastos, recompensas e Limit permanecem na sessão e entram no próximo save manual. HP/MP continuam sendo recriados completos em cada encontro, como nas entregas anteriores.

O aviso **Salvo / slot N** confirma a gravação. Salve no mapa global ou na hospedaria antes de parar Play se quiser conservar as alterações. Se a escrita falhar, o jogo mostra a mensagem; tente salvar novamente. Os finais recém-concluídos permanecem apenas na sessão: voltar ao menu e continuar recupera o último save anterior à batalha. Saves antigos que já registram um final continuam reapresentando esse desfecho. Os finais e as outras mecânicas continuam com arte/interface provisórias.

## Dados e arquivos

O save inclui ouro, baús abertos, estoque de cada produto, equipamento possuído/equipado, matérias e seus encaixes, consumíveis, Limit individual, poema/segredos/selos, dragão derrotado, armadura única, mapa, posição e final.

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

1. Pare Play, aguarde a importação e abra `Game`. Inicie Play: confira o fundo totalmente preto, a escama centralizada e apenas New game / Continue. Abra Continue: confira os três saves, incluindo slots vazios desabilitados. Use Back e Esc para voltar; teste também setas e Enter. Confira ausência de movimentação atrás do menu e nenhum erro vermelho no Console.
2. Com pelo menos um slot vazio, escolha New game por clique e depois por teclado em uma nova sessão: a ilha deve aparecer diretamente, sem seleção de saves, com 50 moedas, duas poções e um antídoto. Altere matérias e pare Play sem salvar. Ao reiniciar, o slot deve continuar vazio.
3. Comece novamente e vá à hospedaria. Mova o herói até um ponto fácil de reconhecer e use F5. Confira **Salvo / slot N**. Ande para longe e reinicie Play: Continuar deve recuperar a posição exata do F5. Repita pelo diálogo da hospedeira. No mapa global, mova o herói até outro ponto, use F5 e reinicie Play: Continuar deve restaurar o mapa global e a posição salva. Em outros interiores e durante combate, F5 não deve gravar.
4. Abra o baú, compre uma poção e altere um equipamento. Nenhum aviso de save deve aparecer. Reinicie sem salvar: ouro, baú, estoque e equipamento devem voltar ao último save manual. Repita as alterações, salve na hospedaria e recarregue: agora devem permanecer, com a tampa aberta e sem repetir a recompensa.
5. Avance os segredos. Retorne à hospedaria e salve antes de recarregar. Confira estoques, poema, monólitos e escama; alterações posteriores sem save devem ser descartadas ao recarregar.
6. Vença o dragão, equipe a armadura e encaixe matérias nos dois pares. Não deve haver gravação automática. Salve na hospedaria e recarregue: Limit, armadura e combos permanecem, sem outra recompensa do dragão.
7. Faça New game novamente e salve manualmente: o próximo slot vazio deve receber a jornada e o primeiro deve permanecer intacto. Com os três slots ocupados, New game ainda deve entrar diretamente na ilha sem alterar arquivos. Use F5 na hospedaria ou no mapa global: agora a seleção de destino deve aparecer. Teste Cancelar na confirmação e Back/Esc na seleção; a jornada deve continuar e os saves devem permanecer intactos. Repita e confirme Salvar jornada para substituir apenas o slot escolhido. Confira também que Continue mantém sua seleção dos três saves.
8. Vença uma batalha ou conclua um final e volte ao menu sem salvar. Continuar deve recuperar o último save manual, anterior ao combate/desfecho. Saves antigos que já registravam um final devem continuar carregando a cena final.

## Validação técnica

`Tools/Test-Saves.ps1` testa o serializador e os arquivos reais fora do Editor, usando uma pasta de testes. No ambiente automático, executar com `-WorkDirectory ../../work/SaveChecks` evita a restrição de escrita na pasta temporária do Windows. Os arquivos de teste ficam fora do Git.

`SaveChecks` cobre roundtrip de toda a jornada, todos os onze mapas, três finais, equipe com espaços vazios, cópias individuais de matérias/equipamentos, combo Raio + All, Limit, passos intermediários, não repetição de recompensas, três slots, backup, escrita interrompida/arquivo em uso, versão incompatível e estados inválidos. A revisão sem autosave também verifica que alterações na jornada carregada conservam o arquivo anterior até uma gravação explícita e que essa gravação persiste os valores acumulados. Resultados atualizados ficam em `VALIDATION.md`.

Clique, teclado, atualização visual dos objetos carregados, caminho real em LocalLow e transição entre menu/exploração/finais ainda precisam do roteiro em Play. Não foi aberta outra instância do Editor para executar estes testes.
