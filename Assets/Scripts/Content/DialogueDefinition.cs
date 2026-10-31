using System;
using System.Collections.Generic;
using System.Globalization;

namespace DragonQuest.Content
{
    public sealed class DialogueDefinition
    {
        private readonly string[] pages;
        public string Id { get; }
        public DialogueSpeaker Speaker { get; }
        public string Title { get; }
        public bool IsNotice { get; }
        public IReadOnlyList<string> Choices { get; }

        public DialogueDefinition(string id, DialogueSpeaker speaker, string title, params string[] pages)
            : this(id, speaker, title, false, null, pages) { }

        private DialogueDefinition(string id, DialogueSpeaker speaker, string title, bool notice,
            IReadOnlyList<string> choices, string[] pages)
        {
            if (string.IsNullOrWhiteSpace(id) || pages == null || pages.Length == 0) throw new ArgumentException("Dialogue needs an ID and pages.");
            Id = id; Speaker = speaker; Title = title; IsNotice = notice;
            this.pages = (string[])pages.Clone();
            var options = new string[choices?.Count ?? 0];
            for (int i = 0; i < options.Length; i++) options[i] = choices[i];
            Choices = Array.AsReadOnly(options);
        }

        public static DialogueDefinition Choice(string id, DialogueSpeaker speaker, string title,
            string message, IReadOnlyList<string> options)
        {
            if (options == null || options.Count == 0) throw new ArgumentException("Escolhas precisam de opcoes.");
            return new DialogueDefinition(id, speaker, title, false, options, new[] { message });
        }

        public static DialogueDefinition Notice(string id, string message)
            => new DialogueDefinition(id, DialogueSpeaker.Aren, string.Empty, true, null, new[] { message });

        public DialogueDefinition Format(params object[] arguments)
        {
            var messages = new string[pages.Length];
            for (int i = 0; i < messages.Length; i++) messages[i] = string.Format(CultureInfo.CurrentCulture, pages[i], arguments);
            var options = new string[Choices.Count];
            for (int i = 0; i < options.Length; i++) options[i] = string.Format(CultureInfo.CurrentCulture, Choices[i], arguments);
            return new DialogueDefinition(Id, Speaker, string.Format(CultureInfo.CurrentCulture, Title, arguments), IsNotice, options, messages);
        }

        public string[] CreatePages() => (string[])pages.Clone();
    }
}
