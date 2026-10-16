# Conversas com retrato

Todos os diálogos abertos por `DialogueController` usam `DialoguePresentation`: retrato à esquerda, nome no alto da coluna direita e fala abaixo do nome. O fundo tem o mesmo degradê azul e a mesma moldura prateada da seleção de Load / New game. A interface exibe somente retrato, nome, fala e opções de resposta; não há instruções de teclas, contador de páginas nem box de interação.

Os retratos dos moradores e do tirano são recortes da parte superior de `NpcRoster`; o guardião da pedreira usa o lenhador. O dragão e sua essência usam a arte existente do dragão. Mensagens narrativas, objetos e a consulta do poema usam Aren como narrador. Os recortes são feitos por UV, sem editar ou esticar os PNGs.

A altura acompanha a fala e as escolhas. As opções ficam na coluna direita e quebram linha; quando o conteúdo excede a tela, a coluna ganha rolagem. A troca de fala retorna ao início. A altura não reserva espaço para dicas abaixo do texto. Páginas, escolhas pelo teclado ou clique, cancelamento e callbacks de batalha/salvamento mantêm o fluxo existente.

O box de proximidade, seus textos e a propriedade `Prompt` foram removidos. O alvo de interação é procurado somente ao pressionar E, preservando alcance, bloqueio por menus e execução da interação.

Validação: scripts compilados com as bibliotecas da Unity 6000.6.5f1; as 159 verificações de `Tools/Test-Secrets.ps1`, incluindo páginas, escolhas e cancelamento, passaram.

Foram geradas 18 capturas nativas de IMGUI em projeto temporário isolado: camponesa, hospedeira com três opções, tirano, dragão, escolhas do altar e guardião da pedreira, em 1280 × 720, 960 × 720 e 540 × 960. A conferência visual verificou a posição dos retratos, o nome acima da fala, a quebra de linha, a ausência de sobreposição e a remoção das ajudas abaixo das falas. A escala DPI e a orientação da superfície de captura foram compensadas na leitura das imagens. Essas prévias verificam a interface; não são capturas da sessão de jogo aberta do usuário.

![Prévia nativa do painel da hospedeira, sobre o fundo da hospedaria](DIALOGUE_PRESENTATION.preview.png)
