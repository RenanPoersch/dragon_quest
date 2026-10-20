using System;
using System.Collections.Generic;

namespace DragonQuest.Prototype
{
    public static class DialoguePortraitCatalog
    {
        private const string Root = "PixelArt/DialoguePortraits/";
        public static IReadOnlyList<string> Characters { get; } = Array.AsReadOnly(new[]
        {
            "aren", "villager", "peasant", "gardener", "explorer", "quarry-keeper",
            "traveler", "innkeeper", "guard", "tyrant", "dragon", "dragon-essence"
        });

        public static string CharacterFor(string speaker)
        {
            switch (speaker)
            {
                case "Aldeão": case "Aldeao": return "villager";
                case "Camponesa": return "peasant";
                case "Jardineiro": return "gardener";
                case "Exploradora": return "explorer";
                case "Guardião da pedreira": case "Guardiao da pedreira": case "Lenhador": return "quarry-keeper";
                case "Viajante": return "traveler";
                case "Hospedeira": case "Hospedaria": return "innkeeper";
                case "Guarda Real": return "guard";
                case "Rei Rubro": return "tyrant";
                case "Dragão corrompido": case "Dragao corrompido": return "dragon";
                case "Essência do dragão": case "Essencia liberta": case "Essencia do dragao": return "dragon-essence";
                default: return "aren";
            }
        }

        public static string ResourceFor(string speaker) => Resource(CharacterFor(speaker));
        public static string Resource(string character) => Root + character;
    }
}
