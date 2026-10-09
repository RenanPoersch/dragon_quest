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
