using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
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
        public static List<Line> GetFanPipes()
        {
            using (var database = AcadDatabase.Active())
            {
                var retLines = new List<Line>();
                var tmpLines = database.ModelSpace.OfType<Entity>().Where(o => o.Layer.Contains("AI-水管路由示意")).ToList();
                foreach(var l in tmpLines)
                {
                    if(l is Line)
                    {
                        retLines.Add(l as Line);
                    }
                    else if(l is Polyline)
                    {
                        var pl = l as Polyline;
                        retLines.AddRange(pl.ToLines());
                    }
                }
                return retLines;
            }
        }
    }
}
