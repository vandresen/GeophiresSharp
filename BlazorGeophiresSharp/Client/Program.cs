using BlazorGeophiresSharp.Client;
using BlazorGeophiresSharp.Client.Services;
using GeophiresLibrary.Models;
using GeophiresLibrary.Repository;
using GeophiresLibrary.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLogging(logging =>
{
    logging.AddProvider(new MyConsoleLoggerProvider());
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

builder.Services.AddScoped<IDisplayMessage, DisplayMessage>();
builder.Services.AddScoped<SimulationParameters>();
builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();

await builder.Build().RunAsync();
