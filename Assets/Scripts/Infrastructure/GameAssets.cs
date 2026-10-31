using System;
using UnityEngine;

namespace DragonQuest.Infrastructure
{
    public static class GameAssets
    {
        public static T Required<T>(string path) where T : UnityEngine.Object
        {
            T asset = Optional<T>(path);
            if (asset == null) throw new InvalidOperationException("Recurso obrigatório ausente (" + typeof(T).Name + "): " + path);
            return asset;
        }

        public static T Optional<T>(string path) where T : UnityEngine.Object => Resources.Load<T>(path);
    }
}
