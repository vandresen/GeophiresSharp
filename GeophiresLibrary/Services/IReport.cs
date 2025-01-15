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
        void EngineeringReport(SubsurfaceTechnicalParameters sstParms, SurfaceTechnicalParameters stParms,
            SimulationParameters simParms, CalculatedResults calcResult);
        void ResourceCharacteristicsReport(SubsurfaceTechnicalParameters sstParms);
        void ReservoirReport(SubsurfaceTechnicalParameters sstParms, CalculatedResults calcResult);
        void CapitalCostReport(CapitalAndOMCostParameters ccParms, SubsurfaceTechnicalParameters sstParms,
            FinancialParameters finParms, CalculatedResults calcResult, SimulationParameters simParms);
        void OperatingMaintenanceCostReport(SimulationParameters simParms, CapitalAndOMCostParameters ccParms,
            CalculatedResults calcResult);
        void PowerGenerationReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms,
            CalculatedResults calcResult);
        void PowerGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms, CalculatedResults calcResult);
        void EnergyGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms, CalculatedResults calcResult);
    }
}
