using BlazorGeophires;
using GeophiresLibrary.Repository;
using GeophiresLibrary.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();
builder.Services.AddScoped<ISurfaceTechnicalRepository, SurfaceTechnicalRepository>();
builder.Services.AddScoped<ISubsurfaceTechnicalRepository, SubsurfaceTechnicalRepository>();
builder.Services.AddScoped<ICapitalAndOMCostRepository, CapitalAndOMCostRepository>();
builder.Services.AddScoped<IFinancialRepository, FinancialRepository>();
builder.Services.AddScoped<IModeling, GeohiresModeling>();

await builder.Build().RunAsync();
