using Gui.Web.Components;
using MudBlazor.Services;
using AiToolkit.Contracts;
using AiToolkit.Providers.OpenAI;
using MudBlazor.Services;
using OpenAI.Responses;
using Gui.Web.Subjects.IntegreretAI.Opgave1;
using Gui.Web.Subjects.IntegreretAI.Opgave2;
using Gui.Web.Subjects.IntegreretAI.Opgave3;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// AI toolkit
string apiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI API key is missing. Set 'OpenAI:ApiKey' in user secrets.");

builder.Services.AddSingleton(new ResponsesClient(apiKey));
builder.Services.AddSingleton<IAiService, OpenAiService>();

// Assignment services
builder.Services.AddScoped<DishSuggestionService>();
builder.Services.AddScoped<TravelSuggestionService>();
builder.Services.AddScoped<TextRewriteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
