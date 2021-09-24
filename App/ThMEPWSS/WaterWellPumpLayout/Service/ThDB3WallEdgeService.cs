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
                columnEngine.Build(db, pts);
                shearWallEngine.Build(db, pts);
                archWallEngine.Build(db, pts);
                var columns = columnEngine.Elements
                    .OfType<ThIfcColumn>()
                    .Select(o => o.Outline)
                    .ToList();
                var shearwalls = shearWallEngine.Elements
                    .OfType<ThIfcWall>()
                    .Select(o => o.Outline)
                    .ToList();
                var archwalls = archWallEngine.Elements
                    .OfType<ThIfcWall>()
                    .Select(o => o.Outline)
                    .ToList();
                results.AddRange(ThWaterWellPumpUtils.ToLines(columns));
                results.AddRange(ThWaterWellPumpUtils.ToLines(archwalls));
                results.AddRange(ThWaterWellPumpUtils.ToLines(shearwalls));
                return results;
            }
        }
    }
}
