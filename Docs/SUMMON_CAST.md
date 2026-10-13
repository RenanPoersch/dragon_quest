# Summons e cast dos herois

Gerados com image_gen integrado, com referências dos sprites existentes dos heróis. Os prompts completos estão em PROMPTS.md.

- PhoenixSummon.png: oito quadros, entrada, preparação, sopro de fogo, recuperação e dissolução.
- BahamutSummon.png: oito quadros, preparação e rajada de energia.
- TitanSummon.png: oito quadros, braços elevados, golpe no chão e erupção de rochas.
- ArenCast.png, LiaCast.png, BramCast.png: quatro poses cada, normal, passo, duas mãos levantadas, retorno.

Os três heróis avançam 28 unidades de tela para a esquerda e retornam ao ponto inicial. As mãos ficam levantadas até o fim da magia/invocação. Magias de ataque, cura, Purificar e Reviver usam cast. Ataques básicos com Infusão usam a animação de arma.

Invocações duram 3,05 segundos, surgem aos 0,35 segundos e atingem o alvo aos 1,55 segundos. Os efeitos de impacto são visuais; Bahamut causa dano de Caos (ver SUMMON_EFFECTS.md). All mantém uma única criatura invocada e exibe o impacto em cada alvo válido. Os alvos são capturados antes de aplicar dano ou cura, incluindo o golpe final.

Os PNGs mantêm o alpha original. Imports usam filtro Point, sem mipmaps e sem compressão. O leitor ajusta os espaçamentos irregulares das folhas, ancora os pés e preserva a escala entre poses.

Validação: compilação completa dos scripts com as bibliotecas da Unity; testes de associação de summons, All, cast dos três heróis, Infusão e temporização; inspeção das seis folhas e renderização de preview sobre fundo escuro. Não foi realizada uma partida na Unity.
