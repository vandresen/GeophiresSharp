using BlazorGeophires;
using GeophiresLibrary.Models;
using GeophiresLibrary.Repository;
using GeophiresLibrary.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient();

builder.Services.AddMudServices();
builder.Services.AddSingleton(new AiSettings());   // added
builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();
builder.Services.AddScoped<ISurfaceTechnicalRepository, SurfaceTechnicalRepository>();
builder.Services.AddScoped<ISubsurfaceTechnicalRepository, SubsurfaceTechnicalRepository>();
builder.Services.AddScoped<ICapitalAndOMCostRepository, CapitalAndOMCostRepository>();
builder.Services.AddScoped<IFinancialRepository, FinancialRepository>();
builder.Services.AddScoped<ILLMBaseService, LLMBaseService>();
builder.Services.AddScoped<IDrillingCostResearchService, DrillingCostResearchService>();
builder.Services.AddScoped<IModeling, GeohiresModeling>();

var host = builder.Build();                         // added

var settings = host.Services.GetRequiredService<AiSettings>();
var js = host.Services.GetRequiredService<IJSRuntime>();
try
{
    settings.OpenAIApiKey =
        await js.InvokeAsync<string?>("localStorage.getItem", AiSettings.StorageKey) ?? "";
}
catch { }

await host.RunAsync();                              // changed