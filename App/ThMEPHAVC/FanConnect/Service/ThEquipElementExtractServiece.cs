using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Engine;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThEquipElementExtractServiece
    {
        public static List<ThFanCUModel> GetFCUModels(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            {
                var fcuEngine = new ThFanCURecognitionEngine();
                var retFcu = fcuEngine.Extract(database.Database, selectArea);
                return retFcu;
            }

        }
    }
}
