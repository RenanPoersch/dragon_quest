using System;
using UnityEngine;

namespace DragonQuest.Exploration
{
    // As primeiras quatro direcoes usam o atlas cardinal; as demais, o atlas diagonal.
    public enum PixelDirection { Down, Left, Right, Up, DownLeft, DownRight, UpLeft, UpRight }
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
            if (width < 16 || height < 12
                || alpha == null || alpha.Length != (long)width * height) throw new ArgumentException("Atlas de sprites invalido.");
            var frames = new PixelFrame[FrameCount];
            for (int index = 0; index < frames.Length; index++)
            {
                // Divide toda a imagem, inclusive quando o tamanho tem um pixel a mais/menos.
                int column = index % Columns, row = index / Columns;
                int left = column * width / Columns, right = (column + 1) * width / Columns;
                int bottom = height - (row + 1) * height / Rows, top = height - row * height / Rows;
                int minX = right, maxX = left - 1, minY = top, maxY = bottom - 1;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                        if (alpha[y * width + x] >= 128)
                        { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                if (maxX < minX || minX <= left || maxX >= right - 1
                    || minY <= bottom || maxY >= top - 1)
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
        public static PixelDirection Facing(Vector2 groundDirection, bool includeDiagonals = false)
        {
            float x = groundDirection.x * ObliqueProjection.YawCos + groundDirection.y * ObliqueProjection.YawSin;
            float y = -groundDirection.x * ObliqueProjection.YawSin + groundDirection.y * ObliqueProjection.YawCos;
            // Oito setores de 45 graus, na orientacao dos controles em relacao a camera.
            const float diagonalThreshold = 0.41421356f; // tan(22.5 graus)
            if (includeDiagonals && Math.Abs(x) > Math.Abs(y) * diagonalThreshold
                && Math.Abs(y) > Math.Abs(x) * diagonalThreshold)
                return y > 0 ? x < 0 ? PixelDirection.UpLeft : PixelDirection.UpRight
                    : x < 0 ? PixelDirection.DownLeft : PixelDirection.DownRight;
            return Math.Abs(x) > Math.Abs(y) ? x < 0 ? PixelDirection.Left : PixelDirection.Right : y > 0 ? PixelDirection.Up : PixelDirection.Down;
        }
        public static bool IsDiagonal(PixelDirection facing) => (int)facing >= Columns;
        public static int WalkFrame(PixelDirection facing, float distance, bool walking)
        {
            int phase = walking ? (int)(distance / 0.22f) % 4 : 0;
            int row = phase == 1 ? 1 : phase == 3 ? 2 : 0;
            return row * Columns + (int)facing % Columns;
        }
    }
}
