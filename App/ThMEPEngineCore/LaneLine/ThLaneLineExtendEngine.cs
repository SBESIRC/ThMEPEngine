using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineExtendEngine : ThLaneLineEngine
    {
        public static DBObjectCollection Extend(DBObjectCollection curves)
        {
            var extendedLines = CreateExtendedLines(curves);
            var allLines = curves.Cast<Line>().Union(extendedLines).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(allLines);
            extendedLines.RemoveAll(o =>
            {
                var objs = spatialIndex.SelectFence(o);
                objs.Remove(o);
                return !IsProperIntersects(objs, o);
            });
            return curves.Cast<Line>().Union(extendedLines).ToCollection();
        }

        private static bool IsProperIntersects(DBObjectCollection lines, Line line)
        {
            var geometry = lines.ToMultiLineString().Intersection(line.ToNTSGeometry());
            // 判断是否存在多个交点（但是要排查共线的情况）
            return geometry is MultiPoint;
        }

        private static List<Line> CreateExtendedLines(DBObjectCollection lines)
        {
            var objs = new List<Line>();
            lines.Cast<Line>().ForEach(o =>
            {
                var direction = o.LineDirection();
                objs.Add(new Line(o.EndPoint, o.EndPoint + direction * extend_distance));
                objs.Add(new Line(o.StartPoint, o.StartPoint - direction * extend_distance));
            });
            return objs;
        }
    }
}
