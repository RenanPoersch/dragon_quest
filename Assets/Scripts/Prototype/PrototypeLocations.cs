using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;

namespace DragonQuest.Prototype
{
    // Alterna ambientes do protótipo sem recriar o herói ou o estado da sessão.
    public sealed class PrototypeLocations : MonoBehaviour
    {
        private Dictionary<string, GameObject> mapRoots;
        private PrototypeTravel navigation;
        private PlayerMovement player;
        private CameraFollow cameraFollow;
        private System.Func<bool> travelBlocked;

        public string LocationId => navigation?.Current.Id ?? PrototypeTravel.World;
        public bool IsInShop => navigation?.Current.IsShop ?? false;
        public bool IsInCity => LocationId == PrototypeTravel.City;
        public string LocationName => IsInCity && player != null ? "PERIFERIA / " + PrototypeCityLayout.DistrictAt(player.transform.position)
            : navigation?.Current.Name ?? "ILHA / MAPA GLOBAL";

        public void Initialize(Dictionary<string, GameObject> roots,
            PlayerMovement hero, CameraFollow camera, PrototypeTravel travel, System.Func<bool> blocked)
        {
            mapRoots = roots; player = hero; cameraFollow = camera;
            navigation = travel; travelBlocked = blocked;
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

        private void Update()
        {
            if (navigation == null || player == null) return;
            if (navigation.TryEnter(player.transform.position, !Application.isFocused || travelBlocked(), out Vector2 arrival))
                ApplyMap(arrival);
        }

        public void RecoverAtInn()
        {
            if (navigation.TryRecoverAtInn(out Vector2 arrival)) ApplyMap(arrival);
        }

        public void Restore(string map, Vector2 position)
        {
            if (!navigation.TryRestore(map, position)) throw new System.ArgumentException("Posicao de save invalida.");
            mapRoots[PrototypeTravel.World].GetComponentInChildren<BlockedPassageInteraction>(true)?.RestoreBarrierState();
            if (map == PrototypeTravel.EastForest) position = EastForestLayout.SafeSavedPosition(position);
            if (map == PrototypeTravel.WestForest) position = QuarryLayout.SafeSavedPosition(position);
            if (map == PrototypeTravel.City) position = PrototypeCityLayout.SafeSavedPosition(position);
            if (map == PrototypeTravel.Ruins) position = PrototypeRuinsLayout.SafeSavedPosition(position);
            if (map == PrototypeTravel.Volcano) position = PrototypeVolcanoLayout.SafeSavedPosition(position);
            if (map == PrototypeTravel.World) position = PrototypeWorldLayout.SafeSavedPosition(position,
                navigation.CanEnterVolcano);
            ApplyMap(position);
        }

        private void ApplyMap(Vector2 arrival)
        {
            foreach (var root in mapRoots) root.Value.SetActive(root.Key == LocationId);
            bool volcano = LocationId == PrototypeTravel.Volcano;
            bool lateral = LocationId == PrototypeTravel.Ruins || volcano;
            bool forest=LocationId==PrototypeTravel.EastForest;
            bool quarry = LocationId == PrototypeTravel.WestForest;
            player.SetMovementConstraint(forest ? EastForestLayout.ConstrainMove : quarry ? QuarryLayout.ConstrainMove : (System.Func<Vector2,Vector2,Vector2>)null);
            if (quarry) arrival = QuarryLayout.SafeSavedPosition(arrival);
            if(forest) arrival=EastForestLayout.SafeSavedPosition(arrival);
            if (volcano) arrival = PrototypeVolcanoLayout.SafeSavedPosition(arrival);
            player.SetLateralPath(volcano ? PrototypeVolcanoLayout.FloorAt
                : lateral ? PrototypeRuinsLayout.FloorAt : (System.Func<float, float>)null,
                volcano ? PrototypeVolcanoLayout.MinX : PrototypeRuinsLayout.MinX,
                volcano ? PrototypeVolcanoLayout.MaxX : PrototypeRuinsLayout.MaxX,
                volcano ? PrototypeVolcanoLayout.WalkingSpeedMultiplier : PrototypeRuinsLayout.WalkingSpeedMultiplier);
            Transform heroShadow = player.transform.Find("Sombra no chao");
            if (heroShadow != null) heroShadow.gameObject.SetActive(!lateral);
            cameraFollow.SetSideView(lateral, volcano ? PrototypeVolcanoLayout.CameraHeight : PrototypeRuinsLayout.CameraHeight);
            cameraFollow.SetArtworkFrame(forest || quarry);
            player.Teleport(arrival);
            cameraFollow.Initialize(player.transform, (forest || quarry) ? PrototypeWorldLayout.ForestBounds : navigation.Current.Bounds, navigation.Current.CameraSize,
                navigation.Current.FixedCamera, navigation.Current.CoveredGroundBounds);
            cameraFollow.GetComponent<Camera>().backgroundColor = lateral ? new Color(.22f, .24f, .25f) : LocationId == PrototypeTravel.World
                ? new Color(0.19f, 0.37f, 0.49f) : new Color(0.15f, 0.24f, 0.21f);
            Physics2D.SyncTransforms();
        }
    }
}
