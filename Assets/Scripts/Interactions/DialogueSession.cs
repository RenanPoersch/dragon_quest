using System;
using System.Collections.Generic;
using DragonQuest.Content;

namespace DragonQuest.Interactions
{
    // Fluxo de paginas/escolhas independente de MonoBehaviour.
    public sealed class DialogueSession
    {
        private string[] pages;
        private IReadOnlyList<string> choices;
        private int pageIndex;
        private Action completion;
        private Action closeAction;
        private Action<int> choiceCallback;
        public bool IsOpen => pages != null;
        public bool HasChoices => IsOpen && choices != null;
        public string Title { get; private set; }
        public string Id { get; private set; }
        public DialogueSpeaker Speaker { get; private set; }
        public bool IsNotice { get; private set; }
        public string Text => IsOpen ? pages[pageIndex] : string.Empty;
        public int PageNumber => IsOpen ? pageIndex + 1 : 0;
        public int PageCount => IsOpen ? pages.Length : 0;
        public IReadOnlyList<string> Choices => choices;

        public void Open(DialogueDefinition definition, Action completed = null, Action closed = null, Action<int> selected = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Choices.Count > 0 && selected == null) throw new ArgumentException("Escolhas precisam de resposta.");
            Clear();
            Id = definition.Id; Speaker = definition.Speaker; Title = definition.Title; IsNotice = definition.IsNotice;
            pages = definition.CreatePages();
            choices = definition.Choices.Count > 0 ? definition.Choices : null;
            completion = completed; closeAction = closed; choiceCallback = selected;
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
        {
            Action action = closeAction;
            Clear();
            action?.Invoke();
        }

        private void Clear()
        {
            Id = null; Title = null; Speaker = DialogueSpeaker.Aren; IsNotice = false;
            pages = null; pageIndex = 0; choices = null; completion = null; closeAction = null; choiceCallback = null;
        }
    }
}
