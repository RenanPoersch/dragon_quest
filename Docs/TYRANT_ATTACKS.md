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

Aos 20% de HP do tirano, a postura de conjuração fica persistente, inclusive durante as ações dos outros participantes. Um meteoro aparece acima da arena e cresce com o progresso. A contagem considera apenas os turnos próprios do tirano:

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

Validação: executar `Tools/Test-Castle.ps1`, `Tools/Test-Combat.ps1` e `Tools/Test-UnityScripts.ps1`. Conferir em Play o tamanho do rei no trono, a ausência de avisos, cinco turnos próprios, pose persistente, crescimento do meteoro, escudo em ataques físicos/mágicos, absorção por cada herói e impacto quando o portador está derrotado ou sem a armadura.

Nesta alteração passaram 518 verificações do castelo/Protect/apresentação, a compilação com Unity 6000.6.5f1 e os testes existentes de recuperação/revivência da Fênix, duração/dano do Shield de Titan e estados simultâneos. As artes passaram em 102 verificações de PNG, importadores, transparência, cópia integral e recortes. A prévia técnica das artes e das cinco etapas foi inspecionada; não é captura da Unity. A suíte geral `Test-Combat.ps1` continua falhando no caso anterior de Infusão/Thunder, antes de executar os checks do castelo. A conferência nativa em Play permanece pendente.
