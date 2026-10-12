# Validação por entrega

## Parte 0 — estrutura inicial

- Base criada a partir do template 2D fornecido com Unity 6000.6.5f1.
- Editor configurado para 2D, serialização em texto e arquivos meta visíveis.
- Arquivos gerados e cache excluídos pelo `.gitignore`.
- Abertura automática: pendente. A tentativa inicial encerrou com erro nativo no serviço de rede do Editor; uma segunda tentativa ficou aguardando a inicialização da licença no ambiente de execução automatizado.
- Ainda não foi confirmado o funcionamento da cena em Play nem gerado um executável.
- Usuário confirmou licença Personal ativa no Hub em 8 de outubro de 2026; falta confirmar a abertura fora do ambiente automatizado.
- Correção de preparação: restaurada a formatação original dos arquivos do template. A remoção de espaços finais havia causado erro de leitura do TagManager no parser da Unity.
- O arquivo `Temp/UnityLockfile` está em uso. A tentativa de remoção foi recusada pelo Windows; encerrar a instância que mantém o bloqueio antes de reabrir.

### Verificação manual pendente

1. Adicionar a raiz do repositório no Unity Hub.
2. Abrir usando 6000.6.5f1 e aguardar a importação.
3. Confirmar ausência de erros vermelhos na janela Console.
4. Abrir `Assets/Scenes/SampleScene.unity` e entrar/sair de Play.

A cena inicial contém o conteúdo padrão do template; movimento e personagens serão adicionados na parte 1.

### Confirmação posterior da parte 0

O usuário apresentou a cena em Play, com os contadores do Console em zero. A abertura manual foi confirmada após as correções; os registros anteriores descrevem as tentativas de preparação.

## Parte 1 — exploração

- Quatro scripts C# compilados sem erros com o compilador Roslyn e as bibliotecas do Editor 6000.6.5f1, incluindo a biblioteca do Input System instalada no projeto.
- Referência do componente PrototypeCity na cena e GUID da cena nos Build Settings conferidos.
- Validação interativa de movimento, colisões e enquadramento ainda pendente no Editor aberto pelo usuário. Compilação isolada não substitui teste em Play.
- Roteiro de verificação: `Docs/PARTE_1.md`.
- Sem build executável nesta etapa.

## Parte 2 — interações e interior

- Scripts do jogo compilados sem erros com Roslyn e bibliotecas da Unity 6000.6.5f1, incluindo Input System.
- Executadas 15 verificações automatizadas do progresso: recompensa única, baús independentes, validação de entradas, overflow e isolamento entre sessões. Todas passaram.
- Ouro e baús ficam em ExplorationProgress, mantido em memória enquanto os ambientes são alternados.
- Verificação visual e interativa de movimento, diálogos, aparência do baú e transições ainda pendente no Editor do usuário.
- Roteiro: `Docs/PARTE_2.md`. Teste reproduzível: `Tools/Test-ExplorationProgress.ps1`.
- Sem build executável nesta etapa.

### Confirmação posterior da parte 2

O usuário confirmou que as interações funcionaram em Play. Não houve validação individual registrada de todos os passos do roteiro.

## Parte 3 — combate básico por turnos

- Scripts do jogo compilados sem erros com Roslyn e as bibliotecas instaladas da Unity 6000.6.5f1 e Input System.
- 204 verificações de combate passaram, incluindo ações inválidas sem gasto de recursos, HP/MP, cura, defesa, ordem de turnos, combatentes derrotados, estados independentes e partidas completas que chegam a vitória e derrota.
- As 15 verificações de progresso de exploração continuam passando.
- Interface de batalha, passagem da exploração para combate, controle de foco e retorno à cidade aguardam validação visual e interativa no Editor do usuário. Compilação isolada e testes das regras não confirmam esses comportamentos visuais.
- Roteiro: `Docs/PARTE_3.md`; verificação automatizada: `Tools/Test-Combat.ps1`.
- Não foi produzido executável.

## Parte 4 — habilidades e condições

- Scripts do jogo compilados sem erros com Roslyn e as bibliotecas da Unity 6000.6.5f1, incluindo Input System.
- 542 verificações de combate passaram. Incluem regressão do encontro básico, ataque em área com custo único, veneno e purificação, ressuscitar e ordem de turnos, duração de provocação, interceptação e expiração de proteção, morte dos responsáveis pelos efeitos, fallback da IA sem MP e partidas completas com vitória e derrota no encontro atual.
- A contagem inclui as verificações das ações realizadas durante as partidas completas; não representa 542 cenários separados.
- As 15 verificações de progresso de exploração continuam passando.
- A interface foi adaptada para cinco ações, dois inimigos e condições, mas sua aparência e interação ainda aguardam teste em Play no Editor do usuário.
- Roteiro: `Docs/PARTE_4.md`; testes reproduzíveis: `Tools/Test-Combat.ps1` e `Tools/Test-ExplorationProgress.ps1`.
- Sem build executável nesta etapa. Mapa e recursos visuais permanecem provisórios.

## Parte 5A — materias, combos e Limit Break

- Scripts do jogo compilados sem erros com Roslyn e as bibliotecas da Unity 6000.6.5f1 e Input System.
- 1.153 verificações de combate passaram, incluindo as regras anteriores e os cenários de matéria, equipamento, afinidade, dano mágico, recuperação em área e Limit.
- Verificadas transferências entre membros, troca entre encaixes, remoção de comandos, rejeição de itens estrangeiros e ausência de duplicação de instâncias.
- Verificados os combos verde/azul, verde/roxa, vermelha/azul e vermelha/roxa, custo único em área, pares isolados e definições de matéria preservadas.
- Verificados bônus sobre valores base, cura em área, dano real após defesa, carga individual de Limit, consumo sem MP e preservação ao concluir uma derrota e iniciar outra batalha com a mesma equipe.
- A contagem inclui verificações de cada ação em partidas completas, não 1.153 cenários separados.
- As 15 verificações de progresso de exploração continuam passando.
- Menu de matérias, rolagem das ações, bloqueio de movimento, cores e barras ainda aguardam validação visual e interativa em Play pelo usuário.
- Roteiro: `Docs/PARTE_5A.md`; testes reproduzíveis: `Tools/Test-Combat.ps1` e `Tools/Test-ExplorationProgress.ps1`.
- Persistência em memória durante a mesma sessão. Não há salvamento em disco, troca de equipamento, consumíveis, animação de summon ou executável nesta entrega.

## Parte 5B — equipamentos e consumiveis

- Scripts do jogo compilados sem erros com Roslyn e as bibliotecas da Unity 6000.6.5f1 e Input System.
- 1.247 verificações de combate/inventário passaram, incluindo todas as regras anteriores, equipamento e consumíveis. A contagem inclui verificações de ações durante partidas completas.
- Confirmados troca/remoção e retorno de todas as matérias ao inventário, equipamentos vazios ao reequipar, troca entre aliados, bloqueio de itens estrangeiros, identidades sem duplicação e jogo sem arma/armadura.
- Confirmados bônus equipados, magia de cajado/manto e combos no último par de uma armadura com seis encaixes.
- Confirmados uso e limites de poção/éter, remoção de veneno, ressurreição sem turno extra, estoque esgotado, tentativas inválidas sem consumo e aplicação de veneno ao fim do uso de item.
- Confirmados persistência de estoque e Limit, consumo sem MP, isolamento entre sessões, rejeição de definições falsas e proteção contra overflow das quantidades.
- As 15 verificações de progresso de exploração continuam passando.
- Abas, comparação de atributos, rolagem dos encaixes e uso de itens pela interface aguardam confirmação visual/interativa em Play.
- Roteiro: `Docs/PARTE_5B.md`; testes: `Tools/Test-Combat.ps1` e `Tools/Test-ExplorationProgress.ps1`.
- Sem compras, salvamento em disco, arte final ou executável nesta etapa.

## Parte 6A — elementos e afinidades

- Scripts do jogo compilados sem erros com Roslyn e bibliotecas da Unity 6000.6.5f1, incluindo Input System.
- 1.362 verificações de combate passaram, incluindo regressões anteriores, dados elementais imutáveis, validação de perfis, fraquezas/resistências dos três elementos, arredondamento e limites numéricos.
- Verificados dano por alvo em área com custo único, Infusão e afinidade de classe, interceptação com perfil do protetor, Defender, magia da IA e carga real de Limit.
- Verificados comandos/estados sem elemento, perfis preservados no snapshot, definições de matéria intactas e Fogo + All no encontro jogável.
- As 15 verificações de progresso de exploração continuam passando.
- Prévia de elementos, fichas de afinidade e relatório visual aguardam confirmação em Play. Roteiro: `Docs/PARTE_6A.md`.
- Inventário de teste mantido até a integração das lojas na parte 6B. Planejamento: `Docs/PLANO_LOJAS.md`.

## Parte 6B — lojas, transações e kit inicial

- 1.415 verificações de combate/inventário/lojas passaram e as 15 verificações de exploração continuam passando.
- Verificados kit econômico e saldo de 50, compra múltipla, cópias distintas e vazias, combos de matérias compradas, afinidade de equipamento e sessões independentes.
- Verificados venda por metade com arredondamento por unidade, seleção antiga revalidada, bloqueio de objetos equipados, categoria errada, itens de história e estoque esgotado.
- Verificados falhas sem mutação, produtos estrangeiros, lotes com IDs duplicados, limites de pilha e overflow de ouro/preço.
- Scripts compilaram com bibliotecas da Unity 6000.6.5f1. Compilação reproduzível: `Tools/Test-UnityScripts.ps1`.
- Três interiores, portas de retorno, UI de quantidade, bloqueio de movimento e interação aguardam verificação em Play: `Docs/PARTE_6B.md`.

## Parte 6C — recompensas, missão e reposição

- 1.488 verificações de combate/inventário/lojas/progresso passaram; as 15 verificações de exploração continuam passando.
- Confirmados pagamento de 35 por vitória, ausência de pagamento por derrota/resultado em andamento, repetição sem duplicação e uso imediato do ouro nas lojas.
- Confirmados passos da entrega, vendedor e ordem corretos, pacote fora do catálogo comercial, pagamento de 30 uma vez e marcos independentes entre sessões.
- Confirmados reposição ao estoque inicial somente de consumíveis, manutenção do estoque de equipamentos/matérias, inventário do jogador preservado e rejeição de repetição do marco.
- Confirmadas recompensas pendentes sem corrupção em caso de limite de ouro e proteção contra seleções repetidas da mesma missão.
- Confirmado percurso completo com kit econômico: vitória sem compras/consumíveis extras, missão, baú, saldo de 140, compra de Fogo/All e comando válido na batalha seguinte.
- Scripts compilaram com bibliotecas da Unity 6000.6.5f1. UI da missão, recompensa visual e percurso entre lojas ainda aguardam teste em Play: `Docs/PARTE_6C.md`.
- Sem salvamento em disco, arte final ou executável nesta fase.

## Ajuste da parte 6C — ouro por inimigo

- 1.518 verificações de combate/inventário/lojas/progresso passaram; as 15 verificações de exploração continuam passando.
- Confirmados valores próprios por tipo de inimigo, soma de vários derrotados e contagem separada de cópias da mesma definição. A patrulha atual configura 25 no Guarda do tirano e 10 no Soldado.
- Confirmados composição da recompensa no resultado, exclusão de aliados vivos/derrotados, valor zero e rejeição de valores negativos.
- Confirmados ausência de pagamento durante o encontro ou após derrota com eliminação parcial, pagamento único e uso imediato nas lojas.
- Confirmados proteção contra overflow da soma/saldo, ausência de pagamento parcial, aceitação do limite exato e recebimento pendente quando o saldo permitir.
- Percurso econômico com missão, baú, patrulha e Fogo/All continua passando com saldo total de 140.
- Scripts compilaram com bibliotecas da Unity 6000.6.5f1. A composição visual da recompensa ainda aguarda o roteiro em Play de `Docs/PARTE_6C.md`.

## Parte 7A — cidade ampliada

- 57 verificações de caminhos passaram sobre os dados usados para montar o mapa de 72 × 48 unidades.
- Confirmados acesso aos seis bairros, novas extremidades, capela, pedestal, moradores, baú e patrulha; portas e posições de retorno das três lojas estão em áreas acessíveis.
- Confirmadas travessias das três pontes, água/muralhas bloqueadas e ausência de células caminháveis isoladas na amostragem de 0,5 unidade. Colisores foram ampliados pelo tamanho do herói mais margem; a área acessível estimada foi 2.511 unidades quadradas.
- Visão geral da geometria conferida por imagem gerada a partir dos mesmos dados, sem executar a Unity.
- As 1.518 verificações de combate/inventário/lojas/progresso e 15 de exploração continuam passando; scripts compilaram com bibliotecas da Unity 6000.6.5f1.
- Dados dos limites são compartilhados pela inicialização e pelo retorno das lojas. A câmera, física em movimento, diálogo, UI e transições ainda aguardam confirmação em Play: `Docs/PARTE_7A.md`.
- Testes reproduzíveis: `Tools/Test-CityLayout.ps1`, `Tools/Test-UnityScripts.ps1`, `Tools/Test-Combat.ps1` e `Tools/Test-ExplorationProgress.ps1`.

## Parte 7A — ilha global e cenários locais

- 88 verificações da ilha, interiores e viagens passaram, junto das 57 verificações geométricas da periferia.
- Confirmados início na ilha, classificação correta de lojas, oito mapas e funcionamento das 14 rotas direcionais. Atalhos não declarados, destinos inexistentes, viagens ao mesmo mapa e vulcão bloqueado não alteram o mapa atual.
- Confirmados pontos de entrada globais acessíveis, costa/baía bloqueadas, passagem oculta alcançável para exame e vulcão inacessível com as rochas atuais.
- Confirmados caminhos ao trono/saída do castelo, moradores/saídas das duas florestas e portas de entrada/saída da periferia.
- Confirmados chegadas dentro dos limites e livres de obstáculos, retorno de cada floresta junto ao seu ícone, retorno de lojas às suas portas e ciclos repetidos ilha/periferia/loja/castelo.
- Confirmada altura dos ícones de castelo/floresta um pouco acima do herói e conferida uma visão geral da ilha gerada pelos dados da geometria.
- As 1.518 verificações de combate/inventário/lojas/progresso e 15 de exploração continuam passando; scripts compilaram com bibliotecas da Unity 6000.6.5f1.
- Ativação de ambientes, física em movimento, câmera, UI e preservação real de estado entre grupos ainda precisam de Play: `Docs/PARTE_7A.md`. O tirano tem diálogo; suas batalhas e os finais permanecem na parte 8.

## Parte 7A — apresentação 2.5D

- Scripts compilaram com as bibliotecas da Unity 6000.6.5f1; 1.404 verificações de câmera, projeção e malhas passaram em `Tools/Test-Oblique.ps1`.
- Confirmados índices válidos, vértices/cores finitos, triângulos com área e preenchimento da superfície côncava da ilha sem buracos ou sobreposição na soma das áreas.
- Confirmados altura visual em -Z, redução da profundidade na tela, controles alinhados à câmera girada, dimensões dos ícones e limites da câmera considerando inclinação/rotação.
- Confirmado enquadramento fixo dos ambientes menores para proporções 16:9, 4:3 e 3:4; a periferia mantém acompanhamento do herói.
- Prévias da ilha, praça e sala do trono conferidas com rasterização externa das mesmas malhas/projeção. Não são capturas do Editor.
- 57 verificações de caminhos da periferia, 88 de ilha/interiores/viagens, 1.518 de combate/inventário/lojas/progresso e 15 de exploração passaram.
- Importação/compilação do shader na Unity, renderização pela GPU, passos/tampa animados, HUD, física e interações ao vivo ainda precisam do roteiro de Play em `Docs/VISUAL_25D.md`.

## Parte 7B — três segredos

- 159 verificações de segredos e escolhas passaram em `Tools/Test-Secrets.ps1`.
- Confirmados poema com três versos, escolhas exatamente iguais aos versos, explicação completa antes de registrar a leitura, contador oculto até a primeira solução e consulta sem avanço de segredo.
- Confirmados altar correto/errado, cancelamento sem solução, Lua/Estrela/Sol, reinício/apagamento por erro, símbolos inválidos sem mutação e conclusões sem duplicação.
- Confirmadas as seis ordens possíveis de descoberta; todos os objetivos são necessários, e a passagem continua fechada até interação explícita. Abrir é uma operação única e não consome a escama.
- Confirmados independência entre sessões, escolhas inválidas/repetidas, avanço que não escolhe automaticamente e callbacks que abrem outro diálogo sem que ele seja apagado.
- 58 verificações de caminhos da periferia e 97 de ilha/interiores/viagens passaram, incluindo acesso ao altar, monólitos, inscrição, pedra e árvore oca. O vulcão é inacessível com o colisor dos selos e acessível após removê-lo, com retorno ao sul possível.
- 1.444 verificações de câmera/malhas, 1.518 de combate/inventário/lojas/progresso e 15 de exploração passaram. Scripts compilaram com bibliotecas da Unity 6000.6.5f1. O acompanhamento nas extremidades da periferia foi ajustado e verificado para manter pés/cabeça e chama do altar visíveis.
- Interações, seleção por clique/teclas, consulta com P, luzes, movimento das rochas, colisão ao vivo e UI aguardam Play em `Docs/PARTE_7B.md`. Interior do vulcão e dragão ficam na parte 7C.

## Ajuste da parte 7B — altar nas ruínas do mapa global

- Altar removido da planta/interações da cidade e instalado em um cenário próprio, acessível por um novo ponto de interesse ao sudoeste da ilha.
- 58 verificações de caminhos da periferia e 115 de ilha/interiores/viagens passaram, incluindo entrada global das ruínas, chegada livre, caminho ao altar, saída/retorno específico e rejeição de viagens diretas entre ruínas e cidade/lojas.
- Poema aprendido e altar resolvido continuam no mesmo estado da sessão ao sair e retornar às ruínas; a solução não duplica progresso.
- 1.552 verificações de câmera/malhas passaram, incluindo ruínas locais, ícone com volume e enquadramento fixo da chama do altar. Scripts compilaram com bibliotecas da Unity 6000.6.5f1.
- As verificações de segredos/escolhas, combate/inventário/lojas/progresso e exploração continuam passando. Roteiro de Play atualizado em `Docs/PARTE_7B.md`; cliques, física e transições reais ainda precisam da conferência no Editor.

## Zoom do mapa mundial

- Tamanho ortográfico da ilha ajustado para 6,85, aproximadamente metade do enquadramento anterior em 16:9. O ajuste automático que mostrava toda a ilha foi desligado nesse mapa; a câmera agora usa acompanhamento do herói.
- Enquadramentos dos cenários locais, inclinação e orientação dos controles conservados.
- Scripts compilaram e 1.538 verificações de câmera/malhas passaram. A contagem mudou porque a ilha passou de enquadramento fixo para acompanhamento. Conferência visual em Play pendente.

## Acompanhamento centralizado

- Corrigido o foco dos mapas com acompanhamento: os limites do retângulo prendiam a câmera ao centro do mapa em parte dos movimentos, deslocando o herói lateralmente na vista oblíqua.
- Foco agora acompanha diretamente o personagem na ilha/periferia, com ponto de mira na altura do centro do corpo e suavização de 0,08 segundo. Zoom aproximado, ângulo e enquadramentos locais fixos preservados.
- Scripts compilaram e 1.586 verificações de câmera/malhas passaram. A regressão confere centralização horizontal e vertical no início da ilha, pontos de interesse e bordas, em proporções 16:9, 4:3 e 3:4.
- Movimento com suavização e aparência real ainda aguardam Play. Verificar caminhada em todas as direções, parada e entrada/saída de cenários.

## Passagens automáticas entre mapas

- As 16 rotas têm uma área de passagem de 1,4 × 1 unidade, definida junto à entrada/saída. O ponto do personagem no chão precisa entrar nessa área; não há confirmação com E.
- A mesma definição posiciona as marcas no cenário e determina a viagem. Menus, diálogos, batalhas e perda de foco da janela suspendem as transições. Uma atualização realiza no máximo uma viagem.
- Retornos das lojas foram afastados das portas. Todas as chegadas e posições iniciais ficam fora das passagens, evitando ciclos de entrada/saída.
- Compilação dos scripts passou. Passaram 58 verificações da periferia e 204 de ilha/interiores/viagens, incluindo alcance das áreas, aproximação sem entrar, suspensão por bloqueio e ausência de retorno imediato.
- Em Play: caminhar até cada entrada e saída sem E, parar após chegar, repetir as viagens e conferir preservação de ouro, equipamentos, matérias, segredos e Limit. E continua acionando moradores, vendedores, objetos e abertura dos selos.

## Parte 7C — dragão e hospedaria

- Vulcão com caminho curto, lava sólida e arena; novo interior de hospedaria. Onze mapas, 20 rotas direcionais e recuperação especial após derrota no boss.
- POIs globais abrangem 2,8 × 2,2 unidades ao redor do ícone; testes conferem entrada pelos quatro lados. Portas locais mantêm 1,4 × 1. Mar e selos continuam bloqueando atalhos; o modelo de navegação também exige passagem aberta para entrar no vulcão.
- Ciclo de garra/preparação/sopro com aviso persistente e oportunidade de todos os aliados defenderem; Gelo causa 150% e Fogo 50% no dragão. Boss inicial: 520 HP, recompensa própria de 100 moedas.
- Vitória entrega armadura uma vez, liberta essência e bloqueia repetição. Armadura: defesa 16, resistência a Fogo, dois pares de matérias, uso livre por classe e venda protegida. Tank aplica +25% sobre defesa base; remover o equipamento retira resistência e devolve matérias.
- Derrota volta à hospedaria. Próxima batalha inicia com HP/MP completos, conservando Limit, segredos e consumíveis já gastos. O retorno à hospedaria não é uma rota normal nem um atalho para outros cenários.
- Passaram 1.697 verificações de combate, 58 da periferia, 268 de viagens/interiores, 1.716 de câmera/malhas e 159 de segredos. Compilação passou; prévias técnicas da geometria e retrato foram inspecionadas fora do Editor.
- Integração visual, cliques, importação da textura, diálogos, colisões e retorno efetivo ainda aguardam Play conforme `PARTE_7C.md`. Tirano, espírito, finais e salvamento em disco seguem pendentes.

## Parte 8 — trono e três finais

- Confronto único com tirano e, se a armadura estiver equipada, espírito visível desde o início. Golpes físicos sem elemento; aos 20% de HP, duas preparações completas do tirano e lançamento no turno seguinte.
- Absorção integral por portador vivo, incluindo recuperação após reviver. Sem absorção, meteoro aniquila todos e determina destruição do castelo. Lançamento antecipado é recusado; a contagem não reinicia e não há repetição após absorção.
- Espírito usa drenagem de HP real, Correntes por uma ação e Caos Magic em área. Condições e feedback de turno foram integrados ao HUD; combate comum e dragão mantiveram as verificações anteriores.
- Três desfechos: rei libertado, castelo destruído e Aren possuído. Morte simultânea de rei/espírito determina possessão; meteoro destrutivo determina destruição. Nenhuma ação continua após um desfecho, e não há pagamento de vitória comum.
- Cenas finais próprias com três páginas, exploração suspensa e reinício da cena. Derrota comum no castelo recupera na hospedaria com Limit e progresso conservados.
- Passaram 1.983 verificações de combate, 58 da periferia, 269 de viagens/interiores e 1.750 de câmera/malhas; scripts compilaram com bibliotecas da Unity 6000.6.5f1. Prévia da geometria detectou e permitiu corrigir o alinhamento das pernas nos modelos estáticos novos.
- Retratos e geometria das cenas foram inspecionados em prévias técnicas. Importação, GPU, UI, início/cancelamento da conversa, troca de câmera e reinício efetivo ainda precisam de Play conforme `PARTE_8.md`.
- Salvamento em disco, arte/áudio final, efeitos, acabamento e executável seguem pendentes.

## Parte 9 — jornadas salvas

- Menu inicial com três slots, Novo jogo e Continuar. Substituição de slot ocupado exige escolha explícita no menu; exploração, entradas automáticas e atalhos ficam suspensos até selecionar uma jornada.
- Autosave por eventos de vitórias/desfechos, segredos, inventário, baú e entrega. Alterações de uma mesma transação são agrupadas e gravadas após sua conclusão. Movimento/viagem e combate em andamento não disparam salvamento; derrota comum não dispara autosave.
- Salvamento manual por escolha no diálogo da hospedeira ou F5. Restauração recupera mapa/coordenadas, ouro, objetos, sockets, Limit, estoques, missão, segredos, passagem e dragão. Final salvo reapresenta o desfecho, com retorno ao menu.
- Catálogo de IDs estáveis, formato JSON versionado, validação antes da instalação e link.xml para os tipos serializados. Arquivos em Application.persistentDataPath/Saves; substituição atômica e backup da revisão válida anterior. Corrupção recupera backup com aviso; versão incompatível bloqueia Continuar.
- Passaram 107 verificações de save, 1.983 de combate, 159 de segredos, 58 de caminhos da periferia e 269 de viagens/interiores. Compilação passou com bibliotecas da Unity 6000.6.5f1. Os testes de arquivos usaram ../../work/SaveChecks porque o processo de teste teve escrita negada na pasta temporária do ambiente automático.
- Em Play, conferir cliques/teclado do menu, objetos restaurados, caminho real em LocalLow e os três finais conforme PARTE_9.md. Saves reais da aplicação e GPU/UI não foram executados fora do Editor. Arte, áudio, acabamento, balanceamento e executável continuam pendentes.

## Parte 10A — sprites e materiais em pixel art

- Cinco atlas originais gerados pela ferramenta integrada image_gen: equipe, moradores e materiais, com referência artística ao FF6 original. Câmera inclinada, cenários em 3D e planta de colisões conservados. PNGs/importadores e prompts foram versionados.
- Recorte por alpha e âncora nos pés; Aren usa quatro direções/caminhada. NPCs têm imagens distintas e equipe/guarda/rei são desenhados com sprites no HUD da batalha. A direção da imagem considera o yaw da câmera e teleportes não acumulam passos.
- Materiais com UVs por face e separação de sombras transparentes; chão, paredes, telhados, folhagem, mar e lava usam atlas sem mipmap/compressão. Contraste da grama reduzido após inspeção da prévia.
- Validação dos arquivos detectou corpos cruzando células no Bram e em parte dos moradores. A ferramenta de imagem reparou o espaçamento; passaram 186 verificações dos PNGs/importadores e 63 de sprites/materiais.
- Scripts C# compilaram; passaram 2358 verificações de malhas/câmera, 107 de save, 58 da periferia e 269 de viagens. Prévias técnicas da ilha, cidade, hospedaria e salão foram inspecionadas. Não são capturas do Editor.
- Compilação dos shaders pela Unity, importação efetiva, oclusão/sombras, movimento e interface aguardam Play segundo PARTE_10A.md. Dragão, espírito, invocações, efeitos, cenas finais e detalhes decorativos ainda têm arte provisória. Aparência do equipamento não muda o sprite nesta etapa.

## Correção do cenário magenta — 12/10/2026

- O log do Editor confirmou três erros de sintaxe em `ObliqueWorld.shader` no DirectX 12. A variável local `texture` usava um nome reservado do HLSL; foi renomeada para `albedo`, incluindo suas duas referências.
- Verificação externa com `d3dcompiler_47.dll` e os headers URP instalados: a versão anterior reproduziu os três erros; a versão corrigida compilou nos estágios de vértices e fragmentos. Os shaders de personagens e sombras também compilaram, totalizando seis compilações bem-sucedidas.
- Essa verificação usa perfis `vs_5_0`/`ps_5_0`, compatibilidade de tipos half e uma adaptação em memória da macro vazia PopMarker para o pré-processador legado. Nenhum pacote instalado foi alterado. O teste não substitui a recompilação DirectX 12 e a conferência visual no Editor.
- Em Play: parar a execução, retornar à Unity e aguardar a reimportação do shader; limpar o Console e iniciar Play novamente. Conferir cenário com cores e ausência de novos erros de shader.

## Aren — movimento em oito direções

- Atlas complementar com doze sprites diagonais, gerado pela ferramenta integrada image_gen e inspecionado visualmente. Cabelo loiro pontiagudo, trajes e alpha preservados; direções cardinais e HUD mantêm o atlas original.
- Orientação considera o yaw da câmera, com oito setores de 45 graus. Caminhada usa distância percorrida e alterna os pés; parar conserva a direção em repouso. Atlas de quatro direções continuam usando a seleção anterior.
- O PNG novo tem 1447 × 1087 pixels. O recorte agora distribui pixels restantes entre as células e examina toda a imagem, conservando a rejeição de sprites vazios ou cortados. Nenhum PNG foi redimensionado.
- Scripts compilaram. Passaram 231 verificações de imagens/importadores, 161 de sprites/materiais e 2358 de perspectiva/câmera/malhas. Os testes incluem oito direções com câmera rodada, transições entre setores, ciclo dos passos, repouso diagonal e recorte de dimensões não divisíveis.
- Em Play: conferir W+A, W+D, S+A e S+D na ilha e nos interiores, parada, mudança para um eixo e passagem entre mapas. A animação no Editor e sua aparência durante o movimento permanecem pendentes de conferência.

## Periferia compacta e ponte de pedra

- Substituída a planta ampla por bairro de 26 × 44, com 22 construções próximas e viela sinuosa. Removidos lago, rio e três pontes de madeira. Única ponte de pedra ao norte, sobre fosso seco, com entrada automática para o castelo ao fim da travessia.
- Dois atlas originais gerados com image_gen e importados sem edição posterior. Casas/toldos/metais, janelas e letreiros nas fachadas, tubulações, caldeira da loja de itens, lampiões e vapor. Materiais próprios da periferia; brilho padrão do restante do cenário conservado.
- Portas, retornos, moradores, patrulha e baú agora usam a planta compacta. Textos de orientação atualizados. Carregar jornadas do mapa amplo conserva progresso; posições agora bloqueadas ou fora dos novos limites voltam à rua central.
- Passaram 243 verificações de PNGs/importadores, 161 de sprites/materiais, 2107 de malhas/câmera, 73 de caminhos, 269 de viagens/interiores e 110 de salvamento. A amostragem de 0,125 unidade confirma toda área caminhável conectada, acesso a moradores/lojas e ponte, além de retornos sem reentrada imediata.
- Scripts compilaram. ObliqueWorld e PixelGlow passaram nos dois estágios da compilação externa HLSL, sob as condições do registro da correção do shader magenta. Importação detectada no log do Editor não confirma a renderização em Play.
- Prévias técnicas da entrada, mercado, capela e ponte foram inspecionadas. Correções visuais: placas na borda dos toldos e piso recortado para não ocultar o fosso. Conferência de GPU, profundidade, movimento e interações no Editor pendente conforme PERIFERIA_PIXEL_ART.md.

## Fortaleza de pedra mais imponente

- Castelo redesenhado com alvenaria cinza própria, muralha de 9,5 × 4,5, torres de 3,4 × 3,6 com altura de 8,02, torre de menagem, ameias, seteiras e estandartes. Grande portão em arco de pedra com madeira/ferro; tochas laterais.
- Atlas original de 1447 × 1087 gerado com image_gen, importado sem redimensionamento/edição. Fortaleza, ponte/pátio e ícone global usam material próprio. Ícone com ameias mantém altura de 1,95 e mesma planta física no mapa mundial.
- Compilação dos scripts passou. Passaram 249 verificações de PNGs/importadores, 161 de sprites/materiais, 2116 de malhas/câmera, 73 de caminhos, 269 de viagens/interiores e 110 de saves. Novas verificações conferem altura dominante, materiais de pedra, limites das cornijas e escala do ícone.
- Prévias geral e junto ao portão foram inspecionadas fora da Unity. A geral usa enquadramento de apresentação; a próxima usa tamanho de câmera do jogo. A câmera do jogo mantém seu zoom. Entrada/retorno do castelo continuam automáticos; aparência real/GPU ainda precisa de Play.

## Iluminação da periferia e profundidade dos sprites

- Retirados postes, pequenos lampiões nas fachadas e círculos aditivos de luz nas ruas/portão. Janelas e letreiros usam o brilho já pintado no atlas existente. Não foi executado bake de lightmaps da Unity; a iluminação está incorporada à arte. O material aditivo permanece para o vapor das chaminés.
- O sprite copiava a inclinação da câmera, deslocando a cabeça para dentro de móveis. Agora fica vertical em −Z e acompanha apenas a direção horizontal da câmera. A escala conserva projeção, proporções e âncora dos pés. O teste de profundidade continua ativo para objetos à frente do personagem.
- Scripts compilaram com bibliotecas da Unity 6000.6.5f1. Passaram 249 verificações de PNGs/importadores, 161 de sprites/materiais e 2167 de perspectiva/câmera/malhas. As 51 novas verificações comparam a projeção anterior/atual e usam interseção de raios com a malha real do balcão: reproduzem o recorte antigo e conferem cabeça visível na frente, corpo oculto atrás e oclusão por parede alta.
- Prévias técnicas do balcão antes/depois e da periferia sem postes foram renderizadas com as malhas/texturas existentes e inspecionadas fora da Unity. O erro relatado foi reproduzido antes da correção. Nenhum PNG do jogo foi alterado. Conferir em Play após parar, aguardar compilação e iniciar novamente; verificar aproximação dos balcões/estantes e passagem atrás dos objetos. GPU e interação no Editor permanecem pendentes.

## Fluidez da caminhada, principalmente nas diagonais

- Análise dos PNGs confirmou que ancorar pela bota inferior deslocava a cabeça horizontalmente entre poses: até aproximadamente 47 pixels de origem nas diagonais. Aren usa agora referência horizontal da parte superior do corpo, com base vertical nos pés; PNGs e recortes permanecem intactos. NPCs estáticos mantêm a âncora anterior. A ferramenta de inspeção exporta o mesmo critério utilizado no jogo.
- Ritmo ajustado de uma pose por 0,22 para 0,4 unidade percorrida, reduzindo de aproximadamente 18 para 10 trocas/s na velocidade inicial. Intervalos de física inferiores a 0,1 s conservam a fase; ausência de input ou bloqueio prolongado volta ao repouso. Revisão de teleporte explícita distingue mudanças curtas de mapa de deslocamentos reais em quadros demorados.
- Passaram 249 verificações de PNGs/importadores, 197 de sprites/materiais/animação e 2167 de perspectiva/câmera/malhas. As 36 novas verificações cobrem corpo estável com pés alternados, preservação dos recortes, ritmo a 30/60/144 FPS, quadros sem deslocamento, repouso ao soltar teclas, bloqueio, teleporte curto e quadro demorado. Compilação passou com bibliotecas da Unity 6000.6.5f1.
- Registro das oito direções inspecionado em prévia técnica; não é captura da Unity. Física, velocidade e câmera mantêm a configuração existente. Em Play, conferir diagonais em área livre, alternância cardinal/diagonal, parada e contato com paredes. Aparência e fluidez reais no Editor permanecem pendentes conforme PARTE_10A.md.
