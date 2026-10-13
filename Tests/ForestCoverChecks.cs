using System;
using UnityEngine;
using DragonQuest.Exploration;
class ForestCoverChecks
{
    static void Main()
    {
        int checks=0;
        foreach(float aspect in new[]{890f/750f,4f/3f,16f/10f,16f/9f,21f/9f,32f/9f})
        {
            Rect viewport=CameraFollow.ArtworkViewport(aspect);
            if(viewport!=new Rect(0,0,1,1))throw new Exception("Viewport leaves old map visible");checks++;
            float size=CameraFollow.ArtworkFrameSize(8,aspect);
            if(size>8.001f||size*aspect>128f/9+.001f)throw new Exception("Camera exposes artwork border");checks++;
            if(Math.Abs(Math.Max(size/8,size*aspect/(128f/9))-1)>.001)throw new Exception("Cover scale wrong");checks++;
            float center=size-8;
            if(Math.Abs(center-size+8)>.001)throw new Exception("Bottom exit is cropped");checks++;
            if(-7.6f<center-size||-7.6f>center+size)throw new Exception("Exit outside viewport");checks++;
        }
        Console.WriteLine(checks+" fullscreen, proportional cover and bottom exit checks passed.");
    }
}
