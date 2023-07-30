using BlazorGeophiresSharp.Server.Extensions;
using BlazorGeophiresSharp.Server.Models;
using BlazorGeophiresSharp.Server.Reports;
using BlazorGeophiresSharp.Server.Repository;
using Microsoft.Extensions.Logging;
using Numpy;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace BlazorGeophiresSharp.Server.Core
{
    public class ModelCalculation
    {
        private readonly ILogger<ModelCalculation> _logger;
        private CalculatedResults calcResult;
        private SubsurfaceTechnicalParameters sstParms;
        private SurfaceTechnicalParameters stParms;
        private FinancialParameters finParms;
        private CapitalAndOMCostParameters ccParms;
        private SimulationParameters simulationParms;
        private int redrill = 0;
        private double rhowater;
        private double cpwater;
        private double Trock;
        private double averagegradient;
        private double Cwell;
        private double Cgath;
        private double Cstim;
        private double Ccap;
        private double Coam;
        private double Cplant = 0;
        private double[] NetElectricityProduced;
        private double[] Tresoutput;
        private double[] ProdTempDrop;
        private double[] pumpdepth;
        private double[] PumpingPower;
        private double[] PumpingPowerProd;
        private double[] PumpingPowerInj;
        private double[] ProducedTemperature;
        private double[] TenteringPP;
        private double[] HeatExtracted;
        private double[] HeatProduced;
        private double[] ElectricityProduced;
        private NDarray nptimevector;
        private NDarray PumpingkWh;
        private double[] timeVector;

        public ModelCalculation(ILogger<ModelCalculation> logger)
        {
            calcResult = new CalculatedResults();
            _logger = logger;
        }

        public async Task ReadFromRepository(string[] content)
        {
            ISimulationRepository simRep = new SimulationRepository(content, _logger);
            simulationParms = await simRep.GetSimulationParameters();

            ICapitalAndOMCostRepository ccRep = new CapitalAndOMCostRepository(content, _logger);
            ccParms = await ccRep.GetCapitalAndOMCostParameters(simulationParms);

            ISubsurfaceTechnicalRepository sstRep = new SubsurfaceTechnicalRepository(content, _logger);
            sstParms = await sstRep.GetSubsurfaceTechnicalParameters(simulationParms);

            ISurfaceTechnicalRepository stRep = new SurfaceTechnicalRepository(content, _logger);
            stParms = await stRep.GetSurfaceTechnicalParameters(sstParms);

            IFinancialRepository finRep = new FinancialRepository(content, _logger);
            finParms = await finRep.GetFinancialParameters(simulationParms);
        }

        public void CalculateModel()
        {
            np.arange(1);
            //PythonEngine.BeginAllowThreads();
            double[] intersecttemperature = CalculateMaximumWellDepth();
            CalculateInitialReservoirTemperature(intersecttemperature);
            CalculateReservoirTemperatureOutput();
            _logger.LogInformation("Start calculate wellbore temp");
            CalculateWellboreTemperatureDrop();
            CalculatePressureDropsAndPumpingPower();
            CalculateEnergyExtractedAndProduced();
            _logger.LogInformation("Start calculate cost");
            CapitalCosts();
            OandCosts();
            CalculateAnnualElectricityAndHeatProduction();
            CalculateReservoirHeatContent();
            CalculateLCOEandLCOH();
        }

        public async Task<string> CreateReport()
        {
            IReport report = new ConsoleReport();
            await report.SummaryReport(simulationParms, sstParms, calcResult);
            await report.EconomicReport(finParms, stParms);
            await report.EngineeringReport(sstParms, stParms, simulationParms, calcResult);
            await report.ResourceCharacteristicsReport(sstParms);
            await report.ReservoirReport(sstParms, calcResult);
            await report.CapitalCostReport(ccParms, sstParms, finParms, calcResult, simulationParms);
            await report.OperatingMaintenanceCostReport(simulationParms, ccParms, calcResult);
            await report.PowerGenerationReport(simulationParms, sstParms, calcResult);
            await report.PowerGenerationProfileReport(simulationParms, finParms, calcResult);
            await report.EnergyGenerationProfileReport(simulationParms, finParms, calcResult);
            string reportOutput = report.GetReport();
            return reportOutput;
        }

        private void CalculateInitialReservoirTemperature(double[] intersecttemperature)
        {
            var tempList = intersecttemperature.ToList();
            tempList.Insert(0, stParms.Tsurf);
            intersecttemperature = tempList.ToArray();
            double[] tempArray = { 0.0 };
            double sum = 0;
            double[] cumSumArray = sstParms.layerthickness.Select(value => sum += value).ToArray();
            double[] totaldepth = tempArray.Append(cumSumArray);
            
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
            Trock = intersecttemperature[temperatureindex] + sstParms.gradient[temperatureindex] * (sstParms.depth - tmpDepth);
            calcResult.Trock = Trock;
            
            // calculate average geothermal gradient
            if (sstParms.numseg == 1)
                averagegradient = sstParms.gradient[0];
            else
                averagegradient = (Trock - stParms.Tsurf) / sstParms.depth;

            // specify time-stepping vectors THIS MUST BE PUT IN A SCRIPT WHEM THE TWO VARAIABLES BELOW ARE NEEDED
            //timevector = np.linspace(0, plantlifetime, timestepsperyear * plantlifetime + 1)
            int numPoints = simulationParms.timestepsperyear * finParms.plantlifetime + 1;
            double start = 0.0;
            double end = finParms.plantlifetime;

            timeVector = Utilities.linspace(start, end, numPoints);
            //var Tresoutput = np.zeros(timevector.len);
            
            // calculate reservoir water properties
            cpwater = Utilities.heatcapacitywater(sstParms.Tinj * 0.5 + (Trock * 0.9 + sstParms.Tinj * 0.1) * 0.5); //J/kg/K (based on TARB in Geophires v1.2)
            rhowater = Utilities.densitywater(sstParms.Tinj * 0.5 + (Trock * 0.9 + sstParms.Tinj * 0.1) * 0.5);

            // temperature gain in injection wells
            sstParms.Tinj = sstParms.Tinj + sstParms.tempgaininj;
            
        }

        private double[] CalculateMaximumWellDepth()
        {
            double[] intersecttemperature = { 1000.0, 1000.0, 1000.0, 1000.0 };
            double maxdepth = 0;
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

        private void CalculateReservoirTemperatureOutput()
        {
            // calculate reservoir temperature output (internal or external)
            // resoption = 1  Multiple parallel fractures model (LANL)                            
            // resoption = 2  Volumetric block model (1D linear heat sweep model (Stanford))                       
            // resoption = 3  Drawdown parameter model (Tester)
            // resoption = 4  Thermal drawdown percentage model (GETEM)
            // resoption = 5  Generic user-provided temperature profile
            // resoption = 6  Tough2 is called
            if (sstParms.resoption == 1)
            {
                // convert flowrate to volumetric rate
                double q = sstParms.nprod * sstParms.prodwellflowrate / rhowater; // m^3/s

                // specify Laplace-space function
                double resParms = rhowater * cpwater * (q / sstParms.fracnumb / sstParms.fracwidth) * (sstParms.fracsep / 2.0) / (2.0 * sstParms.krock * sstParms.fracheight);
                LaplaceFunction fp = (s) => { return (1.0 / s) * Complex.Exp(-Complex.Sqrt(s) * Complex.Tanh(resParms * Complex.Sqrt(s))); };

                // calculate non-dimensional time
                double timeParms = Math.Pow((rhowater * cpwater), 2) / (4 * sstParms.krock * sstParms.rhorock * sstParms.cprock) * Math.Pow((q / sstParms.fracnumb / sstParms.fracwidth / sstParms.fracheight), 2);
                double scalar = timeParms * 365.0 * 24.0 * 3600;
                //var td = nptimevector * scalar;
                double[] td = timeVector.Select(x => x * scalar).ToArray();

                // calculate non-dimensional temperature array
                ILaplaceInversion pi = new LaplaceInversionTalbot();
                double[] temptd = new double[td.Length - 1]; ;
                for (int i = 1; i < td.Length; i++)
                {
                    temptd[i - 1] = (double)td[i];
                }
                double[] Twnd = pi.Calculate(fp, temptd);
                scalar = Trock - sstParms.Tinj;
                double[] tempTwnd = Twnd.Select(x => x * scalar).ToArray();
                double[] tempTresoutput = tempTwnd.Select(x => Trock - x).ToArray();
                Tresoutput = new double[tempTresoutput.Length + 1];
                Tresoutput[0] = Trock;
                for (int i = 0; i < tempTresoutput.Length; i++)
                {
                    Tresoutput[i + 1] = (double)tempTresoutput[i];
                }
            }
            else if (sstParms.resoption == 2)
            {
                // specify rock properties
                double phi = sstParms.porrock; // porosity [%]
                double h = 500.0; // heat transfer coefficient [W/m^2 K]
                double shape = 0.2; // ratio of conduction path length
                double alpha = sstParms.krock / (sstParms.rhorock * sstParms.cprock);
                // storage ratio
                double gamma = (rhowater * cpwater * phi) / (sstParms.rhorock * sstParms.cprock * (1 - phi));
                // effective rock radius
                double vidar1 = 0.75 * (sstParms.fracsep * sstParms.fracheight * sstParms.fracwidth);
                double r_efr = 0.83 * Math.Pow((0.75 * (sstParms.fracsep * sstParms.fracheight * sstParms.fracwidth) / Math.PI), (1.0 / 3.0));
                // Biot number
                double Bi = h * r_efr / sstParms.krock;
                // effective rock time constant
                double tau_efr = Math.Pow(r_efr, 2.0) * (shape + 1.0 / Bi) / (3.0 * alpha);

                // reservoir dimensions and flow properties
                double hl = (sstParms.fracnumb - 1) * sstParms.fracsep;
                double wl = sstParms.fracwidth;
                double aave = hl * wl;
                double u0 = sstParms.nprod * sstParms.prodwellflowrate / (rhowater * aave);
                double tres = (sstParms.fracheight * phi) / u0;

                // number of heat transfer units
                double ntu = tres / tau_efr;

                // specify Laplace-space function
                LaplaceFunction fp = (s) => { return (1.0 / s) * (1 - Complex.Exp(-(1 + ntu / (gamma * (s + ntu))) * s)); };

                nptimevector = np.array(timeVector);
                var td = nptimevector * 365.0 * 24.0 * 3600 / tres;
                ILaplaceInversion pi = new LaplaceInversionTalbot();
                double[] temptd = new double[td.len - 1]; ;
                for (int i = 1; i < td.len; i++)
                {
                    temptd[i - 1] = (double)td[i];
                }
                double[] Twnd = pi.Calculate(fp, temptd);

                //calculate dimensional temperature, add error-handling for non-sensical temperatures
                var npTwnd = np.asarray(Twnd);
                var tempTresoutput = npTwnd * (Trock - sstParms.Tinj) + sstParms.Tinj;
                Tresoutput = new double[tempTresoutput.len + 1];
                Tresoutput[0] = Trock;
                for (int i = 0; i < tempTresoutput.len; i++)
                {
                    Tresoutput[i + 1] = (double)tempTresoutput[i];
                }
                for (int i = 0; i < Tresoutput.Length; i++)
                {
                    if (Tresoutput[i] > Trock) Tresoutput[i] = Trock;
                    if (Tresoutput[i] < sstParms.Tinj) Tresoutput[i] = Trock;
                    if (double.IsNaN(Tresoutput[i])) Tresoutput[i] = Trock;
                }
                //        Tresoutput = np.asarray([Trock if x > Trock or x<Tinj else x for x in Tresoutput])
            }
            else if (sstParms.resoption == 3)
            {
                nptimevector = np.array(timeVector);
                Tresoutput = new double[nptimevector.len];
                Tresoutput[0] = Trock;
                for (int i = 1; i < nptimevector.len; i++)
                {
                    double x = 1.0 / sstParms.drawdp / cpwater * Math.Sqrt(sstParms.krock * sstParms.rhorock * sstParms.cprock / (double)nptimevector[i] / (365.0 * 24.0 * 3600.0));
                    Tresoutput[i] = Utilities.Erf(x) * (Trock - sstParms.Tinj) + sstParms.Tinj;
                }
            }
            else if (sstParms.resoption == 4)
            {
                nptimevector = np.array(timeVector);
                var npTresoutput = (1 - sstParms.drawdp * nptimevector) * (Trock - sstParms.Tinj) + sstParms.Tinj; //this is no longer as in thesis (equation 4.16)
                Tresoutput = npTresoutput.GetData<double>();
            }
            else if (sstParms.resoption == 5)
            {

                Tresoutput[0] = Trock;
                string restempfname = sstParms.filenamereservoiroutput;
                string[] content = new string[0];
                if (File.Exists(restempfname))
                {
                    content = File.ReadAllLines(restempfname);
                }
                else
                {
                    Console.WriteLine($"Error: GEOPHIRES could not read reservoir output file ({restempfname}) and will abort simulation.");
                    Environment.Exit(1);
                }
                int numlines = 0;
                numlines = content.Length;
                if (numlines != finParms.plantlifetime * simulationParms.timestepsperyear + 1)
                {
                    Console.WriteLine($"Error: Reservoir output file ({restempfname}) does not have required {finParms.plantlifetime * simulationParms.timestepsperyear + 1} lines. GEOPHIRES will abort simulation.'");
                    Environment.Exit(1);
                }
                Tresoutput = new double[numlines];
                for (int i = 0; i < numlines; i++)
                {
                    string[] values = content[i].Split(',');
                    Tresoutput[i] = (double)values[1].GetDoubleFromString();
                }
            }
            else if (sstParms.resoption == 6)
            {
                // GEOPHIRES TOUGH2 executable not supported yet
            }
            //strTresoutput = strTresoutput.Trim();
            //strTresoutput = strTresoutput.TrimStart('[').TrimEnd(']');
            //string[] strTresoutputArray = strTresoutput.Split(' ')
            //    .Select(x => x.Trim())
            //    .Where(x => !string.IsNullOrWhiteSpace(x))
            //    .ToArray();
            //double[] Tresoutput = Array.ConvertAll(strTresoutputArray, Double.Parse);

            //Tresoutput = Utilities.ConvertStringToArray(strTresoutput);

            _logger.LogInformation("Completing CalculateReservoirTemperatureOutput");
        }

        private void CalculateWellboreTemperatureDrop()
        {
            if (sstParms.rameyoptionprod == 0)
            {
                ProdTempDrop[0] = sstParms.tempdropprod;
            }
            else if (sstParms.rameyoptionprod == 1)
            {
                nptimevector = np.array(timeVector);
                var alpharock = sstParms.krock / (sstParms.rhorock * sstParms.cprock);
                var temptimevector = nptimevector.GetData<double>();
                var npframey = np.zeros(temptimevector.Length);
                var framey = npframey.GetData<double>();
                var nptempframey = -np.log(1.1 * (sstParms.prodwelldiam / 2.0) / np.sqrt(4.0 * alpharock * np.array(temptimevector[1..]) * 365.0 * 24.0 * 3600.0 * stParms.utilfactor)) - 0.29;
                var tempframey = nptempframey.GetData<double>();
                framey = framey.ArrayCopy(1, tempframey);
                // Assume outside diameter of casing is 10% larger than inside diameter of production pipe (=prodwelldiam)
                var npFirstFramey = -np.log(1.1 * (sstParms.prodwelldiam / 2.0) / np.sqrt(4.0 * alpharock * np.array(temptimevector[1]) * 365.0 * 24.0 * 3600.0 * stParms.utilfactor)) - 0.29;
                var firstFramey = npFirstFramey.GetData<double>();
                framey[0] = firstFramey[0];
                //#assume borehole thermal resistance negligible to rock thermal resistance        
                var nprameyA = sstParms.prodwellflowrate * cpwater * np.array(framey) / 2 / Math.PI / sstParms.krock;
                // This code is only valid so far for 1 gradient and deviation = 0 !!!!!!!!   For multiple gradients, use Ramey's model for every layer
                var scalar1 = (sstParms.depth - nprameyA);
                //var scalar2 = averagegradient * nprameyA - Trock;
                var scalar3 = np.exp(-sstParms.depth / nprameyA);
                var npProdTempDrop = -((Trock - np.array(Tresoutput)) - averagegradient * scalar1 +
                    (np.array(Tresoutput) - averagegradient * nprameyA - Trock) * scalar3);
                ProdTempDrop = npProdTempDrop.GetData<double>();
            }
            var npProducedTemperature = np.array(Tresoutput) - np.array(ProdTempDrop);
            ProducedTemperature = npProducedTemperature.GetData<double>();
            calcResult.ProdTempDrop = ProdTempDrop;

            // Redrilling
            if (sstParms.resoption < 5) // only applies to the built-in analytical reservoir models
            {
                npProducedTemperature = np.array(ProducedTemperature);
                var npindexfirstmaxdrawdown = (int)np.argmax(npProducedTemperature < (1 - sstParms.maxdrawdown) * ProducedTemperature[0]);
                if (npindexfirstmaxdrawdown > 0)
                {
                    redrill = (int)(np.floor((NDarray)(ProducedTemperature.Length / npindexfirstmaxdrawdown)));
                    var npProducedTemperatureRepeatead = np.tile((NDarray)ProducedTemperature[0..npindexfirstmaxdrawdown], (NDarray)(redrill + 1));
                    var ProducedTemperatureRepeatead = npProducedTemperatureRepeatead.GetData<double>();
                    ProducedTemperature = ProducedTemperatureRepeatead[0..ProducedTemperature.Length];
                }
            }
            calcResult.redrill = redrill;
            calcResult.ProducedTemperature = ProducedTemperature;
        }

        private void CalculatePressureDropsAndPumpingPower()
        {
            //------------------------------------------
            // calculate pressure drops and pumping power
            //------------------------------------------
            // production wellbore fluid conditions [kPa]
            double[] f3 = { 0.0 };
            double[] f1 = { 0.0 };
            double[] Tprodaverage = { 0.0 };
            NDarray npf1 = np.zeros(1);
            NDarray npf3 = np.zeros(1);
            var npTprodaverage = np.array(Tresoutput) - np.array(ProdTempDrop) / 4.0; //most of temperature drop happens in upper section (because surrounding rock temperature is lowest in upper section)
            Tprodaverage = npTprodaverage.GetData<double>();
            var rhowaterprod = (NDarray)Utilities.npdensitywater(npTprodaverage);  //replace with correlation based on Tprodaverage
            var muwaterprod = Utilities.npviscositywater(npTprodaverage); //replace with correlation based on Tprodaverage
            var npvprod = sstParms.prodwellflowrate / rhowaterprod / (Math.PI / 4.0 * Math.Pow(sstParms.prodwelldiam, 2));
            double[] vprod = npvprod.GetData<double>();
            var Rewaterprod = 4.0 * sstParms.prodwellflowrate / (muwaterprod * Math.PI * sstParms.prodwelldiam); //laminar or turbulent flow?
            var Rewaterprodaverage = np.average(Rewaterprod);
            if (Rewaterprodaverage < 2300.0)
            {
                npf3 = 64.0 / Rewaterprod;
            }
            else
            {
                var relroughness = 1E-4 / sstParms.prodwelldiam;
                npf3 = 1.0 / np.power(-2 * np.log10(relroughness / 3.7 + 5.74 / np.power(Rewaterprod, (NDarray)0.9)), (NDarray)2.0);
                npf3 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterprod / np.sqrt(npf3))), (NDarray)2.0);
                npf3 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterprod / np.sqrt(npf3))), (NDarray)2.0);
                npf3 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterprod / np.sqrt(npf3))), (NDarray)2.0);
                npf3 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterprod / np.sqrt(npf3))), (NDarray)2.0);
                // 6 iterations to converge
                npf3 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterprod / np.sqrt(npf3))), (NDarray)2.0);
            }
            f3 = npf3.GetData<double>();
            //injection well conditions
            double Tinjaverage = sstParms.Tinj;
            var rhowaterinj = Utilities.densitywater(Tinjaverage) * np.linspace(1, 1, ProducedTemperature.Length);
            var muwaterinj = Utilities.npviscositywater(np.array(Tinjaverage)) * np.linspace(1, 1, ProducedTemperature.Length);  //replace with correlation based on Tinjaverage
            var vinj = ((double)sstParms.nprod / sstParms.ninj) * sstParms.prodwellflowrate * (1.0 + sstParms.waterloss) / rhowaterinj / (Math.PI / 4.0 * Math.Pow(sstParms.injwelldiam, 2));

            // injection well conditions
            var Rewaterinj = 4.0 * sstParms.nprod / sstParms.ninj * sstParms.prodwellflowrate * (1.0 + sstParms.waterloss) / (muwaterinj * Math.PI * sstParms.injwelldiam);
            var Rewaterinjaverage = np.average(Rewaterinj);
            if (Rewaterinjaverage < 2300.0)
            {
                npf1 = 64.0 / Rewaterinj;
            }
            // Turbulent flow
            else
            {
                var relroughness = 1E-4 / sstParms.injwelldiam;
                npf1 = 1.0 / np.power(-2 * np.log10(relroughness / 3.7 + 5.74 / np.power(Rewaterinj, (NDarray)0.9)), (NDarray)2.0);
                npf1 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterinj / np.sqrt(npf1))), (NDarray)2.0);
                npf1 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterinj / np.sqrt(npf1))), (NDarray)2.0);
                npf1 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterinj / np.sqrt(npf1))), (NDarray)2.0);
                npf1 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterinj / np.sqrt(npf1))), (NDarray)2.0);
                npf1 = 1.0 / np.power((-2 * np.log10(relroughness / 3.7 + 2.51 / Rewaterinj / np.sqrt(npf1))), (NDarray)2.0);
            }
            f1 = npf1.GetData<double>();

            string strpumpdepth = "";
            double[] DP = { 0.0 };
            double[] DP1 = { 0.0 };
            double[] DP2 = { 0.0 };
            double[] DP3 = { 0.0 };
            double[] DP4 = { 0.0 };
            string strPumpingPowerInj = "";
            double Pprodwellhead = 0.0;
            if (sstParms.impedancemodelused == 1)
            {
                // injecion well pressure drop [kPa]
                var powervinj = np.array(vinj.GetData<double>().MathPow(2));
                var npDP1 = npf1 * (rhowaterinj * powervinj / 2) * (sstParms.depth / sstParms.injwelldiam) / 1E3;   //1E3 to convert from Pa to kPa
                DP1 = npDP1.GetData<double>();

                //reservoir pressure drop [kPa]
                var rhowaterreservoir = Utilities.npdensitywater(0.1 * sstParms.Tinj + 0.9 * np.array(Tresoutput)); //based on TARB in Geophires v1.2
                var npDP2 = sstParms.impedance * sstParms.nprod * sstParms.prodwellflowrate * 1000.0 / rhowaterreservoir;
                DP2 = npDP2.GetData<double>();

                //production well pressure drop [kPa]
                var npDP3 = f3 * (rhowaterprod * vprod.MathPow(2) / 2.0) * (sstParms.depth / sstParms.prodwelldiam) / 1E3;  //1E3 to convert from Pa to kPa
                DP3 = npDP3.GetData<double>();

                //buoyancy pressure drop [kPa]
                var npDP4 = (rhowaterprod - rhowaterinj) * sstParms.depth * 9.81 / 1E3; // 1E3 to convert from Pa to kPa
                DP4 = npDP4.GetData<double>();

                //overall pressure drop
                var npDP = npDP1 + npDP2 + npDP3 + npDP4;
                DP = npDP.GetData<double>();

                //calculate pumping power [MWe] (approximate)
                var npPumpingPower = npDP * sstParms.nprod * sstParms.prodwellflowrate * (1 + sstParms.waterloss) / rhowaterinj / stParms.pumpeff / 1E3;
                PumpingPower = npPumpingPower.GetData<double>();

                //in GEOPHIRES v1.2, negative pumping power values become zero (b/c we are not generating electricity)
                //    PumpingPower = [0. if x < 0. else x for x in PumpingPower]
                PumpingPower = PumpingPower.NegativeToZero();
            }
            else
            {
                //reservoir hydrostatic pressure[kPa]
                if (sstParms.usebuiltinhydrostaticpressurecorrelation == 1)
                {
                    double CP = 4.64E-7;
                    double CT = 9E-4 / (30.796 * Math.Pow(Trock, -0.552));
                    //double denseWater = Math.Exp(Utilities.densitywater(Tsurf));
                    double denseWater = Math.Exp(Utilities.densitywater(stParms.Tsurf) * 9.81 * CP / 1000 * (sstParms.depth - CT / 2 * averagegradient * Math.Pow(sstParms.depth, 2))) - 1;
                    sstParms.Phydrostatic = 0 + 1.0 / CP * denseWater;
                }

                if (sstParms.productionwellpumping == 1)
                {
                    //[kPa] = 50 psi. Excess pressure covers non-condensable gas pressure and net positive suction head for the pump
                    double Pexcess = 344.7;
                    //[kPa] is minimum production pump inlet pressure and minimum wellhead pressure;
                    double Pminimum = Utilities.VaporPressureWater(Trock) + Pexcess;
                    if (sstParms.usebuiltinppwellheadcorrelation == 1)
                    {
                        // production wellhead pressure [kPa]
                        Pprodwellhead = Pminimum;
                    }
                    else
                    {
                        Pprodwellhead = sstParms.ppwellhead;
                        if (Pprodwellhead < Pminimum)
                        {
                            Pprodwellhead = Pminimum;
                            Console.WriteLine("Warning: provided production wellhead pressure under minimum pressure. GEOPHIRES will assume minimum wellhead pressure");
                        }
                    }
                    calcResult.Pprodwellhead = Pprodwellhead;
                    double PIkPa = sstParms.PI / 100; //onvert PI from kg/s/bar to kg/s/kPa
                                                      // calculate pumping depth
                    var nppumpdepth = sstParms.depth + (Pminimum - sstParms.Phydrostatic + sstParms.prodwellflowrate / PIkPa) / (f3 * (rhowaterprod * vprod.MathPow(2) / 2.0) * (1 / sstParms.prodwelldiam) / 1E3 + rhowaterprod * 9.81 / 1E3);
                    pumpdepth = nppumpdepth.GetData<double>();
                    var vidar = np.max(pumpdepth);
                    double pumpdepthfinal = pumpdepth.Max();
                    if (pumpdepthfinal < 0)
                    {
                        pumpdepthfinal = 0;
                        Console.WriteLine("Warning: GEOPHIRES calculates negative production well pumping depth. " +
                            "No production well pumps will be assumed");
                    }
                    else if (pumpdepthfinal > 600)
                    {
                        Console.WriteLine("Warning: GEOPHIRES calculates pump depth to be deeper than 600 m. " +
                            "Verify reservoir pressure, production well flow rate and production well dimensions");
                    }
                    // calculate production well pumping pressure [kPa]
                    var npDP3 = Pprodwellhead - (sstParms.Phydrostatic - sstParms.prodwellflowrate / PIkPa - rhowaterprod * 9.81 * sstParms.depth / 1E3 - f3 * (rhowaterprod * vprod.MathPow(2) / 2.0) * (sstParms.depth / sstParms.prodwelldiam) / 1E3);
                    DP3 = npDP3.GetData<double>();
                    calcResult.DP3 = DP3;
                    //#DP3 = [0 if x<0 else x for x in DP3] #set negative values to 0
                    //[MWe] total pumping power for production wells
                    var npPumpingPowerProd = npDP3 * sstParms.nprod * sstParms.prodwellflowrate / rhowaterprod / stParms.pumpeff / 1E3;
                    PumpingPowerProd = npPumpingPowerProd.GetData<double>();
                    PumpingPowerProd = PumpingPowerProd.NegativeToZero();
                }

                double IIkPa = sstParms.II / 100; //convert II from kg/s/bar to kg/s/kPa    

                //necessary injection wellhead pressure [kPa]
                var powervinj = np.array(vinj.GetData<double>().MathPow(2));
                var Pinjwellhead = sstParms.Phydrostatic +
                sstParms.prodwellflowrate * (1 + sstParms.waterloss) * sstParms.nprod / sstParms.ninj / IIkPa -
                rhowaterinj * 9.81 * sstParms.depth / 1E3 + f1 * (rhowaterinj * powervinj / 2) * (sstParms.depth / sstParms.injwelldiam) / 1E3;
                // plant outlet pressure [kPa]
                if (sstParms.usebuiltinoutletplantcorrelation == 1)
                {
                    double DPSurfaceplant = 68.95; //[kPa] assumes 10 psi pressure drop in surface equipment
                    sstParms.Pplantoutlet = Pprodwellhead - DPSurfaceplant;
                }

                //injection pump pressure [kPa]
                var npDP1 = Pinjwellhead - sstParms.Pplantoutlet;
                DP1 = npDP1.GetData<double>();
                //#DP1 = [0 if x<0 else x for x in DP1] #set negative values to 0
                var npPumpingPowerInj = npDP1 * sstParms.nprod * sstParms.prodwellflowrate * (1 + sstParms.waterloss) / rhowaterinj / stParms.pumpeff / 1E3; //[MWe] total pumping power for injection wells
                PumpingPowerInj = npPumpingPowerInj.GetData<double>();
                PumpingPowerInj = PumpingPowerInj.NegativeToZero();

                // total pumping power
                if (sstParms.productionwellpumping == 1)
                {
                    var total = np.array(PumpingPowerInj) + np.array(PumpingPowerProd);
                    PumpingPower = total.GetData<double>();
                }
                else
                {
                    var total = np.array(PumpingPowerInj);
                    PumpingPower = total.GetData<double>();
                }
                PumpingPower = PumpingPower.NegativeToZero();

                if (sstParms.usebuiltinppwellheadcorrelation == 1)
                {
                    var CP = 4.64E-7;
                    var CT = 9E-4 / (30.796 * Math.Pow(Trock, -0.552));
                    var A = Utilities.densitywater(stParms.Tsurf);
                    sstParms.Phydrostatic = 0 + 1.0 / CP * (Math.Exp(A * 9.81 * CP / 1000 * (sstParms.depth - CT / 2 * averagegradient * Math.Pow(sstParms.depth, 2))) - 1);
                }
            }
            calcResult.PumpingPower = PumpingPower;
            calcResult.DP = DP;
            calcResult.DP1 = DP1;
            calcResult.DP2 = DP2;
            calcResult.DP3 = DP3;
            calcResult.DP4 = DP4;
        }

        private void CalculateEnergyExtractedAndProduced()
        {
            string strPumpingPower = "";
            double[] ReinjTemp = new double[] { 0 };
            double[] etau = new double[] { 0 };
            double[] HeatExtractedTowardsElectricity = new double[] { 0 };
            double[] Availability = new double[] { 0 };
            double minReinjTemp;
            NDarray FirstLawEfficiency = np.zeros(1);
            NDarray npTenteringPP = np.zeros(1);
            NDarray npAvailability = np.zeros(1);
            NDarray npHeatExtracted = np.zeros(1);
            NDarray npHeatProduced = np.zeros(1);
            //direct - use
            if (simulationParms.enduseoption == 2)
            {
                //heat extracted from geofluid [MWth]
                var npProducedTemperature = np.array(ProducedTemperature);
                npHeatExtracted = sstParms.nprod * sstParms.prodwellflowrate * cpwater * (npProducedTemperature - sstParms.Tinj) / 1E6;
                HeatExtracted = npHeatExtracted.GetData<double>();
                //useful direct-use heat provided to application [MWth]
                npHeatProduced = npHeatExtracted * stParms.enduseefficiencyfactor;
                HeatProduced = npHeatProduced.GetData<double>();
            }
            else
            {
                if ((int)Math.Floor((double)(simulationParms.enduseoption / 10)) == 4)
                {
                    TenteringPP[0] = stParms.Tchpbottom;
                }
                else
                {
                    var npProducedTemperature = np.array(ProducedTemperature);
                    npTenteringPP = npProducedTemperature;
                    TenteringPP = npTenteringPP.GetData<double>();
                }

                npTenteringPP = np.array(TenteringPP);
                var A = 4.041650;
                var B = -1.204E-2;
                var C = 1.60500E-5;
                var T0 = stParms.Tenv + 273.15;
                var T1 = npTenteringPP + 273.15;
                var T12 = np.power(T1, (NDarray)2);
                var T13 = np.power(T1, (NDarray)3);
                var T2 = stParms.Tenv + 273.15;
                var T22 = Math.Pow(T2, 2);
                var T23 = Math.Pow(T2, 3);
                npAvailability = ((A - B * T0) * (T1 - T2) + (B - C * T0) / 2.0 * (T12 - T22) + C / 3.0 * (T13 - T23) - A * T0 * np.log(T1 / T2)) * 2.2046 / 947.83;
                Availability = npAvailability.GetData<double>();
                calcResult.Availability = Availability;

                // Subcritical ORC
                double C0 = 0;
                double C1 = 0;
                double C2 = 0;
                double D0 = 0;
                double D1 = 0;
                double D2 = 0;
                double Tfraction = 0;
                if (simulationParms.pptype == 1)
                {
                    if (stParms.Tenv < 15.0)
                    {
                        C1 = 2.746E-3;
                        C0 = -8.3806E-2;
                        D1 = 2.713E-3;
                        D0 = -9.1841E-2;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C1 = 2.713E-3;
                        C0 = -9.1841E-2;
                        D1 = 2.676E-3;
                        D0 = -1.012E-1;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    npTenteringPP = np.array(TenteringPP);
                    var etaull = C1 * npTenteringPP + C0;
                    var etauul = D1 * npTenteringPP + D0;
                    var npetau = (1 - Tfraction) * etaull + Tfraction * etauul;
                    etau = npetau.GetData<double>();
                    if (stParms.Tenv < 15.0)
                    {
                        C1 = 0.0894;
                        C0 = 55.6;
                        D1 = 0.0894;
                        D0 = 62.6;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C1 = 0.0894;
                        C0 = 62.6;
                        D1 = 0.0894;
                        D0 = 69.6;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    var npreinjtll = C1 * npTenteringPP + C0;
                    var npreinjtul = D1 * npTenteringPP + D0;
                    var npReinjTemp = (1.0 - Tfraction) * npreinjtll + Tfraction * npreinjtul;
                    ReinjTemp = npReinjTemp.GetData<double>();
                }
                // Supercritical ORC
                else if (simulationParms.pptype == 2)
                {
                    if (stParms.Tenv < 15.0)
                    {
                        C2 = -1.55E-5;
                        C1 = 7.604E-3;
                        C0 = -3.78E-1;
                        D2 = -1.499E-5;
                        D1 = 7.4268E-3;
                        D0 = -3.7915E-1;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C2 = -1.499E-5;
                        C1 = 7.4268E-3;
                        C0 = -3.7915E-1;
                        D2 = -1.55E-5;
                        D1 = 7.55136E-3;
                        D0 = -4.041E-1;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    var etaull = C2 * np.array(TenteringPP.MathPow(2)) + C1 * np.array(TenteringPP) + C0;
                    var etauul = D2 * np.array(TenteringPP.MathPow(2)) + D1 * np.array(TenteringPP) + D0;
                    var npetau = (1 - Tfraction) * etaull + Tfraction * etauul;
                    etau = npetau.GetData<double>();

                    if (stParms.Tenv < 15.0)
                    {
                        C1 = 0.02;
                        C0 = 49.26;
                        D1 = 0.02;
                        D0 = 56.26;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C1 = 0.02;
                        C0 = 56.26;
                        D1 = 0.02;
                        D0 = 63.26;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    npTenteringPP = np.array(TenteringPP);
                    var reinjtll = C1 * npTenteringPP + C0;
                    var reinjtul = D1 * npTenteringPP + D0;
                    var npReinjTemp = (1.0 - Tfraction) * reinjtll + Tfraction * reinjtul;
                    ReinjTemp = npReinjTemp.GetData<double>();
                }
                // single-flash
                else if (simulationParms.pptype == 3)
                {
                    //if (Tenv < 15.):
                    //    C2 = -4.27318E-7
                    //    C1 = 8.65629E-4
                    //    C0 = 1.78931E-1
                    //    D2 = -5.85412E-7
                    //    D1 = 9.68352E-4
                    //    D0 = 1.58056E-1
                    //    Tfraction = (Tenv - 5.) / 10.
                    //else:
                    //    C2 = -5.85412E-7
                    //    C1 = 9.68352E-4
                    //    C0 = 1.58056E-1
                    //    D2 = -7.78996E-7
                    //    D1 = 1.09230E-3
                    //    D0 = 1.33708E-1
                    //    Tfraction = (Tenv - 15.) / 10.
                    //etaull = C2 * TenteringPP * *2 + C1 * TenteringPP + C0
                    //etauul = D2 * TenteringPP * *2 + D1 * TenteringPP + D0
                    //etau = (1.- Tfraction) * etaull + Tfraction * etauul
                    //if (Tenv < 15.):
                    //    C2 = -1.11519E-3
                    //    C1 = 7.79126E-1
                    //    C0 = -10.2242
                    //    D2 = -1.10232E-3
                    //    D1 = 7.83893E-1
                    //    D0 = -5.17039
                    //    Tfraction = (Tenv - 5.) / 10.
                    //else:
                    //    C2 = -1.10232E-3
                    //    C1 = 7.83893E-1
                    //    C0 = -5.17039
                    //    D2 = -1.08914E-3
                    //    D1 = 7.88562E-1
                    //    D0 = -1.89707E-1
                    //    Tfraction = (Tenv - 15.) / 10.
                    //reinjtll = C2 * TenteringPP * *2 + C1 * TenteringPP + C0
                    //reinjtul = D2 * TenteringPP * *2 + D1 * TenteringPP + D0
                    //ReinjTemp = (1.- Tfraction) * reinjtll + Tfraction * reinjtul
                }
                // double-flash
                else if (simulationParms.pptype == 4)
                {
                    if (stParms.Tenv < 15.0)
                    {
                        C2 = -1.200E-6;
                        C1 = 1.22731E-3;
                        C0 = 2.26956E-1;
                        D2 = -1.42165E-6;
                        D1 = 1.37050E-3;
                        D0 = 1.99847E-1;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C2 = -1.42165E-6;
                        C1 = 1.37050E-3;
                        C0 = 1.99847E-1;
                        D2 = -1.66771E-6;
                        D1 = 1.53079E-3;
                        D0 = 1.69439E-1;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    var etaull = C2 * np.array(TenteringPP.MathPow(2)) + C1 * np.array(TenteringPP) + C0;
                    var etauul = D2 * np.array(TenteringPP.MathPow(2)) + D1 * np.array(TenteringPP) + D0;
                    var npetau = (1 - Tfraction) * etaull + Tfraction * etauul;
                    etau = npetau.GetData<double>();
                    if (stParms.Tenv < 15.0)
                    {
                        C2 = -7.70928E-4;
                        C1 = 5.02466E-1;
                        C0 = 5.22091;
                        D2 = -7.69455E-4;
                        D1 = 5.09406E-1;
                        D0 = 11.6859;
                        Tfraction = (stParms.Tenv - 5.0) / 10.0;
                    }
                    else
                    {
                        C2 = -7.69455E-4;
                        C1 = 5.09406E-1;
                        C0 = 11.6859;
                        D2 = -7.67751E-4;
                        D1 = 5.16356E-1;
                        D0 = 18.0798;
                        Tfraction = (stParms.Tenv - 15.0) / 10.0;
                    }
                    var reinjtll = C2 * np.array(TenteringPP.MathPow(2)) + C1 * np.array(TenteringPP) + C0;
                    var reinjtul = D2 * np.array(TenteringPP.MathPow(2)) + D1 * np.array(TenteringPP) + D0;
                    var npReinjTemp = (1.0 - Tfraction) * reinjtll + Tfraction * reinjtul;
                    ReinjTemp = npReinjTemp.GetData<double>();
                }

                minReinjTemp = MathNet.Numerics.Statistics.Statistics.Minimum(ReinjTemp);
                double tmpenduseoption = simulationParms.enduseoption / 10;
                // check if reinjectemp (model calculated) >= Tinj (user provided)
                // pure electricity
                if (simulationParms.enduseoption == 1)
                {
                    if (minReinjTemp < sstParms.Tinj)
                    {
                        sstParms.Tinj = minReinjTemp;
                        Console.WriteLine("Warning: injection temperature lowered");
                    }
                }
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 3)
                {
                    if (minReinjTemp < sstParms.Tinj)
                    {
                        sstParms.Tinj = minReinjTemp;
                        Console.WriteLine("Warning: injection temperature lowered");
                    }
                }
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 4)
                {
                    if (minReinjTemp < sstParms.Tinj)
                    {
                        sstParms.Tinj = minReinjTemp;
                        Console.WriteLine("Warning: injection temperature lowered");
                    }
                }
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 4)
                {
                    if (minReinjTemp < sstParms.Tinj)
                    {
                        Console.WriteLine("Warning: injection temperature incorrect but cannot be lowered");
                    }
                }

                // calculate electricity/heat
                if (simulationParms.enduseoption == 1) //pure electricity
                {
                    npAvailability = np.array(Availability);
                    var npetau = np.array(etau);
                    var npElectricityProduced = npAvailability * npetau * sstParms.nprod * sstParms.prodwellflowrate;
                    ElectricityProduced = npElectricityProduced.GetData<double>();

                    var npProducedTemperature = np.array(ProducedTemperature);
                    // Heat extracted from geofluid [MWth]\n";
                    npHeatExtracted = sstParms.nprod * sstParms.prodwellflowrate * cpwater * (npProducedTemperature - sstParms.Tinj) / 1E6;
                    var npHeatExtractedTowardsElectricity = npHeatExtracted;
                    HeatExtractedTowardsElectricity = npHeatExtractedTowardsElectricity.GetData<double>();
                    HeatExtracted = npHeatExtracted.GetData<double>();
                }
                // enduseoption = 3: cogen topping cycle
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 3)
                {
                    npAvailability = np.array(Availability);
                    var npElectricityProduced = npAvailability * etau * sstParms.nprod * sstParms.prodwellflowrate;
                    ElectricityProduced = npElectricityProduced.GetData<double>();
                    //Heat extracted from geofluid [MWth]
                    var npProducedTemperature = np.array(ProducedTemperature);
                    npHeatExtracted = sstParms.nprod * sstParms.prodwellflowrate * cpwater * (npProducedTemperature - sstParms.Tinj) / 1E6;
                    HeatExtracted = npHeatExtracted.GetData<double>();
                    //Useful heat for direct-use application [MWth] 
                    var npReinjTemp = np.array(ReinjTemp);
                    npHeatProduced = stParms.enduseefficiencyfactor * sstParms.nprod * sstParms.prodwellflowrate * cpwater * (npReinjTemp - sstParms.Tinj) / 1E6;
                    HeatProduced = npHeatProduced.GetData<double>();
                    var npHeatExtractedTowardsElectricity = sstParms.nprod * sstParms.prodwellflowrate * cpwater * (npProducedTemperature - npReinjTemp) / 1E6;
                    HeatExtractedTowardsElectricity = npHeatExtractedTowardsElectricity.GetData<double>();
                }
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 4) //enduseoption = 4: cogen bottoming cycle
                {
                    //    ElectricityProduced = Availability * etau * nprod * prodwellflowrate
                    //    HeatExtracted = nprod * prodwellflowrate * cpwater * (ProducedTemperature - Tinj) / 1E6 #Heat extracted from geofluid [MWth]
                    //    HeatProduced = enduseefficiencyfactor * nprod * prodwellflowrate * cpwater * (ProducedTemperature - Tchpbottom) / 1E6 #Useful heat for direct-use application [MWth]
                    //    HeatExtractedTowardsElectricity = nprod * prodwellflowrate * cpwater * (Tchpbottom - Tinj) / 1E6
                }
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 5) //enduseoption = 5: cogen split of mass flow rate
                {
                    //    ElectricityProduced = Availability * etau * nprod * prodwellflowrate * (1.- chpfraction) #electricity part [MWe]
                    //    HeatExtracted = nprod * prodwellflowrate * cpwater * (ProducedTemperature - Tinj) / 1E6 #Total amount of heat extracted from geofluid [MWth]
                    //    HeatProduced = enduseefficiencyfactor * chpfraction * nprod * prodwellflowrate * cpwater * (ProducedTemperature - Tinj) / 1E6 #useful heat part for direct-use application [MWth]
                    //    HeatExtractedTowardsElectricity = (1.- chpfraction) * nprod * prodwellflowrate * cpwater * (ProducedTemperature - Tinj) / 1E6
                }

                // subtract pumping power for net electricity and  calculate first law efficiency
                if (simulationParms.enduseoption == 1 || simulationParms.enduseoption > 2)
                {
                    var npElectricityProduced = np.array(ElectricityProduced);
                    var npPumpingPower = np.array(PumpingPower);
                    var npHeatExtractedTowardsElectricity = np.array(HeatExtractedTowardsElectricity);
                    var npNetElectricityProduced = npElectricityProduced - npPumpingPower;
                    FirstLawEfficiency = npNetElectricityProduced / npHeatExtractedTowardsElectricity;
                    NetElectricityProduced = npNetElectricityProduced.GetData<double>();
                }

            }
            calcResult.NetElectricityProduced = NetElectricityProduced;
            calcResult.FirstLawEfficiency = FirstLawEfficiency;
            calcResult.HeatProduced = HeatProduced;
        }

        private void CapitalCosts()
        {
            //-------------
            //capital costs
            //------------ -
            //well costs(using GeoVision drilling correlations). These are calculated whether or not totalcapcostvalid = 1  
            double C1well = 0;
            if (ccParms.ccwellfixedvalid == 1)
            {
                C1well = ccParms.ccwellfixed;
                Cwell = C1well * (sstParms.nprod + sstParms.ninj);
            }
            else
            {
                if (ccParms.wellcorrelation == 1) //vertical open-hole, small diameter
                {
                    C1well = (0.3021 * Math.Pow(sstParms.depth, 2) + 584.9112 * sstParms.depth + 751368.0) * 1E-6; //well drilling and completion cost in M$/well
                }
                else if (ccParms.wellcorrelation == 2) //deviated liner, small diameter
                {
                    C1well = (0.2898 * Math.Pow(sstParms.depth, 2) + 822.1507 * sstParms.depth + 680563.0) * 1E-6;
                }
                else if (ccParms.wellcorrelation == 3) //vertical open-hole, large diameter
                {
                    C1well = (0.2818 * Math.Pow(sstParms.depth, 2) + 1275.5213 * sstParms.depth + 632315.0) * 1E-6;
                }
                else if (ccParms.wellcorrelation == 4) //deviated liner, large diameter
                {
                    C1well = (0.2553 * Math.Pow(sstParms.depth, 2) + 1716.7157 * sstParms.depth + 500867.0) * 1E-6;
                }
                if (sstParms.depth < 500) Console.WriteLine("Warning: drilling cost correlation extrapolated for drilling depth < 500 m");
                if (sstParms.depth > 7000) Console.WriteLine("Warning: drilling cost correlation extrapolated for drilling depth > 7000 m");
                C1well = ccParms.ccwelladjfactor * C1well;
                Cwell = 1.05 * C1well * (sstParms.nprod + sstParms.ninj); //1.05 for 5% indirect costs
            }
            calcResult.Cwell = Cwell;

            //reservoir stimulation costs (M$/injection well). These are calculated whether or not totalcapcostvalid = 1 
            if (ccParms.ccstimfixedvalid == 1)
            {
                Cstim = ccParms.ccstimfixed;
            }
            else
            {
                Cstim = 1.05 * 1.15 * ccParms.ccstimadjfactor * sstParms.ninj * 1.25; //1.15 for 15% contingency and 1.05 for 5% indirect costs
            }
            calcResult.Cstim = Cstim;

            // Field gathering system costs (M$)
            if (ccParms.ccgathfixedvalid == 1)
            {
                Cgath = ccParms.ccgathfixed;
            }
            else
            {
                double Cpumpsinj = 0;
                double Cpumpsprod = 0;
                double Cpumps = 0;
                if (sstParms.impedancemodelused == 1)
                {
                    var pumphp = np.max(PumpingPower) * 1341;
                    var numberofpumps = (double)np.ceil(pumphp / 2000);
                    if (numberofpumps == 0)
                    {
                        Cpumps = 0;
                    }
                    else
                    {
                        var pumphpcorrected = (double)pumphp / numberofpumps;
                        Cpumps = numberofpumps * 1.5 * ((1750 * Math.Pow(pumphpcorrected, 0.7)) * 3 * Math.Pow(pumphpcorrected, -0.11));
                    }
                }
                else
                {
                    if (sstParms.productionwellpumping == 1)
                    {
                        var prodpumphp = (double)np.max(PumpingPowerProd) / sstParms.nprod * 1341;
                        Cpumpsprod = sstParms.nprod * 1.5 * (1750 * Math.Pow(prodpumphp, 0.7) + 5750 * Math.Pow(prodpumphp, 0.2) + 10000 + (double)np.max(pumpdepth) * 50 * 3.281);
                    }
                    else
                    {
                        Cpumpsprod = 0;
                    }
                    var injpumphp = np.max(PumpingPowerInj) * 1341;
                    var numberofinjpumps = (double)np.ceil(injpumphp / 2000); //pump can be maximum 2,000 hp\n";
                    if (numberofinjpumps == 0)
                    {
                        Cpumpsinj = 0;
                    }
                    else
                    {
                        var injpumphpcorrected = (double)injpumphp / numberofinjpumps;
                        Cpumpsinj = numberofinjpumps * 1.5 * (1750 * Math.Pow(injpumphpcorrected, 0.7)) * 3 * Math.Pow(injpumphpcorrected, -0.11);
                    }
                    Cpumps = Cpumpsinj + Cpumpsprod;
                }

                // Based on GETEM 2016 #1.15 for 15% contingency and 1.12 for 12% indirect costs
                Cgath = 1.15 * ccParms.ccgathadjfactor * 1.12 * ((sstParms.nprod + sstParms.ninj) * 750 * 500.0 + Cpumps) / 1E6;
            }
            calcResult.Cgath = Cgath;

            // plant costs
            double Cplantcorrelation = 0;
            var npTenteringPP = np.array(TenteringPP);
            if (simulationParms.enduseoption == 2) //direct-use
            {
                if (ccParms.ccplantfixedvalid == 1)
                {
                    Cplant = ccParms.ccplantfixed;
                }
                else
                {
                    //1.15 for 15% contingency and 1.12 for 12% indirect costs
                    var npCplant = 1.12 * 1.15 * ccParms.ccplantadjfactor * 250E-6 * np.max(HeatExtracted) * 1000.0;
                    Cplant = (double)npCplant;
                }
            }
            else //all other options have power plant
            {
                if (simulationParms.pptype == 1) //sub-critical ORC
                {
                    var MaxProducedTemperature = (double)Numpy.np.max(TenteringPP);
                    double CCAPP1;
                    if (MaxProducedTemperature < 150.0)
                    {
                        var C3 = -1.458333E-3;
                        var C2 = 7.6875E-1;
                        var C1 = -1.347917E2;
                        var C0 = 1.0075E4;
                        CCAPP1 = C3 * Math.Pow(MaxProducedTemperature, 3) + C2 * Math.Pow(MaxProducedTemperature, 2) + C1 * MaxProducedTemperature + C0;
                    }
                    else
                    {
                        CCAPP1 = 2231 - 2 * (MaxProducedTemperature - 150.0);
                    }
                    var npElectricityProduced = np.array(ElectricityProduced);
                    var maxElectricityProduced = (double)Numpy.np.max(ElectricityProduced);
                    Cplantcorrelation = CCAPP1 * Math.Pow((maxElectricityProduced / 15.0), -0.06) * maxElectricityProduced * 1000.0 / 1E6;
                }
                else if (simulationParms.pptype == 2) //supercritical ORC
                {
                    var MaxProducedTemperature = (double)Numpy.np.max(TenteringPP);
                    double CCAPP1;
                    if (MaxProducedTemperature < 150.0)
                    {
                        var C3 = -1.458333E-3;
                        var C2 = 7.6875E-1;
                        var C1 = -1.347917E2;
                        var C0 = 1.0075E4;
                        CCAPP1 = C3 * Math.Pow(MaxProducedTemperature, 3) + C2 * Math.Pow(MaxProducedTemperature, 2) + C1 * MaxProducedTemperature + C0;
                    }
                    else
                    {
                        CCAPP1 = 2231 - 2 * (MaxProducedTemperature - 150.0);
                    }
                    var npElectricityProduced = np.array(ElectricityProduced);
                    var maxElectricityProduced = (double)Numpy.np.max(ElectricityProduced);
                    //factor 1.1 to make supercritical 10% more expansive than subcritical
                    Cplantcorrelation = 1.1 * CCAPP1 * Math.Pow((maxElectricityProduced / 15.0), -0.06) * maxElectricityProduced * 1000.0 / 1E6;
                }
                else if (simulationParms.pptype == 3) //single-flash
                {
                    //if (np.max(ElectricityProduced) < 10.):
                    //    C2 = 4.8472E-2
                    //    C1 = -35.2186
                    //    C0 = 8.4474E3
                    //    D2 = 4.0604E-2
                    //    D1 = -29.3817
                    //    D0 = 6.9911E3
                    //    PLL = 5.
                    //    PRL = 10.
                    //elif(np.max(ElectricityProduced) < 25.):
                    //    C2 = 4.0604E-2
                    //    C1 = -29.3817
                    //    C0 = 6.9911E3
                    //    D2 = 3.2773E-2
                    //    D1 = -23.5519
                    //    D0 = 5.5263E3
                    //    PLL = 10.
                    //    PRL = 25.
                    //elif(np.max(ElectricityProduced) < 50.):
                    //    C2 = 3.2773E-2
                    //    C1 = -23.5519
                    //    C0 = 5.5263E3
                    //    D2 = 3.4716E-2
                    //    D1 = -23.8139
                    //    D0 = 5.1787E3
                    //    PLL = 25.
                    //    PRL = 50.
                    //elif(np.max(ElectricityProduced) < 75.):
                    //    C2 = 3.4716E-2
                    //    C1 = -23.8139
                    //    C0 = 5.1787E3
                    //    D2 = 3.5271E-2
                    //    D1 = -24.3962
                    //    D0 = 5.1972E3
                    //    PLL = 50.
                    //    PRL = 75.
                    //else:
                    //    C2 = 3.5271E-2
                    //    C1 = -24.3962
                    //    C0 = 5.1972E3
                    //    D2 = 3.3908E-2
                    //    D1 = -23.4890
                    //    D0 = 5.0238E3
                    //    PLL = 75.
                    //    PRL = 100.
                    //maxProdTemp = np.max(TenteringPP)
                    //CCAPPLL = C2 * maxProdTemp * *2 + C1 * maxProdTemp + C0
                    //CCAPPRL = D2 * maxProdTemp * *2 + D1 * maxProdTemp + D0
                    //b = math.log(CCAPPRL / CCAPPLL) / math.log(PRL / PLL)
                    //a = CCAPPRL / PRL * *b
                    //Cplantcorrelation = 0.8 * a * math.pow(np.max(ElectricityProduced), b) * np.max(ElectricityProduced) * 1000./ 1E6 #factor 0.75 to make double flash 25% more expansive than single flash
                }
                else if (simulationParms.pptype == 4) //double-flash
                {
                    double C2 = 0;
                    double C1 = 0;
                    double C0 = 0;
                    double D2 = 0;
                    double D1 = 0;
                    double D0 = 0;
                    double PLL = 0;
                    double PRL = 0;
                    if (ElectricityProduced.Max() < 10.0)
                    {
                        C2 = 4.8472E-2;
                        C1 = -35.2186;
                        C0 = 8.4474E3;
                        D2 = 4.0604E-2;
                        D1 = -29.3817;
                        D0 = 6.9911E3;
                        PLL = 5.0;
                        PRL = 10.0;
                    }
                    else if (ElectricityProduced.Max() < 25.0)
                    {
                        C2 = 4.0604E-2;
                        C1 = -29.3817;
                        C0 = 6.9911E3;
                        D2 = 3.2773E-2;
                        D1 = -23.5519;
                        D0 = 5.5263E3;
                        PLL = 10.0;
                        PRL = 25.0;
                    }
                    else if (ElectricityProduced.Max() < 50.0)
                    {
                        C2 = 3.2773E-2;
                        C1 = -23.5519;
                        C0 = 5.5263E3;
                        D2 = 3.4716E-2;
                        D1 = -23.8139;
                        D0 = 5.1787E3;
                        PLL = 25.0;
                        PRL = 50.0;
                    }
                    else if (ElectricityProduced.Max() < 75.0)
                    {
                        C2 = 3.4716E-2;
                        C1 = -23.8139;
                        C0 = 5.1787E3;
                        D2 = 3.5271E-2;
                        D1 = -24.3962;
                        D0 = 5.1972E3;
                        PLL = 50.0;
                        PRL = 75.0;
                    }
                    else
                    {
                        C2 = 3.5271E-2;
                        C1 = -24.3962;
                        C0 = 5.1972E3;
                        D2 = 3.3908E-2;
                        D1 = -23.4890;
                        D0 = 5.0238E3;
                        PLL = 75.0;
                        PRL = 100.0;
                    }
                    var maxProdTemp = TenteringPP.Max();
                    var CCAPPLL = C2 * Math.Pow(maxProdTemp, 2) + C1 * maxProdTemp + C0;
                    var CCAPPRL = D2 * Math.Pow(maxProdTemp, 2) + D1 * maxProdTemp + D0;
                    var b = Math.Log(CCAPPRL / CCAPPLL) / Math.Log(PRL / PLL);
                    //var a = Math.Pow((CCAPPRL / PRL), b);
                    var a = CCAPPRL / Math.Pow(PRL, b);
                    Cplantcorrelation = a * Math.Pow((ElectricityProduced.Max()), b) * ElectricityProduced.Max() * 1000.0 / 1E6;
                }
                if (ccParms.ccplantfixedvalid == 1)
                {
                    Cplant = ccParms.ccplantfixed;
                }
                else
                {
                    Cplant = 1.12 * 1.15 * ccParms.ccplantadjfactor * Cplantcorrelation * 1.02; //1.02 to convert cost from 2012 to 2016 #factor 1.15 for 15% contingency and 1.12 for 12% indirect costs.
                }

            }

            //add direct-use plant cost of co-gen system to Cplant (only of no total ccplant was provided)
            if (ccParms.ccplantfixedvalid == 0) //1.15 below for contingency and 1.12 for indirect costs
            {
                int option = (int)Math.Floor((double)simulationParms.enduseoption / 10);
                if (option == 3) //enduseoption = 3: cogen topping cycle
                {
                    Cplant = Cplant + 1.12 * 1.15 * ccParms.ccplantadjfactor * 250E-6 * (HeatProduced.Max() / stParms.enduseefficiencyfactor) * 1000.0;
                }
                else if (option == 4) //enduseoption = 4) cogen bottoming cycle
                {
                    //Cplant = Cplant + 1.12 * 1.15 * ccplantadjfactor * 250E-6 * np.max(HeatProduced / enduseefficiencyfactor) * 1000.
                }
                else if (option == 5) //enduseoption = 5: cogen parallel cycle
                {
                    //Cplant = Cplant + 1.12 * 1.15 * ccplantadjfactor * 250E-6 * np.max(HeatProduced / enduseefficiencyfactor) * 1000.
                }

            }
            calcResult.Cplant = Cplant;

            double Cexpl = 0;
            double Cpiping = 0;
            if (ccParms.totalcapcostvalid == 0)
            {
                // exploration costs (same as in Geophires v1.2) (M$)
                if (ccParms.ccexplfixedvalid == 1)
                {
                    Cexpl = ccParms.ccexplfixed;
                }
                else
                {
                    Cexpl = 1.15 * ccParms.ccexpladjfactor * 1.12 * (1.0 + C1well * 0.6); //1.15 for 15% contingency and 1.12 for 12% indirect costs
                }
                //Surface Piping Length Costs (M$) #assumed $750k/km
                Cpiping = 750 / 1000 * ccParms.pipinglength;
                Ccap = Cexpl + Cwell + Cstim + Cgath + Cplant + Cpiping;
            }
            else
            {
                Ccap = ccParms.TotalCapitalCost;
            }
            calcResult.Cpiping = Cpiping;
            calcResult.Cexpl = Cexpl;
            calcResult.Ccap = Ccap;
        }

        private void OandCosts()
        {
            //---------
            // O&M costs
            //---------
            double Claborcorrelation;
            double Coamplant = 0;
            double Coamwell = 0;
            double Coamwater = 0;
            if (ccParms.oamtotalfixedvalid == 0)
            {
                // labor cost
                if (simulationParms.enduseoption == 1) //lectricity
                {
                    var npElectricityProduced = np.array(ElectricityProduced);
                    var maxElectricityProduced = (double)Numpy.np.max(npElectricityProduced);
                    if (maxElectricityProduced < 2.5)
                    {
                        Claborcorrelation = 236.0 / 1E3; //M$/year
                    }
                    else
                    {
                        double logMaxElectricityProduced = Math.Log(maxElectricityProduced);
                        Claborcorrelation = ((589.0 * logMaxElectricityProduced) - 304.0) / 1E3; //M$/year
                    }
                }
                else
                {
                    var npHeatExtracted = np.array(HeatExtracted);
                    var maxHeatExtracted = (double)Numpy.np.max(npHeatExtracted);
                    if (maxHeatExtracted < 2.5 * 5.0)
                    {
                        Claborcorrelation = 236.0 / 1E3; //M$/year
                    }
                    else
                    {
                        double logMaxHeatExtracted = Math.Log(maxHeatExtracted / 5.0);
                        Claborcorrelation = ((589.0 * logMaxHeatExtracted) - 304.0) / 1E3; //M$/year
                    }
                }

                Claborcorrelation = Claborcorrelation * 1.1;  //1.1 to convert from 2012 to 2016$ with BLS employment cost index (for utilities in March)

                //plant O&M cost
                if (ccParms.oamplantfixedvalid == 1)
                {
                    Coamplant = ccParms.oamplantfixed;
                }
                else
                {
                    Coamplant = ccParms.oamplantadjfactor * (1.5 / 100.0 * Cplant + 0.75 * Claborcorrelation);
                }

                // wellfield O&M cost
                if (ccParms.oamwellfixedvalid == 1)
                {
                    Coamwell = ccParms.oamwellfixed;
                }
                else
                {
                    Coamwell = ccParms.oamwelladjfactor * (1.0 / 100.0 * (Cwell + Cgath) + 0.25 * Claborcorrelation);
                }

                // water O&M cost
                if (ccParms.oamwaterfixedvalid == 1)
                {
                    Coamwater = ccParms.oamwaterfixed;
                }
                else
                {
                    Coamwater = ccParms.oamwateradjfactor * (sstParms.nprod * sstParms.prodwellflowrate * sstParms.waterloss * stParms.utilfactor * 365.0 * 24.0 * 3600.0 / 1E6 * 925.0 / 1E6); //here is assumed 1 l per kg maybe correct with real temp. (M$/year) 925$/ML = 3.5$/1,000 gallon 
                }

                Coam = Coamwell + Coamplant + Coamwater; //total O&M cost (M$/year)
            }
            else
            {
                Coam = ccParms.oamtotalfixed; //total O&M cost (M$/year)
            }
            calcResult.Coamwell = Coamwell;
            calcResult.Coamplant = Coamplant;
            calcResult.Coamwater = Coamwater;


            if (redrill > 0)   //account for well redrilling
            {
                Coam = Coam + (Cwell + Cstim) * redrill / finParms.plantlifetime;
            }
            calcResult.Coam = Coam;
        }

        private void CalculateAnnualElectricityAndHeatProduction()
        {
            //---------------------------------------------
            // Calculate annual electricity/heat production
            //---------------------------------------------

            //all these end-use options have an electricity generation component

            double[] HeatkWhExtracted = new double[] { 0 };
            var npHeatkWhExtracted = np.zeros(finParms.plantlifetime);
            PumpingkWh = np.zeros(finParms.plantlifetime);
            float dx = (float)(1.0 / simulationParms.timestepsperyear * 365.0 * 24.0);
            for (int i = 0; i < finParms.plantlifetime; i++)
            {
                int start = 0 + i * simulationParms.timestepsperyear;
                int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                var y = HeatExtracted[start..end];
                var npy = np.array(y);
                npHeatkWhExtracted[i] = (NDarray)np.trapz(y, null, dx) * 1000.0 * stParms.utilfactor;
                y = PumpingPower[start..end];
                PumpingkWh[i] = (NDarray)np.trapz(y, null, dx) * 1000.0 * stParms.utilfactor;
            }
            HeatkWhExtracted = npHeatkWhExtracted.GetData<double>();
            calcResult.HeatkWhExtracted = HeatkWhExtracted;

            double[] NetkWhProduced = new double[] { 0 };
            NDarray npNetkWhProduced = np.zeros(1);
            if (simulationParms.enduseoption == 1 || simulationParms.enduseoption > 2)
            {
                var TotalkWhProduced = np.zeros(finParms.plantlifetime);
                npNetkWhProduced = np.zeros(finParms.plantlifetime);
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    int start = 0 + i * simulationParms.timestepsperyear;
                    int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                    int length = end - start;
                    double[] ep = ElectricityProduced.SubArray(start, length);
                    var npep = np.array(ep);
                    var nep = np.array(NetElectricityProduced.SubArray(start, length));
                    //float dx = (float)(1.0 / simulationParms.timestepsperyear * 365.0 * 24.0);
                    TotalkWhProduced[i] = (NDarray)np.trapz(npep, dx: dx) * 1000.0 * stParms.utilfactor;
                    npNetkWhProduced[i] = (NDarray)np.trapz(nep, dx: dx) * 1000.0 * stParms.utilfactor;
                }
            }
            NetkWhProduced = npNetkWhProduced.GetData<double>();
            calcResult.NetkWhProduced = NetkWhProduced;
            NDarray npHeatkWhProduced = np.zeros(1);
            double[] HeatkWhProduced = new double[] { 0 };

            // all those end-use options have a direct-use component
            if (simulationParms.enduseoption > 1)
            {
                npHeatkWhProduced = np.zeros(finParms.plantlifetime);
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    int start = 0 + i * simulationParms.timestepsperyear;
                    int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                    int length = end - start;
                    double[] hp = HeatProduced.SubArray(start, length);
                    var nphp = np.array(hp);
                    npHeatkWhProduced[i] = (NDarray)np.trapz(nphp, dx: dx) * 1000.0 * stParms.utilfactor;
                    //npHeatkWhProduced[i] = np.trapz(HeatProduced[(0 + i * simulationParms.timestepsperyear):((i + 1) * simulationParms.timestepsperyear) + 1], dx = 1./ timestepsperyear * 365.* 24.) * 1000.* utilfactor
                }
                HeatkWhProduced = npHeatkWhProduced.GetData<double>();
            }
            calcResult.HeatkWhProduced = HeatkWhProduced;
        }

        private void CalculateReservoirHeatContent()
        {
            //-------------------------------- 
            //calculate reservoir heat content
            //-------------------------------- 
            var npHeatkWhExtracted = np.array(calcResult.HeatkWhExtracted);
            var InitialReservoirHeatContent = sstParms.resvol * sstParms.rhorock * sstParms.cprock * (Trock - sstParms.Tinj) / 1E15;   //10^15 J
            var npRemainingReservoirHeatContent = InitialReservoirHeatContent - np.cumsum(npHeatkWhExtracted) * 3600 * 1E3 / 1E15;
            var RemainingReservoirHeatContent = npRemainingReservoirHeatContent.GetData<double>();
            calcResult.RemainingReservoirHeatContent = RemainingReservoirHeatContent;
            calcResult.InitialReservoirHeatContent = InitialReservoirHeatContent;
        }

        private void CalculateLCOEandLCOH()
        {
            //---------------------------
            //Calculate LCOE/LCOH
            //---------------------------
            double averageannualpumpingcosts = 0;
            double Price = 0.0;
            if (finParms.econmodel == 1) //simple FCR model
            {
                if (simulationParms.enduseoption == 1)
                {
                    var average = np.average(calcResult.NetkWhProduced);
                    Price = (finParms.FCR * (1 + finParms.inflrateconstruction) * Ccap + Coam) / average * 1E8; //cents/kWh
                }
                else if (simulationParms.enduseoption == 2)
                {
                    //    averageannualpumpingcosts = np.average(PumpingkWh) * elecprice / 1E6 #M$/year
                    //    Price = (FCR * (1 + inflrateconstruction) * Ccap + Coam + averageannualpumpingcosts) / np.average(HeatkWhProduced) * 1E8 #cents/kWh
                    //    Price = Price * 2.931 #$/Million Btu
                }
                else if (simulationParms.enduseoption > 2) //cogeneration
                {
                    if (simulationParms.enduseoption % 10 == 1) //heat sales is additional income revenue stream
                    {
                        //        averageannualheatincome = np.average(HeatkWhProduced) * heatprice / 1E6 #M$/year ASSUMING heatprice IS IN $/KWH FOR HEAT SALES
                        //        Price = (FCR * (1 + inflrateconstruction) * Ccap + Coam - averageannualheatincome) / np.average(NetkWhProduced) * 1E8 #cents/kWh   
                    }
                    else if (simulationParms.enduseoption % 10 == 2) //electricity sales is additional income revenue stream
                    {
                        //        averageannualelectricityincome = np.average(NetkWhProduced) * elecprice / 1E6 #M$/year
                        //        Price = (FCR * (1 + inflrateconstruction) * Ccap + Coam - averageannualelectricityincome) / np.average(HeatkWhProduced) * 1E8 #cents/kWh
                        //        Price = Price * 2.931 #$/MMBTU
                    }
                }

            }
            else if (finParms.econmodel == 2) //standard levelized cost model
            {
                var tempnp = np.array(1 + finParms.discountrate);
                var discountvector = 1.0 / np.power(tempnp, np.linspace(0, finParms.plantlifetime - 1, finParms.plantlifetime));
                if (simulationParms.enduseoption == 1)
                {
                    //cents/kWh
                    var npPrice = ((1 + finParms.inflrateconstruction) * Ccap + np.sum(Coam * discountvector)) / np.sum(calcResult.NetkWhProduced * discountvector) * 1E8;
                    Price = (double)npPrice;
                }
                else if (simulationParms.enduseoption == 2)
                {
                    //M$/year
                    averageannualpumpingcosts = np.average(PumpingkWh) * ccParms.elecprice / 1E6;
                    //cents/kWh
                    var npPrice = ((1 + finParms.inflrateconstruction) * Ccap + np.sum((Coam + PumpingkWh * ccParms.elecprice / 1E6) * discountvector)) / np.sum(calcResult.HeatkWhProduced * discountvector) * 1E8;
                    Price = (double)npPrice;
                    //$/MMBTU
                    Price = Price * 2.931;
                }
                else if (simulationParms.enduseoption > 2)
                {
                    int option = (int)Math.Floor((double)simulationParms.enduseoption / 10);
                    //if enduseoption % 10 == 1: #heat sales is additional income revenue stream
                    //    annualheatincome = HeatkWhProduced * heatprice / 1E6 #M$/year ASSUMING heatprice IS IN $/KWH FOR HEAT SALES
                    //    Price = ((1 + inflrateconstruction) * Ccap + np.sum((Coam - annualheatincome) * discountvector)) / np.sum(NetkWhProduced * discountvector) * 1E8 #cents/kWh
                    //elif enduseoption % 10 == 2: #electricity sales is additional income revenue stream
                    //    annualelectricityincome = NetkWhProduced * elecprice / 1E6 #M$/year
                    //    Price = ((1 + inflrateconstruction) * Ccap + np.sum((Coam - annualelectricityincome) * discountvector)) / np.sum(HeatkWhProduced * discountvector) * 1E8 #cents/kWh
                    //    Price = Price * 2.931 #$/MMBTU
                }
            }
            else if (finParms.econmodel == 3) //bicycle model
            {
                var iave = finParms.FIB * finParms.BIR * (1 - finParms.CTR) + (1 - finParms.FIB) * finParms.EIR; //average return on investment (tax and inflation adjusted)
                var CRF = iave / (1 - Math.Pow(1 + iave, -finParms.plantlifetime)); //capital recovery factor
                var inflationvector = np.power(np.array(1 + finParms.RINFL), np.linspace(1, finParms.plantlifetime, finParms.plantlifetime));
                var discountvector = 1.0 / np.power(np.array(1 + iave), np.linspace(1, finParms.plantlifetime, finParms.plantlifetime));
                var NPVcap = np.sum((1 + finParms.inflrateconstruction) * Ccap * CRF * discountvector);
                var NPVfc = np.sum((1 + finParms.inflrateconstruction) * Ccap * finParms.PTR * inflationvector * discountvector);
                var NPVit = np.sum(finParms.CTR / (1 - finParms.CTR) * ((1 + finParms.inflrateconstruction) * Ccap * CRF - Ccap / finParms.plantlifetime) * discountvector);
                var NPVitc = (1 + finParms.inflrateconstruction) * Ccap * finParms.RITC / (1 - finParms.CTR);
                if (simulationParms.enduseoption == 1)
                {
                    var NPVoandm = np.sum(Coam * inflationvector * discountvector);
                    var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                    var npPrice = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc) / np.sum(calcResult.NetkWhProduced * inflationvector * discountvector) * 1E8;
                    Price = (double)npPrice;
                }
                else if (simulationParms.enduseoption == 2)
                {
                    var PumpingCosts = PumpingkWh * ccParms.elecprice / 1E6;
                    averageannualpumpingcosts = np.average(PumpingkWh) * ccParms.elecprice / 1E6; //M$/year
                    var NPVoandm = np.sum((Coam + PumpingCosts) * inflationvector * discountvector);
                    var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                    var npPrice = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc) / np.sum(calcResult.HeatkWhProduced * inflationvector * discountvector) * 1E8;
                    npPrice = npPrice * 2.931; //$/MMBTU
                    Price = (double)npPrice;
                }
                else if (simulationParms.enduseoption > 2)
                {
                    if (simulationParms.enduseoption % 10 == 1) //heat sales is additional income revenue stream
                    {
                        var annualheatincome = np.array(calcResult.HeatkWhProduced) * ccParms.heatprice / 1E6; //M$/year ASSUMING ELECPRICE IS IN $/KWH FOR HEAT SALES
                        var NPVoandm = np.sum(Coam * inflationvector * discountvector);
                        var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                        var npPrice = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc - np.sum(annualheatincome * inflationvector * discountvector)) / np.sum(calcResult.NetkWhProduced * inflationvector * discountvector) * 1E8;
                        Price = (double)npPrice;
                    }
                    else if (simulationParms.enduseoption % 10 == 2) //electricity sales is additional income revenue stream
                    {
                        var annualelectricityincome = np.array(calcResult.NetkWhProduced) * ccParms.elecprice / 1E6; //M$/year
                        var NPVoandm = np.sum(Coam * inflationvector * discountvector);
                        var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                        var npPrice = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc - np.sum(annualelectricityincome * inflationvector * discountvector)) / np.sum(calcResult.HeatkWhProduced * inflationvector * discountvector) * 1E8;
                        Price = (double)npPrice * 2.931;  //$/MMBTU
                    }
                }
            }
            calcResult.Price = Price;
            calcResult.averageannualpumpingcosts = averageannualpumpingcosts;
        }
    }
}
