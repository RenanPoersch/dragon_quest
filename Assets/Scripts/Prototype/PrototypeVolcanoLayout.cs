using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeVolcanoLayout
    {
        public static readonly Rect Bounds = new Rect(-10,-14,20,28);
        public static readonly Vector2 Spawn = new Vector2(0,-10), Exit = new Vector2(0,-12.5f), Dragon = new Vector2(0,8);
        public static IReadOnlyList<CityBlock> Blocks { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Piso vulcanico",0,0,20,28,"423E42",false,-10),
            new CityBlock("Lava oeste",-6,-4,7.5f,18,"B54A29",true,-8),
            new CityBlock("Lava leste",6,-4,7.5f,18,"B54A29",true,-8),
            new CityBlock("Trilha de basalto",0,-4,4.5f,18,"655D59",false,-7),
            new CityBlock("Arena ancestral",0,8,16,9,"716666",false,-7),
            new CityBlock("Rochas - oeste",-9.7f,0,0.6f,28,"574E55",true,3),
            new CityBlock("Rochas - leste",9.7f,0,0.6f,28,"574E55",true,3),
            new CityBlock("Rochas - norte",0,13.7f,20,0.6f,"574E55",true,3),
            new CityBlock("Rochas - sul",0,-13.7f,20,0.6f,"574E55",true,3),
            new CityBlock("Rochas - trilha oeste",-1.6f,-5,0.7f,1.2f,"70605D",true,3),
            new CityBlock("Rochas - trilha leste",1.6f,0,0.7f,1.2f,"70605D",true,3),
            new CityBlock("Runa da arena",0,8,5.5f,5.5f,"95725F",false,-6)
        });
    }
}
