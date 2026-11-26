using UnityEngine;

// Configura a apresentação inicial do jogo compilado antes de carregar a primeira cena.
namespace DragonQuest.Bootstrap
{
    public static class GameDisplay
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // Aplica a resolução de 1920 por 1080 em janela de tela cheia nas plataformas de computador previstas.
        private static void Initialize()
        {
            if (Application.isEditor) return;
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.OSXPlayer &&
                Application.platform != RuntimePlatform.LinuxPlayer) return;

            // Reaplica o padrao mesmo quando o player guardou outra resolucao na sessao anterior.
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        }
    }
}
