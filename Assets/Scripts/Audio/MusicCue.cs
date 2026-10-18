using DragonQuest.Progression;

namespace DragonQuest.Audio
{
    public enum MusicCue
    {
        Silence, Intro, World, Village, Quarry, Forest, Ruins, Battle, Dragon, FinalBoss, Victory,
        Voyage, Sleep, Defeat, DragonFreed, GoodEnding, MindControlEnding, MeteorEnding, Throne
    }

    public readonly struct MusicStoryState
    {
        public readonly bool Voyage, Sleeping, DragonDefeated;
        public readonly GameEnding Ending;
        public MusicStoryState(bool voyage = false, bool sleeping = false, bool dragonDefeated = false,
            GameEnding ending = GameEnding.None)
        { Voyage = voyage; Sleeping = sleeping; DragonDefeated = dragonDefeated; Ending = ending; }
    }

    // One priority order for the title, front end, combat and exploration.
    public static class MusicCuePolicy
    {
        public static MusicCue Select(bool intro, bool frontEnd, bool ending, bool combat,
            bool dragon, bool finalBoss, bool results, bool defeat, string map, bool dragonPrelude = false,
            MusicStoryState story = default)
        {
            if (intro) return MusicCue.Intro;
            if (story.Voyage) return MusicCue.Voyage;
            if (frontEnd) return MusicCue.Silence;
            if (ending || story.Ending != GameEnding.None) return EndingCue(story.Ending);
            if (combat)
            {
                if (results) return MusicCue.Victory;
                if (defeat) return MusicCue.Defeat;
                return finalBoss ? MusicCue.FinalBoss : dragon ? MusicCue.Dragon : MusicCue.Battle;
            }
            if (story.Sleeping) return MusicCue.Sleep;
            if (dragonPrelude && map == "volcano") return MusicCue.Dragon;
            switch (map)
            {
                case "world": return MusicCue.World;
                case "city": case "inn": case "item-shop": case "materia-shop": case "equipment-shop":
                    return MusicCue.Village;
                case "west-forest": return MusicCue.Quarry;
                case "east-forest": return MusicCue.Forest;
                case "ancient-ruins": return MusicCue.Ruins;
                case "volcano": return story.DragonDefeated ? MusicCue.DragonFreed : MusicCue.Silence;
                case "castle": return MusicCue.Throne;
                default: return MusicCue.Silence;
            }
        }

        public static MusicCue EndingCue(GameEnding ending) => ending switch
        {
            GameEnding.KingFreed => MusicCue.GoodEnding,
            GameEnding.HeroPossessed => MusicCue.MindControlEnding,
            GameEnding.CastleDestroyed => MusicCue.MeteorEnding,
            _ => MusicCue.Silence
        };

        // Rest is a complete one-shot jingle. Other recordings loop while their scene stays active.
        public static bool ShouldLoop(MusicCue cue) => IsAvailable(cue) && cue != MusicCue.Sleep;

        // Each music cue maps to a supplied recording; silence never loads a clip.
        public static bool IsAvailable(MusicCue cue) => cue == MusicCue.Intro || cue == MusicCue.Battle
            || cue == MusicCue.Victory || cue == MusicCue.World || cue == MusicCue.Village
            || cue == MusicCue.Dragon || cue == MusicCue.FinalBoss || cue == MusicCue.Forest
            || cue == MusicCue.Ruins || cue == MusicCue.Quarry || cue == MusicCue.Voyage
            || cue == MusicCue.Sleep || cue == MusicCue.Defeat || cue == MusicCue.DragonFreed
            || cue == MusicCue.GoodEnding || cue == MusicCue.MindControlEnding || cue == MusicCue.MeteorEnding
            || cue == MusicCue.Throne;
        public static string ResourceName(MusicCue cue) => IsAvailable(cue) ? "Music/" + cue.ToString().ToLowerInvariant() : null;
    }
}
