using DragonQuest.Gameplay.Saving;
using DragonQuest.Gameplay;
using DragonQuest.Presentation.Menus;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using DragonQuest.Audio;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeSaveController : MonoBehaviour
    {
        private static bool loadGameOnStart;
        private JourneyMenuPage page = JourneyMenuPage.Title;
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
        private SaveSlotSession slotSession;
        private SaveSlotSession Slots => slotSession ?? (slotSession = new SaveSlotSession(store));
        private RestoredJourney[] previews => Slots.Previews;
        private bool[] occupied => Slots.Occupied;
        private SaveMenuPresenter saveMenu;
        public SaveMenuPresenter Presenter => saveMenu;
        private readonly SaveMenuContext view = new SaveMenuContext();
        private string message;
        private float messageUntil;
        private int overwriteSlot;
        private bool awaitingFirstSave;
        private double playSeconds;
        private readonly FrontEndPresentation frontEnd = new FrontEndPresentation();
        private readonly TitleMenuPresentation titleMenu = new TitleMenuPresentation();
        public bool IsMenuVisible { get; private set; } = true;
        public bool IsIntroPlaying => IsMenuVisible && frontEnd.IsIntroPlaying;
        public bool IsVoyagePlaying => IsMenuVisible && frontEnd.IsVoyagePlaying;
        public bool IsFrontEndVisible => IsMenuVisible && page != JourneyMenuPage.Save;
        public float IntroMusicGain => IsIntroPlaying ? frontEnd.IntroMusicGain : 0;
        public float VoyageMusicGain => IsVoyagePlaying ? frontEnd.VoyageMusicGain : 0;
        public int ActiveSlot { get; private set; }
        public long PlaySeconds => (long)playSeconds;

        public void Initialize(PlayerMovement hero, PrototypeBattleController combat, PrototypeShopController vendors,
            PrototypeLocations maps, DialogueController conversations, ExplorationProgress exploration,
            AncientSecrets ancient, DragonProgress dragonProgress, Action<RestoredJourney> installJourney)
        {
            player = hero; battle = combat; shops = vendors; locations = maps; dialogue = conversations;
            progress = exploration; secrets = ancient; dragon = dragonProgress; install = installJourney;
            store = new SaveStore(System.IO.Path.Combine(Application.persistentDataPath, "Saves"));
            page = loadGameOnStart ? JourneyMenuPage.Continue : JourneyMenuPage.Title;
            loadGameOnStart = false;
            slotSession = new SaveSlotSession(store);
            saveMenu = new SaveMenuPresenter(frontEnd, titleMenu);
            view.IsVisible = () => IsMenuVisible; view.ReadPage = () => page;
            view.ReadSelection = () => selectedOption; view.ReadOverwriteSlot = () => overwriteSlot;
            view.ReadMessage = () => message; view.ReadMessageUntil = () => messageUntil;
            view.OptionEnabled = OptionEnabled; view.SelectOption = option => selectedOption = option;
            view.NewGame = NewGame; view.OpenContinue = () => OpenPage(JourneyMenuPage.Continue);
            view.SelectSlot = SelectSlot; view.SaveToSlot = SaveToSlot;
            view.Back = Back; view.CancelOverwrite = CancelOverwrite;
            RefreshSlots(); player.SetMovementEnabled(false);
            frontEnd.Initialize(page == JourneyMenuPage.Title);
        }
        private void RefreshSlots()
        {
            Slots.Refresh();
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
            bool saved = Slots.TryWrite(ActiveSlot, Capture, out string error);
            ShowMessage(saved ? success + " / slot " + ActiveSlot : error); return saved;
        }
        private void NewGame() => frontEnd.NewGame();

        private void StartNewGameVoyage()
        {
            frontEnd.BeginVoyage();
            ActiveSlot = Slots.FirstEmptySlot();
            awaitingFirstSave = true;
            playSeconds = 0;
            overwriteSlot = 0;
            // Keep every existing menu/busy gate active through the voyage and map reveal.
            player.SetMovementEnabled(false);
        }
        private void Continue(int slot)
        {
            if (!Slots.TryRead(slot, out var journey, out string warning)) { ShowMessage(warning); RefreshSlots(); return; }
            install(journey); progress = journey.Progress; ActiveSlot = slot; IsMenuVisible = false;
            awaitingFirstSave = false;
            playSeconds = journey.Data.PlaySeconds ?? 0;
            player.SetMovementEnabled(true);
            ShowMessage(warning ?? "Jornada carregada / slot " + slot);
            battle.RestoreEnding((GameEnding)journey.Data.Ending);
        }
        public bool CanReturnToTitle => store != null && !IsMenuVisible && battle != null && battle.IsMenuOpen
            && !battle.IsActive && !battle.HasEnded && !battle.IsCheatMenuBlocking
            && (battle.Flow == null || battle.Flow.Owner == GameInputOwner.Inventory);

        public void ReturnToTitle()
        {
            if (!CanReturnToTitle) return;
            GameInput.ConsumeGamepadFrame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStartupPage() { loadGameOnStart = false; }

        public void ReturnToLoadGame()
        {
            loadGameOnStart = true;
            GameInput.ConsumeGamepadFrame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        public bool CanSaveManually => store != null && !IsMenuVisible && battle != null
            && !battle.IsActive && !battle.HasEnded && !battle.IsCheatMenuBlocking
            && dialogue != null && !dialogue.IsOpen && (shops == null || !shops.IsOpen)
            && (battle.Flow == null || battle.Flow.Owner == GameInputOwner.Exploration || battle.Flow.Owner == GameInputOwner.Inventory)
            && locations != null && (locations.LocationId == PrototypeTravel.Inn || locations.LocationId == PrototypeTravel.World);

        public bool OpenManualSaveMenu()
        {
            if (!CanSaveManually) return false;
            battle.CloseMenu();
            IsMenuVisible = true;
            player.SetMovementEnabled(false);
            OpenPage(JourneyMenuPage.Save);
            return true;
        }

        public void SaveManual()
        {
            if (!CanSaveManually)
            { ShowMessage("O salvamento manual fica disponivel no mapa global ou na hospedaria, fora de combate."); return; }
            if (ActiveSlot == 0 || awaitingFirstSave && Slots.Exists(ActiveSlot))
            {
                IsMenuVisible = true;
                player.SetMovementEnabled(false);
                OpenPage(JourneyMenuPage.Save);
                return;
            }
            if (Write("Jornada salva")) awaitingFirstSave = false;
        }
        public void OpenInnSaveMenu()
        {
            if (IsMenuVisible || dialogue.IsOpen || battle.IsBusy || locations.LocationId != PrototypeTravel.Inn) return;
            IsMenuVisible = true;
            player.SetMovementEnabled(false);
            OpenPage(JourneyMenuPage.Save);
        }
        private void SaveToSlot(int slot)
        {
            MenuSounds.Play(MenuSound.Confirm);
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
            MenuSounds.Play(MenuSound.Cancel);
            if (page == JourneyMenuPage.Save) CloseSaveMenu();
            else OpenPage(JourneyMenuPage.Title);
        }
        private void Update()
        {
            if (store == null) return;
            switch (frontEnd.Update(Time.unscaledDeltaTime, IsMenuVisible, page == JourneyMenuPage.Title, GameInput.IsFocused))
            {
                case FrontEndStep.StartVoyage:
                    StartNewGameVoyage(); return;
                case FrontEndStep.FinishVoyage:
                    IsMenuVisible = false;
                    player.SetMovementEnabled(!dialogue.IsOpen && !battle.IsBusy);
                    return;
                case FrontEndStep.Busy: return;
            }
            if (IsMenuVisible)
            {
                if (frontEnd.AcceptsMenuInput && GameInput.IsFrameAvailable)
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
        private void OpenPage(JourneyMenuPage next)
        {
            if (next == JourneyMenuPage.Continue) MenuSounds.Play(MenuSound.Confirm);
            page = next; selectedOption = 0; overwriteSlot = 0; message = null;
            if (next != JourneyMenuPage.Title) RefreshSlots();
        }
        private void SelectSlot(int slot)
        {
            MenuSounds.Play(MenuSound.Confirm);
            if (page == JourneyMenuPage.Continue)
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
                if (overwriteSlot != 0) CancelOverwrite();
                else if (page != JourneyMenuPage.Title) Back();
                return;
            }
            if (key == KeyCode.UpArrow || key == KeyCode.W)
                MoveSelection(-1);
            if (key == KeyCode.DownArrow || key == KeyCode.S)
                MoveSelection(1);
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter && key != KeyCode.Space) return;
            if (!OptionEnabled(selectedOption)) return;
            if (overwriteSlot != 0)
            {
                if (selectedOption == 0) SaveToSlot(overwriteSlot);
                else CancelOverwrite();
            }
            else if (page == JourneyMenuPage.Title)
            {
                if (selectedOption == 0) NewGame();
                else OpenPage(JourneyMenuPage.Continue);
            }
            else if (selectedOption == SaveStore.SlotCount) Back();
            else SelectSlot(selectedOption + 1);
        }
        private bool OptionEnabled(int option) => page == JourneyMenuPage.Title
            ? option == 0 || option == 1 && Array.Exists(previews, preview => preview != null)
            : overwriteSlot != 0 ? option >= 0 && option < 2
            : option >= 0 && (page == JourneyMenuPage.Save ? option < SaveStore.SlotCount
                : option == SaveStore.SlotCount || option < SaveStore.SlotCount && previews[option] != null);
        private void CancelOverwrite()
        { overwriteSlot = 0; MenuSounds.Play(MenuSound.Cancel); }
        private MenuSoundContext NavigationSoundContext => page == JourneyMenuPage.Title
            ? MenuSoundContext.InitialMenu : MenuSoundContext.SaveSlots;
        private void MoveSelection(int direction)
        {
            int previous = selectedOption;
            int count = page == JourneyMenuPage.Title || overwriteSlot != 0 ? 2
                : SaveStore.SlotCount + (page == JourneyMenuPage.Continue ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                selectedOption = (selectedOption + direction + count) % count;
                if (OptionEnabled(selectedOption)) break;
            }
            MenuSounds.Navigate(previous, selectedOption, NavigationSoundContext);
        }
        private void ShowMessage(string text)
        { message = text; messageUntil = Time.unscaledTime + 8f; }

        private void OnGUI()
        {
            if (store == null || !IsMenuVisible) return;
            view.Slots = Slots;
            saveMenu.Draw(view);
        }
        private void OnDestroy() { frontEnd.Dispose(); saveMenu?.Dispose(); }
    }
}
