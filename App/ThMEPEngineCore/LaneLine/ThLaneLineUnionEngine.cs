using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineUnionEngine
    {
        public static DBObjectCollection Union(DBObjectCollection curves)
        {
            var index = new ThCADCoreNTSSpatialIndex(curves);
            curves.Cast<Line>().ForEach(o =>
            {
                if (index.Tag(o) == null)
                {
                    var tag = Guid.NewGuid().ToString();
                    var buffer = o.ToNTSLineString().Buffer(1.0) as Polygon;
                    var objs = index.SelectCrossingPolygon(buffer.Shell.ToDbPolyline());
                    if (objs.Count > 1)
                    {
                        var lines = objs.Cast<Line>().Where(l => IsParallel(o, l));
                        if (lines.Count() > 1)
                        {
                            lines.ForEach(l => index.AddTag(l, tag));
                        }
                    }
                }
            });
            var results = new DBObjectCollection();
            var groups = curves.Cast<Line>().GroupBy(o => index.Tag(o));
            foreach (var group in groups)
            {
                if (group.Key == null)
                {
                    group.ForEach(o => results.Add(o.WashClone()));
                }
                else
                {
                    results.Add(MergeLines(group.ToList()));
                }
            }
            return results;
        }

        private static bool IsParallel(Line line1, Line line2)
        {
            var angle = line1.LineDirection().GetAngleTo(line2.LineDirection());
            return (angle <= Math.PI / 180.0 || angle >= Math.PI - Math.PI / 180.0);
        }

        private static Curve MergeLines(List<Line> lines)
        {
            var geometries = new List<Geometry>();
            lines.Cast<Line>().ForEach(o => geometries.Add(o.ToNTSGeometry().Buffer(1.0)));
            var polygons = geometries.Cast<Polygon>().ToArray();
            var multiPolygon = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons);
            var results = multiPolygon.Buffer(0.0);
            if (results is Polygon polygon)
            {
                return CenterLine(polygon);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Curve CenterLine(Polygon polygon)
        {
            var rectangle = MinimumDiameter.GetMinimumRectangle(polygon) as Polygon;
            var shell = rectangle.Shell.ToDbPolyline();
            return new Line(
                shell.GetPoint3dAt(0) + 0.5 * (shell.GetPoint3dAt(1) - shell.GetPoint3dAt(0)),
                shell.GetPoint3dAt(2) + 0.5 * (shell.GetPoint3dAt(3) - shell.GetPoint3dAt(2)));
        }
    }
}
