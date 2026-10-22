using GeophiresLibrary.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using static GeophiresLibrary.Models.DrillingCostModels;

namespace GeophiresLibrary.Services
{
    public class DrillingCostResearchService : IDrillingCostResearchService
    {
        private readonly ILLMBaseService _llm;
        private readonly AiSettings _settings;
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string SystemPrompt = """
        You are a geothermal drilling cost analyst. Estimate the DIRECT drilling and
        completion cost PER WELL, in millions of USD (MUSD), for the project described.

        Rules:
        - Return the cost for ONE well, not all wells combined.
        - Return DIRECT cost only. Exclude contingency/indirect capital costs and exclude
          any user-side adjustment factor; both are applied downstream.
        - Base the estimate on published geothermal well-cost data you know well
          (e.g. NREL, DOE GTO, GEOPHIRES documentation, Lukawski et al., GRC papers).
        - You cannot browse the web. List references by title and publisher only.
          Do NOT include URLs and do NOT invent references.
        - If you must scale from a different depth or diameter, say how.
        - If no location or coordinates are given, say in the rationale that the estimate is generic and not location-specific, and widen the low/high range accordingly.
        - State the dollar-year your figure is expressed in. Do not adjust for inflation unless you say so.
        - If you cannot give a defensible estimate, set "cost_per_well_musd" to null.
        - If a place name is given, use it; otherwise use the coordinates. State in the rationale which one you relied on.

        Respond with ONLY a JSON object, no prose and no markdown fences:
        {
          "cost_per_well_musd": number | null,
          "low_musd": number | null,
          "high_musd": number | null,
          "dollar_year": number | null,
          "rationale": string,
          "sources": [ { "title": string, "note": string } ]
        }
        """;

        public DrillingCostResearchService(ILLMBaseService llm, AiSettings settings)
        {
            _llm = llm;
            _settings = settings;
        }

        public async Task<DrillingCostRecommendation> ResearchAsync(DrillingCostResearchRequest r, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.OpenAIApiKey))
                throw new InvalidOperationException("OpenAI API key is not configured.");

            var coords = r.Latitude is double la && r.Longitude is double lo ? $"{la:N4}, {lo:N4}"
                : "not specified";

            var userPrompt = $"""
                Project location: {(string.IsNullOrWhiteSpace(r.Location) ? "not specified" : r.Location.Trim())}
                Coordinates (lat, lon): {coords}
                Well depth: {r.DepthMeters?.ToString("N0") ?? "unknown"} m
                Production wells: {r.ProductionWells}, injection wells: {r.InjectionWells}
                Production borehole diameter: {r.ProductionDiameterMeters?.ToString("N3") ?? "unknown"} m
                Injection borehole diameter: {r.InjectionDiameterMeters?.ToString("N3") ?? "unknown"} m
                Well type / correlation: {r.WellCorrelation}
                End use: {r.EndUse}

                Estimate the direct drilling and completion cost per well in MUSD.
                """;

            var requestBody = new
            {
                model = _settings.Model,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_settings.OpenAIApiKey}"
            };

            string raw = await _llm.SendRawAsync(
                HttpMethod.Post, Endpoint, requestBody, headers, cancellationToken);

            using var envelope = JsonDocument.Parse(raw);
            string content = envelope.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var dto = JsonSerializer.Deserialize<ResearchDto>(content, JsonOptions)
                      ?? throw new InvalidOperationException("Empty model response.");

            if (dto.CostPerWellMusd is not double cost)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(dto.Rationale)
                        ? "The model could not give a defensible estimate."
                        : dto.Rationale);

            var sources = (dto.Sources ?? new List<ReferenceDto>())
                .Select(s => new DrillingCostSource(
                    s.Title ?? "Reference",
                    s.Note ?? ""))
                .ToList();

            var result = new DrillingCostRecommendation(
                cost,
                dto.LowMusd ?? cost,
                dto.HighMusd ?? cost,
                dto.DollarYear,
                dto.Rationale ?? "",
                sources);

            Validate(result, r);

            return result;

        }

        private static void Validate(DrillingCostRecommendation res, DrillingCostResearchRequest r)
        {
            if (!double.IsFinite(res.CostPerWellMUSD) ||
                res.CostPerWellMUSD <= 0 ||
                res.CostPerWellMUSD > 200)
            {
                throw new InvalidOperationException(
                    "Returned cost is outside the permitted range.");
            }

            if (!double.IsFinite(res.LowMUSD) ||
                !double.IsFinite(res.HighMUSD) ||
                res.LowMUSD < 0 ||
                res.HighMUSD < res.LowMUSD ||
                res.CostPerWellMUSD < res.LowMUSD ||
                res.CostPerWellMUSD > res.HighMUSD ||
                res.HighMUSD > 200)
            {
                throw new InvalidOperationException(
                    "Returned cost range is invalid.");
            }

            if (res.DollarYear is < 1900 or > 2100)
            {
                throw new InvalidOperationException(
                    "Returned dollar year is invalid.");
            }

            if (r.DepthMeters is > 0)
            {
                var perKm =
                    res.CostPerWellMUSD / (r.DepthMeters.Value / 1000.0);

                if (!double.IsFinite(perKm) || perKm < 0.3 || perKm > 15)
                {
                    throw new InvalidOperationException(
                        $"Returned cost ({perKm:N1} MUSD/km) is outside " +
                        "the configured plausibility range.");
                }
            }
        }

        private sealed class ResearchDto
        {
            [JsonPropertyName("cost_per_well_musd")] public double? CostPerWellMusd { get; set; }
            [JsonPropertyName("low_musd")] public double? LowMusd { get; set; }
            [JsonPropertyName("high_musd")] public double? HighMusd { get; set; }
            [JsonPropertyName("dollar_year")] public int? DollarYear { get; set; }
            [JsonPropertyName("rationale")] public string? Rationale { get; set; }
            [JsonPropertyName("sources")] public List<ReferenceDto>? Sources { get; set; }
        }

        private sealed class ReferenceDto
        {
            [JsonPropertyName("title")] public string? Title { get; set; }
            [JsonPropertyName("note")] public string? Note { get; set; }
        }

    }
}
