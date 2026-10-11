using System;
using System.Collections.Generic;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeSecretLayout
    {
        public static readonly Vector2 Altar = new Vector2(-24, 18);
        public static readonly Vector2 Inscription = new Vector2(-2, 3);
        public static readonly Vector2 ClawStone = new Vector2(2, 3);
        public static readonly Vector2 HollowTree = new Vector2(4.5f, 5);
        public const string SealedRocks = "Rochas da passagem selada";
        public static IReadOnlyList<CityBlock> WestMonoliths { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Monolito - Sol",2,-1.5f,0.9f,0.8f,"8A8A7B",true,2),
            new CityBlock("Monolito - Lua",-2,0,0.9f,0.8f,"8A8A7B",true,2),
            new CityBlock("Monolito - Estrela",2,3.5f,0.9f,0.8f,"8A8A7B",true,2)
        });
        public static MonolithSymbol SymbolAt(int index) => (MonolithSymbol)index;
    }
}
