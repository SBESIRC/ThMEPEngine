using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThCutCableTrayUnlinkWireService
    {
        private Dictionary<Line, Point3dCollection> WireDict { get; set; }
        private ThCADCoreNTSSpatialIndex WireSpatialIndex { get; set; }
        private double LampLength { get; set; }
        private ThCADCoreNTSSpatialIndex FdxSpatialIndex { get; set; }
        public ThCutCableTrayUnlinkWireService(Dictionary<Line, Point3dCollection> wireDict,
            double lampLength, List<Line> fdxLines)
        {
            WireDict = wireDict;
            LampLength = lampLength;
            FdxSpatialIndex = new ThCADCoreNTSSpatialIndex(fdxLines.ToCollection());
        }

        public DBObjectCollection Cut()
        {
            InitWireSpatialIndex();
            var results = new DBObjectCollection();
            WireDict.ForEach(o =>
            {
                var points = new List<Point3d>();
                var frame = o.Key.BufferSquare(10.0);
                SelectCrossingPolygon(o.Key, frame, WireSpatialIndex, points);
                SelectCrossingPolygon(o.Key, frame, FdxSpatialIndex, points);

                var direction = o.Key.LineDirection();
                foreach (Point3d position in o.Value)
                {
                    points.Add(position + direction * LampLength / 2);
                    points.Add(position - direction * LampLength / 2);
                }
                points = points.Where(pt => pt.DistanceTo(o.Key.StartPoint) < o.Key.Length + 10.0)
                    .Where(pt => pt.DistanceTo(o.Key.EndPoint) < o.Key.Length + 10.0)
                    .OrderBy(pt => pt.DistanceTo(o.Key.StartPoint)).ToList();
                if (points.Count > 1 && points.First().DistanceTo(points.Last()) > 1.0)
                {
                    results.Add(new Line(points.First(), points.Last()));
                }
            });
            return results;
        }

        private void SelectCrossingPolygon(Line line, Polyline frame, ThCADCoreNTSSpatialIndex spatialIndex, List<Point3d> points)
        {
            var filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().Except(new List<Line> { line }).ToList();
            filter.ForEach(l =>
            {
                var intersection = GetIntersectPts(l, line);
                if (intersection.Count == 1)
                {
                    points.Add(intersection[0]);
                }
            });
        }

        private Point3dCollection GetIntersectPts(Line first, Line second)
        {
            return first.IntersectWithEx(second, Intersect.ExtendBoth);
        }

        private void InitWireSpatialIndex()
        {
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(WireDict.Keys.ToCollection());
        }
    }
}
