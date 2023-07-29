using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public interface ICapitalAndOMCostRepository
    {
        Task<CapitalAndOMCostParameters> GetCapitalAndOMCostParameters(SimulationParameters simulationParms);
    }
}
