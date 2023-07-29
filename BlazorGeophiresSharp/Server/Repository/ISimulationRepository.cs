using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public interface ISimulationRepository
    {
        Task<SimulationParameters> GetSimulationParameters();
    }
}
