using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class DialogueController : MonoBehaviour
    {
        private readonly DialogueSession session = new DialogueSession();

        public bool IsOpen => session.IsOpen;
        public bool HasChoices => session.HasChoices;
        public string Title => session.Title;
        public string Text => session.Text;
        public int PageNumber => session.PageNumber;
        public int PageCount => session.PageCount;
        public System.Collections.Generic.IReadOnlyList<string> Choices => session.Choices;

        public void Open(string title, params string[] messages)
        {
            session.Open(title, null, messages);
        }

        public void OpenWithCompletion(string title, System.Action completed, params string[] messages)
            => session.Open(title, completed, messages);
        public void OpenChoices(string title, string text, System.Collections.Generic.IReadOnlyList<string> choices, System.Action<int> selected)
            => session.OpenChoices(title, text, choices, selected);
        public bool Choose(int index) => session.Choose(index);
        public void Advance() => session.Advance();

        public void Close()
        {
            session.Close();
        }
    }
}
