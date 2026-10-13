# Menu de batalha clássico

Menu integrado ao `PrototypeBattleHud` existente. A batalha mantém ações, matérias, itens, custos, regras de alvo, IA e recompensas atuais.

- Arena acima; menu azul com borda clara, texto branco e duas metades iguais abaixo. Comandos e alvos à esquerda; nomes, HP, MP, Limit e condições da equipe à direita.
- Ataque e substituido por Limit quando a barra chega a 100%; Limit fica oculto enquanto nao estiver pronto. Magic e Summon abrem submenus separados e so aparecem quando o personagem possui habilidades dessas categorias. A categoria acompanha a origem da materia mesmo com combos All/Infusao. Esc ou Voltar retorna primeiro dos alvos ao submenu, depois aos comandos principais.
- Barras de HP (verde), MP (azul) e Limit (roxo) usam toda a altura, com preenchimento proporcional ao recurso e numeros brancos sobrepostos.
- Seta branca animada sobre quem está agindo, inclusive inimigos. Mão branca em pixel art no comando ou alvo selecionado.
- Clique nos comandos/alvos ou no personagem na arena para confirmar. Setas cima/baixo ou W/S selecionam; Enter/Espaço confirmam; Esc volta. Ações em área mostram todos os alvos e consomem uma única ação.
- Dano branco e cura verde sobem junto do personagem afetado e desaparecem após 1,4 segundo. Eventos registram valores efetivos após limites de HP/MP, proteção, veneno, drenagem, reviver e efeitos em área. Recuperação de MP identifica o recurso.
- Amarelo é reservado a dano com `TakeDamage(amount, critical: true)`. As regras atuais não sorteiam críticos; esta etapa não muda o balanceamento dos golpes existentes.

## Fundos por local

O controlador recebe `locations.LocationId` e captura `BattleLocationId` ao iniciar o encontro. O HUD tenta carregar `Resources/BattleBackgrounds/<id-do-mapa>` uma vez por batalha. Exemplo: `Assets/Resources/BattleBackgrounds/volcano.png` ou `castle.png`. Nenhuma nova imagem é exigida: sem arquivo, a arena usa cores provisórias do ambiente. Fundo, sprites, números e menu têm posições independentes.

## Verificação

Scripts compilados com as bibliotecas da Unity 6000.6.5f1; 2.009 verificações de combate passaram, incluindo dano efetivo, marcação de crítico, cura limitada ao HP máximo, restauração de MP e histórico visual limitado. Não houve conferência visual em Play nesta conversa.

No Unity, reinicie Play e entre em uma batalha. Confira seleção com mouse/teclado, seta trocando de personagem, cura e ataques em área com números em cada alvo, proteção/veneno, resultado e retorno à exploração. Entre em um encontro em outro mapa para conferir a seleção independente do fundo.
