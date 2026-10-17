# Tela de inventario

Abra com **I**, X/Quadrado ou Start/Options durante a exploracao. O menu suspende movimento, viagens e encontros, usando o bloqueio ja existente no controlador de batalha.

A tela principal segue a referencia fornecida: fundo preto, paineis com degrade azul e bordas prateadas, retratos dos tres aliados, nivel, HP/MP, progresso ate o proximo nivel e Limit. O menu lateral tem somente **Item, Magic, Equip, Status e Config**. As caixas inferiores mostram o tempo real da jornada, ouro e localizacao atual. O tempo acompanha o mesmo contador que e salvo e restaurado nos slots.

- **Item:** retratos e HP/MP a esquerda, lista com icones e quantidades a direita, descricao no topo. Clique no item para levar a luva aos retratos; confirme o rosto do personagem para usar.
- **Magic:** materias, encaixes, pares e previa dos comandos de combate.
- **Equip:** troca e remocao de armas/armaduras e comparacao dos atributos.
- **Status:** ficha individual no painel azul, retrato e HP/MP no topo, atributos a esquerda, EXP/Limit a direita, comandos de combate e equipamentos com materias e pares ligados.
- **Config:** volume e tela cheia; ajustes validos nesta sessao.

Na tela principal, use cima/baixo ou W/S e Enter/E/Espaco, mouse ou controle. Os submenus conservam os botoes, listas rolaveis e suporte a controle da interface existente. Em **Item**, setas/W/S ou direcional percorrem a lista e, depois da selecao, os rostos; Enter/E/Espaco ou A/X confirma. **Esc/B/Circulo** retorna dos rostos a lista, da lista ao resumo e do resumo a exploracao. **Voltar** faz o mesmo recuo em Item; **I** fecha diretamente. Reabrir sempre mostra o resumo com Item selecionado. Perder o foco ou abrir o Cheat Engine bloqueia as acoes da interface.

O topo de Item apresenta **Use**, **Arrange**, **Key Items** e o titulo **Item**. Arrange ordena a lista visualmente pelo nome, sem alterar o estoque compartilhado. Key Items mostra a Escama ancestral quando descoberta, sem permitir seu consumo. Use retorna aos consumiveis.

Selecionar ou cancelar nao consome recursos. Confirmar um rosto usa exatamente uma unidade e atualiza HP/MP/condicoes e estoque na mesma tela. Alvos invalidos exibem a mensagem do dominio e mantem a selecao sem consumo. Com copias restantes, a luva permanece nos rostos para usos adicionais; ao esgotar, retorna a lista. Itens zerados aparecem indisponiveis.

Validacao do fluxo de Item: `Tools/Test-ItemMenu.ps1` executa 26 verificacoes de selecao, navegacao, cancelamento, consumo, alvos invalidos, esgotamento, quatro efeitos, organizacao e inventario vazio. A apresentacao usa o mesmo canvas proporcional de 1280x720 da tela principal.

Conferencia adicional em projeto Unity isolado: Enter mudou da lista aos retratos sem consumir, seta para baixo selecionou Lia, e tres pressionamentos separados de Esc retornaram para lista, resumo e exploracao. Capturas em 1280x720 e 960x720 confirmaram o layout, a transferencia da luva da lista ao rosto e o uso da Pocao em Lia: HP de 35/80 para 75/80 e estoque de 6 para 5. A partida aberta no Editor principal foi preservada.

## Tela de Status

A ficha segue a composicao da referencia: painel azul com borda prateada, titulo no canto superior direito, retrato, nome, nivel e condicao; HP/MP, EXP total, progresso no nivel, EXP restante e barra de Limit. Os atributos usam os valores reais de `PartyMember`: valores base sem equipamento, velocidade, chance critica, classe, ataque/defesa/magia finais e HP/MP maximos. `Magic def` usa a defesa compartilhada pelos danos fisicos e magicos no combate atual. O jogo tem um nivel de Limit; a carga e mostrada pela barra.

A caixa central mostra os comandos de batalha do personagem, incluindo Magic, Summon e LIMIT quando disponiveis. Comandos longos cabem na celula e a caixa permite rolar quando necessario. Arma e armadura mostram os nomes equipados, ate seis encaixes, cores das materias atuais e ligacoes dos pares. Sem equipamento, o nome e `--` e a barra fica vazia. `Acc: --` corresponde a ausencia de um sistema de acessorios. As afinidades aparecem abaixo dos equipamentos.

Use **A/D**, setas, direcional/analogico ou as setas clicaveis no rodape para trocar entre os tres personagens. **Esc/B/Circulo** retorna ao resumo da equipe; **I** fecha diretamente. O titulo **Status** tambem retorna ao resumo pelo mouse. A ficha e somente leitura e conserva HP, MP, EXP, Limit, equipamentos e materias ao navegar.

Validacao: compilacao com as bibliotecas da Unity, 26 verificacoes de regressao da tela de Item e sete verificacoes de integracao do Status (abertura, troca de personagem, volta circular, bloqueio sem foco e Esc). Previa renderizada em 1280x720 e 960x720 com os tres personagens, seis encaixes, nivel 99, KO e equipamentos removidos; o estado da equipe foi comparado antes e depois de cada desenho da ficha, sem alteracoes.

## Validacao inicial da tela principal

- Compilacao de todos os scripts com as bibliotecas da Unity 6000.6.5f1: passou.
- `Tools/Test-MateriaRevision.ps1`: passou.
- `EquipmentItemChecks.RunItemChecks()`: 49 verificacoes passaram.
- Tela principal conferida no Game view com uma jornada carregada: tres personagens, localizacao e ouro reais, painel lateral e cursor de luva.
- A automacao visual foi interrompida pelo usuario com Esc; a ultima revisao de cores sem foco e preenchimento das barras foi compilada, sem nova captura automatizada.
- A suite geral `Tools/Test-Combat.ps1` falhou em `AbilityChecks.CheckEncounterStrategies` com habilidade indisponivel. `EquipmentItemChecks.Run()` tambem falhou ao verificar a restauracao de um comando por materia. Essas rotinas compilam apenas dominio/fabricas/testes, sem nenhum arquivo modificado nesta entrega; as falhas de habilidades permanecem fora do escopo do inventario.

Conferencia manual adicional: abrir cada submenu, usar um item em aliado ferido, trocar equipamento e materias, conferir Status, ajustar Config e voltar; repetir com controle e em janelas 16:9 e 4:3. Evitar salvar a jornada apenas para testar a interface.
