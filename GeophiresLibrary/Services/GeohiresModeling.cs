using GeophiresLibrary.Core;
using GeophiresLibrary.Extensions;
using GeophiresLibrary.Models;
using System.Numerics;
using System.Security.Claims;

namespace GeophiresLibrary.Services
{
    public class GeohiresModeling : IModeling
    {
        private SimulationParameters simulationParms { get; set; }
        private SubsurfaceTechnicalParameters sstParms { get; set; }
        private SurfaceTechnicalParameters stParms { get; set; }
        private FinancialParameters finParms { get; set; }
        private CapitalAndOMCostParameters ccParms { get; set; }

        private int redrill = 0;
        private double Trock;
        private double averagegradient;
        private double cpwater;
        private double Cwell;
        private double Cstim;
        private double Cgath;
        private double Ccap;
        private double Coam;
        private double Cplant = 0;
        private double[] timeVector;
        private double[] Tresoutput = { 0.0 };
        private double[] ProdTempDrop;
        private double[] ProducedTemperature;
        private double[] PumpingPower;
        private double[] pumpdepth;
        private double[] PumpingPowerProd;
        private double[] PumpingPowerInj;
        private double[] HeatExtracted = new double[] { 0 };
        private double[] HeatProduced;
        private double[] PumpingkWh;
        private double[] TenteringPP;
        private double[] ElectricityProduced;
        private double[] NetElectricityProduced = { 0.0 };
        private double rhowater;
        private CalculatedResults calcResult;

        public GeohiresModeling()
        {
            calcResult = new CalculatedResults();
        }

        public async Task<string> Modeling(InputParameters input)
        {
            string result = "";
            ProdTempDrop = [0.0];
            await ReadFromRepository(input);
            CalculateModel(input.TempDataContent);
            Console.WriteLine("Model created");
            result = await CreateReport();
            Console.WriteLine("Report created");
            return result;
        }

        public async Task ReadFromRepository(InputParameters input)
        {
            simulationParms = input.simParms;
            sstParms = input.subsurfaceParms;
            stParms = input.surfaceParms;
            finParms = input.financialParms;
            ccParms = input.capitalParms;
        }

        public async Task<string> CreateReport()
        {
            IReport report = new ConsoleReport();
            report.SummaryReport(simulationParms, sstParms, calcResult);
            report.EconomicReport(finParms, stParms);
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

        public void CalculateModel(string tempDataContent)
        {
            double[] intersecttemperature = CalculateMaximumWellDepth();
            Console.WriteLine("Start calculate reservoir temprature");
            CalculateInitialReservoirTemperature(intersecttemperature);
            Console.WriteLine("Start calculate temprature output");
            CalculateReservoirTemperatureOutput(tempDataContent);
            Console.WriteLine("Start calculate temprature drop");
            CalculateWellboreTemperatureDrop();
            Console.WriteLine("Start calculate pressure");
            CalculatePressureDropsAndPumpingPower();
            Console.WriteLine("Start calculate energy extracted and produced");
            CalculateEnergyExtractedAndProduced();
            Console.WriteLine("Start calculate cost");
            CapitalCosts();
            Console.WriteLine("Start calculate O&A cost");
            OandCosts();
            Console.WriteLine("Start calculate annual electricity and heat");
            CalculateAnnualElectricityAndHeatProduction();
            Console.WriteLine("Start calculate reservoir heat");
            CalculateReservoirHeatContent();
            Console.WriteLine("Start calculate LCOE and LCOH");
            CalculateLCOEandLCOH();
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
                    var average = Utilities.Average(calcResult.NetkWhProduced);
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
                var scalar = 1 + finParms.discountrate;
                var tmpArray = Utilities.linspace(0, finParms.plantlifetime - 1, finParms.plantlifetime);
                double[] discountvector = tmpArray.Select(exponent => 1.0 / Math.Pow(scalar, exponent)).ToArray();
                if (simulationParms.enduseoption == 1)
                {
                    //cents/kWh
                    var temp1 = (1 + finParms.inflrateconstruction) * Ccap;
                    double sumCoamDiscount = discountvector.Sum(dv => Coam * dv);
                    double sumNetkWhDiscount = calcResult.NetkWhProduced.Zip(discountvector, (nkp, dv) => nkp * dv).Sum();
                    Price = (temp1 + sumCoamDiscount) / sumNetkWhDiscount * 1E8;
                    //var npPrice = (temp1 + np.sum(Coam * np.array(discountvector))) / np.sum(calcResult.NetkWhProduced * np.array(discountvector)) * 1E8;
                    //Price = (double)npPrice;
                }
                else if (simulationParms.enduseoption == 2)
                {
                    //M$/year
                    averageannualpumpingcosts = Utilities.Average(PumpingkWh) * ccParms.elecprice / 1E6;
                    //cents/kWh
                    scalar = (1 + finParms.inflrateconstruction) * Ccap;
                    tmpArray = PumpingkWh.Select(x => Coam + x * ccParms.elecprice / 1E6).ToArray();
                    double sumResult1 = tmpArray.Zip(discountvector, (a, b) => a * b).Sum();
                    double sumResult2 = calcResult.HeatkWhProduced.Zip(discountvector, (a, b) => a * b).Sum();
                    Price = (scalar + sumResult1) / sumResult2 * 1E8;
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
                var tmpArray = Utilities.linspace(1, finParms.plantlifetime, finParms.plantlifetime);
                var inflationvector = tmpArray.Select(t => Math.Pow(1 + finParms.RINFL, t)).ToArray();
                var discountvector = tmpArray.Select(t => 1.0 / Math.Pow(1 + iave, t)).ToArray();
                var NPVcap = discountvector.Select(discount => (1 + finParms.inflrateconstruction) * Ccap * CRF * discount).Sum();
                var NPVfc = inflationvector
                        .Zip(discountvector, (inflation, discount) => (1 + finParms.inflrateconstruction) * Ccap * finParms.PTR * inflation * discount)
                        .Sum();
                var NPVit = discountvector
                        .Sum(discount => finParms.CTR / (1 - finParms.CTR) * ((1 + finParms.inflrateconstruction) * Ccap * CRF - Ccap / finParms.plantlifetime) * discount);
                var NPVitc = (1 + finParms.inflrateconstruction) * Ccap * finParms.RITC / (1 - finParms.CTR);
                if (simulationParms.enduseoption == 1)
                {
                    var NPVoandm = inflationvector.Zip(discountvector, (inflation, discount) => Coam * inflation * discount).Sum();
                    var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                    double denominator = calcResult.NetkWhProduced.Zip(inflationvector, (netKWh, inflation) => netKWh * inflation)
                                           .Zip(discountvector, (inflationTerm, discount) => inflationTerm * discount)
                                           .Sum();
                    Price = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc) / denominator * 1E8;
                }
                else if (simulationParms.enduseoption == 2)
                {
                    //var PumpingCosts = np.array(PumpingkWh) * ccParms.elecprice / 1E6;
                    var PumpingCosts = PumpingkWh.Select(kWh => kWh * ccParms.elecprice / 1E6).ToArray();
                    averageannualpumpingcosts = Utilities.Average(PumpingkWh) * ccParms.elecprice / 1E6; //M$/year
                    //var NPVoandm = np.sum((Coam + np.array(PumpingCosts)) * inflationvector * discountvector);
                    var tempArray = PumpingCosts.Select(cost => Coam + cost).ToArray();
                    var Voandm = tempArray.Zip(inflationvector, (temp, inflation) => temp * inflation)
                           .Zip(discountvector, (product, discount) => product * discount)
                           .Sum();
                    var Vgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + Voandm + NPVfc + NPVit - NPVitc);
                    var sum = calcResult.HeatkWhProduced.Zip(inflationvector, (heat, inflation) => heat * inflation)
                                 .Zip(discountvector, (product, discount) => product * discount)
                                 .Sum();
                    var npPrice = (NPVcap + Voandm + NPVfc + NPVit + Vgrt - NPVitc) / sum * 1E8;
                    Price = npPrice * 2.931; //$/MMBTU
                }
                else if (simulationParms.enduseoption > 2)
                {
                    if (simulationParms.enduseoption % 10 == 1) //heat sales is additional income revenue stream
                    {
                        //M$/year ASSUMING ELECPRICE IS IN $/KWH FOR HEAT SALES
                        var annualheatincome = calcResult.HeatkWhProduced.Select(heat => heat * ccParms.heatprice / 1E6).ToArray();
                        var NPVoandm = inflationvector.Zip(discountvector, (inflation, discount) => Coam * inflation * discount).Sum();
                        var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                        var tmpScalar = NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc;
                        //var npPrice = (NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc - np.sum(annualheatincome * np.array(inflationvector) * discountvector)) / np.sum(calcResult.NetkWhProduced * np.array(inflationvector) * discountvector) * 1E8;
                        var sumTerm = annualheatincome
                            .Zip(inflationvector, (income, inflation) => income * inflation)
                            .Zip(discountvector, (term, discount) => term * discount)
                            .Sum();
                        var sumTerm2 = calcResult.NetkWhProduced
                            .Zip(inflationvector, (produced, inflation) => produced * inflation)
                            .Zip(discountvector, (term, discount) => term * discount)
                            .Sum();
                        Price = (tmpScalar - sumTerm) / sumTerm2 * 1E8;
                    }
                    else if (simulationParms.enduseoption % 10 == 2) //electricity sales is additional income revenue stream
                    {
                        var annualelectricityincome = calcResult.NetkWhProduced.Select(nkp => nkp * ccParms.elecprice / 1E6).ToArray();
                        //var annualelectricityincome = np.array(calcResult.NetkWhProduced) * ccParms.elecprice / 1E6;
                        //var NPVoandm = np.sum(Coam * np.array(inflationvector) * discountvector);
                        var NPVoandm = inflationvector.Zip(discountvector, (infl, disc) => infl * disc)
                                       .Sum(x => x * Coam);
                        var NPVgrt = finParms.GTR / (1 - finParms.GTR) * (NPVcap + NPVoandm + NPVfc + NPVit - NPVitc);
                        double temp2 = NPVcap + NPVoandm + NPVfc + NPVit + NPVgrt - NPVitc;
                        double sumElectricityIncome = annualelectricityincome.Zip(inflationvector, (aei, infl) => aei * infl)
                                                             .Zip(discountvector, (aeiInfl, disc) => aeiInfl * disc)
                                                             .Sum();
                        double sumHeatkWhProduced = calcResult.HeatkWhProduced.Zip(inflationvector, (hkp, infl) => hkp * infl)
                                                                   .Zip(discountvector, (hkpInfl, disc) => hkpInfl * disc)
                                                                   .Sum();
                        Price = (temp2 - sumElectricityIncome) / sumHeatkWhProduced * 1E8;
                        //var npPrice = (temp2 - np.sum(annualelectricityincome * np.array(inflationvector) * discountvector)) / np.sum(calcResult.HeatkWhProduced * np.array(inflationvector) * discountvector) * 1E8;
                        //Price = (double)npPrice * 2.931;  //$/MMBTU
                    }
                }
            }
            calcResult.Price = Price;
            calcResult.averageannualpumpingcosts = averageannualpumpingcosts;
        }

        private void CalculateReservoirHeatContent()
        {
            //-------------------------------- 
            //calculate reservoir heat content
            //-------------------------------- 
            //10^15 J
            var InitialReservoirHeatContent = sstParms.resvol * sstParms.rhorock * sstParms.cprock * (Trock - sstParms.Tinj) / 1E15;
            double cumulativeSum = 0.0;
            double[] RemainingReservoirHeatContent = calcResult.HeatkWhExtracted
                .Select(value =>
                {
                    cumulativeSum += value;
                    return InitialReservoirHeatContent - cumulativeSum * 3600 * 1E3 / 1E15;
                })
                .ToArray();
            calcResult.RemainingReservoirHeatContent = RemainingReservoirHeatContent;
            calcResult.InitialReservoirHeatContent = InitialReservoirHeatContent;
        }

        private void CalculateAnnualElectricityAndHeatProduction()
        {
            //---------------------------------------------
            // Calculate annual electricity/heat production
            //---------------------------------------------

            //all these end-use options have an electricity generation component

            double[] HeatkWhExtracted = new double[finParms.plantlifetime];
            //var npHeatkWhExtracted = np.zeros(finParms.plantlifetime);
            PumpingkWh = new double[finParms.plantlifetime];
            float dx = (float)(1.0 / simulationParms.timestepsperyear * 365.0 * 24.0);
            Console.WriteLine($"Plantlifetime: {finParms.plantlifetime}");
            Console.WriteLine($"Timestepperyear: {simulationParms.timestepsperyear}");
            Console.WriteLine($"Heat count: {HeatExtracted.Count()}");
            for (int i = 0; i < finParms.plantlifetime; i++)
            {
                int start = 0 + i * simulationParms.timestepsperyear;
                int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                var y = HeatExtracted[start..end];
                HeatkWhExtracted[i] = Utilities.TrapezoidalIntegration(y, dx) * 1000.0 * stParms.utilfactor;
                y = PumpingPower[start..end];
                PumpingkWh[i] = Utilities.TrapezoidalIntegration(y, dx) * 1000.0 * stParms.utilfactor;
            }
            calcResult.HeatkWhExtracted = HeatkWhExtracted.ToArray();

            double[] NetkWhProduced = new double[finParms.plantlifetime];
            if (simulationParms.enduseoption == 1 || simulationParms.enduseoption > 2)
            {
                double[] TotalkWhProduced = new double[finParms.plantlifetime];
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    int start = 0 + i * simulationParms.timestepsperyear;
                    int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                    int length = end - start;
                    double[] ep = ElectricityProduced.SubArray(start, length);
                    var nep = NetElectricityProduced.SubArray(start, length);
                    TotalkWhProduced[i] = Utilities.TrapezoidalIntegration(ep, dx) * 1000.0 * stParms.utilfactor;
                    NetkWhProduced[i] = Utilities.TrapezoidalIntegration(nep, dx) * 1000.0 * stParms.utilfactor;
                }
            }
            calcResult.NetkWhProduced = NetkWhProduced.ToArray();

            double[] HeatkWhProduced = new double[finParms.plantlifetime];
            // all those end-use options have a direct-use component
            if (simulationParms.enduseoption > 1)
            {
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    int start = 0 + i * simulationParms.timestepsperyear;
                    int end = ((i + 1) * simulationParms.timestepsperyear) + 1;
                    int length = end - start;
                    double[] hp = HeatProduced.SubArray(start, length);
                    HeatkWhProduced[i] = Utilities.TrapezoidalIntegration(hp, dx) * 1000.0 * stParms.utilfactor;
                }
            }
            calcResult.HeatkWhProduced = HeatkWhProduced;
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
                    var maxElectricityProduced = ElectricityProduced.Max();
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
                    var maxHeatExtracted = HeatExtracted.Max();
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
                    var pumphp = PumpingPower.Max() * 1341;
                    var numberofpumps = Math.Ceiling(pumphp / 2000);
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
                        double maxPower = PumpingPowerProd.Max();
                        double maxPumpDepth = pumpdepth.Max();
                        double prodpumphp = maxPower / sstParms.nprod * 1341;
                        Cpumpsprod = sstParms.nprod * 1.5 * (1750 * Math.Pow(prodpumphp, 0.7) +
                            5750 * Math.Pow(prodpumphp, 0.2) + 10000 + maxPumpDepth * 50 * 3.281);
                    }
                    else
                    {
                        Cpumpsprod = 0;
                    }
                    var injpumphp = PumpingPowerInj.Max() * 1341;
                    //pump can be maximum 2,000 hp\n";
                    double numberofinjpumps = Math.Ceiling(injpumphp / 2000);
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
            if (simulationParms.enduseoption == 2) //direct-use
            {
                if (ccParms.ccplantfixedvalid == 1)
                {
                    Cplant = ccParms.ccplantfixed;
                }
                else
                {
                    //1.15 for 15% contingency and 1.12 for 12% indirect costs
                    Cplant = 1.12 * 1.15 * ccParms.ccplantadjfactor * 250E-6 * HeatExtracted.Max() * 1000.0;
                }
            }
            else //all other options have power plant
            {
                if (simulationParms.pptype == 1) //sub-critical ORC
                {
                    var MaxProducedTemperature = TenteringPP.Max();
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
                    var maxElectricityProduced = ElectricityProduced.Max();
                    Cplantcorrelation = CCAPP1 * Math.Pow((maxElectricityProduced / 15.0), -0.06) * maxElectricityProduced * 1000.0 / 1E6;
                }
                else if (simulationParms.pptype == 2) //supercritical ORC
                {
                    var MaxProducedTemperature = TenteringPP.Max();
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
                    var maxElectricityProduced = ElectricityProduced.Max();
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

        private void CalculateEnergyExtractedAndProduced()
        {
            string strPumpingPower = "";
            double[] ReinjTemp = new double[] { 0 };
            double[] etau = new double[] { 0 };
            double[] HeatExtractedTowardsElectricity = new double[] { 0 };
            double[] Availability = new double[] { 0 };
            double minReinjTemp;
            double[] FirstLawEfficiency = new double[] { 0 };
            //double[]HeatExtracted = new double[] { 0 };
            //NDarray npTenteringPP = np.zeros(1);
            //NDarray npAvailability = np.zeros(1);
            //NDarray npHeatExtracted = np.zeros(1);
            //NDarray npHeatProduced = np.zeros(1);
            //direct - use
            if (simulationParms.enduseoption == 2)
            {
                //heat extracted from geofluid [MWth]
                HeatExtracted = ProducedTemperature
                    .Select(x => sstParms.nprod * sstParms.prodwellflowrate * cpwater * (x - sstParms.Tinj) / 1E6)
                    .ToArray();
                //useful direct-use heat provided to application [MWth]
                HeatProduced = HeatExtracted.Select(x => x * stParms.enduseefficiencyfactor).ToArray();
            }
            else
            {
                if ((int)Math.Floor((double)(simulationParms.enduseoption / 10)) == 4)
                {
                    TenteringPP[0] = stParms.Tchpbottom;
                }
                else
                {
                    TenteringPP = new double[ProducedTemperature.Length];
                    Array.Copy(ProducedTemperature, TenteringPP, TenteringPP.Length);
                }

                var A = 4.041650;
                var B = -1.204E-2;
                var C = 1.60500E-5;
                var T0 = stParms.Tenv + 273.15;
                var T1 = TenteringPP.Select(x => x + 273.15).ToArray();
                double[] T12 = T1.Select(value => Math.Pow(value, 2)).ToArray();
                double[] T13 = T1.Select(value => Math.Pow(value, 3)).ToArray();
                var T2 = stParms.Tenv + 273.15;
                var T22 = Math.Pow(T2, 2);
                var T23 = Math.Pow(T2, 3);
                var scalar1 = (A - B * T0);
                var scalar2 = (B - C * T0);
                var tmpArray1 = T1.Select(x => x - T2).ToArray();
                tmpArray1 = tmpArray1.Select((x) => scalar1 * x).ToArray();
                var tmpArray2 = T12.Select(x => x - T22).ToArray();
                tmpArray2 = tmpArray2.Select(x => scalar2 / 2.0 * x).ToArray();
                var tmpArray3 = T13.Select(x => C / 3.0 * (x - T23)).ToArray();
                var tmpArray4 = T1.Select(value => A * T0 * Math.Log(value / T2)).ToArray();
                Availability = tmpArray1
                    .Zip(tmpArray2, (val1, val2) => val1 + val2)
                    .Zip(tmpArray3, (sum12, val3) => sum12 + val3)
                    .Zip(tmpArray4, (sum123, val4) => (sum123 - val4) * 2.2046 / 947.83)
                    .ToArray();
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
                    var etaull = TenteringPP.Select(x => C1 * x + C0).ToArray();
                    var etauul = TenteringPP.Select(x => D1 * x + D0).ToArray();
                    etau = etaull.Zip(etauul, (x, y) => (1 - Tfraction) * x + Tfraction * y).ToArray();
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
                    var reinjtll = TenteringPP.Select(x => C1 * x + C0).ToArray();
                    var reinjtul = TenteringPP.Select(x => D1 * x + D0).ToArray();
                    ReinjTemp = reinjtll.Zip(reinjtul, (x, y) => (1 - Tfraction) * x + Tfraction * y).ToArray();
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
                    double[] etaull = TenteringPP
                        .Select(value => C2 * Math.Pow(value, 2) + C1 * value + C0)
                        .ToArray();
                    double[] etauul = TenteringPP
                        .Select(value => D2 * Math.Pow(value, 2) + D1 * value + D0)
                        .ToArray();
                    etau = etaull
                        .Zip(etauul, (val1, val2) => (1 - Tfraction) * val1 + Tfraction * val2)
                        .ToArray();

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
                    var reinjtll = TenteringPP.Select(x => C1 * x + C0).ToArray();
                    var reinjtul = TenteringPP.Select(x => D1 * x + D0).ToArray();
                    ReinjTemp = reinjtll
                        .Zip(reinjtul, (val1, val2) => (1.0 - Tfraction) * val1 + Tfraction * val2)
                        .ToArray();
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
                    double[] etaull = TenteringPP
                        .Select(value => C2 * Math.Pow(value, 2) + C1 * value + C0)
                        .ToArray();
                    double[] etauul = TenteringPP
                        .Select(value => D2 * Math.Pow(value, 2) + D1 * value + D0)
                        .ToArray();
                    etau = etaull
                        .Zip(etauul, (val1, val2) => (1 - Tfraction) * val1 + Tfraction * val2)
                        .ToArray();
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
                    var reinjtll = TenteringPP
                        .Select(value => C2 * Math.Pow(value, 2) + C1 * value + C0)
                        .ToArray();
                    var reinjtul = TenteringPP
                        .Select(value => D2 * Math.Pow(value, 2) + D1 * value + D0)
                        .ToArray();
                    ReinjTemp = reinjtll
                        .Zip(reinjtul, (val1, val2) => (1.0 - Tfraction) * val1 + Tfraction * val2)
                        .ToArray();
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
                    ElectricityProduced = Availability
                        .Zip(etau, (val1, val2) => val1 * val2 * sstParms.nprod * sstParms.prodwellflowrate)
                        .ToArray();

                    // Heat extracted from geofluid [MWth]\n";
                    var tmpVal1 = sstParms.nprod * sstParms.prodwellflowrate * cpwater;
                    HeatExtracted = ProducedTemperature.Select(x => tmpVal1 * (x - sstParms.Tinj) / 1E6).ToArray();
                    HeatExtractedTowardsElectricity = HeatExtracted.ToArray();
                }
                // enduseoption = 3: cogen topping cycle
                else if (Convert.ToInt32(Math.Floor(tmpenduseoption)) == 3)
                {
                    ElectricityProduced = Availability.Zip(etau, (avail, e) => avail * e)
                        .Select(result => result * sstParms.nprod * sstParms.prodwellflowrate)
                        .ToArray();
                    //Heat extracted from geofluid [MWth]
                    HeatExtracted = ProducedTemperature
                        .Select(temp => sstParms.nprod * sstParms.prodwellflowrate * cpwater * (temp - sstParms.Tinj) / 1E6)
                        .ToArray();
                    ////Useful heat for direct-use application [MWth] 
                    HeatProduced = ReinjTemp
                        .Select(temp => stParms.enduseefficiencyfactor * sstParms.nprod * sstParms.prodwellflowrate * cpwater * (temp - sstParms.Tinj) / 1E6)
                        .ToArray();
                    HeatExtractedTowardsElectricity = ProducedTemperature
                        .Zip(ReinjTemp, (producedTemp, reinjTemp) => sstParms.nprod * sstParms.prodwellflowrate * cpwater * (producedTemp - reinjTemp) / 1E6)
                        .ToArray();
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
                    NetElectricityProduced = ElectricityProduced.Zip(PumpingPower, (x, y) => x - y).ToArray();
                    FirstLawEfficiency = NetElectricityProduced.Zip(HeatExtractedTowardsElectricity, (x, y) => x / y).ToArray();
                }

            }
            calcResult.NetElectricityProduced = NetElectricityProduced.ToArray();
            calcResult.FirstLawEfficiency = FirstLawEfficiency.ToArray();
            if (HeatProduced != null) calcResult.HeatProduced = HeatProduced.ToArray();
        }

        private void CalculatePressureDropsAndPumpingPower()
        {
            //------------------------------------------
            // calculate pressure drops and pumping power
            //------------------------------------------
            // production wellbore fluid conditions [kPa]
            double[] f3 = { 0.0 };
            double[] f1 = { 0.0 };
            double[] npf1 = { 0.0 };
            double[] npf3 = { 0.0 };
            var tmpProdTempDrop = ProdTempDrop.Select(X => X / 4.0).ToArray();
            double[] Tprodaverage = Tresoutput.SubtractArrays(tmpProdTempDrop);
            var rhowaterprod = Utilities.ArrayDensityWater(Tprodaverage);
            var muwaterprod = Utilities.ArrayViscosityWater(Tprodaverage);
            double[] vprod = rhowaterprod.
                Select(x => sstParms.prodwellflowrate / x / (Math.PI / 4.0 * Math.Pow(sstParms.prodwelldiam, 2))).
                ToArray();
            double[] Rewaterprod = muwaterprod.
                Select(x => 4.0 * sstParms.prodwellflowrate / (x * Math.PI * sstParms.prodwelldiam)).
                ToArray();
            //laminar or turbulent flow?
            var Rewaterprodaverage = Utilities.Average(Rewaterprod);
            if (Rewaterprodaverage < 2300.0)
            {
                npf3 = Rewaterprod.Select(x => 64.0 / x).ToArray();
            }
            else
            {
                var relroughness = 1E-4 / sstParms.prodwelldiam;
                f3 = Rewaterprod
                    .Select(R => -2.0 * Math.Log10(relroughness / 3.7 + 5.74 / Math.Pow(R, 0.9)))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                f3 = Rewaterprod.Zip(f3, (R, f) => -2.0 * Math.Log10(relroughness / 3.7 + 2.51 / (R * Math.Sqrt(f))))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                f3 = Rewaterprod.Zip(f3, (R, f) => -2.0 * Math.Log10(relroughness / 3.7 + 2.51 / (R * Math.Sqrt(f))))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                f3 = Rewaterprod.Zip(f3, (R, f) => -2.0 * Math.Log10(relroughness / 3.7 + 2.51 / (R * Math.Sqrt(f))))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                f3 = Rewaterprod.Zip(f3, (R, f) => -2.0 * Math.Log10(relroughness / 3.7 + 2.51 / (R * Math.Sqrt(f))))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                // 6 iterations to converge
                f3 = Rewaterprod.Zip(f3, (R, f) => -2.0 * Math.Log10(relroughness / 3.7 + 2.51 / (R * Math.Sqrt(f))))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
            }
            //injection well conditions
            double Tinjaverage = sstParms.Tinj;
            var tempArray = Utilities.linspace(1, 1, ProducedTemperature.Length);
            var rhowaterinj = tempArray.Select(x => Utilities.densitywater(Tinjaverage) * x).ToArray();
            //replace with correlation based on Tinjaverage
            var muwaterinj = tempArray.Select(x => Utilities.ViscosityWater(Tinjaverage) * x).ToArray();
            var scalar1 = (double)sstParms.nprod / sstParms.ninj * sstParms.prodwellflowrate * (1.0 + sstParms.waterloss);
            var scalar2 = (Math.PI / 4.0 * Math.Pow(sstParms.injwelldiam, 2));
            var vinj = rhowaterinj.Select(x => scalar1 / x / scalar2).ToArray();

            // injection well conditions
            scalar1 = 4.0 * sstParms.nprod / sstParms.ninj * sstParms.prodwellflowrate * (1.0 + sstParms.waterloss);
            scalar2 = Math.PI * sstParms.injwelldiam;
            var Rewaterinj = muwaterinj.Select(x => scalar1 / (x * scalar2)).ToArray();
            var Rewaterinjaverage = Utilities.Average(Rewaterinj);
            if (Rewaterinjaverage < 2300.0)
            {
                //var vidar1 = 64.0 / Rewaterinj;
                npf1 = Rewaterinj.Select(x => 64.0 / x).ToArray();
            }
            // Turbulent flow
            else
            {
                var relroughness = 1E-4 / sstParms.injwelldiam;
                npf1 = Rewaterinj
                    .Select(R => -2.0 * Math.Log10(relroughness / 3.7 + 5.74 / Math.Pow(R, 0.9)))
                    .Select(part1 => 1.0 / Math.Pow(part1, 2.0))
                    .ToArray();
                scalar1 = relroughness / 3.7;
                for (int i = 0; i < 5; i++)
                {
                    double[] tempArray1 = Rewaterinj.Select(R => 2.51 / R).ToArray();
                    double[] tempArray2 = Utilities.SqrtArray(npf1);
                    double[] tempArray3 = tempArray1.Zip(tempArray2, (a, b) => a / b).ToArray();
                    double[] tempArray4 = tempArray3.Select(R => scalar1 + R).ToArray();
                    double[] tempArray5 = tempArray4.Select(d => -2 * Math.Log10(d)).ToArray();
                    npf1 = tempArray5.Select(a => 1.0 / Math.Pow(a, 2.0)).ToArray();
                }
            }
            f1 = npf1;

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
                var powervinj = vinj.MathPow(2);
                DP1 = rhowaterinj.Zip(powervinj, (x, y) => (x * y) / 2).ToArray();
                DP1 = f1.Zip(DP1, (x, y) => x * y).ToArray();
                DP1 = DP1.Select(x => x * (sstParms.depth / sstParms.injwelldiam) / 1E3).ToArray();

                //reservoir pressure drop [kPa]
                var rhowaterreservoir = Tresoutput.Select(x => 0.1 * sstParms.Tinj + 0.9 * x).ToArray();
                rhowaterreservoir = Utilities.ArrayDensityWater(rhowaterreservoir); //based on TARB in Geophires v1.2
                DP2 = rhowaterreservoir
                    .Select(x => sstParms.impedance * sstParms.nprod * sstParms.prodwellflowrate * 1000.0 / x)
                    .ToArray();

                //production well pressure drop [kPa]
                DP3 = vprod.MathPow(2);
                DP3 = DP3.Zip(rhowaterprod, (x, y) => y * x / 2.0).ToArray();
                DP3 = DP3.Zip(f3, (x, y) => y * x).ToArray();
                DP3 = DP3.Select(x => x * (sstParms.depth / sstParms.prodwelldiam) / 1E3).ToArray();

                //buoyancy pressure drop [kPa]
                DP4 = rhowaterprod.Zip(rhowaterinj, (x, y) => (x - y) * sstParms.depth * 9.81 / 1E3).ToArray();

                //overall pressure drop
                DP = DP1.Zip(DP2, (x, y) => x + y)
                        .Zip(DP3, (xy, z) => xy + z)
                        .Zip(DP4, (xyz, w) => xyz + w)
                        .ToArray();

                //calculate pumping power [MWe] (approximate)
                PumpingPower = DP.Select(x => x * sstParms.nprod * sstParms.prodwellflowrate * (1 + sstParms.waterloss)).ToArray();
                PumpingPower = PumpingPower.Zip(rhowaterinj, (x, y) => x / y / stParms.pumpeff / 1E3).ToArray();

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
                    scalar1 = Pminimum - sstParms.Phydrostatic + sstParms.prodwellflowrate / PIkPa;
                    scalar2 = 1 / sstParms.prodwelldiam;
                    //var tempArray1 = f3.Zip(rhowaterprod, (f3Value, rhowaterprodValue) => f3Value * rhowaterprodValue).ToArray();
                    var tempArray1 = vprod.MathPow(2).Select(value => value / 2.0).ToArray();
                    tempArray1 = rhowaterprod.Zip(tempArray1, (x, y) => x * y).ToArray();
                    tempArray1 = f3.Zip(tempArray1, (x, y) => x * y).ToArray();
                    tempArray1 = tempArray1.Select(x => x * scalar2 / 1E3).ToArray();
                    var tempArray2 = rhowaterprod.Select(y => y * 9.81 / 1E3).ToArray();
                    tempArray2 = tempArray2.Zip(tempArray1, (x, y) => x + y).ToArray();
                    pumpdepth = tempArray2.Select(y => sstParms.depth + scalar1 / y).ToArray();
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
                    scalar1 = sstParms.Phydrostatic - sstParms.prodwellflowrate / PIkPa;
                    scalar2 = sstParms.depth / sstParms.prodwelldiam;
                    tempArray1 = rhowaterprod.Select(x => x * 9.81 * sstParms.depth / 1E3).ToArray();
                    tempArray2 = vprod.MathPow(2).Select(x => x / 2.0).ToArray();
                    tempArray2 = tempArray2.Zip(rhowaterprod, (x, y) => x * y).ToArray();
                    tempArray2 = tempArray2.Zip(f3, (x, y) => x * y).ToArray();
                    tempArray2 = tempArray2.Select(x => x * scalar2 / 1E3).ToArray();
                    DP3 = tempArray1
                        .Zip(tempArray2, (val1, val2) => scalar1 - val1 - val2)
                        .ToArray();
                    DP3 = DP3.Select(x => Pprodwellhead - x).ToArray();
                    //var vidar = Pprodwellhead - np.array(DP3);
                    calcResult.DP3 = DP3;
                    //#DP3 = [0 if x<0 else x for x in DP3] #set negative values to 0
                    //[MWe] total pumping power for production wells
                    tempArray1 = DP3.Select(x => x * sstParms.nprod * sstParms.prodwellflowrate).ToArray();
                    PumpingPowerProd = tempArray1
                        .Zip(rhowaterprod, (val1, val2) => val1 / val2 / stParms.pumpeff / 1E3)
                        .ToArray();
                    PumpingPowerProd = PumpingPowerProd.NegativeToZero();
                }

                double IIkPa = sstParms.II / 100; //convert II from kg/s/bar to kg/s/kPa    

                //necessary injection wellhead pressure [kPa]
                var powervinj = vinj.MathPow(2);
                scalar1 = sstParms.Phydrostatic + sstParms.prodwellflowrate * (1 + sstParms.waterloss) * sstParms.nprod / sstParms.ninj / IIkPa;
                var tmpArray1 = rhowaterinj.Select(x => scalar1 - x * 9.81 * sstParms.depth / 1E3).ToArray();
                var tmpArray2 = rhowaterinj.Zip(powervinj, (x, y) => x * y / 2).ToArray();
                tmpArray2 = f1.Zip(tmpArray2, (x, y) => x * y).ToArray();
                tmpArray2 = tmpArray2.Select(x => x * (sstParms.depth / sstParms.injwelldiam) / 1E3).ToArray();
                var Pinjwellhead = tmpArray1.Zip(tmpArray2, (x, y) => x + y).ToArray();

                // plant outlet pressure [kPa]
                if (sstParms.usebuiltinoutletplantcorrelation == 1)
                {
                    double DPSurfaceplant = 68.95; //[kPa] assumes 10 psi pressure drop in surface equipment
                    sstParms.Pplantoutlet = Pprodwellhead - DPSurfaceplant;
                }

                //injection pump pressure [kPa]
                DP1 = Pinjwellhead.Select(x => x - sstParms.Pplantoutlet).ToArray();
                //#DP1 = [0 if x<0 else x for x in DP1] #set negative values to 0
                //[MWe] total pumping power for injection wells
                scalar1 = sstParms.nprod * sstParms.prodwellflowrate * (1 + sstParms.waterloss);
                scalar2 = stParms.pumpeff / 1E3;
                tmpArray1 = DP1.Select(x => x * scalar1).ToArray();
                PumpingPowerInj = tmpArray1
                    .Zip(rhowaterinj, (val1, val2) => val1 / val2 / stParms.pumpeff / 1E3)
                    .ToArray();
                PumpingPowerInj = PumpingPowerInj.NegativeToZero();

                // total pumping power
                if (sstParms.productionwellpumping == 1)
                {
                    PumpingPower = PumpingPowerInj.Zip(PumpingPowerProd, (x, y) => x + y).ToArray();
                }
                else
                {
                    PumpingPower = new double[PumpingPowerInj.Length];
                    Array.Copy(PumpingPowerInj, PumpingPower, PumpingPower.Length);
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

        private void CalculateReservoirTemperatureOutput(string tempdataContent)
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

                double[] td = timeVector.Select(x => x * 365.0 * 24.0 * 3600 / tres).ToArray();
                ILaplaceInversion pi = new LaplaceInversionTalbot();
                double[] temptd = new double[td.Length - 1]; ;
                for (int i = 1; i < td.Length; i++)
                {
                    temptd[i - 1] = (double)td[i];
                }
                double[] Twnd = pi.Calculate(fp, temptd);

                //calculate dimensional temperature, add error-handling for non-sensical temperatures
                Tresoutput = new double[Twnd.Length + 1];
                double[] tempTresoutput = Twnd.Select(x => x * (Trock - sstParms.Tinj) + sstParms.Tinj).ToArray();
                Tresoutput[0] = Trock;
                for (int i = 0; i < tempTresoutput.Length; i++)
                {
                    Tresoutput[i + 1] = tempTresoutput[i];
                }
                for (int i = 0; i < Tresoutput.Length; i++)
                {
                    if (Tresoutput[i] > Trock) Tresoutput[i] = Trock;
                    if (Tresoutput[i] < sstParms.Tinj) Tresoutput[i] = Trock;
                    if (double.IsNaN(Tresoutput[i])) Tresoutput[i] = Trock;
                }
            }
            else if (sstParms.resoption == 3)
            {
                Tresoutput = new double[timeVector.Length];
                Tresoutput[0] = Trock;
                for (int i = 1; i < timeVector.Length; i++)
                {
                    double x = 1.0 / sstParms.drawdp / cpwater * Math.Sqrt(sstParms.krock * sstParms.rhorock * sstParms.cprock / timeVector[i] / (365.0 * 24.0 * 3600.0));
                    Tresoutput[i] = Utilities.Erf(x) * (Trock - sstParms.Tinj) + sstParms.Tinj;
                }
            }
            else if (sstParms.resoption == 4)
            {
                //this is no longer as in thesis (equation 4.16)
                Tresoutput = timeVector.Select(x => (1 - sstParms.drawdp * x) * (Trock - sstParms.Tinj) + sstParms.Tinj).ToArray();
            }
            else if (sstParms.resoption == 5)
            {

                Tresoutput[0] = Trock;
                string[] content = tempdataContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                //string restempfname = sstParms.filenamereservoiroutput;
                //string[] content = new string[0];
                //if (File.Exists(restempfname))
                //{
                //    content = File.ReadAllLines(restempfname);
                //}
                //else
                //{
                //    Console.WriteLine($"Error: GEOPHIRES could not read reservoir output file ({restempfname}) and will abort simulation.");
                //    Environment.Exit(1);
                //}
                int numlines = 0;
                numlines = content.Length;
                if (numlines != finParms.plantlifetime * simulationParms.timestepsperyear + 1)
                {
                    Console.WriteLine($"Error: Reservoir output file does not have required {finParms.plantlifetime * simulationParms.timestepsperyear + 1} lines. GEOPHIRES will abort simulation.'");
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

            Console.WriteLine("Completing CalculateReservoirTemperatureOutput");
        }

        private void CalculateWellboreTemperatureDrop()
        {
            if (sstParms.rameyoptionprod == 0)
            {
                ProdTempDrop[0] = sstParms.tempdropprod;
            }
            else if (sstParms.rameyoptionprod == 1)
            {
                var alpharock = sstParms.krock / (sstParms.rhorock * sstParms.cprock);
                double[] framey = new double[timeVector.Length];
                double scalar1 = 1.1 * (sstParms.prodwelldiam / 2.0);
                double scalar2 = 4.0 * alpharock * 365.0 * 24.0 * 3600.0 * stParms.utilfactor;
                double[] tempArray = Utilities.SliceArray(timeVector, 1);
                tempArray = tempArray.Select(x => x * scalar2).ToArray();
                double[] tempframey = tempArray.Select(x => -Math.Log(scalar1 / Math.Sqrt(x)) - 0.29).ToArray();
                framey = framey.ArrayCopy(1, tempframey);

                // Assume outside diameter of casing is 10% larger than inside diameter of production pipe (=prodwelldiam)
                framey[0] = -Math.Log(scalar1 / Math.Sqrt(timeVector[1] * scalar2)) - 0.29;

                //#assume borehole thermal resistance negligible to rock thermal resistance
                scalar1 = sstParms.prodwellflowrate * cpwater;
                double[] rameyA = framey.Select(x => (scalar1 * x) / 2 / Math.PI / sstParms.krock).ToArray();

                // This code is only valid so far for 1 gradient and deviation = 0 !!!!!!!!   For multiple gradients, use Ramey's model for every layer
                ProdTempDrop = Tresoutput.Select(x => (Trock - x)).ToArray();
                tempArray = rameyA.Select(x => averagegradient * (sstParms.depth - x)).ToArray();
                ProdTempDrop = ProdTempDrop.Zip(tempArray, (x, y) => x - y).ToArray();
                tempArray = rameyA.Select(x => averagegradient * x).ToArray();
                tempArray = Tresoutput.Zip(tempArray, (x, y) => x - y).ToArray();
                tempArray = tempArray.Select(x => x - Trock).ToArray();
                var tempArray2 = rameyA.Select(x => Math.Exp(-sstParms.depth / x)).ToArray();
                tempArray = tempArray.Zip(tempArray2, (x, y) => x * y).ToArray();
                ProdTempDrop = ProdTempDrop.Zip(tempArray, (x, y) => x + y).ToArray();
                ProdTempDrop = ProdTempDrop.Select(X => X * (-1)).ToArray();
            }
            //ProducedTemperature = Tresoutput.Zip(ProdTempDrop, (x, y) => x - y).ToArray();
            ProducedTemperature = Tresoutput.SubtractArrays(ProdTempDrop);
            calcResult.ProdTempDrop = ProdTempDrop;

            // Redrilling
            if (sstParms.resoption < 5) // only applies to the built-in analytical reservoir models
            {
                double threshold = (1 - sstParms.maxdrawdown) * ProducedTemperature[0];
                var indexfirstmaxdrawdown = ProducedTemperature
                    .Select((temp, index) => new { temp, index })
                    .FirstOrDefault(item => item.temp < threshold)?.index ?? -1;
                if (indexfirstmaxdrawdown > 0)
                {
                    redrill = (int)Math.Floor((double)ProducedTemperature.Length / indexfirstmaxdrawdown);
                    double[] segment = ProducedTemperature.Take(indexfirstmaxdrawdown).ToArray();
                    double[] ProducedTemperatureRepeatead = Enumerable.Repeat(segment, redrill + 1).SelectMany(x => x).ToArray();
                    //var npProducedTemperatureRepeatead = np.tile((NDarray)ProducedTemperature[0..indexfirstmaxdrawdown], (redrill + 1));
                    //var ProducedTemperatureRepeatead = npProducedTemperatureRepeatead.GetData<double>();
                    ProducedTemperature = ProducedTemperatureRepeatead[0..ProducedTemperature.Length];
                }
            }
            calcResult.redrill = redrill;
            calcResult.ProducedTemperature = ProducedTemperature;
        }
    }
}
