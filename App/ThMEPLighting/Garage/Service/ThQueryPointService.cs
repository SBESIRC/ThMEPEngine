using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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

        private List<Tuple<Point3d, Vector3d>> PointDirs { get; set; }

        public ThQueryPointService(List<Tuple<Point3d,Vector3d>> pointDirs) 
            :this(pointDirs.Select(o=>o.Item1).ToList())
        {
            PointDirs = pointDirs;
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

        private Dictionary<Line, List<Point3d>> QueryWithDirection(List<Line> lines, double tolerance)
        {
            var result = new Dictionary<Line, List<Point3d>>();
            lines.ForEach(l =>
            {
                var linePts = Query(l, tolerance);
                var direction = l.LineDirection();
                var res = Query(linePts);
                res = res
                .Where(o => o.Item2.IsParallelToEx(direction))
                .Where(o => !result.Where(m => m.Value.Contains(o.Item1) && m.Key.LineDirection().IsParallelToEx(o.Item2)).Any())
                .ToList();
                result.Add(l, res.Select(o=>o.Item1).ToList());
            });
            return result;
        }

        private List<Tuple<Point3d,Vector3d>> Query(List<Point3d> pts)
        {
            return PointDirs.Where(o => pts.Contains(o.Item1)).ToList();
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
        public static Dictionary<Line, List<Point3d>> Query(List<Tuple<Point3d,Vector3d>> points, List<Line> lines, double tolerance = PointTolerane)
        {
            var queryLightBlkService = new ThQueryPointService(points);
            return queryLightBlkService.QueryWithDirection(lines, tolerance);
        }
    }
}
