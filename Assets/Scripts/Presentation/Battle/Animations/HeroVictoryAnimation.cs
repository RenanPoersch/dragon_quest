using DragonQuest.Presentation.Battle.Sprites;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    public sealed class HeroVictoryAnimation
    {
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();
        private readonly HeroKnockoutSprites knockoutSprites = new HeroKnockoutSprites();
        public static string Resource(string id) => id == "aren" ? "ArenVictory" : id == "lia" ? "LiaVictory" : id == "bram" ? "BramVictory" : null;
        public static int Pose(double elapsed) => (int)(Math.Max(0, elapsed) * 6) % 4;
        public bool Draw(Rect area, string id, bool celebrating, double elapsed, bool knockedOut = false)
        {
            if (knockedOut) return knockoutSprites.Draw(area, id);
            string resource = Resource(id);
            if (resource == null) return false;
            string key = id + (celebrating ? "/victory" : "/idle");
            if (!sheets.TryGetValue(key, out var sheet))
            {
                sheet = new BattleAnimationSheet(celebrating ? "PixelArt/Victory/" + resource
                    : "PixelArt/" + resource.Replace("Victory", "Battle"), 4, 1, false);
                sheets[key] = sheet;
            }
            float hop = celebrating ? (float)Math.Max(0, Math.Sin(elapsed * Math.PI * 3)) * 6 : 0;
            return sheet.Draw(new Vector2(area.center.x, area.yMax - hop), area.height,
                celebrating ? Pose(elapsed) : 0, Color.white);
        }
    }
}
