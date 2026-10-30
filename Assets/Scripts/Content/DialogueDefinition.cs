using System;

namespace DragonQuest.Content
{
    public sealed class DialogueDefinition
    {
        private readonly string[] pages;
        public string Id { get; }
        public string Title { get; }
        public DialogueDefinition(string id, string title, params string[] pages)
        {
            if (string.IsNullOrWhiteSpace(id) || pages == null || pages.Length == 0) throw new ArgumentException("Dialogue needs an ID and pages.");
            Id = id; Title = title; this.pages = (string[])pages.Clone();
        }
        public string[] CreatePages() => (string[])pages.Clone();
    }
}
