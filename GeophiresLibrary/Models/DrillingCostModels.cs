namespace GeophiresLibrary.Models
{
    public class DrillingCostModels
    {
        public sealed record DrillingCostResearchRequest(
        string? Location,
        double? Latitude,
        double? Longitude,
        double? DepthMeters,
        int ProductionWells,
        int InjectionWells,
        double? ProductionDiameterMeters,
        double? InjectionDiameterMeters,
        string WellCorrelation,
        string EndUse);

        public sealed record DrillingCostSource(
            string Title,
            string Note);

        public sealed record DrillingCostRecommendation(
            double CostPerWellMUSD,   // direct cost, one well, excludes indirect % and adjustment factor
            double LowMUSD,
            double HighMUSD,
            int? DollarYear,
            string Rationale,
            IReadOnlyList<DrillingCostSource> Sources);
    }
}
