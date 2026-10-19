# Sons de combate

Seleção V4 aprovada: nove sons refeitos e os demais masters V3. São 47 WAV PCM16 estéreo a 48 kHz, copiados sem reedição ou mudança de volume dos arquivos aprovados. Os hashes, durações, marcadores e fontes estão em `CombatSoundManifest.json`; os créditos CC0 estão em `COMBAT_SOUND_CREDITS.md`.

As oito magias usam a mesma gravação All tanto para alvo único quanto para grupo. Os antigos arquivos exclusivos de alvo único foram removidos. `prepare-meteor` é intencionalmente mudo e não carrega um arquivo de silêncio. `CombatSoundSelection.json` registra a seleção de todas as 56 ações/variantes.

Ataques de Aren, Lia e Bram, golpe forte, provocar, Fênix, Bahamut, Omnislash e Bastião usam a V4. Fênix contém apenas grito de águia e fogo; Bahamut usa descarga de pressão/energia; Bastião começa com grito de batalha. O Omnislash mantém sua sequência completa desde o início da ação, incluindo a carga durante a suspensão em 4,40–5,02 s e o ápice em 5,32 s. O carregamento de meteoro permanece sem efeito sonoro.

`BattleSoundCatalog` resolve habilidades, All, infusões e famílias de inimigos e associa os marcadores dos masters aprovados à apresentação visual. Limites, sopro, provocar e resoluções do meteoro preservam a sequência desde o começo da ação. `BattleSoundController` mantém oito fontes 2D, pitch 1 e ganho 0,85; não recorta os WAV, não altera a música nem acumula eventos por frame. A composição da cena e o controle de foco permanecem os existentes.

O volume dos masters foi equilibrado nas prévias a aproximadamente -23 LUFS e teto de -2,5 dBTP. A aplicação utiliza esses mesmos PCM, sem uma nova normalização. A validação desta aplicação não foi executada, conforme pedido do usuário. O utilitário opcional `Tools/Test-CombatSounds.ps1` acompanha o catálogo atual para uso futuro.
