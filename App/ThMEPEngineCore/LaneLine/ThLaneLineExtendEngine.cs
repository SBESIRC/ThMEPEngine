using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineExtendEngine : ThLaneLineEngine
    {
        public static DBObjectCollection Extend(DBObjectCollection curves)
        {
            var lines = ExplodeCurves(curves).ToCollection();
            var nodedLines = NodingLines(CreateExtendedLines(lines));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(nodedLines.ToCollection());
            nodedLines.RemoveAll(l =>
            {
                if (l.Length <= extend_distance)
                {
                    var objs = spatialIndex.SelectFence(l);
                    objs.Remove(l);
                    return (!IntersectsAtPoint(objs, l.StartPoint) || !IntersectsAtPoint(objs, l.EndPoint));
                }
                return false;
            });
            return nodedLines.ToCollection();
        }

        private static bool IntersectsAtPoint(DBObjectCollection lines, Point3d pt)
        {
            return lines.Cast<Line>().Where(o => o.IsOnLine(pt)).Any();
        }

        private static DBObjectCollection CreateExtendedLines(DBObjectCollection curves)
        {
            var objs = new List<Line>();
            objs.AddRange(curves.Cast<Line>());
            curves.Cast<Line>().ForEach(o =>
            {
                var direction = o.LineDirection();
                objs.Add(new Line(o.EndPoint, o.EndPoint + direction * extend_distance));
                objs.Add(new Line(o.StartPoint, o.StartPoint - direction * extend_distance));
            });
            return objs.ToCollection();
        }
    }
}
