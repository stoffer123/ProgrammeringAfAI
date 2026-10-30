namespace Gui.Web.Subjects;

/// <summary>An assignment that can be selected and shown on the assignments page.</summary>
/// <param name="Id">A stable key, for example <c>"integreretai-1"</c>.</param>
/// <param name="Name">The display name shown in the dropdown.</param>
/// <param name="Component">The Razor component that implements the assignment.</param>
public record Assignment(string Id, string Name, Type Component);

/// <summary>A subject in the course, grouping its assignments.</summary>
public record Subject(string Name, IReadOnlyList<Assignment> Assignments);

/// <summary>All subjects and assignments available in the app.</summary>
public static class SubjectCatalog
{
    public static IReadOnlyList<Subject> All { get; } =
    [
        new("IntegreretAI",
        [
            new("integreretai-1", "Opgave 1: Retforslag", typeof(IntegreretAI.Opgave1.DishSuggestions)),
            new("integreretai-2", "Opgave 2: Rejseforslag", typeof(IntegreretAI.Opgave2.TravelSuggestions)),
            new("integreretai-3", "Opgave 3: Omskrivning til målgrupper", typeof(IntegreretAI.Opgave3.TextRewrite)),
        ]),
        new("Chatbot (Prompt engineering)", []),
        new("Chatbot (Context engineering)", []),
        new("AI Agenter", []),
    ];

    /// <summary>Finds an assignment by its ID, or returns null if it does not exist.</summary>
    public static Assignment? Find(string? id) =>
        All.SelectMany(s => s.Assignments).FirstOrDefault(a => a.Id == id);
}