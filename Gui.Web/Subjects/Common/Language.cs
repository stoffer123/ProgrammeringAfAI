namespace Gui.Web.Subjects.Common
{
    public record Language(string Code, string DisplayName, string PromptName)
    {
        /// <summary>The instruction that makes the model respond in this language.</summary>
        public string ResponseInstruction => $"""
            LANGUAGE RULE: Every reply must be written entirely in {PromptName}.
            This rule overrides the language of the user's messages and of earlier replies.
            If the user writes in another language, still reply in {PromptName}.
            """;

        /// <summary>A short reminder sent after each user message.</summary>
        public string ReplyReminder =>
            $"Reply in {PromptName}, regardless of the language of the user's and previous messages.";
    }
    public static class Languages
    {
        public static IReadOnlyList<Language> All { get; } =
        [
            new("da", "Dansk",    "Danish"),
            new("en", "English",  "English"),
            new("de", "Deutsch",  "German"),
            new("es", "Español",  "Spanish"),
            new("fr", "Français", "French"),
        ];

        public static Language Default => All[0];
    }
}
