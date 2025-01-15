using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Services
{
    public interface IReport
    {
        string GetReport();
        void SummaryReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms, CalculatedResults calcResult);
        void EconomicReport(FinancialParameters finParms, SurfaceTechnicalParameters stParms);
        Task EngineeringReport(SubsurfaceTechnicalParameters sstParms, SurfaceTechnicalParameters stParms,
            SimulationParameters simParms, CalculatedResults calcResult);
        Task ResourceCharacteristicsReport(SubsurfaceTechnicalParameters sstParms);
        Task ReservoirReport(SubsurfaceTechnicalParameters sstParms, CalculatedResults calcResult);
        Task CapitalCostReport(CapitalAndOMCostParameters ccParms, SubsurfaceTechnicalParameters sstParms,
            FinancialParameters finParms, CalculatedResults calcResult, SimulationParameters simParms);
        Task OperatingMaintenanceCostReport(SimulationParameters simParms, CapitalAndOMCostParameters ccParms,
            CalculatedResults calcResult);
        Task PowerGenerationReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms,
            CalculatedResults calcResult);
        Task PowerGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms, CalculatedResults calcResult);
        Task EnergyGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms, CalculatedResults calcResult);
    }
}
