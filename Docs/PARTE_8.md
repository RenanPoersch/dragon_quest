# Parte 8 — confronto no trono e três finais

Entre no castelo pela periferia e use E perto do Rei Rubro. Termine o diálogo para iniciar a batalha; Esc cancela. Com a Armadura dracônica equipada em qualquer personagem, o espírito aparece ao lado do rei desde o começo, tanto no salão quanto como alvo com turno próprio na batalha. Guardar a armadura no inventário não revela o espírito.

## Rei Rubro e meteoro

O Rei Rubro tem 600 HP, ataque 28, defesa 10 e velocidade 7. Alterna golpes físicos individuais de poder 8 e 14, sem elemento. Ao chegar a **450 HP ou menos**, interrompe os golpes e inicia a preparação do meteoro.

São **cinco turnos próprios de preparação do Rei Rubro**: Protect no primeiro, carga crescente nos quatro seguintes e lançamento no sexto. Protect reduz todo dano recebido em 60% durante a conjuração, exibindo um escudo quando um golpe o atinge. Shield termina antes de aparecer a pose de cast, que permanece durante as ações dos demais combatentes. Não há meteoro separado durante a carga: números flutuantes de 5 a 1 indicam cada turno próprio, com fonte levemente maior que a do dano e sem rótulos. O meteoro aparece apenas no lançamento final. Ações de jogador, itens, golpes e veneno podem atualizar o limiar; a preparação não é reiniciada a cada mudança de HP.

- Com um portador vivo: a Armadura dracônica absorve o meteoro inteiro, sem dano aos presentes e sem destruir o castelo. O confronto continua; não há um segundo meteoro em loop nessa tentativa.
- Sem portador vivo: o meteoro aniquila equipe, Rei Rubro e espírito presente, ignorando Defender, Shield, Protect, defesa numérica e resistência comum a Fogo. O final de destruição começa.
- Reviver o portador antes do lançamento restaura a absorção. Correntes não impede a propriedade passiva da armadura, desde que o portador esteja vivo.
- Derrotar o espírito com o rei vivo cancela qualquer preparação e encerra imediatamente a luta.

O golpe que atravessa o limiar para em 450 HP, inclusive críticos e os cortes restantes do Omnislash. O rei conserva essa vida até o próprio turno de Protect, garantindo a entrada na fase de conjuração. Depois de lançar Protect ele volta a receber dano normalmente, com a redução de 60%; derrotá-lo durante a carga ainda leva ao final de possessão. Derrotar o espírito primeiro continua permitindo o final bom.

## Espírito maligno

O espírito tem 280 HP, defesa 8, magia 26 e velocidade 10. Não possui afinidades elementais nesta versão. Seu ciclo de ações é:

| Golpe | Efeito |
| --- | --- |
| Drenar vida | Magia individual, poder 8; recupera apenas o HP realmente retirado do alvo, até seu HP máximo |
| Correntes | O alvo perde sua próxima ação; não gasta MP, item ou Limit. As correntes se desfazem após essa ação perdida |
| Caos Magic | Magia sem elemento, poder 5, contra toda a equipe |

Provocar pode direcionar as ações individuais; Caos Magic continua atingindo todos. Proteger intercepta os golpes físicos do Rei Rubro, mas não as magias do espírito. Defender reduz os danos normais e a drenagem, mas não detém o meteoro. Morte/revivência remove Correntes; Purificação continua removendo veneno.

## Finais

| Final | Condição | Cena |
| --- | --- | --- |
| Bom — O rei libertado | Espírito derrotado com Rei Rubro vivo | O antigo rei justo agradece à equipe e retoma seu governo; a ilha é libertada |
| Ruim — Cinzas do castelo | Meteoro lançado sem portador vivo da armadura | Castelo em ruínas após a aniquilação da equipe e do Rei Rubro |
| Terceiro — O novo hospedeiro | Rei Rubro morto antes de libertar o rei | O espírito abandona o corpo do rei e possui Aren |

Matar Rei Rubro e espírito no mesmo ataque em área também dá o terceiro final, pois o rei precisa sobreviver para ser libertado. O meteoro destrutivo tem prioridade sobre essa regra, produzindo o final de destruição.

Os três finais começam automaticamente após sua condição específica. A libertação do rei e a possessão de Aren aguardam a animação da ação final e o desaparecimento do inimigo; em seguida, abrem a cena final sem tela de resultado ou confirmação. A experiência do final bom continua sendo concedida uma única vez. No meteoro destrutivo, o impacto e o clarão antecedem o corte automático para a cena do castelo. As cenas avançam sozinhas até o retorno ao menu e o registro da conquista. Continuar reapresenta um desfecho salvo; Novo jogo cria uma jornada com os estados iniciais no slot escolhido.

Uma derrota por golpes normais, antes de qualquer desfecho, abre automaticamente a tela inicial de Load Game após a animação do golpe final. Não há botão de retorno à hospedaria nem salvamento automático: carregar uma jornada restaura seu último estado salvo. A morte de toda a equipe por veneno no mapa segue o mesmo retorno. Essa derrota não conta como um dos três finais.

## Roteiro em Play

1. Pare Play, aguarde a importação e abra `Assets/Scenes/CidadePrototipo.unity`. Reinicie Play, escolha Novo jogo no menu e confira o Console.
2. Sem a armadura, entre no castelo e use E no Rei Rubro. Teste Esc para cancelar. Conclua a conversa: apenas o Rei Rubro deve ser inimigo; ele deve começar com golpes físicos, sem meteoros antecipados.
3. Ataque até o HP chegar a 450 ou menos. Confira Shield completo antes da pose de cast, somente o sprite do rei durante a carga, números flutuantes de 5 a 1 e escudo ao receber golpes. Use Defender para não matar o rei. Após a contagem, observe o lançamento, o impacto e o clarão; toda a batalha deve ser aniquilada e a cena de Cinzas do castelo deve começar automaticamente, sem confirmação.
4. Em outra tentativa, mate o Rei Rubro antes do lançamento para conferir o terceiro final, com Aren possuído. O desfecho deve acontecer imediatamente, sem permitir continuar atacando.
5. Para o caminho bom, resolva os três segredos, vença o dragão e equipe sua armadura em qualquer aliado. Recoloque as matérias retiradas pela troca. Leve cura, éteres e uma forma de reviver.
6. Volte ao salão: o espírito deve ficar visível. O inventário pode trocar o portador antes da luta, e o início do confronto deve usar a configuração final. Na batalha, confira os dois alvos, turnos independentes, drenagem, perda de ação por Correntes e dano em área.
7. Ataque o espírito preservando o rei. Golpes em área e Limit atingem os dois inimigos. Ao derrotar a sombra, a batalha termina com o rei vivo e qualquer meteoro pendente cancelado. Sem apertar teclas ou clicar, aguarde a animação do golpe e confira o início automático do agradecimento do rei, sem parada na tela de experiência.
8. Para testar a absorção, enfraqueça o Rei Rubro até 450 HP ou menos e mantenha o espírito vivo. Aguarde até o quinto turno. Com o portador vivo, o meteoro deve se contrair até ele e exibir a absorção na escama vermelha, sem tirar HP de ninguém; o confronto deve continuar.
9. Teste também o portador derrotado: reviva-o antes do lançamento para absorver; deixá-lo derrotado deve levar à destruição. Outra pessoa não absorve somente por ter resistência a Fogo.
10. Teste uma derrota comum sem atingir 450 HP ou menos no Rei Rubro. Confira o retorno automático a Load Game, sem botão de hospedaria, e carregue o último save para outra tentativa. Nenhum final deve ter sido registrado.
11. Em cada final, deixe teclado, mouse e controle sem entrada: confira a transição automática após o gatilho, a pausa da exploração e o retorno automático ao menu ao terminar a cena. Continuar deve reapresentar o mesmo desfecho. Ao criar Novo jogo, confirme a ilha inicial, 50 moedas, 2 poções, 1 antídoto, segredos ocultos e ausência da armadura da jornada anterior.

## Código e validação

- `IBattleRules` permite regras particulares de um encontro sem colocar os IDs do Rei Rubro na engine genérica.
- `CastleBattleRules` determina limiar, preparações, absorção e prioridade dos três finais.
- `PrototypeCastleBattleFactory` configura os dois inimigos e os golpes do espírito.
- `AbilityEffect.Drain`, `AbilityEffect.Bind` e `CombatantState.BoundActions` implementam drenagem e perda de ação.
- `TyrantInteraction` revela a figura do espírito conforme a armadura e inicia o confronto após diálogo concluído.
- `PrototypeEndingController` apresenta as cenas, trava a exploração e reinicia a cena configurada para build.
- `EndingNarrative` guarda os textos; `ObliqueScenery` gera rei, espírito, ruínas e figuras dos finais.

Passaram 1.983 verificações de combate/inventário/lojas/progresso, 58 de caminhos da periferia, 269 de ilha/interiores/viagens e 1.750 de câmera/malhas. A compilação usa as bibliotecas da Unity 6000.6.5f1. Os testes cobrem limiar exato e cruzado por golpe, cinco turnos com Protect, rejeição de lançamento antecipado, absorção em qualquer classe, portador derrotado/revivido, drenagem real, Correntes, ataques em área, prioridade dos finais, batalhas completas com final bom e derrota comum, e recuperação na hospedaria.

Os retratos e as prévias técnicas foram conferidos a partir das malhas originais. Prévias não são capturas da Unity. Aparência com shader, importação das imagens, cliques, diálogo, câmera final e retorno ao menu ainda precisam do roteiro em Play. Modelos, interface, efeitos de batalha e cenas são provisórios; acabamento e executável continuam para as próximas entregas. Salvamento em disco foi adicionado na parte 9.

Validação dos gatilhos automáticos: execute `Tools/Test-AutomaticEndings.ps1`. O teste compila os scripts e usa um projeto Unity temporário para conferir os três finais sem entrada do jogador, espera pela animação, frames longos, transição única, experiência do final bom e manutenção da tela de experiência nas vitórias comuns.
