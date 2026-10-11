using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Alterna ambientes do protótipo sem recriar o herói ou o estado da sessão.
    public sealed class PrototypeLocations : MonoBehaviour
    {
        private Dictionary<string, GameObject> mapRoots;
        private PrototypeTravel navigation;
        private PlayerMovement player;
        private CameraFollow cameraFollow;

        public string LocationId => navigation?.Current.Id ?? PrototypeTravel.World;
        public bool IsInShop => navigation?.Current.IsShop ?? false;
        public bool IsInCity => LocationId == PrototypeTravel.City;
        public string LocationName => IsInCity && player != null ? "PERIFERIA / " + PrototypeCityLayout.DistrictAt(player.transform.position)
            : navigation?.Current.Name ?? "ILHA / MAPA GLOBAL";

        public void Initialize(Dictionary<string, GameObject> roots,
            PlayerMovement hero, CameraFollow camera)
        {
            mapRoots = roots; player = hero; cameraFollow = camera;
            navigation = new PrototypeTravel();
            foreach (MapDefinition map in navigation.Maps)
                if (!mapRoots.ContainsKey(map.Id) || mapRoots[map.Id] == null)
                    throw new System.ArgumentException("Cenario ausente: " + map.Id);
            ApplyMap(navigation.Current.Spawn);
        }

        public void TravelTo(string locationId)
        {
            if (!navigation.TryTravel(locationId, out Vector2 arrival)) return;
            ApplyMap(arrival);
        }

        private void ApplyMap(Vector2 arrival)
        {
            foreach (var root in mapRoots) root.Value.SetActive(root.Key == LocationId);
            player.Teleport(arrival);
            cameraFollow.Initialize(player.transform, navigation.Current.Bounds, navigation.Current.CameraSize, navigation.Current.FixedCamera);
            cameraFollow.GetComponent<Camera>().backgroundColor = LocationId == PrototypeTravel.World
                ? new Color(0.19f, 0.37f, 0.49f) : new Color(0.15f, 0.24f, 0.21f);
            Physics2D.SyncTransforms();
        }
    }
}
