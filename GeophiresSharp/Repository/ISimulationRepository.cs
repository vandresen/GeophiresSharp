using GeophiresSharp.Models;
using System.Threading.Tasks;

namespace GeophiresSharp.Repository
{
    public interface ISimulationRepository
    {
        Task<SimulationParameters> GetSimulationParameters();
    }
}
