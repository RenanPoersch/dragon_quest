using System;
using UnityEngine;

// Define dois modos de carregar recursos tipados pela pasta Resources do Unity.
namespace DragonQuest.Infrastructure
{
    public static class GameAssets
    {
        // Falha explicitamente se um recurso indispensável estiver ausente.
        public static T Required<T>(string path) where T : UnityEngine.Object
        {
            T asset = Optional<T>(path);
            if (asset == null) throw new InvalidOperationException("Recurso obrigatório ausente (" + typeof(T).Name + "): " + path);
            return asset;
        }

        // Permite que o chamador trate a ausência de um recurso que tem alternativa.
        public static T Optional<T>(string path) where T : UnityEngine.Object => Resources.Load<T>(path);
    }
}
