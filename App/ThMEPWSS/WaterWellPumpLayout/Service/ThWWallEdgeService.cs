using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.WaterWellPumpLayout.Engine;
using ThMEPWSS.WaterWellPumpLayout.Interface;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThWWallEdgeService : IWallEdgeData
    {
        public List<Line> GetWallEdges(Database db, Point3dCollection pts)
        {
            var results = new List<Line>();
            using (var engine = new ThWWallRecognitionEngine())
            {
                engine.Recognize(db, pts);
                return ThWaterWellPumpUtils.ToLines(engine.Results.Cast<Entity>().ToList());
            }
        }
    }
}
