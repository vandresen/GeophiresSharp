using GeophiresSharp.Models;
using System.Threading.Tasks;

namespace GeophiresSharp.Repository
{
    public interface ICapitalAndOMCostRepository
    {
        Task<CapitalAndOMCostParameters> GetCapitalAndOMCostParameters(SimulationParameters simulationParms);
    }
}
