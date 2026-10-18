# Suporte a controle

O jogo consulta `Gamepad.current` a cada leitura, sem exigir teclado conectado. Usa o Input System 1.19.0 já instalado, sem novas dependências. Consulte o mapeamento em README.md.

## Comportamento

- O analógico esquerdo preserva intensidade e a zona morta radial do Input System. O direcional também movimenta o herói. A entrada de maior intensidade entre teclado e controle prevalece; diagonais ficam limitadas a 1, inclusive nos ambientes oblíquos, frontais e laterais.
- Navegação digital com limiar de .55 e liberação em .35, resposta imediata, atraso inicial de .4 s e repetição a cada .12 s. Um quadro lento gera somente um passo, sem acumular repetições.
- Inventário e lojas preservam seus botões IMGUI, com foco sequencial, borda dourada, opções desabilitadas ignoradas e rolagem automática. A confirmação é processada uma vez por quadro; abrir o menu não confirma um botão. O mouse continua funcionando.
- A confirmação que fecha uma tela é consumida para impedir ações na tela seguinte. Os bloqueios de diálogo, combate, animações, menus, cheats e perda de foco continuam valendo.
- A introdução já aceitava o botão inferior; os menus respeitam o fade e a liberação desse botão antes de selecionar New game. A travessia e as cenas finais cinematográficas conservam seu tempo automático.
- O analógico direito permite rolar falas longas. R3 abre o Cheat Engine, inclusive durante combate, e o bloqueio impede ações no jogo abaixo dele.

## Inventário e voltar — 18 de outubro de 2026

Triângulo/Y abre o inventário; Start/Options também continua disponível. A consulta avulsa do poema foi removida. Círculo/B cancela a seleção atual ou volta uma tela: alvo de item, encaixe de matéria, lista, tela inicial do inventário e exploração. Config também volta à tela inicial do inventário. Nas lojas, volta de quantidade para produtos, depois ações e então fecha, sem comprar ou vender. Diálogos e alvos de batalha usam o mesmo cancelar, sem executar a escolha ou gastar o turno.

Os testes de integração verificam esses retornos, incluindo a abertura com a lenda já aprendida e a prioridade de cancelar sobre confirmar em diálogos.

## Testes reproduzíveis

```powershell
./Tools/Test-ControllerInput.ps1
./Tools/Test-ControllerInput.ps1 -UnityIntegration
./Tools/Test-UnityScripts.ps1
```

O primeiro comando verifica repetição, zona de navegação, direção, reversão, opções desabilitadas, troca de listas, foco vazio e inventários longos. O segundo também compila todos os scripts e executa controles virtuais no Input System real da Unity, em um projeto temporário separado, sem abrir ou alterar os saves reais. O processo batch não possui foco de janela: o teste injeta foco no ponto central de leitura e também simula sua perda.

Os testes de integração cobrem movimento analógico/direcional sem teclado, diagonais, zona morta, botões, consumo de comandos, movimento em ambientes frontais/laterais, bloqueio de movimento, revelação e avanço do diálogo, escolhas, abertura/fechamento de inventário e loja, desconexão, reconexão e regressão do teclado. Os testes de lógica não verificam a apresentação visual dos menus.

## Resultado registrado em 17 de outubro de 2026

- Todos os scripts do jogo compilaram com as bibliotecas da Unity 6000.6.5f1.
- 280 verificações de navegação e foco passaram.
- 32 verificações de integração passaram com controle virtual no Input System real, incluindo perda de foco, ausência de teclado, diálogos, inventário, loja e reconexão.
- A apresentação visual e o uso com um dispositivo físico ainda precisam ser conferidos pelo roteiro abaixo.

Referência de compatibilidade e zona morta: [Unity Input System 1.19 — Gamepad support](https://docs.unity.cn/Packages/com.unity.inputsystem%401.19/manual/Gamepad.html).

## Verificação com dispositivo físico

1. Na cena CidadePrototipo, conectar o controle antes e depois de Play. Pular a introdução com o botão inferior, soltar e iniciar New game. Conferir que o mesmo toque não inicia o jogo antes do fim da abertura.
2. Andar devagar e rápido pelo mapa global, periferia, lojas e ruínas. Testar diagonais, colisões, paradas e perda de foco da janela Game.
3. Conversar com um NPC: primeiro toque revela a fala, novo toque avança. Navegar por uma escolha e confirmar. Fechar com o botão direito. Rolar uma fala longa com o analógico direito.
4. Abrir o inventário, trocar personagem/aba, usar item, equipar/remover equipamento, selecionar matéria e encaixe. Percorrer uma lista maior que a área visível e conferir a rolagem.
5. Em uma loja, percorrer Comprar/Vender, missão, produtos, quantidade, máximo, confirmação e cancelamento. Conferir ouro e estoque depois da transação. Botão direito fecha sem acionar o mundo.
6. Em combate, escolher ataque, magia, summon e item, navegar entre alvos e voltar com o botão direito. Confirmar resultados depois da animação. Testar R3 e confirmar que o combate fica bloqueado pelo Cheat Engine.
7. Salvar com View/Back/Share no mapa global ou hospedaria. Navegar pelos slots e confirmar ou cancelar a sobrescrita. Em Continue, slots indisponíveis devem ser pulados.
8. Desconectar enquanto anda, reconectar e continuar. Alternar teclado/mouse e controle na mesma sessão. Verificar retorno ao menu depois do final.

Compilação e controle virtual não confirmam compatibilidade Bluetooth/USB de cada modelo ou driver, nem substituem esse roteiro com um dispositivo físico.
