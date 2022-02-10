using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryPointService
    {
        private const double PointTolerane = 2.0;
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private Dictionary<DBPoint, Point3d> PointDict { get; set; }
        public ThQueryPointService(List<Point3d> points)
        {
            PointDict = new Dictionary<DBPoint, Point3d>();
            points.ForEach(p => PointDict.Add(new DBPoint(p), p));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(PointDict.Keys.ToCollection());
        }

        public List<Point3d> Query(Line edge, double tolerance = PointTolerane)
        {
            var outline = ThDrawTool.ToOutline(edge.StartPoint, edge.EndPoint, tolerance);
            var dbPoints = SpatialIndex.SelectCrossingPolygon(outline).Cast<DBPoint>().ToList();
            return PointDict
                .Where(p=>dbPoints.Contains(p.Key))
                .Select(p=>p.Value)
                .Where(p => p.IsPointOnLine(edge, tolerance)).ToList();
        }

        private Dictionary<Line, List<Point3d>> Query(List<Line> lines, double tolerance)
        {
            var result = new Dictionary<Line, List<Point3d>>();
            lines.ForEach(l =>
            {
                var linePts = Query(l, tolerance);
                linePts = linePts.Where(p => !result.Values.SelectMany(o=>o).Contains(p)).ToList();
                result.Add(l, linePts);
            });
            return result;
        }

        public static Dictionary<Line, List<Point3d>> Query(List<BlockReference> blks, List<Line> lines,double tolerance= PointTolerane)
        {
            return Query(blks.Select(b => b.Position).ToList(), lines, tolerance);
        }

        public static Dictionary<Line, List<Point3d>> Query(List<Point3d> points, List<Line> lines, double tolerance = PointTolerane)
        {
            var queryLightBlkService = new ThQueryPointService(points);
            return queryLightBlkService.Query(lines, tolerance);
        }
    }
}
