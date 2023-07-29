using GeophiresSharp.Models;
using System.Threading.Tasks;

namespace GeophiresSharp.Repository
{
    public interface IFinancialRepository
    {
        Task<FinancialParameters> GetFinancialParameters(SimulationParameters simulationParms);
    }
}
