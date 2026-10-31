using DragonQuest.Bootstrap;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    // Scene entry point: keep this component and its GUID stable for existing scenes.
    public sealed class GameEntryPoint : MonoBehaviour
    {
        private GameBootstrap bootstrap;

        private void Awake()
        {
            bootstrap = new GameBootstrap(gameObject);
            bootstrap.Initialize();
        }

        private void OnDestroy() => bootstrap?.Dispose();
    }
}
