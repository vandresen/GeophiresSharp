using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Models
{
    public class FinancialParameters
    {
        //Parameter name: Plant Lifetime
        //Description: System lifetime
        //Parameter type: Integer
        //Units: years
        //Allowable value range: [1,100]
        //   o When required: Always
        //   o Default value: 30 years
        public int plantlifetime { get; set; }

        //Parameter name: Economic Model
        //Description: Specify the economic model to calculate the levelized cost of energy
        //Units: N/A
        //Allowable values:
        //   1: Fixed Charge Rate Model
        //   2: Standard Levelized Cost Model
        //   3: BICYCLE Levelized Cost Model
        //When required: Always
        //   o Default value: 2
        public int econmodel { get; set; }

        //Parameter name: Fixed Charge Rate
        //Description: Fixed charge rate(FCR) used in the Fixed Charge Rate Model
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 1
        //   o Default value: 0.1
        public double FCR { get; set; }

        //Parameter name: Discount Rate
        //Description: Discount rate used in the Standard Levelized Cost Model
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 2
        //   o Default value: 0.07
        public double discountrate { get; set; }

        //Parameter name: Fraction of Investment in Bonds
        //Description: Fraction of geothermal project financing through bonds(debt); the
        //             remaining will be through equity.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0.5
        public double FIB { get; set; }

        //Parameter name: Inflated Bond Interest Rate
        //Description: Inflated bond interest rate, defined as: (1+inflated bond interest rate)
        //             = (1 + deflated bond interest rate)×(1 + interest rate). This parameter characterizes
        //             the cost of debt.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0.05
        public double BIR { get; set; }


        //Parameter name: Inflated Equity Interest Rate
        //Description: Inflated equity interest rate, defined as: (1+inflated equity interest
        //             rate) = (1 + deflated equity interest rate)×(1 + interest rate). This parameter
        //             characterizes the cost of equity.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0.1
        public double EIR { get; set; }

        //Parameter name: Inflation Rate
        //Description: Inflation rate
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [-0.1,1]
        //   o When required: If economic model = 3
        //   o Default value: 0.02
        public double RINFL { get; set; }

        //Parameter name: Combined Income Tax Rate
        //Description: Combined income tax rate.Income taxes in each year are calculated
        //             using (combined income tax rate) × (revenue – deductible expenses) – investment
        //             tax credits.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0.3
        public double CTR { get; set; }

        //Parameter name: Gross Revenue Tax Rate
        //Description: Gross revenue tax rate.Gross revenue taxes in each year are
        //             calculated using (gross revenue tax rate) × (revenue).
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0
        public double GTR { get; set; }

        //Parameter name: Investment Tax Credit Rate
        //Description: Investment tax credit rate.Investment tax credits are calculated using
        //             (investment tax credit rate) × (initial capital investment).
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0
        public double RITC { get; set; }

        //Parameter name: Property Tax Rate
        //Description: Property tax rate.Property taxes are fixed annual charges and are
        //             calculated using (property tax rate) × (initial capital investment).
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: If economic model = 3
        //   o Default value: 0
        public double PTR { get; set; }

        //Parameter name: Inflation Rate During Construction
        //Description: Initial capital investment can be inflated with this inflation rate
        //             during construction before annual revenue kicks in.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //   o When required: Always
        //   o Default value: 0
        public double inflrateconstruction { get; set; }
    }
}
