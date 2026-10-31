# Persistência

SaveController coordena o menu, slot ativo, sobrescrita e tempo de jogo. SaveSlotSession expõe snapshots somente leitura de três SaveSlotState; a apresentação recebe os estados e não o serviço de escrita.

JourneySnapshot.Capture converte a jornada em JourneyData. SaveCatalog resolve os IDs de definições. SaveStore serializa com DataContractJsonSerializer em Application.persistentDataPath/Saves/slot-N.json, com temporário e backup da revisão anterior válida quando aplicável.

O estado contém XP/vitais, equipamentos e encaixes, matérias, consumíveis, estoques, ouro, baús, guardas, presente do viajante, segredos, dragão, mapa/posição, tempo e final. GameObjects, cenas carregadas e texturas não são serializados.

Na leitura, a versão e os dados são validados e uma RestoredJourney independente é reconstruída. Um principal indisponível pode usar o backup; uma versão incompatível não é substituída silenciosamente. JourneyInstaller instala equipe, progresso e lojas, restaura segredos e dragão e reaplica mapa/posição.

Os IDs persistidos e o formato do save foram mantidos na reorganização. Renomeações de classes, namespaces, pastas e cena não mudam IDs como aren, potion ou dragon-armor. Os tipos serializados continuam em DragonQuest.Persistence e preservados por link.xml na assembly Assembly-CSharp.

As conquistas de finais usam um perfil independente em Application.persistentDataPath/Achievements/endings.json. Elas não pertencem a um slot individual.
