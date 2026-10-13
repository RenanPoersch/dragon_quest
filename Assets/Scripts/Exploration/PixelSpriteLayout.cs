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
        public Vector2 Pivot => new Vector2(FootX, 1f / Height);
    }

    // Pequenos intervalos sem deslocamento entre amostras da fisica nao reiniciam o passo.
    public struct PixelWalkCycle
    {
        public float Distance { get; private set; }
        public bool Walking { get; private set; }
        private float stationaryTime;
        public void Advance(float distance, float deltaTime, bool movementRequested, bool teleported)
        {
            if (!movementRequested || teleported) { Reset(); return; }
            if (distance > 0.0001f)
            {
                Distance = (Distance + distance) % (PixelSpriteLayout.WalkPoseDistance * 4);
                stationaryTime = 0;
                Walking = true;
            }
            else
            {
                stationaryTime += deltaTime;
                if (stationaryTime >= 0.1f) Reset();
            }
        }
        private void Reset() { Distance = 0; stationaryTime = 0; Walking = false; }
    }

    // Alpha vem de baixo para cima, como GetPixels32. Recorte so em memoria, sem alterar os PNGs.
    public static class PixelSpriteLayout
    {
        public const int Columns = 4, Rows = 3, FrameCount = Columns * Rows;
        public const float WalkPoseDistance = 0.4f;
        public static readonly Vector3 UprightUp = new Vector3(0, 0, -1);
        public static float UprightScaleY => 1f / ObliqueProjection.HeightScale;
        public static Vector3 HorizontalCameraForward(Vector3 forward)
        {
            float length = (float)Math.Sqrt(forward.x * forward.x + forward.y * forward.y);
            return length < 0.0001f ? new Vector3(-ObliqueProjection.YawSin, ObliqueProjection.YawCos, 0)
                : new Vector3(forward.x / length, forward.y / length, 0);
        }
        public static PixelFrame[] Read(int width, int height, byte[] alpha, bool stableWalkingAnchor = false)
            => ReadGrid(width, height, alpha, Columns, Rows, stableWalkingAnchor);

        // Janelas em pixels do PNG original; o gerador preservou os desenhos fora da grade regular.
        // Recortamos apenas em memoria, mantendo a imagem inteira sem edicao ou redimensionamento.
        public static RectInt WorldIconRegion(int index)
        {
            switch (index)
            {
                case 0: return new RectInt(0, 0, 550, 540);
                case 1: return new RectInt(550, 0, 490, 540);
                case 2: return new RectInt(1040, 0, 496, 540);
                case 3: return new RectInt(0, 540, 550, 484);
                case 4: return new RectInt(550, 520, 590, 504);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public static PixelFrame[] ReadWorldIcons(int width, int height, byte[] alpha)
        {
            if (width != 1536 || height != 1024 || alpha == null || alpha.Length != width * height)
                throw new ArgumentException("Dimensoes da folha de POIs invalidas.");
            var frames = new PixelFrame[5];
            for (int index = 0; index < frames.Length; index++)
            {
                RectInt region = WorldIconRegion(index);
                int bottom = height - region.yMax, top = height - region.yMin;
                int minX = region.xMax, maxX = region.xMin - 1, minY = top, maxY = bottom - 1;
                for (int y = bottom; y < top; y++) for (int x = region.xMin; x < region.xMax; x++)
                    if (alpha[y * width + x] >= 128)
                    { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                if (maxX < minX || minX <= region.xMin || maxX >= region.xMax - 1 || minY <= bottom || maxY >= top - 1)
                    throw new ArgumentException("POI vazio ou cortado: " + index);
                frames[index] = new PixelFrame(minX - 1, minY - 1, maxX - minX + 3, maxY - minY + 3, 0.5f);
            }
            return frames;
        }

        public static PixelFrame[] ReadPropPair(int width, int height, byte[] alpha)
        {
            var frames = ReadGrid(width, height, alpha, 2, 1, false);
            // O centro da base, abaixo da tampa, nao muda quando a silhueta aberta fica mais larga.
            int baseRow = Math.Max(1, (int)Math.Round((frames[0].Width - 2) * 0.23f));
            for (int i = 0; i < frames.Length; i++)
            {
                PixelFrame f = frames[i];
                int y = f.Y + 1 + baseRow, left = f.X + f.Width, right = f.X - 1;
                if (y >= f.Y + f.Height - 1) throw new ArgumentException("Base do objeto ausente.");
                for (int x = f.X; x < f.X + f.Width; x++)
                    if (alpha[y * width + x] >= 128) { left = Math.Min(left, x); right = Math.Max(right, x); }
                if (right < left) throw new ArgumentException("Base do objeto vazia.");
                frames[i] = new PixelFrame(f.X, f.Y, f.Width, f.Height, ((left + right + 1) / 2f - f.X) / f.Width);
            }
            return frames;
        }

        private static PixelFrame[] ReadGrid(int width, int height, byte[] alpha, int columns, int rows, bool stableWalkingAnchor)
        {
            if (width < columns * 4 || height < rows * 4
                || alpha == null || alpha.Length != (long)width * height) throw new ArgumentException("Atlas de sprites invalido.");
            var frames = new PixelFrame[columns * rows];
            for (int index = 0; index < frames.Length; index++)
            {
                // Divide toda a imagem, inclusive quando o tamanho tem um pixel a mais/menos.
                int column = index % columns, row = index / columns;
                int left = column * width / columns, right = (column + 1) * width / columns;
                int bottom = height - (row + 1) * height / rows, top = height - row * height / rows;
                int minX = right, maxX = left - 1, minY = top, maxY = bottom - 1;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                        if (alpha[y * width + x] >= 128)
                        { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                if (maxX < minX || minX <= left || maxX >= right - 1
                    || minY <= bottom || maxY >= top - 1)
                    throw new ArgumentException("Sprite vazio ou cortado na celula " + index + ".");
                // Em caminhada, a ancora horizontal acompanha o corpo; o pe que avanca nao deve arrasta-lo.
                // Moradores estaticos continuam usando a base dos pes.
                double sum = 0, count = 0;
                int anchorBottom = stableWalkingAnchor ? maxY - (maxY - minY) / 3 : minY;
                int anchorTop = stableWalkingAnchor ? maxY : Math.Min(maxY, minY + 5);
                for (int y = anchorBottom; y <= anchorTop; y++)
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
            int phase = walking ? (int)(distance / WalkPoseDistance) % 4 : 0;
            int row = phase == 1 ? 1 : phase == 3 ? 2 : 0;
            return row * Columns + (int)facing % Columns;
        }
    }
}
