using System;
using UnityEngine;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeSaveController : MonoBehaviour
    {
        private enum MenuPage { Title, Save, Continue }
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
        private Texture2D hand;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private GUIStyle titleStyle, textStyle, smallStyle, buttonStyle, titleButtonStyle;
        private string message;
        private float messageUntil;
        private int overwriteSlot;
        private bool awaitingFirstSave;
        private double playSeconds;
        private SaveSelectionPresentation saveScreen;
        private readonly EndingAchievementPresentation achievementScreen = new EndingAchievementPresentation();
        private PrototypeIntroPresentation intro;
        private NewGameVoyagePresentation voyage;
        private const float MenuFadeInDuration = 1.2f;
        private const float NewGameFadeOutDuration = 1.2f;
        private bool startingNewGame, newGameBlackHasDrawn;
        private float newGameFadeOutElapsed;
        private float menuFadeInElapsed;
        private bool menuHasDrawn;
        private bool IsMenuFadingIn => menuFadeInElapsed < MenuFadeInDuration;
        public bool IsMenuVisible { get; private set; } = true;
        public bool IsIntroPlaying => IsMenuVisible && intro != null && intro.IsPlaying;
        public bool IsVoyagePlaying => IsMenuVisible && voyage != null && voyage.IsPlaying;
        public bool IsFrontEndVisible => IsMenuVisible && page != MenuPage.Save;
        public float IntroMusicGain => IsIntroPlaying ? intro.Sequence.Opacity : 0;
        public int ActiveSlot { get; private set; }
        public long PlaySeconds => (long)playSeconds;

        public void Initialize(PlayerMovement hero, PrototypeBattleController combat, PrototypeShopController vendors,
            PrototypeLocations maps, DialogueController conversations, ExplorationProgress exploration,
            AncientSecrets ancient, DragonProgress dragonProgress, Action<RestoredJourney> installJourney)
        {
            player = hero; battle = combat; shops = vendors; locations = maps; dialogue = conversations;
            progress = exploration; secrets = ancient; dragon = dragonProgress; install = installJourney;
            store = new SaveStore(System.IO.Path.Combine(Application.persistentDataPath, "Saves"));
            RefreshSlots(); player.SetMovementEnabled(false);
            intro = new PrototypeIntroPresentation();
        }
        private void RefreshSlots()
        {
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++)
            {
                occupied[slot - 1] = store.Exists(slot);
                store.TryRead(slot, out previews[slot - 1], out slotMessages[slot - 1]);
            }
            if (!OptionEnabled(selectedOption)) MoveSelection(1);
        }
        private JourneyData Capture()
        {
            Vector3 position = player.transform.position;
            JourneyData snapshot = JourneySnapshot.Capture(battle.Party, progress, secrets, dragon, shops.Service,
                locations.LocationId, position.x, position.y, battle.CastleRules?.Ending ?? GameEnding.None);
            snapshot.PlaySeconds = (long)playSeconds;
            return snapshot;
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
        private void NewGame()
        {
            if (voyage != null || startingNewGame) return;
            startingNewGame = true;
            newGameFadeOutElapsed = 0;
            newGameBlackHasDrawn = false;
        }
        private void StartNewGameVoyage()
        {
            voyage = new NewGameVoyagePresentation();
            startingNewGame = false;
            ActiveSlot = 0;
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++)
                if (!store.Exists(slot)) { ActiveSlot = slot; break; }
            awaitingFirstSave = true;
            playSeconds = 0;
            overwriteSlot = 0;
            // Keep every existing menu/busy gate active through the voyage and map reveal.
            player.SetMovementEnabled(false);
        }
        private void Continue(int slot)
        {
            if (!store.TryRead(slot, out var journey, out string warning)) { ShowMessage(warning); RefreshSlots(); return; }
            install(journey); progress = journey.Progress; ActiveSlot = slot; IsMenuVisible = false;
            awaitingFirstSave = false;
            playSeconds = journey.Data.PlaySeconds ?? 0;
            player.SetMovementEnabled(true);
            ShowMessage(warning ?? "Jornada carregada / slot " + slot);
            battle.RestoreEnding((GameEnding)journey.Data.Ending);
        }
        public void SaveManual()
        {
            if (IsMenuVisible || battle.IsActive || battle.HasEnded || (locations.LocationId != PrototypeTravel.Inn && locations.LocationId != PrototypeTravel.World))
            { ShowMessage("O salvamento manual fica disponivel no mapa global ou na hospedaria, fora de combate."); return; }
            if (ActiveSlot == 0 || awaitingFirstSave && store.Exists(ActiveSlot))
            {
                IsMenuVisible = true;
                player.SetMovementEnabled(false);
                OpenPage(MenuPage.Save);
                return;
            }
            if (Write("Jornada salva")) awaitingFirstSave = false;
        }
        public void OpenInnSaveMenu()
        {
            if (IsMenuVisible || dialogue.IsOpen || battle.IsBusy || locations.LocationId != PrototypeTravel.Inn) return;
            IsMenuVisible = true;
            player.SetMovementEnabled(false);
            OpenPage(MenuPage.Save);
        }
        private void SaveToSlot(int slot)
        {
            int previousSlot = ActiveSlot;
            ActiveSlot = slot;
            if (Write("Jornada salva"))
            {
                awaitingFirstSave = false;
                CloseSaveMenu();
            }
            else ActiveSlot = previousSlot;
        }
        private void CloseSaveMenu()
        {
            overwriteSlot = 0;
            IsMenuVisible = false;
            player.SetMovementEnabled(!dialogue.IsOpen && !battle.IsBusy);
        }
        private void Back()
        {
            if (page == MenuPage.Save) CloseSaveMenu();
            else OpenPage(MenuPage.Title);
        }
        private void Update()
        {
            if (store == null) return;
            intro?.Update(Time.unscaledDeltaTime);
            bool titleVisible = IsMenuVisible && page == MenuPage.Title && voyage == null
                && intro != null && intro.IsPlaying;
            if (!titleVisible) achievementScreen.Dismiss();
            else if (GameInput.IsFocused)
            {
                if (!achievementScreen.IsVisible)
                {
                    var notification = PrototypeAchievements.TakeNotification();
                    if (notification != null) achievementScreen.Show(notification, PrototypeAchievements.Progress.Counter);
                }
                var displayed = achievementScreen.Achievement;
                if (achievementScreen.Update(Time.unscaledDeltaTime)) PrototypeAchievements.MarkNotified(displayed);
            }
            if (startingNewGame)
            {
                if (GameInput.IsFocused)
                {
                    // Render a fully black frame before loading the voyage's assets.
                    if (newGameBlackHasDrawn) StartNewGameVoyage();
                    else newGameFadeOutElapsed = Mathf.Min(NewGameFadeOutDuration,
                        newGameFadeOutElapsed + Time.unscaledDeltaTime);
                }
                return;
            }
            if (voyage != null)
            {
                if (GameInput.IsFocused) voyage.Update(Time.unscaledDeltaTime);
                if (!voyage.IsPlaying)
                {
                    voyage.Dispose(); voyage = null;
                    IsMenuVisible = false;
                    player.SetMovementEnabled(!dialogue.IsOpen && !battle.IsBusy);
                }
                return;
            }
            if (IsMenuVisible)
            {
                if ((intro == null || !intro.IsPlaying) && menuHasDrawn)
                    menuFadeInElapsed = Mathf.Min(MenuFadeInDuration, menuFadeInElapsed + Time.unscaledDeltaTime);
                if (!IsMenuFadingIn && (intro == null || !intro.BlocksMenuInput) && GameInput.IsFrameAvailable)
                {
                    MenuDirection direction = GameInput.Navigation;
                    if (GameInput.CancelPressed)
                    { UpdateMenuInput(KeyCode.Escape); GameInput.ConsumeGamepadFrame(); }
                    else
                    {
                        if (direction == MenuDirection.Up || direction == MenuDirection.Left) MoveSelection(-1);
                        else if (direction == MenuDirection.Down || direction == MenuDirection.Right) MoveSelection(1);
                        if (GameInput.ConfirmPressed)
                        { UpdateMenuInput(KeyCode.Return); GameInput.ConsumeGamepadFrame(); }
                    }
                }
                return;
            }
            if (!IsMenuVisible && !dialogue.IsOpen && !battle.IsBusy && GameInput.IsFocused
                && GameInput.SavePressed) { SaveManual(); GameInput.ConsumeGamepadFrame(); }
            if (GameInput.IsFocused && !battle.HasEnded) playSeconds += Time.unscaledDeltaTime;
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
            else SaveToSlot(slot);
        }
        private void UpdateMenuInput(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                if (overwriteSlot != 0) overwriteSlot = 0;
                else if (page != MenuPage.Title) Back();
                return;
            }
            if (key == KeyCode.UpArrow || key == KeyCode.W)
                MoveSelection(-1);
            if (key == KeyCode.DownArrow || key == KeyCode.S)
                MoveSelection(1);
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter && key != KeyCode.Space) return;
            if (overwriteSlot != 0)
            {
                if (selectedOption == 0) SaveToSlot(overwriteSlot);
                else overwriteSlot = 0;
            }
            else if (page == MenuPage.Title)
            {
                if (selectedOption == 0) NewGame();
                else OpenPage(MenuPage.Continue);
            }
            else if (selectedOption == SaveStore.SlotCount) Back();
            else SelectSlot(selectedOption + 1);
        }
        private bool OptionEnabled(int option) => page != MenuPage.Continue || overwriteSlot != 0
            || option == SaveStore.SlotCount || previews[option] != null;
        private void MoveSelection(int direction)
        {
            int count = page == MenuPage.Title || overwriteSlot != 0 ? 2 : SaveStore.SlotCount + 1;
            for (int i = 0; i < count; i++)
            {
                selectedOption = (selectedOption + direction + count) % count;
                if (OptionEnabled(selectedOption)) break;
            }
        }
        private void ShowMessage(string text)
        { message = text; messageUntil = Time.unscaledTime + 8f; }
        private void OnGUI()
        {
            // Save notices belong to the journey selection menu, never to the game maps.
            if (store == null || !IsMenuVisible) return;
            if (intro != null && intro.IsPlaying) { intro.Draw(); DrawAchievementNotification(); return; }
            if (voyage != null) { voyage.Draw(); return; }
            // Wait for the reveal and release of the skip input before allowing menu selection.
            if ((startingNewGame || IsMenuFadingIn || intro != null && intro.BlocksMenuInput)
                && (Event.current.isKey || Event.current.isMouse))
                Event.current.Use();
            // Update owns keyboard and controller input; IMGUI only handles pointer activation.
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return
                || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Space
                || Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow
                || Event.current.keyCode == KeyCode.LeftArrow || Event.current.keyCode == KeyCode.RightArrow
                || Event.current.keyCode == KeyCode.A || Event.current.keyCode == KeyCode.D || Event.current.keyCode == KeyCode.E
                || Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.S
                || Event.current.keyCode == KeyCode.Escape))
            {
                Event.current.Use();
                if (!IsMenuVisible) return;
                if (voyage != null) { voyage.Draw(); return; }
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
                if (MenuButton(new Rect(490, 562, 300, 48), "New game", 0, titleButtonStyle)) NewGame();
                if (MenuButton(new Rect(490, 620, 300, 48), "Continue", 1, titleButtonStyle)) OpenPage(MenuPage.Continue);
            }
            else
            {
                bool loading = page == MenuPage.Continue;
                saveScreen.DrawHeader(loading, selectedOption);
                bool previousEnabled = GUI.enabled;
                for (int i = 0; i < SaveStore.SlotCount; i++)
                {
                    var preview = previews[i];
                    saveScreen.DrawSlot(i, preview, occupied[i], slotMessages[i], preview == null ? null : LocationTitle(preview));
                    GUI.enabled = previousEnabled && overwriteSlot == 0 && (!loading || preview != null);
                    if (MenuButton(SaveSelectionPresentation.SlotRect(i), "", i, GUIStyle.none)) SelectSlot(i + 1);
                    GUI.enabled = previousEnabled;
                }
                GUI.enabled = previousEnabled && overwriteSlot == 0;
                if (MenuButton(SaveSelectionPresentation.BackRect, "Back", SaveStore.SlotCount, saveScreen.BackStyle)) Back();
                GUI.enabled = previousEnabled;
                if (overwriteSlot != 0)
                {
                    Color overlayColor = GUI.color; GUI.color = new Color(0, 0, 0, .6f);
                    GUI.DrawTexture(new Rect(0, 0, 1280, 720), pixel); GUI.color = overlayColor;
                    saveScreen.Panel(new Rect(280, 245, 720, 205));
                    GUI.Label(new Rect(310, 265, 660, 74), "Salvar a jornada no slot " + overwriteSlot + " e substituir o progresso anterior?", textStyle);
                    if (MenuButton(new Rect(310, 365, 320, 48), "Salvar jornada", 0, buttonStyle)) SaveToSlot(overwriteSlot);
                    if (MenuButton(new Rect(650, 365, 320, 48), "Cancelar", 1, buttonStyle)) overwriteSlot = 0;
                }
            }
            DrawSelectedHand();
            if (IsMenuVisible && !string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            {
                Rect rect = new Rect(110, 650, 1060, 50);
                Panel(rect); GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 24, rect.height - 10),
                    message, smallStyle);
            }
            if (IsMenuFadingIn || startingNewGame)
            {
                // Fade the whole menu, including the cursor and highlighted buttons.
                GUI.matrix = Matrix4x4.identity; GUI.depth = -101;
                float opacity = startingNewGame
                    ? Mathf.SmoothStep(0, 1, newGameFadeOutElapsed / NewGameFadeOutDuration)
                    : Mathf.SmoothStep(1, 0, menuFadeInElapsed / MenuFadeInDuration);
                GUI.color = new Color(0, 0, 0, opacity);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
                if (startingNewGame && newGameFadeOutElapsed >= NewGameFadeOutDuration
                    && Event.current.type == EventType.Repaint) newGameBlackHasDrawn = true;
            }
            if (Event.current.type == EventType.Repaint) menuHasDrawn = true;
            GUI.matrix = previousMatrix; GUI.depth = previousDepth; GUI.color = previousColor;
        }
        private void DrawAchievementNotification()
        {
            if (!achievementScreen.IsVisible) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.depth = -110;
            GUI.color = Color.white;
            achievementScreen.Draw();
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
        private bool MenuButton(Rect rect, string label, int option, GUIStyle style)
        {
            if (!startingNewGame && GUI.enabled && (Event.current.type == EventType.MouseMove
                || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                && rect.Contains(Event.current.mousePosition))
                selectedOption = option;
            Color previous = GUI.color;
            if (GUI.enabled && selectedOption == option) GUI.color = new Color(0.96f, 0.8f, 0.48f);
            bool clicked = GUI.Button(rect, label, style);
            GUI.color = previous;
            return !startingNewGame && clicked;
        }
        private void DrawSelectedHand()
        {
            if (!IsMenuVisible || hand == null || !OptionEnabled(selectedOption)) return;
            Rect option;
            if (overwriteSlot != 0) option = new Rect(selectedOption == 0 ? 310 : 650, 365, 320, 48);
            else if (page == MenuPage.Title) option = new Rect(490, selectedOption == 0 ? 562 : 620, 300, 48);
            else option = selectedOption == SaveStore.SlotCount ? SaveSelectionPresentation.BackRect
                : SaveSelectionPresentation.SlotRect(selectedOption);
            bool row = page != MenuPage.Title && overwriteSlot == 0 && selectedOption < SaveStore.SlotCount;
            float left = page == MenuPage.Title
                ? option.center.x - titleButtonStyle.CalcSize(new GUIContent(selectedOption == 0 ? "New game" : "Continue")).x / 2
                : row ? 92 : option.x;
            float width = Mathf.Min(row ? 72 : 44, (row ? 40 : 30) * handAspect), height = width / handAspect;
            Color previous = GUI.color; GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(left - width - 12, option.center.y - height / 2, width, height), hand, handUv, true);
            GUI.color = previous;
        }
        private void LoadHandSprite()
        {
            hand = Resources.Load<Texture2D>("PixelArt/BattleGloveCursor");
            if (hand == null) return;
            Color32[] pixels = hand.GetPixels32();
            int left = hand.width, right = -1, bottom = hand.height, top = -1;
            for (int y = 0; y < hand.height; y++) for (int x = 0; x < hand.width; x++)
            {
                if (pixels[y * hand.width + x].a <= 16) continue;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
            if (right < left || top < bottom) return;
            int width = right - left + 1, height = top - bottom + 1;
            handUv = new Rect((float)left / hand.width, (float)bottom / hand.height,
                (float)width / hand.width, (float)height / hand.height);
            handAspect = (float)width / height;
        }
        private static string LocationTitle(RestoredJourney journey)
        {
            if (journey.Data.Ending != 0) return EndingNarrative.Title((GameEnding)journey.Data.Ending);
            var navigation = new PrototypeTravel(journey.Secrets);
            navigation.TryRestore(journey.Data.Map, new Vector2(journey.Data.X, journey.Data.Y)); return navigation.Current.Name;
        }
        private void Panel(Rect rect)
        {
            Color previous = GUI.color; GUI.color = new Color(0.04f, 0.065f, 0.1f, 0.99f); GUI.DrawTexture(rect, pixel); GUI.color = previous;
        }
        private void EnsureStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            scaleSprite = Resources.Load<Texture2D>("PixelArt/DragonScale");
            saveScreen = new SaveSelectionPresentation();
            LoadHandSprite();
            titleStyle = PrototypeTypography.Style(GUI.skin.label, 48, true); titleStyle.normal.textColor = new Color(0.96f, 0.8f, 0.48f);
            textStyle = PrototypeTypography.Style(GUI.skin.label, 24); textStyle.wordWrap = true; textStyle.normal.textColor = Color.white;
            smallStyle = PrototypeTypography.Style(textStyle, 20); buttonStyle = PrototypeTypography.Style(GUI.skin.button, 24);
            titleButtonStyle = PrototypeTypography.Style(GUI.skin.label, 36, true); titleButtonStyle.alignment = TextAnchor.MiddleCenter;
            titleButtonStyle.normal.textColor = Color.white;
            titleButtonStyle.hover.textColor = new Color(0.96f, 0.8f, 0.48f);
            titleButtonStyle.active.textColor = new Color(0.96f, 0.8f, 0.48f);
            titleButtonStyle.focused.textColor = Color.white;
        }
        private void OnDestroy() { voyage?.Dispose(); intro?.Dispose(); saveScreen?.Dispose(); if (pixel != null) Destroy(pixel); }
    }
}
