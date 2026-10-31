> Registro histórico de implementação. As regras e instruções vigentes estão em [Docs/Current](../../Current/README.md).

# Parte 6B — três lojas, compras e vendas

As lojas de itens (noroeste), matérias (nordeste) e equipamentos (sudeste) têm interiores. Caminhe até a marca diante da porta para entrar automaticamente; aproxime-se do vendedor e use E. Esc ou Fechar encerra o menu; caminhar até a saída ao sul retorna à porta da loja correspondente.

Esta página registra a entrega 6B. Use `PARTE_6C.md` para validar a versão atual, com recompensas de batalhas e baús.

## Economia inicial

- A equipe inicia com 50 moedas, 2 poções e 1 antídoto. Aren tem Força/Raio, Lia tem Cura/Purificação e Bram tem Guardião, com armas e armaduras simples. Todas as matérias continuam transferíveis entre classes.
- Catálogos, preços e quantidades seguem `PLANO_LOJAS.md`. Todos os produtos têm estoque limitado.
- Comprar adiciona objetos ao inventário. Armas/armaduras chegam com encaixes vazios; cada cópia de equipamento ou matéria tem ID próprio. Não há equipamento automático.
- Vender rende 50% do preço por unidade, arredondado para baixo. Um éter custa 25 e é vendido por 12; duas unidades rendem 24.
- Cada loja compra sua categoria. Objetos em uso ficam bloqueados; desequipe no inventário antes. A retirada de equipamento devolve todas as matérias, conforme a regra da parte 5B.
- A arma inicial pode ser vendida por 15 moedas e a armadura inicial por 20. Elas não fazem parte do estoque de compra.
- Objetos de história ou sem preço comercial cadastrado ficam bloqueados. Objetos vendidos saem do inventário e não criam estoque de recompra.
- Abrir/fechar o menu, cancelar uma seleção, entrar/sair da loja ou iniciar batalha não repõe estoque.
- O menu mostra quantidade disponível, preço unitário, total e motivo de bloqueio. Equipamentos mostram atributos base e pares ligados; a afinidade da classe entra ao equipar.
- Ouro, estoque e inventário persistem na mesma sessão de Play. Sair de Play inicia outra sessão; ainda não há save em disco.

## Roteiro em Play

1. Abra `Game.unity` com Unity 6000.6.5f1, aguarde a compilação e confira o Console.
2. Entre em Play: o HUD deve mostrar 50 moedas. I abre o inventário com apenas o kit descrito acima.
3. Vá à loja de itens a noroeste. E entra; E perto do vendedor abre Comprar/Vender. Andar e abrir o inventário devem ficar bloqueados enquanto o menu da loja está aberto.
4. Compre 2 poções: estoque do vendedor passa de 12 para 10, seu estoque passa de 2 para 4 e o ouro passa de 50 para 30.
5. Venda 1 poção: seu estoque passa para 3 e o ouro para 35; o estoque da loja permanece 10.
6. Selecione 2 éteres (50 moedas): o botão de compra deve ficar bloqueado com Ouro insuficiente. Cancelar deve preservar todos os recursos.
7. Feche, saia e entre novamente. Ouro e estoque devem permanecer. Esc só fecha o menu da loja, sem abrir inventário.
8. Em outra sessão de Play, abra o baú de 25 moedas e vá à loja de equipamentos a sudeste: haverá 75 moedas. Compre a espada de ferro por 60; restam 15 e o estoque da espada passa a 1.
9. Feche o menu e pressione I. A espada deve estar no inventário, sem estar equipada. Equipe em Aren: ataque passa a 32, Força volta ao inventário e Raio permanece na armadura.
10. Reabra o vendedor e escolha Vender. A espada equipada deve estar bloqueada; a arma inicial agora deve ser vendável por 15, sem levar embora Força.
11. Em outra sessão de Play, visite a loja de matérias a nordeste. Compre Fogo por 50 e confirme sua presença no inventário. Equipe e confira o comando Fogo com 6 MP; a classe não restringe o uso.
12. Confira entradas/saídas das três lojas, diálogo com aldeão, baú único e batalha. O retorno de cada loja deve colocar o herói diante da porta correta.

Os testes de combos das partes anteriores usam uma equipe de teste separada. O jogo usa o kit econômico. A parte 6C adiciona mais fontes de ouro, uma missão e a reposição de consumíveis por progresso.

## Código e validação

- `Assets/Scripts/Shops/ShopInventory.cs`: produtos, categorias, estoques e objetos da lista de venda.
- `ShopService.cs`: transações validadas e reconsulta de propriedade antes de vender.
- `Exploration/ExplorationProgress.cs`: saldo, crédito e débito de ouro.
- `Equipment/PartyProgress.cs` e `Inventory/ItemInventory.cs`: inclusão/remoção de objetos possuídos.
- `ShopCatalog.cs`: preços, estoques e 50 moedas iniciais.
- `ShopController.cs` / `ShopHud.cs`: menu, seleção de quantidade e bloqueio de exploração.
- `MerchantInteraction.cs`, `WorldLocationController.cs` e `GameEntryPoint.cs`: vendedores, interiores e retornos.
- `Tests/ShopChecks.cs`: compras, vendas, identidades, kit inicial, limites, objetos protegidos e isolamento de sessões.

Passaram 1.415 verificações de combate/inventário/lojas e 15 de exploração. Todos os scripts compilaram com bibliotecas da Unity 6000.6.5f1. Testes reproduzíveis: `Tools/Test-Combat.ps1`, `Tools/Test-ExplorationProgress.ps1` e `Tools/Test-UnityScripts.ps1`. A compilação isolada não confirma cliques, posicionamento ou aparência; a verificação em Play continua necessária.
