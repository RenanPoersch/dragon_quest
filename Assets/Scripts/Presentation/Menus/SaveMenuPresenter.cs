using System;
using System.Collections.Generic;
using DragonQuest.Gameplay.Saving;
using DragonQuest.Audio;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Presentation.Menus
{
    public enum JourneyMenuPage { Title, Save, Continue }
    public sealed class SaveMenuContext
    {
        public Func<bool> IsVisible;
        public Func<JourneyMenuPage> ReadPage;
        public Func<int> ReadSelection, ReadOverwriteSlot;
        public Func<string> ReadMessage;
        public Func<float> ReadMessageUntil;
        public IReadOnlyList<SaveSlotState> Slots;
        public Func<int, bool> OptionEnabled;
        public Action<int> SelectOption, SelectSlot, SaveToSlot;
        public Action NewGame, OpenContinue, Back, CancelOverwrite;
    }

    public sealed class SaveMenuPresenter : IDisposable
    {
        private Texture2D pixel;
        private Texture2D hand;
        private Rect handUv = new Rect(0, 0, 1, 1);
        private float handAspect = 1;
        private GUIStyle textStyle, smallStyle, buttonStyle;
        private SaveSelectionPresentation saveScreen;

        private SaveMenuContext context;
        private readonly FrontEndPresentation frontEnd;
        private readonly TitleMenuPresentation titleMenu;
        public SaveMenuPresenter(FrontEndPresentation frontEnd, TitleMenuPresentation titleMenu)
        { this.frontEnd = frontEnd; this.titleMenu = titleMenu; }
        private bool IsMenuVisible => context.IsVisible();
        private JourneyMenuPage page => context.ReadPage();
        private int selectedOption
        {
            get => context.ReadSelection();
            set => context.SelectOption(value);
        }
        private int overwriteSlot => context.ReadOverwriteSlot();
        private string message => context.ReadMessage();
        private float messageUntil => context.ReadMessageUntil();
        private bool OptionEnabled(int option) => context.OptionEnabled(option);
        private void NewGame() => context.NewGame();
        private void SelectSlot(int slot) => context.SelectSlot(slot);
        private void SaveToSlot(int slot) => context.SaveToSlot(slot);
        private void Back() => context.Back();
        private void CancelOverwrite() => context.CancelOverwrite();
        private MenuSoundContext NavigationSoundContext => page == JourneyMenuPage.Title
            ? MenuSoundContext.InitialMenu : MenuSoundContext.SaveSlots;

        public void Draw(SaveMenuContext state)
        {
            context = state;
            // Save notices belong to the journey selection menu, never to the game maps.
            if (context.Slots == null || !IsMenuVisible) return;
            if (frontEnd.DrawStory()) return;
            // Wait for the reveal and release of the skip input before allowing menu selection.
            if (frontEnd.BlocksMenuInput
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
            if (page == JourneyMenuPage.Title)
            {
                titleMenu.Draw(OptionEnabled(1), MenuButton, NewGame, context.OpenContinue);
            }
            else
            {
                bool loading = page == JourneyMenuPage.Continue;
                saveScreen.DrawHeader(loading, selectedOption);
                bool previousEnabled = GUI.enabled;
                for (int i = 0; i < SaveStore.SlotCount; i++)
                {
                    var slot = context.Slots[i];
                    var preview = slot.Preview;
                    saveScreen.DrawSlot(i, preview, slot.Occupied, slot.Message, preview == null ? null : LocationTitle(preview));
                    GUI.enabled = previousEnabled && overwriteSlot == 0 && (!loading || preview != null);
                    if (MenuButton(SaveSelectionPresentation.SlotRect(i), "", i, GUIStyle.none)) SelectSlot(slot.Number);
                    GUI.enabled = previousEnabled;
                }
                GUI.enabled = previousEnabled && overwriteSlot == 0;
                if (loading && MenuButton(SaveSelectionPresentation.BackRect, "Back", SaveStore.SlotCount, saveScreen.BackStyle)) Back();
                GUI.enabled = previousEnabled;
                if (overwriteSlot != 0)
                {
                    Color overlayColor = GUI.color; GUI.color = new Color(0, 0, 0, .6f);
                    GUI.DrawTexture(new Rect(0, 0, 1280, 720), pixel); GUI.color = overlayColor;
                    saveScreen.Panel(new Rect(280, 245, 720, 205));
                    GUI.Label(new Rect(310, 265, 660, 74), "Salvar a jornada no slot " + overwriteSlot + " e substituir o progresso anterior?", textStyle);
                    if (MenuButton(new Rect(310, 365, 320, 48), "Salvar jornada", 0, buttonStyle)) SaveToSlot(overwriteSlot);
                    if (MenuButton(new Rect(650, 365, 320, 48), "Cancelar", 1, buttonStyle)) CancelOverwrite();
                }
            }
            DrawSelectedHand();
            if (IsMenuVisible && !string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            {
                Rect rect = new Rect(110, 650, 1060, 50);
                Panel(rect); GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 24, rect.height - 10),
                    message, smallStyle);
            }
            frontEnd.DrawMenuFade(pixel);
            GUI.matrix = previousMatrix; GUI.depth = previousDepth; GUI.color = previousColor;
        }

        private bool MenuButton(Rect rect, string label, int option, GUIStyle style)
        {
            if (!frontEnd.IsStartingNewGame && GUI.enabled && (Event.current.type == EventType.MouseMove
                || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                && rect.Contains(Event.current.mousePosition))
            {
                int selected = selectedOption;
                MenuSounds.Select(ref selected, option, NavigationSoundContext);
                selectedOption = selected;
            }
            Color previous = GUI.color;
            if (overwriteSlot == 0 && GUI.enabled && selectedOption == option) GUI.color = new Color(0.96f, 0.8f, 0.48f);
            bool clicked = GUI.Button(rect, label, style);
            GUI.color = previous;
            return !frontEnd.IsStartingNewGame && clicked;
        }

        private void DrawSelectedHand()
        {
            if (!IsMenuVisible || hand == null || !OptionEnabled(selectedOption)) return;
            Rect option;
            if (overwriteSlot != 0) option = new Rect(selectedOption == 0 ? 310 : 650, 365, 320, 48);
            else if (page == JourneyMenuPage.Title) option = TitleMenuPresentation.OptionRect(selectedOption);
            else option = selectedOption == SaveStore.SlotCount ? SaveSelectionPresentation.BackRect
                : SaveSelectionPresentation.SlotRect(selectedOption);
            bool row = page != JourneyMenuPage.Title && overwriteSlot == 0 && selectedOption < SaveStore.SlotCount;
            float left = overwriteSlot != 0
                ? option.center.x - buttonStyle.CalcSize(new GUIContent(selectedOption == 0 ? "Salvar jornada" : "Cancelar")).x / 2
                : page == JourneyMenuPage.Title
                ? option.center.x - titleMenu.ButtonStyle.CalcSize(new GUIContent(selectedOption == 0 ? "New game" : "Continue")).x / 2
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
            saveScreen = new SaveSelectionPresentation();
            LoadHandSprite();
            textStyle = PrototypeTypography.Style(GUI.skin.label, 24); textStyle.wordWrap = true; textStyle.normal.textColor = Color.white;
            smallStyle = PrototypeTypography.Style(textStyle, 20);
            buttonStyle = PrototypeTypography.Style(GUI.skin.label, 24);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.padding = new RectOffset(0, 0, 0, 0);
            foreach (GUIStyleState state in new[] { buttonStyle.normal, buttonStyle.hover, buttonStyle.active, buttonStyle.focused })
            {
                state.textColor = Color.white;
                state.background = null;
            }
        }
        public void Dispose() { saveScreen?.Dispose(); if (pixel != null) UnityEngine.Object.Destroy(pixel); }
    }
}
