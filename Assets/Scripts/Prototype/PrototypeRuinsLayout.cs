using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeRuinsLayout
    {
        public static readonly Rect Bounds = new Rect(-9,-7,18,14);
        public static readonly Vector2 Spawn = new Vector2(0,-3.5f);
        public static readonly Vector2 Exit = new Vector2(0,-5.8f);
        public static IReadOnlyList<CityBlock> Icon { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Ruinas - base",0,0.1f,1.8f,0.8f,"85887B",true,2)
        });
        public static IReadOnlyList<CityBlock> Blocks { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Ruinas - terreno",0,0,18,14,"666E58",false,-10),
            new CityBlock("Piso antigo",0,0,11,11,"A49F8D",false,-9),
            new CityBlock("Caminho das ruinas",0,-1,2.3f,11,"B5AB8C",false,-8),
            new CityBlock("Ruina - limite norte",0,6.7f,18,0.6f,"74776C",true,2),
            new CityBlock("Ruina - limite sul",0,-6.7f,18,0.6f,"74776C",true,2),
            new CityBlock("Ruina - limite oeste",-8.7f,0,0.6f,14,"74776C",true,2),
            new CityBlock("Ruina - limite leste",8.7f,0,0.6f,14,"74776C",true,2),
            new CityBlock("Ruina - parede oeste",-5,3,0.8f,5,"8A897B",true,2),
            new CityBlock("Ruina - parede leste",5,4,0.8f,3,"8A897B",true,2),
            new CityBlock("Ruina - parede partida",2.5f,5.5f,3,0.7f,"8A897B",true,2),
            new CityBlock("Coluna quebrada baixa",-3,-2.5f,0.9f,0.9f,"B0AD9A",true,2),
            new CityBlock("Coluna quebrada alta",3,-2.5f,0.9f,0.9f,"B0AD9A",true,2),
            new CityBlock("Coluna quebrada alta",-3,2,0.9f,0.9f,"B0AD9A",true,2),
            new CityBlock("Coluna quebrada baixa",3,2,0.9f,0.9f,"B0AD9A",true,2),
            new CityBlock("Pedra antiga",-6,-3,1.6f,1,"8B927F",true,2),
            new CityBlock("Pedra antiga",6,-1,1.3f,1,"8B927F",true,2),
            new CityBlock("Altar antigo",PrototypeSecretLayout.Altar.x,PrototypeSecretLayout.Altar.y,1.2f,0.8f,"9F9B88",true,2)
        });
    }
}
