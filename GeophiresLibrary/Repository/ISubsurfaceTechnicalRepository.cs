using GeophiresLibrary.Models;

namespace GeophiresLibrary.Repository
{
    public interface ISubsurfaceTechnicalRepository
    {
        Task<SubsurfaceTechnicalParameters> GetSubsurfaceTechnicalParameters(string[] _content, SimulationParameters simulationParms);
    }
}
