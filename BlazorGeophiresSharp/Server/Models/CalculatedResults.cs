using Numpy;

namespace BlazorGeophiresSharp.Server.Models
{
    public class CalculatedResults
    {
        public NDarray FirstLawEfficiency { get; set; }
        public double[] NetElectricityProduced { get; set; }
        public double[] ProdTempDrop { get; set; }
        public double[] Availability { get; set; }
        public double[] PumpingPower { get; set; }
        public double[] NetkWhProduced { get; set; }
        public double[] ProducedTemperature { get; set; }
        public double[] HeatkWhExtracted { get; set; }
        public double[] RemainingReservoirHeatContent { get; set; }
        public double[] DP { get; set; }
        public double[] DP1 { get; set; }
        public double[] DP2 { get; set; }
        public double[] DP3 { get; set; }
        public double[] DP4 { get; set; }
        public double[] HeatProduced { get; set; }
        public double[] HeatkWhProduced { get; set; }
        public double Price { get; set; }
        public double Trock { get; set; }
        public double Pprodwellhead { get; set; }
        public double Cwell { get; set; }
        public double Cstim { get; set; }
        public double Cplant { get; set; }
        public double Cgath { get; set; }
        public double Cpiping { get; set; }
        public double Cexpl { get; set; }
        public double Ccap { get; set; }
        public double Coamwell { get; set; }
        public double Coamplant { get; set; }
        public double Coamwater { get; set; }
        public double Coam { get; set; }
        public double averageannualpumpingcosts { get; set; }
        public double InitialReservoirHeatContent { get; set; }
        public int redrill { get; set; }
    }
}
