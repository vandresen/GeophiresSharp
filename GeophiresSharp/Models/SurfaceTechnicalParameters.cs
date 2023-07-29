namespace GeophiresSharp.Models
{
    public class SurfaceTechnicalParameters
    {
        //Parameter name: Circulation Pump Efficiency
        //Description: Specify the overall efficiency of the injection and production well
        //             pumps
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0.1,1]
        //When required: Always
        //Default value: 0.75
        public double pumpeff { get; set; }

        //Parameter name: Utilization Factor
        //Description: Ratio of the time the plant is running in normal production in a 1-
        //             year time period.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0.1,1]
        //When required: Always
        //Default value: 0.9
        public double utilfactor { get; set; }

        //Parameter name: End-Use Efficiency Factor
        //Description: Constant thermal efficiency of the direct-use application
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0.1,1]
        //When required: If the end-use option is 2, 31, 32, 41, 42, 51, or 52
        //Default value: 0.9
        public double enduseefficiencyfactor { get; set; }

        // Parameter name: CHP Fraction
        //Description: Fraction of produced geofluid flow rate going to direct-use heat
        //             application in CHP parallel cycle
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0.0001, 0.9999]
        //When required: If the end-use option is 51 or 52
        //Default value: 0.5
        public double chpfraction { get; set; }

        //Parameter name: CHP Bottoming Entering Temperature
        //Description: Power plant entering geofluid temperature used in CHP bottoming
        //             cycle
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [Injection Temperature, Maximum Temperature]
        //When required: If the end-use option is 41 or 42
        //Default value: 150°C
        public double Tchpbottom { get; set; }

        //Parameter name: Surface Temperature
        //Description: Surface temperature used for calculating bottom-hole temperature
        //             (with geothermal gradient and reservoir depth)
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [-50,50]
        //When required: Always
        //Default value: 15°C
        public double Tsurf { get; set; }

        //Parameter name: Ambient Temperature
        //Description: Ambient(or dead-state) temperature used for calculating power plant
        //             utilization efficiency
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [-50,50]
        //When required: If the end-use option is 1, 31, 32, 41, 42, 51, or 52
        //Default value: 15°C
        public double Tenv { get; set; }
    }
}
