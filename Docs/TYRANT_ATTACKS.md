# Animações dos golpes do tirano

Folhas originais criadas com ImageGen, com quatro quadros sequenciais em uma linha, PNG RGBA transparente de 2172 × 724 px. Referência de personagem: TyrantBattle.png. Prompts completos nos arquivos de mesmo nome com extensão .prompt.txt; ajustes nos .correction.prompt.txt.

| Ação do jogo | Folha | Sequência |
| --- | --- | --- |
| royal-strike | TyrantRoyalStrike.png | Preparação, movimento, impacto, retorno |
| royal-heavy | TyrantRoyalHeavy.png | Elevação, descida, impacto esmagador, retorno |
| prepare-meteor | TyrantMeteorCharge.png | Concentração crescente em quatro etapas |
| flaming-meteor | TyrantFlamingMeteor.png | Queda inicial, aproximação, explosão, dissipação |

Os quadros têm larguras variáveis. A leitura encontra os intervalos transparentes, preservando o PNG integral. Importação Point/Clamp, alpha e leitura habilitados, sem compressão, mipmaps ou redução. As folhas estão ligadas ao controlador de combate.

## Meteoro em cinco turnos

Aos 20% de HP do tirano, o rei aguarda Shield em sua postura normal. A animação de Shield termina por completo antes de começar a pose de conjuração, que permanece durante as ações dos outros participantes. Durante a carga fica apenas o sprite do rei: não há meteoro separado acima da arena. Um número flutuante, de 1 a 5 e sem rótulo, aparece uma vez por turno próprio do tirano. Usa fonte 44 (dano: 40), subida de 40 unidades por segundo, sombra e dissipação de 1,4 segundo, como os danos. O primeiro número aguarda Shield terminar; os demais surgem ao apresentar a ação do turno. A contagem considera apenas os turnos próprios do tirano:

1. Protect: cria a barreira e começa a canalização.
2. Aumenta a carga.
3. Aumenta novamente a carga.
4. Completa a carga.
5. Lança o meteoro.

Protect reduz todo dano recebido pela metade, com arredondamento para cima nos valores ímpares. Dura até a resolução do quinto turno. O evento de dano registra se houve escudo para exibir `TyrantShieldHit`, mesmo num golpe fatal. Protect usa sua própria folha `TyrantProtect` e não altera a habilidade de proteção de Bram.

`CastleSpellAnimation` apresenta a aproximação do meteoro, a explosão e a dissipação de `TyrantFlamingMeteor`. Se houver portador vivo da Armadura dracônica, a queda se contrai para o portador e a folha `MeteorAbsorption` mostra a energia entrando no símbolo vermelho da escama. Nesse caso ninguém perde HP. Sem portador vivo, o impacto derrota todos, inclusive personagens protegidos por Shield/Protect. Reviver o portador antes do lançamento restaura a absorção; derrotar o espírito antes disso interrompe a conjuração.

O lançamento dura 3,6 segundos, com dano, números e desaparecimento sincronizados ao impacto em 2,1 segundos. Foram retirados o texto sobre a cabeça, a previsão da próxima habilidade, o painel textual de aviso do meteoro e os nomes de ações inimigas durante a luta do castelo. Os efeitos mostram o progresso visualmente.

## Artes novas

Imagens criadas com a ferramenta integrada imagegen, preservadas integralmente em `Assets/Resources/PixelArt`: `TyrantThrone.png` (rei normal sentado, referência `NpcRoster`), `TyrantProtect.png`, `TyrantShieldHit.png` e `MeteorAbsorption.png`. Cada efeito tem quatro quadros horizontais. Prompts finais completos em `Docs/ArtInputs/<Nome>.prompt.txt`.

Validação: executar `Tools/Test-Castle.ps1`, `Tools/Test-Combat.ps1` e `Tools/Test-UnityScripts.ps1`. Conferir em Play o tamanho do rei no trono, a ausência de avisos, cinco turnos próprios, Shield antes da conjuração, pose persistente sem meteoro separado, números únicos de 1 a 5, fonte 44 contra dano 40, escudo em ataques físicos/mágicos, absorção por cada herói e impacto quando o portador está derrotado ou sem a armadura.

Nesta revisão passaram 605 verificações do castelo/contagem/sequência e a compilação dos scripts com as bibliotecas da Unity. A validação cobre os cinco turnos reais, redução de dano, finais, número único por turno, ausência de repetição entre quadros, reinício em nova batalha e ordem Shield → pose de conjuração/primeiro número. As imagens anteriores são preservadas integralmente. A conferência nativa em Play permanece pendente.
