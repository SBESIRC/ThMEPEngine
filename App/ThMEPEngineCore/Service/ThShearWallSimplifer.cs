
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPEngineCore.Service
{
    class ThShearWallSimplifier: ThBuildElementSimplifier
    {
        public ThShearWallSimplifier()
        {
            OFFSETDISTANCE= 20.0;
            ClOSED_DISTANC_TOLERANCE = 600.0;
        }
    }
}
