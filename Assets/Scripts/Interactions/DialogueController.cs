using UnityEngine;
using DragonQuest.Audio;

namespace DragonQuest.Interactions
{
    public sealed class DialogueController : MonoBehaviour
    {
        private readonly DialogueSession session = new DialogueSession();
        private readonly DialogueTextReveal reveal = new DialogueTextReveal();
        private bool notice;

        public bool IsOpen => session.IsOpen;
        public bool IsNotice => IsOpen && notice;
        public bool HasChoices => session.HasChoices;
        public bool IsTyping => IsOpen && !reveal.IsComplete;
        public float RevealSecondsRemaining => reveal.SecondsRemaining;
        public bool ChoicesReady => HasChoices && !IsTyping;
        public string Title => session.Title;
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

        public void Open(string title, params string[] messages)
        {
            notice = false;
            session.Open(title, null, messages);
            BeginPage();
        }

        public void OpenWithCompletion(string title, System.Action completed, params string[] messages)
        {
            notice = false;
            session.Open(title, completed, messages);
            BeginPage();
        }

        public void OpenChoices(string title, string text, System.Collections.Generic.IReadOnlyList<string> choices, System.Action<int> selected)
        {
            notice = false;
            session.OpenChoices(title, text, choices, selected);
            BeginPage();
        }

        public void OpenNotice(string message)
        {
            notice = true;
            session.Open(string.Empty, null, message);
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
