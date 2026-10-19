# Tela de inventario

Abra com **I**, X/Quadrado ou Start/Options durante a exploracao. O menu suspende movimento, viagens e encontros, usando o bloqueio ja existente no controlador de batalha.

A tela principal segue a referencia fornecida: fundo preto, paineis com degrade azul e bordas prateadas, retratos dos tres aliados, nivel, HP/MP, progresso ate o proximo nivel e Limit. O menu lateral tem somente **Item, Magic, Equip, Status e Config**. As caixas inferiores mostram o tempo real da jornada, ouro e localizacao atual. O tempo acompanha o mesmo contador que e salvo e restaurado nos slots.

- **Item:** retratos e HP/MP a esquerda, lista com icones e quantidades a direita, descricao no topo. Clique no item para levar a luva aos retratos; confirme o rosto do personagem para usar.
- **Magic:** lista de materias a direita e retratos com os slots de arma/armadura a esquerda, sem os campos de nivel ou HP/MP.
- **Equip:** armas, armaduras e acessorios, com comparacao dos atributos atuais/novos e diferencas numericas coloridas.
- **Status:** ficha individual no painel azul, retrato e HP/MP no topo, atributos a esquerda, EXP/Limit a direita, comandos de combate e equipamentos com materias e pares ligados.
- **Config:** volume e tela cheia; ajustes validos nesta sessao.

Na tela principal, use cima/baixo ou W/S e Enter/E/Espaco, mouse ou controle. Os submenus conservam os botoes, listas rolaveis e suporte a controle da interface existente. Em **Item**, setas/W/S ou direcional percorrem a lista e, depois da selecao, os rostos; Enter/E/Espaco ou A/X confirma. **Esc/B/Circulo** retorna dos rostos a lista, da lista ao resumo e do resumo a exploracao. **Voltar** faz o mesmo recuo em Item; **I** fecha diretamente. Reabrir sempre mostra o resumo com Item selecionado. Perder o foco ou abrir o Cheat Engine bloqueia as acoes da interface.

O topo de Item apresenta **Use**, **Arrange**, **Key Items** e o titulo **Item**. Arrange ordena a lista visualmente pelo nome, sem alterar o estoque compartilhado. Key Items mostra a Escama ancestral quando descoberta, sem permitir seu consumo. Use retorna aos consumiveis.

Selecionar ou cancelar nao consome recursos. Confirmar um rosto usa exatamente uma unidade e atualiza HP/MP/condicoes e estoque na mesma tela. Alvos invalidos exibem a mensagem do dominio e mantem a selecao sem consumo. Com copias restantes, a luva permanece nos rostos para usos adicionais; ao esgotar, retorna a lista. Itens zerados aparecem indisponiveis.

Validacao do fluxo de Item: `Tools/Test-ItemMenu.ps1` executa 26 verificacoes de selecao, navegacao, cancelamento, consumo, alvos invalidos, esgotamento, quatro efeitos, organizacao e inventario vazio. A apresentacao usa o mesmo canvas proporcional de 1280x720 da tela principal.

Conferencia adicional em projeto Unity isolado: Enter mudou da lista aos retratos sem consumir, seta para baixo selecionou Lia, e tres pressionamentos separados de Esc retornaram para lista, resumo e exploracao. Capturas em 1280x720 e 960x720 confirmaram o layout, a transferencia da luva da lista ao rosto e o uso da Pocao em Lia: HP de 35/80 para 75/80 e estoque de 6 para 5. A partida aberta no Editor principal foi preservada.

## Tela de Equipamentos

**Equip** usa os mesmos paineis azuis, bordas prateadas, tipografia e luva das telas de Item e Materia. As categorias **Armas**, **Armaduras** e **Acessorios** ficam no topo. O painel esquerdo mostra o retrato, o equipamento atual e os valores **Atual**, **Novo** e **Dif.** de ataque, defesa, magia, HP/MP maximos, velocidade e encaixes. Aumento usa seta verde para cima e numero positivo; reducao usa seta vermelha para baixo e numero negativo. Valores iguais ficam neutros.

A lista direita filtra os equipamentos possuidos pela categoria, incluindo pecas de outros aliados e uma opcao de remocao. Selecionar ou passar o mouse mostra a previa sem alterar a equipe; **Equipar/Remover** ou confirmar aplica a escolha. A previa considera a perda das materias dos equipamentos trocados e preserva os bonus das pecas restantes. A troca atomica entre aliados e a propriedade das instancias continuam sendo validadas pelo dominio. A parte inferior mostra o icone, as afinidades e os encaixes resultantes, reutilizando os sprites existentes.

Nas listas de equipamentos e materias, nomes e localizacao dos objetos equipados aparecem em cinza, incluindo os usados por outros aliados. Objetos livres conservam o nome branco e nao mostram o texto `Inventario` abaixo. A cor nao bloqueia selecao nem troca entre personagens.

Setas/WASD ou direcional: cima/baixo percorre a lista, esquerda/direita troca de categoria. **Q/R**, **Page Up/Page Down**, **LB/RB** ou as setas junto ao nome trocam o personagem. **Enter/E/Espaco** ou **A/X** confirma. **Esc/B/Circulo** retorna ao resumo e **I** fecha diretamente. Nao ha texto de ajuda na tela.

Cada personagem pode equipar um acessorio. A loja de equipamentos vende duas copias de cada modelo por 90 moedas; venda por 45 moedas, bloqueada enquanto equipado. O kit inicial conserva seu inventario anterior. Os acessorios nao possuem encaixes de materia e podem ser transferidos ou removidos sem desmontar as materias da arma/armadura.

| Acessorio | Bonus base |
| --- | --- |
| Anel da Forca | +4 ataque |
| Bracelete Guardiao | +4 defesa |
| Pingente Arcano | +4 magia |

Os bonus participam dos atributos finais e do combate, incluindo os modificadores de materia ja existentes. Status mostra o nome e icone do acessorio; os atributos base da ficha excluem todos os equipamentos. Os tres icones foram gerados com imagegen e os caminhos/prompts estao em `Docs/ArtInputs/Accessories.prompts.json`.

Os saves mantem a identidade e o dono do acessorio e o estoque da loja. `Accessory` e um campo opcional; `EquipmentCatalogVersion` distingue o catalogo atual do antigo. Saves anteriores conservam seu inventario e ouro e recebem somente os tres novos produtos na loja. Estoques antigos ausentes ou invalidos continuam sendo recusados.

Validacao: compilacao Unity, 66 verificacoes de equipamentos, 52 de acessorios, 145 de salvamento, 46 de Materia e 26 de Item passaram. A integracao Unity isolada passou em 24 verificacoes de teclado/controle. Capturas em 1280x720 e 960x720 conferiram armas, armaduras, acessorios, Status e listas longas, sem alterar a equipe durante o desenho. O teste de saves usa arquivo de argumentos para evitar o limite de linha de comando do Windows.

## Tela de Materia

A tela aberta por **Magic** reutiliza a composicao de Item: comandos e descricao no topo, os tres retratos a esquerda e a lista rolavel a direita. Ao lado de cada retrato aparecem o nome do personagem, arma e armadura, com ate seis slots por equipamento. Os slots mostram as materias pelas suas cores e as ligacoes dos pares. Sem equipamento, o nome e `--` e nao aparecem slots. Os campos de nivel e HP/MP nao aparecem nesta tela.

Clique ou confirme uma materia para transferir a luva da lista para os slots. Esquerda/direita percorre os encaixes; cima/baixo troca entre as linhas de arma e armadura dos personagens, conservando a coluna quando possivel. Confirmar o slot equipa a instancia selecionada e retorna a lista. A lista mostra onde cada materia esta equipada; trocar entre dois slots ocupados conserva a troca atomica do dominio, sem duplicar materias.

**Equip** retorna a lista. **Remove**, Delete/Backspace ou Y/Triangulo leva a luva a um slot ocupado; confirmar devolve a materia ao inventario. **Arrange** agrupa visualmente por cor: verde, azul, vermelha e roxa, com ordem alfabetica dentro de cada grupo. **Esc/B/Circulo** cancela a escolha de slot, depois retorna ao resumo e a exploracao; **I** fecha diretamente. Selecionar, navegar ou cancelar conserva a equipe. A descricao do topo mostra a materia selecionada ou o resultado da ultima acao, sem dicas de controle ou texto de ajuda no rodape.

`Tools/Test-MateriaMenu.ps1` passou nas 50 verificacoes de selecao, cancelamento, troca entre personagens, propriedade unica, navegacao entre linhas, sexto slot, organizacao por cor, novas aquisicoes, remocao, destinos removidos e inventario vazio. A tela usa o canvas proporcional de 1280x720.

Validacao adicional: scripts compilados com as bibliotecas da Unity, 26 verificacoes de regressao de Item e 13 verificacoes de integracao com teclado e controle. A previa Unity isolada foi renderizada em 1280x720 e 960x720, incluindo selecao de slot, materia equipada, remocao e personagens sem equipamento. Cada desenho conservou o estado da equipe. A partida aberta no Editor principal foi preservada.

O atlas `Assets/Resources/PixelArt/Inventory/MateriaSockets.png` contem um encaixe individual e um par ligado, com metal prateado e fundo transparente. `MateriaSocketArt` compartilha essa arte entre Materia e Status, conservando o formato circular dos aneis ao ajustar somente a largura da ponte. As esferas usam os quatro sprites `materia-green`, `materia-blue`, `materia-red` e `materia-purple` que ja existiam, sem modificar os arquivos: o recorte da margem transparente melhora a leitura nos slots e na lista. O prompt do atlas esta em `Docs/ArtInputs/MateriaSockets.prompts.json`.

Validacao dos sprites: compilacao Unity e 46 verificacoes do menu passaram. Importacao com alpha, filtro Point e os quatro sprites originais conferida em projeto Unity isolado; capturas de Materia e Status em 1280x720 e 960x720, mais uma previa dos encaixes individual/ligado vazios e preenchidos. A renderizacao conservou o estado da equipe.

## Tela de Status

A ficha segue a composicao da referencia: painel azul com borda prateada, titulo no canto superior direito, retrato, nome, nivel e condicao; HP/MP, EXP total, progresso no nivel, EXP restante e barra de Limit. Os atributos usam os valores reais de `PartyMember`: valores base sem equipamento, velocidade, chance critica, classe, ataque/defesa/magia finais e HP/MP maximos. `Magic def` usa a defesa compartilhada pelos danos fisicos e magicos no combate atual. O jogo tem um nivel de Limit; a carga e mostrada pela barra.

A caixa central mostra os comandos de batalha do personagem, incluindo Magic, Summon e LIMIT quando disponiveis. Comandos longos cabem na celula e a caixa permite rolar quando necessario. Arma e armadura mostram os nomes equipados, ate seis encaixes, cores das materias atuais e ligacoes dos pares. Sem equipamento, o nome e `--` e nao aparecem slots. **Acc:** mostra o icone e nome do acessorio equipado, ou `--` quando vazio. As afinidades aparecem abaixo dos equipamentos.

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
