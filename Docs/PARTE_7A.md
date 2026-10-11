# Parte 7A — ampliação da cidade

A cidade agora mede **72 × 48 unidades**, comparada às 36 × 24 anteriores: quatro vezes a área externa. A câmera conserva o mesmo tamanho de visão; o jogador ganha espaço para explorar. A praça e as três lojas continuam próximas, para manter o percurso da missão de entrega.

## Áreas

| Região | Como chegar | O que existe nesta entrega |
| --- | --- | --- |
| Praça central | Local inicial | Fonte, aldeão, baú, lojas e hospedaria |
| Bairro residencial | Siga a rua principal para oeste | Casas de artesãos, moradora, capela e casas junto ao rio |
| Jardins reais | Vá para leste e siga ao norte | Jardineiro, canteiros, lago e pavilhão |
| Ruínas antigas | Siga a rua leste para sul e cruze a ponte | Exploradora, paredes antigas, pedestal e torre |
| Distrito do castelo | Siga a rua central para norte | Pátio, guarda que inicia a batalha, castelo e duas torres |
| Portão sul | Cruze a ponte central e siga para sul | Portão e muralha que delimitam o mapa |

O cabeçalho informa a região em que o herói está. Moradora, jardineiro e exploradora têm diálogos próprios e orientações sobre os arredores. A cena ainda usa formas coloridas geradas por código; sprites e tiles finais entram na entrega de arte.

O rio e o lago têm colisão. As pontes oeste, central e leste permitem cruzar o rio. Não há saída para fora da muralha. Somente as três lojas possuem entrada funcional nesta versão; a capela, o pavilhão, as casas, o castelo e o pedestal são cenário. Os três segredos e o dragão serão implementados nas próximas entregas.

## Roteiro em Play

1. Saia de Play, aguarde a importação e abra `Assets/Scenes/CidadePrototipo.unity` na Unity 6000.6.5f1. Confira o Console e entre em Play.
2. Clique em Game. Use WASD/setas, E para conversar/interagir e I para o inventário. O cabeçalho deve começar em **Praça central**.
3. Vá para oeste pela rua principal, depois para norte pela rua do bairro residencial. Converse com a moradora e alcance a capela. A câmera deve acompanhar o herói além dos antigos limites.
4. Volte para a rua principal, siga para leste e depois norte. Converse com o jardineiro, circule o lago e visite o pavilhão. O lago deve bloquear movimento sobre a água.
5. Siga a rua leste para sul, cruze a ponte e converse com a exploradora. Alcance o pedestal e circule as ruínas. Confira que a ponte é atravessável e a água nas laterais bloqueia passagem.
6. Experimente também as pontes central e oeste. Alcance o portão sul e caminhe próximo às quatro muralhas: o herói deve permanecer dentro do mapa.
7. Volte à praça, entre e saia de cada loja. Confira retorno diante da mesma porta, ouro/estoque preservados e câmera acompanhando novamente os bairros distantes.
8. Vá para o pátio do castelo, mais ao norte, e use E no guarda. Vença a patrulha: a recompensa continua somando Guarda do tirano 25 + Soldado 10. Retorne à cidade e confira que a exploração continua.
9. Abra o inventário e converse com os novos moradores. O movimento deve parar durante os menus/diálogos e voltar ao fechá-los.
10. Confira ausência de erros vermelhos no Console. O roteiro de compras, entrega e reposição continua em `PARTE_6C.md`.

## Código e validação

- `PrototypeCityLayout.cs`: limites compartilhados, posições importantes, bairros, edifícios e blocos do cenário. Alterar os limites aqui atualiza a abertura da cidade e o retorno das lojas.
- `PrototypeCity.cs`: transforma esses dados em objetos, sprites provisórios e colisores; cria os moradores e conecta as interações existentes.
- `PrototypeLocations.cs`: identifica o bairro e recupera os limites ampliados ao sair de lojas.
- `PrototypeHud.cs`: placas dos moradores, capela, pedestal, castelo e portão.
- `Tests/CityLayoutChecks.cs` e `Tools/Test-CityLayout.ps1`: verificam caminhos na geometria compartilhada sem iniciar outra instância da Unity.

Passaram **57 verificações de caminhos**, com amostragem a cada 0,5 unidade e colisores ampliados pelo tamanho do herói mais margem. Foram verificadas regiões, pontos de interação, portas/retornos, pontes, extremidades, água, muralhas e ausência de áreas caminháveis isoladas. A amostragem encontrou aproximadamente **2.511 unidades quadradas acessíveis**.

Também passaram **1.518 verificações de combate/inventário/lojas/progresso**, **15 de exploração**, e a compilação dos scripts com as bibliotecas da Unity 6000.6.5f1. A contagem de combate inclui ações durante partidas completas.

A análise geométrica não executa o Rigidbody2D, a câmera ou os cliques. A aparência, a física em movimento e as transições ainda precisam do roteiro em Play. Não foi gerado executável. Ouro, estoque, missão e Limit continuam persistindo somente na mesma sessão de Play.
