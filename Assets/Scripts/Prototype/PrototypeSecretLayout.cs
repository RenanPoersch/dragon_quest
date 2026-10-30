using System;
using System.Collections.Generic;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeSecretLayout
    {
        public static readonly Vector2 Altar = PrototypeRuinsLayout.Altar;
        public static readonly Vector2 Inscription = new Vector2(-2, 3);
        // Registered to the visible stone and hollow in EastForestBackdrop (fixed oblique camera).
        public static readonly Vector2 ClawStone = new Vector2(3.35f, 0.70f);
        public static readonly Vector2 HollowTree = new Vector2(7.18f, 3.27f);
        public const string SealedRocks = "Barreira magica do vulcao";
        public static IReadOnlyList<CityBlock> WestMonoliths { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Monolito - Sol",2,-1.5f,0.9f,0.8f,"8A8A7B",true,2),
            new CityBlock("Monolito - Lua",-2,0,0.9f,0.8f,"8A8A7B",true,2),
            new CityBlock("Monolito - Estrela",2,3.5f,0.9f,0.8f,"8A8A7B",true,2)
        });
    }
}
