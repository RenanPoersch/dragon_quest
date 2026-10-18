using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeShopHud : MonoBehaviour
    {
        private PrototypeShopController controller;
        private InventoryMenuPresentation theme;
        private readonly ShopMenuPresentation presentation = new ShopMenuPresentation();
        private bool wasOpen;
        public void Initialize(PrototypeShopController shopController) => controller = shopController;
        private void OnGUI()
        {
            if (controller == null || !controller.IsOpen) { wasOpen = false; return; }
            if (theme == null) theme = new InventoryMenuPresentation();
            if (!wasOpen) { presentation.Reset(); wasOpen = true; }
            Color color = GUI.color; GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 matrix = GUI.matrix;
            bool enabled = GUI.enabled;
            try
            {
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                    (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.enabled = enabled && !controller.IsInputBlocked;
                presentation.Draw(theme, controller.Menu, GUI.enabled, controller.ActionIndex,
                    controller.ChooseAction, controller.Back);
            }
            finally { GUI.matrix = matrix; GUI.enabled = enabled; }
        }
        private void OnDestroy() { theme?.Dispose(); }
    }
}
