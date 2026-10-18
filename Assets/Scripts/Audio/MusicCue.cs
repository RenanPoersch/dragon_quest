namespace DragonQuest.Audio
{
    public enum MusicCue { Silence, Intro, World, Village, Quarry, Forest, Ruins, Battle, Dragon, FinalBoss, Victory }

    // One priority order for the title, front end, combat and exploration.
    public static class MusicCuePolicy
    {
        public static MusicCue Select(bool intro, bool frontEnd, bool ending, bool combat,
            bool dragon, bool finalBoss, bool results, bool defeat, string map, bool dragonPrelude = false)
        {
            if (intro) return MusicCue.Intro;
            if (frontEnd || ending) return MusicCue.Silence;
            if (combat)
            {
                if (results) return MusicCue.Victory;
                if (defeat) return MusicCue.Silence;
                return finalBoss ? MusicCue.FinalBoss : dragon ? MusicCue.Dragon : MusicCue.Battle;
            }
            if (dragonPrelude && map == "volcano") return MusicCue.Dragon;
            switch (map)
            {
                case "world": return MusicCue.World;
                case "city": case "inn": case "item-shop": case "materia-shop": case "equipment-shop":
                    return MusicCue.Village;
                case "west-forest": return MusicCue.Quarry;
                case "east-forest": return MusicCue.Forest;
                case "ancient-ruins": return MusicCue.Ruins;
                case "volcano": return MusicCue.Silence;
                default: return MusicCue.Silence;
            }
        }

        // Each music cue maps to a supplied recording; silence never loads a clip.
        public static bool IsAvailable(MusicCue cue) => cue == MusicCue.Intro || cue == MusicCue.Battle
            || cue == MusicCue.Victory || cue == MusicCue.World || cue == MusicCue.Village
            || cue == MusicCue.Dragon || cue == MusicCue.FinalBoss || cue == MusicCue.Forest
            || cue == MusicCue.Ruins || cue == MusicCue.Quarry;
        public static string ResourceName(MusicCue cue) => IsAvailable(cue) ? "Music/" + cue.ToString().ToLowerInvariant() : null;
    }
}
