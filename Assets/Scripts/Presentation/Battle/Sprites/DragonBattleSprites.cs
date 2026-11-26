using DragonQuest.Presentation.Battle.Animations;
using System;
using System.Collections.Generic;
using UnityEngine;

// Desenha os quadros das animações do dragão durante o combate.
namespace DragonQuest.Presentation.Battle.Sprites
{
    // A primeira pose define o tamanho do corpo; fogo e arcos nao reduzem o dragao.
    public sealed class DragonBattleSprites
    {
        // Mantém uma folha por chave de recurso para reutilização entre quadros.
        private readonly Dictionary<string, BattleAnimationSheet> sheets = new Dictionary<string, BattleAnimationSheet>();

        // Recebe o recurso, o quadro da animação e a cor que serão aplicados ao desenho.
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
