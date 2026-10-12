using System;
using UnityEngine;

namespace DragonQuest.Exploration
{
    public enum PixelDirection { Down, Left, Right, Up }
    public readonly struct PixelFrame
    {
        public readonly int X, Y, Width, Height;
        public readonly float FootX;
        public PixelFrame(int x, int y, int width, int height, float footX)
        { X = x; Y = y; Width = width; Height = height; FootX = footX; }
        public Rect Rect => new Rect(X, Y, Width, Height);
    }

    // Alpha vem de baixo para cima, como GetPixels32. Recorte so em memoria, sem alterar os PNGs.
    public static class PixelSpriteLayout
    {
        public const int Columns = 4, Rows = 3, FrameCount = Columns * Rows;
        public static PixelFrame[] Read(int width, int height, byte[] alpha)
        {
            if (width < 16 || height < 12 || width % Columns != 0 || height % Rows != 0
                || alpha == null || alpha.Length != (long)width * height) throw new ArgumentException("Atlas de sprites invalido.");
            int cellWidth = width / Columns, cellHeight = height / Rows;
            var frames = new PixelFrame[FrameCount];
            for (int index = 0; index < frames.Length; index++)
            {
                int left = index % Columns * cellWidth, bottom = (Rows - 1 - index / Columns) * cellHeight;
                int minX = left + cellWidth, maxX = left - 1, minY = bottom + cellHeight, maxY = bottom - 1;
                for (int y = bottom; y < bottom + cellHeight; y++)
                    for (int x = left; x < left + cellWidth; x++)
                        if (alpha[y * width + x] >= 128)
                        { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                if (maxX < minX || minX <= left || maxX >= left + cellWidth - 1
                    || minY <= bottom || maxY >= bottom + cellHeight - 1)
                    throw new ArgumentException("Sprite vazio ou cortado na celula " + index + ".");
                // O centro dos pes e a ancora, evitando que capas/deslocamentos laterais movam o corpo.
                double sum = 0, count = 0;
                for (int y = minY; y <= Math.Min(maxY, minY + 5); y++)
                    for (int x = minX; x <= maxX; x++) if (alpha[y * width + x] >= 128) { sum += x + 0.5; count++; }
                int x0 = minX - 1, y0 = minY - 1, w = maxX - minX + 3, h = maxY - minY + 3;
                frames[index] = new PixelFrame(x0, y0, w, h, (float)((sum / count - x0) / w));
            }
            return frames;
        }
        public static PixelDirection Facing(Vector2 groundDirection)
        {
            float x = groundDirection.x * ObliqueProjection.YawCos + groundDirection.y * ObliqueProjection.YawSin;
            float y = -groundDirection.x * ObliqueProjection.YawSin + groundDirection.y * ObliqueProjection.YawCos;
            return Math.Abs(x) > Math.Abs(y) ? x < 0 ? PixelDirection.Left : PixelDirection.Right : y > 0 ? PixelDirection.Up : PixelDirection.Down;
        }
        public static int WalkFrame(PixelDirection facing, float distance, bool walking)
        {
            int phase = walking ? (int)(distance / 0.22f) % 4 : 0;
            int row = phase == 1 ? 1 : phase == 3 ? 2 : 0;
            return row * Columns + (int)facing;
        }
    }
}
