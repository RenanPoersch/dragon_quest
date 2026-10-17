# Ultimates dos heróis

O comando **LIMIT** continua substituindo Ataque quando a barra chega a 100%. Cada herói tem uma habilidade própria, independente das matérias. Todas consomem uma barra completa e uma ação, sem MP. O cheat de LIMIT infinito continua funcionando.

| Herói | Ultimate | Efeito |
| --- | --- | --- |
| Aren | Omnislash | 12 cortes e uma finalização com dano triplo, exclusivamente no inimigo escolhido. Cada corte calcula defesa, crítico e Shield e produz seu próprio número flutuante. |
| Lia | Aurora da Vida | Recupera 80% do HP máximo de cada herói, limitado ao HP máximo. Revive os derrotados com 80%. Remove veneno, correntes e provocação, preservando proteções benéficas. |
| Bram | Bastião Inabalável | Ataque físico de escudo em todos os inimigos vivos, poder 65. Concede Shield de 50% por três ações futuras de cada aliado vivo, incluindo Bram. Não acumula redução com outro Shield. |

Percentuais de recuperação e redução de dano arredondam para cima. A ultimate de Lia aceita a equipe com HP cheio para permitir purificação.

## Apresentação

- Aren: referência de ritmo e impacto no Omnislash de nível 4. Duas folhas dedicadas, com 16 poses de preparação/cortes e 16 de salto, suspensão com espada recuada, mergulho e aterrissagem. Alterna quatro sequências de cortes, some entre as investidas e nunca apaga o cenário. Os 13 números de dano seguem os impactos. Suspensão de 0,62 segundo; impacto final em 5,32 segundos; duração total de 6,5 segundos.
- Lia: referência no Great Gospel. 16 poses de oração, abertura dos braços, levitação e recuperação. Outra folha de 16 quadros abre asas de luz branca e dourada, com penas e chuva de luz verde. O anjo aparece à frente dos personagens para manter as duas asas inteiras visíveis. Os sprites de cura/ressurreição aparecem sobre os aliados. Recuperação em 3,65 segundos; duração total de 6 segundos.
- Bram: referência no Catastrophe. 16 poses próprias de carga, subida, preparação do disparo e aterrissagem. Suspenso no ar, canaliza um feixe azul/dourado pelo escudo sobre a formação inimiga; o ataque mantém sua natureza de golpe de escudo. Durante a descarga, mantém uma única pose firme e a posição varia menos de dois pixels, em um tremor leve. Só desce após o feixe terminar. Outras duas folhas de 16 quadros animam a descarga com explosões e a barreira com o leão dourado. Impacto em 3,5 segundos; duração total de 6,5 segundos.

As animações usam **oito folhas PNG originais, com 128 quadros distintos**, em `Assets/Resources/PixelArt/Ultimates`. Poses e efeitos são desenhos de sprites, sem círculos, feixes ou explosões desenhados por linhas de GUI. As transformações do código apenas posicionam os sprites, espelham a direção dos cortes e fazem a trajetória aérea.

Os PNGs e sua transparência foram preservados diretamente da geração pelo ImageGen integrado. Importação Point/Clamp, sem mipmaps, compressão ou redução da resolução nativa. Arquivos `*Frames.json` registram os recortes e as âncoras dos pés; cada personagem conserva a mesma escala entre suas poses. O conjunto de prompts está em `Docs/ArtInputs/UltimatesSprites.prompts.json`.

Uma proteção que intercepta Omnislash recebe a sequência inteira. Se o alvo morre antes do último corte, os golpes restantes não transferem dano a outro inimigo; o alvo permanece visível até a finalização. Vitória e próxima ação aguardam a apresentação terminar.

## Prévia e validação

Na Unity, use **Tools → Dragon Quest → Preview Ultimates** para escolher o herói, reproduzir ou percorrer a animação pela barra de tempo. **Validate Ultimates** verifica os sprites, o desaparecimento, a pose de espada e a finalização aérea.

**Export Ultimate Sprite Preview** renderiza os comandos de sprites em uma textura da GPU, em 24 quadros por segundo, sem abrir ou modificar cenas. Exporta os três conjuntos para `Logs/UltimateSpritePreview`, incluindo fundo e inimigos. As legendas de UI e a sobreposição adicional dos sprites antigos de cura são exclusivas da prévia interativa/jogo.

`Tools/Test-Ultimates.ps1` executa os testes de efeitos, custos, alvos, proteção, dano individual, críticos e cronologia sem iniciar outra Unity. `Tools/Test-UnityScripts.ps1` compila os scripts com as bibliotecas do editor instalado.

`Tools/Inspect-UltimateSprites.py` verifica os PNGs, transparência, unicidade dos quadros, metadados e importadores sem modificar imagens. `--write-metadata` recalcula apenas os arquivos de coordenadas.
