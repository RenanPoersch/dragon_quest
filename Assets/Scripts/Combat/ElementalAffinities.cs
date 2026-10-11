using System;
using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public enum DamageElement { None, Fire, Ice, Lightning }

    // Perfil imutavel: pode ser compartilhado entre encontros sem carregar estado da batalha.
    public sealed class ElementalAffinities
    {
        public static readonly ElementalAffinities Neutral = new ElementalAffinities();
        public IReadOnlyList<DamageElement> Weaknesses { get; }
        public IReadOnlyList<DamageElement> Resistances { get; }

        public ElementalAffinities(DamageElement[] weaknesses = null, DamageElement[] resistances = null)
        {
            var seen = new HashSet<DamageElement>();
            Weaknesses = Copy(weaknesses, seen);
            Resistances = Copy(resistances, seen);
        }

        private static IReadOnlyList<DamageElement> Copy(DamageElement[] elements, HashSet<DamageElement> seen)
        {
            var copy = elements == null ? new DamageElement[0] : (DamageElement[])elements.Clone();
            foreach (DamageElement element in copy)
                if (element == DamageElement.None || !Enum.IsDefined(typeof(DamageElement), element) || !seen.Add(element))
                    throw new ArgumentException("Afinidades exigem elementos validos, sem repeticao ou conflito.");
            Array.Sort(copy);
            return Array.AsReadOnly(copy);
        }

        public int DamagePercent(DamageElement element)
        {
            if (!Enum.IsDefined(typeof(DamageElement), element)) throw new ArgumentException("Elemento desconhecido.");
            foreach (DamageElement weakness in Weaknesses) if (weakness == element) return 150;
            foreach (DamageElement resistance in Resistances) if (resistance == element) return 50;
            return 100;
        }

        public static ElementalAffinities Combine(params ElementalAffinities[] profiles)
        {
            ElementalAffinities single=null;int count=0;
            foreach(ElementalAffinities profile in profiles)
                if(profile!=null&&(profile.Weaknesses.Count>0||profile.Resistances.Count>0)) {single=profile;count++;}
            if(count<2) return single??Neutral;
            var weak = new HashSet<DamageElement>(); var resist = new HashSet<DamageElement>();
            foreach (ElementalAffinities profile in profiles)
            {
                if (profile == null) continue;
                foreach (DamageElement element in profile.Weaknesses) weak.Add(element);
                foreach (DamageElement element in profile.Resistances) resist.Add(element);
            }
            // A protecao do equipamento prevalece sobre uma fraqueza do personagem.
            weak.ExceptWith(resist);
            var weaknesses = new DamageElement[weak.Count]; weak.CopyTo(weaknesses);
            var resistances = new DamageElement[resist.Count]; resist.CopyTo(resistances);
            return new ElementalAffinities(weaknesses, resistances);
        }

        public static string NameOf(DamageElement element)
        {
            switch (element)
            {
                case DamageElement.None: return "Sem elemento";
                case DamageElement.Fire: return "Fogo";
                case DamageElement.Ice: return "Gelo";
                case DamageElement.Lightning: return "Raio";
                default: throw new ArgumentException("Elemento desconhecido.");
            }
        }

        public string Description => "Fraco: " + Names(Weaknesses) + " / Resiste: " + Names(Resistances);

        private static string Names(IReadOnlyList<DamageElement> elements)
        {
            var names = new List<string>();
            foreach (DamageElement element in elements) names.Add(NameOf(element));
            return names.Count == 0 ? "nenhum" : string.Join(", ", names);
        }
    }
}
