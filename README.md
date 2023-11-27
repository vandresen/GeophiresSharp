# GeophiresSharp
## Description
GeophiresSharp is a free and open-source geothermal techno-economic simulator. GEOPHIRES combines reservoir, wellbore, surface plant, and economic models to estimate the capital and operation and maintenance costs, instantaneous and lifetime energy production, and overall levelized cost of energy of a geothermal plant. Various reservoir conditions (EGS, doublets, etc.) and end-use options (electricity, direct-use heat, cogeneration) can be modeled. This tool is based on GEOPHIRES v2.0 and converted from Python into C# using Blazor for user interface.
## Warning
We are using Numpy for several calculations. Numpy is not threadsafe so the subsequent runs may not work. We are working on fixing this

GeophiresSharp is based on [GeoPhires-2](https://github.com/NREL/GEOPHIRES-v2) that is written in Python. We have converted this to C# and using Blazor for user interface.
