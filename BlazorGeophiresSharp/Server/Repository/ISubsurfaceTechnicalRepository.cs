using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public interface ISubsurfaceTechnicalRepository
    {
        Task<SubsurfaceTechnicalParameters> GetSubsurfaceTechnicalParameters(SimulationParameters simulationParms);
    }
}
