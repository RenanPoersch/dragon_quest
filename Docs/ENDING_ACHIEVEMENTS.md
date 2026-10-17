# Achievements dos finais

| Achievement | Final | Ícone |
| --- | --- | --- |
| The good | Rei libertado: derrotar o espírito mantendo o tirano vivo | Coroa dourada, louros e amanhecer |
| The bad | Castelo destruído pelo meteoro | Torre atingida por um meteoro |
| The ugly | Espírito possui Aren após a queda do tirano | Coroa e olho violeta |

Cada conquista é liberada ao concluir a sequência do respectivo final, antes de voltar
à tela inicial. Uma notificação no canto inferior direito da tela título mostra somente
o achievement recém-liberado: seu ícone, The good, The bad ou The ugly, e o contador 1/3,
2/3 ou 3/3. Não há lista de conquistas ou ícones bloqueados.

A notificação aparece sobre a tela de abertura que mostra DRAGON QUEST, antes das
opções New game e Continue. Permanece por seis segundos e desaparece com fade; não
recebe foco nem bloqueia os controles. O tempo pausa quando o jogo perde o foco.

Conquistas salvas que ainda não foram notificadas são recuperadas automaticamente,
uma por vez, inclusive em perfis anteriores a esta correção. O aviso só é marcado como
exibido após completar os seis segundos na tela. Se a tela for fechada antes disso,
o aviso continua disponível na próxima abertura. Esse registro usa a chave PlayerPrefs
`DragonQuest.EndingAchievements.NotifiedMask.v1`, sem alterar os slots nem apagar finais.
Concluir novamente um final também exibe seu aviso, mantendo a contagem de finais únicos.

O arquivo `Application.persistentDataPath/Achievements/endings.json` acumula os três finais
entre partidas e reinícios, independentemente dos slots de jornada. Novo jogo, sobrescrever
ou excluir um slot não apaga conquistas. Repetir um final não aumenta a contagem. Derrotas
comuns e iniciar uma sequência sem concluí-la não liberam conquistas. Carregar uma jornada
concluída e assistir ao final também registra a respectiva conquista.

A gravação é automática apenas para esse perfil de conquistas. Os slots continuam com
salvamento manual. A escrita usa arquivo temporário, substituição atômica e backup da revisão
anterior válida; falhas preservam o progresso na sessão e registram um aviso no log.
Versões futuras do perfil são preservadas. Não há integração com achievements de plataformas.

Os três PNGs RGBA em `Assets/Resources/PixelArt/Achievements/` foram gerados individualmente
com a ferramenta integrada image_gen. As imagens originais foram copiadas sem edição,
com transparência preservada, filtragem Point, Clamp, sem mipmaps e sem compressão.
Prompts completos e SHA-256 estão em `ArtInputs/EndingAchievements.prompts.json`.

Validação automática:

- `Tools/Test-EndingAchievements.ps1`: mapeamento, seis ordens de conclusão, reinícios,
  repetição, independência de jornadas, falha de escrita, recuperação e versão futura.
- `Tools/Test-UnityScripts.ps1`: compilação dos scripts com bibliotecas da Unity 6000.6.5f1.
- `Tools/Test-EndingAchievements.ps1 -UnityIntegration`: executa as mesmas validações
  de importação e integração em um projeto temporário, sem fechar o Editor aberto.
- `Tools > Dragon Quest > Validate Ending Achievements`: importadores, alpha, fonte, layout e hooks.
- `Tools > Dragon Quest > Preview Ending Achievements`: prévia de uma notificação por vez, sem alterar o perfil.

Conferência em Play: concluir um final e observar seu ícone, nome e contagem sobre a
tela DRAGON QUEST. Perfis que já têm conquistas também recuperam os avisos não exibidos.
O aviso desaparece após seis segundos. Repetir um final conserva a contagem e volta a
exibir seu aviso. Os finais permanecem salvos entre jornadas; derrota comum não libera conquistas.
