# Parte 9 — menu inicial e salvamento

Ao iniciar Play em `Assets/Scenes/CidadePrototipo.unity`, o jogo abre um menu com **três slots**. Cada linha oferece Novo jogo e, quando existe uma jornada válida, Continuar. A exploração e seus atalhos ficam suspensos enquanto o menu está aberto.

Novo jogo inicia na ilha com o kit econômico e grava o slot escolhido. Um slot ocupado exige confirmar **Substituir e começar**; Cancelar conserva a jornada. Os demais slots são independentes. Continuar restaura o último salvamento, incluindo mapa e coordenadas exatas do personagem.

## Quando salva

- Automaticamente depois de uma vitória, incluindo ouro e a recompensa do dragão.
- Automaticamente ao aprender o poema, resolver/coletar segredos, avançar ou reiniciar os monólitos e abrir os selos.
- Automaticamente depois de abrir o baú, avançar a entrega, comprar, vender, equipar ou desequipar equipamento/matéria.
- Manualmente na hospedaria: E na hospedeira → Salvar jornada, ou **F5** fora de diálogo/menu/combate.
- Os três finais ficam registrados automaticamente. Continuar uma jornada concluída reapresenta sua cena final; **Voltar ao menu** permite escolher outro slot ou uma nova jornada.

Andar ou trocar de mapa, por si só, não salva. A posição restaurada é a do último salvamento, não necessariamente a última posição antes de sair de Play. Não há save no meio de uma batalha. Uma derrota comum não dispara autosave; os itens gastos e o Limit permanecem na sessão e entram no próximo salvamento feito fora do combate. HP/MP continuam sendo recriados completos em cada encontro, como nas entregas anteriores.

O aviso **Salvo / slot N** confirma a gravação. Aguarde esse aviso antes de parar Play depois de uma alteração. Se a escrita falhar, o jogo mostra a mensagem; tente salvar novamente na hospedaria. Os finais e as outras mecânicas continuam com arte/interface provisórias.

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
2. Escolha Novo jogo em um slot vazio. Confira ilha, 50 moedas, duas poções e um antídoto. Abra o inventário, retire/recoloque uma matéria e aguarde o aviso de save.
3. Vá à periferia, abra o baú e compre uma poção. Confira ouro, quantidade e estoque. Pare/reinicie Play e escolha Continuar no mesmo slot: personagem deve reaparecer na posição da compra, mantendo todos esses valores. Revisitar o baú não paga novamente e sua tampa aparece aberta.
4. Avance a entrega, salve/carregue no meio dela, conclua e compre outro consumível. Recarregue: a etapa, a reposição já usada e o estoque após a compra devem permanecer. Não deve receber 30 moedas outra vez.
5. Ative Lua nos monólitos e aguarde o save. Recarregue: Lua continua acesa. Termine Estrela → Sol, resolva o altar e pegue a escama. Abra a passagem, recarregue e confira rochas afastadas, travessia livre e segredos conservados.
6. Vença o dragão, equipe a armadura e encaixe matérias nos dois pares. Recarregue: Limit, armadura e combos permanecem; o dragão continua libertado e não entrega outra armadura. O espírito reaparece ao lado do tirano quando a armadura estiver equipada.
7. Na hospedaria, mova o herói até um lugar fácil de reconhecer e use F5. Ande para longe e reinicie Play. Continuar deve trazer o personagem exatamente à posição do F5. Repita pelo diálogo da hospedeira.
8. Faça Novo jogo em um segundo slot. Confira o kit inicial. Reinicie e retorne ao primeiro slot: seu progresso anterior permanece. Em um slot ocupado, teste Cancelar e depois a substituição confirmada.
9. Entre numa batalha e pare Play antes de encerrá-la. Continuar deve recuperar o save anterior ao combate. Vença uma batalha e aguarde o aviso: o novo ouro, consumíveis gastos e Limit devem ser restaurados, sem repetir recompensa.
10. Conclua cada um dos finais usando jornadas separadas. Confira o aviso de gravação, avance as páginas e volte ao menu. Continuar deve abrir o mesmo desfecho, sem permitir explorar ou lutar novamente naquela jornada concluída.

## Validação técnica

`Tools/Test-Saves.ps1` testa o serializador e os arquivos reais fora do Editor, usando uma pasta de testes. No ambiente automático, executar com `-WorkDirectory ../../work/SaveChecks` evita a restrição de escrita na pasta temporária do Windows. Os arquivos de teste ficam fora do Git.

Passaram **107 verificações de salvamento**, cobrindo roundtrip de toda a jornada, todos os onze mapas, três finais, equipe com espaços vazios, cópias individuais de matérias/equipamentos, combo Raio + All, Limit, passos intermediários, não repetição de recompensas, três slots, backup, escrita interrompida/arquivo em uso, versão incompatível e estados inválidos. Também passaram 1.983 de combate, 159 de segredos, 58 de caminhos da periferia e 269 de viagens/interiores; os scripts compilaram com as bibliotecas da Unity 6000.6.5f1.

Clique, teclado, atualização visual dos objetos carregados, caminho real em LocalLow e transição entre menu/exploração/finais ainda precisam do roteiro em Play. Não foi aberta outra instância do Editor para executar estes testes.
