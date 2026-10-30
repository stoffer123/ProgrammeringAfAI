using AiToolkit.Builders;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;
using System.Text.Json;

namespace Gui.Web.Subjects.IntegreretAI.Opgave2;

/// <summary>A suggested destination or type of trip with a short reason.</summary>
/// <param name="Name">The destination or travel type, for example "Lofoten" or "Cykelferie langs Donau".</param>
/// <param name="Type">Either <c>"destination"</c> or <c>"travel_type"</c>.</param>
/// <param name="Reason">Why the suggestion matches the user's preferences.</param>
public record TravelSuggestion(string Name, string Type, string Reason)
{
    /// <summary>Whether the suggestion is a specific place rather than a type of trip.</summary>
    public bool IsDestination => Type == "destination";
}

/// <summary>
/// Suggests ten destinations or travel types based on the user's holiday preferences.
/// </summary>
public class TravelSuggestionService(IAiService ai)
{
    private record SuggestionResponse(List<TravelSuggestion> Suggestions);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Asks the model for ten travel suggestions matching the given preferences.</summary>
    /// <param name="preferences">Two or more holiday preferences, for example "natur" or "lavt budget".</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>Ten suggestions with reasons.</returns>
    public async Task<List<TravelSuggestion>> GetSuggestionsAsync(
        IReadOnlyList<string> preferences, CancellationToken ct = default)
    {
        if (preferences.Count < 2)
            throw new ArgumentException("At least two preferences are required.", nameof(preferences));

        AiRequest request = new PromptBuilder(OpenAiModels.Gpt6Luna)
            .WithInstructions("""
                You are a travel advisor who helps people discover holidays they would not
                have thought of themselves.
                Respond only with valid JSON in exactly this format, with no other text:
                {"suggestions":[{"name":"...","type":"destination","reason":"..."}]}
                "type" must be either "destination" (a specific place) or "travel_type"
                (a kind of trip, such as a hiking holiday or a river cruise).
                Write "name" and "reason" in Danish.
                """)
            .WithReasoningEffort(ReasoningEffort.Low)
            .WithMaxOutputTokens(4000)
            .AddUserMessage($"""
                These are my holiday preferences:
                {string.Join("\n", preferences.Select(p => "- " + p))}

                Suggest exactly 10 destinations or travel types that match these preferences.

                Rules:
                - Every suggestion must clearly match my preferences. Mention which ones in the reason.
                - Include a mix of specific destinations and travel types.
                - Vary the suggestions across regions and styles. Avoid only suggesting the most famous places.
                - If my preferences mention budget, climate, mood or pace, respect them in every suggestion.
                - If some preferences pull in different directions, find suggestions that balance them.
                - Keep each reason short: one or two sentences.
                """)
            .Build();

        string json = await ai.GetResponseAsync(request, ct);   // <- the API call
        return Parse(json);
    }

    private static List<TravelSuggestion> Parse(string json)
    {
        json = json.Trim().Trim('`');
        if (json.StartsWith("json")) json = json[4..];

        var result = JsonSerializer.Deserialize<SuggestionResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not read the response from the model.");

        if (result.Suggestions.Count != 10)
            throw new InvalidOperationException($"Expected 10 suggestions, but got {result.Suggestions.Count}.");

        return result.Suggestions;
    }
}