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
