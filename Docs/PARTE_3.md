# Parte 3 — combate por turnos

Esta página registra o encontro básico da parte 3. Para jogar e validar a versão atual, com dois inimigos e habilidades adicionais, use `PARTE_4.md`.

## Como iniciar

1. Saia de Play e aguarde a Unity importar e compilar os scripts.
2. Abra `Assets/Scenes/CidadePrototipo.unity` e inicie Play.
3. Clique dentro da janela Game e caminhe ao norte da fonte, em direção ao castelo.
4. Aproxime-se do personagem vermelho com a placa GUARDA e pressione E.
5. A interface de batalha aparece. A exploração fica bloqueada até o resultado.

## Equipe e ações

| Personagem | Papel | HP | MP | Ações iniciais |
| --- | --- | --- | --- | --- |
| Aren | Atacante | 100 | 20 | Ataque, golpe forte, defender |
| Lia | Healer | 80 | 36 | Ataque, curar, defender |
| Bram | Tank | 150 | 15 | Ataque, defender |

O guarda tem 150 HP. Velocidade ordena os turnos: Aren, Lia, guarda, Bram. Cada combatente vivo age uma vez por rodada. Empates usam o ID do combatente em ordem ordinal.

- Clique em uma ação e depois no alvo correspondente.
- Defender age imediatamente sobre o próprio usuário, sem seleção de alvo.
- Ataque não consome MP. Golpe forte custa 4 MP; cura custa 6 MP.
- Cura aceita somente aliados vivos e feridos. Não revive e não ultrapassa o HP máximo.
- Habilidades sem MP suficiente ficam desabilitadas.
- O inimigo age automaticamente após uma breve pausa, priorizando o aliado vivo com menor proporção de HP.
- Ao terminar, clique em Voltar para a cidade. O ouro, o baú e a posição de exploração permanecem.

## Regras básicas

```text
Dano físico = máximo(1, ataque do usuário + poder da habilidade - defesa do alvo)
Cura = poder da habilidade + magia do usuário, limitada ao HP máximo do alvo
Defender = metade do dano físico, arredondada para cima
```

Defesa dura até o início do próximo turno do usuário. Combatentes derrotados não são alvos nem recebem novos turnos. Todas as validações acontecem antes do gasto de MP e da mudança de turno.

## Roteiro de verificação manual

- Iniciar batalha por E e confirmar três aliados, um inimigo e suas barras de HP/MP.
- Tentar andar ou usar E durante a batalha: o herói não deve se mover ou abrir interações da cidade.
- Escolher ataque e depois o guarda: HP do alvo diminui e o turno passa.
- Selecionar uma habilidade e cancelar: não deve consumir turno, HP ou MP.
- Usar golpe forte: gastar 4 MP e causar mais dano que ataque básico.
- Esperar o inimigo agir automaticamente. Conferir a mensagem e a redução de HP do alvo.
- No turno de Lia, curar um aliado ferido. A cura não deve ultrapassar o máximo.
- Sem feridos, selecionar cura: informar ausência de alvos; cancelar e escolher outra ação.
- Defender e confirmar redução de dano até o início do próximo turno.
- Vencer usando ataques e curas; confirmar tela de vitória e retorno à mesma posição na cidade.
- Para testar derrota, defender com todos repetidamente; confirmar tela de derrota e retorno à cidade.
- Conversar com o aldeão, entrar/sair da loja e examinar o baú após retornar. O progresso de exploração não deve reiniciar.
- Iniciar outra batalha: equipe e guarda devem começar com HP/MP completos.
- Confirmar ausência de erros vermelhos no Console e legibilidade da interface no tamanho da janela Game.

## Organização do código

- `Combat/AbilityDefinition`: dados de uma habilidade e seu tipo de alvo.
- `Combat/CombatantDefinition`: atributos e habilidades disponíveis.
- `Combat/CombatantState`: HP, MP e defesa próprios de cada instância.
- `Combat/BattleSession`: validação de ações, aplicação de efeitos, rodadas e resultado.
- `Combat/EnemyAI`: decisão automática do inimigo.
- `PrototypeBattleFactory`: dados iniciais do encontro; podem migrar para ScriptableObjects depois.
- `PrototypeBattleController`: liga as regras à exploração e agenda o turno do inimigo.
- `PrototypeBattleHud`: interface e desenhos provisórios.
- `BattleInteraction`: ponto no mapa que inicia a batalha.

## Verificações automatizadas

Na raiz do repositório:

```powershell
./Tools/Test-Combat.ps1
./Tools/Test-ExplorationProgress.ps1
```

Os testes usam o SDK .NET fornecido com a Unity. Cobrem dano, cura, defesa, MP, alvos inválidos, combatentes derrotados, ordem de turnos, instâncias independentes e partidas completas com vitória e derrota. A interface e a integração com a Unity ainda exigem o roteiro em Play.

## Limites desta entrega

Encontro repetível com um guarda; cada nova batalha inicia com recursos completos. Ainda não há recompensas de batalha, itens em combate, animações finais, progressão de nível ou persistência da equipe entre encontros. Proteção de aliado, provocar, condições e outras habilidades entram na parte 4. A ampliação do mapa permanece adiada até a base de combate e habilidades estar pronta.
