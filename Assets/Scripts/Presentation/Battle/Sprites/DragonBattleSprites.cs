using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // A primeira pose define o tamanho do corpo; fogo e arcos nao reduzem o dragao.
    public sealed class DragonBattleSprites
    {
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();

        public bool Draw(Rect area, string resource, int frame, Color tint)
        {
            if (resource != "Dragon" && resource != "DragonClaw" && resource != "DragonBreath" && resource != "DragonCharge") return false;
            if (!sheets.TryGetValue(resource, out BattleAnimationSheet sheet))
            {
                bool idle = resource == "Dragon";
                sheet = new BattleAnimationSheet("PixelArt/" + resource, idle ? 1 : 3, idle ? 1 : 2, true);
                sheets.Add(resource, sheet);
            }
            return sheet.Draw(new Vector2(area.center.x, area.yMax), area.height, frame, tint);
        }
    }
}
