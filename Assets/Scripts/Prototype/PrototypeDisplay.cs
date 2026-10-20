using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeDisplay
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
