using GeophiresLibrary.Models;

namespace GeophiresLibrary.Repository
{
    public interface ISubsurfaceTechnicalRepository
    {
        SubsurfaceTechnicalParameters GetSubsurfaceTechnicalParameters(string[] _content, SimulationParameters simulationParms);
    }
}
