using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Repository
{
    public interface IFinancialRepository
    {
        Task<FinancialParameters> GetFinancialParameters(string[] _content, SimulationParameters simulationParms);
    }
}
