using System;
using System.Collections.Generic;
using DragonQuest.Combat;

namespace DragonQuest.Equipment
{
    public enum MateriaType { Magic, Support, Summon, PhysicalEnhance }
    public enum MateriaModifier { None, All, Infuse }

    public sealed class MateriaDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public MateriaType Type { get; }
        public MateriaModifier Modifier { get; }
        public IReadOnlyList<AbilityDefinition> Abilities { get; }

        public MateriaDefinition(string id, string name, string description, MateriaType type,
            MateriaModifier modifier = MateriaModifier.None, params AbilityDefinition[] abilities)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Materia precisa de ID.");
            if (!Enum.IsDefined(typeof(MateriaType), type) || !Enum.IsDefined(typeof(MateriaModifier), modifier))
                throw new ArgumentException("Tipo de materia desconhecido.");
            if ((modifier == MateriaModifier.All && type != MateriaType.Support)
                || (modifier == MateriaModifier.Infuse && type != MateriaType.PhysicalEnhance))
                throw new ArgumentException("Modificador incompativel com a cor da materia.");
            if (abilities == null) throw new ArgumentNullException(nameof(abilities));
            var ids = new HashSet<string>();
            foreach (AbilityDefinition ability in abilities)
                if (ability == null || !ids.Add(ability.Id)) throw new ArgumentException("Comandos invalidos na materia.");
            Id = id; Name = name; Description = description; Type = type; Modifier = modifier;
            Abilities = Array.AsReadOnly((AbilityDefinition[])abilities.Clone());
        }
    }

    // Duas copias da mesma materia sao itens distintos e podem ser equipadas separadamente.
    public sealed class MateriaInstance
    {
        public string Id { get; }
        public MateriaDefinition Definition { get; }
        public MateriaInstance(string id, MateriaDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Instancia precisa de ID.");
            Id = id; Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
