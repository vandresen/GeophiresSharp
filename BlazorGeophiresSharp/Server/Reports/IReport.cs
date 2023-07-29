using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Reports
{
    public interface IReport
    {
        string GetReport();
        Task SummaryReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms, CalculatedResults calcResult);
        Task EconomicReport(FinancialParameters finParms, SurfaceTechnicalParameters stParms);
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
