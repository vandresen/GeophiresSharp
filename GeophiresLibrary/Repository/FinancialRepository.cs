using GeophiresLibrary.Extensions;
using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Repository
{
    public class FinancialRepository : IFinancialRepository
    {
        public async Task<FinancialParameters> GetFinancialParameters(string[] _content, SimulationParameters simulationParms)
        {
            var finParms = new FinancialParameters();

            //plantlifetime: plant lifetime (years)
            int plantlifetime = 30;
            int? tmpInt = _content.GetIntFromContent("Plant Lifetime,");
            if (tmpInt < 1 || tmpInt > 100)
            {
                plantlifetime = 30;
            }
            else if (tmpInt != null)
            {
                plantlifetime = (int)tmpInt;
            }
            finParms.plantlifetime = plantlifetime;

            //econmodel
            //econmodel = 1: use Fixed Charge Rate Model (requires an FCR)
            //econmodel = 2: use standard LCOE/LCOH calculation as found on wikipedia (requries an interest rate).
            //econmodel = 3: use Bicycle LCOE/LCOH model (requires several financial input parameters)
            List<int> valid_econmodel = new List<int>() { 1, 2, 3 };
            int econmodel = _content.GetIntParameter("Economic Model,", 2, valid_econmodel);
            finParms.econmodel = econmodel;

            //FCR: fixed charge rate required if econmodel = 1
            double FCR = 0.1;
            if (econmodel == 1)
            {
                FCR = _content.GetDoubleParameter("Fixed Charge Rate,", 0.1, 0.0, 1.0);
            }
            finParms.FCR = FCR;

            //discountrate: discount rate required if econmodel = 2
            double discountrate = 0.07;
            if (econmodel == 2)
            {
                discountrate = _content.GetDoubleParameter("Discount Rate,", 0.07, 0.0, 1.0);
            }
            finParms.discountrate = discountrate;

            //a whole bunch of BICYCLE parameters provided if econmodel = 3
            double? FIB;
            double? BIR;
            double? EIR;
            double? RINFL;
            double? CTR;
            double? GTR;
            double? RITC;
            double? PTR;
            if (econmodel == 3)
            {
                //bicycle parameters 
                //FIB: fraction of investment in bonds (-)
                FIB = _content.GetDoubleParameter("Fraction of Investment in Bonds,", 0.5, 0.0, 1.0);
                finParms.FIB = (double)FIB;
                //BIR: Inflated bond interest rate
                BIR = _content.GetDoubleParameter("Inflated Bond Interest Rate,", 0.05, 0.0, 1.0);
                finParms.BIR = (double)BIR;
                //EIR: inflated equity interest rate (-)
                EIR = _content.GetDoubleParameter("Inflated Equity Interest Rate,", 0.1, 0.0, 1.0);
                finParms.EIR = (double)EIR;
                //RINFL: inflation rate (-)
                RINFL = _content.GetDoubleParameter("Inflation Rate,", 0.02, -0.1, 1.0);
                finParms.RINFL = (double)RINFL;
                //CTR: combined income tax rate in fraction (-)
                CTR = _content.GetDoubleParameter("Combined Income Tax Rate,", 0.3, 0.0, 1.0);
                finParms.CTR = (double)CTR;
                //GTR: gross revenue tax rate in fraction (-)
                GTR = _content.GetDoubleParameter("Gross Revenue Tax Rate,", 0.0, 0.0, 1.0);
                finParms.GTR = (double)GTR;
                //RITC: investment tax credit rate in fraction (-)
                RITC = _content.GetDoubleParameter("Investment Tax Credit Rate,", 0.0, 0.0, 1.0);
                finParms.RITC = (double)RITC;
                //PTR: property tax rate in fraction (-)
                PTR = _content.GetDoubleParameter("Property Tax Rate,", 0.0, 0.0, 1.0);
                finParms.PTR = (double)PTR;
            }

            //inflrateconstruction: inflation rate during construction (-)
            double inflrateconstruction = _content.GetDoubleParameter("Inflation Rate During Construction,", 0.0, 0.0, 1.0);
            finParms.inflrateconstruction = inflrateconstruction;

            return finParms;
        }
    }
}
