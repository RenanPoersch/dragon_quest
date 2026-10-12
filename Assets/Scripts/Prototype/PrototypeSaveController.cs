using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Persistence;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeSaveController : MonoBehaviour
    {
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
        private Action unsubscribe;
        private readonly AutosavePolicy autosave = new AutosavePolicy();
        private readonly RestoredJourney[] previews = new RestoredJourney[SaveStore.SlotCount];
        private readonly string[] slotMessages = new string[SaveStore.SlotCount];
        private readonly bool[] occupied = new bool[SaveStore.SlotCount];
        private Texture2D pixel;
        private GUIStyle titleStyle, textStyle, smallStyle, buttonStyle;
        private string message;
        private string compactMessage;
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
        private void BindChanges()
        {
            var party = battle.Party; var items = party.Consumables; var quest = shops.Quest;
            party.Changed += MarkChanged; items.Changed += MarkChanged; progress.Changed += MarkChanged;
            secrets.Changed += MarkChanged; quest.Changed += MarkChanged; battle.EncounterCompleted += EncounterCompleted;
            unsubscribe = () =>
            {
                party.Changed -= MarkChanged; items.Changed -= MarkChanged; progress.Changed -= MarkChanged;
                secrets.Changed -= MarkChanged; quest.Changed -= MarkChanged; battle.EncounterCompleted -= EncounterCompleted;
            };
        }
        private void MarkChanged() => autosave.Changed(battle.IsActive);
        private void EncounterCompleted(BattleOutcome outcome) => autosave.BattleCompleted(outcome);
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
                ShowMessage(saved ? success + " / slot " + ActiveSlot : error, saved ? "Salvo / slot " + ActiveSlot : null); return saved;
            }
            catch (Exception ex) { ShowMessage("Nao foi possivel salvar: " + ex.Message); return false; }
        }
        private void NewGame(int slot)
        {
            ActiveSlot = slot;
            if (!Write("Nova jornada salva")) return;
            overwriteSlot = 0; IsMenuVisible = false; BindChanges(); player.SetMovementEnabled(true);
        }
        private void Continue(int slot)
        {
            if (!store.TryRead(slot, out var journey, out string warning)) { ShowMessage(warning); RefreshSlots(); return; }
            install(journey); progress = journey.Progress; ActiveSlot = slot; IsMenuVisible = false;
            BindChanges(); player.SetMovementEnabled(true);
            ShowMessage(warning ?? "Jornada carregada / slot " + slot, warning == null ? "Carregado / slot " + slot : null);
            battle.RestoreEnding((GameEnding)journey.Data.Ending);
        }
        public void SaveManual()
        {
            if (IsMenuVisible || battle.IsActive || battle.HasEnded || locations.LocationId != PrototypeTravel.Inn)
            { ShowMessage("O salvamento manual fica disponivel na hospedaria, fora de combate."); return; }
            autosave.Acknowledge(); Write("Jornada salva na hospedaria");
        }
        private void Update()
        {
            if (!IsMenuVisible && !dialogue.IsOpen && !battle.IsBusy && Application.isFocused
                && Keyboard.current?.f5Key.wasPressedThisFrame == true) SaveManual();
        }
        private void LateUpdate()
        {
            if (IsMenuVisible || !autosave.Pending || battle.IsActive && battle.Session.Outcome == BattleOutcome.None) return;
            // Uma compra pode alterar inventario, ouro e estoque. Grava apenas a transacao concluida no frame.
            autosave.Acknowledge(); Write("Progresso salvo automaticamente");
        }
        private void ShowMessage(string text, string compact = null)
        { message = text; compactMessage = compact; messageUntil = Time.unscaledTime + 8; }
        private void OnGUI()
        {
            if (store == null) return;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix; int previousDepth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.depth = -100;
            if (IsMenuVisible)
            {
                Panel(new Rect(-1280, -720, 3840, 2160));
                GUI.Label(new Rect(110, 55, 1060, 60), "DRAGON QUEST", titleStyle);
                GUI.Label(new Rect(110, 120, 1060, 48), "Escolha uma jornada. Novo jogo inicia na ilha; Continuar recupera o ultimo save do slot.", textStyle);
                bool previousEnabled = GUI.enabled; GUI.enabled = overwriteSlot == 0;
                for (int i = 0; i < SaveStore.SlotCount; i++)
                {
                    float top = 196 + i * 126; var preview = previews[i];
                    GUI.Label(new Rect(110, top, 640, 35), "SLOT " + (i + 1) + " / " + (preview == null ? occupied[i] ? "Indisponivel" : "Vazio" : LocationTitle(preview)), textStyle);
                    GUI.Label(new Rect(110, top + 37, 650, 50), preview == null ? slotMessages[i] : Summary(preview), smallStyle);
                    if (GUI.Button(new Rect(810, top, 160, 45), "Novo jogo", buttonStyle))
                    { if (occupied[i]) overwriteSlot = i + 1; else NewGame(i + 1); }
                    GUI.enabled = previousEnabled && overwriteSlot == 0 && preview != null;
                    if (GUI.Button(new Rect(990, top, 170, 45), "Continuar", buttonStyle)) Continue(i + 1);
                    GUI.enabled = previousEnabled && overwriteSlot == 0;
                }
                GUI.enabled = previousEnabled;
                GUI.Label(new Rect(110, 598, 1060, 48), "Autosave: vitorias, segredos, bau, entrega e inventario. Salvar manualmente: hospedeira ou F5 na hospedaria.", smallStyle);
                if (overwriteSlot != 0)
                {
                    Panel(new Rect(280, 245, 720, 205));
                    GUI.Label(new Rect(310, 265, 660, 74), "Comecar uma nova jornada no slot " + overwriteSlot + " substituira seu progresso atual.", textStyle);
                    if (GUI.Button(new Rect(310, 365, 320, 48), "Substituir e comecar", buttonStyle)) NewGame(overwriteSlot);
                    if (GUI.Button(new Rect(650, 365, 320, 48), "Cancelar", buttonStyle)) overwriteSlot = 0;
                }
            }
            if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            {
                Rect rect = IsMenuVisible ? new Rect(110, 650, 1060, 50) : compactMessage != null
                    ? new Rect(750, 28, 235, 35) : new Rect(710, 190, 540, 130);
                Panel(rect); GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 24, rect.height - 10),
                    IsMenuVisible ? message : compactMessage ?? message, smallStyle);
            }
            GUI.matrix = previousMatrix; GUI.depth = previousDepth;
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
            titleStyle = new GUIStyle(GUI.skin.label) {fontSize = 38, fontStyle = FontStyle.Bold}; titleStyle.normal.textColor = new Color(0.96f, 0.8f, 0.48f);
            textStyle = new GUIStyle(GUI.skin.label) {fontSize = 20, wordWrap = true}; textStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(textStyle) {fontSize = 16}; buttonStyle = new GUIStyle(GUI.skin.button) {fontSize = 18};
        }
        private void OnDestroy() { unsubscribe?.Invoke(); if (pixel != null) Destroy(pixel); }
    }
}
