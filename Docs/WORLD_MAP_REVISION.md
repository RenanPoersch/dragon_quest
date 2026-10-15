> Registro historico preservado na auditoria dos chats. Para o mapa atual, consulte WORLD_UNIFIED_MAP.md; o jogo usa a pintura continua e nao carrega os quadrantes nem os shaders costeiros antigos.

# Revisão do mapa mundial — 12 de outubro de 2026

WorldGroundMap.png foi editado com image_gen a partir do terreno anterior. A versão final é uma textura opaca de 1536 × 1024, em pixel art, com grama clara e detalhes discretos. Todos os caminhos foram removidos a pedido do usuário. O PNG gerado foi copiado integralmente, sem edição por outras ferramentas. O importador anterior conserva Point/Clamp, sem mipmaps ou compressão.

Prompt final: “Edit this RPG terrain texture. REMOVE ALL dirt paths completely, replace every path pixel with seamless grassy meadow matching surrounding terrain. No trails, roads or connections of any kind. Keep light muted sage green and restrained crisp 16-bit pixel art style. Also reduce grass clumps and stones by about half to create calm open terrain, with subtle broad grass color patches, no busy speckled texture. Flat overhead rectangular full bleed land texture 1536x1024, no coast/water (rendered separately), no POI, trees, buildings, mountains, text, grid or borders.”

ObliqueScenery.IslandData substitui a antiga parede costeira por faixas inclinadas de rocha, areia seca, areia molhada e água rasa. Rochas pequenas quebram a regularidade do contorno. A malha física da ilha, as posições dos POIs, colisores e pontos de viagem permanecem iguais. WorldSurf.shader anima três faixas descontínuas de espuma com deslocamento e opacidade em passos discretos. WorldPoi acrescenta duas sombras de contato sob os sprites existentes, sem modificar a folha WorldPoiSprites.png.

Validação: scripts compilados com as bibliotecas Unity 6000.6.5f1; 603 verificações de sprites/materiais/movimento e 6517 de perspectiva/câmera/malhas passaram. Esses testes não substituem a inspeção do shader e da animação no Editor Unity.
