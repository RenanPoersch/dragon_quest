# Parte 1 — exploração

## Como executar

1. Saia de Play, caso esteja ativo.
2. Aguarde a Unity importar e compilar os novos arquivos.
3. Na janela Project, abra `Assets → Scenes → CidadePrototipo` com dois cliques.
4. Clique em Play e depois dentro da janela Game para dar foco ao teclado.
5. Use WASD ou setas para andar.

Se nada aparecer, confirme que abriu CidadePrototipo, e não SampleScene. O mapa é construído somente ao entrar em Play.

## Verificação manual

- Confirmar que há herói, ruas, praça e prédios, além das instruções na tela.
- Andar nas quatro direções com WASD e repetir com as setas.
- Andar na diagonal: deve ter a mesma velocidade total que andando reto.
- Soltar as teclas: o herói deve parar.
- Pressionar esquerda e direita juntas: não deve haver movimento horizontal.
- Caminhar contra a fonte, bancos, árvores e prédios: não atravessar.
- Segurar uma direção contra uma muralha por alguns segundos: permanecer dentro do mapa.
- Andar na diagonal contra uma parede: conseguir deslizar ao longo dela.
- Percorrer a cidade: câmera deve acompanhar e parar nos limites.
- Alternar para outro aplicativo: o movimento deve parar ao perder foco.
- Sair e entrar em Play novamente: reaparecer ao sul da praça sem duplicar o mapa.
- Confirmar ausência de erros vermelhos no Console.

## Limites desta entrega

As lojas, hospedaria e castelo são obstáculos com placas. Não há interiores, interações, combate ou inventário nesta etapa. Arte e interface são provisórias.

O mapa é reproduzível a partir do código de `PrototypeCity`. As regras de movimento e câmera ficam em componentes separados, para reutilização quando o cenário definitivo for montado.
