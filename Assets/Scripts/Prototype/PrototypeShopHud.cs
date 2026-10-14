using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Shops;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeShopHud : MonoBehaviour
    {
        private PrototypeShopController controller;
        private Texture2D pixel;
        private GUIStyle title, text, small, button, iconButton;
        private ShopInventory observedShop;
        private ShopProduct selectedProduct;
        private SaleEntry selectedSale;
        private bool selling;
        private int quantity = 1;
        private Vector2 listScroll;
        public void Initialize(PrototypeShopController shopController) => controller = shopController;

        private void OnGUI()
        {
            if (controller == null || !controller.IsOpen) { observedShop = null; return; }
            EnsureStyles();
            ShopInventory shop = controller.ActiveShop;
            if (observedShop != shop) { observedShop = shop; selling = false; ResetSelection(); }
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.045f, 0.07f));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Panel(new Rect(30, 20, 1220, 90));
            GUI.Label(new Rect(48, 32, 700, 32), shop.Name.ToUpperInvariant(), title);
            GUI.Label(new Rect(48, 70, 700, 26), "Ouro: " + controller.Service.Progress.Gold + "  |  Venda por 50% do valor de compra", small);
            if (GUI.Button(new Rect(1000, 40, 220, 40), "Fechar / Esc", button)) controller.Close();
            if (GUI.Button(new Rect(30, 125, 240, 42), (!selling ? "> " : "") + "Comprar", button)) { selling = false; ResetSelection(); }
            if (GUI.Button(new Rect(285, 125, 240, 42), (selling ? "> " : "") + "Vender", button)) { selling = true; ResetSelection(); }
            string questAction = controller.Quest.ActionLabel(shop.Id);
            GUI.Label(new Rect(552, 119, questAction == null ? 675 : 405, 58), controller.Quest.ContextText(shop.Id), small);
            if (questAction != null && GUI.Button(new Rect(972, 125, 278, 42), questAction, button)) controller.InteractQuest();
            Panel(new Rect(30, 182, 595, 435));
            Panel(new Rect(645, 182, 605, 435));
            if (selling) DrawSales(shop); else DrawProducts(shop);
            DrawDetails();
            Panel(new Rect(30, 635, 1220, 65));
            GUI.Label(new Rect(48, 646, 1180, 46), controller.Message, small);
            GUI.matrix = previous;
        }

        private void ResetSelection() { selectedProduct = null; selectedSale = null; quantity = 1; listScroll = Vector2.zero; }
        private void DrawProducts(ShopInventory shop)
        {
            var products = new List<ShopProduct>();
            foreach (ShopProduct product in shop.Products) if (product.InitialStock > 0) products.Add(product);
            GUI.Label(new Rect(48, 192, 540, 30), "Catalogo / estoque limitado", title);
            listScroll = GUI.BeginScrollView(new Rect(45, 238, 565, 357), listScroll, new Rect(0, 0, 532, products.Count * 55));
            for (int i = 0; i < products.Count; i++)
            {
                ShopProduct product = products[i];
                if (GUI.Button(new Rect(0, i * 55, 526, 49), (selectedProduct == product ? "> " : "") + product.Name
                    + "\n" + product.Price + " moedas / " + (product.Stock == 0 ? "ESGOTADO" : "Estoque: " + product.Stock), iconButton))
                { selectedProduct = product; quantity = 1; }
                InventoryPixelArt.Draw(new Rect(7, i * 55 + 6, 36, 36), ProductTexture(product));
            }
            GUI.EndScrollView();
        }

        private void DrawSales(ShopInventory shop)
        {
            var entries = controller.Service.SaleEntries(shop);
            if (selectedSale != null)
            {
                SaleEntry fresh = null;
                foreach (SaleEntry entry in entries)
                    if (SameObject(entry, selectedSale)) fresh = entry;
                selectedSale = fresh;
            }
            GUI.Label(new Rect(48, 192, 540, 30), "Seus objetos / categoria desta loja", title);
            listScroll = GUI.BeginScrollView(new Rect(45, 238, 565, 357), listScroll, new Rect(0, 0, 532, entries.Count * 60));
            for (int i = 0; i < entries.Count; i++)
            {
                SaleEntry entry = entries[i];
                string location = entry.Equipment != null ? controller.Service.Party.LocationOf(entry.Equipment)
                    : entry.Materia != null ? controller.Service.Party.LocationOf(entry.Materia) : "Inventario";
                if (GUI.Button(new Rect(0, i * 60, 526, 54), (selectedSale != null && SameObject(entry, selectedSale) ? "> " : "")
                    + entry.Name + " / x" + entry.Quantity + " / " + location + "\n" + (entry.CanSell ? entry.UnitPrice + " moedas cada" : entry.BlockReason), iconButton))
                { selectedSale = entry; quantity = 1; }
                InventoryPixelArt.Draw(new Rect(7, i * 60 + 9, 36, 36), SaleTexture(entry));
            }
            GUI.EndScrollView();
            if (entries.Count == 0) GUI.Label(new Rect(48, 249, 530, 100), "Nao ha objetos desta categoria no seu inventario.", text);
        }
        private static bool SameObject(SaleEntry one, SaleEntry two) => one.Item != null && one.Item == two.Item
            || one.Equipment != null && one.Equipment == two.Equipment || one.Materia != null && one.Materia == two.Materia;

        private static Texture2D ProductTexture(ShopProduct product) => product.Item != null ? InventoryPixelArt.Texture(product.Item)
            : product.Equipment != null ? InventoryPixelArt.Texture(product.Equipment) : InventoryPixelArt.Texture(product.Materia);
        private static Texture2D SaleTexture(SaleEntry entry) => entry.Item != null ? InventoryPixelArt.Texture(entry.Item)
            : entry.Equipment != null ? InventoryPixelArt.Texture(entry.Equipment.Definition) : InventoryPixelArt.Texture(entry.Materia?.Definition);

        private void DrawDetails()
        {
            bool selected = selling ? selectedSale != null : selectedProduct != null;
            GUI.Label(new Rect(665, 192, 555, 35), !selected ? "Escolha um objeto" : selling ? selectedSale.Name : selectedProduct.Name, title);
            if (selected) InventoryPixelArt.Draw(new Rect(1128, 242, 88, 88), selling ? SaleTexture(selectedSale) : ProductTexture(selectedProduct));
            GUI.Label(new Rect(665, 242, selected ? 447 : 555, 125), !selected ? "Confira descricao, quantidade e valor antes de confirmar a transacao. Equipamentos e materias em uso precisam ser desequipados para venda."
                : selling ? selectedSale.Description : selectedProduct.Description, text);
            if (!selected) return;
            int price = selling ? selectedSale.UnitPrice : selectedProduct.Price;
            int maximum = selling ? selectedSale.Quantity : selectedProduct.Stock;
            quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, maximum));
            GUI.Label(new Rect(665, 383, 550, 28), "Quantidade: " + quantity + " / Disponivel: " + maximum, text);
            bool enabled = GUI.enabled;
            GUI.enabled = quantity > 1;
            if (GUI.Button(new Rect(665, 421, 65, 38), "-", button)) quantity--;
            GUI.enabled = maximum > quantity;
            if (GUI.Button(new Rect(742, 421, 65, 38), "+", button)) quantity++;
            GUI.enabled = maximum > 0;
            if (GUI.Button(new Rect(819, 421, 180, 38), selling ? "Todas as unidades" : "Maximo acessivel", button))
                quantity = selling ? maximum : Mathf.Max(1, Mathf.Min(maximum, controller.Service.Progress.Gold / price));
            GUI.enabled = enabled;
            long total = (long)price * quantity;
            GUI.Label(new Rect(665, 477, 550, 30), (selling ? "Receber: " : "Pagar: ") + total + " moedas", text);
            string reason = selling ? selectedSale.BlockReason : maximum == 0 ? "Produto esgotado."
                : total > controller.Service.Progress.Gold ? "Ouro insuficiente." : null;
            GUI.Label(new Rect(665, 517, 550, 40), reason ?? (selling ? "Objetos vendidos saem do inventario." : "Compra enviada ao inventario. Use I depois de fechar a loja."), small);
            GUI.enabled = maximum > 0 && string.IsNullOrEmpty(reason);
            if (GUI.Button(new Rect(665, 567, 300, 35), selling ? "Confirmar venda" : "Confirmar compra", button))
            {
                if (selling) controller.Sell(selectedSale, quantity); else controller.Buy(selectedProduct, quantity);
                quantity = 1;
            }
            GUI.enabled = enabled;
            if (GUI.Button(new Rect(986, 567, 230, 35), "Cancelar selecao", button)) ResetSelection();
        }

        private void Panel(Rect rect) => Box(rect, new Color(0.075f, 0.12f, 0.18f));
        private void Box(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }
        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            iconButton = InventoryPixelArt.ButtonStyle(button);
            title.normal.textColor = new Color(0.96f, 0.8f, 0.48f); text.normal.textColor = Color.white;
            small.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }
        private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
