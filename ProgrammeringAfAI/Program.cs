using System.Text.Json;
using AiToolkit.Builders;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
#pragma warning disable OPENAI001

// ---------- Opsætning ----------
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

string apiKey = config["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI API key is missing.");

IAiService ai = new OpenAiService(new ResponsesClient(apiKey));

// ---------- 1. Input: præcis fem retter ----------
List<string> dishes = ReadFiveDishes();

// ---------- 2. Formulér prompten ----------
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

// ---------- 3. Kald OpenAI ----------
Console.WriteLine("\nFinder forslag...\n");
string json = await ai.GetResponseAsync(request);   // <- her sker API-kaldet

// ---------- 4. Vis resultatet ----------
List<DishSuggestion> suggestions = ParseSuggestions(json);

Console.WriteLine("Ti retter du måske også vil kunne lide:\n");
for (int i = 0; i < suggestions.Count; i++)
{
    Console.WriteLine($"{i + 1,2}. {suggestions[i].Name}");
    Console.WriteLine($"    {suggestions[i].Reason}\n");
}


// ---------- Hjælpemetoder ----------
static List<string> ReadFiveDishes()
{
    var dishes = new List<string>();
    Console.WriteLine("Indtast fem retter, du normalt spiser til aftensmad:");

    while (dishes.Count < 5)
    {
        Console.Write($"Ret {dishes.Count + 1}: ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("  Skriv venligst en ret.");
            continue;
        }
        if (dishes.Contains(input, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("  Den ret har du allerede skrevet.");
            continue;
        }
        dishes.Add(input);
    }
    return dishes;
}

static List<DishSuggestion> ParseSuggestions(string json)
{
    // Modeller pakker nogle gange JSON ind i ```json ... ``` - fjern det, hvis det sker
    json = json.Trim().Trim('`');
    if (json.StartsWith("json")) json = json[4..];

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var result = JsonSerializer.Deserialize<SuggestionResponse>(json, options)
        ?? throw new InvalidOperationException("Could not read the response from the model.");

    if (result.Dishes.Count != 10)
        throw new InvalidOperationException($"Expected 10 suggestions, but got {result.Dishes.Count}.");

    return result.Dishes;
}

record DishSuggestion(string Name, string Reason);
record SuggestionResponse(List<DishSuggestion> Dishes);