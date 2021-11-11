using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    class ThSprinklerConnectEngine
    {
        public static void SprinklerConnectEngine(ThSprinklerParameter sprinklerParameter)
        {

            ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(sprinklerParameter);



        }
    }
}
