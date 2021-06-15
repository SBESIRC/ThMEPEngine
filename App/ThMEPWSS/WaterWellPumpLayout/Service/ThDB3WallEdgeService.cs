using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.WaterWellPumpLayout.Interface;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThDB3WallEdgeService : IWallEdgeData
    {
        public List<Line> GetWallEdges(Database db, Point3dCollection pts)
        {
            var results = new List<Line>();
            using (var columnEngine = new ThColumnRecognitionEngine())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            using (var archWallEngine = new ThDB3ArchWallRecognitionEngine())
            {
                columnEngine.Recognize(db, pts);
                shearWallEngine.Recognize(db, pts); 
                archWallEngine.Recognize(db, pts);
                results.AddRange(ThWaterWellPumpUtils.ToLines(columnEngine.Elements.Cast<ThIfcColumn>().Select(o => o.Outline).ToList()));
                results.AddRange(ThWaterWellPumpUtils.ToLines(shearWallEngine.Elements.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                results.AddRange(ThWaterWellPumpUtils.ToLines(archWallEngine.Elements.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                return results;
            }
        }
    }
}
