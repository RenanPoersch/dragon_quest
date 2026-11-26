using DragonQuest.Content;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Progression
{
    // Guarda o progresso do dragão e as regras da recompensa na instância compartilhada pelo bootstrap.
    public sealed class DragonProgress
    {
        // Identificador constante usado para reconhecer o dragão entre os combatentes.
        public const string EnemyId = "ancient-dragon";
        // Definição da armadura entregue como recompensa pela vitória contra o dragão.
        // static compartilha a referência entre as instâncias; readonly impede sua reatribuição após a inicialização.
        public static readonly EquipmentDefinition Armor = EquipmentCatalog.DragonArmor;
        // Indica se a conclusão foi registrada; outros sistemas podem consultar, mas só esta classe pode alterar.
        public bool IsDefeated { get; private set; }
        // Restaura o estado salvo sem tentar entregar a recompensa novamente.
        internal void Restore(bool defeated) => IsDefeated = defeated;

        // Recebe a batalha e o grupo; retorna true somente ao entregar a recompensa e registrar a conclusão.
        public bool TryComplete(BattleSession battle, PartyProgress party)
        {
            // Recusa conclusões repetidas, referências ausentes e batalhas que não terminaram em vitória.
            if (IsDefeated || battle == null || party == null || battle.Outcome != BattleOutcome.Victory) return false;
            // enemies conta os adversários encontrados na batalha.
            // dragon registra se o último inimigo examinado é o dragão e está morto; abaixo também se exige um único inimigo.
            int enemies = 0; bool dragon = false;
            // participant representa cada combatente; apenas os integrantes do time inimigo entram na verificação.
            foreach (CombatantState participant in battle.Combatants)
                if (participant.Team == CombatTeam.Enemy)
                { enemies++; dragon = participant.Id == EnemyId && !participant.IsAlive; }
            // Exige exatamente um inimigo: o dragão derrotado. Só então tenta adicionar a armadura ao inventário.
            // O ID fixo dragon-armor identifica essa cópia; TryAddEquipments rejeita IDs duplicados.
            if (enemies != 1 || !dragon || !party.TryAddEquipments(new EquipmentLoadout(Armor, "dragon-armor"))) return false;
            // Só registra a conclusão após confirmar que a armadura foi adicionada ao grupo.
            IsDefeated = true;
            return true;
        }

        // Consulta se, após registrar a derrota do dragão, algum personagem está usando a armadura da recompensa.
        public bool HasEquippedArmor(PartyProgress party)
        {
            // A consulta exige que a conclusão já esteja registrada e que exista um grupo válido.
            if (!IsDefeated || party == null) return false;
            // member representa cada personagem do grupo, cuja armadura equipada será examinada.
            foreach (PartyMember member in party.Members)
                // O operador ?. permite consultar a definição com segurança quando o personagem está sem armadura.
                if (member.Armor?.Definition == Armor) return true;
            // Nenhum personagem do grupo está usando uma armadura com essa definição.
            return false;
        }
    }
}
