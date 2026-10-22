# GeophiresSharp
## Description
GeophiresSharp is a free and open-source geothermal techno-economic simulator. GEOPHIRES combines reservoir, wellbore, surface plant, and economic models to estimate the capital and operation and maintenance costs, instantaneous and lifetime energy production, and overall levelized cost of energy of a geothermal plant. Various reservoir conditions (EGS, doublets, etc.) and end-use options (electricity, direct-use heat, cogeneration) can be modeled. 

This tool is based on [GEOPHIRES v2.0](https://github.com/NREL/GEOPHIRES-v2) and converted from Python into C# using Blazor for the user interface. Selected
features from [GEOPHIRES-X](https://github.com/NREL/GEOPHIRES-X) have been added, including project latitude and longitude. It is not a full port of GEOPHIRES-X,
and results may differ from either Python version.

## AI Drilling Cost Assistant (optional)

The Capital and O&M Cost tab includes an AI assistant that suggests a drilling and
completion cost per well. It is optional and the simulator works without it.

- It uses the OpenAI API, so you need your own API key. Enter it in the assistant
  dialog. The key is stored only in your browser's local storage and is never
  committed or sent anywhere except to OpenAI.
- The estimate comes from the model's general knowledge of published geothermal
  well costs. It does not search the web, and the references it lists are
  unverified. Treat it as a starting point and check it against your own data.
- The suggestion is a direct cost per well (excluding the indirect cost percentage
  and the adjustment factor, which GEOPHIRES applies afterwards).

## Building

Requires the .NET 10 SDK.

    dotnet restore BlazorGeophires/BlazorGeophires.csproj
    dotnet run --project BlazorGeophires/BlazorGeophires.csproj

