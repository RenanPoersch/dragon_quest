using DragonQuest.Presentation.World.Characters;
using DragonQuest.Exploration.Layouts;
using UnityEngine;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;

namespace DragonQuest.Presentation.World
{
    // Alterna ambientes do versão inicial sem recriar o herói ou o estado da sessão.
    public sealed class WorldLocationController : MonoBehaviour
    {
        private Dictionary<string, GameObject> mapRoots;
        private WorldNavigation navigation;
        private PlayerMovement player;
        private CameraFollow cameraFollow;
        private ThroneRoomDepth throneDepth;
        private System.Func<bool> travelBlocked;

        public string LocationId => navigation?.Current.Id ?? WorldNavigation.World;
        public bool IsInShop => navigation?.Current.IsShop ?? false;
        public bool IsInCity => LocationId == WorldNavigation.City;
        public string LocationName => IsInCity && player != null ? "PERIFERIA / " + CityLayout.DistrictAt(player.transform.position)
            : navigation?.Current.Name ?? "ILHA / MAPA GLOBAL";

        public void Initialize(Dictionary<string, GameObject> roots,
            PlayerMovement hero, CameraFollow camera, WorldNavigation travel, System.Func<bool> blocked)
        {
            mapRoots = roots; player = hero; cameraFollow = camera;
            navigation = travel; travelBlocked = blocked;
            throneDepth=player.gameObject.AddComponent<ThroneRoomDepth>(); throneDepth.enabled=false;
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
            if (navigation.TryEnter(player.transform.position, travelBlocked(), out Vector2 arrival))
                ApplyMap(arrival);
        }

        public void RecoverAtInn()
        {
            if (navigation.TryRecoverAtInn(out Vector2 arrival)) ApplyMap(arrival);
        }

        public void Restore(string map, Vector2 position)
        {
            if (!navigation.TryRestore(map, position)) throw new System.ArgumentException("Posicao de save invalida.");
            mapRoots[WorldNavigation.World].GetComponentInChildren<BlockedPassageInteraction>(true)?.RestoreBarrierState();
            if (map == WorldNavigation.City) position = CityLayout.SafeSavedPosition(position);
            if (map == WorldNavigation.Ruins) position = RuinsLayout.SafeSavedPosition(position);
            if (map == WorldNavigation.Volcano) position = VolcanoLayout.SafeSavedPosition(position);
            if (map == WorldNavigation.EastForest) position = EastForestLayout.SafeSavedPosition(position);
            if (map == WorldNavigation.WestForest) position = QuarryLayout.SafeSavedPosition(position);
            if (map == WorldNavigation.World) position = WorldLayout.SafeSavedPosition(position,
                navigation.CanEnterVolcano);
            // Saves de lojas reaparecem na entrada livre, inclusive os da planta antiga.
            if (navigation.Current.IsShop) position = navigation.Current.Spawn;
            ApplyMap(position);
        }

        private void ApplyMap(Vector2 arrival)
        {
            foreach (var root in mapRoots) root.Value.SetActive(root.Key == LocationId);
            bool volcano = LocationId == WorldNavigation.Volcano;
            bool lateral = LocationId == WorldNavigation.Ruins || volcano;
            bool forest = LocationId == WorldNavigation.EastForest;
            bool quarry = LocationId == WorldNavigation.WestForest;
            bool inn = LocationId == WorldNavigation.Inn;
            bool weapons = LocationId == WeaponShopLayout.MapId;
            bool throne = LocationId == WorldNavigation.Castle;
            CompactShopLayout compact=CompactShopLayout.Find(LocationId);
            player.SetMovementConstraint(throne ? ThroneRoomLayout.ConstrainMove : compact != null ? compact.ConstrainMove : weapons ? WeaponShopLayout.ConstrainMove : inn ? InnLayout.ConstrainMove : forest ? EastForestLayout.ConstrainMove : quarry ? QuarryLayout.ConstrainMove : (System.Func<Vector2,Vector2,Vector2>)null);
            if (throne) arrival=ThroneRoomLayout.SafeSavedPosition(arrival);
            player.SetFrontalView(throne);
            player.SetWalkingSpeedMultiplier(throne || lateral ? .64f : 1f);
            throneDepth.enabled=throne;
            cameraFollow.SetPerspectiveView(throne,ThroneRoomLayout.CameraPosition,ThroneRoomLayout.CameraRotation,
                ThroneRoomLayout.FieldOfView,ThroneRoomLayout.ArtAspect);
            if (compact != null) arrival=compact.SafeSavedPosition(arrival);
            if (weapons) arrival = WeaponShopLayout.SafeSavedPosition(arrival);
            if (LocationId == WorldNavigation.Inn) arrival = InnLayout.SafeSavedPosition(arrival);
            if (quarry) arrival = QuarryLayout.SafeSavedPosition(arrival);
            if (forest) arrival=EastForestLayout.SafeSavedPosition(arrival);
            if (volcano) arrival = VolcanoLayout.SafeSavedPosition(arrival);
            player.SetLateralPath(volcano ? VolcanoLayout.FloorAt
                : lateral ? RuinsLayout.FloorAt : (System.Func<float, float>)null,
                volcano ? VolcanoLayout.MinX : RuinsLayout.MinX,
                volcano ? VolcanoLayout.MaxX : RuinsLayout.MaxX,
                volcano ? VolcanoLayout.WalkingSpeedMultiplier : RuinsLayout.WalkingSpeedMultiplier);
            Transform heroShadow = player.transform.Find("Sombra no chao");
            if (heroShadow != null) heroShadow.gameObject.SetActive(!lateral);
            cameraFollow.SetSideView(lateral, volcano ? VolcanoLayout.CameraHeight : RuinsLayout.CameraHeight);
            cameraFollow.SetArtworkFrame(forest || quarry || inn || weapons || compact != null, inn || weapons || compact != null,
                compact != null ? CompactShopLayout.ArtAspect : weapons ? WeaponShopLayout.ArtAspect : inn ? InnLayout.ArtAspect : 16f / 9f);
            player.Teleport(arrival);
            if (volcano) player.FaceTowards(arrival + Vector2.right);
            if(throne) throneDepth.Refresh();
            cameraFollow.Initialize(player.transform, (forest || quarry) ? WorldLayout.ForestBounds : navigation.Current.Bounds, navigation.Current.CameraSize,
                navigation.Current.FixedCamera, navigation.Current.CoveredGroundBounds);
            bool singleRoom = navigation.Current.IsShop || LocationId == WorldNavigation.Inn || LocationId == WorldNavigation.Castle;
            cameraFollow.GetComponent<Camera>().backgroundColor = singleRoom ? Color.black
                : lateral ? new Color(.22f, .24f, .25f)
                : LocationId == WorldNavigation.World ? new Color(0.19f, 0.37f, 0.49f) : new Color(0.15f, 0.24f, 0.21f);
            Physics2D.SyncTransforms();
        }
    }
}
