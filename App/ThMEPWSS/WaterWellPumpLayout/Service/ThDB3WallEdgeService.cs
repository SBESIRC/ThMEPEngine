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
            using (var columnEngine = new ThColumnBuilderEngine())
            using (var shearWallEngine = new ThShearwallBuilderEngine())
            using (var archWallEngine = new ThArchWallBuilderEngine())
            {
                var columns = columnEngine.Build(db, pts);
                var shearwalls =  shearWallEngine.Build(db, pts);
                var archwalls = archWallEngine.Build(db, pts);                
                results.AddRange(ThWaterWellPumpUtils.ToLines(columns.Cast<ThIfcColumn>().Select(o => o.Outline).ToList()));
                results.AddRange(ThWaterWellPumpUtils.ToLines(shearwalls.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                results.AddRange(ThWaterWellPumpUtils.ToLines(archwalls.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                return results;
            }
        }
    }
}
