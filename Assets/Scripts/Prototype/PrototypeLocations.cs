using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Alterna ambientes do protótipo sem recriar o herói ou o estado da sessão.
    public sealed class PrototypeLocations : MonoBehaviour
    {
        private GameObject city;
        private Dictionary<string, GameObject> shops;
        private Dictionary<string, Vector2> exits;
        private PlayerMovement player;
        private CameraFollow cameraFollow;

        public string LocationId { get; private set; } = "city";
        public bool IsInShop => LocationId != "city";
        public string LocationName => LocationId == "item-shop" ? "LOJA DE ITENS" : LocationId == "equipment-shop" ? "LOJA DE EQUIPAMENTOS"
            : LocationId == "materia-shop" ? "LOJA DE MATERIAS" : player != null ? PrototypeCityLayout.DistrictAt(player.transform.position) : "CIDADE";

        public void Initialize(GameObject cityRoot, Dictionary<string, GameObject> shopRoots, Dictionary<string, Vector2> outsideExits,
            PlayerMovement hero, CameraFollow camera)
        {
            city = cityRoot;
            shops = shopRoots; exits = outsideExits;
            player = hero;
            cameraFollow = camera;
        }

        public void TravelTo(string locationId)
        {
            if (locationId != "city" && !shops.ContainsKey(locationId) || locationId == LocationId) return;
            Vector2 exit = IsInShop ? exits[LocationId] : PrototypeCityLayout.Spawn;
            LocationId = locationId;
            city.SetActive(!IsInShop);
            foreach (var shop in shops) shop.Value.SetActive(shop.Key == LocationId);
            player.Teleport(IsInShop ? new Vector2(0, -2.7f) : exit);
            Rect bounds = IsInShop ? new Rect(-7, -5, 14, 10) : PrototypeCityLayout.Bounds;
            cameraFollow.Initialize(player.transform, bounds, IsInShop ? 4f : 6f);
            Physics2D.SyncTransforms();
        }
    }
}
