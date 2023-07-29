using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public interface IFinancialRepository
    {
        Task<FinancialParameters> GetFinancialParameters(SimulationParameters simulationParms);
    }
}
