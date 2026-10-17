# Tela de inventario

Abra com **I**, X/Quadrado ou Start/Options durante a exploracao. O menu suspende movimento, viagens e encontros, usando o bloqueio ja existente no controlador de batalha.

A tela principal segue a referencia fornecida: fundo preto, paineis com degrade azul e bordas prateadas, retratos dos tres aliados, nivel, HP/MP, progresso ate o proximo nivel e Limit. O menu lateral tem somente **Item, Magic, Equip, Status e Config**. As caixas inferiores mostram o tempo real da jornada, ouro e localizacao atual. O tempo acompanha o mesmo contador que e salvo e restaurado nos slots.

- **Item:** estoque compartilhado, selecao de aliado e uso dos consumiveis existentes.
- **Magic:** materias, encaixes, pares e previa dos comandos de combate.
- **Equip:** troca e remocao de armas/armaduras e comparacao dos atributos.
- **Status:** atributos, experiencia, equipamento, afinidades e condicao do aliado selecionado.
- **Config:** volume e tela cheia; ajustes validos nesta sessao.

Na tela principal, use cima/baixo ou W/S e Enter/E/Espaco, mouse ou controle. Os submenus conservam os botoes, listas rolaveis e suporte a controle da interface existente. **Voltar** retorna ao resumo da equipe. **I/Esc** ou B/Circulo fecha o inventario. Reabrir sempre mostra o resumo com Item selecionado. Perder o foco ou abrir o Cheat Engine bloqueia as acoes da interface.

## Validacao desta entrega

- Compilacao de todos os scripts com as bibliotecas da Unity 6000.6.5f1: passou.
- `Tools/Test-MateriaRevision.ps1`: passou.
- `EquipmentItemChecks.RunItemChecks()`: 49 verificacoes passaram.
- Tela principal conferida no Game view com uma jornada carregada: tres personagens, localizacao e ouro reais, painel lateral e cursor de luva.
- A automacao visual foi interrompida pelo usuario com Esc; a ultima revisao de cores sem foco e preenchimento das barras foi compilada, sem nova captura automatizada.
- A suite geral `Tools/Test-Combat.ps1` falhou em `AbilityChecks.CheckEncounterStrategies` com habilidade indisponivel. `EquipmentItemChecks.Run()` tambem falhou ao verificar a restauracao de um comando por materia. Essas rotinas compilam apenas dominio/fabricas/testes, sem nenhum arquivo modificado nesta entrega; as falhas de habilidades permanecem fora do escopo do inventario.

Conferencia manual adicional: abrir cada submenu, usar um item em aliado ferido, trocar equipamento e materias, conferir Status, ajustar Config e voltar; repetir com controle e em janelas 16:9 e 4:3. Evitar salvar a jornada apenas para testar a interface.
