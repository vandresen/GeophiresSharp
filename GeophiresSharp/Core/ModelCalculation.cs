using GeophiresSharp.Models;
using GeophiresSharp.Repository;
using Numpy;
using System.Linq;
using System.Threading.Tasks;

namespace GeophiresSharp.Core
{
    public class ModelCalculation
    {
        private CalculatedResults calcResult;
        private SubsurfaceTechnicalParameters sstParms;
        private SurfaceTechnicalParameters stParms;
        private FinancialParameters finParms;
        private CapitalAndOMCostParameters ccParms;
        private SimulationParameters simulationParms;

        public ModelCalculation()
        {

        }

        public async Task ReadFromRepository(string[] content)
        {
            ISimulationRepository simRep = new SimulationRepository(content);
            simulationParms = await simRep.GetSimulationParameters();

            ICapitalAndOMCostRepository ccRep = new CapitalAndOMCostRepository(content);
            ccParms = await ccRep.GetCapitalAndOMCostParameters(simulationParms);

            ISubsurfaceTechnicalRepository sstRep = new SubsurfaceTechnicalRepository(content);
            sstParms = await sstRep.GetSubsurfaceTechnicalParameters(simulationParms);

            ISurfaceTechnicalRepository stRep = new SurfaceTechnicalRepository(content);
            stParms = await stRep.GetSurfaceTechnicalParameters(sstParms);

            IFinancialRepository finRep = new FinancialRepository(content);
            finParms = await finRep.GetFinancialParameters(simulationParms);
        }

        public void CalculateModel()
        {
            double[] intersecttemperature = CalculateMaximumWellDepth();
            CalculateInitialReservoirTemperature(intersecttemperature);
        }

        public string CreateReport()
        {
            string report = "This is the report";
            return report;
        }

        private void CalculateInitialReservoirTemperature(double[] intersecttemperature)
        {
            var tempList = intersecttemperature.ToList();
            tempList.Insert(0, stParms.Tsurf);
            intersecttemperature = tempList.ToArray();

            var npTotaldepth = Numpy.np.append(Numpy.np.array(new[] { 0.0 }), Numpy.np.cumsum(sstParms.layerthickness));
            var totaldepth = npTotaldepth.GetData<double>();
            double max = -99999999.0;
            int index = -1;
            int temperatureindex = -1;
            foreach (var item in totaldepth)
            {
                index++;
                if (sstParms.depth > item)
                {
                    if (item > max)
                    {
                        max = item;
                        temperatureindex = index;
                    }
                }
            }
            double tmpDepth = (double)totaldepth[temperatureindex];
            double Trock = intersecttemperature[temperatureindex] + sstParms.gradient[temperatureindex] * (sstParms.depth - tmpDepth);
            calcResult.Trock = Trock;

            // calculate average geothermal gradient
            double averagegradient;
            if (sstParms.numseg == 1)
                averagegradient = sstParms.gradient[0];
            else
                averagegradient = (Trock - stParms.Tsurf) / sstParms.depth;

            // specify time-stepping vectors THIS MUST BE PUT IN A SCRIPT WHEM THE TWO VARAIABLES BELOW ARE NEEDED
            //timevector = np.linspace(0, plantlifetime, timestepsperyear * plantlifetime + 1)
            var timevector = np.linspace(0, finParms.plantlifetime, (simulationParms.timestepsperyear * finParms.plantlifetime + 1));
            //var Tresoutput = np.zeros(timevector.len);

            // calculate reservoir water properties
            double cpwater = Utilities.heatcapacitywater(sstParms.Tinj * 0.5 + (Trock * 0.9 + sstParms.Tinj * 0.1) * 0.5); //J/kg/K (based on TARB in Geophires v1.2)
            double rhowater = Utilities.densitywater(sstParms.Tinj * 0.5 + (Trock * 0.9 + sstParms.Tinj * 0.1) * 0.5);

            // temperature gain in injection wells
            sstParms.Tinj = sstParms.Tinj + sstParms.tempgaininj;
        }

        private double[] CalculateMaximumWellDepth()
        {
            double[] intersecttemperature = { 1000.0, 1000.0, 1000.0, 1000.0 };
            double maxdepth = 0;
            string pyCmd = "";
            string comma = "";
            if (sstParms.numseg == 1)
            {
                maxdepth = (sstParms.Tmax - stParms.Tsurf) / sstParms.gradient[0];
            }
            else
            {
                maxdepth = 0;
                intersecttemperature[0] = stParms.Tsurf + sstParms.gradient[0] * sstParms.layerthickness[0];
                for (int i = 1; i < sstParms.numseg - 1; i++)
                {
                    intersecttemperature[i] = intersecttemperature[i - 1] + sstParms.gradient[i] * sstParms.layerthickness[i];
                }

                int layerindex = -1;

                for (int i = 0; i < intersecttemperature.Length; i++)
                {
                    if (intersecttemperature[i] > sstParms.Tmax)
                    {
                        layerindex = i;
                        break;
                    }
                }

                if (layerindex > 0)
                {
                    for (int i = 0; i < layerindex; i++)
                    {
                        maxdepth = maxdepth + sstParms.layerthickness[i];
                    }
                    maxdepth = maxdepth + (sstParms.Tmax - intersecttemperature[layerindex - 1]) / sstParms.gradient[layerindex];
                }
                else
                {
                    maxdepth = (sstParms.Tmax - stParms.Tsurf) / sstParms.gradient[0];
                }
            }
            if (sstParms.depth > maxdepth) sstParms.depth = maxdepth;
            return intersecttemperature;
        }
    }
}
