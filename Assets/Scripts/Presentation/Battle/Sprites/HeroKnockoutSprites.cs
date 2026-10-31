using DragonQuest.Presentation.Battle.Animations;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Sprites
{
    public sealed class HeroKnockoutSprites
    {
        // Bent legs lower the silhouette while knees remain on the same ground baseline.
        public const float HeightRatio = .76f;
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();
        public static string Resource(string id) => id == "aren" ? "ArenKnockout"
            : id == "lia" ? "LiaKnockout" : id == "bram" ? "BramKnockout" : null;
        public bool Draw(Rect area, string id)
        {
            string resource = Resource(id);
            if (resource == null) return false;
            if (!sheets.TryGetValue(id, out var sheet))
            {
                sheet = new BattleAnimationSheet("PixelArt/Knockout/" + resource, 1, 1, false);
                sheets.Add(id, sheet);
            }
            return sheet.Draw(new Vector2(area.center.x, area.yMax), area.height * HeightRatio, 0, Color.white);
        }
    }
}
