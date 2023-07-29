using GeophiresSharp.Models;
using System.Threading.Tasks;

namespace GeophiresSharp.Repository
{
    public interface ISubsurfaceTechnicalRepository
    {
        Task<SubsurfaceTechnicalParameters> GetSubsurfaceTechnicalParameters(SimulationParameters simulationParms);
    }
}
