using System;
using System.Collections.Generic;
using DragonQuest.Content;

namespace DragonQuest.Presentation.Dialogue
{
    public static class DialoguePortraitCatalog
    {
        private const string Root = "PixelArt/DialoguePortraits/";
        public static IReadOnlyList<string> Characters { get; } = Array.AsReadOnly(new[]
        {
            "aren", "villager", "peasant", "gardener", "explorer", "quarry-keeper",
            "traveler", "innkeeper", "guard", "tyrant", "dragon", "dragon-essence"
        });

        public static string CharacterFor(DialogueSpeaker speaker)
        {
            switch (speaker)
            {
                case DialogueSpeaker.Citizen: return "villager";
                case DialogueSpeaker.Peasant: return "peasant";
                case DialogueSpeaker.Gardener: return "gardener";
                case DialogueSpeaker.Explorer: return "explorer";
                case DialogueSpeaker.QuarryKeeper: return "quarry-keeper";
                case DialogueSpeaker.Traveler: return "traveler";
                case DialogueSpeaker.Innkeeper: return "innkeeper";
                case DialogueSpeaker.RoyalGuard: return "guard";
                case DialogueSpeaker.Tyrant: return "tyrant";
                case DialogueSpeaker.Dragon: return "dragon";
                case DialogueSpeaker.DragonEssence: return "dragon-essence";
                default: return "aren";
            }
        }

        public static string ResourceFor(DialogueSpeaker speaker) => Resource(CharacterFor(speaker));
        public static string Resource(string character) => Root + character;
    }
}
