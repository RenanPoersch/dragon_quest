using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeSaveController : MonoBehaviour
    {
        private enum MenuPage { Title, NewGame, Continue }
        private MenuPage page = MenuPage.Title;
        private int selectedOption;
        private SaveStore store;
        private PrototypeBattleController battle;
        private PrototypeShopController shops;
        private PrototypeLocations locations;
        private PlayerMovement player;
        private DialogueController dialogue;
        private ExplorationProgress progress;
        private AncientSecrets secrets;
        private DragonProgress dragon;
        private Action<RestoredJourney> install;
        private readonly RestoredJourney[] previews = new RestoredJourney[SaveStore.SlotCount];
        private readonly string[] slotMessages = new string[SaveStore.SlotCount];
        private readonly bool[] occupied = new bool[SaveStore.SlotCount];
        private Texture2D pixel;
        private Texture2D scaleSprite;
        private GUIStyle titleStyle, textStyle, smallStyle, buttonStyle, titleButtonStyle;
        private string message;
        private float messageUntil;
        private int overwriteSlot;
        public bool IsMenuVisible { get; private set; } = true;
        public int ActiveSlot { get; private set; }

        public void Initialize(PlayerMovement hero, PrototypeBattleController combat, PrototypeShopController vendors,
            PrototypeLocations maps, DialogueController conversations, ExplorationProgress exploration,
            AncientSecrets ancient, DragonProgress dragonProgress, Action<RestoredJourney> installJourney)
        {
            player = hero; battle = combat; shops = vendors; locations = maps; dialogue = conversations;
            progress = exploration; secrets = ancient; dragon = dragonProgress; install = installJourney;
            store = new SaveStore(System.IO.Path.Combine(Application.persistentDataPath, "Saves"));
            RefreshSlots(); player.SetMovementEnabled(false);
        }
        private void RefreshSlots()
        {
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++)
            {
                occupied[slot - 1] = store.Exists(slot);
                store.TryRead(slot, out previews[slot - 1], out slotMessages[slot - 1]);
            }
        }
        private JourneyData Capture()
        {
            Vector3 position = player.transform.position;
            return JourneySnapshot.Capture(battle.Party, progress, secrets, dragon, shops.Service, shops.Quest,
                locations.LocationId, position.x, position.y, battle.CastleRules?.Ending ?? GameEnding.None);
        }
        private bool Write(string success)
        {
            try
            {
                bool saved = store.TryWrite(ActiveSlot, Capture(), out string error);
                ShowMessage(saved ? success + " / slot " + ActiveSlot : error); return saved;
            }
            catch (Exception ex) { ShowMessage("Nao foi possivel salvar: " + ex.Message); return false; }
        }
        private void NewGame(int slot)
        {
            ActiveSlot = slot;
            overwriteSlot = 0; IsMenuVisible = false; player.SetMovementEnabled(true);
        }
        private void Continue(int slot)
        {
            if (!store.TryRead(slot, out var journey, out string warning)) { ShowMessage(warning); RefreshSlots(); return; }
            install(journey); progress = journey.Progress; ActiveSlot = slot; IsMenuVisible = false;
            player.SetMovementEnabled(true);
            ShowMessage(warning ?? "Jornada carregada / slot " + slot);
            battle.RestoreEnding((GameEnding)journey.Data.Ending);
        }
        public void SaveManual()
        {
            if (IsMenuVisible || battle.IsActive || battle.HasEnded || (locations.LocationId != PrototypeTravel.Inn && locations.LocationId != PrototypeTravel.World))
            { ShowMessage("O salvamento manual fica disponivel no mapa global ou na hospedaria, fora de combate."); return; }
            Write("Jornada salva");
        }
        private void Update()
        {
            if (IsMenuVisible) return;
            if (!IsMenuVisible && !dialogue.IsOpen && !battle.IsBusy && Application.isFocused
                && Keyboard.current?.f5Key.wasPressedThisFrame == true) SaveManual();
        }
        private void OpenPage(MenuPage next)
        {
            page = next; selectedOption = 0; overwriteSlot = 0; message = null;
            if (next != MenuPage.Title) RefreshSlots();
        }
        private void SelectSlot(int slot)
        {
            if (page == MenuPage.Continue)
            {
                if (previews[slot - 1] != null) Continue(slot);
            }
            else if (occupied[slot - 1]) { overwriteSlot = slot; selectedOption = 0; }
            else NewGame(slot);
        }
        private void UpdateMenuInput(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                if (overwriteSlot != 0) overwriteSlot = 0;
                else if (page != MenuPage.Title) OpenPage(MenuPage.Title);
                return;
            }
            int count = page == MenuPage.Title || overwriteSlot != 0 ? 2 : SaveStore.SlotCount + 1;
            if (key == KeyCode.UpArrow || key == KeyCode.W)
                selectedOption = (selectedOption + count - 1) % count;
            if (key == KeyCode.DownArrow || key == KeyCode.S)
                selectedOption = (selectedOption + 1) % count;
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter && key != KeyCode.Space) return;
            if (overwriteSlot != 0)
            {
                if (selectedOption == 0) NewGame(overwriteSlot);
                else overwriteSlot = 0;
            }
            else if (page == MenuPage.Title) OpenPage(selectedOption == 0 ? MenuPage.NewGame : MenuPage.Continue);
            else if (selectedOption == SaveStore.SlotCount) OpenPage(MenuPage.Title);
            else SelectSlot(selectedOption + 1);
        }
        private void ShowMessage(string text)
        { message = text; messageUntil = Time.unscaledTime + 8f; }
        private void OnGUI()
        {
            // Save notices belong to the journey selection menu, never to the game maps.
            if (store == null || !IsMenuVisible) return;
            // Consume menu keys before drawing controls so a focused IMGUI button cannot activate twice.
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return
                || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Space
                || Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow
                || Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.S
                || Event.current.keyCode == KeyCode.Escape))
            {
                UpdateMenuInput(Event.current.keyCode);
                Event.current.Use();
                if (!IsMenuVisible) return;
            }
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix; int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity; GUI.depth = -100;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
            GUI.color = Color.white;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            if (page == MenuPage.Title)
            {
                if (scaleSprite != null)
                    GUI.DrawTexture(new Rect(440, 160, 400, 400), scaleSprite, ScaleMode.ScaleToFit, true);
                if (MenuButton(new Rect(490, 562, 300, 48), "New game", 0, titleButtonStyle)) OpenPage(MenuPage.NewGame);
                if (MenuButton(new Rect(490, 620, 300, 48), "Continue", 1, titleButtonStyle)) OpenPage(MenuPage.Continue);
            }
            else
            {
                bool loading = page == MenuPage.Continue;
                GUI.Label(new Rect(110, 55, 1060, 60), loading ? "Continue" : "New game", titleStyle);
                GUI.Label(new Rect(110, 120, 1060, 48), loading
                    ? "Escolha um dos tres saves para continuar sua jornada."
                    : "Escolha um slot para sua nova jornada.", textStyle);
                bool previousEnabled = GUI.enabled; GUI.enabled = overwriteSlot == 0;
                for (int i = 0; i < SaveStore.SlotCount; i++)
                {
                    float top = 196 + i * 126; var preview = previews[i];
                    GUI.Label(new Rect(110, top, 640, 35), "SLOT " + (i + 1) + " / " + (preview == null ? occupied[i] ? "Indisponivel" : "Vazio" : LocationTitle(preview)), textStyle);
                    GUI.Label(new Rect(110, top + 37, 650, 50), preview == null ? slotMessages[i] : Summary(preview), smallStyle);
                    GUI.enabled = previousEnabled && overwriteSlot == 0 && (!loading || preview != null);
                    if (MenuButton(new Rect(950, top, 210, 45), loading ? "Continue" : "New game", i, buttonStyle)) SelectSlot(i + 1);
                    GUI.enabled = previousEnabled && overwriteSlot == 0;
                }
                if (MenuButton(new Rect(110, 585, 200, 45), "Back", SaveStore.SlotCount, buttonStyle)) OpenPage(MenuPage.Title);
                GUI.enabled = previousEnabled;
                if (overwriteSlot != 0)
                {
                    Panel(new Rect(280, 245, 720, 205));
                    GUI.Label(new Rect(310, 265, 660, 74), "Ao salvar esta nova jornada no slot " + overwriteSlot + ", o progresso anterior sera substituido.", textStyle);
                    if (MenuButton(new Rect(310, 365, 320, 48), "Comecar nova jornada", 0, buttonStyle)) NewGame(overwriteSlot);
                    if (MenuButton(new Rect(650, 365, 320, 48), "Cancelar", 1, buttonStyle)) overwriteSlot = 0;
                }
            }
            if (IsMenuVisible && !string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            {
                Rect rect = new Rect(110, 650, 1060, 50);
                Panel(rect); GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 24, rect.height - 10),
                    message, smallStyle);
            }
            GUI.matrix = previousMatrix; GUI.depth = previousDepth; GUI.color = previousColor;
        }
        private bool MenuButton(Rect rect, string label, int option, GUIStyle style)
        {
            if (GUI.enabled && Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition))
                selectedOption = option;
            Color previous = GUI.color;
            if (GUI.enabled && selectedOption == option) GUI.color = new Color(0.96f, 0.8f, 0.48f);
            bool clicked = GUI.Button(rect, label, style);
            GUI.color = previous;
            return clicked;
        }
        private static string LocationTitle(RestoredJourney journey)
        {
            if (journey.Data.Ending != 0) return EndingNarrative.Title((GameEnding)journey.Data.Ending);
            var navigation = new PrototypeTravel(journey.Secrets);
            navigation.TryRestore(journey.Data.Map, new Vector2(journey.Data.X, journey.Data.Y)); return navigation.Current.Name;
        }
        private static string Summary(RestoredJourney journey) => DateTimeOffset.Parse(journey.Data.SavedUtc).ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            + " / " + journey.Progress.Gold + " moedas / " + journey.Secrets.CompletedCount + " de 3 segredos"
            + (journey.Dragon.IsDefeated ? " / Dragao libertado" : "");
        private void Panel(Rect rect)
        {
            Color previous = GUI.color; GUI.color = new Color(0.04f, 0.065f, 0.1f, 0.99f); GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }
        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            scaleSprite = Resources.Load<Texture2D>("PixelArt/DragonScale");
            titleStyle = new GUIStyle(GUI.skin.label) {fontSize = 38, fontStyle = FontStyle.Bold}; titleStyle.normal.textColor = new Color(0.96f, 0.8f, 0.48f);
            textStyle = new GUIStyle(GUI.skin.label) {fontSize = 20, wordWrap = true}; textStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(textStyle) {fontSize = 16}; buttonStyle = new GUIStyle(GUI.skin.button) {fontSize = 18};
            titleButtonStyle = new GUIStyle(GUI.skin.label) {fontSize = 28, alignment = TextAnchor.MiddleCenter};
            titleButtonStyle.normal.textColor = Color.white;
            titleButtonStyle.hover.textColor = new Color(0.96f, 0.8f, 0.48f);
            titleButtonStyle.active.textColor = new Color(0.96f, 0.8f, 0.48f);
            titleButtonStyle.focused.textColor = Color.white;
        }
        private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
