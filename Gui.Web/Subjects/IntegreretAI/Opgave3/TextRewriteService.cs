using System.Text.Json;
using AiToolkit.Builders;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;

namespace Gui.Web.Subjects.IntegreretAI.Opgave3;

/// <summary>A version of the text rewritten for one audience.</summary>
/// <param name="Audience">The target audience, for example "Børn".</param>
/// <param name="Text">The rewritten text.</param>
public record RewrittenText(string Audience, string Text);

/// <summary>
/// Rewrites a text for three different audiences.
/// </summary>
public class TextRewriteService(IAiService ai)
{
    private record RewriteResponse(List<RewrittenText> Versions);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Asks the model to rewrite the text once for each audience.</summary>
    /// <param name="text">The original text.</param>
    /// <param name="audiences">Exactly three target audiences.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>One rewritten version per audience, in the same order.</returns>
    public async Task<List<RewrittenText>> RewriteAsync(
        string text, IReadOnlyList<string> audiences, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be empty.", nameof(text));
        if (audiences.Count != 3)
            throw new ArgumentException("Exactly three audiences are required.", nameof(audiences));

        string instructions = """
            You are an expert in communication and plain language. You rewrite texts so they
            fit a specific audience, without changing what the text is about.

            Respond only with valid JSON in exactly this format, with no other text:
            {"versions":[{"audience":"...","text":"..."}]}

            Write each rewritten text in the same language as the original text.
            Use the audience names exactly as given, in the same order.
            """;

        string userMessage = $"""
            Rewrite the text below three times, once for each of these audiences:
            1. {audiences[0]}
            2. {audiences[1]}
            3. {audiences[2]}

            Rules for every version:
            - Keep the central message and all important facts. Do not add new information.
            - Keep key terms and names from the original, but explain them if the audience
              would not understand them.
            - Adapt reading level, sentence length, word choice and tone to the audience.
            - Keep each version roughly as long as the original, unless the audience needs
              a shorter or slightly longer explanation.

            The three versions must be clearly different from each other:
            - For audiences without professional background, such as children or citizens,
              avoid jargon and use short sentences and everyday examples.
            - For professional audiences, use precise terminology and a concise, factual tone.
            - Children should be addressed directly and kindly, with simple words.

            Original text:
            ---
            {text}
            ---
            """;

        AiRequest request = new PromptBuilder(OpenAiModels.Gpt6Luna)
            .WithInstructions(instructions)
            .WithReasoningEffort(ReasoningEffort.Low)
            .WithMaxOutputTokens(6000)
            .AddUserMessage(userMessage)
            .Build();

        string json = await ai.GetResponseAsync(request, ct);   // <- the API call
        return Parse(json);
    }

    private static List<RewrittenText> Parse(string json)
    {
        json = json.Trim().Trim('`');
        if (json.StartsWith("json")) json = json[4..];

        var result = JsonSerializer.Deserialize<RewriteResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not read the response from the model.");

        if (result.Versions.Count != 3)
            throw new InvalidOperationException($"Expected 3 versions, but got {result.Versions.Count}.");

        return result.Versions;
    }
}