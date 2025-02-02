﻿using GeophiresLibrary.Extensions;
using GeophiresLibrary.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Repository
{
    public class SurfaceTechnicalRepository : ISurfaceTechnicalRepository
    {
        private readonly ILogger _logger;

        public SurfaceTechnicalRepository(ILogger<SurfaceTechnicalRepository> logger)
        {
            _logger = logger;
        }
        public SurfaceTechnicalParameters GetSurfaceTechnicalParameters(string[] _content, SubsurfaceTechnicalParameters sstParms)
        {
            var surfTechParms = new SurfaceTechnicalParameters();

            double utilfactor = _content.GetDoubleParameter("Utilization Factor,", 0.9, 0.1, 1.0);
            surfTechParms.utilfactor = utilfactor;

            double enduseefficiencyfactor = _content.GetDoubleParameter("End-Use Efficiency Factor,", 0.9, 0.1, 1.0);
            surfTechParms.enduseefficiencyfactor = enduseefficiencyfactor;

            double chpfraction = _content.GetDoubleParameter("CHP Fraction,", 0.5, 0.0001, 0.9999);
            surfTechParms.chpfraction = chpfraction;

            //Tchpbottom: power plant entering temperature in the CHP Bottom cycle (in deg.C)
            double Tchpbottom = _content.GetDoubleParameter("CHP Bottoming Entering Temperature,", 150.0, sstParms.Tinj, sstParms.Tmax);
            surfTechParms.Tchpbottom = Tchpbottom;

            //Tsurf: surface temperature used for calculating bottomhole temperature(in deg.C)
            double Tsurf = _content.GetDoubleParameter("Surface Temperature,", 15.0, -50.0, 50.0);
            surfTechParms.Tsurf = Tsurf;

            //Tenv: ambient temperature(in deg.C)
            double Tenv = _content.GetDoubleParameter("Ambient Temperature,", 15.0, -50.0, 50.0);
            surfTechParms.Tenv = Tenv;

            double pumpeff = _content.GetDoubleParameter("Circulation Pump Efficiency,", 0.75, 0.1, 1.0);
            surfTechParms.pumpeff = pumpeff;

            return surfTechParms;
        }
    }
}
