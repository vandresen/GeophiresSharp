using GeophiresLibrary.Models;

namespace GeophiresLibrary.Repository
{
    public interface ISimulationRepository
    {
        Task<SimulationParameters> GetSimulationParameters(string[] _content);
    }
}
