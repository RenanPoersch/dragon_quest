> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

> Registro historico preservado na auditoria dos chats. Para o mapa atual, consulte WORLD_UNIFIED_MAP.md; o jogo usa a pintura continua e nao carrega os quadrantes nem os shaders costeiros antigos.

# Costa e oceano — 13 de outubro de 2026

O gramado e a escala de 110% dos POIs foram preservados. ObliqueScenery.IslandData acrescenta grupos costeiros de rochedos baixos com faces irregulares, topos achatados e alguns recifes parcialmente submersos. A areia seca, a areia molhada e a água rasa possuem cores próprias. Não foram criados colliders ou alterações nas rotas.

WorldCoast.shader é um shader original de materiais procedurais: fissuras e estratos nas rochas, pequenas variações de areia, ondulações, cristas e reflexos em movimento sobre toda a água. A animação usa _Time.y, com amostragem em passos de 1/12 s e pixels de 1/32 unidade no chão. Substitui os traços estáticos do mar. WorldSurf.shader conserva as três faixas de espuma costeira, agora com maior movimento e contraste e conversão correta de cores para projetos em espaço linear. Ambos usam Universal2D.

Nenhuma nova imagem ou recurso externo foi adicionado. Malhas e shaders foram criados por código para este projeto. Os materiais são destruídos junto com ObliqueScenery.

Validação: scripts compilados; 603 verificações de sprites/materiais/movimento e 6517 de perspectiva/câmera/malhas passaram. Essas verificações cobrem C# e geometria, não confirmam compilação gráfica nem aparência em execução no Editor.
