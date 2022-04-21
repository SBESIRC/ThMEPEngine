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
    public class ThMarkExtractionService
    {
        public List<ThMarkModel> GetMarkModelList(Point3dCollection pts=null)
        {
            var markEngine = new ThMarkExtractionEngine();
            var retList = markEngine.GetMarkListOptimized(pts);
            return retList;
        }
    }
}
