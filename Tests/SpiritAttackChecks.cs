using DragonQuest.Presentation.Battle.Animations;
using DragonQuest.Content;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Inventory;

internal static class SpiritAttackChecks
{
    private static int checks;
    private static readonly AbilityDefinition Attack = AbilityCatalog.Attack, Defend = AbilityCatalog.Defend;
    private static readonly AbilityDefinition Mind = AbilityCatalog.MindControl, Silence = AbilityCatalog.Silence;
    private static readonly AbilityDefinition Spell = new AbilityDefinition("spell", "Magia", "", 3, 4,
        AbilityEffect.MagicDamage, TargetRule.Enemy, menuGroup: AbilityMenuGroup.Magic);
    private static readonly AbilityDefinition Summon = new AbilityDefinition("summon", "Invocacao", "", 4, 5,
        AbilityEffect.MagicDamage, TargetRule.AllEnemies, menuGroup: AbilityMenuGroup.Summon);
    private static readonly AbilityDefinition FreeSpell = new AbilityDefinition("free-spell", "Magia gratis", "", 0, 2,
        AbilityEffect.MagicDamage, TargetRule.Enemy);
    private static readonly AbilityDefinition Physical = new AbilityDefinition("skill", "Golpe forte", "", 2, 4,
        AbilityEffect.PhysicalDamage, TargetRule.Enemy);
    private static void Check(bool value, string text) { checks++; if (!value) throw new Exception(text); }
    private static CombatantState Hero(string id, int speed = 10) => new CombatantState(id,
        new CombatantDefinition(id, "Heroi", 100, 30, 20, 2, 15, speed, Attack, Defend, Spell, Summon,
            FreeSpell, Physical, AbilityCatalog.Heal), CombatTeam.Party);
    private static CombatantState Spirit(params AbilityDefinition[] abilities) => new CombatantState("evil-spirit",
        new CombatantDefinition("Espirito", "Inimigo", 300, 0, 0, 3, 30, 20, abilities), CombatTeam.Enemy);

    private static void CheckMindControl()
    {
        foreach (double roll in new[] { 0d, .499d, .5d, .999999d })
        {
            var hero = Hero("hero"); var ally = Hero("ally", 5); var spirit = Spirit(Mind, Defend);
            var fallen = Hero("fallen", 0); fallen.TakeDamage(100);
            var battle = new BattleSession(hero, ally, spirit, fallen);
            Check(battle.TryAct(spirit, Mind, hero, out _) && hero.MindControlTurns == 1, "Aplica controle por uma acao do alvo.");
            Check(CombatStatuses.Active(hero).Any(s => s.Status == CombatStatus.MindControlled && s.Turns == 1), "Confusao aparece no painel.");
            int mp = hero.Mp, turns = hero.TurnsTaken;
            Check(!battle.TryAct(hero, Defend, hero, out _) && hero.TurnsTaken == turns, "Nao permite escolher outra acao nem gasta o turno na recusa.");
            Check(battle.TryTakeMindControlTurn(roll, out var target, out var message), "Executa ataque automaticamente.");
            Check(target == (roll < .5 ? ally : spirit), "Escolhe uniformemente entre aliados e inimigos vivos, excluindo usuario e mortos.");
            Check(target.Hp < target.Definition.MaxHp && hero.Hp == 100 && fallen.Hp == 0, "Somente o alvo vivo sorteado recebe dano.");
            Check(hero.Mp == mp && hero.MindControlTurns == 0 && hero.TurnsTaken == turns + 1 && battle.CurrentActor == ally, "Ataque forcado gasta uma acao, sem MP.");
            Check(message.Contains("confuso") && !CombatStatuses.Active(hero).Any(s => s.Status == CombatStatus.MindControlled), "Feedback identifica a confusao e o status expira.");
            Check(!battle.TryTakeMindControlTurn(roll, out _, out _), "Uma aplicacao nao obriga duas acoes.");
        }
        {
            var hero = Hero("hero"); var ally = Hero("ally", 5); var protector = Hero("protector", 0);
            var spirit = Spirit(Mind);
            var battle = new BattleSession(hero, ally, protector, spirit);
            battle.TryAct(spirit, Mind, hero, out _); ally.ProtectedBy = protector;
            Check(battle.TryTakeMindControlTurn(0, out var actual, out _) && actual == protector
                && protector.Hp < 100 && ally.Hp == 100 && ally.ProtectedBy == null, "Proteger intercepta e e consumido por ataque confuso.");
        }
    }

    private static void CheckSilence()
    {
        var hero = Hero("hero"); var spirit = Spirit(Silence, Defend);
        var inventory = new ItemInventory();
        inventory.TryAdd(ItemCatalog.Potion, 1, out _);
        var battle = new BattleSession(inventory, hero, spirit);
        Check(battle.TryAct(spirit, Silence, hero, out _) && hero.SilenceTurns == 1, "Silence dura ate a acao do alvo, inclusive quando ele age em seguida.");
        Check(CombatStatuses.Active(hero).Any(s => s.Status == CombatStatus.Silenced && s.Turns == 1), "Silencio aparece no painel.");
        int mp = hero.Mp, turns = hero.TurnsTaken;
        hero.Limit.AddDamage(100,100); int limit = hero.Limit.Charge;
        foreach (var magic in new[] { Spell, Summon, FreeSpell, AbilityCatalog.Heal })
            Check(!battle.TryAct(hero, magic, magic == AbilityCatalog.Heal ? hero : spirit, out var message)
                && message.Contains("Silencio") && hero.Mp == mp && hero.TurnsTaken == turns && hero.SilenceTurns == 1
                && hero.Limit.Charge == limit, "Silencio bloqueia magia, invocacao, magia gratis e cura sem gastar recursos.");
        Check(battle.TryAct(hero, Physical, spirit, out _) && hero.SilenceTurns == 0 && hero.Mp == mp - 2,
            "Habilidade fisica continua disponivel mesmo com custo de MP e encerra o silencio.");
        battle.TryAct(spirit, Defend, spirit, out _);
        Check(battle.TryAct(hero, Spell, spirit, out _), "Magia volta no proximo turno.");
        battle.TryAct(spirit, Silence, hero, out _);
        Check(battle.TryAct(hero, Defend, hero, out _) && !hero.IsSilenced, "Defender e permitido e consome a duracao.");
        battle.TryAct(spirit, Silence, hero, out _); hero.TakeDamage(20);
        Check(battle.TryUseItem(hero, ItemCatalog.Potion, hero, out _) && !hero.IsSilenced,
            "Itens sao permitidos durante Silence e consomem a duracao.");

        hero = Hero("hero"); var other = Hero("other", 15); spirit = Spirit(Silence, Defend);
        battle = new BattleSession(hero, other, spirit);
        battle.TryAct(spirit, Silence, hero, out _);
        Check(battle.CurrentActor == other && hero.SilenceTurns == 1, "Silencio nao depende de rodada global.");
        battle.TryAct(other, Defend, other, out _);
        Check(hero.SilenceTurns == 1 && battle.CurrentActor == hero, "Acoes de terceiros nao reduzem silencio.");
    }

    private static void CheckCleanupAndDrain()
    {
        var hero = Hero("hero"); hero.MindControlTurns = hero.SilenceTurns = 1;
        hero.ClearHarmfulConditions();
        Check(hero.MindControlTurns == 0 && !hero.IsSilenced, "Purificacao total remove ambos os estados.");
        hero.MindControlTurns = hero.SilenceTurns = 1; hero.TakeDamage(100);
        Check(hero.MindControlTurns == 0 && !hero.IsSilenced, "Derrota remove ambos os estados.");
        hero.RecoverHp(10);
        Check(!CombatStatuses.Active(hero).Any(s => s.Status == CombatStatus.MindControlled || s.Status == CombatStatus.Silenced), "Reviver nao reaplica estados antigos.");
        hero = Hero("hero"); hero.TakeDamage(90); hero.GrantDamageShield(2,false);
        var spirit = Spirit(AbilityCatalog.DrainLife); spirit.TakeDamage(290);
        var battle = new BattleSession(hero, spirit);
        Check(EnemyAI.TryTakeTurn(battle, out _) && hero.Hp == 0 && spirit.Hp == 20,
            "Sugar vida recupera apenas HP perdido, respeitando Shield e limite do golpe fatal.");
        hero = Hero("hero"); spirit = Spirit(AbilityCatalog.DrainLife); spirit.TakeDamage(1);
        battle = new BattleSession(hero,spirit); EnemyAI.TryTakeTurn(battle,out _);
        Check(spirit.Hp == spirit.Definition.MaxHp, "Drenagem nao ultrapassa HP maximo do espirito.");
    }

    private static void CheckEnemyCycle()
    {
        var hero = Hero("hero"); var spirit = Spirit(Mind, Silence, AbilityCatalog.DrainLife, Defend);
        var battle = new BattleSession(hero, spirit);
        Check(EnemyAI.ChooseAbility(battle) == Mind && EnemyAI.TryTakeTurn(battle, out _), "IA seleciona mind control.");
        battle.TryTakeMindControlTurn(0,out _,out _);
        Check(EnemyAI.ChooseAbility(battle) == Silence && EnemyAI.TryTakeTurn(battle,out _), "IA seleciona silencio.");
        battle.TryAct(hero,Defend,hero,out _);
        Check(EnemyAI.ChooseAbility(battle) == AbilityCatalog.DrainLife, "IA inclui drenagem no ciclo.");
        foreach (var ability in new[] { Mind, Silence, AbilityCatalog.DrainLife,
            AbilityCatalog.Chains, AbilityCatalog.ChaosMagic })
            Check(BattleAnimationCatalog.Resource("evil-spirit",ability) == SpiritAttackTimeline.Resource(ability), "Cada ataque seleciona seu atlas independente.");
        Check(BattleAnimationCatalog.Idle("evil-spirit") == "SpiritBattle"
            && BattleAnimationCatalog.ImpactDelay("evil-spirit",Mind) == SpiritAttackTimeline.Impact(Mind), "Sprite idle e sincronismo do impacto ficam definidos.");
    }

    private static void CheckPresentation()
    {
        var abilities = new[] { Mind, Silence, AbilityCatalog.DrainLife,
            AbilityCatalog.Chains, AbilityCatalog.ChaosMagic };
        Check(abilities.Select(SpiritAttackTimeline.Resource).Distinct().Count() == 5, "Habilidades nao compartilham sprites recoloridos.");
        Check(abilities.Select(SpiritAttackTimeline.Duration).Distinct().Count() == 5, "Cada ritual tem seu proprio ritmo.");
        foreach(var ability in abilities)
        {
            float impact=SpiritAttackTimeline.Impact(ability), end=SpiritAttackTimeline.Duration(ability);
            Check(impact>SpiritAttackTimeline.EffectStart(ability)+.5f && end>impact+.7f,
                "Antecipacao, impacto e recuperacao permanecem visiveis.");
            Check(SpiritAttackTimeline.Opacity(ability,0)==0 && SpiritAttackTimeline.Opacity(ability,end)==0,
                "Efeito nao vaza antes nem depois da acao.");
            Check(SpiritAttackTimeline.Shake(ability,impact-.01f)==0 && SpiritAttackTimeline.Shake(ability,end)==0,
                "Tremor acompanha apenas o impacto.");
            Check(SpiritAttackTimeline.ActorFrame(ability,0)==0 && SpiritAttackTimeline.ActorFrame(ability,end)==3,
                "Poses percorrem preparacao e recuperacao.");
            int previous=-1;
            for(int step=0;step<=100;step++)
            {
                int frame=SpiritAttackTimeline.EffectFrame(ability,end*step/100);
                if(frame<previous||frame<0||frame>7) throw new Exception("Sequencia do efeito saltou para tras ou escapou do atlas.");
                previous=frame;
            }
            Check(previous==7,"O ritual termina no ultimo sprite, sem repetir o impacto.");
        }
        Check(SpiritAttackTimeline.Impact(AbilityCatalog.DrainLife)>2,
            "Cura de Sugar Vida espera as almas chegarem ao espirito.");
    }

    public static void Main()
    {
        CheckMindControl(); CheckSilence(); CheckCleanupAndDrain(); CheckEnemyCycle(); CheckPresentation();
        Console.WriteLine(checks + " verificacoes de mind control, silencio, drenagem e sprites passaram.");
    }
}
