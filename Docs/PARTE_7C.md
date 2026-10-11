# Parte 7C — vulcão, dragão e Armadura dracônica

Depois dos três segredos, use E na passagem para desfazer os selos. Caminhe até a área do ícone do vulcão: a entrada é automática. O novo cenário tem um caminho curto de basalto entre rochas e lava, terminando numa arena. A lava bloqueia o movimento, sem causar dano nesta versão. A saída fica ao sul.

Todos os POIs globais usam áreas de 2,8 × 2,2 unidades em torno do ícone, aceitando aproximação pelos lados acessíveis. Portas e saídas locais usam 1,4 × 1 unidade. Chegadas ficam fora das áreas; nenhum mapa muda enquanto diálogos, inventário, menu de loja ou batalha estiverem abertos. A demo agora possui 11 mapas e 20 rotas direcionais, além do retorno especial à hospedaria após derrota no dragão.

## Dragão corrompido

Use E próximo do dragão e termine a conversa para iniciar o combate. Esc cancela sem iniciar. O espírito que controla o tirano também aprisionou o dragão. A vitória liberta sua essência, entrega a armadura e concede as 100 moedas configuradas nesse inimigo, uma única vez na sessão. Visitas posteriores permitem ouvir a essência, sem repetir batalha ou recompensa.

| Atributo | Valor inicial |
| --- | --- |
| HP / MP | 520 / 0 |
| Ataque / defesa / magia / velocidade | 25 / 10 / 22 / 6 |
| Fraqueza | Gelo: dano ×1,5 |
| Resistência | Fogo: dano ×0,5 |

O ciclo é **Garra dracônica → Preparar sopro → Sopro de fogo**. A preparação consome um turno inteiro do boss e mantém um aviso visível enquanto os três aliados agem. O sopro acontece no próximo turno do dragão e atinge toda a equipe. Defender reduz seu dano pela metade; Proteger e Provocar não desviam o ataque em área. Depois, o ciclo reinicia.

O balanceamento inicial permite vencer com os equipamentos básicos, Gelo equipado e uma estratégia de cura e defesa. Não é necessário explorar uma fraqueza para validar os comandos, mas Gelo reduz bastante o tempo da batalha. Nenhum consumível ou MP é gasto pela preparação do inimigo.

## Armadura

- Uma cópia da **Armadura dracônica**, no inventário de equipamentos; não é equipada automaticamente.
- +16 de defesa base; o tank recebe +25% sobre esse bônus, resultando em +20.
- Resistência a Fogo: metade do dano após a defesa numérica. A resistência é aplicada somente enquanto o equipamento estiver em uso.
- Quatro encaixes, formando dois pares ligados. Qualquer personagem pode equipá-la; trocar ou remover equipamento continua devolvendo suas matérias ao inventário.
- Objeto de história protegido contra venda, mesmo desequipado.
- O estado registra quando ela está efetivamente equipada para a futura revelação do espírito. Tirano, espírito e finais pertencem à parte 8.

## Derrota e hospedaria

Na tela de derrota, **Voltar para a hospedaria** leva ao novo interior da hospedaria da periferia, ao sudoeste da praça. A equipe se recupera para a próxima batalha. Limit, ouro, equipamentos, matérias, segredos e passagem aberta são mantidos; consumíveis usados continuam gastos. O boss permanece disponível para outra tentativa. Não há taxa de recuperação.

A hospedaria também pode ser visitada normalmente pela porta de seu prédio. Sua saída sul devolve à periferia, fora da área de entrada. Como nas entregas anteriores, HP/MP são estados de cada batalha e começam completos no encontro seguinte; Limit e inventário persistem entre encontros.

## Roteiro em Play

1. Pare Play, aguarde a importação e reinicie `Assets/Scenes/CidadePrototipo.unity`. Caminhe por castelo, florestas, ruínas, lojas e hospedaria sem usar E nas entradas/saídas. Pare após cada chegada: não deve haver retorno automático imediato.
2. Prepare Gelo em um encaixe livre de Lia; conserve Cura. Você pode obter moedas com baú, missão e patrulha, conforme `PARTE_6C.md`.
3. Resolva os três segredos conforme `PARTE_7B.md`. Use E na passagem e caminhe até o ícone do vulcão. Confira a trilha, colisão da lava e saída para a ilha.
4. Siga ao norte até a arena. Use E no dragão e teste Esc; nenhuma batalha deve abrir. Interaja novamente e conclua o diálogo.
5. Observe o título e retrato próprios do boss, fraqueza/resistência na ficha e aviso persistente da preparação. Escolha Defender com cada aliado antes do sopro. Teste cura, Gelo e Limit.
6. Vença. Confira +100 moedas e uma armadura no inventário. Continue explorando, ouça a essência e visite outro mapa; retornar não pode reiniciar o boss nem duplicar recompensas.
7. Abra I → Equipamentos e equipe a armadura em qualquer membro. Compare a defesa de Bram com a dos demais, leia a resistência e monte os dois pares na aba Matérias. Remova/troque: matérias retornam ao inventário e a resistência deixa de estar ativa.
8. Tente vender a armadura na loja de equipamentos, inclusive desequipada: a venda deve estar bloqueada como item de história.
9. Para testar derrota, inicie uma nova sessão, abra novamente a trilha e enfrente o dragão sem atacar. Use uma poção durante a tentativa. Ao perder, volte à hospedaria e confira Limit mantido, poção gasta, saída correta e possibilidade de reencontrar o boss.

## Código e verificação

- `PrototypeVolcanoLayout` e `PrototypeInnLayout`: geometria dos novos cenários.
- `PrototypeTravel` e `PrototypeLocations`: rotas automáticas, exigência dos selos e recuperação na hospedaria.
- `AbilityEffect.Prepare`, `CombatantState.IsCharging` e `EnemyAI`: preparação persistente e ciclo do boss.
- `DragonProgress`: recompensa única e condição de armadura equipada.
- `EquipmentDefinition.Affinities` e `PartyMember.Affinities`: resistências recebidas por equipamento. Proteção prevalece sobre fraqueza; resistências duplicadas não multiplicam redução.
- `DragonInteraction`: conversa cancelável, início da luta e substituição do corpo pela essência após vitória.
- `ObliqueScenery.DragonData`: modelo original; o retrato PNG da batalha é derivado dessa mesma malha.

Passaram 1.697 verificações de combate/inventário/lojas/progresso, 58 de caminhos da periferia, 268 de ilha/interiores/viagens, 1.716 de câmera/malhas e 159 de segredos/escolhas. Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1. Os testes incluem batalhas completas de vitória e derrota, tempo para defender, recompensa sem duplicação, resistência em dano real, retirada da resistência, bônus do tank, venda protegida e retomada com Limit.

As prévias técnicas de vulcão e hospedaria usam as malhas e a projeção do jogo; não são capturas da Unity. Cliques, importação da imagem, física, ativação dos cenários e HUD ainda precisam do roteiro em Play. Arte e interface permanecem provisórias; o progresso continua em memória e reiniciar Play o apaga.
