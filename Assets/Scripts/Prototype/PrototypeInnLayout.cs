using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeInnLayout
    {
        public const float CameraSize = 6.65f;
        public static readonly Rect Bounds = new Rect(-7, -5, 14, 10);
        public static readonly Vector2 Spawn = new Vector2(0, -2.7f), Exit = new Vector2(0, -3.8f), Keeper = new Vector2(2, -.5f);
        public static IReadOnlyList<CityBlock> Blocks { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Piso da hospedaria",0,0,14,10,"967756",false,-10),
            new CityBlock("Tapete da hospedaria",0,-2.1f,2.8f,4.65f,"315A5A",false,-8),
            new CityBlock("Parede norte",0,4.7f,14,.6f,"5A4635",true,5),
            new CityBlock("Parede sul oeste",-3.95f,-4.7f,6.1f,.6f,"5A4635",true,5),
            new CityBlock("Parede sul leste",3.95f,-4.7f,6.1f,.6f,"5A4635",true,5),
            new CityBlock("Parede oeste",-6.7f,0,.6f,10,"5A4635",true,5),
            new CityBlock("Parede leste",6.7f,0,.6f,10,"5A4635",true,5),
            new CityBlock("Cama oeste",-4,2.5f,1.5f,2.4f,"658B8A",true,3),
            new CityBlock("Cama central",0,2.5f,1.5f,2.4f,"658B8A",true,3),
            new CityBlock("Cama leste",4,2.5f,1.5f,2.4f,"658B8A",true,3),
            new CityBlock("Lareira",-5.1f,-.1f,1.9f,1.4f,"70533F",true,3),
            new CityBlock("Balcao da recepcao",4.4f,-1.25f,2.9f,.75f,"70533F",true,3),
            new CityBlock("Armario de enxoval",5.65f,1.6f,1.1f,1.5f,"70533F",true,3),
            new CityBlock("Mesa do cafe",-3.65f,-2.6f,2.3f,1.4f,"70533F",true,3),
            new CityBlock("Samambaia oeste",-5.8f,-3.5f,.6f,.6f,"477755",true,3),
            new CityBlock("Samambaia leste",5.8f,-3.5f,.6f,.6f,"477755",true,3)
        });

        public static bool IsWalkable(Vector2 position)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.x) || float.IsInfinity(position.y)
                || !Bounds.Contains(position)) return false;
            foreach (CityBlock block in Blocks)
            {
                if (!block.Solid) continue;
                Vector2 half = block.Size * .5f + new Vector2(.3f, .32f);
                if (Math.Abs(position.x - block.Position.x) <= half.x && Math.Abs(position.y - block.Position.y) <= half.y) return false;
            }
            return true;
        }

        public static Vector2 SafeSavedPosition(Vector2 position) => IsWalkable(position) ? position : Spawn;
    }
}
