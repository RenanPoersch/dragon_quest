using UnityEngine;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    // Alterna ambientes do protótipo sem recriar o herói ou o estado da sessão.
    public sealed class PrototypeLocations : MonoBehaviour
    {
        private GameObject city;
        private GameObject shop;
        private PlayerMovement player;
        private CameraFollow cameraFollow;

        public bool IsInShop { get; private set; }
        public string LocationName => IsInShop ? "LOJA DE ITENS" : "CIDADE";

        public void Initialize(GameObject cityRoot, GameObject shopRoot, PlayerMovement hero, CameraFollow camera)
        {
            city = cityRoot;
            shop = shopRoot;
            player = hero;
            cameraFollow = camera;
        }

        public void TravelTo(string locationId)
        {
            if (locationId != "city" && locationId != "item-shop") return;
            IsInShop = locationId == "item-shop";
            city.SetActive(!IsInShop);
            shop.SetActive(IsInShop);
            player.Teleport(IsInShop ? new Vector2(0, -2.7f) : new Vector2(-9, 1.7f));
            Rect bounds = IsInShop ? new Rect(-7, -5, 14, 10) : new Rect(-18, -12, 36, 24);
            cameraFollow.Initialize(player.transform, bounds, IsInShop ? 4f : 6f);
            Physics2D.SyncTransforms();
        }
    }
}
