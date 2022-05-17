﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.DrainingSetting
{
    public abstract class DraningSettingService
    {
        public double scale = 1;

        public List<RouteModel> pipes;

        public ThMEPOriginTransformer originTransformer;

        public virtual void CreateDraningSetting() { }
    }
}
