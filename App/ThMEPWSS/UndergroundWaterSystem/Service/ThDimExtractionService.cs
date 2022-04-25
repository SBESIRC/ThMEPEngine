using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Engine;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThDimExtractionService
    {
        public List<ThDimModel> GetDimModelList(Point3dCollection pts)
        {
            var dimExtractionEngine = new ThDimExtractionEngine();
            var retList = dimExtractionEngine.GetDimListOptimized(pts);
            return retList;
        }
    }
}
