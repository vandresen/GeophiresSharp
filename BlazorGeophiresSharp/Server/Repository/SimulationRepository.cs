using BlazorGeophiresSharp.Server.Extensions;
using BlazorGeophiresSharp.Server.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public class SimulationRepository : ISimulationRepository
    {
        private readonly string[] _content;
        private readonly ILogger _logger;

        public SimulationRepository(string[] content, ILogger logger)
        {
            _content = content;
            _logger = logger;
        }

        public async Task<SimulationParameters> GetSimulationParameters()
        {
            var simulationParms = new SimulationParameters();
            List<int> valid_printoutput = new List<int>() { 0, 1 };
            simulationParms.printoutput = _content.GetIntParameter("Print Output to Console,", 1, valid_printoutput);

            int? iTemp;
            simulationParms.timestepsperyear = 4;
            iTemp = _content.GetIntFromContent("Time steps per year");
            if (iTemp == null) _logger.LogWarning($"Warning: No valid number of time steps per year provided. GEOPHIRES will assume default number of time steps per year (4)");
            else simulationParms.timestepsperyear = (int)iTemp;
            if (simulationParms.timestepsperyear < 1 || simulationParms.timestepsperyear > 101)
            {
                _logger.LogWarning($"No valid number of time steps per year provided. GEOPHIRES will assume default number of time steps per year (4)");
                simulationParms.timestepsperyear = 4;
            }

            //enduseoption
            //enduseoption = 1: electricitys
            //enduseoption = 2: direct-use heat
            //enduseoption = 3: cogen topping cycle
            //enduseoption = 4: cogen bottoming cycle
            //enduseoption = 5: cogen split of mass flow rate
            List<int> valid_enduseoption = new List<int>() { 1, 2, 31, 32, 41, 42, 51, 52 };
            int enduseoption = _content.GetIntParameter("End-Use Option,", 1, valid_enduseoption);
            simulationParms.enduseoption = enduseoption;

            //ptype: power plant type
            //pptype = 1: Subcritical ORC
            //pptype = 2: Supercritical ORC
            //pptype = 3: Single-Flash
            //pptype = 4: Double-Flash
            List<int> valid_pptype = new List<int>() { 1, 2, 3, 4 };
            int pptype = _content.GetIntParameter("Power Plant Type,", 1, valid_pptype);
            simulationParms.pptype = pptype;

            return simulationParms;
        }
    }
}
