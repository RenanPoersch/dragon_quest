> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Covil do dragao em pixel art

O vulcao agora abre um corredor lateral, seguindo a apresentacao das ruinas: camera horizontal acompanhando o heroi, caminhada com A/D ou setas e degraus suaves ate a camara do dragao.

O panorama mostra paredes de basalto, pilares antigos, quedas de lava e um patamar ao fundo. O chao fica separado da lava. A entrada e a saida estao a esquerda; o dragao repousa a direita.

O dragao inicia cada visita dormindo, com respiracao sutil. Aproximar-se nao o acorda. Use **E — Acordar o dragao** para levantar a cabeca e abrir as asas. O confronto comeca ao concluir as duas paginas da conversa. Esc cancela o confronto; o dragao permanece acordado ate sair do mapa. Uma nova visita reinicia o sono. Depois da vitoria, o corpo desaparece e a essencia liberta ocupa seu lugar; a armadura e a recompensa continuam sendo concedidas apenas uma vez.

Os PNGs foram criados pela ferramenta integrada Image Gen e sao preservados sem alteracao. O jogo amostra as imagens em memoria numa grade de pixels com filtro Point, sem mipmaps nem compressao. O dragao tem tres poses com escala e ancora compartilhadas. Fontes:

- `Assets/Resources/PixelArt/DragonLairPanorama.png`
- `Assets/Resources/PixelArt/DragonLairStates.png`
- Prompts completos em `Docs/ArtReferences/ArtInputs/DragonLair.prompt.json`.

Saves da antiga arena sao aceitos e recuperam a entrada quando nao coincidem com o novo piso. Os tres segredos ainda abrem a barreira; a derrota ainda leva a equipe para a hospedaria.

## Validacao

- `Tools/Test-DragonLair.ps1`: compila os scripts e verifica sono, despertar, percurso, alcance de E, degraus, camera, segredos, save antigo e retorno apos derrota.
- Unity: `Tools > Dragon Quest > Validate Dragon Lair` gera capturas e verifica as poses, a interacao real, cancelamento, retorno ao sono e a essencia apos a vitoria.
- Cena de jogo: `Assets/Scenes/Game.unity`. Abra a passagem do vulcao, entre no covil, caminhe para a direita e use E perto do focinho.

Resultados: 2.655 verificacoes do covil e 168 do combate/recompensas do dragao passaram. A Unity 6000.6.5f1 concluiu a validacao com codigo de saida 0 e `DRAGON_LAIR_VALIDATION_OK`. As capturas de caminhada, sono, despertar, asas abertas e essencia foram inspecionadas em 16:9, 16:10 e 4:3.

`Tools/Test-Combat.ps1` continua falhando na verificacao de Thunder com Infusao no menu Magic. A mesma falha foi reproduzida no commit anterior `6af6f35`, sem as alteracoes deste covil. O combate do dragao passou nas verificacoes especificas.
