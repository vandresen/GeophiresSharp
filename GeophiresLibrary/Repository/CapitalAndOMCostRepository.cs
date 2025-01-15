using GeophiresLibrary.Extensions;
using GeophiresLibrary.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Repository
{
    public class CapitalAndOMCostRepository : ICapitalAndOMCostRepository
    {
        private readonly ILogger _logger;

        public CapitalAndOMCostRepository(ILogger<CapitalAndOMCostRepository> logger)
        {
            _logger = logger;
        }

        public CapitalAndOMCostParameters GetCapitalAndOMCostParameters(string[] _content, SimulationParameters simulationParms)
        {
            var capCostParms = new CapitalAndOMCostParameters();

            int totalcapcostvalid = 1;
            int totalcapcostprovided = 1;
            double totalcapcost = _content.GetDoubleParameter("Total Capital Cost,", -1.0, 0.0, 1000.0);
            if (totalcapcost < 0)
            {
                totalcapcostvalid = 0;
                totalcapcostprovided = 0;
            }
            capCostParms.TotalCapitalCost = totalcapcost;
            capCostParms.totalcapcostvalid = totalcapcostvalid;

            // ccwellfixed: well drilling and completion capital cost in M$ (per well)
            int ccwellfixedvalid = 1;
            int ccwellfixedprovided = 1;
            double ccwellfixed = _content.GetDoubleParameter("Well Drilling and Completion Capital Cost,", -1.0, 0.0, 200.0);
            if (ccwellfixed < 0)
            {
                ccwellfixedvalid = 0;
                ccwellfixedprovided = 0;
            }
            capCostParms.ccwellfixed = ccwellfixed;
            capCostParms.ccwellfixedvalid = ccwellfixedvalid;

            //ccwelladjfactor: adj factor for built-in correlation well drilling and completion cost
            int ccwelladjfactorvalid = 1;
            int ccwelladjfactorprovided = 1;
            double ccwelladjfactor = _content.GetDoubleParameter("Well Drilling and Completion Capital Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (ccwelladjfactor < 0)
            {
                ccwelladjfactorvalid = 0;
                ccwelladjfactorprovided = 0;
            }
            capCostParms.ccwelladjfactor = ccwelladjfactor;

            if (ccwellfixedvalid == 1 && ccwelladjfactorvalid == 1)
            {
                _logger.LogWarning("Provided well drilling and completion cost adjustment factor not considered because valid total well drilling and completion cost provided.");
            }
            else if (ccwellfixedprovided == 0 && ccwelladjfactorprovided == 0)
            {
                ccwelladjfactor = 1;
                _logger.LogWarning("No valid well drilling and completion total cost or adjustment factor provided. GEOPHIRES will assume default built-in well drilling and completion cost correlation with adjustment factor = 1.");
            }
            else if (ccwellfixedprovided == 1 && ccwellfixedvalid == 0)
            {
                _logger.LogWarning("Provided well drilling and completion cost outside of range 0-1000. GEOPHIRES will assume default built-in well drilling and completion cost correlation with adjustment factor = 1.");
                ccwelladjfactor = 1;
            }
            else if (ccwellfixedprovided == 0 && ccwelladjfactorprovided == 1 && ccwelladjfactorvalid == 0)
            {
                _logger.LogWarning("Provided well drilling and completion cost adjustment factor outside of range 0-10. GEOPHIRES will assume default built-in well drilling and completion cost correlation with adjustment factor = 1.");
                ccwelladjfactor = 1;
            }

            // Drilling cost correlation (should be 1, 2, 3, or 4) if no valid fixed well drilling cost is provided
            int wellcorrelation = 1;
            if (ccwellfixedvalid == 0)
            {
                List<int> valid_wellcorrelation = new List<int>() { 1, 2, 3, 4 };
                wellcorrelation = _content.GetIntParameter("Well Drilling Cost Correlation,", 1, valid_wellcorrelation);
            }
            capCostParms.wellcorrelation = wellcorrelation;

            // ccstimfixed: reservoir stimulation cost in M$
            int ccstimfixedvalid = 1;
            int ccstimfixedprovided = 1;
            double ccstimfixed = _content.GetDoubleParameter("Reservoir Stimulation Capital Cost,", -1.0, 0.0, 100.0);
            if (ccstimfixed < 0)
            {
                ccstimfixedvalid = 0;
                ccstimfixedprovided = 0;
            }
            capCostParms.ccstimfixed = ccstimfixed;
            capCostParms.ccstimfixedvalid = ccstimfixedvalid;

            // ccstimadjfactor: adj factor for built-in correlation for reservoir stimulation cost
            int ccstimadjfactorprovided = 0;
            int ccstimadjfactorvalid = 0;
            double ccstimadjfactor = _content.GetDoubleParameter("Reservoir Stimulation Capital Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (ccstimadjfactor < 0)
            {
                ccstimadjfactorvalid = 1;
                ccstimadjfactorprovided = 1;
            }
            capCostParms.ccstimadjfactor = ccstimadjfactor;

            if (ccstimfixedvalid == 1 && ccstimadjfactorvalid == 1)
            {
                _logger.LogWarning("Provided reservoir stimulation cost adjustment factor not considered because valid total reservoir stimulation cost provided.");
            }
            else if (ccstimfixedprovided == 0 && ccstimadjfactorprovided == 0)
            {
                ccstimadjfactor = 1;
                _logger.LogWarning("No valid reservoir stimulation total cost or adjustment factor provided. GEOPHIRES will assume default built-in reservoir stimulation cost correlation with adjustment factor = 1.");
            }
            else if (ccstimfixedprovided == 1 && ccstimfixedvalid == 0)
            {
                _logger.LogWarning("Provided reservoir stimulation cost outside of range 0-100. GEOPHIRES will assume default built-in reservoir stimulation cost correlation with adjustment factor = 1.");
                ccstimadjfactor = 1;
            }
            else if (ccstimfixedprovided == 0 && ccstimadjfactorprovided == 1 && ccstimadjfactorvalid == 0)
            {
                _logger.LogWarning("Provided reservoir stimulation cost adjustment factor outside of range 0-10. GEOPHIRES will assume default reservoir stimulation cost correlation with adjustment factor = 1.");
                ccstimadjfactor = 1;
            }

            //ccplantfixed: surface plant cost in M$
            int ccplantfixedprovided = 1;
            int ccplantfixedvalid = 1;
            double ccplantfixed = _content.GetDoubleParameter("Surface Plant Capital Cost,", -1.0, 0.0, 1000.0);
            if (ccplantfixed < 0)
            {
                ccplantfixedvalid = 0;
                ccplantfixedprovided = 0;
            }
            capCostParms.ccplantfixed = ccplantfixed;
            capCostParms.ccplantfixedvalid = ccplantfixedvalid;

            //ccplantadjfactor: adj factor for built-in surface plant cost correlation
            int ccplantadjfactorprovided = 1;
            int ccplantadjfactorvalid = 1;
            double ccplantadjfactor = _content.GetDoubleParameter("Surface Plant Capital Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (ccplantadjfactor < 0)
            {
                ccplantadjfactorprovided = 0;
                ccplantadjfactorvalid = 0;
                ccplantadjfactor = 1;
            }
            else
            {
                ccplantadjfactorvalid = 1;
            }
            capCostParms.ccplantadjfactor = ccplantadjfactor;

            if (totalcapcostvalid == 1)
            {
                if (ccplantfixedprovided == 1)
                    _logger.LogWarning("Provided surface plant cost not considered because valid total capital cost provided.");
                if (ccplantadjfactorprovided == 1)
                    _logger.LogWarning("Provided surface plant cost adjustment factor not considered because valid total capital cost provided.");
            }
            else
            {
                if (ccplantfixedvalid == 1 && ccplantadjfactorvalid == 1)
                    _logger.LogWarning("Provided surface plant cost adjustment factor not considered because valid total surface plant cost provided.");
                else if (ccplantfixedprovided == 0 && ccplantadjfactorprovided == 0)
                {
                    ccplantadjfactor = 1;
                    _logger.LogWarning("No valid surface plant total cost or adjustment factor provided. GEOPHIRES will assume default built-in surface plant cost correlation with adjustment factor = 1.");
                }
                else if (ccplantfixedprovided == 1 && ccplantfixedvalid == 0)
                {
                    _logger.LogWarning("Provided surface plant cost outside of range 0-1000. GEOPHIRES will assume default built-in surface plant cost correlation with adjustment factor = 1.");
                    ccplantadjfactor = 1;
                }
                else if (ccplantfixedprovided == 0 && ccplantadjfactorprovided == 1 && ccplantadjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided surface plant cost adjustment factor outside of range 0-10. GEOPHIRES will assume default surface plant cost correlation with adjustment factor = 1.");
                    ccplantadjfactor = 1;
                }
            }

            // ccgathfixed: field gathering system network cost in M$
            int ccgathfixedvalid = 1;
            int ccgathfixedprovided = 1;
            double ccgathfixed = _content.GetDoubleParameter("Field Gathering System Capital Cost,", -1.0, 0.0, 100.0);
            if (ccgathfixed < 0)
            {
                ccgathfixedvalid = 0;
                ccgathfixedprovided = 0;
            }
            capCostParms.ccgathfixed = ccgathfixed;
            capCostParms.ccgathfixedvalid = ccgathfixedvalid;

            // ccgathadjfactor: adj factor for built-in field gathering system cost correlation
            int ccgathadjfactorprovided = 1;
            int ccgathadjfactorvalid = 1;
            double ccgathadjfactor = _content.GetDoubleParameter("Field Gathering System Capital Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (ccgathadjfactor < 0)
            {
                ccgathadjfactorvalid = 0;
                ccgathadjfactorvalid = 0;
                ccgathadjfactor = 1;
            }
            capCostParms.ccgathadjfactor = ccgathadjfactor;

            if (totalcapcostvalid == 1)
            {
                if (ccgathfixedprovided == 1)
                    _logger.LogWarning("Provided field gathering system cost not considered because valid total capital cost provided.");
                if (ccgathadjfactorprovided == 1)
                    _logger.LogWarning("Provided field gathering system cost adjustment factor not considered because valid total capital cost provided.");
            }
            else
            {
                if (ccgathfixedvalid == 1 && ccgathadjfactorvalid == 1)
                {
                    _logger.LogWarning("Provided field gathering system cost adjustment factor not considered because valid total field gathering system cost provided.");
                }
                else if (ccgathfixedprovided == 0 && ccgathadjfactorprovided == 0)
                {
                    ccgathadjfactor = 1;
                    _logger.LogWarning("No valid field gathering system total cost or adjustment factor provided. GEOPHIRES will assume default built-in field gathering system cost correlation with adjustment factor = 1.");
                }
                else if (ccgathfixedprovided == 1 && ccgathfixedvalid == 0)
                {
                    _logger.LogWarning("Provided field gathering system cost outside of range 0-100. GEOPHIRES will assume default built-in field gathering system cost correlation with adjustment factor = 1.");
                    ccgathadjfactor = 1;
                }
                else if (ccgathfixedprovided == 0 && ccgathadjfactorprovided == 1 && ccgathadjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided field gathering system cost adjustment factor outside of range 0-10. GEOPHIRES will assume default field gathering system cost correlation with adjustment factor = 1.");
                    ccgathadjfactor = 1;
                }
            }

            // ccexplfixed: exploration cost in M$
            int ccexplfixedprovided = 1;
            int ccexplfixedvalid = 1;
            double ccexplfixed = _content.GetDoubleParameter("Exploration Capital Cost,", -1.0, 0.0, 100.0);
            if (ccexplfixed < 0)
            {
                ccexplfixedprovided = 0;
                ccexplfixedvalid = 0;
            }
            capCostParms.ccexplfixed = ccexplfixed;
            capCostParms.ccexplfixedvalid = ccexplfixedvalid;

            // ccexpladjfactor: adj factor for built-in exploration cost correlation
            int ccexpladjfactorvalid = 1;
            int ccexpladjfactorprovided = 1;
            double ccexpladjfactor = _content.GetDoubleParameter("Exploration Capital Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (ccexpladjfactor < 0)
            {
                ccexpladjfactorvalid = 0;
                ccexpladjfactorprovided = 0;
            }
            capCostParms.ccexpladjfactor = ccexpladjfactor;

            if (totalcapcostvalid == 1)
            {
                if (ccexplfixedprovided == 1)
                    _logger.LogWarning("Provided exploration cost not considered because valid total capital cost provided.");
                if (ccexpladjfactorprovided == 1)
                    _logger.LogWarning("Provided exploration cost adjustment factor not considered because valid total capital cost provided.");
            }
            else
            {
                if (ccexplfixedvalid == 1 && ccexpladjfactorvalid == 1)
                {
                    _logger.LogWarning("Provided exploration cost adjustment factor not considered because valid total exploration cost provided.");
                }
                else if (ccexplfixedprovided == 0 && ccexpladjfactorprovided == 0)
                {
                    ccexpladjfactor = 1;
                    _logger.LogWarning("No valid exploration total cost or adjustment factor provided. GEOPHIRES will assume default built-in exploration cost correlation with adjustment factor = 1.");
                }
                else if (ccexplfixedprovided == 1 && ccexplfixedvalid == 0)
                {
                    _logger.LogWarning("Provided exploration cost outside of range 0-100. GEOPHIRES will assume default built-in exploration cost correlation with adjustment factor = 1.");
                    ccexpladjfactor = 1;
                }
                else if (ccexplfixedprovided == 0 && ccexpladjfactorprovided == 1 && ccexpladjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided exploration cost adjustment factor outside of range 0-10. GEOPHIRES will assume default exploration cost correlation with adjustment factor = 1.");
                    ccexpladjfactor = 1;
                }
            }

            // pipinglength: surface piping length (-)
            double pipinglength = _content.GetDoubleParameter("Surface Piping Length,", 0.0, 0.0, 100.0);
            capCostParms.pipinglength = pipinglength;

            // O&M cost parameters
            // oamtotalfixed: total O&M cost in M$/year
            // user can provide total O&M cost (M$)
            int oamtotalfixedprovided = 1;
            int oamtotalfixedvalid = 1;
            double oamtotalfixed = _content.GetDoubleParameter("Total O&M Cost,", -1.0, 0.0, 100.0);
            if (oamtotalfixed < 0)
            {
                oamtotalfixedprovided = 0;
                oamtotalfixedvalid = 0;
            }
            capCostParms.oamtotalfixed = oamtotalfixed;
            capCostParms.oamtotalfixedvalid = oamtotalfixedvalid;

            // amwellfixed: total wellfield O&M cost in M$/year
            int oamwellfixedprovided = 1;
            int oamwellfixedvalid = 1;
            double oamwellfixed = _content.GetDoubleParameter("Wellfield O&M Cost,", -1.0, 0.0, 100.0);
            if (oamwellfixed < 0)
            {
                oamwellfixedvalid = 0;
                oamwellfixedprovided = 0;
            }
            capCostParms.oamwellfixed = oamwellfixed;
            capCostParms.oamwellfixedvalid = oamwellfixedvalid;

            // oamwelladjfactor: adj factor to built-in correlation for wellfield O&M cost
            int oamwelladjfactorprovided = 1;
            int oamwelladjfactorvalid = 1;
            double oamwelladjfactor = _content.GetDoubleParameter("Wellfield O&M Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (oamwelladjfactor < 0)
            {
                oamwelladjfactorvalid = 0;
                oamwelladjfactorprovided = 0;
            }
            capCostParms.oamwelladjfactor = oamwelladjfactor;

            if (oamtotalfixedvalid == 1)
            {
                if (oamwellfixedprovided == 1)
                    _logger.LogWarning("Provided total wellfield O&M cost not considered because valid total annual O&M cost provided.");
                if (oamwelladjfactorprovided == 1)
                    _logger.LogWarning("Provided wellfield O&M cost adjustment factor not considered because valid total annual O&M cost provided.");
            }
            else
            {
                if (oamwellfixedvalid == 1 && oamwelladjfactorvalid == 1)
                {
                    _logger.LogWarning("Provided wellfield O&M cost adjustment factor not considered because valid total wellfield O&M cost provided.");
                }
                else if (oamwellfixedprovided == 0 && oamwelladjfactorprovided == 0)
                {
                    oamwelladjfactor = 1;
                    _logger.LogWarning("No valid total wellfield O&M cost or adjustment factor provided. GEOPHIRES will assume default built-in wellfield O&M cost correlation with adjustment factor = 1.");
                }
                else if (oamwellfixedprovided == 1 && oamwellfixedvalid == 0)
                {
                    _logger.LogWarning("Provided total wellfield O&M cost outside of range 0-100. GEOPHIRES will assume default built-in wellfield O&M cost correlation with adjustment factor = 1.");
                    oamwelladjfactor = 1;
                }
                else if (oamwellfixedprovided == 0 && oamwelladjfactorprovided == 1 && oamwelladjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided wellfield O&M cost adjustment factor outside of range 0-10. GEOPHIRES will assume default wellfield O&M cost correlation with adjustment factor = 1.");
                    oamwelladjfactor = 1;
                }
            }

            // oamplantfixed: plant O&M cost in M$/year
            int oamplantfixedprovided = 1;
            int oamplantfixedvalid = 1;
            double oamplantfixed = _content.GetDoubleParameter("Surface Plant O&M Cost,", -1.0, 0.0, 100.0);
            if (oamplantfixed < 0)
            {
                oamplantfixedvalid = 0;
                oamplantfixedprovided = 0;
            }
            capCostParms.oamplantfixed = oamplantfixed;
            capCostParms.oamplantfixedvalid = oamplantfixedvalid;

            // oamplantadjfactor: adj factor for built-in correlation for plant O&M cost
            int oamplantadjfactorprovided = 1;
            int oamplantadjfactorvalid = 1;
            double oamplantadjfactor = _content.GetDoubleParameter("Surface Plant O&M Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (oamplantadjfactor < 0)
            {
                oamplantadjfactorvalid = 0;
                oamplantadjfactorvalid = 0;
            }
            capCostParms.oamplantadjfactor = oamplantadjfactor;

            if (oamtotalfixedvalid == 1)
            {
                if (oamplantfixedprovided == 1)
                    _logger.LogWarning("Provided total surface plant O&M cost not considered because valid total annual O&M cost provided.");
                if (oamplantadjfactorprovided == 1)
                    _logger.LogWarning("Provided surface plant O&M cost adjustment factor not considered because valid total annual O&M cost provided.");
            }
            else
            {
                if (oamplantfixedvalid == 1 && oamplantadjfactorvalid == 1)
                {
                    _logger.LogWarning("Provided surface plant O&M cost adjustment factor not considered because valid total surface plant O&M cost provided.");
                }
                else if (oamplantfixedprovided == 0 && oamplantadjfactorprovided == 0)
                {
                    oamplantadjfactor = 1;
                    _logger.LogWarning("No valid surface plant O&M cost or adjustment factor provided. GEOPHIRES will assume default built-in surface plant O&M cost correlation with adjustment factor = 1.");
                }
                else if (oamplantfixedprovided == 1 && oamplantfixedvalid == 0)
                {
                    _logger.LogWarning("Provided surface plant O&M cost outside of range 0-100. GEOPHIRES will assume default built-in surface plant O&M cost correlation with adjustment factor = 1.");
                    oamplantadjfactor = 1;
                }
                else if (oamplantfixedprovided == 0 && oamplantadjfactorprovided == 1 && oamplantadjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided surface plant O&M cost adjustment factor outside of range 0-10. GEOPHIRES will assume default surface plant O&M cost correlation with adjustment factor = 1.");
                    oamplantadjfactor = 1;
                }
            }

            // oamwaterfixed: total water cost in M$/year
            int oamwaterfixedprovided = 1;
            int oamwaterfixedvalid = 1;
            double oamwaterfixed = _content.GetDoubleParameter("Water Cost,", -1.0, 0.0, 100.0);
            if (oamwaterfixed < 0)
            {
                oamwaterfixedvalid = 0;
                oamwaterfixedprovided = 0;
            }
            capCostParms.oamwaterfixed = oamwaterfixed;
            capCostParms.oamwaterfixedvalid = oamwaterfixedvalid;

            // oamwateradjfactor: adj factor for built-in correlation for water cost
            int oamwateradjfactorprovided = 1;
            int oamwateradjfactorvalid = 1;
            double oamwateradjfactor = _content.GetDoubleParameter("Water Cost Adjustment Factor,", -1.0, 0.0, 10.0);
            if (oamwateradjfactor < 0)
            {
                oamwateradjfactorvalid = 0;
                oamwateradjfactorprovided = 0;
            }
            capCostParms.oamwateradjfactor = oamwateradjfactor;

            if (oamtotalfixedvalid == 1)
            {
                if (oamwaterfixedprovided == 1)
                    _logger.LogWarning("Warning: Provided total water cost not considered because valid total annual O&M cost provided.");
                if (oamwateradjfactorprovided == 1)
                    _logger.LogWarning("Warning: Provided water cost adjustment factor not considered because valid total annual O&M cost provided.");
            }
            else
            {
                if (oamwaterfixedvalid == 1 && oamwateradjfactorvalid == 1)
                {
                    _logger.LogWarning("Warning: Provided water cost adjustment factor not considered because valid total water cost provided.");
                }
                else if (oamwaterfixedprovided == 0 && oamwateradjfactorprovided == 0)
                {
                    oamwateradjfactor = 1;
                    _logger.LogWarning("Warning: No valid total water cost or adjustment factor provided. GEOPHIRES will assume default built-in water cost correlation with adjustment factor = 1.");
                }
                else if (oamwaterfixedprovided == 1 && oamwaterfixedvalid == 0)
                {
                    _logger.LogWarning("Provided total water cost outside of range 0-100. GEOPHIRES will assume default built-in water cost correlation with adjustment factor = 1.");
                    oamwateradjfactor = 1;
                }
                else if (oamwaterfixedprovided == 0 && oamwateradjfactorprovided == 1 && oamwateradjfactorvalid == 0)
                {
                    _logger.LogWarning("Provided water cost adjustment factor outside of range 0-10. GEOPHIRES will assume default water cost correlation with adjustment factor = 1.");
                    oamwateradjfactor = 1;
                }
            }

            // elecprice: electricity price (in $/kWh) to calculate pumping cost in case of direct-use or additional revenue stream from electricity sales in co-gen option
            double elecprice = -1.0;
            if (new[] { 2, 32, 42, 52 }.Contains(simulationParms.enduseoption))
            {
                elecprice = _content.GetDoubleParameter("Electricity Rate", 0.07, 0.0, 1.0);
            }
            capCostParms.elecprice = elecprice;

            // heatprice: heat price (in $/kWh) to calculate additional revenue stream from heat sales in co-gen option
            double heatprice = -1.0;
            if (new[] { 31, 41, 51 }.Contains(simulationParms.enduseoption))
            {
                heatprice = _content.GetDoubleParameter("Heat Rate", 0.02, 0.0, 1.0);
            }
            capCostParms.heatprice = heatprice;

            return capCostParms;
        }
    }
}
