# Sons de combate

Seleção aprovada até a V10: 47 WAV PCM16 estéreo a 48 kHz. Provocar e Fênix usam a V5, Omnislash usa o final grave V8, Lia usa a descarga mágica V9 e Bahamut usa o feixe de energia e estrondo V10. Os masters aprovados são copiados sem nova edição, exceto a incorporação da camada corporal V8 nos contatos físicos existentes.

A camada de receber golpe usa pancada real e contato de carne, substituindo o antigo material oco dentro de 14 composições. Não há evento de dano separado ou áudio duplicado. As demais gravações, posições, panoramas e durações dessas ações são preservados. No Omnislash, a carga e o último golpe permanecem idênticos à V8 a partir de 4,35 s; somente os contatos iniciais são recompostos. Os masters mantêm a referência aproximada de -23 LUFS e teto de -2,5 dBTP.

As oito magias usam a mesma versão All para alvo único e grupo. `prepare-meteor` permanece intencionalmente mudo. Bastião continua começando com o grito de batalha aprovado. Demais ações mantêm a seleção anterior. `CombatSoundSelection.json` registra todas as 56 ações/variantes; `CombatSoundManifest.json` registra os 47 recursos instalados e seus hashes, níveis, marcadores e fontes. Créditos e proveniência em `COMBAT_SOUND_CREDITS.md`.

`BattleSoundCatalog` associa os marcadores dos masters aos impactos visuais. Os limites, sopro, provocar e resoluções do meteoro preservam a sequência desde o começo da ação. `BattleSoundController` mantém oito fontes 2D, pitch 1 e ganho 0,85, sem recorte dos WAV, mudança na música ou duplicação de eventos. Os GUIDs e parâmetros dos importadores existentes são mantidos.

A aplicação não executa validação ou testes Unity, conforme pedido do usuário. A cópia dos arquivos, os hashes do manifesto e a preservação das alterações de outras tarefas são conferidos para o commit.
