using DragonQuest.Presentation.World;
using DragonQuest.Gameplay.Shops;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Presentation.Menus;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public sealed class InventoryHud : MonoBehaviour
    {
        public InventoryMenuPresenter Presenter { get; private set; }
        public void Initialize(BattleController battleController, AncientSecrets ancientSecrets)
        {
            Presenter?.Dispose();
            Presenter = new InventoryMenuPresenter(battleController, ancientSecrets,
                () => GetComponent<SaveController>(),
                () => GetComponent<ShopController>()?.Service?.Progress.Gold ?? 0,
                () => GetComponent<WorldLocationController>()?.LocationName ?? "ILHA / MAPA GLOBAL");
        }
        private void Update() => Presenter?.Update();
        private void OnGUI() => Presenter?.Draw();
        private void OnDestroy() => Presenter?.Dispose();
    }
}
