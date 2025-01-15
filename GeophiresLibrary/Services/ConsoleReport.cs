using GeophiresLibrary.Core;
using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Services
{
    public class ConsoleReport : IReport
    {
        private StringBuilder reportBuilder;
        private int headerIndent = 25;

        public ConsoleReport()
        {
            reportBuilder = new StringBuilder();
        }

        public string GetReport()
        {
            return reportBuilder.ToString();
        }

        public void CapitalCostReport(CapitalAndOMCostParameters ccParms, SubsurfaceTechnicalParameters sstParms,
            FinancialParameters finParms, CalculatedResults calcResult, SimulationParameters simParms)
        {
            HeaderOutput("***CAPITAL COSTS (M$)***");
            if (ccParms.totalcapcostvalid == 0)
            {
                reportOutput($"    Drilling and completion costs                     {calcResult.Cwell}");
                reportOutput($"    Drilling and completion costs per well            {calcResult.Cwell / (sstParms.nprod + sstParms.ninj)}");
                reportOutput($"    Stimulation costs                                 {calcResult.Cstim}");
                reportOutput($"    Surface power plant costs                         {calcResult.Cplant}");
                reportOutput($"    Field gathering system costs                      {calcResult.Cgath}");
                if (ccParms.pipinglength > 0)
                    reportOutput($"    Transmission pipeline cost                        {calcResult.Cpiping}");
                reportOutput($"    Total surface equipment costs                     {calcResult.Cplant + calcResult.Cgath}");
                reportOutput($"    Exploration costs                                 {calcResult.Cexpl}");
            }

            if (ccParms.totalcapcostvalid == 1 && calcResult.redrill > 0)
            {
                reportOutput($"    Drilling and completion costs (for redrilling)    {calcResult.Cwell}");
                reportOutput($"    Drilling and completion costs per redrilled well  {calcResult.Cwell / (sstParms.nprod + sstParms.ninj)}");
                reportOutput($"    Stimulation costs (for redrilling)                {calcResult.Cstim}");
            }
            reportOutput($"    Total capital costs                               {calcResult.Ccap}");
            if (finParms.econmodel == 1)
            {
                reportOutput($"    Annualized capital costs                          {calcResult.Ccap * (1 + finParms.inflrateconstruction) * finParms.FCR}");
            }


        }

        public void EconomicReport(FinancialParameters finParms, SurfaceTechnicalParameters stParms)
        {
            HeaderOutput("***ECONOMIC PARAMETERS***");
            if (finParms.econmodel == 1)
            {
                reportOutput($"    Economic Model = Fixed Charge Rate (FCR) Model");
                reportOutput($"    Fixed Charge Rate (FCR) (%)                       " + (finParms.FCR * 100).ToString("F2"));
            }
            else if (finParms.econmodel == 2)
            {
                reportOutput($"    Economic Model = Standard Levelized Cost Model");
                reportOutput($"    Interest Rate (%)                                 {finParms.discountrate * 100}");
            }
            else if (finParms.econmodel == 3)
            {
                reportOutput($"    Economic Model  = BICYCLE Model");
            }
            reportOutput($"    Accrued financing during construction (%)         " + (finParms.inflrateconstruction * 100).ToString("F2"));
            reportOutput($"    Project lifetime (years)                         {finParms.plantlifetime}");
            reportOutput($"    Capacity factor (%)                              " + (stParms.utilfactor * 100).ToString("F1"));
        }

        public void EnergyGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms,
            CalculatedResults calcResult)
        {
            reportOutput("");
            reportOutput("                            ***************************************************************");
            reportOutput("                            *  HEAT AND/OR ELECTRICITY EXTRACTION AND GENERATION PROFILE  *");
            reportOutput("                            ***************************************************************");
            // Only electricity
            if (simParms.enduseoption == 1)
            {
                reportOutput(" YEAR      ELECTRICITY        HEAT           RESERVOIR           PERCENTAGE OF");
                reportOutput("            PROVIDED        EXTRACTED       HEAT CONTENT       TOTAL HEAT MINED");
                reportOutput("           (GWh/year)       (GWh/year)       (10^15 J)                (%)");
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    string year = String.Format("{0,5}", i + 1);
                    string electricityProvided = String.Format("{0,13:F1}", (calcResult.NetkWhProduced[i] / 1E6));
                    string heatExtracted = String.Format("{0,18:F1}", (calcResult.HeatkWhExtracted[i] / 1E6));
                    string reservoirHeatContent = String.Format("{0,17:F2}", (calcResult.RemainingReservoirHeatContent[i]));
                    string totalHeatMined = String.Format("{0,20:F2}", ((calcResult.InitialReservoirHeatContent - calcResult.RemainingReservoirHeatContent[i]) * 100 / calcResult.InitialReservoirHeatContent));
                    reportOutput(year + electricityProvided + heatExtracted + reservoirHeatContent + totalHeatMined);
                }
            }
            // Only direct-use    
            else if (simParms.enduseoption == 2)
            {
                reportOutput(" YEAR         HEAT            HEAT                RESERVOIR            PERCENTAGE OF");
                reportOutput("            PROVIDED        EXTRACTED            HEAT CONTENT        TOTAL HEAT MINED");
                reportOutput("           (GWh/year)       (GWh/year)            (10^15 J)                 (%)");
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    string year = String.Format("{0,5}", i + 1);
                    string heatProvided = String.Format("{0,13:F1}", (calcResult.HeatkWhProduced[i] / 1E6));
                    string heatExtracted = String.Format("{0,18:F1}", (calcResult.HeatkWhExtracted[i] / 1E6));
                    string reservoirHeatContent = String.Format("{0,17:F2}", (calcResult.RemainingReservoirHeatContent[i]));
                    string totalHeatMined = String.Format("{0,20:F2}", ((calcResult.InitialReservoirHeatContent - calcResult.RemainingReservoirHeatContent[i]) * 100 / calcResult.InitialReservoirHeatContent));
                    reportOutput(year + heatProvided + heatExtracted + reservoirHeatContent + totalHeatMined);
                }
            }
            // Both electricity and direct-use
            else if (simParms.enduseoption > 2)
            {
                reportOutput(" YEAR         HEAT          ELECTRICITY          HEAT            RESERVOIR          PERCENTAGE OF");
                reportOutput("            PROVIDED         PROVIDED          EXTRACTED        HEAT CONTENT      TOTAL HEAT MINED");
                reportOutput("           (GWh/year)       (GWh/year)         (GWh/year)        (10^15 J)               (%)");
                for (int i = 0; i < finParms.plantlifetime; i++)
                {
                    string year = String.Format("{0,5}", i + 1);
                    string heatProvided = String.Format("{0,13:F1}", (calcResult.HeatkWhProduced[i] / 1E6));
                    string heatExtracted = String.Format("{0,19:F1}", (calcResult.HeatkWhExtracted[i] / 1E6));
                    string electricityProvided = String.Format("{0,18:F1}", (calcResult.NetkWhProduced[i] / 1E6));
                    string reservoirHeatContent = String.Format("{0,17:F2}", (calcResult.RemainingReservoirHeatContent[i]));
                    string totalHeatMined = String.Format("{0,20:F2}", ((calcResult.InitialReservoirHeatContent - calcResult.RemainingReservoirHeatContent[i]) * 100 / calcResult.InitialReservoirHeatContent));
                    reportOutput(year + heatProvided + electricityProvided + heatExtracted + reservoirHeatContent + totalHeatMined);
                }

                //reportOutput('  YEAR               HEAT                   ELECTRICITY                  HEAT                RESERVOIR            PERCENTAGE OF\n')
                //reportOutput('                    PROVIDED                 PROVIDED                  EXTRACTED            HEAT CONTENT        TOTAL HEAT MINED\n')
                //reportOutput('                   (GWh/year)               (GWh/year)                 (GWh/year)            (10^15 J)                 (%)\n')
                //for i in range(0, plantlifetime):
                //    reportOutput('  {0:2.0f}              {1:8.1f}                 {2:8.1f}                    {3:8.2f}              {4:8.2f}               {5:8.2f}'.format(
                //    i + 1,
                //    HeatkWhProduced[i] / 1E6,
                //    NetkWhProduced[i] / 1E6,
                //    HeatkWhExtracted[i] / 1E6,
                //    RemainingReservoirHeatContent[i],
                //    (InitialReservoirHeatContent - RemainingReservoirHeatContent[i]) * 100 / InitialReservoirHeatContent) + '\n')
            }
        }

        public void EngineeringReport(SubsurfaceTechnicalParameters sstParms, SurfaceTechnicalParameters stParms,
            SimulationParameters simParms, CalculatedResults calcResult)
        {
            HeaderOutput("***ENGINEERING PARAMETERS***");
            reportOutput($"    Well depth (m)                                   " + sstParms.depth.ToString("F1"));
            reportOutput($"    Water loss rate (%)                              {sstParms.waterloss * 100}");
            reportOutput($"    Pump efficiency (%)                              {stParms.pumpeff * 100}");
            reportOutput($"    Injection temperature (deg.C)                    {sstParms.Tinj}");
            if (sstParms.rameyoptionprod == 1)
            {
                reportOutput($"    Production Wellbore heat transmission calculated with Ramey's model");
                reportOutput($"    Average production well temperature drop (deg.C) {Utilities.Average(calcResult.ProdTempDrop)}");
            }
            else if (sstParms.rameyoptionprod == 0)
            {
                reportOutput($"    User-provided production well temperature drop");
                reportOutput($"    Constant production well temperature drop (deg.C) {sstParms.tempdropprod}");
            }
            reportOutput($"    Flowrate per production well (kg/s)              {sstParms.prodwellflowrate}");
            reportOutput($"    Injection well casing ID (inches)                {sstParms.injwelldiam / 0.0254}");
            reportOutput($"    Produciton well casing ID (inches)               {sstParms.prodwelldiam / 0.0254}");
            reportOutput($"    Number of times redrilling                       {calcResult.redrill}");
            if (simParms.enduseoption == 1 || simParms.enduseoption > 2)
            {
                if (simParms.pptype == 1)
                {
                    reportOutput($"    Power plant type                                        Subcritical ORC");
                }
                else if (simParms.pptype == 2)
                {
                    reportOutput($"    Power plant type                                        Supercritical ORC");
                }
                else if (simParms.pptype == 3)
                {
                    reportOutput($"    Power plant type                                        Single-Flash");
                }
                else if (simParms.pptype == 4)
                {
                    reportOutput($"    Power plant type                                        Double-Flash");
                }
            }
            reportOutput($"");
        }

        public void OperatingMaintenanceCostReport(SimulationParameters simParms, CapitalAndOMCostParameters ccParms,
            CalculatedResults calcResult)
        {
            HeaderOutput("***OPERATING AND MAINTENANCE COSTS (M$/yr)***");
            if (ccParms.oamtotalfixedvalid == 0)
            {
                reportOutput($"    Wellfield maintenance costs                       {calcResult.Coamwell}");
                reportOutput($"    Power plant maintenance costs                     {calcResult.Coamplant}");
                reportOutput($"    Water costs                                       {calcResult.Coamwater}");
                if (simParms.enduseoption == 2)
                {
                    reportOutput($"    Average annual pumping costs                      {calcResult.averageannualpumpingcosts}");
                    reportOutput($"    Total operating and maintenance costs             {calcResult.Coam + calcResult.averageannualpumpingcosts}");
                }
                else
                {
                    reportOutput($"    Total operating and maintenance costs             {calcResult.Coam}");
                }

            }
        }

        public void PowerGenerationProfileReport(SimulationParameters simParms, FinancialParameters finParms,
            CalculatedResults calcResult)
        {
            reportOutput($"");
            reportOutput($"                                ******************************");
            reportOutput($"                                *  POWER GENERATION PROFILE  *");
            reportOutput($"                                ******************************");
            if (simParms.enduseoption == 1)   //only electricity
            {
                reportOutput($" YEAR       THERMAL         GEOFLUID          PUMP              NET           FIRST LAW");
                reportOutput($"            DRAWDOWN       TEMPERATURE        POWER            POWER          EFFICIENCY");
                reportOutput($"                             (deg C)          (MWe)            (MWe)              (%)");
                for (int i = 0; i < finParms.plantlifetime + 1; i++)
                {
                    string year = String.Format("{0,5}", i);
                    string thermalDrawdown = String.Format("{0,13:F4}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear] / calcResult.ProducedTemperature[0]));
                    string geofluidTemprature = String.Format("{0,18:F2}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear]));
                    string pumpPower = String.Format("{0,15:F4}", (calcResult.PumpingPower[i * simParms.timestepsperyear]));
                    string netPower = String.Format("{0,14:F4}", (calcResult.NetElectricityProduced[i * simParms.timestepsperyear]));
                    string firstLaw = String.Format("{0,17:F4}", ((calcResult.FirstLawEfficiency[i * simParms.timestepsperyear] * 100)));
                    reportOutput(year + thermalDrawdown + geofluidTemprature + pumpPower + netPower + firstLaw);
                }

            }
            // Only direct-use
            else if (simParms.enduseoption == 2)
            {
                reportOutput($" YEAR       THERMAL         GEOFLUID          PUMP              NET");
                reportOutput($"            DRAWDOWN       TEMPERATURE        POWER             HEAT");
                reportOutput($"                             (deg C)          (MWe)            (MWth)");
                for (int i = 0; i < finParms.plantlifetime + 1; i++)
                {
                    string year = String.Format("{0,5}", i);
                    string thermalDrawdown = String.Format("{0,13:F4}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear] / calcResult.ProducedTemperature[0]));
                    string geofluidTemprature = String.Format("{0,18:F2}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear]));
                    string pumpPower = String.Format("{0,15:F4}", (calcResult.PumpingPower[i * simParms.timestepsperyear]));
                    string netPower = String.Format("{0,17:F4}", (calcResult.HeatProduced[i * simParms.timestepsperyear]));
                    reportOutput(year + thermalDrawdown + geofluidTemprature + pumpPower + netPower);
                }

            }
            // Both electricity and direct-use
            else if (simParms.enduseoption > 2)
            {
                reportOutput($" YEAR       THERMAL         GEOFLUID          PUMP         NET          NET          FIRST LAW");
                reportOutput($"            DRAWDOWN       TEMPERATURE        POWER        POWER        HEAT         EFFICIENCY");
                reportOutput($"                             (deg C)          (MWe)        (MWe)        (MWth)          (%)");
                for (int i = 0; i < finParms.plantlifetime + 1; i++)
                {
                    string year = String.Format("{0,5}", i);
                    string thermalDrawdown = String.Format("{0,13:F4}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear] / calcResult.ProducedTemperature[0]));
                    string geofluidTemprature = String.Format("{0,18:F2}", (calcResult.ProducedTemperature[i * simParms.timestepsperyear]));
                    string pumpPower = String.Format("{0,15:F4}", (calcResult.PumpingPower[i * simParms.timestepsperyear]));
                    string netPower = String.Format("{0,14:F4}", (calcResult.NetElectricityProduced[i * simParms.timestepsperyear]));
                    string netHeat = String.Format("{0,13:F4}", (calcResult.HeatProduced[i * simParms.timestepsperyear]));
                    string firstLaw = String.Format("{0,15:F4}", ((calcResult.FirstLawEfficiency[i * simParms.timestepsperyear] * 100)));
                    reportOutput(year + thermalDrawdown + geofluidTemprature + pumpPower + netPower + netHeat + firstLaw);
                }
            }
            reportOutput("");
        }

        public void PowerGenerationReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms,
            CalculatedResults calcResult)
        {
            HeaderOutput("***POWER GENERATION RESULTS***");
            //reportOutput($"");
            //reportOutput($"");
            //reportOutput($"                         ***POWER GENERATION RESULTS***");
            //reportOutput($"");
            if (simParms.enduseoption == 1 || simParms.enduseoption > 2) //there is electricity component
            {
                reportOutput($"    Initial geofluid availability (MWe/(kg/s)         {calcResult.Availability[0]}");
                reportOutput($"    Initial net power generation (MWe)                {calcResult.NetElectricityProduced[0]}");
                reportOutput($"    Average net power generation (MWe)                {Utilities.Average(calcResult.NetElectricityProduced)}");
                reportOutput($"    Initial pumping power/net installed power (%)     {calcResult.PumpingPower[0] / calcResult.NetElectricityProduced[0] * 100}");
                reportOutput($"    Average Annual Net Electricity Generation (GWh/yr) {Utilities.Average(calcResult.NetkWhProduced) / 1E6}");
            }
            if (simParms.enduseoption > 1) //there is direct-use component
            {
                reportOutput($"    Initial direct-use heat production (MWth)         {calcResult.HeatProduced[0]}");
                reportOutput($"    Average direct-use heat production (MWth)         {Utilities.Average(calcResult.HeatProduced)}");
                reportOutput($"    Average annual heat production (GWh/yr)           {Utilities.Average(calcResult.HeatkWhProduced) / 1E6}");
            }
            if (sstParms.impedancemodelused == 1)
            {
                reportOutput($"    Average total geofluid pressure drop (kPa)        {Utilities.Average(calcResult.DP)}");
                reportOutput($"    Average injection well pressure drop (kPa)        {Utilities.Average(calcResult.DP1)}");
                reportOutput($"    Average reservoir pressure drop (kPa)             {Utilities.Average(calcResult.DP2)}");
                reportOutput($"    Average production well pressure drop (kPa)       {Utilities.Average(calcResult.DP3)}");
                reportOutput($"    Average buoyancy correction (kPa)                 {Utilities.Average(calcResult.DP4)}");
            }
            else
            {
                reportOutput($"    Average injection well pump pressure drop (kPa)       {Utilities.Average(calcResult.DP1)}");
                if (sstParms.productionwellpumping == 1)
                {
                    reportOutput($"    Average production well pump pressure drop (kPa)      {Utilities.Average(calcResult.DP3)}");
                }

            }
        }

        public void ReservoirReport(SubsurfaceTechnicalParameters sstParms, CalculatedResults calcResult)
        {
            HeaderOutput("***RESERVOIR PARAMETERS***");
            if (sstParms.resoption == 1)
            {
                reportOutput($"    Reservoir Model = Multiple Parallel Fractures Model");
            }
            else if (sstParms.resoption == 2)
            {
                reportOutput($"    Reservoir Model = 1-D Linear Heat Sweep Model");
            }
            else if (sstParms.resoption == 3)
            {
                reportOutput($"    Reservoir Model = Single Fracture m/A Thermal Drawdown Model");
                reportOutput($"    m/A Drawdown Parameter (kg/s/m^2)                       " + sstParms.drawdp.ToString("F5"));
            }
            else if (sstParms.resoption == 4)
            {
                reportOutput($"    Reservoir Model = Annual Percentage Thermal Drawdown Model");
                reportOutput($"    Annual Thermal Drawdown (%/year)                        {sstParms.drawdp * 100}");
            }
            else if (sstParms.resoption == 5)
            {
                reportOutput($"    Reservoir Model = User-Provided Temperature Profile");
            }
            else if (sstParms.resoption == 6)
            {
                reportOutput($"    Reservoir Model = TOUGH2 Simulator");
            }

            reportOutput($"    Bottom-hole temperature (deg.C)                   {calcResult.Trock}");
            if (sstParms.resoption > 3)
            {
                reportOutput($"    Warning: the reservoir dimensions and thermo-physical properties ");
                reportOutput($"             listed below are default values if not provided by the user.   ");
                reportOutput($"             They are only used for calculating remaining heat content.  ");
            }

            if (sstParms.resoption == 1 || sstParms.resoption == 2)
            {
                if (sstParms.fracshape == 1)
                {
                    reportOutput($"    Fracture model = circular fracture with known area ");
                    reportOutput($"    Well seperation = fracture diameter (m)           {sstParms.fracheight}");
                }
                else if (sstParms.fracshape == 2)
                {
                    reportOutput($"    Fracture model = circular fracture with known diameter");
                    reportOutput($"    Well seperation = fracture diameter (m)           {sstParms.fracheight}");
                }
                else if (sstParms.fracshape == 3)
                {
                    reportOutput($"    Fracture model = square fracture with known fracture height");
                    reportOutput($"    Well seperation = fracture height (m)             {sstParms.fracheight}");
                }
                else if (sstParms.fracshape == 4)
                {
                    reportOutput($"    Fracture model = rectangular fracture with known fracture height and width");
                    reportOutput($"    Well seperation = fracture height (m)             {sstParms.fracheight}");
                    reportOutput($"    Fracture width (m)                                {sstParms.fracwidth}");
                }
                reportOutput($"    Fracture area (m^2)                            {sstParms.fracarea}");
            }
            if (sstParms.resvoloption == 1)
            {
                reportOutput($"    Reservoir volume calculated with fracture separation and number of fractures as input");
            }
            else if (sstParms.resvoloption == 2)
            {
                reportOutput($"    Number of fractures calculated with reservoir volume and fracture separation as input");
            }
            else if (sstParms.resvoloption == 3)
            {
                reportOutput($"    Fracture separation calculated with reservoir volume and number of fractures as input");
            }
            else if (sstParms.resvoloption == 4)
            {
                reportOutput($"    Reservoir volume provided as input");
            }
            if (new[] { 1, 2, 3 }.Contains(sstParms.resvoloption))
            {
                reportOutput($"    Number of fractures                               {sstParms.fracnumb}");
                reportOutput($"    Fracture separation (m)                           {sstParms.fracsep}");
            }

            reportOutput($"    Reservoir volume (m^3)                         {sstParms.resvol}");
            if (sstParms.impedancemodelused == 1)
            {
                reportOutput($"    Reservoir impedance (GPa/m^3/s)                   {sstParms.impedance / 1000}");
            }
            else
            {
                reportOutput($"    Reservoir hydrostatic pressure (kPa)              {sstParms.Phydrostatic}");
                reportOutput($"    Plant outlet pressure (kPa)                       {sstParms.Pplantoutlet}");
                if (sstParms.productionwellpumping == 1)
                {
                    reportOutput($"    Production wellhead pressure (kPa)                  {calcResult.Pprodwellhead}");
                    reportOutput($"    Productivity Index (kg/s/bar)                       {sstParms.PI}");
                }
                reportOutput($"    Injectivity Index (kg/s/bar)                      {sstParms.II}");
            }

            reportOutput($"    Reservoir density (kg/m^3)                        {sstParms.rhorock}");
            if (new[] { 1, 2, 3, 6 }.Contains(sstParms.resvoloption) || sstParms.rameyoptionprod == 1)
            {
                reportOutput($"    Reservoir thermal conductivity (W/m/K)            {sstParms.krock}");
            }

            reportOutput($"    Reservoir heat capacity (J/kg/K)                  {sstParms.cprock}");
            if (sstParms.resoption == 2 || (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1))
            {
                reportOutput($"    Reservoir porosity (%)                            {sstParms.porrock * 100}");
            }

            if (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1)
            {
                reportOutput($"    Reservoir permeability (m^2)                      {sstParms.permrock}");
                reportOutput($"    Reservoir thickness (m)                           {sstParms.resthickness}");
                reportOutput($"    Reservoir width (m)                               {sstParms.reswidth}");
                reportOutput($"    Well separation (m)                               {sstParms.wellsep}");
            }
        }

        public void ResourceCharacteristicsReport(SubsurfaceTechnicalParameters sstParms)
        {
            HeaderOutput("***RESOURCE CHARACTERISTICS***");
            reportOutput($"    Maximum reservoir temperature (deg.C)            {sstParms.Tmax}");
            reportOutput($"    Number of segments                               {sstParms.numseg}");
            if (sstParms.numseg == 1)
            {
                reportOutput($"    Geothermal gradient (deg.C/km)                   {(sstParms.gradient[0] * 1E3)}");
            }
            else if (sstParms.numseg == 2)
            {
                reportOutput($"    Segment 1 geothermal gradient (deg.C/km)         {sstParms.gradient[0] * 1E3}");
                reportOutput($"    Segment 1 thickness (km)                         {sstParms.layerthickness[0] / 1E3}");
                reportOutput($"    Segment 2 geothermal gradient (deg.C/km)         {sstParms.gradient[1] * 1E3}");
            }
            else if (sstParms.numseg == 3)
            {
                //    reportOutput($"      Segment 1 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[0] * 1E3)) + '\n')
                //    reportOutput($"      Segment 1 thickness (km)                       ' + "{0:10.0f}".format((layerthickness[0] / 1E3)) + '\n')
                //    reportOutput($"      Segment 2 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[1] * 1E3)) + '\n')
                //    reportOutput($"      Segment 2 thickness (km)                       ' + "{0:10.0f}".format((layerthickness[1] / 1E3)) + '\n')
                //    reportOutput($"      Segment 3 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[2] * 1E3)) + '\n')
            }
            else if (sstParms.numseg == 4)
            {
                //    reportOutput($"      Segment 1 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[0] * 1E3)) + '\n')
                //    reportOutput($"      Segment 1 thickness (km)                       ' + "{0:10.0f}".format((layerthickness[0] / 1E3)) + '\n')
                //    reportOutput($"      Segment 2 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[1] * 1E3)) + '\n')
                //    reportOutput($"      Segment 2 thickness (km)                       ' + "{0:10.0f}".format((layerthickness[1] / 1E3)) + '\n')
                //    reportOutput($"      Segment 3 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[2] * 1E3)) + '\n')
                //    reportOutput($"      Segment 3 thickness (km)                       ' + "{0:10.0f}".format((layerthickness[2] / 1E3)) + '\n')
                //    reportOutput($"      Segment 4 geothermal gradient (deg.C/km)         ' + "{0:10.1f}".format((gradient[3] * 1E3)) + '\n')
            }
        }

        public void SummaryReport(SimulationParameters simParms, SubsurfaceTechnicalParameters sstParms,
            CalculatedResults calcResult)
        {
            MainHeaderOutput();
            HeaderOutput("***SUMMARY OF RESULTS***");
            if (simParms.enduseoption == 1)
            {
                reportOutput("    End-Use Option = Electricity");
            }
            else if (simParms.enduseoption == 2)
            {
                reportOutput("    End-Use Option = Direct-Use Heat");
            }
            else if (simParms.enduseoption == 31) //topping cycle
            {
                reportOutput("    End-Use Option = Cogeneration Topping Cycle");
                reportOutput("    Heat sales considered as extra income");
            }
            else if (simParms.enduseoption == 32) //topping cycle
            {
                reportOutput("    End-Use Option = Cogeneration Topping Cycle");
                reportOutput("    Electricity sales considered as extra income");
            }
            else if (simParms.enduseoption == 41) //bottoming cycle
            {
                reportOutput("    End-Use Option = Cogeneration Bottoming Cycle");
                reportOutput("    Heat Sales considered as extra income");
            }
            else if (simParms.enduseoption == 42) //bottoming cycle
            {
                reportOutput("    End-Use Option = Cogeneration Bottoming Cycle");
                reportOutput("    Electricity sales considered as extra income");
            }
            else if (simParms.enduseoption == 51) //cogen split of mass flow rate
            {
                reportOutput("    End-Use Option = Cogeneration Parallel Cycle");
                reportOutput("    Heat sales considered as extra income");
            }
            else if (simParms.enduseoption == 52) //cogen split of mass flow rate
            {
                reportOutput("    End-Use Option = Cogeneration Parallel Cycle");
                reportOutput("    Electricity sales considered as extra income");
            }
            if (simParms.enduseoption == 1 || simParms.enduseoption > 2)    //there is an electricity component
            {
                reportOutput($"    Average Net Electricity Production (MWe)          {(Utilities.Average(calcResult.NetElectricityProduced)).ToString("F2")}");
            }

            if (simParms.enduseoption > 1)    //there is a direct-use component
            {
                reportOutput($"      Average Direct-Use Heat Production (MWth)         {Utilities.Average(calcResult.HeatProduced)}");
            }

            if ((simParms.enduseoption % 10) == 1)
                reportOutput($"    Electricity breakeven price (cents/kWh)           " + calcResult.Price.ToString("F2"));
            else if ((simParms.enduseoption % 10) == 2)    //levelized cost expressed as LCOH
                reportOutput("    Direct-Use heat breakeven price ($/MMBTU)          " + calcResult.Price.ToString("F2"));

            reportOutput($"    Number of production wells                        {sstParms.nprod}");
            reportOutput($"    Number of injection wells                         {sstParms.ninj}");
            reportOutput($"    Flowrate per production well (kg/s)               " + sstParms.prodwellflowrate.ToString("F1"));
            reportOutput($"    Well depth (m)                                    " + sstParms.depth.ToString("F1"));
            if (sstParms.numseg == 1)
            {
                reportOutput($"    Geothermal gradient (deg.C/km)                    " + (sstParms.gradient[0] * 1E3).ToString("F1"));
            }
            else if (sstParms.numseg == 2)
            {
                reportOutput($"    Segment 1 geothermal gradient (deg.C/km)       {sstParms.gradient[0] * 1E3}");
                reportOutput($"    Segment 1 thickness (km)                       {sstParms.layerthickness[0] / 1E3}");
                reportOutput($"    Segment 2 geothermal gradient (deg.C/km)       {sstParms.gradient[1] * 1E3}");
            }
            else if (sstParms.numseg == 3)
            {
                reportOutput($"    Segment 1 geothermal gradient (deg.C/km)        {sstParms.gradient[0] * 1E3}");
                reportOutput($"    Segment 1 thickness (km)                        {sstParms.layerthickness[0] / 1E3}");
                reportOutput($"    Segment 2 geothermal gradient (deg.C/km)        {sstParms.gradient[1] * 1E3}");
                reportOutput($"    Segment 2 thickness (km)                        {sstParms.layerthickness[1] / 1E3}");
                reportOutput($"    Segment 3 geothermal gradient (deg.C/km)        {sstParms.gradient[2] * 1E3}");
            }
            else if (sstParms.numseg == 4)
            {
                reportOutput($"    Segment 1 geothermal gradient (deg.C/km)        {sstParms.gradient[0] * 1E3}");
                reportOutput($"    Segment 1 thickness (km)                        {sstParms.layerthickness[0] / 1E3}");
                reportOutput($"    Segment 2 geothermal gradient (deg.C/km)        {sstParms.gradient[1] * 1E3}");
                reportOutput($"    Segment 2 thickness (km)                        {sstParms.layerthickness[1] / 1E3}");
                reportOutput($"    Segment 3 geothermal gradient (deg.C/km)        {sstParms.gradient[2] * 1E3}");
                reportOutput($"    Segment 3 thickness (km)                        {sstParms.layerthickness[2] / 1E3}");
                reportOutput($"    Segment 4 geothermal gradient (deg.C/km)        {sstParms.gradient[3] * 1E3}");

            }
        }

        private void reportOutput(string output)
        {
            reportBuilder.Append(output);
            reportBuilder.AppendLine();
        }

        private void MainHeaderOutput()
        {
            string text = "*****************";
            string lineOutput = $"{new string(' ', headerIndent)}{text,-40}";
            reportOutput(lineOutput);

            text = "***CASE REPORT***";
            lineOutput = $"{new string(' ', headerIndent)}{text,-40}";
            reportOutput(lineOutput);

            text = "*****************";
            lineOutput = $"{new string(' ', headerIndent)}{text,-40}";
            reportOutput(lineOutput);
            reportOutput("");
        }

        private void HeaderOutput(string text)
        {
            reportOutput("");
            string lineOutput = $"{new string(' ', headerIndent)}{text,-40}";
            reportOutput(lineOutput);
            reportOutput("");
        }
    }
}
