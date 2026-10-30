using DragonQuest.Equipment;

namespace DragonQuest.Content
{
    public static class MateriaCatalog
    {
        public static readonly MateriaDefinition ThunderMateria = new MateriaDefinition("thunder", "Raio", "Verde: concede Thunder.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Thunder });
        public static readonly MateriaDefinition FireMateria = new MateriaDefinition("fire", "Fogo", "Verde: magia de Fogo, poder 22, 6 MP. Fraqueza x1,5; resistencia x0,5.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Fire });
        public static readonly MateriaDefinition IceMateria = new MateriaDefinition("ice", "Gelo", "Verde: magia de Gelo, poder 22, 6 MP. Fraqueza x1,5; resistencia x0,5.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Ice });
        public static readonly MateriaDefinition CureMateria = new MateriaDefinition("cure", "Cura", "Verde: concede Curar.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Heal });
        public static readonly MateriaDefinition CleanseMateria = new MateriaDefinition("cleanse", "Purificacao", "Verde: remove veneno.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Cleanse });
        public static readonly MateriaDefinition ReviveMateria = new MateriaDefinition("revive", "Vida", "Verde: concede Reviver.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Revive });
        public static readonly MateriaDefinition SummonMateria = new MateriaDefinition("phoenix", "Fenix", "Vermelha: revive e cura 80% do HP maximo de toda a equipe; dano baixo de Fogo em todos os inimigos. 12 MP.", MateriaType.Summon, abilities: new[] { AbilityCatalog.Summon });
        public static readonly MateriaDefinition AllMateria = new MateriaDefinition("all", "All", "Azul: ligada a verde/vermelha, aplica o comando a todos os alvos validos; MP x1,5. Sozinha nao concede comando.", MateriaType.Support, MateriaModifier.All);
        public static readonly MateriaDefinition InfuseMateria = new MateriaDefinition("infuse", "Infusao", "Roxa: ligada a magia elemental, infunde o ataque basico na arma (0 MP) ou concede resistencia de 50% na armadura. Primeiro par valido define o elemento da arma.", MateriaType.PhysicalEnhance, MateriaModifier.Infuse);
        public static readonly MateriaDefinition HeavyMateria = new MateriaDefinition("heavy", "Forca", "Roxa: aumenta ataque em 20%. Nao concede comando.", MateriaType.PhysicalEnhance, MateriaModifier.AttackUp);
        public static readonly MateriaDefinition CleaveMateria = new MateriaDefinition("cleave", "Corte", "Roxa: concede Corte amplo a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { AbilityCatalog.Cleave });
        public static readonly MateriaDefinition GuardianMateria = new MateriaDefinition("guardian", "Guardiao", "Roxa: concede Provocar e Proteger a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { AbilityCatalog.Taunt, AbilityCatalog.Protect });
        public static readonly MateriaDefinition EarthMateria = new MateriaDefinition("earth", "Terra", "Verde: concede Terra.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Earth });
        public static readonly MateriaDefinition ChaosMateria = new MateriaDefinition("chaos", "Caos", "Verde: concede Caos.", MateriaType.Magic, abilities: new[] { AbilityCatalog.Chaos });
        public static readonly MateriaDefinition BahamutMateria = new MateriaDefinition("bahamut", "Bahamut", "Vermelha: dano alto de Caos em todos os inimigos. 20 MP.", MateriaType.Summon, abilities: new[] { AbilityCatalog.Bahamut });
        public static readonly MateriaDefinition TitanMateria = new MateriaDefinition("titan", "Titan", "Vermelha: dano baixo de Terra; Shield de 50% por 3 turnos de cada aliado. 16 MP.", MateriaType.Summon, abilities: new[] { AbilityCatalog.Titan });
        public static readonly MateriaDefinition DefenseMateria = new MateriaDefinition("defense", "Defesa", "Roxa: aumenta defesa em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.DefenseUp);
        public static readonly MateriaDefinition HpMateria = new MateriaDefinition("hp", "HP", "Roxa: aumenta HP maximo em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.HpUp);
        public static readonly MateriaDefinition MpMateria = new MateriaDefinition("mp", "MP", "Roxa: aumenta MP maximo em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.MpUp);
    }
}
