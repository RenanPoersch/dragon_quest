using System;

// Calcula o deslocamento de rótulos que excedem a área visível do comando.
namespace DragonQuest.Presentation.Battle
{
    public static class BattleCommandTextScroll
    {
        // Define as pausas e a velocidade de deslocamento usadas pela animação de texto.
        public const double StartDelay = .7, EndDelay = .4, PixelsPerSecond = 60;

        // Usa o tempo e a diferença de largura para calcular quanto texto deve deslizar.
        public static float Offset(double elapsed, float textWidth, float viewportWidth)
        {
            double distance = Math.Max(0, textWidth - viewportWidth);
            if (distance == 0 || elapsed <= 0) return 0;
            double cycle = StartDelay + distance / PixelsPerSecond + EndDelay;
            double phase = elapsed % cycle;
            return (float)Math.Max(0, Math.Min(distance, (phase - StartDelay) * PixelsPerSecond));
        }
    }
}
