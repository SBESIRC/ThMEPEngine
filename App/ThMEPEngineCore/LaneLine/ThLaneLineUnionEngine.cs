using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineUnionEngine : ThLaneLineEngine
    {
        public static DBObjectCollection Union(DBObjectCollection curves)
        {
            var index = new ThCADCoreNTSSpatialIndex(curves);
            curves.Cast<Line>().ForEach(o =>
            {
                var buffer = Buffer(o, 1.0);
                var objs = index.SelectCrossingPolygon(buffer.Shell.ToDbPolyline());
                if (objs.Count > 1)
                {
                    var lines = objs.Cast<Line>().Where(l => IsParallel(o, l));
                    if (lines.Count() > 1)
                    {
                        var tag = index.Tag(o);
                        if (tag == null)
                        {
                            tag = Guid.NewGuid().ToString();
                        }
                        lines.ForEach(l => index.AddTag(l, tag));
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
                    results.Add(UnionLines(group.ToList()));
                }
            }
            return Noding(results).ToCollection();
        }

        private static bool IsParallel(Line line1, Line line2)
        {
            var angle = line1.LineDirection().GetAngleTo(line2.LineDirection());
            return (angle <= Math.PI / 180.0 || angle >= Math.PI - Math.PI / 180.0);
        }

        private static Polygon Buffer(Line line, double distance)
        {
            return line.ToNTSLineString().Buffer(distance, EndCapStyle.Flat) as Polygon;
        }

        private static Line UnionLines(List<Line> lines)
        {
            var polygons = lines.Select(o => Buffer(o, 1.0)).ToArray();
            var multiPolygon = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons);
            var results = multiPolygon.Union();
            if (results is Polygon polygon)
            {
                return CenterLine(polygon);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Line CenterLine(Polygon polygon)
        {
            var rectangle = MinimumDiameter.GetMinimumRectangle(polygon) as Polygon;
            var shell = rectangle.Shell.ToDbPolyline();
            return new Line(
                shell.GetPoint3dAt(0) + 0.5 * (shell.GetPoint3dAt(1) - shell.GetPoint3dAt(0)),
                shell.GetPoint3dAt(2) + 0.5 * (shell.GetPoint3dAt(3) - shell.GetPoint3dAt(2)));
        }
    }
}
