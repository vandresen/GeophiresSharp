using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Models
{
    public class SimulationParameters
    {
        //Parameter name: Print Output to Console
        //Description: Select whether to print simulation results to the console screen
        //Parameter type: Boolean
        //Units: N/A
        //Allowable values: 0 (no) or 1 (yes)
        //When required: Always
        //Default value: 1
        public int printoutput { get; set; }

        //Parameter name: Time steps per year
        //Description: Number of internal simulation time steps per year
        //Parameter type: Integer
        //Units: N/A
        //Range of allowable values: [1,100]
        //When required: Always
        //Default value: 4
        public int timestepsperyear { get; set; }

        //Parameter name: End-Use Option
        //Description: Select the end-use application of the geofluid heat
        //Parameter type: Integer
        //Units: N/A
        //Allowable values:
        //   1: Electricity: All the geothermal fluid heat is used to generate electricity
        //      (with a certain conversion heat-to-power conversion efficiency.The
        //      levelized cost of energy is expressed as levelized cost of electricity
        //      (LCOE) in ¢/kWhe.
        //   2: Direct-Use Heat: All the geothermal fluid heat is directly used as heat.
        //      The levelized cost of energy is expressed as levelized cost of heat (LCOH)
        //      in $/MMBTU.
        //   3X: CHP Topping Cycle: An electricity generation cycle (high
        //       temperature) is in series with direct-use heat (low temperature).
        //   31: The levelized cost of energy is calculated as LCOE (¢/kWhe)
        //       with electricity considered the main product, and the heat sales
        //       considered an additional revenue stream subtracted from the
        //       annual O&M costs.
        //   32: The levelized cost of energy is calculated as LCOH
        //       ($/MMBTU) with heat considered the main product, and the
        //       electricity sales considered an additional revenue stream subtracted
        //       from the annual O&M costs.
        //   4X: CHP Bottoming Cycle: A direct-use heat application (high
        //       temperature) is in series with an electricity generation cycle (low
        //       temperature).
        //   41: The levelized cost of energy is calculated as LCOE (¢/kWhe)
        //       with electricity considered the main product, and the heat sales
        //       considered an additional revenue stream subtracted from the
        //       annual O&M costs.
        //   42: The levelized cost of energy is calculated as LCOH
        //       ($/MMBTU) with heat considered the main product, and the
        //       electricity sales considered an additional revenue stream subtracted
        //       from the annual O&M costs.
        //   5X: CHP Parallel Cycle: The geothermal fluid flow splits in two parts two
        //       serve an electricity generation cycle in parallel with direct-use heat, both
        //       at the same temperature.
        //   51: The levelized cost of energy is calculated as LCOE (¢/kWhe)
        //       with electricity considered the main product, and the heat sales
        //       considered an additional revenue stream subtracted from the
        //       annual O&M costs.
        //   52: The levelized cost of energy is calculated as LCOH
        //       ($/MMBTU) with heat considered the main product, and the
        //       electricity sales considered an additional revenue stream subtracted
        //       from the annual O&M costs.
        //When required: Always
        // Default value: 1
        public int enduseoption { get; set; }

        //Parameter name: Power Plant Type
        //Description: Specify the type of power plant in case of electricity generation
        //Parameter type: Integer
        //Units: N/A
        //Allowable values:
        //   1: Subcritical ORC
        //   2: Supercritical ORC
        //   3: Single-flash
        //   4: Double-flash
        //When required: If the end-use option is 1, 31, 32, 41, 42, 51, or 52
        //Default value: 1
        public int pptype { get; set; }
    }
}
