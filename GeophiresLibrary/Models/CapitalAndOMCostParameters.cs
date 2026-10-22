namespace GeophiresLibrary.Models
{
    public class CapitalAndOMCostParameters
    {
        //Parameter name: Total Capital Cost
        //Description: Total initial capital cost.
        //Units: M$
        //Allowable value range: [0,1000]
        //When required: Optional
        //Default value: If not provided or out of range, GEOPHIRES will calculate total
        //               capital cost using built-in capital cost correlations for each plant component.
        public double TotalCapitalCost { get; set; }

        //Parameter name: Well Drilling and Completion Capital Cost
        //Description: Total fixed drilling and completion capital cost per well
        //Units: M$/well
        //Allowable value range: [0,200]
        //When required: Optional
        //Default value: If not provided or out of range, GEOPHIRES will calculate well
        //               drilling and completion capital cost using built-in correlation.
        public double ccwellfixed { get; set; }

        //Parameter name: Well Drilling and Completion Capital Cost Adjustment Factor
        //Description: Multiplier for built-in well drilling and completion capital cost
        //correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //When required: If no well drilling and completion capital cost number provided
        //Default value: 1 (i.e., use built-in correlation as is)
        public double ccwelladjfactor { get; set; }

        //Parameter name: Well Drilling Cost Correlation
        //Description: Select the built-in well drilling and completion cost correlation
        //Parameter type: Integer
        //Units: N/A
        //Allowable value range:
        //  1: vertical small diameter, baseline
        //  2: deviated small diameter, baseline
        //  3: vertical large diameter, baseline
        //  4: deviated large diameter, baseline
        //  5: Simple (per-meter cost)
        //  6: vertical small diameter, intermediate1
        //  7: vertical small diameter, intermediate2
        //  8: deviated small diameter, intermediate1
        //  9: deviated small diameter, intermediate2
        //  10: vertical large diameter, intermediate1
        //  11: vertical large diameter, intermediate2
        //  12: deviated large diameter, intermediate1
        //  13: deviated large diameter, intermediate2
        //  14: vertical open-hole, small diameter, ideal
        //  15: deviated liner, small diameter, ideal
        //  16: vertical open-hole, large diameter, ideal
        //  17: deviated liner, large diameter, ideal
        //  (1-4: 2025 NREL cost curves; 6-17: GeoVision; 5 uses the all-in vertical drilling cost per meter)
        //When required: If no valid fixed well drilling and completion capital cost is provided
        //Default value: 10 (GEOPHIRES-X default; use 1 to keep the previous default)
        public int wellcorrelation { get; set; } = 1;

        //Parameter name: Reservoir Stimulation Capital Cost
        //Description: Total reservoir stimulation capital cost
        //Parameter type: Real
        //Units: M$
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate
        //                    reservoir stimulation capital cost using built-in correlation.
        public double ccstimfixed { get; set; }

        //Parameter name: Reservoir Stimulation Capital Cost Adjustment Factor
        //Description: Multiplier for built-in reservoir stimulation capital cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no reservoir stimulation capital cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double ccstimadjfactor { get; set; }

        //Parameter name: Surface Plant Capital Cost
        //Description: Total surface plant capital cost
        //Parameter type: Real
        //Units: M$
        //Allowable value range: [0,1000]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate surface
        //                    plant capital cost using built-in correlation.
        public double ccplantfixed { get; set; }

        //Parameter name: Surface Plant Capital Cost Adjustment Factor
        //Description: Multiplier for built-in surface plant capital cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no surface plant capital cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double ccplantadjfactor { get; set; }

        //Parameter name: Field Gathering System Capital Cost
        //Description: Total field gathering system capital cost
        //Parameter type: Real
        //Units: M$
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate field
        //                    gathering system capital cost using built-in correlation.
        public double ccgathfixed { get; set; }

        //Parameter name: Field Gathering System Capital Cost Adjustment Factor
        //Description: Multiplier for built-in field gathering system capital cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no field gathering system capital cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double ccgathadjfactor { get; set; }

        //Parameter name: Exploration Capital Cost
        //Description: Total exploration capital cost
        //Parameter type: Real
        //Units: M$
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate
        //                    exploration capital cost using built-in correlation.
        public double ccexplfixed { get; set; }

        //Parameter name: Exploration Capital Cost Adjustment Factor
        //Description: Multiplier for built-in exploration capital cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no exploration capital cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double ccexpladjfactor { get; set; }

        //Parameter name: Total O&M Cost
        //Description: Total initial O&M cost.
        //Parameter type: Real
        //Units: M$/year
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate total
        //                    O&M cost using built-in O&M cost correlations for each plant component.
        public double oamtotalfixed { get; set; }

        //Parameter name: Wellfield O&M Cost
        //Description: Total annual wellfield O&M cost
        //Parameter type: Real
        //Units: M$/year
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate annual
        //                    wellfield O&M cost using built-in correlation.
        public double oamwellfixed { get; set; }

        //Parameter name: Wellfield O&M Cost Adjustment Factor
        //Description: Multiplier for built-in wellfield O&M cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no annual wellfield O&M cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double oamwelladjfactor { get; set; }

        //Parameter name: Surface Plant O&M Cost
        //Description: Total annual surface plant O&M cost
        //Parameter type: Real
        //Units: M$/year
        //Allowable value range: [0,100]
        //   o When required: Optional 22
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate annual
        //                    surface plant O&M cost using built-in correlation.
        public double oamplantfixed { get; set; }

        //Parameter name: Surface Plant O&M Cost Adjustment Factor
        //Description: Multiplier for built-in surface plant O&M cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no annual surface plant O&M cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double oamplantadjfactor { get; set; }

        //Parameter name: Water Cost
        //Description: Total annual make-up water cost
        //Parameter type: Real
        //Units: M$/year
        //Allowable value range: [0,100]
        //   o When required: Optional
        //   o Default value: If not provided or out of range, GEOPHIRES will calculate annual
        //                    make-up water cost using built-in correlation.
        public double oamwaterfixed { get; set; }

        //Parameter name: Water Cost Adjustment Factor
        //Description: Multiplier for built-in make-up water cost correlation
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,10]
        //   o When required: If no annual make-up water cost number provided
        //   o Default value: 1 (i.e., use built-in correlation as is)
        public double oamwateradjfactor { get; set; }

        //Parameter name: Electricity Rate
        //Description: Price of electricity to calculate pumping costs in direct-use heat only
        //             mode or revenue from electricity sales in CHP mode.
        //Parameter type: Real
        //Units: $/kWh
        //Range of allowable values: [0,1]
        //   o When required: In case end-use option = 2, 32, 42, or 52
        //   o Default value: 0.07
        public double elecprice { get; set; }

        //Parameter name: Heat Rate
        //Description: Price of heat to calculate revenue from heat sales in CHP mode.
        //Parameter type: Real
        //Units: $/kWh
        //Range of allowable values: [0,1]
        //   o When required: In case end-use option = 31, 41, or 51
        //   o Default value: 0.02
        public double heatprice { get; set; }

        // All-in vertical drilling cost. Units: $/m. Range: [0,10000]. Default: 1000
        public double verticaldrillingcostperm { get; set; } = 1000.0;

        // Indirect capital cost for well drilling and completion, as a percentage of direct cost.
        // Units: %. Range: [0,100]. Default: 5
        public double wellindirectpercent { get; set; } = 5.0;

        //Additional parameters
        public int ccwellfixedvalid { get; set; }
        public int ccstimfixedvalid { get; set; }
        public int ccgathfixedvalid { get; set; }
        public int ccplantfixedvalid { get; set; }
        public int totalcapcostvalid { get; set; }
        public int ccexplfixedvalid { get; set; }
        public double pipinglength { get; set; }
        public int oamtotalfixedvalid { get; set; }
        public int oamplantfixedvalid { get; set; }
        public int oamwellfixedvalid { get; set; }
        public int oamwaterfixedvalid { get; set; }
    }
}
