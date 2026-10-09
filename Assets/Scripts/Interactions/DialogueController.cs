using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class DialogueController : MonoBehaviour
    {
        private string[] pages;
        private int pageIndex;

        public bool IsOpen => pages != null;
        public string Title { get; private set; }
        public string Text => IsOpen ? pages[pageIndex] : string.Empty;
        public int PageNumber => IsOpen ? pageIndex + 1 : 0;
        public int PageCount => IsOpen ? pages.Length : 0;

        public void Open(string title, params string[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            Title = title;
            pages = messages;
            pageIndex = 0;
        }

        public void Advance()
        {
            if (!IsOpen) return;
            pageIndex++;
            if (pageIndex >= pages.Length) Close();
        }

        public void Close()
        {
            pages = null;
            pageIndex = 0;
        }
    }
}
