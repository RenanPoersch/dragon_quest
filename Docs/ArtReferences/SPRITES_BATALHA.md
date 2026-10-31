> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Sprites de batalha dos herois

Folhas originais geradas com ImageGen a partir de ArenWalk.png, LiaWalk.png e BramWalk.png, preservando identidade, roupas, cores e acabamento. Cada PNG transparente tem 2172x724 pixels e quatro poses voltadas para a esquerda: postura de batalha, preparacao, impacto e retorno.

- ArenBattle.png: espada desembainhada, preparacao e corte com arco prateado.
- LiaBattle.png: cajado com orbe verde, concentracao e disparo magico.
- BramBattle.png: guarda com escudo, preparacao e investida com impacto dourado.

Arquivos em Assets/Resources/PixelArt. Prompts completos em Docs/ArtInputs/<Nome>Battle.prompt.txt. Importacao Point/Clamp, alpha, leitura habilitada, sem compressao/mipmaps ou reducao (limite 4096). Os PNGs gerados sao preservados integralmente; os recortes existem apenas em UVs.

BattleSprites le as quatro regioes de cada folha, acompanha a base dos pes e utiliza a escala da postura inicial em todas as poses. As regioes seguem os intervalos transparentes reais dos desenhos, evitando cortar a espada ou os efeitos por uma grade uniforme.

Estas folhas representam apenas a postura de batalha e o ataque basico de cada heroi. A selecao usa a chave heroi/acao: aren/attack, lia/attack e bram/attack. Magias, Summon, Limit e outros golpes nao reutilizam essas poses; cada habilidade recebera sua propria folha e registro.

Ao aceitar um ataque basico com sprite registrado, BattleController apresenta preparacao, impacto e retorno por 0,72 segundo. Novos comandos, IA e retorno da batalha aguardam o fim da apresentacao. A seta permanece no atacante durante o movimento; os numeros flutuantes aguardam o impacto (0,18 segundo). A resolucao de dano e recursos continua usando BattleSession.

Scripts compilados com Unity 6000.6.5f1; 2.047 verificacoes de combate passaram. Foram inspecionadas as folhas geradas, a transparencia e as quatro regioes. A animacao nativa em Play ainda precisa de verificacao visual.

Em Play, confira os tres ataques basicos, o ultimo golpe que encerra a batalha, ataques em area e ausencia de comandos duplos durante a animacao. Verifique pes e escala entre as poses, seta no atacante e numeros no alvo no momento do impacto.
