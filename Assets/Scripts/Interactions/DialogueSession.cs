using System;
using System.Collections.Generic;

namespace DragonQuest.Interactions
{
    // Fluxo de paginas/escolhas independente de MonoBehaviour. Cancela sem executar a resposta.
    public sealed class DialogueSession
    {
        private string[] pages;
        private IReadOnlyList<string> choices;
        private int pageIndex;
        private Action completion;
        private Action<int> choiceCallback;
        public bool IsOpen => pages != null;
        public bool HasChoices => IsOpen && choices != null;
        public string Title { get; private set; }
        public string Id { get; private set; }
        public string Text => IsOpen ? pages[pageIndex] : string.Empty;
        public int PageNumber => IsOpen ? pageIndex + 1 : 0;
        public int PageCount => IsOpen ? pages.Length : 0;
        public IReadOnlyList<string> Choices => choices;

        public void Open(string title, Action completed, params string[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            Close(); Title = title; pages = (string[])messages.Clone(); completion = completed;
        }

        public void OpenIdentified(string id, string title, Action completed, params string[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            Open(title, completed, messages); Id = id;
        }

        public void OpenChoices(string title, string message, IReadOnlyList<string> options, Action<int> selected)
        {
            if (options == null || options.Count == 0 || selected == null) throw new ArgumentException("Escolhas precisam de opcoes e resposta.");
            var copy = new string[options.Count];
            for (int i=0; i<copy.Length; i++) copy[i] = options[i];
            Open(title, null, message); choices = Array.AsReadOnly(copy); choiceCallback = selected;
        }

        public void Advance()
        {
            if (!IsOpen || HasChoices) return;
            pageIndex++;
            if (pageIndex < pages.Length) return;
            Action callback = completion; Close(); callback?.Invoke();
        }

        public bool Choose(int index)
        {
            if (!HasChoices || index < 0 || index >= choices.Count) return false;
            Action<int> callback = choiceCallback;
            Close(); callback(index); return true;
        }

        public void Close()
        { Id = null; pages = null; pageIndex = 0; choices = null; completion = null; choiceCallback = null; }
    }
}
