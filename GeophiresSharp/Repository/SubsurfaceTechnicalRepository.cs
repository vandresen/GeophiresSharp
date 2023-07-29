using GeophiresSharp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using GeophiresSharp.Extensions;

namespace GeophiresSharp.Repository
{
    public class SubsurfaceTechnicalRepository : ISubsurfaceTechnicalRepository
    {
        private readonly string[] _content;

        public SubsurfaceTechnicalRepository(string[] content)
        {
            _content = content;
        }

        public async Task<SubsurfaceTechnicalParameters> GetSubsurfaceTechnicalParameters(SimulationParameters simulationParms)
        {
            var sstParms = new SubsurfaceTechnicalParameters();

            //resoption: Reservoir Option
            //resoption = 1  Multiple parallel fractures model (LANL)                            
            //resoption = 2  Volumetric block model (1D linear heat sweep model (Stanford))                       
            //resoption = 3  Drawdown parameter model (Tester)
            //resoption = 4  Thermal drawdown percentage model (GETEM)
            //resoption = 5  Generic user-provided temperature profile
            //resoption = 6  TOUGH2 is called
            List<int> valid_resoption = new List<int>() { 1, 2, 3, 4, 5, 6 };
            int resoption = _content.GetIntParameter("Reservoir Model,", 4, valid_resoption);
            sstParms.resoption = resoption;

            //drawdp: Drawdown parameter
            //used in both resopt 3 and 4
            //if resoption = 3: drawdp is in units of kg/s/m2
            //if resoption = 4: drawdp is in units of 1/year
            double drawdp = 0.0;
            if (sstParms.resoption == 3)
            {
                drawdp = _content.GetDoubleParameter("Drawdown Parameter,", 0.0001, 0.0, 0.2);
            }
            if (sstParms.resoption == 4)
            {
                drawdp = _content.GetDoubleParameter("Drawdown Parameter,", 0.005, 0.0, 0.2);
            }
            sstParms.drawdp = drawdp;

            //read file name of reservoir output in case reservoir model 5 is selected
            string filenamereservoiroutput = "";
            if (sstParms.resoption == 5)
            {

                filenamereservoiroutput = _content.GetStringFromContent("Reservoir Output File Name,");
            }
            sstParms.filenamereservoiroutput = filenamereservoiroutput;

            string tough2modelfilename = "";
            int usebuiltintough2model = 0;
            if (sstParms.resoption == 6)
            {
                tough2modelfilename = "Doublet";
                tough2modelfilename = _content.GetStringFromContent("TOUGH2 Model/File Name,");
                if (tough2modelfilename == "Doublet") usebuiltintough2model = 1;
                else usebuiltintough2model = 0;
            }
            sstParms.tough2modelfilename = tough2modelfilename;
            sstParms.usebuiltintough2model = usebuiltintough2model;

            //depth: Measured depth of the well (provided in km by user and converted here to m).
            double depth = _content.GetDoubleParameter("Reservoir Depth,", 3.0, 0.1, 15.0);
            depth = depth * 1000.0;
            sstParms.depth = depth;

            //numseg: number of segments
            List<int> valid_numseg = new List<int>() { 1, 2, 3, 4 };
            int numseg = _content.GetIntParameter("Number of Segments,", 1, valid_numseg);
            sstParms.numseg = numseg;

            //gradient(i): geothermal gradient of layer i (provided in C/km and converted to C/m)
            //layerthickness(i): thickness of layer i (provided in km and converted to m)
            double[] gradient = new double[] { 0, 0, 0, 0 };
            double[] layerthickness = new double[] { 0, 0, 0, 0 };
            sstParms.gradient = new double[] { 0, 0, 0, 0 };
            sstParms.layerthickness = new double[] { 0, 0, 0, 0 };
            gradient[0] = _content.GetDoubleParameter("Gradient 1,", 50.0, 0.0 * 1000, 0.5 * 1000);
            gradient[0] = gradient[0] / 1000;

            if (sstParms.numseg > 1)
            {
                gradient[1] = _content.GetDoubleParameter("Gradient 2,", 50.0, 0.0 * 1000, 0.5 * 1000);
                gradient[1] = gradient[1] / 1000;
                layerthickness[0] = _content.GetDoubleParameter("Thickness 1,", 2.0, 10.0 / 1000, 100000 / 1000);
                layerthickness[0] = layerthickness[0] * 1000;
            }

            if (sstParms.numseg > 2)
            {
                gradient[2] = _content.GetDoubleParameter("Gradient 3,", 50.0, 0.0 * 1000, 0.5 * 1000);
                gradient[2] = gradient[2] / 1000;
                layerthickness[1] = _content.GetDoubleParameter("Thickness 2,", 2.0, 10.0 / 1000, 100000 / 1000);
                layerthickness[1] = layerthickness[1] * 1000;
            }

            if (sstParms.numseg > 3)
            {
                gradient[3] = _content.GetDoubleParameter("Gradient 4,", 50.0, 0.0 * 1000, 0.5 * 1000);
                gradient[3] = gradient[3] / 1000;
                layerthickness[2] = _content.GetDoubleParameter("Thickness 3,", 2.0, 10.0 / 1000, 100000 / 1000);
                layerthickness[2] = layerthickness[2] * 1000;
            }

            //set thickness of bottom segment to large number to override lower, unused segments
            layerthickness[sstParms.numseg - 1] = 100000;

            //convert 0 C/m gradients to very small number, avoids divide by zero errors later
            for (int i = 0; i < 4; i++)
            {
                if (gradient[i] == 0.0) gradient[i] = 1e-6;
            }
            Array.Copy(gradient, 0, sstParms.gradient, 0, gradient.Length);
            Array.Copy(layerthickness, 0, sstParms.layerthickness, 0, layerthickness.Length);

            //Tmax: Maximum allowable Reservoir Temperature (C)
            double Tmax = _content.GetDoubleParameter("Maximum Temperature,", 400.0, 50.0, 1000.0);
            sstParms.Tmax = Tmax;

            //nprod: number of production wells
            //ninj: number of injection wells
            List<int> valid_nprod = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            int nprod = _content.GetIntParameter("Number of Production Wells,", 2, valid_nprod);
            sstParms.nprod = nprod;
            List<int> valid_ninj = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            int ninj = _content.GetIntParameter("Number of Injection Wells,", 2, valid_ninj);
            sstParms.ninj = ninj;

            //prodwelldiam: production well diameter (input as inch and converted to m)
            //injwelldiam: injection well diameter (input as inch and converted to m)
            double prodwelldiam = _content.GetDoubleParameter("Production Well Diameter,", 8, 1.0, 30.0);
            prodwelldiam = prodwelldiam * 0.0254;
            sstParms.prodwelldiam = prodwelldiam;
            double injwelldiam = _content.GetDoubleParameter("Injection Well Diameter,", 8, 1.0, 30.0);
            injwelldiam = injwelldiam * 0.0254;
            sstParms.injwelldiam = injwelldiam;

            //rameyoptionprod = 0: use tempdrop to calculate production well temperature drop
            //rameyoptionprod = 1: use Ramey model to calculate production well temperature drop
            List<int> valid_rameyoptionprod = new List<int>() { 0, 1 };
            int rameyoptionprod = _content.GetIntParameter("Ramey Production Wellbore Model,", 1, valid_rameyoptionprod);
            sstParms.rameyoptionprod = rameyoptionprod;

            //tempdropprod: temperature drop in production well in deg. C (if Ramey model is not used)
            double tempdropprod = 5.0;
            if (rameyoptionprod == 0)
            {
                tempdropprod = _content.GetDoubleParameter("Production Wellbore Temperature Drop,", 5.0, -5.0, 50.0);
            }
            sstParms.tempdropprod = tempdropprod;
            double tempgaininj = _content.GetDoubleParameter("Injection Wellbore Temperature Gain,", 0.0, -5.0, 50.0);
            sstParms.tempgaininj = tempgaininj;

            //prodwellflowrate: flow rate per production well (kg/s)
            double prodwellflowrate = _content.GetDoubleParameter("Production Flow Rate per Well,", 50.0, 1.0, 500.0);
            sstParms.prodwellflowrate = prodwellflowrate;

            //resvoloption: Rock mass volume option
            //resvoloption = 1  Specify fracnumb, fracsep                                 
            //resvoloption = 2  specify resvol, fracsep                                
            //resvoloption = 3  Specify resvol, fracnumb
            //resvoloption = 4: Specify resvol only (sufficient for reservoir models 3, 4, 5 and 6)
            List<int> valid_resvoloption = new List<int>() { 1, 2, 3, 4 };
            int resvoloption = _content.GetIntParameter("Reservoir Volume Option,", -1, valid_resvoloption);
            if (resvoloption == -1)
            {
                if (sstParms.resoption == 1 || sstParms.resoption == 2)
                {
                    resvoloption = 3;
                }
                else
                {
                    resvoloption = 4;
                }
            }
            if (new[] { 1, 2 }.Contains(sstParms.resoption) && resvoloption == 4)
            {
                resvoloption = 3;
                Console.WriteLine("Warning: If user-selected reservoir model is 1 or 2, then user-selected reservoir volume option cannot be 4 but should  be 1, 2, or 3. GEOPHIRES will assume reservoir volume option 3.");
            }

            sstParms.resvoloption = resvoloption;

            double fracarea = 0.0;
            double fracheight = 0.0;
            double fracwidth = 0.0;
            int fracshape = 1;
            //the first two reservoir models require fracture geometry
            if (new[] { 1, 2 }.Contains(sstParms.resoption) || new[] { 1, 2, 3 }.Contains(sstParms.resvoloption))
            {
                //fracshape: Shape of fractures
                //fracshape = 1  Circular fracture with known area                                  
                //fracshape = 2  Circular fracture with known diameter                                   
                //fracshape = 3  Square fracture
                //fracshape = 4  Rectangular fracture
                List<int> valid_fracshape = new List<int>() { 1, 2, 3, 4 };
                fracshape = _content.GetIntParameter("Fracture Shape,", 1, valid_fracshape);

                //fracarea: Effective heat transfer area per fracture (m2) (required if fracshape = 1)
                if (fracshape == 1)
                {
                    fracarea = _content.GetDoubleParameter("Fracture Area,", 250000.0, 1.0, 100000000.0);
                }

                //fracheight: Height of fracture = well separation (m)
                if (new[] { 2, 3, 4 }.Contains(fracshape))
                {
                    fracheight = _content.GetDoubleParameter("Fracture Height,", 500.0, 1.0, 10000.0);
                }

                //fracwidth: Width of fracture (m)
                if (fracshape == 4)
                {
                    fracwidth = _content.GetDoubleParameter("Fracture Width,", 500.0, 1.0, 10000.0);
                }

                //calculate fracture geometry:
                //fracshape = 1: calculate diameter of circular fracture
                //fracshape = 2: calculate area of circular fracture
                //fracshape = 3: calculate area of square fracture
                //fracshape = 4: calculate area of rectangular fracture
                if (fracshape == 1)
                {
                    fracheight = Math.Sqrt(4 / Math.PI * fracarea);
                    fracwidth = fracheight;
                }
                else if (fracshape == 2)
                {
                    fracwidth = fracheight;
                    fracarea = Math.PI / 4 * fracheight * fracheight;
                }
                else if (fracshape == 3)
                {
                    fracwidth = fracheight;
                    fracarea = fracheight * fracwidth;
                }
                else if (fracshape == 4)
                {
                    fracarea = fracheight * fracwidth;
                }
            }
            sstParms.fracshape = fracshape;
            sstParms.fracheight = fracheight;
            sstParms.fracwidth = fracwidth;
            sstParms.fracarea = fracarea;

            double resvol = 0.0;
            double fracsep = 0.0;
            double fracnumb = 10;
            //fracnumb: number of fractures
            if (new[] { 1, 3 }.Contains(sstParms.resvoloption))
            {
                List<int> valid_fracnumb = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
                int intfracnumb = _content.GetIntParameter("Number of Fractures,", 10, valid_fracnumb);
                fracnumb = intfracnumb;
            }

            //fracsep: fracture separation [m]
            if (new[] { 1, 2 }.Contains(sstParms.resvoloption))
            {
                ;
                fracsep = _content.GetDoubleParameter("Fracture Separation,", 50.0, 1.0, 10000.0);
            }

            //resvol: reservoir volume [m^3]
            if (new[] { 2, 3, 4 }.Contains(sstParms.resvoloption))
            {
                resvol = _content.GetDoubleParameter("Reservoir Volume,", 500.0 * 500 * 500, 10.0, 10000.0 * 10000.0 * 10000.0);
            }

            //calculate reservoir geometry:
            //resvoloption = 1: calculate volume of fractured rock mass
            //resvoloption = 2: calculate number of fractures
            //resvoloption = 3: calculate fracture separation
            if (sstParms.resvoloption == 1)
            {
                resvol = (fracnumb - 1) * sstParms.fracarea * fracsep;
            }
            else if (sstParms.resvoloption == 2)
            {
                fracnumb = resvol / sstParms.fracarea / fracsep + 1;
            }
            else if (sstParms.resvoloption == 3)
            {
                fracsep = resvol / sstParms.fracarea / (fracnumb - 1);
            }
            sstParms.resvol = resvol;
            sstParms.fracnumb = fracnumb;
            sstParms.fracsep = fracsep;

            //waterloss: fraction of water lost = (total geofluid lost)/(total geofluid produced)
            double waterloss = _content.GetDoubleParameter("Water Loss Fraction,", 0.0, 0.0, 0.99);
            sstParms.waterloss = waterloss;

            int impedancemodelallowed = 1;
            int productionwellpumping = 1;
            int setinjectionpressurefixed = 0;
            if (simulationParms.enduseoption == 1)
            {
                //simple single- or double-flash power plant assumes no production well pumping
                if (new[] { 3, 4 }.Contains(simulationParms.pptype))
                {
                    impedancemodelallowed = 0;
                    productionwellpumping = 0;
                    setinjectionpressurefixed = 1;
                }
            }
            else if (new[] { 31, 32 }.Contains(simulationParms.enduseoption))
            {
                if (new[] { 3, 4 }.Contains(simulationParms.pptype))
                {
                    impedancemodelallowed = 0;
                    productionwellpumping = 0;
                    setinjectionpressurefixed = 1;
                }
            }
            else if (new[] { 41, 42 }.Contains(simulationParms.enduseoption))
            {
                if (new[] { 3, 4 }.Contains(simulationParms.pptype))
                {
                    impedancemodelallowed = 0;
                    setinjectionpressurefixed = 1;
                }
            }
            else if (new[] { 51, 52 }.Contains(simulationParms.enduseoption))
            {
                if (new[] { 3, 4 }.Contains(simulationParms.pptype))
                {
                    impedancemodelallowed = 0;
                    setinjectionpressurefixed = 1;
                }
            }

            double impedancemodelused = 0.0;
            double impedance = 0.1 * 1E6 / 1E3;
            if (impedancemodelallowed == 1)
            {
                //impedance: impedance per wellpair (input as GPa*s/m^3 and converted to KPa/kg/s (assuming 1000 for density; density will be corrected for later))
                if (Array.Exists(_content, element => element.Contains("Reservoir Impedance,")))
                {
                    impedance = _content.GetDoubleParameter("Reservoir Impedance,", 0.1, 0.0001, 10000.0);
                    impedance = impedance * 1E6 / 1E3;
                    impedancemodelused = 1;
                }
            }
            sstParms.impedance = impedance;
            sstParms.impedancemodelused = impedancemodelused;
            sstParms.productionwellpumping = productionwellpumping;

            double Phydrostatic = -1.0;
            double ppwellhead = -1;
            int usebuiltinhydrostaticpressurecorrelation = 0;
            int usebuiltinppwellheadcorrelation = 0;
            double PI = 10;
            double II = 10;
            double Pplantoutlet = -1.0;
            int usebuiltinoutletplantcorrelation = 0;
            if (impedancemodelallowed == 0 || impedancemodelused == 0)
            {
                //reservoir hydrostatic pressure [kPa]
                Phydrostatic = _content.GetDoubleParameter("Reservoir Hydrostatic Pressure,", -1.0, 100, 100000.0);
                if (Phydrostatic == -1.0) usebuiltinhydrostaticpressurecorrelation = 1;

                II = _content.GetDoubleParameter("Injectivity Index,", 10.0, 0.01, 10000.0);

                if (productionwellpumping == 1)
                {
                    PI = _content.GetDoubleParameter("Productivity Index,", 10.0, 0.01, 10000.0);
                    ppwellhead = _content.GetDoubleParameter("Production Wellhead Pressure,", -1.0, 0.0, 10000.0);
                    if (ppwellhead == -1.0)
                    {
                        usebuiltinppwellheadcorrelation = 1;
                    }
                }

                //plant outlet pressure [kPa]

                Pplantoutlet = _content.GetDoubleParameter("Plant Outlet Pressure,", -1.0, 0.0, 10000.0);
                if (Pplantoutlet == -1.0)
                {
                    if (setinjectionpressurefixed == 1)
                    {
                        Pplantoutlet = 100.0;
                        if (!_content.Contains("Plant Outlet Pressure,")) usebuiltinoutletplantcorrelation = 0;
                    }
                    else
                    {
                        usebuiltinoutletplantcorrelation = 1;
                    }

                }
            }
            sstParms.Phydrostatic = Phydrostatic;
            sstParms.II = II;
            sstParms.PI = PI;
            sstParms.ppwellhead = ppwellhead;
            sstParms.Pplantoutlet = Pplantoutlet;
            sstParms.usebuiltinhydrostaticpressurecorrelation = usebuiltinhydrostaticpressurecorrelation;
            sstParms.usebuiltinppwellheadcorrelation = usebuiltinppwellheadcorrelation;
            sstParms.usebuiltinoutletplantcorrelation = usebuiltinoutletplantcorrelation;

            double Tinj = _content.GetDoubleParameter("Injection Temperature,", 70.0, 0.0, 200.0);
            sstParms.Tinj = Tinj;

            double maxdrawdown = 1.0;
            if (new[] { 1, 2, 3, 4 }.Contains(sstParms.resoption))
            {
                maxdrawdown = _content.GetDoubleParameter("Maximum Drawdown,", 1.0, 0.0, 1.0);
            }
            sstParms.maxdrawdown = maxdrawdown;

            //cprock: reservoir heat capacity (in J/kg/K)
            double cprock = _content.GetDoubleParameter("Reservoir Heat Capacity,", 1000, 100.0, 10000.0);
            sstParms.cprock = cprock;

            //rhorock: reservoir density (in kg/m3)
            double rhorock = _content.GetDoubleParameter("Reservoir Density,", 2700.0, 100.0, 20000.0);
            sstParms.rhorock = rhorock;

            //krock: reservoir thermal conductivity (in W/m/K)
            double krock = 3.0;
            if (sstParms.rameyoptionprod == 1 || new[] { 1, 2, 3 }.Contains(sstParms.resoption) || (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1))
            {
                krock = _content.GetDoubleParameter("Reservoir Thermal Conductivity,", 3.0, 0.01, 100.0);
            }
            sstParms.krock = krock;

            //porrock: reservoir porosity (-)
            double porrock = 0.04;
            if (sstParms.resoption == 2 || (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1))
            {
                porrock = _content.GetDoubleParameter("Reservoir Porosity,", 0.04, 0.001, 0.99);
            }
            sstParms.porrock = porrock;

            //permrock: reservoir permeability (m2)
            double permrock = 1E-13;
            if (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1)
            {
                permrock = _content.GetDoubleParameter("Reservoir Permeability,", 1E-13, 1E-20, 1E-5);
            }
            sstParms.permrock = permrock;

            //resthickness: reservoir thickness (m)
            double resthickness = 250.0;
            if (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1)
            {
                resthickness = _content.GetDoubleParameter("Reservoir Thickness,", 250.0, 10.0, 10000.0);
            }
            sstParms.resthickness = resthickness;

            //reswidth: reservoir width (m)
            double reswidth = 500.0;
            if (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1)
            {
                reswidth = _content.GetDoubleParameter("Reservoir Width,", 500.0, 10.0, 10000.0);
            }
            sstParms.reswidth = reswidth;

            //wellsep: well separation (m)
            double wellsep = 1000.0;
            if (sstParms.resoption == 6 && sstParms.usebuiltintough2model == 1)
            {
                wellsep = _content.GetDoubleParameter("Well Separation,", 1000.0, 10.0, 10000.0);
            }
            sstParms.wellsep = wellsep;

            return sstParms;
        }
    }
}
