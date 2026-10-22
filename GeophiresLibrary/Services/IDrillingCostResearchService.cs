using static GeophiresLibrary.Models.DrillingCostModels;

namespace GeophiresLibrary.Services
{
    public interface IDrillingCostResearchService
    {
        Task<DrillingCostRecommendation> ResearchAsync(
            DrillingCostResearchRequest request,
            CancellationToken cancellationToken = default);
    }
}
