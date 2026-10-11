# Parte 7B — poema, três segredos e trilha do vulcão

A lenda da ilha aparece em um poema original de três versos em latim. A camponesa, no bairro residencial da periferia, recita o poema e explica cada verso. Ela não identifica a resposta do altar nem apresenta uma missão de encontrar os segredos.

## Poema da lenda

> Flamma ardet, regnum perit.
> Luna surgit, stella lucet, sol redit.
> Silva antiqua memoriam servat.

| Verso | Explicação dada pela camponesa |
| --- | --- |
| Flamma ardet, regnum perit. | A chama arde; o reino perece. A lenda lembra a queda de um antigo reino. |
| Luna surgit, stella lucet, sol redit. | A Lua se ergue, a Estrela brilha, o Sol retorna. O verso conta o ciclo da noite até o novo dia. |
| Silva antiqua memoriam servat. | A floresta antiga guarda a memória. As árvores conservam vestígios que as pessoas esqueceram. |

O texto foi escrito para a lenda fictícia deste projeto, sem atribuição a um poeta histórico. O vocabulário foi consultado em fontes acadêmicas: [Latin Core Vocabulary, Dickinson College](https://dcc.dickinson.edu/latin-core-list1), [ardeo, Lewis & Short](https://atlas.perseus.tufts.edu/dictionaries/entry/urn%3Acite2%3Ascaife-viewer%3Adictionary-entries.atlas_v1%3Alat.ls.perseus-eng2-n3492/), [pereo, Lewis & Short](https://www.perseus.tufts.edu/hopper/text?doc=Perseus%3Atext%3A1999.04.0059%3Aentry%3Dpereo) e [servo, Lewis & Short](https://atlas.perseus.tufts.edu/dictionaries/entry/urn%3Acite2%3Ascaife-viewer%3Adictionary-entries.atlas_v1%3Alat.ls.perseus-eng2-n43947/).

Depois de terminar a conversa completa, **P** permite reler o poema e suas explicações durante a exploração. Cancelar antes do fim não registra a conversa como concluída. Ouvir a lenda não revela o contador de segredos.

## Os três segredos

1. **Altar nas ruínas antigas, acessadas pelo mapa global:** entre no novo ponto de interesse ao sudoeste da ilha, em **(-7; -5)**, usando E junto à entrada ao sul do ícone. O cenário local possui colunas quebradas, paredes antigas e um altar ao norte. Uma imagem de chama e reino destruído pede um verso da lenda. Após ouvir a camponesa na periferia, o jogador escolhe entre as três frases em latim, com clique ou teclas 1/2/3. A resposta é **Flamma ardet, regnum perit.** Os outros versos deixam o altar frio e permitem tentar novamente. A frase correta acende uma chama e registra o segredo uma única vez. O altar foi retirado da cidade.
2. **Monólitos, na floresta oeste:** a inscrição na pedra repete **Luna surgit, stella lucet, sol redit.** A sequência é **Lua → Estrela → Sol**. Use E em cada monólito. Os símbolos acendem; um erro apaga todos e reinicia, sem dano ou gasto. O lenhador dá pistas sobre a inscrição e a camponesa, sem anunciar a solução. Progresso parcial e conclusão persistem entre viagens.
3. **Árvore oca, na floresta leste:** examine as marcas de garras na pedra. Elas apontam para nordeste, até uma árvore de raízes abertas. Usar E nas raízes revela a **Escama ancestral**, uma única vez. É possível descobri-la pela exploração sem antes ler a pista. A escama aparece na aba Itens do inventário como objeto de história, sem integrar os catálogos de venda ou consumíveis de batalha.

O contador **Sinais antigos: 1/3** aparece somente após resolver o primeiro segredo. Eles podem ser concluídos em qualquer ordem. Escama, altar e monólitos contam como objetivos distintos, sem recompensas de ouro nesta entrega.

Com os três concluídos, volte à passagem entre as montanhas no mapa global e use **E**. A escama reage, os selos se desfazem e as rochas se afastam. O colisor central é desativado e existe um caminho real até o ícone do vulcão. A passagem permanece aberta entre viagens e a escama não é consumida.

A cratera tem uma interação de ambientação. O interior do vulcão, a batalha do dragão e a armadura especial ficam na parte 7C. A Escama ancestral desta entrega é um objeto diferente da armadura futura.

## Roteiro em Play

1. Saia de Play e aguarde a importação. Abra `Assets/Scenes/CidadePrototipo.unity`, entre em Play e confira o Console.
2. Na ilha, examine a passagem antes de resolver qualquer segredo: as rochas devem impedir atravessar. O HUD não deve mostrar o contador.
3. Entre na periferia pelo castelo global. Vá ao bairro residencial a oeste: a camponesa fica em **(-25,5; -3)**. Abra a conversa e cancele com Esc antes do fim: P ainda não deve abrir o poema. Converse de novo até terminar e confira as três explicações. P passa a abrir a leitura.
4. Saia da periferia pelo portão sul e procure **Ruínas antigas** ao sudoeste do castelo global. Use E junto à entrada do ícone. O altar fica em **(0; 4)** do novo cenário; aproxime-se pelo sul. Teste cancelar a escolha, selecionar um verso errado e tentar novamente. A resposta correta acende a chama e mostra 1/3; repetir não aumenta o contador. Use a saída em **(0; -5,8)** para voltar à ilha, junto ao ícone das ruínas. Entre novamente e confirme a chama acesa e o progresso preservado.
5. Na ilha, entre na floresta oeste. A inscrição fica na pedra em **(-2; 3)**. Teste começar pelo Sol, depois Lua/Lua e depois Lua/Estrela/Estrela: cada erro apaga o ciclo. Resolva Lua/Estrela/Sol. Os monólitos ficam em Lua **(-2; 0)**, Estrela **(2; 3,5)** e Sol **(2; -1,5)**.
6. Saia e retorne à floresta: as luzes e a conclusão devem continuar. Teste P e confirme que ler o poema não gasta recursos nem altera o progresso.
7. Entre na floresta leste. Examine a pedra em **(2; 3)**, siga a pista e examine as raízes da árvore em **(4,5; 5)**, aproximando-se pelo oeste. Confira a escama em I → Itens; repetir a interação não duplica o objeto.
8. Antes de ter todos os objetivos, examine novamente a passagem: possuir somente a escama não basta. Com 3/3, as rochas continuam fechadas até usar E.
9. Use E na passagem, aguarde as rochas se afastarem e caminhe ao vulcão. Examine a cratera e retorne pelo mesmo caminho. Não deve existir batalha do dragão nesta entrega.
10. Entre/saia de florestas, lojas e periferia. Confira a passagem aberta, escama preservada, ouro/estoques/equipamentos/matérias/Limit mantidos e ausência de interações de mapas inativos. Em uma nova sessão de Play, o progresso deve voltar ao início.

## Código e validação

- `LegendPoem.cs`: versos e explicações compartilhados por camponesa, consulta e altar.
- `AncientSecrets.cs`: estado dos três objetivos, sequência, contador e condição da passagem.
- `DialogueSession.cs` / `DialogueController.cs`: páginas, escolhas, conclusão e cancelamento. Fechar uma escolha não executa sua resposta.
- `AncientSecretInteraction.cs`: camponesa, altar, monólitos, inscrição, marcas e árvore oca.
- `PrototypeSecretLayout.cs`: posições e áreas dos monólitos, compartilhadas com as verificações de caminhos.
- `PrototypeRuinsLayout.cs`: cenário local das ruínas, posição inicial, saída, altar, colunas e paredes. A demo agora tem nove mapas e 16 rotas direcionais.
- `BlockedPassageInteraction.cs`: abertura explícita da trilha e movimento das duas partes da barreira.

Passaram 159 verificações de segredos/escolhas, 58 de caminhos da periferia, 115 de ilha/interiores/viagens, 1.538 de câmera/malhas na versão com zoom aproximado da ilha, 1.518 de combate/inventário/lojas/progresso e 15 de exploração. Os scripts compilaram com as bibliotecas da Unity 6000.6.5f1.

Os testes conferem a lógica de cancelamento, respostas, repetição, ordens de descoberta, progresso e caminho aberto/fechado. Eles não executam cliques, teclado, física, animação ou renderização na Unity. Esses pontos precisam do roteiro de Play acima. Progresso continua restrito à sessão, sem salvamento em disco.
