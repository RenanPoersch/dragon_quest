using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeInnLayout
    {
        public static readonly Rect Bounds = new Rect(-7,-5,14,10);
        public static readonly Vector2 Spawn = new Vector2(0,-2.7f), Exit = new Vector2(0,-3.8f), Keeper = new Vector2(2,-0.5f);
        public static IReadOnlyList<CityBlock> Blocks { get; } = System.Array.AsReadOnly(new[]
        {
            new CityBlock("Piso da hospedaria",0,0,14,10,"967756",false,-10),
            new CityBlock("Tapete da hospedaria",0,-2,3,4,"74607F",false,-8),
            new CityBlock("Parede norte",0,4.7f,14,0.6f,"5A4635",true,5),
            new CityBlock("Parede sul",0,-4.7f,14,0.6f,"5A4635",true,5),
            new CityBlock("Parede oeste",-6.7f,0,0.6f,10,"5A4635",true,5),
            new CityBlock("Parede leste",6.7f,0,0.6f,10,"5A4635",true,5),
            new CityBlock("Cama oeste",-4,2.5f,1.5f,2.4f,"658B8A",true,3),
            new CityBlock("Cama central",0,2.5f,1.5f,2.4f,"658B8A",true,3),
            new CityBlock("Cama leste",4,2.5f,1.5f,2.4f,"658B8A",true,3)
        });
    }
}
