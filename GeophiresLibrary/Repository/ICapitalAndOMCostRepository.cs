using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Repository
{
    public interface ICapitalAndOMCostRepository
    {
        CapitalAndOMCostParameters GetCapitalAndOMCostParameters(string[] _content, SimulationParameters simulationParms);
    }
}
