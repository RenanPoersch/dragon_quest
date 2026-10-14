# Cenários de batalha em pixel art

Seis artes originais geradas com a ferramenta integrada ImageGen em 14 de outubro de 2026. Referências visuais: EastForestBackdrop, QuarryBackdrop e DragonLairPanorama do projeto. Os PNGs gerados foram preservados integralmente, sem edição de pixels ou redimensionamento posterior. Prompts completos em Docs/ArtInputs/BattleBackground-<mapa>.prompt.txt.

| Região / encontro | Arquivo em Assets/Resources/BattleBackgrounds |
| --- | --- |
| Mapa-múndi / bandidos piratas | world.png — litoral da ilha com clareira de terra e grama |
| Pedreira dos Monólitos / golens | west-forest.png — paredões de pedra e piso de cascalho |
| Floresta Leste / lobos | east-forest.png — clareira entre árvores antigas |
| Periferia do castelo / guardas | city.png — pátio de pedra junto à muralha |
| Interior do castelo / tirano e espírito | castle.png — salão do trono com bandeiras roxas |
| Covil do dragão | volcano.png — plataforma ampla de basalto com lava ao fundo |

PrototypeBattleHud já seleciona Resources.Load<Texture2D>("BattleBackgrounds/" + BattleLocationId) a cada encontro. Os nomes dos arquivos ativam essa seleção em todas as batalhas atuais. O fundo preenche a arena de 1240 × 396, com leve recorte proporcional quando necessário. Os combatentes, seus efeitos e o menu continuam desenhados por cima. As posições dos heróis foram ajustadas levemente para que seus pés permaneçam sobre a superfície de combate dos seis fundos; sem textura carregada, o posicionamento anterior é conservado.

Importação: Point/Clamp, sem mipmaps, compressão ou escala NPOT, limite 4096, textura opaca e sem cópia de leitura em CPU. São fundos de batalha próprios para o enquadramento da arena.

Verificação: inspeção visual das seis artes, leitura dos PNGs, opacidade e dimensões, hashes das cópias, configurações dos importadores e correspondência de todos os IDs de região. A apresentação no modo Play ainda precisa de conferência na Unity.
