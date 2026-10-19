# Animações dos golpes do Rei Rubro

Folhas originais criadas com ImageGen, com quatro quadros sequenciais em uma linha, PNG RGBA transparente de 2172 × 724 px. Referência de personagem: TyrantBattle.png. Prompts completos nos arquivos de mesmo nome com extensão .prompt.txt; ajustes nos .correction.prompt.txt.

| Ação do jogo | Folha | Sequência |
| --- | --- | --- |
| royal-strike | TyrantRoyalStrike.png | Preparação, movimento, impacto, retorno |
| royal-heavy | TyrantRoyalHeavy.png | Elevação, descida, impacto esmagador, retorno |
| prepare-meteor | TyrantMeteorCharge.png | Concentração crescente em quatro etapas |
| flaming-meteor | TyrantMeteorThrow.png + TyrantFireballRelease.png | Duas mãos erguidas, recuo, arremesso da esfera e impacto central |

Os quadros têm larguras variáveis. A leitura encontra os intervalos transparentes, preservando o PNG integral. Importação Point/Clamp, alpha e leitura habilitados, sem compressão, mipmaps ou redução. As folhas estão ligadas ao controlador de combate.

## Contagem de cinco turnos e lançamento no sexto

Aos 20% de HP do Rei Rubro, o rei aguarda Shield em sua postura normal. A animação de Shield termina por completo antes de começar a pose de conjuração, que permanece durante as ações dos outros participantes. Durante a carga fica apenas o sprite do rei: não há meteoro separado acima da arena. Um número flutuante, de 5 a 1 e sem rótulo, aparece uma vez por turno próprio do Rei Rubro. Usa fonte 44 (dano: 40), subida de 40 unidades por segundo, sombra e dissipação de 1,4 segundo, como os danos. O primeiro número aguarda Shield terminar; os demais surgem ao apresentar a ação do turno. A contagem considera apenas os turnos próprios do Rei Rubro:

1. Protect: cria a barreira e começa a canalização.
2. Aumenta a carga.
3. Aumenta novamente a carga.
4. Completa a carga.
5. Exibe **1** e mantém a preparação. A equipe ainda recebe um turno completo.
6. Lança o meteoro, sem repetir o número 1 nem mostrar zero.

Protect reduz todo dano recebido pela metade, com arredondamento para cima nos valores ímpares. Dura até a resolução do sexto turno. O evento de dano registra se houve escudo para exibir `TyrantShieldHit`, mesmo num golpe fatal. Protect usa sua própria folha `TyrantProtect` e não altera a habilidade de proteção de Bram.

`CastleSpellAnimation` usa quatro poses novas de `TyrantMeteorThrow` quando não há portador vivo. O rei ergue as duas mãos a 0,4 s enquanto a esfera separada de `TyrantFireballRelease` cresce até 1,65 s. Puxa as duas mãos e a esfera para trás até 2,35 s, então inclina o corpo e arremessa com as duas mãos para a frente e para baixo. A bola percorre um arco até o centro da arena (640, 240), chegando a 3,15 s. O sprite de meteoro não aparece na batalha. O impacto exibe **9999** sobre cada atingido, mantendo a perda real de HP e Limit do combate. Veneno, cura e variações de MP conservam seus números.

Com portador vivo da Armadura dracônica, a absorção usa a bola de fogo do casting do Rei Rubro, separada em camada própria de `TyrantFireballRelease`. Não desenha `TyrantFlamingMeteor` nem o antigo emblema `MeteorAbsorption`. O portador avança 215 unidades e sobe 100; Aren usa os quatro sprites de `ArenMeteorDraconic`, com o peito projetado para a esquerda e os braços para trás. As tiras animadas de `MeteorFireRibbons` ligam a bola de fogo ao peito em 2D, enquanto a bola diminui até desaparecer. O portador sustenta a posição até terminar a transferência e retorna ao ponto inicial antes do próximo turno. Outros portadores mantêm sua identidade e também avançam e flutuam. Ninguém perde HP nessa sequência. Sem portador vivo, o impacto derrota todos, inclusive personagens protegidos por Shield/Protect. Reviver o portador antes do lançamento restaura a absorção; derrotar o espírito antes disso interrompe a conjuração.

No lançamento destrutivo, o controlador adia a resolução real do dano até a esfera atingir o centro em 3,15 segundos. Nesse instante os HP caem e toda a tela, inclusive os painéis e barras externas, fica branca. Os números de dano são desenhados sobre o branco para continuarem legíveis. Após 0,25 segundo de branco, o controlador abre automaticamente a cena externa do meteoro, sem confirmação ou painel de desfecho. A absorção mantém sua duração de 3,6 segundos. Foram retirados o texto sobre a cabeça, a previsão da próxima habilidade, o painel textual de aviso do meteoro e os nomes de ações inimigas durante a luta do castelo. Os efeitos mostram o progresso visualmente.

A absorção preserva os 3,6 segundos: avanço até 0,7 s, transferência de 0,8 a 2,65 s e retorno a partir de 2,8 s. Todas as camadas são sprites com alpha e filtro Point; não há objetos 3D, malhas, feixes sólidos ou sistemas de partículas. As três artes desta revisão foram criadas com a ferramenta integrada ImageGen; prompts finais em `Docs/ArtInputs/CometAbsorption.prompts.json`.

Durante a absorção, Aren usa `ArenMeteorDraconic`: as mesmas quatro poses aéreas com couraça de escamas vermelhas, ombreiras dracônicas e braçadeiras. `DraconicAbsorptionAura` envolve o corpo com um contorno de escamas e arcos de fogo que se curvam para dentro do peito. A aura acompanha o avanço e a altura do herói, pulsa durante a transferência e tem camadas atrás do corpo e à frente da chegada das tiras. Surge entre 0,12 e 0,5 s e se dissipa até 3,25 s durante o retorno. As novas folhas são sprites transparentes gerados com ImageGen; referências e prompts completos em `Docs/ArtInputs/DraconicAbsorption.prompts.json`.

Validação da armadura/aura: scripts compilados com as bibliotecas da Unity, 874 verificações do castelo e validação nativa de 216 instantes com renderização de seis prévias. A conferência cobre importação Point/Clamp/alpha sem compressão, quatro poses armaduradas, aura atrás e à frente do corpo, recepção das tiras, enquadramento inteiro na tela, dissipação antes da aterrissagem e preservação do impacto sem absorção.

## Artes novas

Imagens criadas com a ferramenta integrada imagegen, preservadas integralmente em `Assets/Resources/PixelArt`: `TyrantThrone.png` (rei normal sentado, referência `NpcRoster`), `TyrantProtect.png`, `TyrantShieldHit.png` e `MeteorAbsorption.png`. Cada efeito tem quatro quadros horizontais. Prompts finais completos em `Docs/ArtInputs/<Nome>.prompt.txt`.

Validação: executar `Tools/Test-Castle.ps1`, `Tools/Test-Combat.ps1` e `Tools/Test-UnityScripts.ps1`. Conferir em Play o tamanho do rei no trono, a ausência de avisos, cinco turnos próprios, Shield antes da conjuração, pose persistente sem meteoro separado, números únicos de 5 a 1, fonte 44 contra dano 40, escudo em ataques físicos/mágicos, absorção por cada herói e impacto quando o portador está derrotado ou sem a armadura.

Nesta revisão passaram 605 verificações do castelo/contagem/sequência e a compilação dos scripts com as bibliotecas da Unity. A validação cobre os cinco turnos reais, redução de dano, finais, número único por turno, ausência de repetição entre quadros, reinício em nova batalha e ordem Shield → pose de conjuração/primeiro número. As imagens anteriores são preservadas integralmente. A conferência nativa em Play permanece pendente.

Revisão da absorção: uma cópia limpa de HEAD com apenas esta atividade passou 874 verificações do castelo e compilou todos os scripts com as bibliotecas da Unity. A validação nativa `MeteorAbsorptionValidation.Run` passou em projeto isolado, verificando importação e 216 instantes da sequência e renderizando seis prévias com as mesmas funções de desenho do jogo. Confirmou ausência do sprite do cometa/emblema na absorção, transferência em sprites e preservação do impacto destrutivo. A suíte geral `Test-Combat.ps1` continua falhando em `AbilityChecks.CheckEncounterStrategies` com “Habilidade indisponível para este personagem”; seus arquivos de combate não são alterados nesta atividade.

Validação da revisão do lançamento: `Tools/Test-MeteorRelease.ps1` executa as regras do castelo, compila os scripts e abre um projeto Unity isolado. Confere os cinco turnos de preparação, dano adiado, clarão simultâneo, corte automático único inclusive em quadro longo, absorção e prévias renderizadas da bola crescente.

As poses de arremesso foram criadas com a ferramenta integrada ImageGen, preservando a identidade do rei, em uma folha transparente de quatro quadros. Prompt final em `Docs/ArtInputs/TyrantMeteorThrow.prompt.txt`; importação Point/Clamp/alpha, sem compressão ou mipmaps. A absorção continua usando sua própria pose e os tempos anteriores.
