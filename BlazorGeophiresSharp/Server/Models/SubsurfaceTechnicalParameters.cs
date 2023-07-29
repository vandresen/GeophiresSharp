namespace BlazorGeophiresSharp.Server.Models
{
    public class SubsurfaceTechnicalParameters
    {
        //Parameter name: Reservoir Model
        //Description: Select the reservoir model to be used in GEOPHIRES
        //Parameter type: Integer
        //Units: N/A
        //Allowable values:
        //   1: Multiple parallel fractures model
        //   2: 1D linear heat sweep model
        //   3: m/a single fracture drawdown model
        //   4: Linear thermal drawdown model
        //   5: Generic user-provided temperature profile
        //   6: TOUGH2
        //When required: Always
        //Default value: 4
        public int resoption { get; set; }

        //Parameter name: Drawdown Parameter
        //Description: specify the thermal drawdown for reservoir model 3 and 4
        //Parameter type: Real
        //Units:
        //  kg/s/m2 if reservoir model = 3
        //  1/year if reservoir model = 4
        //Allowable value range:
        //   [0,0.2] if reservoir model = 3
        //   [0,0.2] if reservoir model = 4
        //When required: if reservoir model = 3 or 4
        //Default value:
        //   0.0001 if reservoir model = 3
        //   0.005 if reservoir model = 3
        public double drawdp { get; set; }

        //Parameter name: Reservoir Output File Name
        //Description: File name of reservoir output in case reservoir model 5 is selected
        //Parameter type: String
        //Units: N/A
        //Allowable values: N/A
        //When required: if reservoir model = 5
        //Default value: ReservoirOutput.txt
        public string filenamereservoiroutput { get; set; }

        //Parameter name: TOUGH2 Model/File Name
        //Description: File name of reservoir output in case reservoir model 5 is selected
        //Parameter type: String
        //Units: N/A
        //Allowable values: N/A
        //When required: if reservoir model = 5
        //Default value: ReservoirOutput.txt
        public string tough2modelfilename { get; set; }

        //Parameter name: Reservoir Depth
        //Description: Depth of the reservoir
        //Parameter type: Real
        //Units: km
        //Allowable value range: [0.1,15]
        //When required: Always
        //Default value: 3 km
        public double depth { get; set; }

        //Parameter name: Number of Segments
        //Description: Number of rock segments from surface to reservoir depth with
        //             specific geothermal gradient
        //Parameter type: Integer
        //Units: N/A
        //Allowable values: [1,2,3,4]
        //When required: Always
        //Default value: 1
        public int numseg { get; set; }

        //Parameter name: Gradient 1, Gradient 2, Gradient 3, Gradient 4
        //Description: Geothermal gradient in rock segment 1
        //Parameter type: Real
        //Units: °C/km
        //Allowable value range: [0,500]
        //When required: Always
        //Default value: 50
        public double[] gradient { get; set; }

        //Parameter name: Thickness 1
        //Description: Thickness of rock segment 1
        //Parameter type: Real
        //Units: km
        //Allowable value range: [0.01,100]
        //When required: If number of segments > 1
        //Default value: 2
        public double[] layerthickness { get; set; }

        //Parameter name: Maximum Temperature
        //Description: Maximum allowable reservoir temperature(e.g.due to drill bit or
        //             logging tools constraints). GEOPHIRES will cap the drilling depth to stay below
        //             this maximum temperature.
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [50,1000]
        //When required: Always
        //Default value: 400
        public double Tmax { get; set; }

        //Parameter name: Number of Production Wells
        //Description: Number of (identical) production wells
        //Parameter type: Integer
        //Units: N/A
        //Allowable value range: [1,20]
        //When required: Always
        //Default value: 2
        public int nprod { get; set; }

        //Parameter name: Number of Injection Wells
        //Description: Number of (identical) injection wells
        //Parameter type: Integer
        //Units: N/A
        //Allowable value range: [1,20]
        //When required: Always
        //Default value: 2
        public int ninj { get; set; }

        //Parameter name: Production Well Diameter
        //Description: Inner diameter of production wellbore (assumed constant along the
        //             wellbore) to calculate frictional pressure drop and wellbore heat transmission with
        //             Ramey’s model
        //Parameter type: Real
        //Units: Inch
        //Allowable value range: [1,30]
        //When required: Always
        //Default value: 8 inch
        public double prodwelldiam { get; set; }

        //Parameter name: Injection Well Diameter
        //Description: Inner diameter of injection wellbore (assumed constant along the
        //             wellbore) to calculate frictional pressure drop
        //Parameter type: Real
        //Units: Inch
        //Allowable value range: [1,30]
        //When required: Always
        //Default value: 8 inch
        public double injwelldiam { get; set; }

        //Parameter name: Ramey Production Wellbore Model
        //Description: Select whether to use Ramey’s model to estimate the geofluid
        //             temperature drop in the production wells.
        //Parameter type: Boolean
        //Units: N/A
        //Allowable value range: 0 (disable) or 1 (enable)
        //   o When required: Always
        //Default value: 1
        public int rameyoptionprod { get; set; }

        //Parameter name: Production Wellbore Temperature Drop
        //Description: Specify constant production well geofluid temperature drop in case
        //             Ramey’s model is disabled.
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [-5,50]
        //When required: If Ramey’s model is disabled
        //Default value: 5
        public double tempdropprod { get; set; }

        //Parameter name: Injection Wellbore Temperature Gain
        //Description: Specify constant injection well geofluid temperature gain.
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [-5,50]
        //When required: Always
        //Default value: 0
        public double tempgaininj { get; set; }

        //Parameter name: Production Flow Rate per Well
        //Description: Geofluid flow rate per production well.
        //Parameter type: Real
        //Units: kg/s
        //Allowable value range: [1,500]
        //When required: Always
        //Default value: 50
        public double prodwellflowrate { get; set; }

        //Parameter name: Fracture Shape
        //Description: Specifies the shape of the (identical) fractures in a fracture-based
        //             reservoir
        //Parameter type: Integer
        //Units: N/A
        //Allowable values:
        //   1: Circular fracture with known area
        //   2: Circular fracture with known diameter
        //   3: Square fracture
        //   4: Rectangular fracture
        //When required: If reservoir model = 1 or 2
        //Default value: 1
        public int fracshape { get; set; }

        //Parameter name: Fracture Area
        //Description: Effective heat transfer area per fracture
        //Parameter type: Real
        //Units: m2
        //Allowable value range: [1,1E8]
        //When required: If reservoir model = 1 or 2 and fracture shape = 1
        //Default value: 250,000 m2
        public double fracarea { get; set; }

        //Parameter name: Fracture Height
        //Description: Diameter (if fracture shape = 2) or height(if fracture shape = 3 or 4)
        //             of each fracture
        //Parameter type: Real
        //Units: m
        //Allowable value range: [1,10000]
        //When required: If reservoir model = 1 or 2 and fracture shape = 2, 3 or 4
        //Default value: 500 m
        public double fracheight { get; set; }

        //Parameter name: Fracture Width
        //Description: Width of each fracture
        //Parameter type: Real
        //Units: m
        //Allowable value range: [1,10000]
        //When required: If reservoir model = 1 or 2 and fracture shape = 4
        //Default value: 500 m
        public double fracwidth { get; set; }

        //Parameter name: Reservoir Volume Option
        //Description: Specifies how the reservoir volume, and fracture distribution(for
        //             reservoir models 1 and 2) are calculated.The reservoir volume is used by
        //             GEOPHIRES to estimate the stored heat in place.The fracture distribution is
        //             needed as input for the EGS fracture-based reservoir models 1 and 2.
        //Parameter type: Integer
        //Units: N/A
        //Allowable values:
        //   1: Specify number of fractures and fracture separation
        //   2: Specify reservoir volume and fracture separation
        //   3: Specify reservoir volume and number of fractures
        //   4: Specify reservoir volume only (sufficient for reservoir models 3, 4, 5
        //      and 6)
        //When required: Always
        //Default value:
        //  4 if reservoir model = 1 or 2
        //  3 if reservoir model = 3, 4, 5 or 6
        public int resvoloption { get; set; }

        //Parameter name: Number of Fractures
        //Description: Number of identical parallel fractures in EGS fracture-based
        //             reservoir model.
        //Parameter type: Integer
        //Units: N/A
        //Allowable value range: [1,20]
        //When required: If reservoir model = 1 or 2 and reservoir volume option = 1 or 3
        //Default value: 10
        public double fracnumb { get; set; }

        //Parameter name: Fracture Separation
        //Description: Separation of identical parallel fractures with uniform spatial
        //             distribution in EGS fracture-based reservoir.
        //Parameter type: real
        //Units: m
        //Allowable value range: [1,1E4]
        //When required: If reservoir model = 1 or 2 and reservoir volume option = 1 or 2
        //Default value: 50 m
        public double fracsep { get; set; }

        //Parameter name: Reservoir Volume
        //Description: Geothermal reservoir volume
        //Parameter type: real
        //Units: m3
        //Allowable value range: [10,1E12]
        //When required: If reservoir volume option = 3 or 4
        //Default value: 500 × 500 × 500 m3 = 125,000,000 m3
        public double resvol { get; set; }

        //Parameter name: Water Loss Fraction
        //Description: Fraction of water lost in the reservoir defined as (total geofluid
        //             lost)/(total geofluid produced). The total injection flow rate is then calculated as:
        //             (injection rate) = (production rate) × (1+ water loss fraction)
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,0.99]
        //When required: If reservoir volume option = 3 or 4
        //Default value: 0
        public double waterloss { get; set; }

        //Parameter name: Reservoir Impedance
        //Description: Reservoir resistance to flow per well-pair.For EGS-type reservoirs
        //             when the injection well is in hydraulic communication with the production well,
        //             this parameter specifies the overall pressure drop in the reservoir between
        //             injection well and production well, calculated as (reservoir impedance) ×
        //             (reservoir flow rate).
        //Parameter type: Real
        //Units: GPa·s/m3
        //Allowable value range: [1E-4,1E4]
        //When required: Optional; default is specifying productivity and injectivity indices
        //Default value: 0.1
        public double impedance { get; set; }

        //Parameter name: Productivity Index
        //Description: Productivity index defined as ratio of production well flow rate over
        //             production well inflow pressure drop(hydrostatic reservoir pressure – flowing
        //             bottom hole pressure).
        //Parameter type: Real
        //Units: kg/s/bar
        //Allowable value range: [1E-2,1E4]
        //When required: Required if not specifying reservoir impedance and no flash
        //               power plant considered
        //Default value: 10
        public double PI { get; set; }

        //Parameter name: Injectivity Index
        //Description: Injectivity index defined as ratio of injection well flow rate over
        //             injection well outflow pressure drop(flowing bottom hole pressure - hydrostatic
        //             reservoir pressure).
        //Parameter type: Real
        //Units: kg/s/bar
        //Allowable value range: [1E-2,1E4]
        //When required: Required if not specifying reservoir impedance
        //Default value: 10
        public double II { get; set; }

        //Parameter name: Reservoir Hydrostatic Pressure
        //Description: Reservoir hydrostatic far-field pressure
        //Parameter type: Real
        //Units: kPa
        //Allowable value range: [1E2,1E5]
        //When required: Required if specifying productivity/injectivity indices.
        //Default value: Built-in modified Xie-Bloomfield-Shook equation (DOE, 2016).
        public double Phydrostatic { get; set; }

        //Parameter name: Production Wellhead Pressure
        //Description: Constant production wellhead pressure
        //Parameter type: Real
        //Units: kPa
        //Allowable value range: [0,1E4]
        //When required: Required if specifying productivity index
        //Default value: Water vapor pressure(at initial production temperature) + 344.7
        //               kPa(50 psi).
        public double ppwellhead { get; set; }

        //Parameter name: Plant Outlet Pressure
        //Description: Constant plant outlet pressure equal to injection well pump(s)
        //             suction pressure
        //Parameter type: Real
        //Units: kPa
        //Allowable value range: [0,1E4]
        //When required: Required if specifying injectivity index
        //Default value:
        //   100 kPa(1 bar) in case of flash power plant
        //   Production wellhead pressure – 68.95 kPa(10 psi) in all other cases
        public double Pplantoutlet { get; set; }

        //Parameter name: Injection Temperature
        //Description: Constant geofluid injection temperature at injection wellhead.
        //Parameter type: Real
        //Units: °C
        //Allowable value range: [0,200]
        //When required: Always
        //Default value: 70°C
        public double Tinj { get; set; }

        //Parameter name: Maximum Drawdown
        //Description: Maximum allowable thermal drawdown before redrilling of all wells
        //             into new reservoir(most applicable to EGS-type reservoirs with heat farming
        //             strategies). E.g.a value of 0.2 means that all wells are redrilled after the
        //             production temperature(at the wellhead) has dropped by 20% of its initial
        //             temperature.
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0,1]
        //When required: If reservoir model is 1, 2, 3, or 4.
        //Default value: 1 (i.e., no redrilling considered)
        public double maxdrawdown { get; set; }

        //Parameter name: Reservoir Heat Capacity
        //Description: Constant and uniform reservoir rock heat capacity
        //Parameter type: Real
        //Units: J/kg/K
        //Allowable value range: [100,10000]
        //When required: Always
        //Default value: 1000 J/kg/K
        public double cprock { get; set; }

        //Parameter name: Reservoir Density
        //Description: Constant and uniform reservoir rock density
        //Parameter type: Real
        //Units: kg/m3
        //Allowable value range: [100,10000]
        //When required: Always
        //Default value: 2700 kg/m3
        public double rhorock { get; set; }

        //Parameter name: Reservoir Thermal Conductivity
        //Description: Constant and uniform reservoir rock thermal conductivity
        //Parameter type: Real
        //Units: W/m/K
        //Allowable value range: [0.01,100]
        //When required: If use of Ramey model, or reservoir model = 1, 2 or 3, or
        //               reservoir model = 6 and use of built-in TOUGH2 model
        //Default value: 3 W/m/K
        public double krock { get; set; }

        //Parameter name: Reservoir Porosity
        //Description: Constant and uniform reservoir porosity
        //Parameter type: Real
        //Units: N/A
        //Allowable value range: [0.001,0.99]
        //When required: If reservoir model = 2, or reservoir model = 6 and use of built-in
        //               TOUGH2 model
        //Default value: 0.04
        public double porrock { get; set; }

        //Parameter name: Reservoir Permeability
        //Description: Constant and uniform reservoir permeability
        //Parameter type: Real
        //Units: m2
        //Allowable value range: [1E-20,1E-5]
        //When required: If reservoir model = 6 and use of built-in TOUGH2 model
        //Default value: 1E-13 m2
        public double permrock { get; set; }

        //Parameter name: Reservoir Thickness
        //Description: Reservoir thickness for built-in TOUGH2 doublet reservoir model
        //Parameter type: Real
        //Units: m
        //Allowable value range: [10,10000]
        //When required: If reservoir model = 6 and use of built-in TOUGH2 model
        //Default value: 250 m
        public double resthickness { get; set; }

        //Parameter name: Reservoir Width
        //Description: Reservoir width for built-in TOUGH2 doublet reservoir model
        //Parameter type: Real
        //Units: m
        //Allowable value range: [10,10000]
        //When required: If reservoir model = 6 and use of built-in TOUGH2 model
        //Default value: 500 m
        public double reswidth { get; set; }

        //Parameter name: Well Separation
        //Description: Well separation for built-in TOUGH2 doublet reservoir model
        //Parameter type: Real
        //Units: m
        //Allowable value range: [10,10000]
        //When required: If reservoir model = 6 and use of built-in TOUGH2 model
        //Default value: 1000 m
        public double wellsep { get; set; }

        public int usebuiltintough2model { get; set; }
        public double impedancemodelused { get; set; }
        public int usebuiltinhydrostaticpressurecorrelation { get; set; }
        public int usebuiltinppwellheadcorrelation { get; set; }
        public int productionwellpumping { get; set; }
        public int usebuiltinoutletplantcorrelation { get; set; }
    }
}
