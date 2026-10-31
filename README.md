# Dragon Quest

RPG 2.5D com exploração de uma ilha, batalhas por turnos, três heróis, equipamentos, matérias, chefe secreto e três finais. O jogo usa pixel art, cenários montados por código e apresentação própria para abertura, combate e encerramentos.

## Abrir e executar

1. Adicione esta pasta ao Unity Hub como projeto existente.
2. Abra com **Unity 6000.6.5f1**, a versão registrada em ProjectSettings/ProjectVersion.txt.
3. Aguarde a importação dos pacotes e recursos.
4. Abra **Assets/Scenes/Game.unity** e entre em Play.
5. Use New game para a abertura da jornada ou Continue para carregar um dos três slots.

O destino configurado de build é Windows. O executável desktop inicia em janela sem bordas de tela cheia, em 1920 × 1080; o enquadramento se adapta à tela. Gere a entrega pelo perfil configurado em Build Profiles, com Game como cena habilitada. Builds gerados ficam fora do versionamento.

## Comandos

| Ação | Teclado | Controle |
| --- | --- | --- |
| Movimento e navegação | WASD ou setas | Analógico esquerdo ou direcional |
| Interagir | E | Botão inferior |
| Confirmar | Enter, Espaço ou E | Botão inferior |
| Cancelar/voltar | Esc | Botão direito, bolinha no PlayStation |
| Inventário | I | Botão superior ou Start |
| Salvar diretamente, quando permitido | F5 | Pelo menu de save |
| Preparar fuga de batalha comum | Segurar Q + E | Segurar L2 + R2 |

Chefes não permitem fuga. O menu de salvar não tem botão Voltar; use cancelamento. O inventário permite retornar ao título pela opção Sair. O menu de ajustes de partida abre com backquote no teclado ou Select/clique do analógico direito no controle.

## Jornada e dados locais

O salvamento manual fica disponível no mapa global e na pousada, fora dos estados bloqueados. Há três slots. Os arquivos ficam em Application.persistentDataPath/Saves e as conquistas dos finais em Application.persistentDataPath/Achievements. Heróis que terminam uma batalha mortos não recebem XP.

## Organização

- [Arquitetura e fluxo completo](Docs/Current/ARCHITECTURE.md): bootstrap, jornada, diálogos, navegação, encontros, combate, assets e áudio.
- [Progressão](Docs/Current/LEVEL_PROGRESSION.md), [resultados de batalha](Docs/Current/POST_BATTLE_RESULTS.md) e [persistência](Docs/Current/PERSISTENCE.md).
- [Índice da documentação](Docs/README.md): contratos atuais e referências de autoria em uso.
- Assets/Scripts contém runtime; Assets/Editor contém ferramentas de autoria; Tools contém utilitários externos. Diálogos usam um catálogo com falantes explícitos, menus compartilham a leitura de comandos, GameFlowState define permissões e GameAssets padroniza o carregamento de recursos.

A documentação vigente fica em Docs/Current. Os testes e registros antigos foram retirados do projeto.
