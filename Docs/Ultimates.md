# Ultimates dos heróis

O comando **LIMIT** continua substituindo Ataque quando a barra chega a 100%. Cada herói tem uma habilidade própria, independente das matérias. Todas consomem uma barra completa e uma ação, sem MP. O cheat de LIMIT infinito continua funcionando.

| Herói | Ultimate | Efeito |
| --- | --- | --- |
| Aren | Omnislash | 12 cortes e uma finalização com dano triplo, exclusivamente no inimigo escolhido. Cada corte calcula defesa, crítico e Shield e produz seu próprio número flutuante. |
| Lia | Aurora da Vida | Recupera 80% do HP máximo de cada herói, limitado ao HP máximo. Revive os derrotados com 80%. Remove veneno, correntes e provocação, preservando proteções benéficas. |
| Bram | Bastião Inabalável | Ataque físico de escudo em todos os inimigos vivos, poder 65. Concede Shield de 50% por três ações futuras de cada aliado vivo, incluindo Bram. Não acumula redução com outro Shield. |

Percentuais de recuperação e redução de dano arredondam para cima. A ultimate de Lia aceita a equipe com HP cheio para permitir purificação.

## Apresentação

- Aren: o cenário permanece visível. Ele desaparece e reaparece nos dois lados do mesmo alvo, com pós-imagens, rastros e faíscas. Depois salta alto com a espada recuada, fica suspenso por 0,54 segundo e mergulha para o impacto final, com onda de choque e tremor da arena. A cena completa dura 5,55 segundos.
- Lia: levitação, círculos de luz verde e dourada sob os aliados, pilares, partículas ascendentes e halos. A recuperação ocorre visualmente em 2,35 segundos, com identificação de quem reviveu. Duração total de 4,1 segundos.
- Bram: brasões azuis cercam os aliados antes da investida. Ele avança com o escudo e produz impactos e ondas de choque na formação inimiga em 2,25 segundos. Duração total de 3,85 segundos.

Uma proteção que intercepta Omnislash recebe a sequência inteira. Se o alvo morre antes do último corte, os golpes restantes não transferem dano a outro inimigo; o alvo permanece visível até a finalização. Vitória e próxima ação aguardam a apresentação terminar.

## Prévia e validação

Na Unity, use **Tools → Dragon Quest → Preview Ultimates** para escolher o herói, reproduzir ou percorrer a animação pela barra de tempo. **Validate Ultimates** verifica os sprites, o desaparecimento, a pose de espada e a finalização aérea.

`Tools/Test-Ultimates.ps1` executa os testes de efeitos, custos, alvos, proteção, dano individual, críticos e cronologia sem iniciar outra Unity. `Tools/Test-UnityScripts.ps1` compila os scripts com as bibliotecas do editor instalado.
