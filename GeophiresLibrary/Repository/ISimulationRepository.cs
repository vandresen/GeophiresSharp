using GeophiresLibrary.Models;

namespace GeophiresLibrary.Repository
{
    public interface ISimulationRepository
    {
        SimulationParameters GetSimulationParameters(string[] _content);
    }
}
