using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThFireCompartmentExtendEngine : ThLaneLineEngine
    {
        public static DBObjectCollection Extend(DBObjectCollection curves)
        {
            var extendedLines = CreateExtendedLines(curves);
            return curves.Cast<Curve>().Union(extendedLines).ToCollection();
        }

        private static List<Line> CreateExtendedLines(DBObjectCollection lines)
        {
            var objs = new List<Line>();
            lines.Cast<Curve>().ForEach(o =>
            {
                var direction = o.CurveDirection();
                objs.Add(new Line(o.EndPoint, o.EndPoint + direction * extend_distance));
                objs.Add(new Line(o.StartPoint, o.StartPoint - direction * extend_distance));
            });
            return objs;
        }
    }
}
