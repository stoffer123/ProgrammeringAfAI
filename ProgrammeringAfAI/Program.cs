using AiToolkit.Builders;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

string openAiApiKey = config["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI API key is missing. Set 'OpenAI:ApiKey' in user secrets.");

IAiService ai = new OpenAiService(new ResponsesClient(openAiApiKey));

// List the models the toolkit supports
Console.WriteLine("Supported models:");
foreach (var m in ai.SupportedModels)
{
    Console.WriteLine($"  {m.Id,-14} sampling: {m.SupportsSampling,-5} reasoning: {m.SupportsReasoning,-5}"
        + (m.IsDeprecated ? $"  shuts down {m.ShutdownDate}" : ""));
}
Console.WriteLine();

AiRequest request = new PromptBuilder(OpenAiModels.Gpt6Luna)
    .WithInstructions("""
        You are a meal planner for a family of 2 adults and 2 children (aged 4 and 2).
        The family is allergic to lactose and gluten.
        Hard cheeses are tolerated.
        Respond in Danish.
        """)
    .WithReasoningEffort(ReasoningEffort.Low)
    .WithMaxOutputTokens(4000)
    .AddUserMessage("""
        Create a 4-day meal plan.
        Make it child-friendly, and include pasta where possible.
        Add a lunch suggestion for each day.
        Finish with a combined shopping list.
        """)
    .Build();

string answer = await ai.GetResponseAsync(request);
Console.WriteLine(answer);