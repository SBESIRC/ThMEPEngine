using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThBreakExtractionService
    {
        public List<ThBreakModel> GetBreakModelList(Point3dCollection pts)
        {
            var retList = new List<ThBreakModel>();
            return retList;
        }
    }
}
