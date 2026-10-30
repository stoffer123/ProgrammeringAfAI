using AiToolkit.Builders;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;
using System.Text.Json;

namespace Gui.Web.Subjects.IntegreretAI.Opgave1;

/// <summary>A suggested dinner dish with a short reason.</summary>
public record DishSuggestion(string Name, string Reason);

/// <summary>
/// Suggests ten new dinner dishes based on five dishes the user usually eats.
/// </summary>
public class DishSuggestionService(IAiService ai)
{
    private record SuggestionResponse(List<DishSuggestion> Dishes);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Asks the model for ten dishes similar in taste to the given five.</summary>
    /// <param name="dishes">Exactly five dishes the user usually eats.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>Ten suggested dishes with reasons.</returns>
    public async Task<List<DishSuggestion>> GetSuggestionsAsync(
        IReadOnlyList<string> dishes, CancellationToken ct = default)
    {
        if (dishes.Count != 5)
            throw new ArgumentException("Exactly five dishes are required.", nameof(dishes));

        AiRequest request = new PromptBuilder(OpenAiModels.Gpt6Luna)
            .WithInstructions("""
                You are a food advisor who suggests everyday dinner dishes.
                Respond only with valid JSON in exactly this format, with no other text:
                {"dishes":[{"name":"...","reason":"..."}]}
                Write the dish names and reasons in Danish.
                """)
            .WithReasoningEffort(ReasoningEffort.Low)
            .WithMaxOutputTokens(3000)
            .AddUserMessage($"""
                These are five dishes I usually eat for dinner:
                {string.Join("\n", dishes.Select(d => "- " + d))}

                Suggest exactly 10 other dinner dishes I would probably also like,
                based on the taste and style of the dishes above.

                Rules:
                - Do not repeat any of my five dishes.
                - Suggest ordinary everyday dishes that can be made on a weeknight, not advanced restaurant food.
                - Give each suggestion a short reason (one sentence) that links it to my dishes.
                """)
            .Build();

        string json = await ai.GetResponseAsync(request, ct);
        return Parse(json);
    }

    private static List<DishSuggestion> Parse(string json)
    {
        // Models sometimes wrap JSON in ```json ... ``` - strip it if so
        json = json.Trim().Trim('`');
        if (json.StartsWith("json")) json = json[4..];

        var result = JsonSerializer.Deserialize<SuggestionResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not read the response from the model.");

        if (result.Dishes.Count != 10)
            throw new InvalidOperationException($"Expected 10 suggestions, but got {result.Dishes.Count}.");

        return result.Dishes;
    }
}