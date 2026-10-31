> Registro histórico de implementação. As regras e instruções vigentes estão em [Docs/Current](../../Current/README.md).

# Parte 6C — recompensas de batalhas e baús

O ouro das lojas vem de batalhas e baús. A missão entre comerciantes foi removida por solicitação do usuário em 18 de outubro de 2026, junto das ações, recompensa, reposição de estoque e estado de missão nos saves.

## Regras atuais

- A vitória concede a soma do ouro definido em cada inimigo derrotado. A patrulha rende 35 moedas: 25 do guarda e 10 do soldado.
- Cada batalha paga uma vez; novas vitórias podem pagar novamente. Derrotas não concedem ouro.
- O baú da praça rende 25 moedas uma vez.
- As lojas oferecem somente Comprar, Vender e Sair. Seus estoques permanecem consumidos após sair, retornar ou vencer batalhas.
- O jogo começa com 50 moedas, duas poções e um antídoto. Uma vitória e o baú totalizam 110 moedas. Duas vitórias e o baú totalizam 145, suficientes para Fogo (50) e All (90), com cinco moedas restantes.
- Saves antigos continuam carregando ouro, inventário e estoques. Os campos da missão descontinuada são ignorados; novos saves não os gravam.

## Validação

Use `Tools/Test-ShopMenu.ps1`, `Tools/Test-Saves.ps1`, `Tools/Test-Accessories.ps1`, `Tools/Test-EquipmentMenu.ps1` e `Tools/Test-UnityScripts.ps1`.

As verificações cobrem compra e venda, quantidades e limites, proteção de objetos equipados, previews por classe, estoques salvos e leitura de saves antigos sem reativar a missão ou conceder recompensas.
