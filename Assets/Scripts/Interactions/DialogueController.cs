using DragonQuest.Content;
using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Interactions
{
    public sealed class DialogueController : MonoBehaviour
    {
        private readonly DialogueSession session = new DialogueSession();
        private readonly DialogueTextReveal reveal = new DialogueTextReveal();

        public bool IsOpen => session.IsOpen;
        public bool IsNotice => IsOpen && session.IsNotice;
        public bool HasChoices => session.HasChoices;
        public bool IsTyping => IsOpen && !reveal.IsComplete;
        public float RevealSecondsRemaining => reveal.SecondsRemaining;
        public bool ChoicesReady => HasChoices && !IsTyping;
        public string Title => session.Title;
        public string DialogueId => session.Id;
        public DialogueSpeaker Speaker => session.Speaker;
        public string Text => session.Text;
        public string VisibleText => reveal.VisibleText;
        public int ContentVersion { get; private set; }
        public int SelectedChoice { get; private set; }
        public int PageNumber => session.PageNumber;
        public int PageCount => session.PageCount;
        public System.Collections.Generic.IReadOnlyList<string> Choices => session.Choices;

        private void Update()
        {
            if (IsOpen && Application.isFocused) reveal.Tick(Time.unscaledDeltaTime);
        }

        public void Open(DialogueDefinition definition, System.Action completed = null,
            System.Action closed = null, System.Action<int> selected = null)
        {
            session.Open(definition, completed, closed, selected);
            BeginPage();
        }

        public void SelectChoice(int index)
        {
            if (!ChoicesReady || index < 0 || index >= Choices.Count) return;
            MenuSounds.Navigate(SelectedChoice, index, MenuSoundContext.DialogueChoices);
            SelectedChoice = index;
        }

        public void MoveChoice(int direction)
        {
            if (!ChoicesReady || direction == 0) return;
            int count = Choices.Count;
            SelectChoice((SelectedChoice + (direction > 0 ? 1 : count - 1)) % count);
        }

        public bool ConfirmChoice() => ChoicesReady && Choose(SelectedChoice);

        public bool Choose(int index)
        {
            if (!session.Choose(index)) return false;
            BeginPage();
            return true;
        }

        // Keyboard continuation first finishes the current line, then advances on a new press.
        public void Continue()
        {
            if (IsTyping) RevealAll();
            else Advance();
        }

        public void RevealAll() => reveal.Finish();

        // Scripted progression keeps its existing immediate page/callback semantics.
        public void Advance()
        {
            if (!IsOpen || HasChoices) return;
            session.Advance();
            BeginPage();
        }

        public void Close()
        {
            session.Close();
            BeginPage();
        }

        private void BeginPage()
        {
            reveal.Begin(session.Text);
            if (IsNotice) reveal.Finish();
            SelectedChoice = 0;
            ContentVersion++;
        }
    }
}
