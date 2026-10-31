> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../../Current/README.md).

# Sons de combate

Seleção aprovada até a V12: 47 WAV PCM16 estéreo a 48 kHz. O ataque de Bram usa a pancada seca de escudo V12; Omnislash usa a pancada e o corte pesado final V12. Provocar e Fênix usam a V5, Lia usa a descarga mágica V9 e Bahamut permanece com a V10. Os WAV desta aplicação são cópias exatas dos arquivos aprovados.

A camada de receber golpe V8 continua incorporada nos contatos físicos existentes. No Omnislash, a sequência anterior e o carregamento no ar são preservados amostra por amostra até 5,02 s. O trecho novo começa no mergulho e mantém o contato final em 5,32 s. O ataque de Bram mantém o contato em 0,15 s. Os marcadores do catálogo e os importadores existentes continuam compatíveis. Os masters mantêm a referência aproximada de -23 LUFS e teto de -2,5 dBTP.

As oito magias usam a mesma versão All para alvo único e grupo. `prepare-meteor` permanece intencionalmente mudo. Bastião continua começando com o grito de batalha aprovado. Demais ações mantêm a seleção anterior. `CombatSoundSelection.json` registra todas as 56 ações/variantes; `CombatSoundManifest.json` registra os 47 recursos instalados e seus hashes, níveis, marcadores e fontes. Créditos e proveniência em `COMBAT_SOUND_CREDITS.md`.

`BattleSoundCatalog` associa os marcadores dos masters aos impactos visuais. Os limites, sopro, provocar e resoluções do meteoro preservam a sequência desde o começo da ação. `BattleSoundController` mantém oito fontes 2D, pitch 1 e ganho 0,85, sem recorte dos WAV, mudança na música ou duplicação de eventos. Os GUIDs e parâmetros dos importadores existentes são mantidos.

A aplicação não executa validação ou testes Unity, conforme pedido do usuário. A cópia dos arquivos, os hashes do manifesto e a preservação das alterações de outras tarefas são conferidos para o commit.
