# Correção dos sopros de Bahamut e Fênix

As folhas originais cortavam a ponta esquerda do sopro no quinto quadro. A separação automática também podia dividir efeitos que invadiam o quadro seguinte.

BahamutAttack.png e PhoenixAttack.png contêm o summon e o sopro completos em uma imagem transparente independente. A animação substitui somente o quinto quadro por essa imagem. Os outros quadros, a duração e os efeitos sobre os alvos permanecem iguais. O leitor ajusta a escala da imagem nova à altura original e preserva a posição dos pés.

Validação do canal alpha (pixels com alpha >= 128):

| Imagem | Esquerda | Direita | Topo | Base |
|---|---:|---:|---:|---:|
| BahamutAttack | 23 px | 17 px | 165 px | 107 px |
| PhoenixAttack | 62 px | 62 px | 21 px | 28 px |

Compilação completa e testes de summons, All, cast, Infusão e seleção dos novos quadros passaram. Foi feita inspeção dos sprites completos; não foi realizada uma partida na Unity.

Gerado com image_gen integrada. Os prompts finais estão em PROMPTS.md. As tentativas de novas folhas na raiz desta pasta foram descartadas; somente os dois PNGs em Assets/Resources/PixelArt/Summons são instalados.
