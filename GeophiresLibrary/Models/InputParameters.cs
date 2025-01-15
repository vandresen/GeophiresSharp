namespace GeophiresLibrary.Models
{
    public class InputParameters
    {
        public string Content { get; set; }
        public string TempDataContent { get; set; }
        public SimulationParameters simParms { get; set; }
        public SubsurfaceTechnicalParameters subsurfaceParms { get; set; }
        public SurfaceTechnicalParameters surfaceParms { get; set; }
        public FinancialParameters financialParms { get; set; }
        public CapitalAndOMCostParameters capitalParms { get; set; }
    }
}
