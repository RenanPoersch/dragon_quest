using DragonQuest.Presentation.Menus;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeMateriaHud : MonoBehaviour
    {
        public InventoryMenuPresenter Presenter { get; private set; }
        public void Initialize(PrototypeBattleController battleController, AncientSecrets ancientSecrets)
        {
            Presenter?.Dispose();
            Presenter = new InventoryMenuPresenter(battleController, ancientSecrets,
                () => GetComponent<PrototypeSaveController>(),
                () => GetComponent<PrototypeShopController>()?.Service?.Progress.Gold ?? 0,
                () => GetComponent<PrototypeLocations>()?.LocationName ?? "ILHA / MAPA GLOBAL");
        }
        private void Update() => Presenter?.Update();
        private void OnGUI() => Presenter?.Draw();
        private void OnDestroy() => Presenter?.Dispose();
    }
}
