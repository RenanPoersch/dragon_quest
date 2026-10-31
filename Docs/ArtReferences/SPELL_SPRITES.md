> Referência de autoria e produção; pode registrar uma versão visual substituída. O funcionamento vigente está em [Docs/Current](../Current/README.md).

# Sprites de magias

Oito folhas PNG transparentes, com oito quadros por folha (quatro colunas e duas linhas):

| Arquivo | Magia |
|---|---|
| FireSpell.png | Fogo |
| IceSpell.png | Gelo |
| LightningSpell.png | Raio / Thunder |
| EarthSpell.png | Terra |
| ChaosSpell.png | Caos |
| HealSpell.png | Curar |
| CleanseSpell.png | Purificar |
| ReviveSpell.png | Reviver |

Geradas pela ferramenta integrada image_gen. Os prompts completos estão em PROMPTS.md.
As folhas têm 1254 × 1254 pixels e canal alpha. Os arquivos originais foram preservados.

Integração: Resources/PixelArt/Spells. A classe SpellAnimation lê os quadros com ajuste para os espaçamentos irregulares, filtro Point, sem mipmaps nem compressão. Cada animação dura 1 segundo, com impacto aos 0,5 segundos. All exibe o efeito em cada alvo válido; Infusão usa o efeito do elemento do ataque básico. Purificar anima mesmo sem alteração de HP.

Verificação: compilação de todos os scripts com bibliotecas da Unity; testes de identificação da magia, All, Infusão e temporização; inspeção visual das oito folhas e verificação do canal alpha. Não foi realizada uma sessão jogável na Unity.

## Validação local

Execute Tools/Test-SpellScripts.ps1 com WorkDirectory apontando para validation, depois Tools/Test-SpellAnimation.ps1. A pasta backup contém os arquivos anteriores à instalação.
