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
