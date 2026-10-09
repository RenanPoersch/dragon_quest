# Parte 2 — interações e interior da loja

## Como testar

1. Saia de Play e aguarde a importação dos scripts.
2. Abra `Assets/Scenes/CidadePrototipo.unity`.
3. Clique em Play e depois dentro da janela Game.
4. Ande com WASD ou setas. A tecla E interage com o objeto próximo; a faixa inferior informa a ação disponível.

## Roteiro

- O aldeão está a sudoeste da fonte. Aproxime-se e pressione E. Uma conversa com três páginas deve abrir, e o herói deve parar.
- Use E, Espaço ou Enter para avançar. Esc fecha a conversa. Segurar E não deve pular páginas automaticamente.
- Fechar a conversa deve permitir andar novamente. A mesma tecla que fecha não pode reabrir o diálogo no mesmo frame.
- O baú está a sudeste da fonte. Pressione E perto dele para receber 25 moedas. O contador no cabeçalho deve mudar e a tampa deve abrir.
- Interaja novamente: o baú deve informar que está vazio, sem dar mais moedas.
- A loja de itens está a noroeste da praça. Aproxime-se da marca dourada diante da porta e pressione E.
- O cenário deve trocar para o interior, com piso, estantes, balcão e lojista. O herói deve aparecer no tapete, ao sul.
- Aproxime-se do balcão e converse com o lojista. Compras ainda não estão disponíveis.
- Vá até a marca dourada ao sul e pressione E para voltar à cidade, diante da mesma porta.
- O ouro e a aparência aberta do baú devem permanecer. Reabrir o baú depois de voltar da loja não pode dar outra recompensa.
- Entre e saia da loja várias vezes. Não deve haver duplicação de objetos ou diálogo com NPCs do ambiente desativado.
- Sair de Play e entrar novamente inicia uma nova sessão: ouro zero e baú fechado. Salvamento em disco será implementado depois.
- Confirmar ausência de erros vermelhos no Console.

## Código

- `ExplorationProgress`: ouro e IDs dos baús abertos, independente do cenário.
- `PlayerMovement`: pode suspender movimento durante diálogo e reposicionar o herói nas transições.
- `IInteractable` / `Interactable`: contrato compartilhado por portas, NPCs e baús.
- `PlayerInteraction`: objeto próximo, tecla E e controle do movimento.
- `DialogueController`: páginas, avanço e fechamento da conversa.
- `NpcInteraction`, `ChestInteraction`, `DoorInteraction`: comportamentos específicos.
- `PrototypeLocations`: alterna cidade/interior, mantém o herói e ajusta câmera e posição.
- `PrototypeCity` e `PrototypeHud`: objetos e interface provisórios.

## Teste automatizado do progresso

Na raiz do repositório, execute no PowerShell:

```powershell
./Tools/Test-ExplorationProgress.ps1
```

O script usa o SDK .NET fornecido com a Unity, sem instalar dependências. Verifica recompensa única, baús independentes, entradas inválidas, overflow de ouro e isolamento entre sessões. Não substitui a verificação interativa de portas, colisões e diálogos.

## Limites

Somente a loja de itens tem interior nesta entrega. Magias, armaduras, hospedaria e castelo ainda não têm interação de entrada. Os ambientes provisórios são alternados na mesma cena, preservando o estado em memória. Não há combate, catálogo de compras nem save em disco.
