using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryLightBlockService
    {
        public ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThQueryLightBlockService(DBObjectCollection dbObjs)
        {
            SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }
        public List<Point3d> Query(Line edge, double width = 1.0)
        {
            var outline = ThDrawTool.ToOutline(edge.StartPoint, edge.EndPoint, width);
            var blocks = SpatialIndex.SelectCrossingPolygon(outline).Cast<BlockReference>().ToList();
            return Filter(blocks, edge, width);
        }
        private List<Point3d> Filter(List<BlockReference> blocks, Line edge, double tolerance = 1.0)
        {
            var results = new List<Point3d>();
            blocks.ForEach(o =>
                {
                    var projectionPt = ThGeometryTool.GetProjectPtOnLine(o.Position, edge.StartPoint, edge.EndPoint);
                    if (projectionPt.DistanceTo(o.Position) <= tolerance)
                    {
                        if (ThGeometryTool.IsPointOnLine(edge.StartPoint, edge.EndPoint, projectionPt))
                        {
                            results.Add(projectionPt);
                        }
                    }
                });
            return results;
        }
    }
}
